// Domain/Entities/Catalog.cs

using System;
using TradeSystem.Domain.Enums;
using TradeSystem.Domain.ValueObjects;

namespace TradeSystem.Domain.Entities;

// Товар справочника.
public sealed class Product : Entity
{
    public string Name { get; set; } = string.Empty; // Наименование товара

    public string Article { get; set; } = string.Empty; // Артикул

    public MeasurementUnit Unit { get; set; } = MeasurementUnit.NotSpecified; // Единица измерения

    public int MinimumStockQuantity { get; set; } // Минимальный остаток, ниже которого нужно пополнение

    public int? TargetStockQuantity { get; set; } // Желаемый остаток после пополнения (null — не задан)

    public bool IsActive { get; set; } = true; // Признак активности: неактивные скрыты из списков

    public string? Description { get; set; } // Описание товара

    public Guid? DefaultShelfId { get; set; } // Полка по умолчанию для размещения
}

// Поставщик товаров.
public sealed class Supplier : Entity
{
    public string Name { get; set; } = string.Empty; // Наименование поставщика

    public string? TaxId { get; set; } // Налоговый номер (ИНН)

    public Address Address { get; set; } = new(); // Юридический адрес

    public string Phone { get; set; } = string.Empty; // Телефон

    public string? Email { get; set; } // Электронная почта

    public bool IsActive { get; set; } = true; // Признак активности

    public string? Note { get; set; } // Примечание
}

// Место хранения товара (полка).
public sealed class Shelf : Entity
{
    public string Code { get; set; } = string.Empty; // Краткий код полки

    public string Name { get; set; } = string.Empty; // Название полки

    public string? Location { get; set; } // Физическое расположение

    public bool IsActive { get; set; } = true; // Признак активности

    public string? Note { get; set; } // Примечание
}

// Связь «товар — поставщик» с условиями закупки.
public sealed class SupplierProduct : Entity
{
    public Guid ProductId { get; set; } // Товар

    public Guid SupplierId { get; set; } // Поставщик

    public string? SupplierArticle { get; set; } // Артикул товара у поставщика

    public decimal? PurchasePrice { get; set; } // Закупочная цена

    public int? LeadTimeDays { get; set; } // Срок поставки в днях

    public int? MinimumOrderQuantity { get; set; } // Минимальная партия заказа

    public int? OrderMultiple { get; set; } // Кратность заказа

    public bool IsPreferred { get; set; } // Предпочтительный поставщик для товара

    public bool IsActive { get; set; } = true; // Признак активности связи

    public string? Note { get; set; } // Примечание
}
