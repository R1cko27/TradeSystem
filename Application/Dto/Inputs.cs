// Application/Dto/Inputs.cs

using System;
using System.Collections.Generic;
using TradeSystem.Domain.Enums;
using TradeSystem.Domain.ValueObjects;

namespace TradeSystem.Application.Dto;

public sealed class SupplierInput
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? TaxId { get; set; }
    public Address Address { get; set; } = new();
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Note { get; set; }
}

public sealed class ProductInput
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Article { get; set; } = string.Empty;
    public MeasurementUnit Unit { get; set; } = MeasurementUnit.Piece;
    public int MinimumStockQuantity { get; set; }
    public int? TargetStockQuantity { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
    public Guid? DefaultShelfId { get; set; }
}

public sealed class ShelfInput
{
    public Guid? Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Location { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Note { get; set; }
}

public sealed class SupplierProductInput // Входные данные связи поставщика и товара
{
    public Guid? Id { get; set; }
    public Guid ProductId { get; set; }
    public Guid SupplierId { get; set; }
    public string? SupplierArticle { get; set; }
    public decimal? PurchasePrice { get; set; }
    public int? LeadTimeDays { get; set; }
    public int? MinimumOrderQuantity { get; set; }
    public int? OrderMultiple { get; set; }
    public bool IsPreferred { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Note { get; set; }
}

public sealed class CreatePurchaseOrderLineInput
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal? UnitPrice { get; set; }
    public Guid? SupplierProductId { get; set; }
}

public sealed class CreatePurchaseOrderInput
{
    public Guid SupplierId { get; set; }
    public string? Comment { get; set; }
    public List<CreatePurchaseOrderLineInput> Lines { get; set; } = new();
}

/// <summary>
/// Параметры автоформирования заказов по дефициту.
/// ExplicitSupplierChoice позволяет вручную указать поставщика для товара,
/// который поставляют несколько фирм (ключ — ProductId, значение — SupplierId).
/// AdditionalProductIds — дополнительные товары, включаемые в заказ сверх дефицита.
/// </summary>
public sealed class AutoOrderRequest
{
    public Dictionary<Guid, Guid>? ExplicitSupplierChoice { get; set; }
    public HashSet<Guid>? AdditionalProductIds { get; set; }
}

public sealed class GoodsReceiptLineInput
{
    public Guid? OrderLineId { get; set; }
    public Guid ProductId { get; set; }
    public Guid ShelfId { get; set; }
    public int Quantity { get; set; }
    public decimal? UnitCost { get; set; }
}

public sealed class GoodsReceiptInput
{
    public Guid SupplierId { get; set; }
    public Guid? OrderId { get; set; }
    public string? Comment { get; set; }
    public List<GoodsReceiptLineInput> Lines { get; set; } = new();
}

public sealed class SaleLineInput
{
    public Guid ProductId { get; set; }
    public Guid ShelfId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public sealed class SaleInput
{
    public string? Comment { get; set; }
    public List<SaleLineInput> Lines { get; set; } = new();
}
