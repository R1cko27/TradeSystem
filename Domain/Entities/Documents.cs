// Domain/Entities/Documents.cs

using System;
using System.Collections.Generic;
using TradeSystem.Domain.Enums;

namespace TradeSystem.Domain.Entities;

public sealed class PurchaseOrder : Entity
{
    public string Number { get; set; } = string.Empty;

    public Guid SupplierId { get; set; }

    public DateTimeOffset OrderDateUtc { get; set; } = DateTimeOffset.UtcNow;

    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;

    public decimal TotalAmount { get; set; }

    public string? Comment { get; set; }

    public List<PurchaseOrderLine> Lines { get; set; } = new();
}

public sealed class PurchaseOrderLine : Entity
{
    public Guid ProductId { get; set; }

    public Guid? SupplierProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public MeasurementUnit Unit { get; set; } = MeasurementUnit.NotSpecified;

    public int OrderedQuantity { get; set; }

    public int ReceivedQuantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal TotalPrice { get; set; }

    public bool IsClosed { get; set; }

    public string? Comment { get; set; }
}

public sealed class Invoice : Entity
{
    public Guid OrderId { get; set; }

    public string Number { get; set; } = string.Empty;

    public DateTimeOffset IssueDateUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? DueDateUtc { get; set; }

    public decimal Amount { get; set; }

    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;

    public DateTimeOffset? PaidAtUtc { get; set; }

    public string? Comment { get; set; }
}

public sealed class GoodsReceipt : Entity
{
    public string Number { get; set; } = string.Empty;

    public Guid SupplierId { get; set; }

    public Guid? OrderId { get; set; }

    public DateTimeOffset ReceivedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public GoodsReceiptStatus Status { get; set; } = GoodsReceiptStatus.Draft;

    public string? Comment { get; set; }

    public List<GoodsReceiptLine> Lines { get; set; } = new();
}

public sealed class GoodsReceiptLine : Entity
{
    public Guid? OrderLineId { get; set; }

    public Guid ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public Guid ShelfId { get; set; }

    public int Quantity { get; set; }

    public decimal? UnitCost { get; set; }

    public string? Comment { get; set; }
}

public sealed class Sale : Entity
{
    public string Number { get; set; } = string.Empty;

    public DateTimeOffset SoldAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public SaleStatus Status { get; set; } = SaleStatus.Draft;

    public string? Comment { get; set; }

    public List<SaleLine> Lines { get; set; } = new();
}

public sealed class SaleLine : Entity
{
    public Guid ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public Guid ShelfId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal TotalPrice { get; set; }

    public string? Comment { get; set; }
}
