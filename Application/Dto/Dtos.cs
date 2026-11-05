// Application/Dto/Dtos.cs

using System;
using System.Collections.Generic;
using TradeSystem.Domain.Enums;

namespace TradeSystem.Application.Dto;

// Поставщик для списков и форм.
public sealed record SupplierDto
{
    public Guid Id { get; init; } // Идентификатор
    public string Name { get; init; } = string.Empty; // Наименование
    public string? TaxId { get; init; } // Налоговый номер (ИНН)

    public string? Country { get; init; } // Страна
    public string? Region { get; init; } // Регион/область
    public string? City { get; init; } // Город
    public string? Street { get; init; } // Улица
    public string? Building { get; init; } // Дом
    public string? Apartment { get; init; } // Квартира/офис
    public string? PostalCode { get; init; } // Почтовый индекс

    public string FullAddress { get; init; } = string.Empty; // Склеенный адрес для списков и отчётов

    public string Phone { get; init; } = string.Empty; // Телефон
    public string? Email { get; init; } // Электронная почта
    public bool IsActive { get; init; } // Признак активности
    public string? Note { get; init; } // Примечание
    public int ProductsCount { get; init; } // Количество связанных товаров
}
// Товар с вычисленными остатками и счётчиками.
public sealed record ProductDto
{
    public Guid Id { get; init; } // Идентификатор
    public string Name { get; init; } = string.Empty; // Наименование
    public string Article { get; init; } = string.Empty; // Артикул
    public MeasurementUnit Unit { get; init; } // Единица измерения
    public string UnitLabel { get; init; } = string.Empty; // Единица измерения текстом
    public int MinimumStockQuantity { get; init; } // Минимальный остаток
    public int? TargetStockQuantity { get; init; } // Целевой остаток
    public bool IsActive { get; init; } // Признак активности
    public string? Description { get; init; } // Описание
    public Guid? DefaultShelfId { get; init; } // Полка по умолчанию
    public string? DefaultShelfName { get; init; } // Название полки по умолчанию

    public int CurrentStock { get; init; } // Суммарный остаток по всем полкам

    public int SuppliersCount { get; init; } // Количество активных поставщиков

    public bool NeedsReplenish { get; init; } // Требуется пополнение

    public string StorageShelves { get; init; } = string.Empty; // Полки, на которых лежит товар
}

// Полка с числом товаров на ней.
public sealed record ShelfDto
{
    public Guid Id { get; init; } // Идентификатор
    public string Code { get; init; } = string.Empty; // Код полки
    public string Name { get; init; } = string.Empty; // Название полки
    public string? Location { get; init; } // Расположение
    public bool IsActive { get; init; } // Признак активности
    public string? Note { get; init; } // Примечание
    public int ProductsCount { get; init; } // Количество товаров на полке
}

// Связь «товар — поставщик» для списков.
public sealed record SupplierProductDto
{
    public Guid Id { get; init; } // Идентификатор связи
    public Guid ProductId { get; init; } // Товар
    public string ProductName { get; init; } = string.Empty; // Наименование товара
    public Guid SupplierId { get; init; } // Поставщик
    public string SupplierName { get; init; } = string.Empty; // Наименование поставщика
    public string? SupplierArticle { get; init; } // Артикул у поставщика
    public decimal? PurchasePrice { get; init; } // Закупочная цена
    public int? LeadTimeDays { get; init; } // Срок поставки, дней
    public int? MinimumOrderQuantity { get; init; } // Минимальная партия
    public int? OrderMultiple { get; init; } // Кратность заказа
    public bool IsPreferred { get; init; } // Предпочтительный поставщик
    public bool IsActive { get; init; } // Признак активности
    public string? Note { get; init; } // Примечание
}

// Остаток товара на полке.
public sealed record StockBalanceDto
{
    public Guid ProductId { get; init; } // Товар
    public string ProductName { get; init; } = string.Empty; // Наименование товара
    public Guid ShelfId { get; init; } // Полка
    public string ShelfCode { get; init; } = string.Empty; // Код полки
    public string ShelfName { get; init; } = string.Empty; // Название полки
    public int Quantity { get; init; } // Количество
}

// Товар с дефицитом и рекомендацией по заказу.
public sealed record LowStockProductDto
{
    public Guid ProductId { get; init; } // Товар
    public string ProductName { get; init; } = string.Empty; // Наименование товара
    public string UnitLabel { get; init; } = string.Empty; // Единица измерения текстом
    public int CurrentStock { get; init; } // Текущий остаток
    public int MinimumStockQuantity { get; init; } // Минимальный остаток
    public int RecommendedOrderQuantity { get; init; } // Рекомендуемое количество заказа
}

// Позиция заказа поставщику.
public sealed record PurchaseOrderLineDto
{
    public Guid Id { get; init; } // Идентификатор
    public Guid ProductId { get; init; } // Товар
    public string ProductName { get; init; } = string.Empty; // Наименование товара
    public string UnitLabel { get; init; } = string.Empty; // Единица измерения текстом
    public int OrderedQuantity { get; init; } // Заказано
    public int ReceivedQuantity { get; init; } // Получено
    public int RemainingQuantity { get; init; } // Осталось получить
    public decimal UnitPrice { get; init; } // Цена за единицу
    public decimal TotalPrice { get; init; } // Стоимость позиции
    public bool IsClosed { get; init; } // Позиция закрыта
}

// Заказ поставщику с позициями и статусом оплаты.
public sealed record PurchaseOrderDto
{
    public Guid Id { get; init; } // Идентификатор
    public string Number { get; init; } = string.Empty; // Номер заказа
    public Guid SupplierId { get; init; } // Поставщик
    public string SupplierName { get; init; } = string.Empty; // Наименование поставщика
    public DateTimeOffset OrderDateUtc { get; init; } // Дата заказа (UTC)
    public PurchaseOrderStatus Status { get; init; } // Статус заказа
    public string StatusLabel { get; init; } = string.Empty; // Статус заказа текстом
    public decimal TotalAmount { get; init; } // Сумма заказа
    public PaymentStatus PaymentStatus { get; init; } // Статус оплаты
    public string PaymentStatusLabel { get; init; } = string.Empty; // Статус оплаты текстом
    public string? Comment { get; init; } // Комментарий
    public IReadOnlyList<PurchaseOrderLineDto> Lines { get; init; } = Array.Empty<PurchaseOrderLineDto>(); // Позиции заказа
}

// Счёт на оплату заказа.
public sealed record InvoiceDto
{
    public Guid Id { get; init; } // Идентификатор
    public Guid OrderId { get; init; } // Заказ
    public string? OrderNumber { get; init; } // Номер заказа
    public string Number { get; init; } = string.Empty; // Номер счёта
    public DateTimeOffset IssueDateUtc { get; init; } // Дата выставления (UTC)
    public DateTimeOffset? DueDateUtc { get; init; } // Срок оплаты (UTC)
    public decimal Amount { get; init; } // Сумма
    public PaymentStatus PaymentStatus { get; init; } // Статус оплаты
    public string PaymentStatusLabel { get; init; } = string.Empty; // Статус оплаты текстом
    public DateTimeOffset? PaidAtUtc { get; init; } // Дата оплаты (UTC)
    public string? Comment { get; init; } // Комментарий
}

// Позиция поступления товара.
public sealed record GoodsReceiptLineDto
{
    public Guid Id { get; init; } // Идентификатор
    public Guid? OrderLineId { get; init; } // Строка заказа (необязательно)
    public Guid ProductId { get; init; } // Товар
    public string ProductName { get; init; } = string.Empty; // Наименование товара
    public Guid ShelfId { get; init; } // Полка
    public string ShelfName { get; init; } = string.Empty; // Название полки
    public int Quantity { get; init; } // Количество
    public decimal? UnitCost { get; init; } // Цена за единицу
}

// Поступление товара с позициями.
public sealed record GoodsReceiptDto
{
    public Guid Id { get; init; } // Идентификатор
    public string Number { get; init; } = string.Empty; // Номер поступления
    public Guid SupplierId { get; init; } // Поставщик
    public string SupplierName { get; init; } = string.Empty; // Наименование поставщика
    public Guid? OrderId { get; init; } // Заказ (необязательно)
    public string? OrderNumber { get; init; } // Номер заказа
    public DateTimeOffset ReceivedAtUtc { get; init; } // Дата поступления (UTC)
    public GoodsReceiptStatus Status { get; init; } // Статус поступления
    public string StatusLabel { get; init; } = string.Empty; // Статус текстом
    public string? Comment { get; init; } // Комментарий
    public IReadOnlyList<GoodsReceiptLineDto> Lines { get; init; } = Array.Empty<GoodsReceiptLineDto>(); // Позиции поступления
}

// Позиция продажи.
public sealed record SaleLineDto
{
    public Guid Id { get; init; } // Идентификатор
    public Guid ProductId { get; init; } // Товар
    public string ProductName { get; init; } = string.Empty; // Наименование товара
    public Guid ShelfId { get; init; } // Полка
    public string ShelfName { get; init; } = string.Empty; // Название полки
    public int Quantity { get; init; } // Количество
    public decimal UnitPrice { get; init; } // Цена продажи
    public decimal TotalPrice { get; init; } // Стоимость позиции
}

// Продажа с позициями и суммой.
public sealed record SaleDto
{
    public Guid Id { get; init; } // Идентификатор
    public string Number { get; init; } = string.Empty; // Номер продажи
    public DateTimeOffset SoldAtUtc { get; init; } // Дата продажи (UTC)
    public SaleStatus Status { get; init; } // Статус продажи
    public string StatusLabel { get; init; } = string.Empty; // Статус текстом
    public decimal TotalAmount { get; init; } // Сумма продажи
    public string? Comment { get; init; } // Комментарий
    public IReadOnlyList<SaleLineDto> Lines { get; init; } = Array.Empty<SaleLineDto>(); // Позиции продажи
}
