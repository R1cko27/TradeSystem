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

// Сервисы с репозиториями в памяти для изоляции тестов.
public sealed class TestContext
{
    public InMemorySupplierRepository Suppliers { get; } = new(); // Поставщики
    public InMemoryProductRepository Products { get; } = new(); // Товары
    public InMemoryShelfRepository Shelves { get; } = new(); // Полки
    public InMemorySupplierProductRepository Links { get; } = new(); // Связи товара и поставщика
    public InMemoryStockBalanceRepository Balances { get; } = new(); // Остатки
    public InMemoryPurchaseOrderRepository Orders { get; } = new(); // Заказы
    public InMemoryInvoiceRepository Invoices { get; } = new(); // Счета
    public InMemoryGoodsReceiptRepository Receipts { get; } = new(); // Поступления
    public InMemorySaleRepository Sales { get; } = new(); // Продажи
    public InMemoryStockMovementRepository Movements { get; } = new(); // Движения

    public DtoMapper Mapper { get; } // Преобразователь в DTO
    public StockService Stock { get; } // Складской сервис
    public CatalogService Catalog { get; } // Сервис справочников
    public PurchaseOrderService OrderService { get; } // Сервис заказов
    public InvoiceService InvoiceService { get; } // Сервис счетов
    public GoodsReceiptService ReceiptService { get; } // Сервис поступлений
    public SaleService SaleService { get; } // Сервис продаж
    public ReportService ReportService { get; } // Сервис отчётов

    // Собирает граф сервисов на репозиториях в памяти.
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

    // Добавляет товар в репозиторий.
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

    // Добавляет поставщика.
    public Supplier AddSupplier(string name = "ООО Ромашка", string phone = "+7 383 000-00-00",
        bool active = true)
    {
        return Suppliers.Seed(new Supplier { Name = name, Phone = phone, IsActive = active });
    }

    // Добавляет полку.
    public Shelf AddShelf(string code = "A-1", string name = "Полка A-1", bool active = true)
    {
        return Shelves.Seed(new Shelf { Code = code, Name = name, IsActive = active });
    }

    // Добавляет связь товара и поставщика.
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

    // Добавляет остаток на полку.
    public StockBalance AddBalance(Guid productId, Guid shelfId, int quantity)
    {
        return Balances.Seed(new StockBalance
        {
            ProductId = productId,
            ShelfId = shelfId,
            Quantity = quantity
        });
    }

    // Добавляет заказ.
    public PurchaseOrder AddOrder(Guid supplierId, PurchaseOrderStatus status = PurchaseOrderStatus.Draft)
    {
        return Orders.Seed(new PurchaseOrder
        {
            Number = "PO-20260101-0001",
            SupplierId = supplierId,
            Status = status
        });
    }

    // Добавляет позицию в заказ и пересчитывает сумму.
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
