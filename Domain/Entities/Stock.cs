// Domain/Entities/Stock.cs

using System;
using TradeSystem.Domain.Enums;

namespace TradeSystem.Domain.Entities;

public sealed class StockBalance : Entity
{
    public Guid ProductId { get; set; }

    public Guid ShelfId { get; set; }

    public int Quantity { get; set; }

    public string? Note { get; set; }
}

public sealed class StockMovement : Entity
{
    public Guid ProductId { get; set; }

    public Guid ShelfId { get; set; }

    public int QuantityDelta { get; set; }

    public StockMovementType Type { get; set; }

    public DocumentType SourceType { get; set; } = DocumentType.None;

    public Guid? SourceId { get; set; }

    public Guid? SourceLineId { get; set; }

    public DateTimeOffset OccurredAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public string? Comment { get; set; }
}
