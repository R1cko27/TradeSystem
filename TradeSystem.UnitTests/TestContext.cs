// TradeSystem.UnitTests/TestContext.cs

using System;
using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using TradeSystem.Application.Mappers;
using TradeSystem.Application.Services;
using TradeSystem.Domain.Entities;
using TradeSystem.Domain.Enums;
using TradeSystem.UnitTests.Fakes;

namespace TradeSystem.UnitTests;

/// <summary>Собирает сервисы с репозиториями в памяти, чтобы тесты не трогали файлы.</summary>
public sealed class TestContext
{
    public InMemorySupplierRepository Suppliers { get; } = new();
    public InMemoryProductRepository Products { get; } = new();
    public InMemoryShelfRepository Shelves { get; } = new();
    public InMemorySupplierProductRepository Links { get; } = new();
    public InMemoryStockBalanceRepository Balances { get; } = new();
    public InMemoryPurchaseOrderRepository Orders { get; } = new();
    public InMemoryInvoiceRepository Invoices { get; } = new();
    public InMemoryGoodsReceiptRepository Receipts { get; } = new();
    public InMemorySaleRepository Sales { get; } = new();
    public InMemoryStockMovementRepository Movements { get; } = new();

    public DtoMapper Mapper { get; }
    public StockService Stock { get; }
    public CatalogService Catalog { get; }
    public PurchaseOrderService OrderService { get; }
    public InvoiceService InvoiceService { get; }
    public GoodsReceiptService ReceiptService { get; }
    public SaleService SaleService { get; }
    public ReportService ReportService { get; }

    public TestContext()
    {
        var configuration = new MapperConfiguration(
            cfg => cfg.AddProfile<MappingProfile>(), NullLoggerFactory.Instance);
        Mapper = new DtoMapper(configuration.CreateMapper());

        Stock = new StockService(Products, Shelves, Balances, Movements);
        Catalog = new CatalogService(Suppliers, Products, Shelves, Links, Orders, Receipts,
            Sales, Movements, Stock, Mapper);
        OrderService = new PurchaseOrderService(Orders, Products, Suppliers, Links, Invoices,
            Receipts, Stock, Mapper);
        InvoiceService = new InvoiceService(Invoices, Orders, Mapper);
        ReceiptService = new GoodsReceiptService(Receipts, Orders, Products, Shelves, Suppliers,
            Stock, Mapper);
        SaleService = new SaleService(Sales, Products, Shelves, Stock, Mapper);
        ReportService = new ReportService(Products, Shelves, Suppliers, Links, Stock, Mapper);
    }

    public Product AddProduct(string name = "Морковь", int min = 0, int? target = null,
        bool active = true, MeasurementUnit unit = MeasurementUnit.Kilogram)
    {
        return Products.Seed(new Product
        {
            Name = name,
            Article = "ART-" + name,
            Unit = unit,
            MinimumStockQuantity = min,
            TargetStockQuantity = target,
            IsActive = active
        });
    }

    public Supplier AddSupplier(string name = "ООО Ромашка", string phone = "+7 383 000-00-00",
        bool active = true)
    {
        return Suppliers.Seed(new Supplier { Name = name, Phone = phone, IsActive = active });
    }

    public Shelf AddShelf(string code = "A-1", string name = "Полка A-1", bool active = true)
    {
        return Shelves.Seed(new Shelf { Code = code, Name = name, IsActive = active });
    }

    public SupplierProduct AddLink(Guid productId, Guid supplierId, decimal? price = 100m,
        int? min = null, int? multiple = null, bool preferred = false, bool active = true,
        int? leadTime = null)
    {
        return Links.Seed(new SupplierProduct
        {
            ProductId = productId,
            SupplierId = supplierId,
            PurchasePrice = price,
            MinimumOrderQuantity = min,
            OrderMultiple = multiple,
            LeadTimeDays = leadTime,
            IsPreferred = preferred,
            IsActive = active
        });
    }

    public StockBalance AddBalance(Guid productId, Guid shelfId, int quantity)
    {
        return Balances.Seed(new StockBalance
        {
            ProductId = productId,
            ShelfId = shelfId,
            Quantity = quantity
        });
    }

    public PurchaseOrder AddOrder(Guid supplierId, PurchaseOrderStatus status = PurchaseOrderStatus.Draft)
    {
        return Orders.Seed(new PurchaseOrder
        {
            Number = "PO-20260101-0001",
            SupplierId = supplierId,
            Status = status
        });
    }

    public PurchaseOrderLine AddOrderLine(PurchaseOrder order, Product product, int quantity,
        int received = 0, decimal price = 100m)
    {
        var line = new PurchaseOrderLine
        {
            ProductId = product.Id,
            ProductName = product.Name,
            Unit = product.Unit,
            OrderedQuantity = quantity,
            ReceivedQuantity = received,
            UnitPrice = price,
            TotalPrice = price * quantity,
            IsClosed = received >= quantity
        };
        order.Lines.Add(line);
        order.TotalAmount += line.TotalPrice;
        return line;
    }
}
