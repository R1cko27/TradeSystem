// Domain/Entities/Stock.cs

using System;
using TradeSystem.Domain.Enums;

namespace TradeSystem.Domain.Entities;

// Текущий остаток товара на конкретной полке.
public sealed class StockBalance : Entity
{
    public Guid ProductId { get; set; } // Товар

    public Guid ShelfId { get; set; } // Полка

    public int Quantity { get; set; } // Количество на полке

    public string? Note { get; set; } // Примечание
}

// История движений товара: приход, расход, корректировки.
public sealed class StockMovement : Entity
{
    public Guid ProductId { get; set; } // Товар

    public Guid ShelfId { get; set; } // Полка

    public int QuantityDelta { get; set; } // Изменение количества: плюс приход, минус расход

    public StockMovementType Type { get; set; } // Тип движения

    public DocumentType SourceType { get; set; } = DocumentType.None; // Тип документа-источника

    public Guid? SourceId { get; set; } // Идентификатор документа-источника

    public Guid? SourceLineId { get; set; } // Идентификатор строки документа

    public DateTimeOffset OccurredAtUtc { get; set; } = DateTimeOffset.UtcNow; // Дата и время движения (UTC)

    public string? Comment { get; set; } // Комментарий
}
