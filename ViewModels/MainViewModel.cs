// ViewModels/MainViewModel.cs

using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TradeSystem.App.ViewModels.Abstractions;
using TradeSystem.App.ViewModels.Invoices;
using TradeSystem.App.ViewModels.Orders;
using TradeSystem.App.ViewModels.Products;
using TradeSystem.App.ViewModels.Receipts;
using TradeSystem.App.ViewModels.Reports;
using TradeSystem.App.ViewModels.Sales;
using TradeSystem.App.ViewModels.Shelves;
using TradeSystem.App.ViewModels.SupplierProducts;
using TradeSystem.App.ViewModels.Suppliers;

namespace TradeSystem.App.ViewModels;

// Главная ViewModel: навигация между разделами и общие команды.
public partial class MainViewModel : ViewModelBase
{
    private readonly ProductsListViewModel _productsList; // Список товаров
    private readonly ProductEditViewModel _productEdit; // Редактор товара
    private readonly SuppliersListViewModel _suppliersList; // Список поставщиков
    private readonly SupplierEditViewModel _supplierEdit; // Редактор поставщика
    private readonly ShelvesListViewModel _shelvesList; // Список полок
    private readonly ShelfEditViewModel _shelfEdit; // Редактор полки
    private readonly SupplierProductsListViewModel _supplierProductsList; // Список связей товара и поставщика
    private readonly SupplierProductEditViewModel _supplierProductEdit; // Редактор связи
    private readonly OrdersListViewModel _ordersList; // Список заказов
    private readonly OrderAutoPreviewViewModel _autoPreview; // Предпросмотр автоформирования заказов
    private readonly OrderManualEditViewModel _manualEdit; // Ручное создание заказа
    private readonly InvoiceListViewModel _invoiceList; // Счета заказа
    private readonly ReceiptsListViewModel _receiptsList; // Список поступлений
    private readonly ReceiptEditViewModel _receiptEdit; // Редактор поступления
    private readonly SalesListViewModel _salesList; // Список продаж
    private readonly SaleEditViewModel _saleEdit; // Редактор продажи
    private readonly ReportsViewModel _reports; // Отчёты

    [ObservableProperty]
    private object? _currentViewModel; // Текущий отображаемый экран

    // Подписывается на события навигации и связывает экраны.
    public MainViewModel(
        ProductsListViewModel productsList, ProductEditViewModel productEdit,
        SuppliersListViewModel suppliersList, SupplierEditViewModel supplierEdit,
        ShelvesListViewModel shelvesList, ShelfEditViewModel shelfEdit,
        SupplierProductsListViewModel supplierProductsList, SupplierProductEditViewModel supplierProductEdit,
        OrdersListViewModel ordersList, OrderAutoPreviewViewModel autoPreview,
        OrderManualEditViewModel manualEdit, InvoiceListViewModel invoiceList,
        ReceiptsListViewModel receiptsList, ReceiptEditViewModel receiptEdit,
        SalesListViewModel salesList, SaleEditViewModel saleEdit,
        ReportsViewModel reports,
        IDialogService dialog)
        : base(dialog)
    {
        _productsList = productsList; _productEdit = productEdit;
        _suppliersList = suppliersList; _supplierEdit = supplierEdit;
        _shelvesList = shelvesList; _shelfEdit = shelfEdit;
        _supplierProductsList = supplierProductsList; _supplierProductEdit = supplierProductEdit;
        _ordersList = ordersList; _autoPreview = autoPreview; _manualEdit = manualEdit; _invoiceList = invoiceList;
        _receiptsList = receiptsList; _receiptEdit = receiptEdit;
        _salesList = salesList; _saleEdit = saleEdit;
        _reports = reports;

        _productsList.AddRequested += () => _ = _productEdit.LoadAsync(null).ContinueWith(_ => CurrentViewModel = (object)_productEdit);
        _productsList.EditRequested += id => _ = _productEdit.LoadAsync(id).ContinueWith(_ => CurrentViewModel = (object)_productEdit);
        _productEdit.CloseRequested += () => _ = NavigateToAsync(_productsList);

        _suppliersList.AddRequested += () => _ = _supplierEdit.LoadAsync(null).ContinueWith(_ => CurrentViewModel = (object)_supplierEdit);
        _suppliersList.EditRequested += id => _ = _supplierEdit.LoadAsync(id).ContinueWith(_ => CurrentViewModel = (object)_supplierEdit);
        _supplierEdit.CloseRequested += () => _ = NavigateToAsync(_suppliersList);

        _shelvesList.AddRequested += () => _ = _shelfEdit.LoadAsync(null).ContinueWith(_ => CurrentViewModel = (object)_shelfEdit);
        _shelvesList.EditRequested += id => _ = _shelfEdit.LoadAsync(id).ContinueWith(_ => CurrentViewModel = (object)_shelfEdit);
        _shelfEdit.CloseRequested += () => _ = NavigateToAsync(_shelvesList);

        _supplierProductsList.AddRequested += () => _ = _supplierProductEdit.LoadAsync(null).ContinueWith(_ => CurrentViewModel = (object)_supplierProductEdit);
        _supplierProductsList.EditRequested += id => _ = _supplierProductEdit.LoadAsync(id).ContinueWith(_ => CurrentViewModel = (object)_supplierProductEdit);
        _supplierProductEdit.CloseRequested += () => _ = NavigateToAsync(_supplierProductsList);

        _ordersList.AutoPreviewRequested += () => _ = OpenAutoPreviewAsync();
        _ordersList.ManualCreateRequested += () => _ = OpenManualAsync();
        _ordersList.InvoicesRequested += id => _ = OpenInvoicesAsync(id);
        _autoPreview.CloseRequested += () => _ = ReturnToOrdersAsync();
        _manualEdit.CloseRequested += () => _ = ReturnToOrdersAsync();
        _invoiceList.CloseRequested += () => _ = NavigateToAsync(_ordersList);

        _receiptsList.NewRequested += () => _ = OpenReceiptEditorAsync();
        _receiptEdit.CloseRequested += () => _ = ReturnToReceiptsAsync();

        _salesList.NewRequested += () => _ = OpenSaleEditorAsync();
        _saleEdit.CloseRequested += () => _ = ReturnToSalesAsync();

        _currentViewModel = _productsList;
    }

    // Запускает начальный экран.
    public Task StartAsync() => ActivateAsync(CurrentViewModel);

    // Команды меню для перехода между разделами.
    [RelayCommand] private Task ProductsAsync() => NavigateToAsync(_productsList);
    [RelayCommand] private Task SuppliersAsync() => NavigateToAsync(_suppliersList);
    [RelayCommand] private Task ShelvesAsync() => NavigateToAsync(_shelvesList);
    [RelayCommand] private Task SupplierProductsAsync() => NavigateToAsync(_supplierProductsList);
    [RelayCommand] private Task OrdersAsync() => NavigateToAsync(_ordersList);
    [RelayCommand] private Task ReceiptsAsync() => NavigateToAsync(_receiptsList);
    [RelayCommand] private Task SalesAsync() => NavigateToAsync(_salesList);
    [RelayCommand] private Task ReportsAsync() => NavigateToAsync(_reports);

    // Заглушка для разделов в разработке.
    [RelayCommand]
    private void Feature(string? name) =>
        Dialog.ShowInfo($"Раздел «{name}» в разработке", "В разработке");

    // Переключает экран и активирует его.
    private async Task NavigateToAsync(object page) { CurrentViewModel = page; await ActivateAsync(page); }

    // Возврат к списку после закрытия редактора.
    private async Task ReturnToOrdersAsync() { CurrentViewModel = _ordersList; await _ordersList.OnActivatedAsync(); }
    private async Task ReturnToReceiptsAsync() { CurrentViewModel = _receiptsList; await _receiptsList.OnActivatedAsync(); }
    private async Task ReturnToSalesAsync() { CurrentViewModel = _salesList; await _salesList.OnActivatedAsync(); }

    // Открывает редактор и загружает в него данные.
    private async Task OpenAutoPreviewAsync() { await _autoPreview.LoadAsync(); CurrentViewModel = _autoPreview; }
    private async Task OpenManualAsync() { await _manualEdit.LoadAsync(); CurrentViewModel = _manualEdit; }
    private async Task OpenInvoicesAsync(Guid orderId) { await _invoiceList.LoadAsync(orderId); CurrentViewModel = _invoiceList; }
    private async Task OpenReceiptEditorAsync() { await _receiptEdit.LoadAsync(); CurrentViewModel = _receiptEdit; }
    private async Task OpenSaleEditorAsync() { await _saleEdit.LoadAsync(); CurrentViewModel = _saleEdit; }

    // Вызывает активацию, если экран её поддерживает.
    private static async Task ActivateAsync(object? vm)
    {
        if (vm is IActivatable a) await a.OnActivatedAsync();
    }
}
