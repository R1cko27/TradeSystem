// ViewModels/Sales/SalesViewModels.cs

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using TradeSystem.App.ViewModels.Abstractions;
using TradeSystem.Application.Contracts.Services;
using TradeSystem.Application.Dto;
using TradeSystem.Domain.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace TradeSystem.App.ViewModels.Sales;

// Список продаж с поиском.
public partial class SalesListViewModel : ViewModelBase, IActivatable
{
    private readonly ISaleService _sales; // Сервис продаж
    private IReadOnlyList<SaleDto> _all = Array.Empty<SaleDto>(); // Полный список до фильтрации

    public ObservableCollection<SaleDto> Items { get; } = new(); // Отображаемые продажи

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PostCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
    private SaleDto? _selectedSale; // Выбранная продажа

    [ObservableProperty] private string _searchText = string.Empty; // Строка поиска

    public event Action? NewRequested; // Запрос на создание

    public SalesListViewModel(ISaleService sales, IDialogService dialog)
        : base(dialog)
    {
        _sales = sales;
    }

    public Task OnActivatedAsync() => RunAsync(() => ReloadCoreAsync(SelectedSale?.Id)); // Загрузить данные при активации ViewModel

    [RelayCommand]
    private Task RefreshAsync() => RunAsync(() => ReloadCoreAsync(SelectedSale?.Id));

    [RelayCommand]
    private void New() => NewRequested?.Invoke();

    [RelayCommand(CanExecute = nameof(CanPost))]
    // Проводит выбранную продажу после подтверждения.
    private Task PostAsync()
    {
        var target = SelectedSale;
        if (target is null) return Task.CompletedTask;

        if (!Dialog.Confirm(
                $"Провести продажу {target.Number}? Товар будет списан с указанных полок.",
                "Проведение продажи"))
            return Task.CompletedTask;

        return RunAsync(async () =>
        {
            await _sales.PostAsync(target.Id);
            await ReloadCoreAsync(target.Id);
        });
    }

    [RelayCommand(CanExecute = nameof(CanDelete))]
    // Удаляет черновик продажи.
    private Task DeleteAsync()
    {
        var target = SelectedSale;
        if (target is null) return Task.CompletedTask;

        if (!Dialog.Confirm($"Удалить черновик продажи {target.Number}?", "Удаление"))
            return Task.CompletedTask;

        return RunAsync(async () =>
        {
            await _sales.DeleteDraftAsync(target.Id);
            await ReloadCoreAsync(null);
        });
    }

    private bool CanPost => SelectedSale?.Status == SaleStatus.Draft; // Провести можно черновик
    private bool CanDelete => SelectedSale?.Status == SaleStatus.Draft; // Удалить можно черновик

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    // Загружает продажи без RunAsync: вызывается из уже активного RunAsync.
    private async Task ReloadCoreAsync(Guid? keepSelectedId)
    {
        _all = await _sales.GetSalesAsync();
        ApplyFilter(keepSelectedId);
    }

    // Фильтрует по номеру, сохраняя выбор.
    private void ApplyFilter(Guid? keepSelectedId = null)
    {
        IEnumerable<SaleDto> query = _all;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var s = SearchText.Trim();
            query = query.Where(x => Contains(x.Number, s));
        }

        var previousId = keepSelectedId ?? SelectedSale?.Id;

        Items.Clear();
        foreach (var item in query)
            Items.Add(item);

        if (previousId is { } pid)
            SelectedSale = Items.FirstOrDefault(x => x.Id == pid);
    }

    private static bool Contains(string? source, string value) =>
        !string.IsNullOrEmpty(source) && source.Contains(value, StringComparison.CurrentCultureIgnoreCase);
}

// Создание продажи: строки, полки и итог.
public partial class SaleEditViewModel : ViewModelBase
{
    private readonly ISaleService _sales; // Сервис продаж
    private readonly ICatalogService _catalog; // Сервис справочников
    private readonly IStockService _stock; // Складской сервис

    [ObservableProperty] private SaleLineViewModel? _selectedLine; // Выбранная строка
    [ObservableProperty] private IReadOnlyList<ProductDto> _availableProducts = Array.Empty<ProductDto>(); // Товары для добавления

    public ObservableCollection<SaleLineViewModel> Lines { get; } = new(); // Позиции продажи

    public decimal TotalAmount => Lines.Sum(l => l.Total); // Итог по позициям

    public event Action? CloseRequested; // Запрос на закрытие

    public SaleEditViewModel(
        ISaleService sales,
        ICatalogService catalog,
        IStockService stock,
        IDialogService dialog)
        : base(dialog)
    {
        _sales = sales;
        _catalog = catalog;
        _stock = stock;

        // Живой итог: пересчитываем при изменении состава и содержимого строк.
        Lines.CollectionChanged += OnLinesCollectionChanged;
    }

    // Очищает форму и загружает товары.
    public async Task LoadAsync()
    {
        await RunAsync(async () =>
        {
            SelectedLine = null;
            Lines.Clear();
            AvailableProducts = await _catalog.GetProductsAsync(includeInactive: false);
            OnPropertyChanged(nameof(TotalAmount));
        });
    }

    // Добавляет строку и подписывается на смену товара.
    [RelayCommand]
    private void AddLine()
    {
        var line = new SaleLineViewModel();
        line.ProductChanged += pid => _ = ReloadShelvesAsync(line, pid);
        Lines.Add(line);
        SelectedLine = line;
    }

    // Удаляет выбранную строку.
    [RelayCommand(CanExecute = nameof(HasLineSelection))]
    private void RemoveLine()
    {
        if (SelectedLine is null) return;

        SelectedLine.PropertyChanged -= OnLinePropertyChanged;
        Lines.Remove(SelectedLine);
        SelectedLine = Lines.LastOrDefault();
    }

    private bool HasLineSelection => SelectedLine is not null; // Есть выбранная строка

    // Пересчитывает итог при изменении состава строк.
    private void OnLinesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
            foreach (SaleLineViewModel l in e.NewItems)
                l.PropertyChanged += OnLinePropertyChanged;

        if (e.OldItems is not null)
            foreach (SaleLineViewModel l in e.OldItems)
                l.PropertyChanged -= OnLinePropertyChanged;

        OnPropertyChanged(nameof(TotalAmount));
    }

    // Пересчитывает итог при изменении количества или цены.
    private void OnLinePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(SaleLineViewModel.Quantity)
                          or nameof(SaleLineViewModel.UnitPrice))
        {
            OnPropertyChanged(nameof(TotalAmount));
        }
    }

    // Загружает полки с остатком выбранного товара.
    private async Task ReloadShelvesAsync(SaleLineViewModel line, Guid productId)
    {
        if (productId == Guid.Empty)
        {
            line.AvailableShelves = Array.Empty<ShelfStockOption>();
            line.ShelfId = Guid.Empty;
            return;
        }

        var balances = await _stock.GetBalancesByProductAsync(productId);

        // Гонка: если за время await товар сменился — отбрасываем устаревший результат.
        if (line.ProductId != productId)
            return;

        line.AvailableShelves = balances
            .Where(b => b.Quantity > 0)
            .Select(b => new ShelfStockOption(b.ShelfId, b.ShelfCode, b.ShelfName, b.Quantity))
            .OrderBy(o => o.ShelfCode, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        line.ShelfId = Guid.Empty;
    }

    // Сохраняет черновик продажи.
    [RelayCommand]
    private Task SaveDraftAsync() => RunAsync(async () =>
    {
        if (!TryBuildInput(out var input)) return;
        await _sales.CreateDraftAsync(input);
        CloseRequested?.Invoke();
    });

    // Сохраняет и сразу проводит продажу.
    [RelayCommand]
    private Task SaveAndPostAsync() => RunAsync(async () =>
    {
        if (!TryBuildInput(out var input)) return;
        var draft = await _sales.CreateDraftAsync(input);
        await _sales.PostAsync(draft.Id);
        CloseRequested?.Invoke();
    });

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke();

    // Проверяет форму и собирает входные данные.
    private bool TryBuildInput(out SaleInput input)
    {
        input = new SaleInput();

        var errors = Validate();
        if (errors.Count > 0)
        {
            Dialog.ShowError(string.Join(Environment.NewLine, errors));
            return false;
        }

        input.Lines = Lines.Select(l => new SaleLineInput
        {
            ProductId = l.ProductId,
            ShelfId = l.ShelfId,
            Quantity = l.Quantity,
            UnitPrice = l.UnitPrice
        }).ToList();

        return true;
    }

    // Проверяет товары, полки, количество и остатки.
    private List<string> Validate()
    {
        var errors = new List<string>();

        if (Lines.Count == 0)
            errors.Add("Добавьте хотя бы одну позицию продажи.");

        for (int i = 0; i < Lines.Count; i++)
        {
            var l = Lines[i];
            var n = i + 1;

            if (l.ProductId == Guid.Empty)
                errors.Add($"Позиция {n}: не выбран товар.");
            if (l.ShelfId == Guid.Empty)
                errors.Add($"Позиция {n}: не выбрана полка.");
            if (l.Quantity <= 0)
                errors.Add($"Позиция {n}: количество должно быть больше нуля.");
            if (l.UnitPrice < 0)
                errors.Add($"Позиция {n}: цена продажи не может быть отрицательной.");

            var available = l.AvailableOnShelf;
            if (l.ShelfId != Guid.Empty && l.Quantity > available)
                errors.Add($"Позиция {n}: на полке доступно {available}, запрошено {l.Quantity}.");
        }

        // Одна и та же пара товар+полка в нескольких строках теряет смысл.
        var dup = Lines
            .Where(l => l.ProductId != Guid.Empty && l.ShelfId != Guid.Empty)
            .GroupBy(l => (l.ProductId, l.ShelfId))
            .FirstOrDefault(g => g.Count() > 1);
        if (dup is not null)
            errors.Add("Один и тот же товар на одной полке указан в нескольких позициях.");

        return errors;
    }
}

// Строка продажи: товар, полка, количество и цена.
public partial class SaleLineViewModel : ObservableObject
{
    [ObservableProperty] private Guid _productId; // Товар
    [ObservableProperty] private Guid _shelfId; // Полка

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Total))]
    private int _quantity = 1; // Количество

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Total))]
    private decimal _unitPrice; // Цена продажи

    [ObservableProperty] private IReadOnlyList<ShelfStockOption> _availableShelves = Array.Empty<ShelfStockOption>(); // Полки с остатком выбранного товара

    // Событие смены товара: редактор перезагружает полки.
    public event Action<Guid>? ProductChanged;

    partial void OnProductIdChanged(Guid value) => ProductChanged?.Invoke(value); // Сообщает редактору о смене товара

    public decimal Total => UnitPrice * Math.Max(0, Quantity); // Стоимость строки

    // Остаток выбранного товара на выбранной полке.
    public int AvailableOnShelf =>
        ShelfId == Guid.Empty
            ? 0
            : AvailableShelves.FirstOrDefault(s => s.ShelfId == ShelfId)?.Available ?? 0;
}

// Полка с остатком товара для ComboBox выбора полки.
public sealed record ShelfStockOption(Guid ShelfId, string ShelfCode, string ShelfName, int Available)
{
    public string Display => $"{ShelfCode} · {ShelfName} (остаток {Available})"; // Текст для ComboBox
}
