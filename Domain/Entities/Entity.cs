// Domain/Entities/Entity.cs

using System;

namespace TradeSystem.Domain.Entities;

public abstract class Entity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ModifiedAtUtc { get; set; } // Дата и время последнего изменения сущности в формате UTC
}
