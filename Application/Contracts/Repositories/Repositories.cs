// Application/Contracts/Repositories/Repositories.cs

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TradeSystem.Domain.Entities;

namespace TradeSystem.Application.Contracts.Repositories;

// Базовый контракт репозитория: чтение, добавление, изменение и удаление.
public interface IRepository<TEntity> where TEntity : Entity
{
    Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

// Репозиторий поставщиков.
public interface ISupplierRepository : IRepository<Supplier>
{
}

// Репозиторий товаров.
public interface IProductRepository : IRepository<Product>
{
}

// Репозиторий полок.
public interface IShelfRepository : IRepository<Shelf>
{
}

// Репозиторий связей товара и поставщика.
public interface ISupplierProductRepository : IRepository<SupplierProduct>
{
}

// Репозиторий остатков на полках.
public interface IStockBalanceRepository : IRepository<StockBalance>
{
}

// Репозиторий заказов поставщикам.
public interface IPurchaseOrderRepository : IRepository<PurchaseOrder>
{
}

// Репозиторий счетов на оплату.
public interface IInvoiceRepository : IRepository<Invoice>
{
}

// Репозиторий поступлений товара.
public interface IGoodsReceiptRepository : IRepository<GoodsReceipt>
{
}

// Репозиторий продаж.
public interface ISaleRepository : IRepository<Sale>
{
}

// Репозиторий движений товара.
public interface IStockMovementRepository : IRepository<StockMovement>
{
}
