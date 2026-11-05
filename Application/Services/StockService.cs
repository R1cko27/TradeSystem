// Application/Services/StockService.cs

using System;
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
using TradeSystem.Domain.Enums;

namespace TradeSystem.Application.Contracts.Services
{
    public interface IStockService
    {
        // Суммарный остаток товара по всем полкам.
        Task<int> GetTotalStockAsync(Guid productId, CancellationToken ct = default);

        // Остатки всех товаров одним словарём.
        Task<IReadOnlyDictionary<Guid, int>> GetTotalStocksAsync(CancellationToken ct = default);

        // Остатки товара по полкам.
        Task<IReadOnlyList<StockBalanceDto>> GetBalancesByProductAsync(Guid productId, CancellationToken ct = default);

        // Остатки на полке.
        Task<IReadOnlyList<StockBalanceDto>> GetBalancesByShelfAsync(Guid shelfId, CancellationToken ct = default);

        // Приход на полку: увеличивает остаток и пишет движение.
        Task ReceiveAsync(
            Guid productId,
            Guid shelfId,
            int quantity,
            StockMovementType type,
            DocumentType sourceType,
            Guid? sourceId,
            Guid? sourceLineId,
            string? comment,
            CancellationToken ct = default);

        // Расход с полки: недостаток остатка — BusinessException.
        Task ShipAsync(
            Guid productId,
            Guid shelfId,
            int quantity,
            StockMovementType type,
            DocumentType sourceType,
            Guid? sourceId,
            Guid? sourceLineId,
            string? comment,
            CancellationToken ct = default);

        // Дефицитные товары с рекомендуемым количеством.
        Task<IReadOnlyList<LowStockProductDto>> GetLowStockProductsAsync(CancellationToken ct = default);
        // Сырые остатки для подсчётов.
        Task<IReadOnlyList<StockBalance>> GetAllBalancesRawAsync(CancellationToken ct = default);
        // Удаляет нулевой остаток.
        Task RemoveZeroBalanceAsync(Guid balanceId, CancellationToken ct = default);
    }
}

namespace TradeSystem.Application.Services
{
    public sealed class StockService : IStockService
    {
        private readonly IProductRepository _products; // Репозиторий товаров
        private readonly IShelfRepository _shelves; // Репозиторий полок
        private readonly IStockBalanceRepository _balances; // Репозиторий остатков
        private readonly IStockMovementRepository _movements; // Репозиторий движений

        public StockService(
            IProductRepository products,
            IShelfRepository shelves,
            IStockBalanceRepository balances,
            IStockMovementRepository movements)
        {
            _products = products;
            _shelves = shelves;
            _balances = balances;
            _movements = movements;
        }

        // Суммарный остаток товара по всем полкам.
        public async Task<int> GetTotalStockAsync(Guid productId, CancellationToken ct = default)
        {
            var balances = await _balances.GetAllAsync(ct);
            return balances.Where(b => b.ProductId == productId).Sum(b => b.Quantity);
        }

        // Остатки всех товаров одним словарём.
        public async Task<IReadOnlyDictionary<Guid, int>> GetTotalStocksAsync(CancellationToken ct = default)
        {
            var balances = await _balances.GetAllAsync(ct);
            return balances
                .GroupBy(b => b.ProductId)
                .ToDictionary(g => g.Key, g => g.Sum(b => b.Quantity));
        }

        // Остатки товара по полкам.
        public async Task<IReadOnlyList<StockBalanceDto>> GetBalancesByProductAsync(Guid productId, CancellationToken ct = default)
        {
            var balances = (await _balances.GetAllAsync(ct)).Where(b => b.ProductId == productId).ToList();
            return await BuildBalanceDtosAsync(balances, ct);
        }

        // Остатки на полке.
        public async Task<IReadOnlyList<StockBalanceDto>> GetBalancesByShelfAsync(Guid shelfId, CancellationToken ct = default)
        {
            var balances = (await _balances.GetAllAsync(ct)).Where(b => b.ShelfId == shelfId).ToList();
            return await BuildBalanceDtosAsync(balances, ct);
        }

        // Сырые остатки для подсчётов.
        public async Task<IReadOnlyList<StockBalance>> GetAllBalancesRawAsync(CancellationToken ct = default)
            => await _balances.GetAllAsync(ct);

        // Удаляет нулевой остаток.
        public async Task RemoveZeroBalanceAsync(Guid balanceId, CancellationToken ct = default)
        {
            var b = await _balances.GetByIdAsync(balanceId, ct);
            if (b is null) return;
            if (b.Quantity != 0)
                throw new BusinessException("Нельзя удалить ненулевой остаток.");
            await _balances.DeleteAsync(balanceId, ct);
        }

            // Приход на полку: увеличивает остаток и пишет движение.
            public async Task ReceiveAsync(
                Guid productId, Guid shelfId, int quantity,
                StockMovementType type, DocumentType sourceType,
                Guid? sourceId, Guid? sourceLineId, string? comment,
                CancellationToken ct = default)
            {
            if (quantity <= 0) throw new BusinessException("Количество прихода должно быть больше нуля.");
            await EnsureProductAndShelfAsync(productId, shelfId, ct);

            var balances = await _balances.GetAllAsync(ct);
            var balance = balances.FirstOrDefault(b => b.ProductId == productId && b.ShelfId == shelfId);

            if (balance is null)
            {
                balance = new StockBalance { ProductId = productId, ShelfId = shelfId, Quantity = quantity };
                await _balances.AddAsync(balance, ct);
            }
            else
            {
                balance.Quantity += quantity;
                await _balances.UpdateAsync(balance, ct);
            }

            await WriteMovementAsync(productId, shelfId, +quantity, type, sourceType, sourceId, sourceLineId, comment, ct);
        }

        // Расход с полки: уменьшает остаток и пишет движение.
        public async Task ShipAsync(
            Guid productId, Guid shelfId, int quantity,
            StockMovementType type, DocumentType sourceType,
            Guid? sourceId, Guid? sourceLineId, string? comment,
            CancellationToken ct = default)
        {
            if (quantity <= 0) throw new BusinessException("Количество расхода должно быть больше нуля.");
            await EnsureProductAndShelfAsync(productId, shelfId, ct);

            var balances = await _balances.GetAllAsync(ct);
            var balance = balances.FirstOrDefault(b => b.ProductId == productId && b.ShelfId == shelfId)
                          ?? throw new BusinessException("На этой полке нет данного товара.");

            if (balance.Quantity < quantity)
                throw new BusinessException(
                    $"Недостаточно товара на полке: есть {balance.Quantity}, требуется {quantity}.");

            balance.Quantity -= quantity;
            await _balances.UpdateAsync(balance, ct);

            await WriteMovementAsync(productId, shelfId, -quantity, type, sourceType, sourceId, sourceLineId, comment, ct);
        }

        // Дефицит: товары, у которых остаток ниже минимума.
        public async Task<IReadOnlyList<LowStockProductDto>> GetLowStockProductsAsync(CancellationToken ct = default)
        {
            var products = (await _products.GetAllAsync(ct)).Where(p => p.IsActive && p.MinimumStockQuantity > 0).ToList();
            if (products.Count == 0) return Array.Empty<LowStockProductDto>();

            var stockMap = await GetTotalStocksAsync(ct);

            var result = new List<LowStockProductDto>();
            foreach (var p in products)
            {
                var current = stockMap.GetValueOrDefault(p.Id);
                if (current >= p.MinimumStockQuantity) continue;

                var target = p.TargetStockQuantity ?? (p.MinimumStockQuantity * 2);
                var recommended = Math.Max(1, target - current);

                result.Add(new LowStockProductDto
                {
                    ProductId = p.Id,
                    ProductName = p.Name,
                    UnitLabel = p.Unit.Of(),
                    CurrentStock = current,
                    MinimumStockQuantity = p.MinimumStockQuantity,
                    RecommendedOrderQuantity = recommended
                });
            }

            return result.OrderBy(x => x.ProductName, StringComparer.CurrentCultureIgnoreCase).ToList();
        }

        // Проверяет существование товара и полки.
        private async Task EnsureProductAndShelfAsync(Guid productId, Guid shelfId, CancellationToken ct)
        {
            if (await _products.GetByIdAsync(productId, ct) is null)
                throw new BusinessException("Товар не найден.");
            if (await _shelves.GetByIdAsync(shelfId, ct) is null)
                throw new BusinessException("Полка не найдена.");
        }

        // Пишет движение товара.
        private async Task WriteMovementAsync(
            Guid productId, Guid shelfId, int delta,
            StockMovementType type, DocumentType sourceType,
            Guid? sourceId, Guid? sourceLineId, string? comment,
            CancellationToken ct)
        {
            var movement = new StockMovement
            {
                ProductId = productId,
                ShelfId = shelfId,
                QuantityDelta = delta,
                Type = type,
                SourceType = sourceType,
                SourceId = sourceId,
                SourceLineId = sourceLineId,
                OccurredAtUtc = DateTimeOffset.UtcNow,
                Comment = comment
            };
            await _movements.AddAsync(movement, ct);
        }

        // Подставляет названия товара и полки в остатки.
        private async Task<IReadOnlyList<StockBalanceDto>> BuildBalanceDtosAsync(List<StockBalance> balances, CancellationToken ct)
        {
            if (balances.Count == 0) return Array.Empty<StockBalanceDto>();

            var products = (await _products.GetAllAsync(ct)).ToDictionary(p => p.Id, p => p.Name);
            var shelves = (await _shelves.GetAllAsync(ct)).ToDictionary(s => s.Id, s => s);

            return balances.Select(b =>
            {
                shelves.TryGetValue(b.ShelfId, out var sh);
                return new StockBalanceDto
                {
                    ProductId = b.ProductId,
                    ProductName = products.GetValueOrDefault(b.ProductId, "?"),
                    ShelfId = b.ShelfId,
                    ShelfCode = sh?.Code ?? "?",
                    ShelfName = sh?.Name ?? "?",
                    Quantity = b.Quantity
                };
            }).ToList();
        }
    }
}
