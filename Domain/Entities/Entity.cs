// Domain/Entities/Entity.cs

using System;

namespace TradeSystem.Domain.Entities;

// Базовый класс сущности: идентификатор и отметки времени создания/изменения.
public abstract class Entity
{
    public Guid Id { get; set; } = Guid.NewGuid(); // Уникальный идентификатор

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow; // Дата и время создания в UTC

    public DateTimeOffset? ModifiedAtUtc { get; set; } // Дата и время последнего изменения сущности в формате UTC
}
