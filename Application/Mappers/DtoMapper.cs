// Application/Mappers/DtoMapper.cs

using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using TradeSystem.Application.Dto;
using TradeSystem.Domain.Entities;
using TradeSystem.Domain.Enums;
using TradeSystem.Domain.ValueObjects;

namespace TradeSystem.Application.Mappers;

// Преобразует доменные сущности в DTO: общие поля копирует AutoMapper,
// вычисляемые значения добавляются через with.
public sealed class DtoMapper
{
    private readonly IMapper _mapper; // AutoMapper для копирования общих полей

    public DtoMapper(IMapper mapper) => _mapper = mapper;

    // Склеивает части адреса в строку с сокращениями «ул.», «д.», «кв.».
    public static string Format(Address address)
    {
        if (address is null) return string.Empty;

        var parts = new List<string?>
        {
            address.Country, address.Region, address.City,
            string.IsNullOrWhiteSpace(address.Street) ? null : $"ул. {address.Street}",
            string.IsNullOrWhiteSpace(address.Building) ? null : $"д. {address.Building}",
            string.IsNullOrWhiteSpace(address.Apartment) ? null : $"кв. {address.Apartment}",
            string.IsNullOrWhiteSpace(address.PostalCode) ? null : address.PostalCode
        };

        return string.Join(", ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
    }

    // Поставщик: добавляет склеенный адрес и число товаров.
    public SupplierDto ToDto(Supplier s, int productsCount) =>
        _mapper.Map<SupplierDto>(s) with
        {
            FullAddress = Format(s.Address ?? new Address()),
            ProductsCount = productsCount
        };

    // Полка: добавляет число товаров.
    public ShelfDto ToDto(Shelf sh, int productsCount) =>
        _mapper.Map<ShelfDto>(sh) with { ProductsCount = productsCount };

    // Товар: добавляет остаток, число поставщиков и признак пополнения.
    public ProductDto ToDto(
        Product p,
        int currentStock,
        int suppliersCount,
        string? defaultShelfName) =>
        _mapper.Map<ProductDto>(p) with
        {
            DefaultShelfName = defaultShelfName,
            CurrentStock = currentStock,
            SuppliersCount = suppliersCount,
            NeedsReplenish = p.MinimumStockQuantity > 0 && currentStock < p.MinimumStockQuantity
        };

    // Связь: подставляет названия товара и поставщика.
    public SupplierProductDto ToDto(
        SupplierProduct sp,
        string productName,
        string supplierName) =>
        _mapper.Map<SupplierProductDto>(sp) with
        {
            ProductName = productName,
            SupplierName = supplierName
        };

    // Позиция заказа: остаток к получению не может быть отрицательным.
    public PurchaseOrderLineDto ToDto(PurchaseOrderLine l) =>
        _mapper.Map<PurchaseOrderLineDto>(l) with
        {
            RemainingQuantity = Math.Max(0, l.OrderedQuantity - l.ReceivedQuantity)
        };

    // Заказ: поставщик, статус оплаты и позиции.
    public PurchaseOrderDto ToDto(
        PurchaseOrder o,
        string supplierName,
        PaymentStatus paymentStatus) =>
        _mapper.Map<PurchaseOrderDto>(o) with
        {
            SupplierName = supplierName,
            PaymentStatus = paymentStatus,
            PaymentStatusLabel = paymentStatus.Of(),
            Lines = o.Lines.Select(ToDto).ToList()
        };

    // Счёт: добавляет номер заказа.
    public InvoiceDto ToDto(Invoice i, string? orderNumber) =>
        _mapper.Map<InvoiceDto>(i) with { OrderNumber = orderNumber };

    // Позиция поступления: названия товара и полки.
    public GoodsReceiptLineDto ToDto(GoodsReceiptLine l, string productName, string shelfName) =>
        _mapper.Map<GoodsReceiptLineDto>(l) with
        {
            ProductName = productName,
            ShelfName = shelfName
        };

    // Поступление: поставщик, заказ и строки с названиями.
    public GoodsReceiptDto ToDto(
        GoodsReceipt r,
        string supplierName,
        string? orderNumber,
        Func<GoodsReceiptLine, (string product, string shelf)> resolver) =>
        _mapper.Map<GoodsReceiptDto>(r) with
        {
            SupplierName = supplierName,
            OrderNumber = orderNumber,
            Lines = r.Lines.Select(l =>
            {
                var (product, shelf) = resolver(l);
                return ToDto(l, product, shelf);
            }).ToList()
        };

    // Позиция продажи: названия товара и полки.
    public SaleLineDto ToDto(SaleLine l, string productName, string shelfName) =>
        _mapper.Map<SaleLineDto>(l) with
        {
            ProductName = productName,
            ShelfName = shelfName
        };

    // Продажа: сумма по строкам и позиции.
    public SaleDto ToDto(Sale s, Func<SaleLine, (string product, string shelf)> resolver) =>
        _mapper.Map<SaleDto>(s) with
        {
            TotalAmount = s.Lines.Sum(l => l.TotalPrice),
            Lines = s.Lines.Select(l =>
            {
                var (product, shelf) = resolver(l);
                return ToDto(l, product, shelf);
            }).ToList()
        };
}

// Человекочитаемые подписи для перечислений.
public static class Labels
{
    private static readonly Dictionary<MeasurementUnit, string> Units = new() // Подписи единиц измерения
    {
        [MeasurementUnit.NotSpecified] = "не указано",
        [MeasurementUnit.Piece]        = "шт.",
        [MeasurementUnit.Kilogram]     = "кг",
        [MeasurementUnit.Liter]        = "л",
        [MeasurementUnit.Meter]        = "м",
        [MeasurementUnit.Pack]         = "уп.",
        [MeasurementUnit.Box]          = "кор.",
        [MeasurementUnit.Other]        = "др."
    };

    private static readonly Dictionary<PurchaseOrderStatus, string> OrderStatuses = new() // Статусы заказов
    {
        [PurchaseOrderStatus.Draft]             = "Черновик",
        [PurchaseOrderStatus.Confirmed]         = "Подтверждён",
        [PurchaseOrderStatus.Sent]              = "Отправлен",
        [PurchaseOrderStatus.PartiallyReceived] = "Частично получен",
        [PurchaseOrderStatus.Received]          = "Получен",
        [PurchaseOrderStatus.Closed]            = "Закрыт",
        [PurchaseOrderStatus.Cancelled]         = "Отменён"
    };

    private static readonly Dictionary<PaymentStatus, string> PaymentStatuses = new() // Статусы оплаты
    {
        [PaymentStatus.Unpaid]         = "Не оплачен",
        [PaymentStatus.PartiallyPaid]  = "Частично оплачен",
        [PaymentStatus.Paid]           = "Оплачен",
        [PaymentStatus.Cancelled]      = "Отменён"
    };

    private static readonly Dictionary<GoodsReceiptStatus, string> ReceiptStatuses = new() // Статусы поступлений
    {
        [GoodsReceiptStatus.Draft]     = "Черновик",
        [GoodsReceiptStatus.Posted]    = "Проведён",
        [GoodsReceiptStatus.Cancelled] = "Отменён"
    };

    private static readonly Dictionary<SaleStatus, string> SaleStatuses = new() // Статусы продаж
    {
        [SaleStatus.Draft]     = "Черновик",
        [SaleStatus.Completed] = "Завершена",
        [SaleStatus.Cancelled] = "Отменена"
    };

    // Возвращает подпись значения; для неизвестного значения — его имя.
    public static string Of(this MeasurementUnit unit) =>
        Units.TryGetValue(unit, out var v) ? v : unit.ToString();

    public static string Of(this PurchaseOrderStatus status) =>
        OrderStatuses.TryGetValue(status, out var v) ? v : status.ToString();

    public static string Of(this PaymentStatus status) =>
        PaymentStatuses.TryGetValue(status, out var v) ? v : status.ToString();

    public static string Of(this GoodsReceiptStatus status) =>
        ReceiptStatuses.TryGetValue(status, out var v) ? v : status.ToString();

    public static string Of(this SaleStatus status) =>
        SaleStatuses.TryGetValue(status, out var v) ? v : status.ToString();
}
