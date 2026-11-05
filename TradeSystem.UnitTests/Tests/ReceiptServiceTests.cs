// TradeSystem.UnitTests/Tests/ReceiptServiceTests.cs

using System;
using System.Linq;
using System.Threading.Tasks;
using TradeSystem.Application.Dto;
using TradeSystem.Application.Exceptions;
using TradeSystem.Domain.Entities;
using TradeSystem.Domain.Enums;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TradeSystem.UnitTests.Tests;

/// <summary>Проверки поступлений товара: черновик, проведение, влияние на склад и заказ.</summary>
[TestClass]
public sealed class ReceiptServiceTests
{
    private static GoodsReceiptInput Receipt(Guid supplierId, Guid productId, Guid shelfId,
        int quantity = 5, Guid? orderLineId = null, Guid? orderId = null)
        => new()
        {
            SupplierId = supplierId,
            OrderId = orderId,
            Lines = new()
            {
                new GoodsReceiptLineInput
                {
                    ProductId = productId,
                    ShelfId = shelfId,
                    Quantity = quantity,
                    OrderLineId = orderLineId
                }
            }
        };

    // Проверяем: создание поступления без поставщика.
    [TestMethod]
    public async Task CreateDraft_WithoutSupplier_Throws()
    {
        var context = new TestContext();
        var product = context.AddProduct();
        var shelf = context.AddShelf();

        await Check.ThrowsAsync<BusinessException>(
            () => context.ReceiptService.CreateDraftAsync(Receipt(Guid.Empty, product.Id, shelf.Id)),
            "Не выбран поставщик");
    }

    [TestMethod]
    public async Task CreateDraft_WithoutLines_Throws()
    {
        var context = new TestContext();
        var supplier = context.AddSupplier();

        await Check.ThrowsAsync<BusinessException>(
            () => context.ReceiptService.CreateDraftAsync(new GoodsReceiptInput { SupplierId = supplier.Id }),
            "не содержит позиций");
    }

    // Проверяем: создание поступления для несуществующего поставщика.
    // Ожидаем: исключение «Поставщик не найден».
    [TestMethod]
    public async Task CreateDraft_UnknownSupplier_Throws()
    {
        var context = new TestContext();
        var product = context.AddProduct();
        var shelf = context.AddShelf();

        await Check.ThrowsAsync<BusinessException>(
            () => context.ReceiptService.CreateDraftAsync(Receipt(Guid.NewGuid(), product.Id, shelf.Id)),
            "Поставщик не найден");
    }

    [TestMethod]
    public async Task CreateDraft_ZeroQuantity_Throws()
    {
        var context = new TestContext();
        var supplier = context.AddSupplier();
        var product = context.AddProduct();
        var shelf = context.AddShelf();

        await Check.ThrowsAsync<BusinessException>(
            () => context.ReceiptService.CreateDraftAsync(Receipt(supplier.Id, product.Id, shelf.Id, 0)),
            "Количество");
    }

    // Ожидаем: исключение «Товар не найден».
    [TestMethod]
    public async Task CreateDraft_UnknownProduct_Throws()
    {
        var context = new TestContext();
        var supplier = context.AddSupplier();
        var shelf = context.AddShelf();

        await Check.ThrowsAsync<BusinessException>(
            () => context.ReceiptService.CreateDraftAsync(Receipt(supplier.Id, Guid.NewGuid(), shelf.Id)),
            "Товар не найден");
    }

    [TestMethod]
    public async Task CreateDraft_UnknownShelf_Throws()
    {
        var context = new TestContext();
        var supplier = context.AddSupplier();
        var product = context.AddProduct();

        await Check.ThrowsAsync<BusinessException>(
            () => context.ReceiptService.CreateDraftAsync(Receipt(supplier.Id, product.Id, Guid.NewGuid())),
            "Полка не найдена");
    }

    // Ожидаем: статус «Черновик» и номер вида GR-ГГГГММДД-НННН.
    [TestMethod]
    public async Task CreateDraft_Valid_ReturnsDraftWithNumber()
    {
        var context = new TestContext();
        var supplier = context.AddSupplier();
        var product = context.AddProduct();
        var shelf = context.AddShelf();

        var dto = await context.ReceiptService.CreateDraftAsync(Receipt(supplier.Id, product.Id, shelf.Id));

        Check.Equal(GoodsReceiptStatus.Draft, dto.Status);
        Check.True(dto.Number.StartsWith("GR-"), $"неверный номер поступления: {dto.Number}");
        Check.Equal(1, dto.Lines.Count);
    }

    // Ожидаем: остаток на полке увеличен, статус «Проведён», движение записано.
    [TestMethod]
    public async Task Post_IncreasesStockAndSetsPosted()
    {
        var context = new TestContext();
        var supplier = context.AddSupplier();
        var product = context.AddProduct();
        var shelf = context.AddShelf();
        var receipt = await context.ReceiptService.CreateDraftAsync(
            Receipt(supplier.Id, product.Id, shelf.Id, quantity: 6));

        var dto = await context.ReceiptService.PostAsync(receipt.Id);

        Check.Equal(GoodsReceiptStatus.Posted, dto.Status);
        Check.Equal(6, context.Balances.Items.Single().Quantity);
        Check.Equal(6, context.Movements.Items.Single().QuantityDelta);
    }

    [TestMethod]
    public async Task Post_NonDraft_Throws()
    {
        var context = new TestContext();
        var receipt = context.Receipts.Seed(new GoodsReceipt
        {
            Number = "GR-1",
            Status = GoodsReceiptStatus.Posted
        });

        await Check.ThrowsAsync<BusinessException>(
            () => context.ReceiptService.PostAsync(receipt.Id),
            "Черновик");
    }

    // Ожидаем: полученное количество в позиции увеличивается, заказ переходит в «Частично получен».
    [TestMethod]
    public async Task Post_WithOrder_UpdatesReceivedQuantity()
    {
        var context = new TestContext();
        var supplier = context.AddSupplier();
        var product = context.AddProduct();
        var shelf = context.AddShelf();
        var order = context.AddOrder(supplier.Id);
        var line = context.AddOrderLine(order, product, quantity: 10);

        var receipt = await context.ReceiptService.CreateDraftAsync(
            Receipt(supplier.Id, product.Id, shelf.Id, quantity: 4, orderLineId: line.Id, orderId: order.Id));
        await context.ReceiptService.PostAsync(receipt.Id);

        Check.Equal(4, order.Lines[0].ReceivedQuantity);
        Check.Equal(PurchaseOrderStatus.PartiallyReceived, order.Status);
    }

    // Проверяем: проведение поступления на полную заказанную величину.
    // Ожидаем: позиция закрыта, заказ получает статус «Получен».
    [TestMethod]
    public async Task Post_FullOrderQuantity_SetsReceived()
    {
        var context = new TestContext();
        var supplier = context.AddSupplier();
        var product = context.AddProduct();
        var shelf = context.AddShelf();
        var order = context.AddOrder(supplier.Id);
        var line = context.AddOrderLine(order, product, quantity: 5);

        var receipt = await context.ReceiptService.CreateDraftAsync(
            Receipt(supplier.Id, product.Id, shelf.Id, quantity: 5, orderLineId: line.Id, orderId: order.Id));
        await context.ReceiptService.PostAsync(receipt.Id);

        Check.True(order.Lines[0].IsClosed, "позиция должна быть закрыта");
        Check.Equal(PurchaseOrderStatus.Received, order.Status);
    }

    [TestMethod]
    public async Task DeleteDraft_Posted_Throws()
    {
        var context = new TestContext();
        var receipt = context.Receipts.Seed(new GoodsReceipt
        {
            Number = "GR-1",
            Status = GoodsReceiptStatus.Posted
        });

        await Check.ThrowsAsync<BusinessException>(
            () => context.ReceiptService.DeleteDraftAsync(receipt.Id),
            "Черновик");
    }

    // Ожидаем: документ удаляется из репозитория.
    [TestMethod]
    public async Task DeleteDraft_Draft_Removes()
    {
        var context = new TestContext();
        var receipt = context.Receipts.Seed(new GoodsReceipt { Number = "GR-1" });

        await context.ReceiptService.DeleteDraftAsync(receipt.Id);

        Check.Equal(0, context.Receipts.Items.Count);
    }
}
