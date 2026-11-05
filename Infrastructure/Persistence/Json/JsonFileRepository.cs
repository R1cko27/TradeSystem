// Infrastructure/Persistence/Json/JsonFileRepository.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TradeSystem.Application.Contracts.Repositories;
using TradeSystem.Domain.Entities;

namespace TradeSystem.Infrastructure.Persistence.Json;

// Базовый репозиторий поверх JSON-файла.
public class JsonFileRepository<TEntity> : IRepository<TEntity> where TEntity : Entity
{
    // Хранилище передаётся снаружи, чтобы репозиторий не знал о путях.
    protected JsonFileRepository(IJsonFileStore<TEntity> store)
    {
        Store = store ?? throw new ArgumentNullException(nameof(store));
    }

    protected IJsonFileStore<TEntity> Store { get; } // Хранилище JSON-файла

    // Все записи.
    public virtual async Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await Store.ReadAsync(cancellationToken).ConfigureAwait(false);
    }

    // Запись по идентификатору или null.
    public virtual async Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return null;
        }

        var items = await Store.ReadAsync(cancellationToken).ConfigureAwait(false);

        return items.FirstOrDefault(item => item.Id == id);
    }

    // Добавляет запись, проверяя уникальность Id.
    public virtual async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (entity.Id == Guid.Empty)
        {
            entity.Id = Guid.NewGuid();
        }

        await Store.UpdateAsync(items =>
        {
            if (items.Any(item => item.Id == entity.Id))
            {
                throw new InvalidOperationException(
                    $"{typeof(TEntity).Name} with Id {entity.Id} already exists.");
            }

            if (entity.CreatedAtUtc == default)
            {
                entity.CreatedAtUtc = DateTimeOffset.UtcNow;
            }

            items.Add(entity);

            return Task.CompletedTask;
        }, cancellationToken).ConfigureAwait(false);
    }

    // Обновляет запись, сохраняя дату создания.
    public virtual async Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        await Store.UpdateAsync(items =>
        {
            var index = items.FindIndex(item => item.Id == entity.Id);

            if (index < 0)
            {
                throw new KeyNotFoundException(
                    $"{typeof(TEntity).Name} with Id {entity.Id} was not found.");
            }

            entity.CreatedAtUtc = items[index].CreatedAtUtc;
            entity.ModifiedAtUtc = DateTimeOffset.UtcNow;

            items[index] = entity;

            return Task.CompletedTask;
        }, cancellationToken).ConfigureAwait(false);
    }

    // Удаляет запись по идентификатору.
    public virtual async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return false;
        }

        var deleted = false;

        await Store.UpdateAsync(items =>
        {
            deleted = items.RemoveAll(item => item.Id == id) > 0;

            return Task.CompletedTask;
        }, cancellationToken).ConfigureAwait(false);

        return deleted;
    }
}
