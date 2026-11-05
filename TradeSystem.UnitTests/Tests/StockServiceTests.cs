// TradeSystem.UnitTests/Tests/StockServiceTests.cs

using System;
using System.Linq;
using TradeSystem.Application.Exceptions;
using TradeSystem.Domain.Enums;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TradeSystem.UnitTests.Tests;

/// <summary>Проверки складских операций: приход, расход, остатки, дефицит.</summary>
[TestClass]
public sealed class StockServiceTests
{
    // Проверяем: приход товара на полку, где остатка ещё нет.
    // Ожидаем: создаётся запись остатка с нужным количеством и одно движение с положительной разностью.
    [TestMethod]
    public async Task Receive_NewBalance_CreatesBalanceAndMovement()
    {
        var context = new TestContext();
        var product = context.AddProduct();
        var shelf = context.AddShelf();

        await context.Stock.ReceiveAsync(product.Id, shelf.Id, 5, StockMovementType.PurchaseReceipt,
            DocumentType.GoodsReceipt, Guid.NewGuid(), Guid.NewGuid(), null);

        var balance = context.Balances.Items.Single();
        Check.Equal(5, balance.Quantity);
        Check.Equal(5, context.Movements.Items.Single().QuantityDelta);
    }

    // Проверяем: приход товара на полку с существующим остатком.
    // Ожидаем: количество увеличивается с 3 до 7.
    [TestMethod]
    public async Task Receive_ExistingBalance_Increments()
    {
        var context = new TestContext();
        var product = context.AddProduct();
        var shelf = context.AddShelf();
        context.AddBalance(product.Id, shelf.Id, 3);

        await context.Stock.ReceiveAsync(product.Id, shelf.Id, 4, StockMovementType.PurchaseReceipt,
            DocumentType.GoodsReceipt, null, null, null);

        Check.Equal(7, context.Balances.Items.Single().Quantity);
    }

    // Ожидаем: исключение с текстом о количестве прихода.
    [TestMethod]
    public async Task Receive_ZeroQuantity_Throws()
    {
        var context = new TestContext();
        var product = context.AddProduct();
        var shelf = context.AddShelf();

        await Check.ThrowsAsync<BusinessException>(
            () => context.Stock.ReceiveAsync(product.Id, shelf.Id, 0, StockMovementType.PurchaseReceipt,
                DocumentType.GoodsReceipt, null, null, null),
            "Количество прихода");
    }

    // Ожидаем: исключение с текстом «Товар не найден».
    [TestMethod]
    public async Task Receive_UnknownProduct_Throws()
    {
        var context = new TestContext();
        var shelf = context.AddShelf();

        await Check.ThrowsAsync<BusinessException>(
            () => context.Stock.ReceiveAsync(Guid.NewGuid(), shelf.Id, 1, StockMovementType.PurchaseReceipt,
                DocumentType.GoodsReceipt, null, null, null),
            "Товар не найден");
    }

    // Ожидаем: исключение с текстом «Полка не найдена».
    [TestMethod]
    public async Task Receive_UnknownShelf_Throws()
    {
        var context = new TestContext();
        var product = context.AddProduct();

        await Check.ThrowsAsync<BusinessException>(
            () => context.Stock.ReceiveAsync(product.Id, Guid.NewGuid(), 1, StockMovementType.PurchaseReceipt,
                DocumentType.GoodsReceipt, null, null, null),
            "Полка не найдена");
    }

    // Ожидаем: количество уменьшается с 10 до 6, движение записано с разностью минус 4.
    [TestMethod]
    public async Task Ship_ExistingBalance_Decreases()
    {
        var context = new TestContext();
        var product = context.AddProduct();
        var shelf = context.AddShelf();
        context.AddBalance(product.Id, shelf.Id, 10);

        await context.Stock.ShipAsync(product.Id, shelf.Id, 4, StockMovementType.Sale,
            DocumentType.Sale, null, null, null);

        Check.Equal(6, context.Balances.Items.Single().Quantity);
        Check.Equal(-4, context.Movements.Items.Single().QuantityDelta);
    }

    // Проверяем: расход товара, которого нет на полке.
    // Ожидаем: исключение с текстом об отсутствии товара на полке.
    [TestMethod]
    public async Task Ship_NoBalance_Throws()
    {
        var context = new TestContext();
        var product = context.AddProduct();
        var shelf = context.AddShelf();

        await Check.ThrowsAsync<BusinessException>(
            () => context.Stock.ShipAsync(product.Id, shelf.Id, 1, StockMovementType.Sale,
                DocumentType.Sale, null, null, null),
            "нет данного товара");
    }

    // Ожидаем: исключение о недостатке товара на полке.
    [TestMethod]
    public async Task Ship_Insufficient_Throws()
    {
        var context = new TestContext();
        var product = context.AddProduct();
        var shelf = context.AddShelf();
        context.AddBalance(product.Id, shelf.Id, 2);

        await Check.ThrowsAsync<BusinessException>(
            () => context.Stock.ShipAsync(product.Id, shelf.Id, 5, StockMovementType.Sale,
                DocumentType.Sale, null, null, null),
            "Недостаточно");
    }

    // Ожидаем: исключение с текстом о количестве расхода.
    [TestMethod]
    public async Task Ship_ZeroQuantity_Throws()
    {
        var context = new TestContext();
        var product = context.AddProduct();
        var shelf = context.AddShelf();

        await Check.ThrowsAsync<BusinessException>(
            () => context.Stock.ShipAsync(product.Id, shelf.Id, 0, StockMovementType.Sale,
                DocumentType.Sale, null, null, null),
            "Количество расхода");
    }

    // Проверяем: суммарный остаток товара по нескольким полкам.
    [TestMethod]
    public async Task GetTotalStock_SumsAllShelves()
    {
        var context = new TestContext();
        var product = context.AddProduct();
        var first = context.AddShelf("A-1");
        var second = context.AddShelf("A-2");
        context.AddBalance(product.Id, first.Id, 5);
        context.AddBalance(product.Id, second.Id, 7);

        var total = await context.Stock.GetTotalStockAsync(product.Id);

        Check.Equal(12, total);
    }

    // Ожидаем: в словаре есть оба товара с правильными количествами.
    [TestMethod]
    public async Task GetTotalStocks_ReturnsAllProducts()
    {
        var context = new TestContext();
        var first = context.AddProduct("Морковь");
        var second = context.AddProduct("Свёкла");
        var shelf = context.AddShelf();
        context.AddBalance(first.Id, shelf.Id, 3);
        context.AddBalance(second.Id, shelf.Id, 2);

        var stocks = await context.Stock.GetTotalStocksAsync();

        Check.Equal(3, stocks[first.Id]);
        Check.Equal(2, stocks[second.Id]);
    }

    // Проверяем: остатки товара по полкам с названиями.
    [TestMethod]
    public async Task GetBalancesByProduct_ReturnsShelfNames()
    {
        var context = new TestContext();
        var product = context.AddProduct();
        var shelf = context.AddShelf("B-2", "Полка B-2");
        context.AddBalance(product.Id, shelf.Id, 4);

        var balances = await context.Stock.GetBalancesByProductAsync(product.Id);

        Check.Equal("B-2", balances.Single().ShelfCode);
        Check.Equal("Полка B-2", balances.Single().ShelfName);
    }

    // Проверяем: остатки товаров на полке с названиями товаров.
    [TestMethod]
    public async Task GetBalancesByShelf_ReturnsProductNames()
    {
        var context = new TestContext();
        var product = context.AddProduct("Морковь");
        var shelf = context.AddShelf();
        context.AddBalance(product.Id, shelf.Id, 6);

        var balances = await context.Stock.GetBalancesByShelfAsync(shelf.Id);

        Check.Equal("Морковь", balances.Single().ProductName);
    }

    [TestMethod]
    public async Task RemoveZeroBalance_RemovesRecord()
    {
        var context = new TestContext();
        var product = context.AddProduct();
        var shelf = context.AddShelf();
        var balance = context.AddBalance(product.Id, shelf.Id, 0);

        await context.Stock.RemoveZeroBalanceAsync(balance.Id);

        Check.Equal(0, context.Balances.Items.Count);
    }

    [TestMethod]
    public async Task RemoveZeroBalance_NonZero_Throws()
    {
        var context = new TestContext();
        var product = context.AddProduct();
        var shelf = context.AddShelf();
        var balance = context.AddBalance(product.Id, shelf.Id, 3);

        await Check.ThrowsAsync<BusinessException>(
            () => context.Stock.RemoveZeroBalanceAsync(balance.Id),
            "ненулевой остаток");
    }

    // Ожидаем: попадает только товар с остатком ниже минимума.
    [TestMethod]
    public async Task GetLowStock_ReturnsOnlyDeficit()
    {
        var context = new TestContext();
        var low = context.AddProduct("Морковь", min: 10, target: 20);
        var normal = context.AddProduct("Свёкла", min: 5, target: 10);
        var shelf = context.AddShelf();
        context.AddBalance(low.Id, shelf.Id, 3);
        context.AddBalance(normal.Id, shelf.Id, 8);

        var items = await context.Stock.GetLowStockProductsAsync();

        Check.Equal(1, items.Count);
        Check.Equal(low.Id, items[0].ProductId);
    }

    // Проверяем: расчёт рекомендуемого количества при заданном целевом остатке.
    [TestMethod]
    public async Task GetLowStock_ComputesRecommendedFromTarget()
    {
        var context = new TestContext();
        var product = context.AddProduct("Морковь", min: 10, target: 20);
        var shelf = context.AddShelf();
        context.AddBalance(product.Id, shelf.Id, 3);

        var items = await context.Stock.GetLowStockProductsAsync();

        Check.Equal(17, items[0].RecommendedOrderQuantity);
    }

    // Проверяем: расчёт рекомендуемого количества без целевого остатка.
    // Ожидаем: целевой уровень равен удвоенному минимуму, то есть 20 при минимуме 10.
    [TestMethod]
    public async Task GetLowStock_TargetDefaultsToDoubleMinimum()
    {
        var context = new TestContext();
        var product = context.AddProduct("Морковь", min: 10);
        var shelf = context.AddShelf();
        context.AddBalance(product.Id, shelf.Id, 0);

        var items = await context.Stock.GetLowStockProductsAsync();

        Check.Equal(20, items[0].RecommendedOrderQuantity);
    }

    // Проверяем: неактивные товары и товары без минимума не попадают в дефицит.
    [TestMethod]
    public async Task GetLowStock_IgnoresInactiveAndZeroMinimum()
    {
        var context = new TestContext();
        context.AddProduct("Неактивный", min: 10, active: false);
        context.AddProduct("Без минимума", min: 0);

        var items = await context.Stock.GetLowStockProductsAsync();

        Check.Equal(0, items.Count);
    }

    // Проверяем: сортировку списка дефицита по названию товара.
    [TestMethod]
    public async Task GetLowStock_SortsByName()
    {
        var context = new TestContext();
        context.AddProduct("Морковь", min: 10);
        context.AddProduct("Абрикос", min: 10);

        var items = await context.Stock.GetLowStockProductsAsync();

        Check.Equal("Абрикос", items[0].ProductName);
    }

    // Ожидаем: тип, ссылка на документ и дата заполнены.
    [TestMethod]
    public async Task Receive_Movement_HasTypeAndSource()
    {
        var context = new TestContext();
        var product = context.AddProduct();
        var shelf = context.AddShelf();
        var documentId = Guid.NewGuid();

        await context.Stock.ReceiveAsync(product.Id, shelf.Id, 2, StockMovementType.PurchaseReceipt,
            DocumentType.GoodsReceipt, documentId, null, "поступление");

        var movement = context.Movements.Items.Single();
        Check.Equal(StockMovementType.PurchaseReceipt, movement.Type);
        Check.Equal(DocumentType.GoodsReceipt, movement.SourceType);
        Check.Equal(documentId, movement.SourceId);
        Check.True(movement.OccurredAtUtc > DateTimeOffset.UtcNow.AddMinutes(-1),
            "дата движения должна быть свежей");
    }
}
