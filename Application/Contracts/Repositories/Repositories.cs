// Application/Contracts/Repositories/Repositories.cs

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TradeSystem.Domain.Entities;

namespace TradeSystem.Application.Contracts.Repositories;

public interface IRepository<TEntity> where TEntity : Entity
{
    Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface ISupplierRepository : IRepository<Supplier>
{
}

public interface IProductRepository : IRepository<Product>
{
}

public interface IShelfRepository : IRepository<Shelf>
{
}

public interface ISupplierProductRepository : IRepository<SupplierProduct>
{
}

public interface IStockBalanceRepository : IRepository<StockBalance>
{
}

public interface IPurchaseOrderRepository : IRepository<PurchaseOrder>
{
}

public interface IInvoiceRepository : IRepository<Invoice>
{
}

public interface IGoodsReceiptRepository : IRepository<GoodsReceipt>
{
}

public interface ISaleRepository : IRepository<Sale>
{
}

public interface IStockMovementRepository : IRepository<StockMovement>
{
}
