// Domain/Enums/Enums.cs

namespace TradeSystem.Domain.Enums;

// Единица измерения товара.
public enum MeasurementUnit
{
    NotSpecified = 0, // Единица не указана
    Piece = 1, // Штука
    Kilogram = 2, // Килограмм
    Liter = 3, // Литр
    Meter = 4, // Метр
    Pack = 5, // Упаковка
    Box = 6, // Коробка
    Other = 99 // Другая единица
}

// Тип документа, создающего движение по складу.
public enum DocumentType
{
    None = 0, // Нет документа-источника

    PurchaseOrder = 1, // Заказ на покупку
    GoodsReceipt = 2, // Поступление товара
    Sale = 3, // Продажа
    InventoryCorrection = 4 // Корректировка остатков
}

// Статус заказа поставщику.
public enum PurchaseOrderStatus
{
    Draft = 0, // Черновик
    Confirmed = 1, // Подтверждён
    Sent = 2, // Отправлен поставщику
    PartiallyReceived = 3, // Частично получен
    Received = 4, // Получен
    Closed = 5, // Закрыт
    Cancelled = 6 // Отменён
}

// Статус оплаты счёта.
public enum PaymentStatus
{
    Unpaid = 0, // Не оплачен
    PartiallyPaid = 1, // Частично оплачен
    Paid = 2, // Оплачен
    Cancelled = 3 // Отменён
}

// Статус поступления товара.
public enum GoodsReceiptStatus
{
    Draft = 0, // Черновик
    Posted = 1, // Проведён
    Cancelled = 2 // Отменён
}

// Статус продажи.
public enum SaleStatus
{
    Draft = 0, // Черновик
    Completed = 1, // Завершена
    Cancelled = 2 // Отменена
}

// Тип складского движения.
public enum StockMovementType
{
    PurchaseReceipt = 1, // Приход по поступлению

    Sale = 2, // Расход по продаже
    WriteOff = 3, // Списание
    TransferIn = 4, // Перемещение на полку
    TransferOut = 5, // Перемещение с полки
    Correction = 6 // Корректировка
}
