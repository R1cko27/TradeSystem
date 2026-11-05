// TradeSystem.UnitTests/Tests/OrderServiceTests.cs

using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TradeSystem.Application.Dto;
using TradeSystem.Application.Exceptions;
using TradeSystem.Domain.Entities;
using TradeSystem.Domain.Enums;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TradeSystem.UnitTests.Tests;

/// <summary>Проверки заказов поставщикам и автоматического формирования.</summary>
[TestClass]
public sealed class OrderServiceTests
{
    private static CreatePurchaseOrderInput Order(Guid supplierId, Guid productId, int quantity,
        decimal? price = 50m)
        => new()
        {
            SupplierId = supplierId,
            Lines = new() { new CreatePurchaseOrderLineInput { ProductId = productId, Quantity = quantity, UnitPrice = price } }
        };

    // Проверяем: создание заказа без поставщика.
    [TestMethod]
    public async Task CreateOrder_WithoutSupplier_Throws()
    {
        var context = new TestContext();
        var product = context.AddProduct();

        await Check.ThrowsAsync<BusinessException>(
            () => context.OrderService.CreateOrderAsync(Order(Guid.Empty, product.Id, 1)),
            "Не выбран поставщик");
    }

    // Ожидаем: исключение «Заказ не содержит позиций».
    [TestMethod]
    public async Task CreateOrder_WithoutLines_Throws()
    {
        var context = new TestContext();
        var supplier = context.AddSupplier();

        await Check.ThrowsAsync<BusinessException>(
            () => context.OrderService.CreateOrderAsync(new CreatePurchaseOrderInput { SupplierId = supplier.Id }),
            "не содержит позиций");
    }

    // Проверяем: создание заказа для несуществующего поставщика.
    // Ожидаем: исключение «Поставщик не найден».
    [TestMethod]
    public async Task CreateOrder_UnknownSupplier_Throws()
    {
        var context = new TestContext();
        var product = context.AddProduct();

        await Check.ThrowsAsync<BusinessException>(
            () => context.OrderService.CreateOrderAsync(Order(Guid.NewGuid(), product.Id, 1)),
            "Поставщик не найден");
    }

    // Проверяем: создание заказа с нулевым количеством в позиции.
    [TestMethod]
    public async Task CreateOrder_ZeroQuantity_Throws()
    {
        var context = new TestContext();
        var supplier = context.AddSupplier();
        var product = context.AddProduct();
        context.AddLink(product.Id, supplier.Id);

        await Check.ThrowsAsync<BusinessException>(
            () => context.OrderService.CreateOrderAsync(Order(supplier.Id, product.Id, 0)),
            "Количество в позиции");
    }

    // Ожидаем: исключение «Товар в позиции не найден».
    [TestMethod]
    public async Task CreateOrder_UnknownProduct_Throws()
    {
        var context = new TestContext();
        var supplier = context.AddSupplier();

        await Check.ThrowsAsync<BusinessException>(
            () => context.OrderService.CreateOrderAsync(Order(supplier.Id, Guid.NewGuid(), 1)),
            "Товар в позиции не найден");
    }

    // Проверяем: заказ товара, который поставщик не поставляет.
    // Ожидаем: исключение о том, что товар не поставляется выбранным поставщиком.
    [TestMethod]
    public async Task CreateOrder_ProductWithoutLink_Throws()
    {
        var context = new TestContext();
        var supplier = context.AddSupplier();
        var product = context.AddProduct();

        await Check.ThrowsAsync<BusinessException>(
            () => context.OrderService.CreateOrderAsync(Order(supplier.Id, product.Id, 1)),
            "не поставляется");
    }

    // Ожидаем: статус «Черновик», номер вида PO-ГГГГММДД-НННН, сумма по строке.
    [TestMethod]
    public async Task CreateOrder_Valid_CreatesDraft()
    {
        var context = new TestContext();
        var supplier = context.AddSupplier();
        var product = context.AddProduct();
        context.AddLink(product.Id, supplier.Id);

        var dto = await context.OrderService.CreateOrderAsync(Order(supplier.Id, product.Id, 2, price: 50m));

        Check.Equal(PurchaseOrderStatus.Draft, dto.Status);
        Check.True(Regex.IsMatch(dto.Number, @"^PO-\d{8}-\d{4}$"), $"неверный номер заказа: {dto.Number}");
        Check.Equal(100m, dto.TotalAmount);
    }

    // Проверяем: нумерацию второго заказа за тот же день.
    [TestMethod]
    public async Task CreateOrder_SecondOrderSameDay_IncrementsNumber()
    {
        var context = new TestContext();
        var supplier = context.AddSupplier();
        var product = context.AddProduct();
        context.AddLink(product.Id, supplier.Id);

        await context.OrderService.CreateOrderAsync(Order(supplier.Id, product.Id, 1));
        var second = await context.OrderService.CreateOrderAsync(Order(supplier.Id, product.Id, 1));

        Check.True(second.Number.EndsWith("-0002"), $"ожидался номер с 0002, получен {second.Number}");
    }

    [TestMethod]
    public async Task ConfirmOrder_Draft_ChangesStatus()
    {
        var context = new TestContext();
        var supplier = context.AddSupplier();
        var order = context.AddOrder(supplier.Id);

        await context.OrderService.ConfirmOrderAsync(order.Id);

        Check.Equal(PurchaseOrderStatus.Confirmed, context.Orders.Items.Single().Status);
    }

    [TestMethod]
    public async Task ConfirmOrder_NonDraft_Throws()
    {
        var context = new TestContext();
        var supplier = context.AddSupplier();
        var order = context.AddOrder(supplier.Id, PurchaseOrderStatus.Confirmed);

        await Check.ThrowsAsync<BusinessException>(
            () => context.OrderService.ConfirmOrderAsync(order.Id),
            "Черновик");
    }

    [TestMethod]
    public async Task CancelOrder_Draft_ChangesStatus()
    {
        var context = new TestContext();
        var supplier = context.AddSupplier();
        var order = context.AddOrder(supplier.Id);

        await context.OrderService.CancelOrderAsync(order.Id);

        Check.Equal(PurchaseOrderStatus.Cancelled, context.Orders.Items.Single().Status);
    }

    // Проверяем: отмену заказа, который уже отправлен поставщику.
    [TestMethod]
    public async Task CancelOrder_NonDraft_Throws()
    {
        var context = new TestContext();
        var supplier = context.AddSupplier();
        var order = context.AddOrder(supplier.Id, PurchaseOrderStatus.Sent);

        await Check.ThrowsAsync<BusinessException>(
            () => context.OrderService.CancelOrderAsync(order.Id),
            "Черновик");
    }

    // Проверяем: закрытие заказа, который ещё не получен.
    [TestMethod]
    public async Task CloseOrder_NotReceived_Throws()
    {
        var context = new TestContext();
        var supplier = context.AddSupplier();
        var order = context.AddOrder(supplier.Id, PurchaseOrderStatus.Confirmed);

        await Check.ThrowsAsync<BusinessException>(
            () => context.OrderService.CloseOrderAsync(order.Id),
            "Получен");
    }

    [TestMethod]
    public async Task CloseOrder_Received_ChangesStatus()
    {
        var context = new TestContext();
        var supplier = context.AddSupplier();
        var order = context.AddOrder(supplier.Id, PurchaseOrderStatus.Received);

        await context.OrderService.CloseOrderAsync(order.Id);

        Check.Equal(PurchaseOrderStatus.Closed, context.Orders.Items.Single().Status);
    }

    // Ожидаем: заполнены название поставщика и статус оплаты «Не оплачен».
    [TestMethod]
    public async Task GetOrders_FillsSupplierAndPayment()
    {
        var context = new TestContext();
        var supplier = context.AddSupplier("ООО Ромашка");
        context.AddOrder(supplier.Id);

        var items = await context.OrderService.GetOrdersAsync();

        Check.Equal("ООО Ромашка", items.Single().SupplierName);
        Check.Equal("Не оплачен", items.Single().PaymentStatusLabel);
    }

    // Проверяем: автоматическое формирование при отсутствии дефицита.
    [TestMethod]
    public async Task AutoGenerate_NoDeficit_ReturnsEmpty()
    {
        var context = new TestContext();
        var supplier = context.AddSupplier();
        var product = context.AddProduct(min: 5, target: 10);
        context.AddLink(product.Id, supplier.Id);
        var shelf = context.AddShelf();
        context.AddBalance(product.Id, shelf.Id, 8);

        var result = await context.OrderService.AutoGenerateOrdersAsync();

        Check.Equal(0, result.Orders.Count);
        Check.Equal(0, context.Orders.Items.Count);
    }

    // Проверяем: расчёт количества при автоматическом формировании.
    // Ожидаем: потребность 17 поднимается до минимальной партии и кратности 5, получается 20.
    [TestMethod]
    public async Task AutoGenerate_RoundsQuantityBySupplierTerms()
    {
        var context = new TestContext();
        var supplier = context.AddSupplier();
        var product = context.AddProduct("Морковь", min: 10, target: 20);
        context.AddLink(product.Id, supplier.Id, price: 100m, min: 10, multiple: 5);
        var shelf = context.AddShelf();
        context.AddBalance(product.Id, shelf.Id, 3);

        var result = await context.OrderService.AutoGenerateOrdersAsync();

        Check.Equal(1, result.Orders.Count);
        Check.Equal(20, result.Orders[0].Lines[0].OrderedQuantity);
        Check.Equal(2000m, result.Orders[0].TotalAmount);
    }

    // Проверяем: товар с дефицитом, но без активного поставщика.
    // Ожидаем: предупреждение в результате и отсутствие заказа.
    [TestMethod]
    public async Task AutoGenerate_ProductWithoutSupplier_AddsWarning()
    {
        var context = new TestContext();
        context.AddProduct("Морковь", min: 10, target: 20);

        var result = await context.OrderService.AutoGenerateOrdersAsync();

        Check.Equal(0, result.Orders.Count);
        Check.Equal(1, result.Warnings.Count);
        Check.Contains("не имеет активного поставщика", result.Warnings[0]);
    }

    // Ожидаем: два дефицитных товара одного поставщика попадают в один заказ.
    [TestMethod]
    public async Task AutoGenerate_GroupsBySupplier()
    {
        var context = new TestContext();
        var supplier = context.AddSupplier();
        var first = context.AddProduct("Морковь", min: 10, target: 20);
        var second = context.AddProduct("Свёкла", min: 10, target: 20);
        context.AddLink(first.Id, supplier.Id, price: 100m);
        context.AddLink(second.Id, supplier.Id, price: 50m);

        var result = await context.OrderService.AutoGenerateOrdersAsync();

        Check.Equal(1, result.Orders.Count);
        Check.Equal(2, result.Orders[0].Lines.Count);
    }
}
