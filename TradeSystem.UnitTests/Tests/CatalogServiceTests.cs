// TradeSystem.UnitTests/Tests/CatalogServiceTests.cs

using System;
using System.Linq;
using System.Threading.Tasks;
using TradeSystem.Application.Dto;
using TradeSystem.Application.Exceptions;
using TradeSystem.Domain.Entities;
using TradeSystem.Domain.Enums;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TradeSystem.UnitTests.Tests;

/// <summary>Проверки справочников: валидация, фильтрация и запреты удаления.</summary>
[TestClass]
public sealed class CatalogServiceTests
{
    private static SupplierInput Supplier(string name = "ООО Ромашка", string phone = "+7 383 111-22-33")
        => new() { Name = name, Phone = phone };

    private static ProductInput Product(string name = "Морковь", int min = 0, int? target = null,
        MeasurementUnit unit = MeasurementUnit.Kilogram)
        => new() { Name = name, Article = "ART-1", Unit = unit, MinimumStockQuantity = min, TargetStockQuantity = target };

    private static ShelfInput Shelf(string code = "A-1", string name = "Полка A-1")
        => new() { Code = code, Name = name };

    // Проверяем: создание поставщика с пустым названием.
    // Ожидаем: исключение с текстом «Название поставщика обязательно».
    [TestMethod]
    public async Task CreateSupplier_EmptyName_Throws()
    {
        var context = new TestContext();

        await Check.ThrowsAsync<BusinessException>(
            () => context.Catalog.CreateSupplierAsync(Supplier(name: " ")),
            "Название поставщика");
    }

    // Проверяем: создание поставщика с пустым телефоном.
    // Ожидаем: исключение с текстом «Телефон поставщика обязателен».
    [TestMethod]
    public async Task CreateSupplier_EmptyPhone_Throws()
    {
        var context = new TestContext();

        await Check.ThrowsAsync<BusinessException>(
            () => context.Catalog.CreateSupplierAsync(Supplier(phone: "")),
            "Телефон поставщика");
    }

    // Ожидаем: возвращается DTO с тем же названием, запись сохранена.
    [TestMethod]
    public async Task CreateSupplier_Valid_ReturnsDto()
    {
        var context = new TestContext();

        var dto = await context.Catalog.CreateSupplierAsync(Supplier());

        Check.Equal("ООО Ромашка", dto.Name);
        Check.Equal(1, context.Suppliers.Items.Count);
    }

    // Проверяем: обновление поставщика без идентификатора.
    // Ожидаем: исключение с текстом «Не указан Id поставщика».
    [TestMethod]
    public async Task UpdateSupplier_WithoutId_Throws()
    {
        var context = new TestContext();
        var input = Supplier();
        input.Id = null;

        await Check.ThrowsAsync<BusinessException>(
            () => context.Catalog.UpdateSupplierAsync(input),
            "Не указан Id поставщика");
    }

    // Ожидаем: исключение «Поставщик не найден».
    [TestMethod]
    public async Task UpdateSupplier_Unknown_Throws()
    {
        var context = new TestContext();
        var input = Supplier();
        input.Id = Guid.NewGuid();

        await Check.ThrowsAsync<BusinessException>(
            () => context.Catalog.UpdateSupplierAsync(input),
            "Поставщик не найден");
    }

    // Проверяем: удаление поставщика, у которого есть связи с товарами.
    [TestMethod]
    public async Task DeleteSupplier_WithLinks_Throws()
    {
        var context = new TestContext();
        var supplier = context.AddSupplier();
        var product = context.AddProduct();
        context.AddLink(product.Id, supplier.Id);

        await Check.ThrowsAsync<BusinessException>(
            () => context.Catalog.DeleteSupplierAsync(supplier.Id),
            "связанные товары");
    }

    // Проверяем: удаление поставщика, у которого есть заказы.
    [TestMethod]
    public async Task DeleteSupplier_WithOrders_Throws()
    {
        var context = new TestContext();
        var supplier = context.AddSupplier();
        context.AddOrder(supplier.Id);

        await Check.ThrowsAsync<BusinessException>(
            () => context.Catalog.DeleteSupplierAsync(supplier.Id),
            "есть заказы");
    }

    // Проверяем: удаление поставщика, у которого есть поступления.
    [TestMethod]
    public async Task DeleteSupplier_WithReceipts_Throws()
    {
        var context = new TestContext();
        var supplier = context.AddSupplier();
        context.Receipts.Seed(new GoodsReceipt { SupplierId = supplier.Id });

        await Check.ThrowsAsync<BusinessException>(
            () => context.Catalog.DeleteSupplierAsync(supplier.Id),
            "есть поступления");
    }

    [TestMethod]
    public async Task DeleteSupplier_WithoutReferences_Removes()
    {
        var context = new TestContext();
        var supplier = context.AddSupplier();

        await context.Catalog.DeleteSupplierAsync(supplier.Id);

        Check.Equal(0, context.Suppliers.Items.Count);
    }

    // Проверяем: список поставщиков по умолчанию.
    [TestMethod]
    public async Task GetSuppliers_HidesInactiveByDefault()
    {
        var context = new TestContext();
        context.AddSupplier("Активный");
        context.AddSupplier("Скрытый", active: false);

        var items = await context.Catalog.GetSuppliersAsync();

        Check.Equal(1, items.Count);
        Check.Equal("Активный", items[0].Name);
    }

    [TestMethod]
    public async Task GetSuppliers_IncludeInactive_ReturnsAll()
    {
        var context = new TestContext();
        context.AddSupplier("Активный");
        context.AddSupplier("Скрытый", active: false);

        var items = await context.Catalog.GetSuppliersAsync(includeInactive: true);

        Check.Equal(2, items.Count);
    }

    [TestMethod]
    public async Task CreateProduct_EmptyName_Throws()
    {
        var context = new TestContext();

        await Check.ThrowsAsync<BusinessException>(
            () => context.Catalog.CreateProductAsync(Product(name: "")),
            "Наименование товара");
    }

    // Проверяем: создание товара с отрицательным минимальным остатком.
    [TestMethod]
    public async Task CreateProduct_NegativeMinimum_Throws()
    {
        var context = new TestContext();

        await Check.ThrowsAsync<BusinessException>(
            () => context.Catalog.CreateProductAsync(Product(min: -1)),
            "Минимальный остаток");
    }

    // Проверяем: создание товара с отрицательным целевым остатком.
    [TestMethod]
    public async Task CreateProduct_NegativeTarget_Throws()
    {
        var context = new TestContext();

        await Check.ThrowsAsync<BusinessException>(
            () => context.Catalog.CreateProductAsync(Product(target: -5)),
            "Целевой остаток");
    }

    // Ожидаем: DTO содержит название и единицу измерения в виде текста.
    [TestMethod]
    public async Task CreateProduct_Valid_ReturnsDto()
    {
        var context = new TestContext();

        var dto = await context.Catalog.CreateProductAsync(Product());

        Check.Equal("Морковь", dto.Name);
        Check.Equal("кг", dto.UnitLabel);
        Check.Equal(1, context.Products.Items.Count);
    }

    [TestMethod]
    public async Task DeleteProduct_WithLinks_Throws()
    {
        var context = new TestContext();
        var supplier = context.AddSupplier();
        var product = context.AddProduct();
        context.AddLink(product.Id, supplier.Id);

        await Check.ThrowsAsync<BusinessException>(
            () => context.Catalog.DeleteProductAsync(product.Id),
            "связи с поставщиками");
    }

    [TestMethod]
    public async Task DeleteProduct_WithStock_Throws()
    {
        var context = new TestContext();
        var product = context.AddProduct();
        var shelf = context.AddShelf();
        context.AddBalance(product.Id, shelf.Id, 5);

        await Check.ThrowsAsync<BusinessException>(
            () => context.Catalog.DeleteProductAsync(product.Id),
            "ненулевые остатки");
    }

    // Проверяем: удаление товара, входящего в заказ.
    [TestMethod]
    public async Task DeleteProduct_InOrder_Throws()
    {
        var context = new TestContext();
        var supplier = context.AddSupplier();
        var product = context.AddProduct();
        var order = context.AddOrder(supplier.Id);
        context.AddOrderLine(order, product, 3);

        await Check.ThrowsAsync<BusinessException>(
            () => context.Catalog.DeleteProductAsync(product.Id),
            "входит в заказы");
    }

    // Проверяем: удаление товара, по которому есть движения.
    [TestMethod]
    public async Task DeleteProduct_WithMovements_Throws()
    {
        var context = new TestContext();
        var product = context.AddProduct();
        var shelf = context.AddShelf();
        await context.Stock.ReceiveAsync(product.Id, shelf.Id, 1, StockMovementType.Correction,
            DocumentType.InventoryCorrection, null, null, null);
        await context.Stock.ShipAsync(product.Id, shelf.Id, 1, StockMovementType.Correction,
            DocumentType.InventoryCorrection, null, null, null);

        await Check.ThrowsAsync<BusinessException>(
            () => context.Catalog.DeleteProductAsync(product.Id),
            "движения товара");
    }

    [TestMethod]
    public async Task DeleteProduct_WithoutReferences_Removes()
    {
        var context = new TestContext();
        var product = context.AddProduct();

        await context.Catalog.DeleteProductAsync(product.Id);

        Check.Equal(0, context.Products.Items.Count);
    }

    [TestMethod]
    public async Task CreateShelf_EmptyCode_Throws()
    {
        var context = new TestContext();

        await Check.ThrowsAsync<BusinessException>(
            () => context.Catalog.CreateShelfAsync(Shelf(code: "")),
            "Код полки");
    }

    [TestMethod]
    public async Task CreateShelf_EmptyName_Throws()
    {
        var context = new TestContext();

        await Check.ThrowsAsync<BusinessException>(
            () => context.Catalog.CreateShelfAsync(Shelf(name: " ")),
            "Название полки");
    }

    [TestMethod]
    public async Task CreateShelf_Valid_ReturnsDto()
    {
        var context = new TestContext();

        var dto = await context.Catalog.CreateShelfAsync(Shelf());

        Check.Equal("A-1", dto.Code);
        Check.Equal(1, context.Shelves.Items.Count);
    }

    // Проверяем: удаление полки, на которой есть товар.
    [TestMethod]
    public async Task DeleteShelf_WithStock_Throws()
    {
        var context = new TestContext();
        var product = context.AddProduct();
        var shelf = context.AddShelf();
        context.AddBalance(product.Id, shelf.Id, 2);

        await Check.ThrowsAsync<BusinessException>(
            () => context.Catalog.DeleteShelfAsync(shelf.Id),
            "на ней есть товар");
    }

    // Ожидаем: исключение «Товар не найден».
    [TestMethod]
    public async Task LinkProduct_UnknownProduct_Throws()
    {
        var context = new TestContext();
        var supplier = context.AddSupplier();

        await Check.ThrowsAsync<BusinessException>(
            () => context.Catalog.LinkProductToSupplierAsync(new SupplierProductInput
            {
                ProductId = Guid.NewGuid(),
                SupplierId = supplier.Id
            }),
            "Товар не найден");
    }

    // Ожидаем: исключение «Поставщик не найден».
    [TestMethod]
    public async Task LinkProduct_UnknownSupplier_Throws()
    {
        var context = new TestContext();
        var product = context.AddProduct();

        await Check.ThrowsAsync<BusinessException>(
            () => context.Catalog.LinkProductToSupplierAsync(new SupplierProductInput
            {
                ProductId = product.Id,
                SupplierId = Guid.NewGuid()
            }),
            "Поставщик не найден");
    }

    // Проверяем: повторное связывание той же пары товар и поставщик.
    [TestMethod]
    public async Task LinkProduct_Duplicate_Throws()
    {
        var context = new TestContext();
        var supplier = context.AddSupplier();
        var product = context.AddProduct();
        var input = new SupplierProductInput { ProductId = product.Id, SupplierId = supplier.Id };
        await context.Catalog.LinkProductToSupplierAsync(input);

        await Check.ThrowsAsync<BusinessException>(
            () => context.Catalog.LinkProductToSupplierAsync(input),
            "уже существует");
    }

    // Проверяем: корректное связывание товара и поставщика.
    // Ожидаем: DTO содержит цену и названия сторон.
    [TestMethod]
    public async Task LinkProduct_Valid_ReturnsDto()
    {
        var context = new TestContext();
        var supplier = context.AddSupplier();
        var product = context.AddProduct();

        var dto = await context.Catalog.LinkProductToSupplierAsync(new SupplierProductInput
        {
            ProductId = product.Id,
            SupplierId = supplier.Id,
            PurchasePrice = 120m,
            OrderMultiple = 5
        });

        Check.Equal("Морковь", dto.ProductName);
        Check.Equal("ООО Ромашка", dto.SupplierName);
        Check.Equal(120m, dto.PurchasePrice);
    }

    // Проверяем: число активных поставщиков в DTO товара.
    // Ожидаем: у товара с двумя связями поле SuppliersCount равно двум.
    [TestMethod]
    public async Task GetProducts_CountsActiveSuppliers()
    {
        var context = new TestContext();
        var product = context.AddProduct();
        var first = context.AddSupplier("Первый");
        var second = context.AddSupplier("Второй");
        context.AddLink(product.Id, first.Id);
        context.AddLink(product.Id, second.Id);

        var items = await context.Catalog.GetProductsAsync();

        Check.Equal(2, items.Single().SuppliersCount);
    }
}
