// TradeSystem.UnitTests/Tests/MapperTests.cs

using System;
using System.Collections.Generic;
using TradeSystem.Application.Mappers;
using TradeSystem.Domain.Entities;
using TradeSystem.Domain.Enums;
using TradeSystem.Domain.ValueObjects;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TradeSystem.UnitTests.Tests;

// Проверки преобразования сущностей в DTO и форматирования адреса.
[TestClass]
public sealed class MapperTests
{
    // Адрес со всеми заполненными полями.
    private static Address FullAddress() => new()
    {
        Country = "Россия",
        Region = "Новосибирская область",
        City = "Новосибирск",
        Street = "Ленина",
        Building = "10",
        Apartment = "5",
        PostalCode = "630000"
    };

    // Ожидаем: компоненты соединяются через запятую, улица и дом получают сокращения.
    [TestMethod]
    public void Format_FullAddress_JoinsComponents()
    {
        var text = DtoMapper.Format(FullAddress());

        Check.Equal("Россия, Новосибирская область, Новосибирск, ул. Ленина, д. 10, кв. 5, 630000", text);
    }
// Ожидаем: null даёт пустую строку.

    [TestMethod]
    public void Format_NullAddress_ReturnsEmpty()
    {
        Check.Equal(string.Empty, DtoMapper.Format(null!));
    }
// Ожидаем: пустой адрес даёт пустую строку.

    [TestMethod]
    public void Format_EmptyAddress_ReturnsEmpty()
    {
        Check.Equal(string.Empty, DtoMapper.Format(new Address()));
    }

    // Проверяем: форматирование адреса только с улицей.
    [TestMethod]
    public void Format_StreetOnly_ReturnsStreet()
    {
        var address = new Address { Street = "Ленина" };

        Check.Equal("ул. Ленина", DtoMapper.Format(address));
    }

    // Проверяем: преобразование поставщика в DTO.
    // Ожидаем: переносятся название, телефон и число товаров, адрес склеен.
    [TestMethod]
    public void ToDto_Supplier_CopiesFields()
    {
        var context = new TestContext();
        var supplier = new Supplier
        {
            Name = "ООО Ромашка",
            Phone = "+7 383 111-22-33",
            Address = FullAddress()
        };

        var dto = context.Mapper.ToDto(supplier, productsCount: 4);

        Check.Equal("ООО Ромашка", dto.Name);
        Check.Equal(4, dto.ProductsCount);
        Check.Equal("Новосибирск", dto.City);
        Check.Contains("ул. Ленина", dto.FullAddress);
    }

    // Ожидаем: переносятся код, название и число товаров.
    [TestMethod]
    public void ToDto_Shelf_CopiesFields()
    {
        var context = new TestContext();
        var shelf = new Shelf { Code = "A-1", Name = "Полка A-1" };

        var dto = context.Mapper.ToDto(shelf, productsCount: 2);

        Check.Equal("A-1", dto.Code);
        Check.Equal("Полка A-1", dto.Name);
        Check.Equal(2, dto.ProductsCount);
    }

    // Ожидаем: подставляются остаток, число поставщиков и полка по умолчанию, единица переведена в текст.
    [TestMethod]
    public void ToDto_Product_CopiesCalculatedFields()
    {
        var context = new TestContext();
        var product = new Product { Name = "Морковь", Unit = MeasurementUnit.Kilogram };

        var dto = context.Mapper.ToDto(product, currentStock: 5, suppliersCount: 2,
            defaultShelfName: "A-1");

        Check.Equal("Морковь", dto.Name);
        Check.Equal("кг", dto.UnitLabel);
        Check.Equal(5, dto.CurrentStock);
        Check.Equal(2, dto.SuppliersCount);
        Check.Equal("A-1", dto.DefaultShelfName);
    }

    // Проверяем: признак необходимости пополнения при остатке ниже минимума.
    [TestMethod]
    public void ToDto_Product_BelowMinimum_NeedsReplenish()
    {
        var context = new TestContext();
        var product = new Product { MinimumStockQuantity = 10 };

        var dto = context.Mapper.ToDto(product, currentStock: 3, suppliersCount: 0, defaultShelfName: null);

        Check.True(dto.NeedsReplenish, "товар ниже минимума требует пополнения");
    }

    // Проверяем: признак пополнения при нулевом минимуме.
    // Ожидаем: NeedsReplenish равен false, даже если остаток нулевой.
    [TestMethod]
    public void ToDto_Product_ZeroMinimum_NoReplenish()
    {
        var context = new TestContext();
        var product = new Product { MinimumStockQuantity = 0 };

        var dto = context.Mapper.ToDto(product, currentStock: 0, suppliersCount: 0, defaultShelfName: null);

        Check.False(dto.NeedsReplenish, "при нулевом минимуме пополнение не требуется");
    }

    // Проверяем: преобразование связи товара и поставщика.
    // Ожидаем: подставляются названия товара и поставщика.
    [TestMethod]
    public void ToDto_SupplierProduct_CopiesNames()
    {
        var context = new TestContext();
        var link = new SupplierProduct { PurchasePrice = 55.5m, OrderMultiple = 5 };

        var dto = context.Mapper.ToDto(link, productName: "Морковь", supplierName: "ООО Ромашка");

        Check.Equal("Морковь", dto.ProductName);
        Check.Equal("ООО Ромашка", dto.SupplierName);
        Check.Equal(55.5m, dto.PurchasePrice);
        Check.Equal(5, dto.OrderMultiple);
    }

    // Ожидаем: подставляются поставщик, статус оплаты и строки заказа.
    [TestMethod]
    public void ToDto_PurchaseOrder_CopiesLinesAndStatus()
    {
        var context = new TestContext();
        var order = new PurchaseOrder { Number = "PO-20260101-0001" };
        order.Lines.Add(new PurchaseOrderLine { ProductName = "Морковь", OrderedQuantity = 5 });

        var dto = context.Mapper.ToDto(order, supplierName: "ООО Ромашка",
            paymentStatus: PaymentStatus.Paid);

        Check.Equal("ООО Ромашка", dto.SupplierName);
        Check.Equal("Оплачен", dto.PaymentStatusLabel);
        Check.Equal("Черновик", dto.StatusLabel);
        Check.Equal(1, dto.Lines.Count);
    }

    // Проверяем: остаток к получению в позиции заказа.
    // Ожидаем: 10 заказано минус 4 получено равно 6.
    [TestMethod]
    public void ToDto_OrderLine_ComputesRemaining()
    {
        var context = new TestContext();
        var line = new PurchaseOrderLine { OrderedQuantity = 10, ReceivedQuantity = 4 };

        var dto = context.Mapper.ToDto(line);

        Check.Equal(6, dto.RemainingQuantity);
    }

    // Проверяем: остаток к получению при перевыполнении.
    [TestMethod]
    public void ToDto_OrderLine_OverReceived_RemainingIsZero()
    {
        var context = new TestContext();
        var line = new PurchaseOrderLine { OrderedQuantity = 5, ReceivedQuantity = 8 };

        var dto = context.Mapper.ToDto(line);

        Check.Equal(0, dto.RemainingQuantity);
    }

    // Ожидаем: подставляется номер заказа и русское название статуса оплаты.
    [TestMethod]
    public void ToDto_Invoice_CopiesOrderNumber()
    {
        var context = new TestContext();
        var invoice = new Invoice { Number = "INV-1", Amount = 100m, PaymentStatus = PaymentStatus.Unpaid };

        var dto = context.Mapper.ToDto(invoice, orderNumber: "PO-20260101-0001");

        Check.Equal("PO-20260101-0001", dto.OrderNumber);
        Check.Equal("Не оплачен", dto.PaymentStatusLabel);
        Check.Equal(100m, dto.Amount);
    }

    // Ожидаем: строки получают названия товара и полки из справочника.
    [TestMethod]
    public void ToDto_GoodsReceipt_ResolvesLineNames()
    {
        var context = new TestContext();
        var receipt = new GoodsReceipt { Number = "GR-1" };
        receipt.Lines.Add(new GoodsReceiptLine { Quantity = 3 });

        var dto = context.Mapper.ToDto(receipt, supplierName: "ООО Ромашка", orderNumber: null,
            resolver: _ => ("Морковь", "A-1"));

        Check.Equal("ООО Ромашка", dto.SupplierName);
        Check.Equal("Морковь", dto.Lines[0].ProductName);
        Check.Equal("A-1", dto.Lines[0].ShelfName);
    }

    // Ожидаем: сумма считается по строкам, статус переведён в текст.
    [TestMethod]
    public void ToDto_Sale_ComputesTotal()
    {
        var context = new TestContext();
        var sale = new Sale { Number = "SALE-1" };
        sale.Lines.Add(new SaleLine { Quantity = 2, UnitPrice = 30m, TotalPrice = 60m });
        sale.Lines.Add(new SaleLine { Quantity = 1, UnitPrice = 40m, TotalPrice = 40m });

        var dto = context.Mapper.ToDto(sale, resolver: _ => ("Морковь", "A-1"));

        Check.Equal(100m, dto.TotalAmount);
        Check.Equal("Черновик", dto.StatusLabel);
        Check.Equal(2, dto.Lines.Count);
    }
}
