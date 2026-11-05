// TradeSystem.UnitTests/Tests/DomainTests.cs

using System;
using TradeSystem.Domain.Entities;
using TradeSystem.Domain.Enums;
using TradeSystem.Domain.ValueObjects;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TradeSystem.UnitTests.Tests;

/// <summary>Проверки начальных значений доменных сущностей.</summary>
[TestClass]
public sealed class DomainTests
{
    // Проверяем: новый товар получает идентификатор и дату создания.
    // Ожидаем: Id не пустой, CreatedAtUtc отличается от текущего времени не более чем на минуту.
    [TestMethod]
    public void Product_NewInstance_HasIdAndCreatedAt()
    {
        var product = new Product();

        Check.False(product.Id == Guid.Empty, "идентификатор не должен быть пустым");
        Check.True(Math.Abs((DateTimeOffset.UtcNow - product.CreatedAtUtc).TotalMinutes) < 1,
            "дата создания должна быть близка к текущему времени");
    }

    // Проверяем: значения товара по умолчанию.
    // Ожидаем: единица измерения не указана, товар активен, минимальный остаток равен нулю.
    [TestMethod]
    public void Product_Defaults_AreExpected()
    {
        var product = new Product();

        Check.Equal(MeasurementUnit.NotSpecified, product.Unit);
        Check.True(product.IsActive, "товар по умолчанию активен");
        Check.Equal(0, product.MinimumStockQuantity);
        Check.Null(product.TargetStockQuantity);
        Check.Null(product.Description);
    }

    // Проверяем: два новых товара получают разные идентификаторы.
    [TestMethod]
    public void Product_TwoInstances_HaveDifferentIds()
    {
        var first = new Product();
        var second = new Product();

        Check.True(first.Id != second.Id, "идентификаторы должны различаться");
    }

    // Проверяем: поставщик при создании получает объект адреса.
    // Ожидаем: Address не null, поставщик активен, телефон пустой.
    [TestMethod]
    public void Supplier_Defaults_HaveEmptyAddressAndActive()
    {
        var supplier = new Supplier();

        Check.NotNull(supplier.Address, "адрес должен быть создан");
        Check.True(supplier.IsActive, "поставщик по умолчанию активен");
        Check.Equal(string.Empty, supplier.Phone);
    }

    // Проверяем: значения полки по умолчанию.
    // Ожидаем: код и название пустые, полка активна.
    [TestMethod]
    public void Shelf_Defaults_AreExpected()
    {
        var shelf = new Shelf();

        Check.Equal(string.Empty, shelf.Code);
        Check.Equal(string.Empty, shelf.Name);
        Check.True(shelf.IsActive, "полка по умолчанию активна");
    }

    // Проверяем: значения связи товара и поставщика по умолчанию.
    // Ожидаем: связь активна, не приоритетна, цена не задана.
    [TestMethod]
    public void SupplierProduct_Defaults_AreExpected()
    {
        var link = new SupplierProduct();

        Check.True(link.IsActive, "связь по умолчанию активна");
        Check.False(link.IsPreferred, "связь не приоритетна по умолчанию");
        Check.Null(link.PurchasePrice);
        Check.Null(link.OrderMultiple);
    }

    // Проверяем: значения заказа по умолчанию.
    // Ожидаем: статус «Черновик», список позиций пуст, дата заказа близка к текущей.
    [TestMethod]
    public void PurchaseOrder_Defaults_AreDraftWithEmptyLines()
    {
        var order = new PurchaseOrder();

        Check.Equal(PurchaseOrderStatus.Draft, order.Status);
        Check.Equal(0, order.Lines.Count);
        Check.True(Math.Abs((DateTimeOffset.UtcNow - order.OrderDateUtc).TotalMinutes) < 1,
            "дата заказа должна быть близка к текущей");
    }

    // Проверяем: значения позиции заказа по умолчанию.
    // Ожидаем: количества и сумма равны нулю, позиция не закрыта.
    [TestMethod]
    public void PurchaseOrderLine_Defaults_AreZero()
    {
        var line = new PurchaseOrderLine();

        Check.Equal(0, line.OrderedQuantity);
        Check.Equal(0, line.ReceivedQuantity);
        Check.Equal(0m, line.TotalPrice);
        Check.False(line.IsClosed, "позиция по умолчанию не закрыта");
    }

    // Проверяем: значения счёта по умолчанию.
    // Ожидаем: статус «Не оплачен», дата оплаты не задана, дата выставления близка к текущей.
    [TestMethod]
    public void Invoice_Defaults_AreUnpaid()
    {
        var invoice = new Invoice();

        Check.Equal(PaymentStatus.Unpaid, invoice.PaymentStatus);
        Check.Null(invoice.PaidAtUtc);
        Check.True(Math.Abs((DateTimeOffset.UtcNow - invoice.IssueDateUtc).TotalMinutes) < 1,
            "дата выставления должна быть близка к текущей");
    }

    // Проверяем: значения поступления товара по умолчанию.
    [TestMethod]
    public void GoodsReceipt_Defaults_AreDraft()
    {
        var receipt = new GoodsReceipt();

        Check.Equal(GoodsReceiptStatus.Draft, receipt.Status);
        Check.Equal(0, receipt.Lines.Count);
    }

    // Проверяем: значения продажи по умолчанию.
    [TestMethod]
    public void Sale_Defaults_AreDraft()
    {
        var sale = new Sale();

        Check.Equal(SaleStatus.Draft, sale.Status);
        Check.Equal(0, sale.Lines.Count);
    }

    // Проверяем: значения остатка товара по умолчанию.
    // Ожидаем: количество равно нулю, примечание не задано.
    [TestMethod]
    public void StockBalance_Defaults_AreZero()
    {
        var balance = new StockBalance();

        Check.Equal(0, balance.Quantity);
        Check.Null(balance.Note);
    }

    // Проверяем: значения движения товара по умолчанию.
    // Ожидаем: разность равна нулю, дата движения близка к текущей.
    [TestMethod]
    public void StockMovement_Defaults_HaveDate()
    {
        var movement = new StockMovement();

        Check.Equal(0, movement.QuantityDelta);
        Check.True(Math.Abs((DateTimeOffset.UtcNow - movement.OccurredAtUtc).TotalMinutes) < 1,
            "дата движения должна быть близка к текущей");
    }

    // Ожидаем: все строковые компоненты пусты или null.
    [TestMethod]
    public void Address_NewInstance_HasNoComponents()
    {
        var address = new Address();

        Check.True(string.IsNullOrEmpty(address.Country), "страна не задана");
        Check.True(string.IsNullOrEmpty(address.City), "город не задан");
        Check.True(string.IsNullOrEmpty(address.Street), "улица не задана");
        Check.True(string.IsNullOrEmpty(address.PostalCode), "индекс не задан");
    }

    // Проверяем: дата изменения у новой сущности не заполнена.
    [TestMethod]
    public void Entity_NewInstance_HasNoModifiedDate()
    {
        var product = new Product();

        Check.Null(product.ModifiedAtUtc);
    }
}
