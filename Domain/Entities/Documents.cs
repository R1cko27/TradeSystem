// Domain/Entities/Documents.cs

using System;
using System.Collections.Generic;
using TradeSystem.Domain.Enums;

namespace TradeSystem.Domain.Entities;

// Заказ поставщику.
public sealed class PurchaseOrder : Entity
{
    public string Number { get; set; } = string.Empty; // Номер заказа

    public Guid SupplierId { get; set; } // Поставщик

    public DateTimeOffset OrderDateUtc { get; set; } = DateTimeOffset.UtcNow; // Дата заказа (UTC)

    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft; // Статус заказа

    public decimal TotalAmount { get; set; } // Сумма заказа

    public string? Comment { get; set; } // Комментарий

    public List<PurchaseOrderLine> Lines { get; set; } = new(); // Позиции заказа
}

// Позиция заказа поставщику.
public sealed class PurchaseOrderLine : Entity
{
    public Guid ProductId { get; set; } // Товар

    public Guid? SupplierProductId { get; set; } // Связь товара с поставщиком

    public string ProductName { get; set; } = string.Empty; // Наименование товара на момент заказа

    public MeasurementUnit Unit { get; set; } = MeasurementUnit.NotSpecified; // Единица измерения

    public int OrderedQuantity { get; set; } // Заказанное количество

    public int ReceivedQuantity { get; set; } // Полученное количество

    public decimal UnitPrice { get; set; } // Цена за единицу

    public decimal TotalPrice { get; set; } // Стоимость позиции

    public bool IsClosed { get; set; } // Позиция закрыта (получено всё заказанное)

    public string? Comment { get; set; } // Комментарий
}

// Счёт на оплату заказа.
public sealed class Invoice : Entity
{
    public Guid OrderId { get; set; } // Заказ, по которому выставлен счёт

    public string Number { get; set; } = string.Empty; // Номер счёта

    public DateTimeOffset IssueDateUtc { get; set; } = DateTimeOffset.UtcNow; // Дата выставления (UTC)

    public DateTimeOffset? DueDateUtc { get; set; } // Срок оплаты (UTC)

    public decimal Amount { get; set; } // Сумма счёта

    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid; // Статус оплаты

    public DateTimeOffset? PaidAtUtc { get; set; } // Дата оплаты (UTC)

    public string? Comment { get; set; } // Комментарий
}

// Поступление товара на склад.
public sealed class GoodsReceipt : Entity
{
    public string Number { get; set; } = string.Empty; // Номер поступления

    public Guid SupplierId { get; set; } // Поставщик

    public Guid? OrderId { get; set; } // Заказ, по которому пришёл товар (необязательно)

    public DateTimeOffset ReceivedAtUtc { get; set; } = DateTimeOffset.UtcNow; // Дата поступления (UTC)

    public GoodsReceiptStatus Status { get; set; } = GoodsReceiptStatus.Draft; // Статус поступления

    public string? Comment { get; set; } // Комментарий

    public List<GoodsReceiptLine> Lines { get; set; } = new(); // Позиции поступления
}

// Позиция поступления.
public sealed class GoodsReceiptLine : Entity
{
    public Guid? OrderLineId { get; set; } // Ссылка на строку заказа (для режима «по заказу»)

    public Guid ProductId { get; set; } // Товар

    public string ProductName { get; set; } = string.Empty; // Наименование товара

    public Guid ShelfId { get; set; } // Полка размещения

    public int Quantity { get; set; } // Количество

    public decimal? UnitCost { get; set; } // Цена за единицу

    public string? Comment { get; set; } // Комментарий
}

// Продажа товара.
public sealed class Sale : Entity
{
    public string Number { get; set; } = string.Empty; // Номер продажи

    public DateTimeOffset SoldAtUtc { get; set; } = DateTimeOffset.UtcNow; // Дата продажи (UTC)

    public SaleStatus Status { get; set; } = SaleStatus.Draft; // Статус продажи

    public string? Comment { get; set; } // Комментарий

    public List<SaleLine> Lines { get; set; } = new(); // Позиции продажи
}

// Позиция продажи.
public sealed class SaleLine : Entity
{
    public Guid ProductId { get; set; } // Товар

    public string ProductName { get; set; } = string.Empty; // Наименование товара

    public Guid ShelfId { get; set; } // Полка, с которой списывается товар

    public int Quantity { get; set; } // Количество

    public decimal UnitPrice { get; set; } // Цена за единицу

    public decimal TotalPrice { get; set; } // Стоимость позиции

    public string? Comment { get; set; } // Комментарий
}
