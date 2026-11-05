// Domain/Entities/Catalog.cs

using System;
using TradeSystem.Domain.Enums;
using TradeSystem.Domain.ValueObjects;

namespace TradeSystem.Domain.Entities;

public sealed class Product : Entity
{
    public string Name { get; set; } = string.Empty;

    public string Article { get; set; } = string.Empty;

    public MeasurementUnit Unit { get; set; } = MeasurementUnit.NotSpecified;

    public int MinimumStockQuantity { get; set; }

    public int? TargetStockQuantity { get; set; }

    public bool IsActive { get; set; } = true;

    public string? Description { get; set; }

    public Guid? DefaultShelfId { get; set; }
}

public sealed class Supplier : Entity
{
    public string Name { get; set; } = string.Empty;

    public string? TaxId { get; set; }

    public Address Address { get; set; } = new();

    public string Phone { get; set; } = string.Empty;

    public string? Email { get; set; }

    public bool IsActive { get; set; } = true;

    public string? Note { get; set; }
}

public sealed class Shelf : Entity
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Location { get; set; }

    public bool IsActive { get; set; } = true;

    public string? Note { get; set; }
}

public sealed class SupplierProduct : Entity
{
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
