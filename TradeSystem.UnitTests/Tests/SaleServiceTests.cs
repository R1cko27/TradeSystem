// TradeSystem.UnitTests/Tests/SaleServiceTests.cs

using System;
using System.Linq;
using System.Threading.Tasks;
using TradeSystem.Application.Dto;
using TradeSystem.Application.Exceptions;
using TradeSystem.Domain.Entities;
using TradeSystem.Domain.Enums;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TradeSystem.UnitTests.Tests;

// Проверки продаж: черновик, проведение, списание остатка.
[TestClass]
public sealed class SaleServiceTests
{
    // Готовит входные данные продажи.
    private static SaleInput Sale(Guid productId, Guid shelfId, int quantity = 2, decimal price = 30m)
        => new()
        {
            Lines = new()
            {
                new SaleLineInput { ProductId = productId, ShelfId = shelfId, Quantity = quantity, UnitPrice = price }
            }
        };
// Ожидаем: исключение об отсутствии позиций.

    [TestMethod]
    public async Task CreateDraft_WithoutLines_Throws()
    {
        var context = new TestContext();

        await Check.ThrowsAsync<BusinessException>(
            () => context.SaleService.CreateDraftAsync(new SaleInput()),
            "не содержит позиций");
    }
// Ожидаем: исключение о количестве.

    [TestMethod]
    public async Task CreateDraft_ZeroQuantity_Throws()
    {
        var context = new TestContext();
        var product = context.AddProduct();
        var shelf = context.AddShelf();

        await Check.ThrowsAsync<BusinessException>(
            () => context.SaleService.CreateDraftAsync(Sale(product.Id, shelf.Id, 0)),
            "Количество");
    }

    // Ожидаем: исключение «Товар не найден».
    [TestMethod]
    public async Task CreateDraft_UnknownProduct_Throws()
    {
        var context = new TestContext();
        var shelf = context.AddShelf();

        await Check.ThrowsAsync<BusinessException>(
            () => context.SaleService.CreateDraftAsync(Sale(Guid.NewGuid(), shelf.Id)),
            "Товар не найден");
    }
// Ожидаем: исключение «Полка не найдена».

    [TestMethod]
    public async Task CreateDraft_UnknownShelf_Throws()
    {
        var context = new TestContext();
        var product = context.AddProduct();

        await Check.ThrowsAsync<BusinessException>(
            () => context.SaleService.CreateDraftAsync(Sale(product.Id, Guid.NewGuid())),
            "Полка не найдена");
    }

    // Ожидаем: статус «Черновик», сумма по строкам и номер вида SALE-ГГГГММДД-НННН.
    [TestMethod]
    public async Task CreateDraft_Valid_ReturnsDraft()
    {
        var context = new TestContext();
        var product = context.AddProduct();
        var shelf = context.AddShelf();

        var dto = await context.SaleService.CreateDraftAsync(Sale(product.Id, shelf.Id, 2, 30m));

        Check.Equal(SaleStatus.Draft, dto.Status);
        Check.Equal(60m, dto.TotalAmount);
        Check.True(dto.Number.StartsWith("SALE-"), $"неверный номер продажи: {dto.Number}");
    }

    // Проверяем: проведение продажи при достаточном остатке.
    // Ожидаем: остаток уменьшается, статус «Завершена», движение с отрицательной разностью.
    [TestMethod]
    public async Task Post_DecreasesStockAndCompletes()
    {
        var context = new TestContext();
        var product = context.AddProduct();
        var shelf = context.AddShelf();
        context.AddBalance(product.Id, shelf.Id, 10);
        var sale = await context.SaleService.CreateDraftAsync(Sale(product.Id, shelf.Id, 4));

        var dto = await context.SaleService.PostAsync(sale.Id);

        Check.Equal(SaleStatus.Completed, dto.Status);
        Check.Equal(6, context.Balances.Items.Single().Quantity);
        Check.Equal(-4, context.Movements.Items.Single().QuantityDelta);
    }

    // Проверяем: проведение продажи при нехватке товара.
    // Ожидаем: исключение о недостатке и сохранение статуса «Черновик».
    [TestMethod]
    public async Task Post_InsufficientStock_ThrowsAndKeepsDraft()
    {
        var context = new TestContext();
        var product = context.AddProduct();
        var shelf = context.AddShelf();
        context.AddBalance(product.Id, shelf.Id, 1);
        var sale = await context.SaleService.CreateDraftAsync(Sale(product.Id, shelf.Id, 5));

        await Check.ThrowsAsync<BusinessException>(
            () => context.SaleService.PostAsync(sale.Id),
            "Недостаточно");

        Check.Equal(SaleStatus.Draft, context.Sales.Items.Single().Status);
    }
// Ожидаем: исключение для не-черновика.

    [TestMethod]
    public async Task Post_NonDraft_Throws()
    {
        var context = new TestContext();
        var sale = context.Sales.Seed(new Sale { Number = "SALE-1", Status = SaleStatus.Completed });

        await Check.ThrowsAsync<BusinessException>(
            () => context.SaleService.PostAsync(sale.Id),
            "Черновик");
    }
// Ожидаем: завершённую продажу нельзя удалить.

    [TestMethod]
    public async Task DeleteDraft_Completed_Throws()
    {
        var context = new TestContext();
        var sale = context.Sales.Seed(new Sale { Number = "SALE-1", Status = SaleStatus.Completed });

        await Check.ThrowsAsync<BusinessException>(
            () => context.SaleService.DeleteDraftAsync(sale.Id),
            "Черновик");
    }
}
