// Domain/Enums/Enums.cs

namespace TradeSystem.Domain.Enums;

public enum MeasurementUnit
{
    NotSpecified = 0,
    Piece = 1,
    Kilogram = 2,
    Liter = 3,
    Meter = 4,
    Pack = 5,
    Box = 6,
    Other = 99
}

public enum DocumentType
{
    None = 0,

    PurchaseOrder = 1, // Заказ на покупку
    GoodsReceipt = 2,
    Sale = 3,
    InventoryCorrection = 4
}

public enum PurchaseOrderStatus
{
    Draft = 0,
    Confirmed = 1,
    Sent = 2,
    PartiallyReceived = 3,
    Received = 4,
    Closed = 5,
    Cancelled = 6
}

public enum PaymentStatus
{
    Unpaid = 0,
    PartiallyPaid = 1,
    Paid = 2,
    Cancelled = 3
}

public enum GoodsReceiptStatus
{
    Draft = 0,
    Posted = 1,
    Cancelled = 2
}

public enum SaleStatus
{
    Draft = 0,
    Completed = 1,
    Cancelled = 2
}

public enum StockMovementType
{
    PurchaseReceipt = 1,

    Sale = 2,
    WriteOff = 3,
    TransferIn = 4,
    TransferOut = 5,
    Correction = 6
}
