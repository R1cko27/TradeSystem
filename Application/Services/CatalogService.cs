// Application/Services/CatalogService.cs

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TradeSystem.Application.Contracts.Repositories;
using TradeSystem.Application.Contracts.Services;
using TradeSystem.Application.Dto;
using TradeSystem.Application.Exceptions;
using TradeSystem.Application.Mappers;
using TradeSystem.Domain.Entities;

namespace TradeSystem.Application.Contracts.Services
{
    public interface ICatalogService
    {
        // Поставщики с числом активных товаров, по алфавиту.
        Task<IReadOnlyList<SupplierDto>> GetSuppliersAsync(bool includeInactive = false, CancellationToken ct = default);
        Task<SupplierDto?> GetSupplierAsync(Guid id, CancellationToken ct = default);
        Task<SupplierDto> CreateSupplierAsync(SupplierInput input, CancellationToken ct = default);
        Task<SupplierDto> UpdateSupplierAsync(SupplierInput input, CancellationToken ct = default);
        Task DeleteSupplierAsync(Guid id, CancellationToken ct = default);

        // Товары с остатками, числом поставщиков и полкой по умолчанию.
        Task<IReadOnlyList<ProductDto>> GetProductsAsync(bool includeInactive = false, CancellationToken ct = default);
        Task<ProductDto?> GetProductAsync(Guid id, CancellationToken ct = default);
        Task<ProductDto> CreateProductAsync(ProductInput input, CancellationToken ct = default);
        Task<ProductDto> UpdateProductAsync(ProductInput input, CancellationToken ct = default);
        Task DeleteProductAsync(Guid id, CancellationToken ct = default);

        // Полки с числом товаров на них.
        Task<IReadOnlyList<ShelfDto>> GetShelvesAsync(bool includeInactive = false, CancellationToken ct = default);
        Task<ShelfDto?> GetShelfAsync(Guid id, CancellationToken ct = default);

        Task<ShelfDto> CreateShelfAsync(ShelfInput input, CancellationToken ct = default);
        Task<ShelfDto> UpdateShelfAsync(ShelfInput input, CancellationToken ct = default);
        Task DeleteShelfAsync(Guid id, CancellationToken ct = default);

        // Все связи «товар — поставщик» с названиями сторон.
        Task<IReadOnlyList<SupplierProductDto>> GetSupplierProductsAsync(CancellationToken ct = default);
        Task<IReadOnlyList<SupplierProductDto>> GetSupplierProductsBySupplierAsync(Guid supplierId, CancellationToken ct = default);
        Task<IReadOnlyList<SupplierProductDto>> GetSupplierProductsByProductAsync(Guid productId, CancellationToken ct = default);
        Task<SupplierProductDto> LinkProductToSupplierAsync(SupplierProductInput input, CancellationToken ct = default);
        Task<SupplierProductDto> UpdateLinkAsync(SupplierProductInput input, CancellationToken ct = default);
        Task UnlinkAsync(Guid id, CancellationToken ct = default);
    }
}

namespace TradeSystem.Application.Services
{
    public sealed class CatalogService : ICatalogService
    {
        private readonly DtoMapper _mapper; // Преобразование сущностей в DTO
        private readonly ISupplierRepository _suppliers; // Репозиторий поставщиков
        private readonly IProductRepository _products; // Репозиторий товаров
        private readonly IShelfRepository _shelves; // Репозиторий полок
        private readonly ISupplierProductRepository _supplierProducts; // Репозиторий связей товара и поставщика
        private readonly IPurchaseOrderRepository _orders; // Репозиторий заказов
        private readonly IGoodsReceiptRepository _receipts; // Репозиторий поступлений
        private readonly ISaleRepository _sales; // Репозиторий продаж
        private readonly IStockMovementRepository _movements; // Репозиторий движений товара
        private readonly IStockService _stock; // Складской сервис

        public CatalogService(
            ISupplierRepository suppliers,
            IProductRepository products,
            IShelfRepository shelves,
            ISupplierProductRepository supplierProducts,
            IPurchaseOrderRepository orders,
            IGoodsReceiptRepository receipts,
            ISaleRepository sales,
            IStockMovementRepository movements,
            IStockService stock,
            DtoMapper mapper)
        {
            _suppliers = suppliers;
            _products = products;
            _shelves = shelves;
            _supplierProducts = supplierProducts;
            _orders = orders;
            _receipts = receipts;
            _sales = sales;
            _movements = movements;
            _stock = stock;
                    _mapper = mapper;
        }

        // Поставщики с числом активных связей, по алфавиту.
        public async Task<IReadOnlyList<SupplierDto>> GetSuppliersAsync(bool includeInactive = false, CancellationToken ct = default)
        {
            var all = await _suppliers.GetAllAsync(ct);
            var links = await _supplierProducts.GetAllAsync(ct);

            var counts = links
                .Where(l => l.IsActive)
                .GroupBy(l => l.SupplierId)
                .ToDictionary(g => g.Key, g => g.Count());

            return all
                .Where(s => includeInactive || s.IsActive)
                .OrderBy(s => s.Name, StringComparer.CurrentCultureIgnoreCase)
                .Select(s => _mapper.ToDto(s, counts.GetValueOrDefault(s.Id)))
                .ToList();
        }

        // Один поставщик с числом активных связей.
        public async Task<SupplierDto?> GetSupplierAsync(Guid id, CancellationToken ct = default)
        {
            var s = await _suppliers.GetByIdAsync(id, ct);
            if (s is null) return null;

            var links = await _supplierProducts.GetAllAsync(ct);
            var count = links.Count(l => l.SupplierId == id && l.IsActive);
            return _mapper.ToDto(s, count);
        }

        public Task<SupplierDto> CreateSupplierAsync(SupplierInput input, CancellationToken ct = default)
            => SaveSupplierAsync(input, isNew: true, ct);

        public Task<SupplierDto> UpdateSupplierAsync(SupplierInput input, CancellationToken ct = default)
            => SaveSupplierAsync(input, isNew: false, ct);

        // Общая логика создания и обновления поставщика.
        private async Task<SupplierDto> SaveSupplierAsync(SupplierInput input, bool isNew, CancellationToken ct)
        {
            ValidateSupplier(input);

            Supplier entity;
            if (isNew)
            {
                entity = new Supplier();
            }
            else
            {
                if (input.Id is null || input.Id == Guid.Empty)
                    throw new BusinessException("Не указан Id поставщика.");
                entity = await _suppliers.GetByIdAsync(input.Id.Value, ct)
                         ?? throw new BusinessException("Поставщик не найден.");
            }

            entity.Name = input.Name.Trim();
            entity.TaxId = input.TaxId;
            entity.Address = input.Address ?? new();
            entity.Phone = input.Phone.Trim();
            entity.Email = input.Email;
            entity.IsActive = input.IsActive;
            entity.Note = input.Note;

            if (isNew) await _suppliers.AddAsync(entity, ct);
            else       await _suppliers.UpdateAsync(entity, ct);

            return (await GetSupplierAsync(entity.Id, ct))!;
        }

        // Удаление запрещено, если поставщик используется в справочнике или документах.
        public async Task DeleteSupplierAsync(Guid id, CancellationToken ct = default)
        {
            var links = await _supplierProducts.GetAllAsync(ct);
            if (links.Any(l => l.SupplierId == id))
                throw new BusinessException("Нельзя удалить поставщика: есть связанные товары. Сначала удалите связи.");

            // Поставщик фигурирует в документах — удаление оставило бы ссылки без адресата.
            if ((await _orders.GetAllAsync(ct)).Any(o => o.SupplierId == id))
                throw new BusinessException("Нельзя удалить поставщика: по нему есть заказы. Снимите признак активности.");

            if ((await _receipts.GetAllAsync(ct)).Any(r => r.SupplierId == id))
                throw new BusinessException("Нельзя удалить поставщика: по нему есть поступления. Снимите признак активности.");

            await _suppliers.DeleteAsync(id, ct);
        }

        // Проверяет обязательные поля поставщика.
        private static void ValidateSupplier(SupplierInput input)
        {
            if (string.IsNullOrWhiteSpace(input.Name))
                throw new BusinessException("Название поставщика обязательно.");
            if (string.IsNullOrWhiteSpace(input.Phone))
                throw new BusinessException("Телефон поставщика обязателен.");
        }

        // Товары с вычисленными остатками; по умолчанию без неактивных.
        public async Task<IReadOnlyList<ProductDto>> GetProductsAsync(bool includeInactive = false, CancellationToken ct = default)
        {
            var all = await _products.GetAllAsync(ct);
            if (!includeInactive) all = all.Where(p => p.IsActive).ToList();

            return await BuildProductDtosAsync(all.ToList(), ct);
        }

        // Один товар с вычисленными остатками.
        public async Task<ProductDto?> GetProductAsync(Guid id, CancellationToken ct = default)
        {
            var p = await _products.GetByIdAsync(id, ct);
            if (p is null) return null;
            var list = await BuildProductDtosAsync(new List<Product> { p }, ct);
            return list.FirstOrDefault();
        }

        public Task<ProductDto> CreateProductAsync(ProductInput input, CancellationToken ct = default)
            => SaveProductAsync(input, isNew: true, ct);

        public Task<ProductDto> UpdateProductAsync(ProductInput input, CancellationToken ct = default)
            => SaveProductAsync(input, isNew: false, ct);

        // Общая логика создания и обновления товара.
        private async Task<ProductDto> SaveProductAsync(ProductInput input, bool isNew, CancellationToken ct)
        {
            ValidateProduct(input);

            Product entity;
            if (isNew)
            {
                entity = new Product();
            }
            else
            {
                if (input.Id is null || input.Id == Guid.Empty)
                    throw new BusinessException("Не указан Id товара.");
                entity = await _products.GetByIdAsync(input.Id.Value, ct)
                         ?? throw new BusinessException("Товар не найден.");
            }

            entity.Name = input.Name.Trim();
            entity.Article = input.Article.Trim();
            entity.Unit = input.Unit;
            entity.MinimumStockQuantity = input.MinimumStockQuantity;
            entity.TargetStockQuantity = input.TargetStockQuantity;
            entity.IsActive = input.IsActive;
            entity.Description = input.Description;
            entity.DefaultShelfId = input.DefaultShelfId;

            if (isNew) await _products.AddAsync(entity, ct);
            else       await _products.UpdateAsync(entity, ct);

            var dtos = await BuildProductDtosAsync(new List<Product> { entity }, ct);
            return dtos.First();
        }

        // Удаление запрещено, если товар используется в связях, остатках или документах.
        public async Task DeleteProductAsync(Guid id, CancellationToken ct = default)
        {
            var links = await _supplierProducts.GetAllAsync(ct);
            if (links.Any(l => l.ProductId == id))
                throw new BusinessException("Нельзя удалить товар: есть связи с поставщиками.");

            var balances = await _stock.GetBalancesByProductAsync(id, ct);
            if (balances.Any(b => b.Quantity != 0))
                throw new BusinessException("Нельзя удалить товар: по нему есть ненулевые остатки.");

            // Товар присутствует в документах и движениях — история должна остаться целой.
            if ((await _orders.GetAllAsync(ct)).Any(o => o.Lines.Any(l => l.ProductId == id)))
                throw new BusinessException("Нельзя удалить товар: он входит в заказы поставщикам. Снимите признак активности.");

            if ((await _receipts.GetAllAsync(ct)).Any(r => r.Lines.Any(l => l.ProductId == id)))
                throw new BusinessException("Нельзя удалить товар: он входит в поступления. Снимите признак активности.");

            if ((await _sales.GetAllAsync(ct)).Any(s => s.Lines.Any(l => l.ProductId == id)))
                throw new BusinessException("Нельзя удалить товар: он входит в продажи. Снимите признак активности.");

            if ((await _movements.GetAllAsync(ct)).Any(m => m.ProductId == id))
                throw new BusinessException("Нельзя удалить товар: по нему есть движения товара. Снимите признак активности.");

            await _products.DeleteAsync(id, ct);
        }

        // Проверяет обязательные поля и неотрицательные остатки товара.
        private static void ValidateProduct(ProductInput input)
        {
            if (string.IsNullOrWhiteSpace(input.Name))
                throw new BusinessException("Наименование товара обязательно.");
            if (input.MinimumStockQuantity < 0)
                throw new BusinessException("Минимальный остаток не может быть отрицательным.");
            if (input.TargetStockQuantity is < 0)
                throw new BusinessException("Целевой остаток не может быть отрицательным.");
        }

        // Полки с числом различных товаров на них.
        public async Task<IReadOnlyList<ShelfDto>> GetShelvesAsync(bool includeInactive = false, CancellationToken ct = default)
        {
            var all = await _shelves.GetAllAsync(ct);

            // Собираем все балансы через StockService (он умеет отдавать сырые данные для подсчёта).
            var raw = await _stock.GetAllBalancesRawAsync(ct);
            var perShelf = raw
                .Where(b => b.Quantity != 0)
                .GroupBy(b => b.ShelfId)
                .ToDictionary(g => g.Key, g => g.Select(b => b.ProductId).Distinct().Count());

            return all
                .Where(s => includeInactive || s.IsActive)
                .OrderBy(s => s.Code, StringComparer.CurrentCultureIgnoreCase)
                .Select(s => _mapper.ToDto(s, perShelf.GetValueOrDefault(s.Id)))
                .ToList();
        }
        // Одна полка с числом товаров.
        public async Task<ShelfDto?> GetShelfAsync(Guid id, CancellationToken ct = default)
        {
            var sh = await _shelves.GetByIdAsync(id, ct);
            if (sh is null) return null;

            var raw = await _stock.GetAllBalancesRawAsync(ct);
            var count = raw
                .Where(b => b.Quantity != 0 && b.ShelfId == id)
                .Select(b => b.ProductId)
                .Distinct()
                .Count();

            return _mapper.ToDto(sh, count);
        }

        public Task<ShelfDto> CreateShelfAsync(ShelfInput input, CancellationToken ct = default)
            => SaveShelfAsync(input, isNew: true, ct);

        public Task<ShelfDto> UpdateShelfAsync(ShelfInput input, CancellationToken ct = default)
            => SaveShelfAsync(input, isNew: false, ct);

        // Общая логика создания и обновления полки.
        private async Task<ShelfDto> SaveShelfAsync(ShelfInput input, bool isNew, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(input.Code))
                throw new BusinessException("Код полки обязателен.");
            if (string.IsNullOrWhiteSpace(input.Name))
                throw new BusinessException("Название полки обязательно.");

            Shelf entity;
            if (isNew)
            {
                entity = new Shelf();
            }
            else
            {
                if (input.Id is null || input.Id == Guid.Empty)
                    throw new BusinessException("Не указан Id полки.");
                entity = await _shelves.GetByIdAsync(input.Id.Value, ct)
                         ?? throw new BusinessException("Полка не найдена.");
            }

            entity.Code = input.Code.Trim();
            entity.Name = input.Name.Trim();
            entity.Location = input.Location;
            entity.IsActive = input.IsActive;
            entity.Note = input.Note;

            if (isNew) await _shelves.AddAsync(entity, ct);
            else       await _shelves.UpdateAsync(entity, ct);

            var all = await _shelves.GetAllAsync(ct);
            var me = all.First(s => s.Id == entity.Id);

            return await BuildShelfDtoAsync(me, ct);
        }

        // Собирает DTO полки с числом товаров.
        private async Task<ShelfDto> BuildShelfDtoAsync(Shelf me, CancellationToken ct)
        {
            var raw = await _stock.GetAllBalancesRawAsync(ct);
            var count = raw.Where(b => b.Quantity != 0 && b.ShelfId == me.Id).Select(b => b.ProductId).Distinct().Count();
            return _mapper.ToDto(me, count);
        }

        // Удаление запрещено, если на полке есть товар или она используется в документах.
        public async Task DeleteShelfAsync(Guid id, CancellationToken ct = default)
        {
            var raw = await _stock.GetAllBalancesRawAsync(ct);
            var onShelf = raw.Where(b => b.ShelfId == id).ToList();
            if (onShelf.Any(b => b.Quantity != 0))
                throw new BusinessException("Нельзя удалить полку: на ней есть товар.");

            // Полка указана в документах, движениях или как полка по умолчанию у товара.
            if ((await _receipts.GetAllAsync(ct)).Any(r => r.Lines.Any(l => l.ShelfId == id)))
                throw new BusinessException("Нельзя удалить полку: она указана в поступлениях. Снимите признак активности.");

            if ((await _sales.GetAllAsync(ct)).Any(s => s.Lines.Any(l => l.ShelfId == id)))
                throw new BusinessException("Нельзя удалить полку: она указана в продажах. Снимите признак активности.");

            if ((await _movements.GetAllAsync(ct)).Any(m => m.ShelfId == id))
                throw new BusinessException("Нельзя удалить полку: по ней есть движения товара. Снимите признак активности.");

            if ((await _products.GetAllAsync(ct)).Any(p => p.DefaultShelfId == id))
                throw new BusinessException("Нельзя удалить полку: она назначена полкой по умолчанию. Снимите признак активности.");

            foreach (var b in onShelf)
                await _stock.RemoveZeroBalanceAsync(b.Id, ct);

            await _shelves.DeleteAsync(id, ct);
        }

        // Все связи с названиями товара и поставщика.
        public async Task<IReadOnlyList<SupplierProductDto>> GetSupplierProductsAsync(CancellationToken ct = default)
        {
            var links = await _supplierProducts.GetAllAsync(ct);
            return await BuildLinkDtosAsync(links.ToList(), ct);
        }

        // Связи выбранного поставщика.
        public async Task<IReadOnlyList<SupplierProductDto>> GetSupplierProductsBySupplierAsync(Guid supplierId, CancellationToken ct = default)
        {
            var links = (await _supplierProducts.GetAllAsync(ct)).Where(l => l.SupplierId == supplierId).ToList();
            return await BuildLinkDtosAsync(links, ct);
        }

        // Связи выбранного товара.
        public async Task<IReadOnlyList<SupplierProductDto>> GetSupplierProductsByProductAsync(Guid productId, CancellationToken ct = default)
        {
            var links = (await _supplierProducts.GetAllAsync(ct)).Where(l => l.ProductId == productId).ToList();
            return await BuildLinkDtosAsync(links, ct);
        }

        public Task<SupplierProductDto> LinkProductToSupplierAsync(SupplierProductInput input, CancellationToken ct = default)
            => SaveLinkAsync(input, isNew: true, ct);

        public Task<SupplierProductDto> UpdateLinkAsync(SupplierProductInput input, CancellationToken ct = default)
            => SaveLinkAsync(input, isNew: false, ct);

        // Создание или обновление связи; предпочтительной может быть только одна.
        private async Task<SupplierProductDto> SaveLinkAsync(SupplierProductInput input, bool isNew, CancellationToken ct)
        {
            if (input.ProductId == Guid.Empty) throw new BusinessException("Не указан товар.");
            if (input.SupplierId == Guid.Empty) throw new BusinessException("Не указан поставщик.");
            if (await _products.GetByIdAsync(input.ProductId, ct) is null) throw new BusinessException("Товар не найден.");
            if (await _suppliers.GetByIdAsync(input.SupplierId, ct) is null) throw new BusinessException("Поставщик не найден.");

            var existing = await _supplierProducts.GetAllAsync(ct);

            SupplierProduct entity;
            if (isNew)
            {
                if (existing.Any(l => l.ProductId == input.ProductId && l.SupplierId == input.SupplierId))
                    throw new BusinessException("Такая связь товар-поставщик уже существует.");
                entity = new SupplierProduct();
            }
            else
            {
                if (input.Id is null || input.Id == Guid.Empty)
                    throw new BusinessException("Не указан Id связи.");
                entity = existing.FirstOrDefault(l => l.Id == input.Id.Value)
                         ?? throw new BusinessException("Связь не найдена.");
            }

            entity.ProductId = input.ProductId;
            entity.SupplierId = input.SupplierId;
            entity.SupplierArticle = input.SupplierArticle;
            entity.PurchasePrice = input.PurchasePrice;
            entity.LeadTimeDays = input.LeadTimeDays;
            entity.MinimumOrderQuantity = input.MinimumOrderQuantity;
            entity.OrderMultiple = input.OrderMultiple;
            entity.IsPreferred = input.IsPreferred;
            entity.IsActive = input.IsActive;
            entity.Note = input.Note;

            // Если выставили предпочтительным — снимаем флаг с остальных по этому товару.
            if (entity.IsPreferred)
            {
                foreach (var other in existing.Where(l => l.ProductId == entity.ProductId && l.Id != entity.Id))
                {
                    other.IsPreferred = false;
                    await _supplierProducts.UpdateAsync(other, ct);
                }
            }

            if (isNew) await _supplierProducts.AddAsync(entity, ct);
            else       await _supplierProducts.UpdateAsync(entity, ct);

            var dtos = await BuildLinkDtosAsync(new List<SupplierProduct> { entity }, ct);
            return dtos.First();
        }

        // Удаляет связь товара и поставщика.
        public async Task UnlinkAsync(Guid id, CancellationToken ct = default)
            => await _supplierProducts.DeleteAsync(id, ct);

        // Подставляет остатки, число поставщиков и название полки по умолчанию.
        private async Task<IReadOnlyList<ProductDto>> BuildProductDtosAsync(List<Product> products, CancellationToken ct)
        {
            if (products.Count == 0) return Array.Empty<ProductDto>();

            var stockMap = await _stock.GetTotalStocksAsync(ct);
            var shelves = await _shelves.GetAllAsync(ct);
            var shelfNames = shelves.ToDictionary(s => s.Id, s => s.Name);
            var links = await _supplierProducts.GetAllAsync(ct);
            var supplierCounts = links
                .Where(l => l.IsActive)
                .GroupBy(l => l.ProductId)
                .ToDictionary(g => g.Key, g => g.Count());

            return products.Select(p => _mapper.ToDto(
                p,
                stockMap.GetValueOrDefault(p.Id),
                supplierCounts.GetValueOrDefault(p.Id),
                p.DefaultShelfId is { } sid ? shelfNames.GetValueOrDefault(sid) : null
            )).ToList();
        }

        // Подставляет названия товара и поставщика.
        private async Task<IReadOnlyList<SupplierProductDto>> BuildLinkDtosAsync(List<SupplierProduct> links, CancellationToken ct)
        {
            if (links.Count == 0) return Array.Empty<SupplierProductDto>();

            var products = (await _products.GetAllAsync(ct)).ToDictionary(p => p.Id, p => p.Name);
            var suppliers = (await _suppliers.GetAllAsync(ct)).ToDictionary(s => s.Id, s => s.Name);

            return links.Select(l => _mapper.ToDto(
                l,
                products.GetValueOrDefault(l.ProductId, "?"),
                suppliers.GetValueOrDefault(l.SupplierId, "?")
            )).ToList();
        }
    }
}
