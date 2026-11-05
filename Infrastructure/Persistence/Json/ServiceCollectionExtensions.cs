// Infrastructure/Persistence/Json/ServiceCollectionExtensions.cs

using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using TradeSystem.Application.Contracts.Repositories;
using TradeSystem.Domain.Entities;

namespace TradeSystem.Infrastructure.Persistence.Json;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTradeSystemJsonPersistence(
        this IServiceCollection services,
        JsonStorageOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        options ??= new JsonStorageOptions();

        services.AddSingleton(options);

        var directoryPath = string.IsNullOrWhiteSpace(options.DirectoryPath)
            ? "data"
            : options.DirectoryPath;

        var rootPath = Path.IsPathRooted(directoryPath)
            ? directoryPath
            : Path.Combine(AppContext.BaseDirectory, directoryPath);

        rootPath = Path.GetFullPath(rootPath);

        AddStore<Supplier>(services, rootPath, JsonFileNames.Suppliers, options.WriteIndented);
        AddStore<Product>(services, rootPath, JsonFileNames.Products, options.WriteIndented);
        AddStore<Shelf>(services, rootPath, JsonFileNames.Shelves, options.WriteIndented);
        AddStore<SupplierProduct>(services, rootPath, JsonFileNames.SupplierProducts, options.WriteIndented);
        AddStore<StockBalance>(services, rootPath, JsonFileNames.StockBalances, options.WriteIndented);
        AddStore<PurchaseOrder>(services, rootPath, JsonFileNames.PurchaseOrders, options.WriteIndented);
        AddStore<Invoice>(services, rootPath, JsonFileNames.Invoices, options.WriteIndented);
        AddStore<GoodsReceipt>(services, rootPath, JsonFileNames.GoodsReceipts, options.WriteIndented);
        AddStore<Sale>(services, rootPath, JsonFileNames.Sales, options.WriteIndented);
        AddStore<StockMovement>(services, rootPath, JsonFileNames.StockMovements, options.WriteIndented);

        services.AddSingleton<ISupplierRepository, SupplierRepository>();
        services.AddSingleton<IProductRepository, ProductRepository>();
        services.AddSingleton<IShelfRepository, ShelfRepository>();
        services.AddSingleton<ISupplierProductRepository, SupplierProductRepository>();
        services.AddSingleton<IStockBalanceRepository, StockBalanceRepository>();
        services.AddSingleton<IPurchaseOrderRepository, PurchaseOrderRepository>();
        services.AddSingleton<IInvoiceRepository, InvoiceRepository>();
        services.AddSingleton<IGoodsReceiptRepository, GoodsReceiptRepository>();
        services.AddSingleton<ISaleRepository, SaleRepository>();
        services.AddSingleton<IStockMovementRepository, StockMovementRepository>();

        return services;
    }

    private static void AddStore<TEntity>(
        IServiceCollection services,
        string rootPath,
        string fileName,
        bool writeIndented)
    {
        var filePath = Path.Combine(rootPath, fileName);

        services.AddSingleton<IJsonFileStore<TEntity>>(
            new JsonFileStore<TEntity>(filePath, writeIndented));
    }
}
