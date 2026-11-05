// Application/Dto/Dtos.cs

using System;
using System.Collections.Generic;
using TradeSystem.Domain.Enums;

namespace TradeSystem.Application.Dto;

public sealed record SupplierDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? TaxId { get; init; }

    public string? Country { get; init; }
    public string? Region { get; init; }
    public string? City { get; init; }
    public string? Street { get; init; }
    public string? Building { get; init; }
    public string? Apartment { get; init; }
    public string? PostalCode { get; init; }

    // Склеенный адрес — для вывода в списках/отчётах.
    public string FullAddress { get; init; } = string.Empty;

    public string Phone { get; init; } = string.Empty;
    public string? Email { get; init; }
    public bool IsActive { get; init; }
    public string? Note { get; init; }
    public int ProductsCount { get; init; }
}
public sealed record ProductDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Article { get; init; } = string.Empty;
    public MeasurementUnit Unit { get; init; }
    public string UnitLabel { get; init; } = string.Empty;
    public int MinimumStockQuantity { get; init; }
    public int? TargetStockQuantity { get; init; }
    public bool IsActive { get; init; }
    public string? Description { get; init; }
    public Guid? DefaultShelfId { get; init; }
    public string? DefaultShelfName { get; init; }

    /// <summary>Суммарный остаток по всем полкам.</summary>
    public int CurrentStock { get; init; }

    /// <summary>Сколько активных поставщиков по этому товару.</summary>
    public int SuppliersCount { get; init; }

    public bool NeedsReplenish { get; init; }

    /// <summary>Полки, на которых реально лежит товар (заполняется для отчёта «Товары в наличии»).</summary>
    public string StorageShelves { get; init; } = string.Empty;
}

public sealed record ShelfDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Location { get; init; }
    public bool IsActive { get; init; }
    public string? Note { get; init; }
    public int ProductsCount { get; init; }
}

public sealed record SupplierProductDto // DTO связи поставщика и товара
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public Guid SupplierId { get; init; }
    public string SupplierName { get; init; } = string.Empty;
    public string? SupplierArticle { get; init; }
    public decimal? PurchasePrice { get; init; }
    public int? LeadTimeDays { get; init; }
    public int? MinimumOrderQuantity { get; init; }
    public int? OrderMultiple { get; init; }
    public bool IsPreferred { get; init; }
    public bool IsActive { get; init; }
    public string? Note { get; init; }
}

public sealed record StockBalanceDto
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public Guid ShelfId { get; init; }
    public string ShelfCode { get; init; } = string.Empty;
    public string ShelfName { get; init; } = string.Empty;
    public int Quantity { get; init; }
}

public sealed record LowStockProductDto
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string UnitLabel { get; init; } = string.Empty;
    public int CurrentStock { get; init; }
    public int MinimumStockQuantity { get; init; }
    public int RecommendedOrderQuantity { get; init; }
}

public sealed record PurchaseOrderLineDto
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string UnitLabel { get; init; } = string.Empty;
    public int OrderedQuantity { get; init; }
    public int ReceivedQuantity { get; init; }
    public int RemainingQuantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal TotalPrice { get; init; }
    public bool IsClosed { get; init; }
}

public sealed record PurchaseOrderDto
{
    public Guid Id { get; init; }
    public string Number { get; init; } = string.Empty;
    public Guid SupplierId { get; init; }
    public string SupplierName { get; init; } = string.Empty;
    public DateTimeOffset OrderDateUtc { get; init; }
    public PurchaseOrderStatus Status { get; init; }
    public string StatusLabel { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public PaymentStatus PaymentStatus { get; init; }
    public string PaymentStatusLabel { get; init; } = string.Empty;
    public string? Comment { get; init; }
    public IReadOnlyList<PurchaseOrderLineDto> Lines { get; init; } = Array.Empty<PurchaseOrderLineDto>();
}

public sealed record InvoiceDto
{
    public Guid Id { get; init; }
    public Guid OrderId { get; init; }
    public string? OrderNumber { get; init; }
    public string Number { get; init; } = string.Empty;
    public DateTimeOffset IssueDateUtc { get; init; }
    public DateTimeOffset? DueDateUtc { get; init; }
    public decimal Amount { get; init; }
    public PaymentStatus PaymentStatus { get; init; }
    public string PaymentStatusLabel { get; init; } = string.Empty;
    public DateTimeOffset? PaidAtUtc { get; init; }
    public string? Comment { get; init; }
}

public sealed record GoodsReceiptLineDto
{
    public Guid Id { get; init; }
    public Guid? OrderLineId { get; init; }
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public Guid ShelfId { get; init; }
    public string ShelfName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal? UnitCost { get; init; }
}

public sealed record GoodsReceiptDto
{
    public Guid Id { get; init; }
    public string Number { get; init; } = string.Empty;
    public Guid SupplierId { get; init; }
    public string SupplierName { get; init; } = string.Empty;
    public Guid? OrderId { get; init; }
    public string? OrderNumber { get; init; }
    public DateTimeOffset ReceivedAtUtc { get; init; }
    public GoodsReceiptStatus Status { get; init; }
    public string StatusLabel { get; init; } = string.Empty;
    public string? Comment { get; init; }
    public IReadOnlyList<GoodsReceiptLineDto> Lines { get; init; } = Array.Empty<GoodsReceiptLineDto>();
}

public sealed record SaleLineDto
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public Guid ShelfId { get; init; }
    public string ShelfName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal TotalPrice { get; init; }
}

public sealed record SaleDto
{
    public Guid Id { get; init; }
    public string Number { get; init; } = string.Empty;
    public DateTimeOffset SoldAtUtc { get; init; }
    public SaleStatus Status { get; init; }
    public string StatusLabel { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public string? Comment { get; init; }
    public IReadOnlyList<SaleLineDto> Lines { get; init; } = Array.Empty<SaleLineDto>();
}
