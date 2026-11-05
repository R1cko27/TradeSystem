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

public partial class MainViewModel : ViewModelBase
{
    private readonly ProductsListViewModel _productsList;
    private readonly ProductEditViewModel _productEdit;
    private readonly SuppliersListViewModel _suppliersList;
    private readonly SupplierEditViewModel _supplierEdit;
    private readonly ShelvesListViewModel _shelvesList;
    private readonly ShelfEditViewModel _shelfEdit;
    private readonly SupplierProductsListViewModel _supplierProductsList;
    private readonly SupplierProductEditViewModel _supplierProductEdit;
    private readonly OrdersListViewModel _ordersList;
    private readonly OrderAutoPreviewViewModel _autoPreview;
    private readonly OrderManualEditViewModel _manualEdit;
    private readonly InvoiceListViewModel _invoiceList;
    private readonly ReceiptsListViewModel _receiptsList;
    private readonly ReceiptEditViewModel _receiptEdit;
    private readonly SalesListViewModel _salesList;
    private readonly SaleEditViewModel _saleEdit;
    private readonly ReportsViewModel _reports;

    [ObservableProperty]
    private object? _currentViewModel;

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

    public Task StartAsync() => ActivateAsync(CurrentViewModel);

    [RelayCommand] private Task ProductsAsync() => NavigateToAsync(_productsList);
    [RelayCommand] private Task SuppliersAsync() => NavigateToAsync(_suppliersList);
    [RelayCommand] private Task ShelvesAsync() => NavigateToAsync(_shelvesList);
    [RelayCommand] private Task SupplierProductsAsync() => NavigateToAsync(_supplierProductsList);
    [RelayCommand] private Task OrdersAsync() => NavigateToAsync(_ordersList);
    [RelayCommand] private Task ReceiptsAsync() => NavigateToAsync(_receiptsList);
    [RelayCommand] private Task SalesAsync() => NavigateToAsync(_salesList);
    [RelayCommand] private Task ReportsAsync() => NavigateToAsync(_reports);

    [RelayCommand]
    private void Feature(string? name) =>
        Dialog.ShowInfo($"Раздел «{name}» в разработке", "В разработке");

    private async Task NavigateToAsync(object page) { CurrentViewModel = page; await ActivateAsync(page); }

    private async Task ReturnToOrdersAsync() { CurrentViewModel = _ordersList; await _ordersList.OnActivatedAsync(); }
    private async Task ReturnToReceiptsAsync() { CurrentViewModel = _receiptsList; await _receiptsList.OnActivatedAsync(); }
    private async Task ReturnToSalesAsync() { CurrentViewModel = _salesList; await _salesList.OnActivatedAsync(); }

    private async Task OpenAutoPreviewAsync() { await _autoPreview.LoadAsync(); CurrentViewModel = _autoPreview; }
    private async Task OpenManualAsync() { await _manualEdit.LoadAsync(); CurrentViewModel = _manualEdit; }
    private async Task OpenInvoicesAsync(Guid orderId) { await _invoiceList.LoadAsync(orderId); CurrentViewModel = _invoiceList; }
    private async Task OpenReceiptEditorAsync() { await _receiptEdit.LoadAsync(); CurrentViewModel = _receiptEdit; }
    private async Task OpenSaleEditorAsync() { await _saleEdit.LoadAsync(); CurrentViewModel = _saleEdit; }

    private static async Task ActivateAsync(object? vm)
    {
        if (vm is IActivatable a) await a.OnActivatedAsync();
    }
}
