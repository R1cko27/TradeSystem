// Application/Services/ReportService.cs

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TradeSystem.Application.Contracts.Repositories;
using TradeSystem.Application.Contracts.Services;
using TradeSystem.Application.Dto;
using TradeSystem.Application.Mappers;
using TradeSystem.Domain.Entities;

namespace TradeSystem.Application.Contracts.Services
{
    public interface IReportService
    {
        // Список всех активных товаров.
        Task<IReadOnlyList<ProductDto>> GetAllProductsReportAsync(CancellationToken ct = default);

        // Товары с положительным остатком.
        Task<IReadOnlyList<ProductDto>> GetAvailableProductsReportAsync(CancellationToken ct = default);

        // Товары, которым нужно пополнение.
        Task<IReadOnlyList<LowStockProductDto>> GetToReplenishReportAsync(CancellationToken ct = default);

        // Товары, поставляемые выбранным поставщиком.
        Task<IReadOnlyList<SupplierProductDto>> GetSupplierProductsReportAsync(Guid supplierId, CancellationToken ct = default);
    }
}

namespace TradeSystem.Application.Services
{
    public sealed class ReportService : IReportService
    {
        private readonly DtoMapper _mapper; // Преобразование в DTO
        private readonly IProductRepository _products; // Репозиторий товаров
        private readonly IShelfRepository _shelves; // Репозиторий полок
        private readonly ISupplierRepository _suppliers; // Репозиторий поставщиков
        private readonly ISupplierProductRepository _supplierProducts; // Репозиторий связей товара и поставщика
        private readonly IStockService _stock; // Складской сервис

        public ReportService(
            IProductRepository products,
            IShelfRepository shelves,
            ISupplierRepository suppliers,
            ISupplierProductRepository supplierProducts,
            IStockService stock,
            DtoMapper mapper)
        {
            _products = products;
            _shelves = shelves;
            _suppliers = suppliers;
            _supplierProducts = supplierProducts;
            _stock = stock;
                    _mapper = mapper;
        }

        // Все активные товары с расчётными полями.
        public async Task<IReadOnlyList<ProductDto>> GetAllProductsReportAsync(CancellationToken ct = default)
        {
            var products = (await _products.GetAllAsync(ct)).Where(p => p.IsActive).ToList();
            return await BuildAsync(products, ct);
        }

        // Товары с положительным остатком и полками хранения.
        public async Task<IReadOnlyList<ProductDto>> GetAvailableProductsReportAsync(CancellationToken ct = default)
        {
            var products = (await _products.GetAllAsync(ct)).Where(p => p.IsActive).ToList();
            var dtos = (await BuildAsync(products, ct)).Where(d => d.CurrentStock > 0).ToList();
            if (dtos.Count == 0) return dtos;

            var shelfCodes = (await _shelves.GetAllAsync(ct)).ToDictionary(s => s.Id, s => s.Code);
            var shelvesByProduct = (await _stock.GetAllBalancesRawAsync(ct))
                .Where(b => b.Quantity > 0)
                .GroupBy(b => b.ProductId)
                .ToDictionary(
                    g => g.Key,
                    g => string.Join(", ", g
                        .Select(b => shelfCodes.GetValueOrDefault(b.ShelfId, "?"))
                        .Distinct()
                        .OrderBy(code => code, StringComparer.CurrentCultureIgnoreCase)));

            return dtos
                .Select(d => d with { StorageShelves = shelvesByProduct.GetValueOrDefault(d.Id, string.Empty) })
                .ToList();
        }

        // Дефицит через складской сервис.
        public Task<IReadOnlyList<LowStockProductDto>> GetToReplenishReportAsync(CancellationToken ct = default)
            => _stock.GetLowStockProductsAsync(ct);

        // Активные связи выбранного поставщика.
        public async Task<IReadOnlyList<SupplierProductDto>> GetSupplierProductsReportAsync(Guid supplierId, CancellationToken ct = default)
        {
            var links = (await _supplierProducts.GetAllAsync(ct))
                .Where(l => l.SupplierId == supplierId && l.IsActive)
                .ToList();

            var products = (await _products.GetAllAsync(ct)).ToDictionary(p => p.Id, p => p.Name);
            var supplierName = (await _suppliers.GetByIdAsync(supplierId, ct))?.Name ?? "?";

            return links
                .Select(l => _mapper.ToDto(l, products.GetValueOrDefault(l.ProductId, "?"), supplierName))
                .OrderBy(x => x.ProductName, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }

        // Подставляет остатки, число поставщиков и полку по умолчанию.
        private async Task<IReadOnlyList<ProductDto>> BuildAsync(List<Product> products, CancellationToken ct)
        {
            if (products.Count == 0) return Array.Empty<ProductDto>();

            var stockMap = await _stock.GetTotalStocksAsync(ct);
            var shelfNames = (await _shelves.GetAllAsync(ct)).ToDictionary(s => s.Id, s => s.Name);
            var supplierCounts = (await _supplierProducts.GetAllAsync(ct))
                .Where(l => l.IsActive)
                .GroupBy(l => l.ProductId)
                .ToDictionary(g => g.Key, g => g.Count());

            return products
                .OrderBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase)
                .Select(p => _mapper.ToDto(
                    p,
                    stockMap.GetValueOrDefault(p.Id),
                    supplierCounts.GetValueOrDefault(p.Id),
                    p.DefaultShelfId is { } sid ? shelfNames.GetValueOrDefault(sid) : null))
                .ToList();
        }
    }
}
