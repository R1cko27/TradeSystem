// ViewModels/Orders/OrdersViewModels.cs

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

namespace TradeSystem.App.ViewModels.Orders;

public partial class OrdersListViewModel : ViewModelBase, IActivatable
{
    private readonly IPurchaseOrderService _orders;
    private IReadOnlyList<PurchaseOrderDto> _all = Array.Empty<PurchaseOrderDto>();

    public ObservableCollection<PurchaseOrderDto> Items { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    [NotifyCanExecuteChangedFor(nameof(CloseCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(InvoicesCommand))]
    private PurchaseOrderDto? _selectedOrder;

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private bool _showCancelled;

    public event Action? AutoPreviewRequested;
    public event Action? ManualCreateRequested;
    public event Action<Guid>? InvoicesRequested;

    public OrdersListViewModel(IPurchaseOrderService orders, IDialogService dialog)
        : base(dialog)
    {
        _orders = orders;
    }

    public Task OnActivatedAsync() => RunAsync(() => ReloadCoreAsync(SelectedOrder?.Id)); // Загрузить данные при активации ViewModel

    [RelayCommand]
    private Task RefreshAsync() => RunAsync(() => ReloadCoreAsync(SelectedOrder?.Id));

    [RelayCommand]
    private void AutoGenerate() => AutoPreviewRequested?.Invoke();

    [RelayCommand]
    private void NewManual() => ManualCreateRequested?.Invoke();

    [RelayCommand(CanExecute = nameof(CanConfirm))]
    private Task ConfirmAsync() => GuardedStatusChangeAsync(
        o => _orders.ConfirmOrderAsync(o.Id), "Подтверждение заказа");

    [RelayCommand(CanExecute = nameof(CanCancel))]
    private Task CancelAsync() => GuardedStatusChangeAsync(
        o => _orders.CancelOrderAsync(o.Id), "Отмена заказа");

    [RelayCommand(CanExecute = nameof(CanClose))]
    private Task CloseAsync() => GuardedStatusChangeAsync(
        o => _orders.CloseOrderAsync(o.Id), "Закрытие заказа");

    [RelayCommand(CanExecute = nameof(CanDelete))]
    private Task DeleteAsync()
    {
        var target = SelectedOrder;
        if (target is null) return Task.CompletedTask;
        if (!Dialog.Confirm($"Удалить черновик заказа {target.Number}?", "Удаление заказа"))
            return Task.CompletedTask;

        return RunAsync(async () =>
        {
            await _orders.DeleteOrderAsync(target.Id);
            await ReloadCoreAsync(null);
        });
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void Invoices()
    {
        if (SelectedOrder is not null)
            InvoicesRequested?.Invoke(SelectedOrder.Id);
    }

    private bool HasSelection => SelectedOrder is not null;
    private bool CanConfirm => SelectedOrder?.Status == PurchaseOrderStatus.Draft;
    private bool CanCancel => SelectedOrder?.Status == PurchaseOrderStatus.Draft; // Отмена допускается только для черновика
    private bool CanClose => SelectedOrder?.Status == PurchaseOrderStatus.Received;
    private bool CanDelete => SelectedOrder?.Status == PurchaseOrderStatus.Draft;

    partial void OnSearchTextChanged(string value) => ApplyFilter();
    partial void OnShowCancelledChanged(bool value) => ApplyFilter();

    private async Task GuardedStatusChangeAsync(Func<PurchaseOrderDto, Task> action, string title)
    {
        var target = SelectedOrder;
        if (target is null) return;
        if (!Dialog.Confirm($"{title}: {target.Number}?", title))
            return;

        await RunAsync(async () =>
        {
            await action(target);
            await ReloadCoreAsync(target.Id);
        });
    }

    /// <summary>Загрузка без RunAsync: вызывается из мест, где RunAsync уже активен.</summary>
    private async Task ReloadCoreAsync(Guid? keepSelectedId) // Загрузить заказы и применить фильтр
    {
        _all = await _orders.GetOrdersAsync();
        ApplyFilter(keepSelectedId);
    }

    private void ApplyFilter(Guid? keepSelectedId = null) // Применить фильтр поиска и отменённых
    {
        IEnumerable<PurchaseOrderDto> query = _all;

        if (!ShowCancelled)
            query = query.Where(o => o.Status != PurchaseOrderStatus.Cancelled);

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var s = SearchText.Trim();
            query = query.Where(o =>
                Contains(o.Number, s) || Contains(o.SupplierName, s));
        }

        var previousId = keepSelectedId ?? SelectedOrder?.Id;

        Items.Clear();
        foreach (var item in query)
            Items.Add(item);

        if (previousId is { } pid)
            SelectedOrder = Items.FirstOrDefault(o => o.Id == pid);
    }

    private static bool Contains(string? source, string value) =>
        !string.IsNullOrEmpty(source) && source.Contains(value, StringComparison.CurrentCultureIgnoreCase);
}

public partial class OrderAutoPreviewViewModel : ViewModelBase
{
    private readonly IPurchaseOrderService _orders;
    private readonly ICatalogService _catalog;

    private readonly HashSet<Guid> _additionalIds = new();

    [ObservableProperty] private IReadOnlyList<ProductDto> _allActiveProducts = Array.Empty<ProductDto>();
    [ObservableProperty] private ProductDto? _productToAdd;

    public ObservableCollection<PreviewRowViewModel> Rows { get; } = new();
    public ObservableCollection<string> Warnings { get; } = new();

    public event Action? CloseRequested;

    public OrderAutoPreviewViewModel(IPurchaseOrderService orders, ICatalogService catalog, IDialogService dialog)
        : base(dialog)
    {
        _orders = orders;
        _catalog = catalog;
    }

    public Task LoadAsync() => RunAsync(ReloadCoreAsync);

    /// <summary>
    /// Перестроение плана. Не оборачивается в RunAsync: команды уже выполняются
    /// внутри RunAsync, а вложенный вызов молча не сработал бы из-за флага IsBusy.
    /// </summary>
    private async Task ReloadCoreAsync()
    {
        AllActiveProducts = await _catalog.GetProductsAsync(includeInactive: false);

        var request = BuildRequest();
        var preview = await _orders.PreviewAutoOrdersAsync(request);

        // Определяем, какие строки пришли из дефицита, а какие — дополнительные.
        Rows.Clear();
        foreach (var p in preview.Products)
            Rows.Add(new PreviewRowViewModel(p, isAdditional: _additionalIds.Contains(p.ProductId)));

        Warnings.Clear();
        foreach (var w in preview.Warnings)
            Warnings.Add(w);

        // Список «доп.» мог измениться — пересчитываем доступность команды удаления.
        RemoveAdditionalCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private Task AddAdditionalAsync() => RunAsync(async () =>
    {
        if (ProductToAdd is null)
        {
            Dialog.ShowWarning("Выберите товар для добавления.");
            return;
        }
        if (_additionalIds.Contains(ProductToAdd.Id) || Rows.Any(r => r.ProductId == ProductToAdd.Id))
        {
            Dialog.ShowWarning("Этот товар уже в плане.");
            return;
        }
        _additionalIds.Add(ProductToAdd.Id);
        ProductToAdd = null;
        await ReloadCoreAsync();
    });

    [RelayCommand(CanExecute = nameof(CanRemoveAdditional))]
    private Task RemoveAdditionalAsync() => RunAsync(async () =>
    {
        // Удаляем последний добавленный доп. товар (простое и предсказуемое поведение).
        var target = Rows.LastOrDefault(r => r.IsAdditional);
        if (target is null) return;
        _additionalIds.Remove(target.ProductId);
        await ReloadCoreAsync();
    });

    private bool CanRemoveAdditional => Rows.Any(r => r.IsAdditional);

    [RelayCommand]
    private Task ConfirmAsync() => RunAsync(async () =>
    {
        if (Rows.Count == 0)
        {
            Dialog.ShowInfo("Нечего создавать: нет товаров к пополнению.");
            return;
        }

        var request = BuildRequest();
        var result = await _orders.AutoGenerateOrdersAsync(request);

        var msg = result.Orders.Count == 0
            ? "Заказы не созданы."
            : $"Создано заказов: {result.Orders.Count}.";
        if (result.Warnings.Count > 0)
            msg += Environment.NewLine + "Предупреждения:" + Environment.NewLine + string.Join(Environment.NewLine, result.Warnings);

        Dialog.ShowInfo(msg, "Итог автоформирования");
        CloseRequested?.Invoke();
    });

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke();

    private AutoOrderRequest BuildRequest()
    {
        var explicitChoice = new Dictionary<Guid, Guid>();
        foreach (var row in Rows)
        {
            if (row.SelectedOption is { } opt && opt.SupplierId != Guid.Empty)
                explicitChoice[row.ProductId] = opt.SupplierId;
        }

        return new AutoOrderRequest
        {
            ExplicitSupplierChoice = explicitChoice.Count > 0 ? explicitChoice : null,
            AdditionalProductIds = _additionalIds.Count > 0 ? new HashSet<Guid>(_additionalIds) : null
        };
    }
}

public partial class OrderManualEditViewModel : ViewModelBase
{
    private readonly ICatalogService _catalog;
    private readonly IPurchaseOrderService _orders;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddLineCommand))]
    private Guid _supplierId;

    [ObservableProperty] private string _comment = string.Empty;
    [ObservableProperty] private IReadOnlyList<SupplierDto> _suppliers = Array.Empty<SupplierDto>();
    [ObservableProperty] private IReadOnlyList<SupplierProductDto> _availableProducts = Array.Empty<SupplierProductDto>();
    [ObservableProperty] private ManualOrderLineViewModel? _selectedLine;

    public ObservableCollection<ManualOrderLineViewModel> Lines { get; } = new();

    public decimal TotalAmount => Lines.Sum(l => l.Total);

    public event Action? CloseRequested;

    public OrderManualEditViewModel(ICatalogService catalog, IPurchaseOrderService orders, IDialogService dialog)
        : base(dialog)
    {
        _catalog = catalog;
        _orders = orders;

        // Живой итог: пересчитываем при изменении состава и содержимого позиций.
        Lines.CollectionChanged += OnLinesCollectionChanged;
    }

    private void OnLinesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
            foreach (ManualOrderLineViewModel line in e.NewItems)
                line.PropertyChanged += OnLinePropertyChanged;

        if (e.OldItems is not null)
            foreach (ManualOrderLineViewModel line in e.OldItems)
                line.PropertyChanged -= OnLinePropertyChanged;

        OnPropertyChanged(nameof(TotalAmount));
    }

    private void OnLinePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ManualOrderLineViewModel.Quantity)
                          or nameof(ManualOrderLineViewModel.SelectedSupplierProduct))
        {
            OnPropertyChanged(nameof(TotalAmount));
        }
    }

    public async Task LoadAsync()
    {
        await RunAsync(async () =>
        {
            SupplierId = Guid.Empty;
            Comment = string.Empty;
            Lines.Clear();
            SelectedLine = null;
            Suppliers = await _catalog.GetSuppliersAsync(includeInactive: false);
            AvailableProducts = Array.Empty<SupplierProductDto>();
            OnPropertyChanged(nameof(TotalAmount));
        });
    }

    partial void OnSupplierIdChanged(Guid value) => _ = HandleSupplierChangedAsync(value);

    private async Task HandleSupplierChangedAsync(Guid supplierId)
    {
        if (Lines.Count > 0 &&
            !Dialog.Confirm("Смена поставщика очистит добавленные позиции. Продолжить?", "Смена поставщика"))
        {
            return; // откат не делаем осознанно: пользователь сам разберётся, позиция останется битой до сохранения
        }

        await RunAsync(async () =>
        {
            Lines.Clear();
            SelectedLine = null;
            AvailableProducts = supplierId == Guid.Empty
                ? Array.Empty<SupplierProductDto>()
                : await _catalog.GetSupplierProductsBySupplierAsync(supplierId);
            OnPropertyChanged(nameof(TotalAmount));
        });
    }

    [RelayCommand(CanExecute = nameof(CanAddLine))]
    private void AddLine()
    {
        Lines.Add(new ManualOrderLineViewModel());
        OnPropertyChanged(nameof(TotalAmount));
    }

    private bool CanAddLine => SupplierId != Guid.Empty && AvailableProducts.Count > 0;

    [RelayCommand(CanExecute = nameof(HasLineSelection))]
    private void RemoveLine()
    {
        if (SelectedLine is null) return;
        Lines.Remove(SelectedLine);
        SelectedLine = Lines.LastOrDefault();
        OnPropertyChanged(nameof(TotalAmount));
    }

    private bool HasLineSelection => SelectedLine is not null;

    [RelayCommand]
    private Task SaveAsync() => RunAsync(SaveImplementationAsync);

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke();

    private async Task SaveImplementationAsync()
    {
        var errors = Validate();
        if (errors.Count > 0)
        {
            Dialog.ShowError(string.Join(Environment.NewLine, errors));
            return;
        }

        var input = new CreatePurchaseOrderInput
        {
            SupplierId = SupplierId,
            Comment = string.IsNullOrWhiteSpace(Comment) ? null : Comment.Trim(),
            Lines = Lines.Select(l => new CreatePurchaseOrderLineInput
            {
                ProductId = l.ProductId,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                SupplierProductId = l.SupplierProductId
            }).ToList()
        };

        await _orders.CreateOrderAsync(input);
        CloseRequested?.Invoke();
    }

    private List<string> Validate()
    {
        var errors = new List<string>();
        if (SupplierId == Guid.Empty)
            errors.Add("Выберите поставщика.");
        if (Lines.Count == 0)
            errors.Add("Добавьте хотя бы одну позицию.");
        for (int i = 0; i < Lines.Count; i++)
        {
            var l = Lines[i];
            if (l.SelectedSupplierProduct is null)
                errors.Add($"Позиция {i + 1}: не выбран товар.");
            else if (l.Quantity <= 0)
                errors.Add($"Позиция {i + 1}: количество должно быть больше нуля.");
        }
        if (Lines.GroupBy(l => l.ProductId).Any(g => g.Key != Guid.Empty && g.Count() > 1))
            errors.Add("Один и тот же товар не должен повторяться в позициях.");
        return errors;
    }
}

/// <summary>
/// Обёртка над read-only DTO предпросмотра: добавляет наблюдаемый выбор
/// поставщика (для товаров с несколькими вариантами) и признак «дополнительный».
/// </summary>
public partial class PreviewRowViewModel : ObservableObject
{
    private readonly PreviewProductDto _source;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedSupplierId))]
    private PreviewSupplierOptionDto? _selectedOption; // Выбранный вариант поставщика

    public PreviewRowViewModel(PreviewProductDto source, bool isAdditional)
    {
        _source = source;
        IsAdditional = isAdditional;
        Options = source.Options;
        SelectedOption = source.Options.FirstOrDefault(o => o.SupplierId == source.RecommendedSupplierId)
                         ?? source.Options.FirstOrDefault();
    }

    public bool IsAdditional { get; }
    public IReadOnlyList<PreviewSupplierOptionDto> Options { get; }

    public Guid ProductId => _source.ProductId;
    public string ProductName => _source.ProductName;
    public string UnitLabel => _source.UnitLabel;
    public int CurrentStock => _source.CurrentStock;
    public int MinimumStockQuantity => _source.MinimumStockQuantity;
    public bool HasMultipleOptions => Options.Count > 1;
    public Guid SelectedSupplierId => SelectedOption?.SupplierId ?? Guid.Empty;
}

/// <summary>
/// Одна позиция будущего заказа. Держит выбранный вариант «товар-поставщик»
/// целиком, поэтому ProductId и цена всегда согласованы между собой.
/// </summary>
public partial class ManualOrderLineViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Total))]
    [NotifyPropertyChangedFor(nameof(UnitPrice))]
    private SupplierProductDto? _selectedSupplierProduct; // Выбранный вариант «товар-поставщик»

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Total))]
    private int _quantity = 1;

    public Guid ProductId => SelectedSupplierProduct?.ProductId ?? Guid.Empty;
    public Guid SupplierProductId => SelectedSupplierProduct?.Id ?? Guid.Empty;

    public decimal UnitPrice => SelectedSupplierProduct?.PurchasePrice ?? 0m;

    public decimal Total => UnitPrice * Math.Max(0, Quantity);
}
