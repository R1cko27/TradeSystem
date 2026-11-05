// Application/Dto/PreviewDtos.cs

using System;
using System.Collections.Generic;

namespace TradeSystem.Application.Dto;

/// <summary>
/// READ-ONLY план автоформирования: что будет создано, но без записи в хранилище.
/// Позволяет мастеру показать пользователю разбивку и варианты поставщиков
/// до подтверждения, не дублируя бизнес-правила выбора в UI.
/// </summary>
public sealed class AutoOrderPreviewDto
{
    public IReadOnlyList<PreviewProductDto> Products { get; init; } = Array.Empty<PreviewProductDto>();
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
}

public sealed class PreviewProductDto
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string UnitLabel { get; init; } = string.Empty;
    public int CurrentStock { get; init; }
    public int MinimumStockQuantity { get; init; }

    /// <summary>Потребность до корректировки под конкретного поставщика.</summary>
    public int BaseNeededQuantity { get; init; }

    public IReadOnlyList<PreviewSupplierOptionDto> Options { get; init; } = Array.Empty<PreviewSupplierOptionDto>();

    /// <summary>Рекомендованный поставщик (с учётом явного выбора, если был передан).</summary>
    public Guid? RecommendedSupplierId { get; init; }
}

public sealed class PreviewSupplierOptionDto // DTO варианта поставщика в предварительном плане
{
    public Guid SupplierId { get; init; }
    public string SupplierName { get; init; } = string.Empty;
    public Guid SupplierProductId { get; init; }
    public decimal? PurchasePrice { get; init; }
    public int? LeadTimeDays { get; init; }
    public int? MinimumOrderQuantity { get; init; }
    public int? OrderMultiple { get; init; }
    public bool IsPreferred { get; init; }

    /// <summary>Количество с учётом минималки и кратности именно этого поставщика.</summary>
    public int AdjustedQuantity { get; init; }

    public decimal LineAmount { get; init; }
}

/// <summary>
/// Результат автоформирования заказов: сами заказы + предупреждения
/// (например, товар без активного поставщика был пропущен).
/// </summary>
public sealed class AutoOrderResult
{
    public List<PurchaseOrderDto> Orders { get; } = new();
    public List<string> Warnings { get; } = new();
}
