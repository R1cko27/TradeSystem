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

public partial class ReceiptsListViewModel : ViewModelBase, IActivatable
{
    private readonly IGoodsReceiptService _receipts;
    private IReadOnlyList<GoodsReceiptDto> _all = Array.Empty<GoodsReceiptDto>();

    public ObservableCollection<GoodsReceiptDto> Items { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PostCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
    private GoodsReceiptDto? _selectedReceipt;

    [ObservableProperty] private string _searchText = string.Empty;

    public event Action? NewRequested;

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

    // Проводить и удалять можно только черновик.
    private bool CanPost => SelectedReceipt?.Status == GoodsReceiptStatus.Draft;
    private bool CanDelete => SelectedReceipt?.Status == GoodsReceiptStatus.Draft;

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    /// <summary>Загрузка без RunAsync: вызывается из мест, где RunAsync уже активен.</summary>
    private async Task ReloadCoreAsync(Guid? keepSelectedId)
    {
        _all = await _receipts.GetReceiptsAsync();
        ApplyFilter(keepSelectedId);
    }

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

/// <summary>Источник строк поступления.</summary>
public enum ReceiptSourceMode
{
    ByOrder = 0,
    Free = 1
}

/// <summary>Пара «значение режима + подпись» для привязки к ComboBox.</summary>
public sealed record ReceiptSourceModeOption(ReceiptSourceMode Value, string Label);

public partial class ReceiptEditViewModel : ViewModelBase
{
    private readonly IGoodsReceiptService _receipts;
    private readonly IPurchaseOrderService _orders;
    private readonly ICatalogService _catalog;

    // Полный набор заказов (для фильтра по поставщику) и словарь «товар -> полка по умолчанию».
    private IReadOnlyList<PurchaseOrderDto> _allOrders = Array.Empty<PurchaseOrderDto>();
    private Dictionary<Guid, Guid?> _defaultShelfByProduct = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoadOrderLinesCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddLineCommand))]
    [NotifyCanExecuteChangedFor(nameof(RemoveLineCommand))]
    private ReceiptSourceMode _sourceMode = ReceiptSourceMode.ByOrder;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoadOrderLinesCommand))]
    private Guid _supplierId;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoadOrderLinesCommand))]
    private Guid _selectedOrderId;

    [ObservableProperty] private ReceiptLineViewModel? _selectedLine;
    [ObservableProperty] private IReadOnlyList<SupplierDto> _suppliers = Array.Empty<SupplierDto>();
    [ObservableProperty] private IReadOnlyList<ShelfDto> _shelves = Array.Empty<ShelfDto>();
    [ObservableProperty] private IReadOnlyList<SupplierProductDto> _availableProducts = Array.Empty<SupplierProductDto>();
    [ObservableProperty] private IReadOnlyList<PurchaseOrderDto> _ordersForSupplier = Array.Empty<PurchaseOrderDto>();

    public ObservableCollection<ReceiptLineViewModel> Lines { get; } = new();

    // Полный список поставщиков; в режиме «по заказу» он сужается до тех,
    private IReadOnlyList<SupplierDto> _allSuppliers = Array.Empty<SupplierDto>();

    /// <summary>Варианты режима поступления для ComboBox.</summary>
    public IReadOnlyList<ReceiptSourceModeOption> SourceModes { get; } = new[]
    {
        new ReceiptSourceModeOption(ReceiptSourceMode.ByOrder, "По заказу поставщика"),
        new ReceiptSourceModeOption(ReceiptSourceMode.Free, "Свободное поступление")
    };

    public event Action? CloseRequested;

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

    /// <summary>
    /// В режиме «по заказу» доступны только поставщики с активным оплаченным заказом,
    /// в свободном режиме — все активные поставщики.
    /// </summary>
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

    /// <summary>
    /// Заказ годится для оформления поступления: не отменён и не закрыт,
    /// есть недополученные позиции и он полностью оплачен.
    /// </summary>
    private static bool IsReceivableOrder(PurchaseOrderDto order)
        => order.Status is not (PurchaseOrderStatus.Cancelled or PurchaseOrderStatus.Closed)
           && order.PaymentStatus == PaymentStatus.Paid
           && order.Lines.Any(l => !l.IsClosed && l.RemainingQuantity > 0);

    private Guid _appliedSupplierId = Guid.Empty;
    private bool _restoringSupplierId;

    partial void OnSupplierIdChanged(Guid value)
    {
        if (_restoringSupplierId) return;
        _ = HandleSupplierChangedAsync(value);
    }

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

    partial void OnSelectedOrderIdChanged(Guid value)
    {
        // Не перегружаем строки молча, чтобы не потерять ручной ввод: очистка + явная кнопка.
        Lines.Clear();
        SelectedLine = null;
    }

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

    private bool CanLoadOrderLines =>
        SourceMode == ReceiptSourceMode.ByOrder && SelectedOrderId != Guid.Empty;

    [RelayCommand(CanExecute = nameof(CanAddLine))]
    private void AddLine()
    {
        Lines.Add(new ReceiptLineViewModel { IsProductEditable = true });
        SelectedLine = Lines.Last();
    }

    private bool CanAddLine => SourceMode == ReceiptSourceMode.Free && SupplierId != Guid.Empty;

    [RelayCommand(CanExecute = nameof(HasLineSelection))]
    private void RemoveLine()
    {
        if (SelectedLine is null) return;
        Lines.Remove(SelectedLine);
        SelectedLine = Lines.LastOrDefault();
    }

    private bool HasLineSelection => SelectedLine is not null;

    [RelayCommand]
    private Task SaveDraftAsync() => RunAsync(async () =>
    {
        if (!TryBuildInput(out var input)) return;
        await _receipts.CreateDraftAsync(input);
        CloseRequested?.Invoke();
    });

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

/// <summary>
/// Строка поступления. Наблюдаемые поля (товар, полка, количество) меняются
/// пользователем; служебные (ссылка на строку заказа, редактируемость товара)
/// фиксируются при создании строки и потому не требуют уведомлений.
/// </summary>
public partial class ReceiptLineViewModel : ObservableObject
{
    [ObservableProperty] private Guid _productId;
    [ObservableProperty] private Guid _shelfId;
    [ObservableProperty] private int _quantity = 1;

    /// <summary>Верхняя граница количества (остаток по строке заказа). null — без лимита.</summary>
    [ObservableProperty] private int? _maxQuantity;

    /// <summary>Ссылка на строку заказа (для режима «по заказу»). Не меняется после создания.</summary>
    public Guid? OrderLineId { get; set; }

    /// <summary>Можно ли менять товар (true — свободный ввод, false — товар из заказа).</summary>
    public bool IsProductEditable { get; set; }
}
