// ViewModels/Receipts/ReceiptsViewModels.cs

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using TradeSystem.App.ViewModels.Abstractions;
using TradeSystem.Application.Contracts.Services;
using TradeSystem.Application.Dto;
using TradeSystem.Domain.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace TradeSystem.App.ViewModels.Receipts;

// Список поступлений товара с поиском.
public partial class ReceiptsListViewModel : ViewModelBase, IActivatable
{
    private readonly IGoodsReceiptService _receipts; // Сервис поступлений
    private IReadOnlyList<GoodsReceiptDto> _all = Array.Empty<GoodsReceiptDto>(); // Полный список до фильтрации

    public ObservableCollection<GoodsReceiptDto> Items { get; } = new(); // Отображаемые поступления

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PostCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
    private GoodsReceiptDto? _selectedReceipt; // Выбранное поступление

    [ObservableProperty] private string _searchText = string.Empty; // Строка поиска

    public event Action? NewRequested; // Запрос на создание

    public ReceiptsListViewModel(IGoodsReceiptService receipts, IDialogService dialog)
        : base(dialog)
    {
        _receipts = receipts;
    }

    public Task OnActivatedAsync() => RunAsync(() => ReloadCoreAsync(SelectedReceipt?.Id));

    [RelayCommand]
    private Task RefreshAsync() => RunAsync(() => ReloadCoreAsync(SelectedReceipt?.Id));

    [RelayCommand]
    private void New() => NewRequested?.Invoke();

    [RelayCommand(CanExecute = nameof(CanPost))]
    // Проводит выбранное поступление после подтверждения.
    private Task PostAsync()
    {
        var target = SelectedReceipt;
        if (target is null) return Task.CompletedTask;

        if (!Dialog.Confirm(
                $"Провести поступление {target.Number}? Товар будет оприходован на указанные полки.",
                "Проведение поступления"))
            return Task.CompletedTask;

        return RunAsync(async () =>
        {
            await _receipts.PostAsync(target.Id);
            await ReloadCoreAsync(target.Id);
        });
    }

    [RelayCommand(CanExecute = nameof(CanDelete))]
    // Удаляет черновик поступления.
    private Task DeleteAsync()
    {
        var target = SelectedReceipt;
        if (target is null) return Task.CompletedTask;

        if (!Dialog.Confirm($"Удалить черновик поступления {target.Number}?", "Удаление"))
            return Task.CompletedTask;

        return RunAsync(async () =>
        {
            await _receipts.DeleteDraftAsync(target.Id);
            await ReloadCoreAsync(null);
        });
    }

    private bool CanPost => SelectedReceipt?.Status == GoodsReceiptStatus.Draft; // Провести можно черновик
    private bool CanDelete => SelectedReceipt?.Status == GoodsReceiptStatus.Draft; // Удалить можно черновик

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    // Загружает поступления без RunAsync: вызывается из уже активного RunAsync.
    private async Task ReloadCoreAsync(Guid? keepSelectedId)
    {
        _all = await _receipts.GetReceiptsAsync();
        ApplyFilter(keepSelectedId);
    }

    // Фильтрует по номеру и поставщику, сохраняя выбор.
    private void ApplyFilter(Guid? keepSelectedId = null)
    {
        IEnumerable<GoodsReceiptDto> query = _all;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var s = SearchText.Trim();
            query = query.Where(r => Contains(r.Number, s) || Contains(r.SupplierName, s));
        }

        var previousId = keepSelectedId ?? SelectedReceipt?.Id;

        Items.Clear();
        foreach (var item in query)
            Items.Add(item);

        if (previousId is { } pid)
            SelectedReceipt = Items.FirstOrDefault(r => r.Id == pid);
    }

    private static bool Contains(string? source, string value) =>
        !string.IsNullOrEmpty(source) && source.Contains(value, StringComparison.CurrentCultureIgnoreCase);
}

// Режим создания поступления: по заказу или свободный.
public enum ReceiptSourceMode
{
    ByOrder = 0, // По заказу поставщика
    Free = 1 // Свободное поступление
}

// Режим и его подпись для ComboBox.
public sealed record ReceiptSourceModeOption(ReceiptSourceMode Value, string Label);

// Создание поступления: по заказу или свободным приходом.
public partial class ReceiptEditViewModel : ViewModelBase
{
    private readonly IGoodsReceiptService _receipts; // Сервис поступлений
    private readonly IPurchaseOrderService _orders; // Сервис заказов
    private readonly ICatalogService _catalog; // Сервис справочников

    private IReadOnlyList<PurchaseOrderDto> _allOrders = Array.Empty<PurchaseOrderDto>(); // Все заказы для фильтра
    private Dictionary<Guid, Guid?> _defaultShelfByProduct = new(); // Полка по умолчанию по товару

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoadOrderLinesCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddLineCommand))]
    [NotifyCanExecuteChangedFor(nameof(RemoveLineCommand))]
    private ReceiptSourceMode _sourceMode = ReceiptSourceMode.ByOrder; // Режим поступления

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoadOrderLinesCommand))]
    private Guid _supplierId; // Выбранный поставщик

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoadOrderLinesCommand))]
    private Guid _selectedOrderId; // Выбранный заказ

    [ObservableProperty] private ReceiptLineViewModel? _selectedLine; // Выбранная строка
    [ObservableProperty] private IReadOnlyList<SupplierDto> _suppliers = Array.Empty<SupplierDto>(); // Поставщики
    [ObservableProperty] private IReadOnlyList<ShelfDto> _shelves = Array.Empty<ShelfDto>(); // Полки
    [ObservableProperty] private IReadOnlyList<SupplierProductDto> _availableProducts = Array.Empty<SupplierProductDto>(); // Товары поставщика
    [ObservableProperty] private IReadOnlyList<PurchaseOrderDto> _ordersForSupplier = Array.Empty<PurchaseOrderDto>(); // Доступные заказы поставщика

    public ObservableCollection<ReceiptLineViewModel> Lines { get; } = new(); // Позиции поступления

    private IReadOnlyList<SupplierDto> _allSuppliers = Array.Empty<SupplierDto>(); // Полный список поставщиков; в режиме «по заказу» сужается до доступных

    // Варианты режима поступления для ComboBox.
    public IReadOnlyList<ReceiptSourceModeOption> SourceModes { get; } = new[]
    {
        new ReceiptSourceModeOption(ReceiptSourceMode.ByOrder, "По заказу поставщика"),
        new ReceiptSourceModeOption(ReceiptSourceMode.Free, "Свободное поступление")
    };

    public event Action? CloseRequested; // Запрос на закрытие

    public ReceiptEditViewModel(
        IGoodsReceiptService receipts,
        IPurchaseOrderService orders,
        ICatalogService catalog,
        IDialogService dialog)
        : base(dialog)
    {
        _receipts = receipts;
        _orders = orders;
        _catalog = catalog;
    }

    // Загружает справочники и сбрасывает форму.
    public async Task LoadAsync()
    {
        await RunAsync(async () =>
        {
            SourceMode = ReceiptSourceMode.ByOrder;
            _appliedSupplierId = Guid.Empty;
            SupplierId = Guid.Empty;
            SelectedOrderId = Guid.Empty;
            SelectedLine = null;
            Lines.Clear();

            var suppliersTask = _catalog.GetSuppliersAsync(includeInactive: false);
            var shelvesTask = _catalog.GetShelvesAsync(includeInactive: false);
            var productsTask = _catalog.GetProductsAsync(includeInactive: false);
            var ordersTask = _orders.GetOrdersAsync();
            await Task.WhenAll(suppliersTask, shelvesTask, productsTask, ordersTask);

            _allSuppliers = await suppliersTask;
            Shelves = await shelvesTask;
            _allOrders = await ordersTask;

            var products = await productsTask;
            _defaultShelfByProduct = products.ToDictionary(p => p.Id, p => p.DefaultShelfId);

            // Поставщики для режима «по заказу» определяются после загрузки заказов.
            ApplySupplierFilter();

            AvailableProducts = Array.Empty<SupplierProductDto>();
            OrdersForSupplier = Array.Empty<PurchaseOrderDto>();
        });
    }

    // Оставляет поставщиков, доступных в выбранном режиме.
    private void ApplySupplierFilter()
    {
        if (SourceMode == ReceiptSourceMode.Free)
        {
            Suppliers = _allSuppliers;
            return;
        }

        var allowed = _allOrders
            .Where(IsReceivableOrder)
            .Select(o => o.SupplierId)
            .ToHashSet();

        Suppliers = _allSuppliers.Where(s => allowed.Contains(s.Id)).ToList();
    }

    // Заказ годен для поступления: не отменён/закрыт, есть недополученное и он оплачен.
    private static bool IsReceivableOrder(PurchaseOrderDto order)
        => order.Status is not (PurchaseOrderStatus.Cancelled or PurchaseOrderStatus.Closed)
           && order.PaymentStatus == PaymentStatus.Paid
           && order.Lines.Any(l => !l.IsClosed && l.RemainingQuantity > 0);

    private Guid _appliedSupplierId = Guid.Empty; // Ранее выбранный поставщик
    private bool _restoringSupplierId; // Флаг отката выбора

    partial void OnSupplierIdChanged(Guid value) // Реагирует на смену поставщика
    {
        if (_restoringSupplierId) return;
        _ = HandleSupplierChangedAsync(value);
    }

    // Смена поставщика очищает строки и загружает его товары.
    private async Task HandleSupplierChangedAsync(Guid supplierId)
    {
        // Смена поставщика сбрасывает заказ и строки: они принадлежат другому поставщику.
        if (Lines.Count > 0 &&
            !Dialog.Confirm("Смена поставщика очистит добавленные позиции. Продолжить?", "Смена поставщика"))
        {
            // Пользователь отказался — возвращаем прежнего поставщика, иначе строки
            _restoringSupplierId = true;
            SupplierId = _appliedSupplierId;
            _restoringSupplierId = false;
            return;
        }

        _appliedSupplierId = supplierId;

        SelectedOrderId = Guid.Empty;
        Lines.Clear();
        SelectedLine = null;

        if (supplierId == Guid.Empty)
        {
            AvailableProducts = Array.Empty<SupplierProductDto>();
            OrdersForSupplier = Array.Empty<PurchaseOrderDto>();
            return;
        }

        await RunAsync(async () =>
        {
            AvailableProducts = await _catalog.GetSupplierProductsBySupplierAsync(supplierId);

            // В поступление по заказу попадают только оплаченные активные заказы.
            OrdersForSupplier = _allOrders
                .Where(o => o.SupplierId == supplierId && IsReceivableOrder(o))
                .OrderByDescending(o => o.OrderDateUtc)
                .ToList();
        });
    }

    // Смена режима пересобирает список поставщиков.
    partial void OnSourceModeChanged(ReceiptSourceMode value)
    {
        Lines.Clear();
        SelectedLine = null;

        // Список поставщиков зависит от режима: по заказу — только с оплаченными заказами.
        ApplySupplierFilter();

        // Если выбранный поставщик в новом режиме недоступен — сбрасываем выбор.
        if (SupplierId != Guid.Empty && Suppliers.All(s => s.Id != SupplierId))
            ResetSupplierSelection();
    }

    // Сбрасывает выбранного поставщика и связанные списки.
    private void ResetSupplierSelection()
    {
        _restoringSupplierId = true;
        SupplierId = Guid.Empty;
        _restoringSupplierId = false;

        _appliedSupplierId = Guid.Empty;
        SelectedOrderId = Guid.Empty;
        AvailableProducts = Array.Empty<SupplierProductDto>();
        OrdersForSupplier = Array.Empty<PurchaseOrderDto>();
    }

    // Смена заказа очищает строки, чтобы не потерять ввод.
    partial void OnSelectedOrderIdChanged(Guid value)
    {
        // Не перегружаем строки молча, чтобы не потерять ручной ввод: очистка + явная кнопка.
        Lines.Clear();
        SelectedLine = null;
    }

    // Заполняет строки незакрытыми позициями заказа.
    [RelayCommand(CanExecute = nameof(CanLoadOrderLines))]
    private Task LoadOrderLinesAsync() => RunAsync(async () =>
    {
        var order = _allOrders.FirstOrDefault(o => o.Id == SelectedOrderId);
        if (order is null)
        {
            Dialog.ShowWarning("Заказ не найден.");
            return;
        }

        var openLines = order.Lines.Where(l => !l.IsClosed && l.RemainingQuantity > 0).ToList();
        if (openLines.Count == 0)
        {
            Dialog.ShowInfo("У этого заказа нет незакрытых позиций для прихода.");
            return;
        }

        Lines.Clear();
        foreach (var l in openLines)
        {
            _defaultShelfByProduct.TryGetValue(l.ProductId, out var defShelf);
            Lines.Add(new ReceiptLineViewModel
            {
                ProductId = l.ProductId,
                ShelfId = defShelf ?? Guid.Empty,
                Quantity = l.RemainingQuantity,
                MaxQuantity = l.RemainingQuantity,
                OrderLineId = l.Id,
                IsProductEditable = false
            });
        }
        SelectedLine = Lines.FirstOrDefault();
    });

    private bool CanLoadOrderLines => // Загружать строки можно в режиме по заказу с выбранным заказом
        SourceMode == ReceiptSourceMode.ByOrder && SelectedOrderId != Guid.Empty;

    // Добавляет пустую строку в свободном режиме.
    [RelayCommand(CanExecute = nameof(CanAddLine))]
    private void AddLine()
    {
        Lines.Add(new ReceiptLineViewModel { IsProductEditable = true });
        SelectedLine = Lines.Last();
    }

    private bool CanAddLine => SourceMode == ReceiptSourceMode.Free && SupplierId != Guid.Empty; // Добавлять можно в свободном режиме при выбранном поставщике

    // Удаляет выбранную строку.
    [RelayCommand(CanExecute = nameof(HasLineSelection))]
    private void RemoveLine()
    {
        if (SelectedLine is null) return;
        Lines.Remove(SelectedLine);
        SelectedLine = Lines.LastOrDefault();
    }

    private bool HasLineSelection => SelectedLine is not null; // Есть выбранная строка

    // Сохраняет черновик поступления.
    [RelayCommand]
    private Task SaveDraftAsync() => RunAsync(async () =>
    {
        if (!TryBuildInput(out var input)) return;
        await _receipts.CreateDraftAsync(input);
        CloseRequested?.Invoke();
    });

    // Сохраняет и сразу проводит поступление.
    [RelayCommand]
    private Task SaveAndPostAsync() => RunAsync(async () =>
    {
        if (!TryBuildInput(out var input)) return;
        var draft = await _receipts.CreateDraftAsync(input);
        await _receipts.PostAsync(draft.Id);
        CloseRequested?.Invoke();
    });

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke();

    // Проверяет форму и собирает входные данные.
    private bool TryBuildInput(out GoodsReceiptInput input)
    {
        input = new GoodsReceiptInput();

        var errors = Validate();
        if (errors.Count > 0)
        {
            Dialog.ShowError(string.Join(Environment.NewLine, errors));
            return false;
        }

        input.SupplierId = SupplierId;
        input.OrderId = SourceMode == ReceiptSourceMode.ByOrder ? SelectedOrderId : null;
        input.Lines = Lines.Select(l => new GoodsReceiptLineInput
        {
            OrderLineId = l.OrderLineId,
            ProductId = l.ProductId,
            ShelfId = l.ShelfId,
            Quantity = l.Quantity
        }).ToList();

        return true;
    }

    // Проверяет обязательные поля, количества и дубли.
    private List<string> Validate()
    {
        var errors = new List<string>();

        if (SupplierId == Guid.Empty)
            errors.Add("Выберите поставщика.");
        if (SourceMode == ReceiptSourceMode.ByOrder && SelectedOrderId == Guid.Empty)
            errors.Add("Выберите заказ (или переключитесь в свободный режим).");
        if (Lines.Count == 0)
            errors.Add("Добавьте хотя бы одну позицию поступления.");

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
            if (l.MaxQuantity is { } max && l.Quantity > max)
                errors.Add($"Позиция {n}: количество больше остатка по заказу ({max}).");
        }

        // Один товар на одну полку в рамках одного поступления — иначе смысл строки теряется.
        var dup = Lines
            .Where(l => l.ProductId != Guid.Empty && l.ShelfId != Guid.Empty)
            .GroupBy(l => (l.ProductId, l.ShelfId))
            .FirstOrDefault(g => g.Count() > 1);
        if (dup is not null)
            errors.Add("Один и тот же товар на одну и ту же полку указан в нескольких позициях.");

        return errors;
    }
}

// Строка поступления: товар, полка и количество.
public partial class ReceiptLineViewModel : ObservableObject
{
    [ObservableProperty] private Guid _productId; // Товар
    [ObservableProperty] private Guid _shelfId; // Полка
    [ObservableProperty] private int _quantity = 1; // Количество

    [ObservableProperty] private int? _maxQuantity; // Верхняя граница количества (null — без лимита)

    public Guid? OrderLineId { get; set; } // Ссылка на строку заказа (не меняется)

    public bool IsProductEditable { get; set; } // Можно ли менять товар (false — товар из заказа)
}
