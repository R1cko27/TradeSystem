// ViewModels/Products/ProductsViewModels.cs

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using TradeSystem.App.ViewModels.Abstractions;
using TradeSystem.Application.Contracts.Services;
using TradeSystem.Application.Dto;
using TradeSystem.Application.Mappers;
using TradeSystem.Domain.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace TradeSystem.App.ViewModels.Products;

// Список товаров с поиском и фильтром дефицита.
public partial class ProductsListViewModel : ViewModelBase, IActivatable
{
    private readonly ICatalogService _catalog; // Сервис справочников

    private IReadOnlyList<ProductDto> _all = Array.Empty<ProductDto>(); // Полный список до фильтрации

    public ObservableCollection<ProductDto> Items { get; } = new(); // Отображаемые товары

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
    private ProductDto? _selectedItem; // Выбранный товар

    [ObservableProperty]
    private string _searchText = string.Empty; // Строка поиска

    [ObservableProperty]
    private bool _onlyLowStock; // Показывать только товары к пополнению

    [ObservableProperty]
    private bool _showInactive; // Показывать неактивные товары

    public event Action<Guid?>? EditRequested; // Запрос на редактирование
    public event Action? AddRequested; // Запрос на добавление

    public ProductsListViewModel(ICatalogService catalog, IDialogService dialog)
        : base(dialog)
    {
        _catalog = catalog;
    }

    public Task OnActivatedAsync() => RunAsync(LoadCoreAsync); // Загрузить данные при активации ViewModel

    [RelayCommand]
    private Task RefreshAsync() => RunAsync(LoadCoreAsync);

    [RelayCommand]
    private void Add() => AddRequested?.Invoke();

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void Edit()
    {
        if (SelectedItem is not null)
            EditRequested?.Invoke(SelectedItem.Id);
    }

    // Удаляет выбранный товар после подтверждения.
    [RelayCommand(CanExecute = nameof(HasSelection))]
    private Task DeleteAsync()
    {
        var target = SelectedItem;
        if (target is null) return Task.CompletedTask;

        if (!Dialog.Confirm($"Удалить товар «{target.Name}»?", "Удаление товара"))
            return Task.CompletedTask;

        return RunAsync(async () =>
        {
            await _catalog.DeleteProductAsync(target.Id);
            SelectedItem = null;
            await LoadCoreAsync();
        });
    }

    private bool HasSelection => SelectedItem is not null; // Есть ли выбранная строка

    partial void OnSearchTextChanged(string value) => ApplyFilter(); // Перефильтровать при поиске
    partial void OnOnlyLowStockChanged(bool value) => ApplyFilter(); // Перефильтровать по дефициту
    partial void OnShowInactiveChanged(bool value) => _ = RunAsync(LoadCoreAsync); // Перечитать список

    // Загружает товары без RunAsync: вызывается из уже активного RunAsync.
    private async Task LoadCoreAsync()
    {
        _all = await _catalog.GetProductsAsync(includeInactive: ShowInactive);
        ApplyFilter();
    }

    // Фильтрует по поиску и признаку дефицита.
    private void ApplyFilter()
    {
        IEnumerable<ProductDto> query = _all;

        if (OnlyLowStock)
            query = query.Where(p => p.NeedsReplenish);

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var s = SearchText.Trim();
            query = query.Where(p =>
                ContainsIgnoreCase(p.Name, s) || ContainsIgnoreCase(p.Article, s));
        }

        Items.Clear();
        foreach (var item in query.OrderBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase))
            Items.Add(item);
    }

    private static bool ContainsIgnoreCase(string source, string value) =>
        source.Contains(value, StringComparison.CurrentCultureIgnoreCase);
}

// Создание и редактирование товара.
public partial class ProductEditViewModel : ViewModelBase
{
    private readonly ICatalogService _catalog; // Сервис справочников
    private Guid? _productId; // Id редактируемого товара (null — новый)

    [ObservableProperty] private bool _isNew = true; // Признак новой записи
    [ObservableProperty] private bool _isActive = true; // Признак активности
    [ObservableProperty] private string _name = string.Empty; // Наименование
    [ObservableProperty] private string _article = string.Empty; // Артикул
    [ObservableProperty] private MeasurementUnit _unit = MeasurementUnit.Piece; // Единица измерения
    [ObservableProperty] private int _minimumStockQuantity; // Минимальный остаток
    [ObservableProperty] private string _targetStockText = string.Empty; // Целевой остаток (текст)
    [ObservableProperty] private Guid? _defaultShelfId; // Идентификатор полки по умолчанию
    [ObservableProperty] private IReadOnlyList<ShelfDto> _shelves = Array.Empty<ShelfDto>(); // Доступные полки

    // Варианты единиц измерения для ComboBox.
    public IReadOnlyList<UnitOption> AvailableUnits { get; } =
        Enum.GetValues<MeasurementUnit>()
            .Select(u => new UnitOption(u, u.Of()))
            .ToList();

    public event Action? CloseRequested; // Запрос на закрытие

    public ProductEditViewModel(ICatalogService catalog, IDialogService dialog)
        : base(dialog)
    {
        _catalog = catalog;
    }

    // Загружает форму; id == null — новый товар.
    public async Task LoadAsync(Guid? id)
    {
        await RunAsync(async () =>
        {
            _productId = id;
            IsNew = id is null;

            Shelves = await _catalog.GetShelvesAsync(includeInactive: false);

            if (id is null)
            {
                ResetForm();
                return;
            }

            var p = await _catalog.GetProductAsync(id.Value);
            if (p is null)
            {
                Dialog.ShowError("Товар не найден.");
                ResetForm();
                return;
            }

            Name = p.Name;
            Article = p.Article;
            Unit = p.Unit;
            IsActive = p.IsActive;
            MinimumStockQuantity = p.MinimumStockQuantity;
            TargetStockText = p.TargetStockQuantity?.ToString() ?? string.Empty;
            DefaultShelfId = p.DefaultShelfId;
        });
    }

    [RelayCommand]
    private Task SaveAsync() => RunAsync(SaveImplementationAsync);

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke();

    // Проверяет поля и сохраняет товар.
    private async Task SaveImplementationAsync()
    {
        var errors = Validate(out int? target);
        if (errors.Count > 0)
        {
            Dialog.ShowError(string.Join(Environment.NewLine, errors));
            return;
        }

        var input = new ProductInput
        {
            Id = _productId,
            Name = Name.Trim(),
            Article = Article.Trim(),
            Unit = Unit,
            MinimumStockQuantity = MinimumStockQuantity,
            TargetStockQuantity = target,
            IsActive = IsActive,
            DefaultShelfId = DefaultShelfId
        };

        if (IsNew)
            await _catalog.CreateProductAsync(input);
        else
            await _catalog.UpdateProductAsync(input);

        CloseRequested?.Invoke();
    }

    // Проверяет поля и разбирает целевой остаток.
    private List<string> Validate(out int? target)
    {
        target = null;
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Name))
            errors.Add("Наименование товара обязательно.");
        if (string.IsNullOrWhiteSpace(Article))
            errors.Add("Артикул товара обязателен.");
        if (MinimumStockQuantity < 0)
            errors.Add("Минимальный остаток не может быть отрицательным.");

        if (!string.IsNullOrWhiteSpace(TargetStockText))
        {
            if (!int.TryParse(TargetStockText.Trim(), out var t) || t < 0)
                errors.Add("Целевой остаток должен быть целым числом >= 0.");
            else
                target = t;
        }

        return errors;
    }

    // Очищает форму.
    private void ResetForm()
    {
        Name = string.Empty;
        Article = string.Empty;
        Unit = MeasurementUnit.Piece;
        IsActive = true;
        MinimumStockQuantity = 0;
        TargetStockText = string.Empty;
        DefaultShelfId = null;
    }
}

// Значение перечисления и его подпись для ComboBox.
public sealed record UnitOption(MeasurementUnit Value, string Label);
