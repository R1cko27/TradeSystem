// App.xaml.cs

using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using TradeSystem.App.ViewModels;
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
using TradeSystem.App.Views;
using TradeSystem.Application;
using TradeSystem.Infrastructure.Persistence.Json;
using AppBase = System.Windows.Application;

namespace TradeSystem.App
{
    public partial class App : AppBase
    {
        public static IServiceProvider Services { get; private set; } = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var services = new ServiceCollection();

            services.AddTradeSystemJsonPersistence();
            services.AddTradeSystemApplication();
            services.AddSingleton<IDialogService, DialogService>();
            services.AddSingleton<IDocumentOutputService, DocumentOutputService>();

            services.AddSingleton<ProductsListViewModel>();
            services.AddSingleton<ProductEditViewModel>();
            services.AddSingleton<SuppliersListViewModel>();
            services.AddSingleton<SupplierEditViewModel>();
            services.AddSingleton<ShelvesListViewModel>();
            services.AddSingleton<ShelfEditViewModel>();
            services.AddSingleton<SupplierProductsListViewModel>();
            services.AddSingleton<SupplierProductEditViewModel>();
            services.AddSingleton<OrdersListViewModel>();
            services.AddSingleton<OrderAutoPreviewViewModel>();
            services.AddSingleton<OrderManualEditViewModel>();
            services.AddSingleton<InvoiceListViewModel>();
            services.AddSingleton<ReceiptsListViewModel>();
            services.AddSingleton<ReceiptEditViewModel>();
            services.AddSingleton<SalesListViewModel>();
            services.AddSingleton<SaleEditViewModel>();
            services.AddSingleton<ReportsViewModel>();
            services.AddSingleton<MainViewModel>();

            Services = services.BuildServiceProvider();

            var mainVm = Services.GetRequiredService<MainViewModel>();
            var window = new MainWindow { DataContext = mainVm };
            window.Show();
        }
    }
}
