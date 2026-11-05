// Infrastructure/Persistence/Json/Repositories.cs

using TradeSystem.Application.Contracts.Repositories;
using TradeSystem.Domain.Entities;

namespace TradeSystem.Infrastructure.Persistence.Json;

// Репозиторий поставщиков.
public sealed class SupplierRepository : JsonFileRepository<Supplier>, ISupplierRepository
{
    public SupplierRepository(IJsonFileStore<Supplier> store) : base(store)
    {
    }
}

// Репозиторий товаров.
public sealed class ProductRepository : JsonFileRepository<Product>, IProductRepository
{
    public ProductRepository(IJsonFileStore<Product> store) : base(store)
    {
    }
}

// Репозиторий полок.
public sealed class ShelfRepository : JsonFileRepository<Shelf>, IShelfRepository
{
    public ShelfRepository(IJsonFileStore<Shelf> store) : base(store)
    {
    }
}

// Репозиторий связей товара и поставщика.
public sealed class SupplierProductRepository : JsonFileRepository<SupplierProduct>, ISupplierProductRepository
{
    public SupplierProductRepository(IJsonFileStore<SupplierProduct> store) : base(store)
    {
    }
}

// Репозиторий остатков.
public sealed class StockBalanceRepository : JsonFileRepository<StockBalance>, IStockBalanceRepository
{
    public StockBalanceRepository(IJsonFileStore<StockBalance> store) : base(store)
    {
    }
}

// Репозиторий заказов.
public sealed class PurchaseOrderRepository : JsonFileRepository<PurchaseOrder>, IPurchaseOrderRepository
{
    public PurchaseOrderRepository(IJsonFileStore<PurchaseOrder> store) : base(store)
    {
    }
}

// Репозиторий счетов.
public sealed class InvoiceRepository : JsonFileRepository<Invoice>, IInvoiceRepository
{
    public InvoiceRepository(IJsonFileStore<Invoice> store) : base(store)
    {
    }
}

// Репозиторий поступлений.
public sealed class GoodsReceiptRepository : JsonFileRepository<GoodsReceipt>, IGoodsReceiptRepository
{
    public GoodsReceiptRepository(IJsonFileStore<GoodsReceipt> store) : base(store)
    {
    }
}

// Репозиторий продаж.
public sealed class SaleRepository : JsonFileRepository<Sale>, ISaleRepository
{
    public SaleRepository(IJsonFileStore<Sale> store) : base(store)
    {
    }
}

// Репозиторий движений товара.
public sealed class StockMovementRepository : JsonFileRepository<StockMovement>, IStockMovementRepository
{
    public StockMovementRepository(IJsonFileStore<StockMovement> store) : base(store)
    {
    }
}
