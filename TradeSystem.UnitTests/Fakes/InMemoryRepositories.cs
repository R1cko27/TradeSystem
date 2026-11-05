// TradeSystem.UnitTests/Fakes/InMemoryRepositories.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TradeSystem.Application.Contracts.Repositories;
using TradeSystem.Domain.Entities;

namespace TradeSystem.UnitTests.Fakes;

// Репозиторий в памяти для изоляции тестов от файловой системы.
public class InMemoryRepository<TEntity> : IRepository<TEntity> where TEntity : Entity
{
    private readonly List<TEntity> _items = new(); // Записи в памяти

    public IReadOnlyList<TEntity> Items => _items; // Все записи

    public Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<TEntity>>(_items.ToList());

    public Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_items.FirstOrDefault(item => item.Id == id));

    public Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        _items.Add(entity);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        var index = _items.FindIndex(item => item.Id == entity.Id);
        if (index >= 0) _items[index] = entity;
        return Task.CompletedTask;
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_items.RemoveAll(item => item.Id == id) > 0);

    // Добавляет запись напрямую, минуя проверки.
    public TEntity Seed(TEntity entity)
    {
        _items.Add(entity);
        return entity;
    }

    // Очищает репозиторий.
    public void Clear() => _items.Clear();
}

public sealed class InMemorySupplierRepository : InMemoryRepository<Supplier>, ISupplierRepository
{
}

public sealed class InMemoryProductRepository : InMemoryRepository<Product>, IProductRepository
{
}

public sealed class InMemoryShelfRepository : InMemoryRepository<Shelf>, IShelfRepository
{
}

public sealed class InMemorySupplierProductRepository : InMemoryRepository<SupplierProduct>, ISupplierProductRepository
{
}

public sealed class InMemoryStockBalanceRepository : InMemoryRepository<StockBalance>, IStockBalanceRepository
{
}

public sealed class InMemoryPurchaseOrderRepository : InMemoryRepository<PurchaseOrder>, IPurchaseOrderRepository
{
}

public sealed class InMemoryInvoiceRepository : InMemoryRepository<Invoice>, IInvoiceRepository
{
}

public sealed class InMemoryGoodsReceiptRepository : InMemoryRepository<GoodsReceipt>, IGoodsReceiptRepository
{
}

public sealed class InMemorySaleRepository : InMemoryRepository<Sale>, ISaleRepository
{
}

public sealed class InMemoryStockMovementRepository : InMemoryRepository<StockMovement>, IStockMovementRepository
{
}
