// Application/Dto/Inputs.cs

using System;
using System.Collections.Generic;
using TradeSystem.Domain.Enums;
using TradeSystem.Domain.ValueObjects;

namespace TradeSystem.Application.Dto;

// Данные формы поставщика.
public sealed class SupplierInput
{
    public Guid? Id { get; set; } // Идентификатор (null — новая запись)
    public string Name { get; set; } = string.Empty; // Наименование
    public string? TaxId { get; set; } // Налоговый номер
    public Address Address { get; set; } = new(); // Адрес
    public string Phone { get; set; } = string.Empty; // Телефон
    public string? Email { get; set; } // Электронная почта
    public bool IsActive { get; set; } = true; // Признак активности
    public string? Note { get; set; } // Примечание
}

// Данные формы товара.
public sealed class ProductInput
{
    public Guid? Id { get; set; } // Идентификатор (null — новая запись)
    public string Name { get; set; } = string.Empty; // Наименование
    public string Article { get; set; } = string.Empty; // Артикул
    public MeasurementUnit Unit { get; set; } = MeasurementUnit.Piece; // Единица измерения
    public int MinimumStockQuantity { get; set; } // Минимальный остаток
    public int? TargetStockQuantity { get; set; } // Целевой остаток
    public bool IsActive { get; set; } = true; // Признак активности
    public string? Description { get; set; } // Описание
    public Guid? DefaultShelfId { get; set; } // Полка по умолчанию
}

// Данные формы полки.
public sealed class ShelfInput
{
    public Guid? Id { get; set; } // Идентификатор (null — новая запись)
    public string Code { get; set; } = string.Empty; // Код полки
    public string Name { get; set; } = string.Empty; // Название
    public string? Location { get; set; } // Расположение
    public bool IsActive { get; set; } = true; // Признак активности
    public string? Note { get; set; } // Примечание
}

// Данные формы связи «товар — поставщик».
public sealed class SupplierProductInput
{
    public Guid? Id { get; set; } // Идентификатор (null — новая запись)
    public Guid ProductId { get; set; } // Товар
    public Guid SupplierId { get; set; } // Поставщик
    public string? SupplierArticle { get; set; } // Артикул у поставщика
    public decimal? PurchasePrice { get; set; } // Закупочная цена
    public int? LeadTimeDays { get; set; } // Срок поставки, дней
    public int? MinimumOrderQuantity { get; set; } // Минимальная партия
    public int? OrderMultiple { get; set; } // Кратность заказа
    public bool IsPreferred { get; set; } // Предпочтительный поставщик
    public bool IsActive { get; set; } = true; // Признак активности
    public string? Note { get; set; } // Примечание
}

// Строка создаваемого заказа.
public sealed class CreatePurchaseOrderLineInput
{
    public Guid ProductId { get; set; } // Товар
    public int Quantity { get; set; } // Количество
    public decimal? UnitPrice { get; set; } // Цена за единицу (null — из связи)
    public Guid? SupplierProductId { get; set; } // Связь товара с поставщиком
}

// Данные для создания заказа поставщику.
public sealed class CreatePurchaseOrderInput
{
    public Guid SupplierId { get; set; } // Поставщик
    public string? Comment { get; set; } // Комментарий
    public List<CreatePurchaseOrderLineInput> Lines { get; set; } = new(); // Позиции заказа
}

// Параметры автоформирования заказов по дефициту.
public sealed class AutoOrderRequest
{
    public Dictionary<Guid, Guid>? ExplicitSupplierChoice { get; set; } // Явный выбор поставщика по товару
    public HashSet<Guid>? AdditionalProductIds { get; set; } // Дополнительные товары сверх дефицита
}

// Строка создаваемого поступления.
public sealed class GoodsReceiptLineInput
{
    public Guid? OrderLineId { get; set; } // Строка заказа (необязательно)
    public Guid ProductId { get; set; } // Товар
    public Guid ShelfId { get; set; } // Полка
    public int Quantity { get; set; } // Количество
    public decimal? UnitCost { get; set; } // Цена за единицу
}

// Данные для создания поступления.
public sealed class GoodsReceiptInput
{
    public Guid SupplierId { get; set; } // Поставщик
    public Guid? OrderId { get; set; } // Заказ (необязательно)
    public string? Comment { get; set; } // Комментарий
    public List<GoodsReceiptLineInput> Lines { get; set; } = new(); // Позиции поступления
}

// Строка создаваемой продажи.
public sealed class SaleLineInput
{
    public Guid ProductId { get; set; } // Товар
    public Guid ShelfId { get; set; } // Полка
    public int Quantity { get; set; } // Количество
    public decimal UnitPrice { get; set; } // Цена продажи
}

// Данные для создания продажи.
public sealed class SaleInput
{
    public string? Comment { get; set; } // Комментарий
    public List<SaleLineInput> Lines { get; set; } = new(); // Позиции продажи
}
