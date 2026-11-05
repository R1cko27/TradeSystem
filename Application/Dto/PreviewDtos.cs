// Application/Dto/PreviewDtos.cs

using System;
using System.Collections.Generic;

namespace TradeSystem.Application.Dto;

// План автоформирования: что будет создано, без записи в хранилище.
public sealed class AutoOrderPreviewDto
{
    public IReadOnlyList<PreviewProductDto> Products { get; init; } = Array.Empty<PreviewProductDto>(); // Товары плана
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>(); // Предупреждения по пропущенным товарам
}

// Товар в плане автоформирования.
public sealed class PreviewProductDto
{
    public Guid ProductId { get; init; } // Товар
    public string ProductName { get; init; } = string.Empty; // Наименование товара
    public string UnitLabel { get; init; } = string.Empty; // Единица измерения текстом
    public int CurrentStock { get; init; } // Текущий остаток
    public int MinimumStockQuantity { get; init; } // Минимальный остаток

    public int BaseNeededQuantity { get; init; } // Потребность до корректировки под поставщика

    public IReadOnlyList<PreviewSupplierOptionDto> Options { get; init; } = Array.Empty<PreviewSupplierOptionDto>(); // Варианты поставщиков

    public Guid? RecommendedSupplierId { get; init; } // Рекомендованный поставщик
}

// Вариант поставщика в плане.
public sealed class PreviewSupplierOptionDto
{
    public Guid SupplierId { get; init; } // Поставщик
    public string SupplierName { get; init; } = string.Empty; // Наименование поставщика
    public Guid SupplierProductId { get; init; } // Связь товара с поставщиком
    public decimal? PurchasePrice { get; init; } // Закупочная цена
    public int? LeadTimeDays { get; init; } // Срок поставки, дней
    public int? MinimumOrderQuantity { get; init; } // Минимальная партия
    public int? OrderMultiple { get; init; } // Кратность заказа
    public bool IsPreferred { get; init; } // Предпочтительный поставщик

    public int AdjustedQuantity { get; init; } // Количество с учётом минималки и кратности

    public decimal LineAmount { get; init; } // Сумма по строке
}

// Результат автоформирования: созданные заказы и предупреждения.
public sealed class AutoOrderResult
{
    public List<PurchaseOrderDto> Orders { get; } = new(); // Созданные заказы
    public List<string> Warnings { get; } = new(); // Предупреждения
}
