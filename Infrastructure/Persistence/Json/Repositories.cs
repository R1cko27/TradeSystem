// Infrastructure/Persistence/Json/Repositories.cs

using TradeSystem.Application.Contracts.Repositories;
using TradeSystem.Domain.Entities;

namespace TradeSystem.Infrastructure.Persistence.Json;

public sealed class SupplierRepository : JsonFileRepository<Supplier>, ISupplierRepository
{
    public SupplierRepository(IJsonFileStore<Supplier> store) : base(store)
    {
    }
}

public sealed class ProductRepository : JsonFileRepository<Product>, IProductRepository
{
    public ProductRepository(IJsonFileStore<Product> store) : base(store)
    {
    }
}

public sealed class ShelfRepository : JsonFileRepository<Shelf>, IShelfRepository
{
    public ShelfRepository(IJsonFileStore<Shelf> store) : base(store)
    {
    }
}

public sealed class SupplierProductRepository : JsonFileRepository<SupplierProduct>, ISupplierProductRepository
{
    public SupplierProductRepository(IJsonFileStore<SupplierProduct> store) : base(store)
    {
    }
}

public sealed class StockBalanceRepository : JsonFileRepository<StockBalance>, IStockBalanceRepository
{
    public StockBalanceRepository(IJsonFileStore<StockBalance> store) : base(store)
    {
    }
}

public sealed class PurchaseOrderRepository : JsonFileRepository<PurchaseOrder>, IPurchaseOrderRepository
{
    public PurchaseOrderRepository(IJsonFileStore<PurchaseOrder> store) : base(store)
    {
    }
}

public sealed class InvoiceRepository : JsonFileRepository<Invoice>, IInvoiceRepository
{
    public InvoiceRepository(IJsonFileStore<Invoice> store) : base(store)
    {
    }
}

public sealed class GoodsReceiptRepository : JsonFileRepository<GoodsReceipt>, IGoodsReceiptRepository
{
    public GoodsReceiptRepository(IJsonFileStore<GoodsReceipt> store) : base(store)
    {
    }
}

public sealed class SaleRepository : JsonFileRepository<Sale>, ISaleRepository
{
    public SaleRepository(IJsonFileStore<Sale> store) : base(store)
    {
    }
}

public sealed class StockMovementRepository : JsonFileRepository<StockMovement>, IStockMovementRepository
{
    public StockMovementRepository(IJsonFileStore<StockMovement> store) : base(store)
    {
    }
}
