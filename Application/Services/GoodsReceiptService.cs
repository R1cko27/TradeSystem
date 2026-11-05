// Application/Services/GoodsReceiptService.cs

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
using TradeSystem.Domain.Enums;

namespace TradeSystem.Application.Contracts.Services
{
    public interface IGoodsReceiptService
    {
        Task<IReadOnlyList<GoodsReceiptDto>> GetReceiptsAsync(CancellationToken ct = default);
        Task<GoodsReceiptDto?> GetReceiptAsync(Guid id, CancellationToken ct = default);

        Task<GoodsReceiptDto> CreateDraftAsync(GoodsReceiptInput input, CancellationToken ct = default);

        Task<GoodsReceiptDto> PostAsync(Guid receiptId, CancellationToken ct = default);

        Task DeleteDraftAsync(Guid receiptId, CancellationToken ct = default);
    }
}

namespace TradeSystem.Application.Services
{
    public sealed class GoodsReceiptService : IGoodsReceiptService
    {
        private readonly DtoMapper _mapper;
        private readonly IGoodsReceiptRepository _receipts;
        private readonly IPurchaseOrderRepository _orders;
        private readonly IProductRepository _products;
        private readonly IShelfRepository _shelves;
        private readonly ISupplierRepository _suppliers;
        private readonly IStockService _stock;

        public GoodsReceiptService(
            IGoodsReceiptRepository receipts,
            IPurchaseOrderRepository orders,
            IProductRepository products,
            IShelfRepository shelves,
            ISupplierRepository suppliers,
            IStockService stock,
            DtoMapper mapper)
        {
            _receipts = receipts;
            _orders = orders;
            _products = products;
            _shelves = shelves;
            _suppliers = suppliers;
            _stock = stock;
                    _mapper = mapper;
        }

        public async Task<IReadOnlyList<GoodsReceiptDto>> GetReceiptsAsync(CancellationToken ct = default)
        {
            var all = await _receipts.GetAllAsync(ct);
            return await BuildDtosAsync(all.ToList(), ct);
        }

        public async Task<GoodsReceiptDto?> GetReceiptAsync(Guid id, CancellationToken ct = default)
        {
            var r = await _receipts.GetByIdAsync(id, ct);
            if (r is null) return null;
            var list = await BuildDtosAsync(new List<GoodsReceipt> { r }, ct);
            return list.FirstOrDefault();
        }

        public async Task<GoodsReceiptDto> CreateDraftAsync(GoodsReceiptInput input, CancellationToken ct = default)
        {
            if (input.SupplierId == Guid.Empty) throw new BusinessException("Не выбран поставщик.");
            if (input.Lines.Count == 0) throw new BusinessException("Поступление не содержит позиций.");
            if (await _suppliers.GetByIdAsync(input.SupplierId, ct) is null)
                throw new BusinessException("Поставщик не найден.");

            var products = (await _products.GetAllAsync(ct)).ToDictionary(p => p.Id);
            var shelves = (await _shelves.GetAllAsync(ct)).ToDictionary(s => s.Id);

            var receipt = new GoodsReceipt
            {
                Number = await GenerateNumberAsync("GR", ct),
                SupplierId = input.SupplierId,
                OrderId = input.OrderId,
                ReceivedAtUtc = DateTimeOffset.UtcNow,
                Status = GoodsReceiptStatus.Draft,
                Comment = input.Comment
            };

            foreach (var li in input.Lines)
            {
                if (li.Quantity <= 0) throw new BusinessException("Количество должно быть больше нуля.");
                if (!products.ContainsKey(li.ProductId)) throw new BusinessException("Товар не найден.");
                if (!shelves.ContainsKey(li.ShelfId)) throw new BusinessException("Полка не найдена.");

                receipt.Lines.Add(new GoodsReceiptLine
                {
                    OrderLineId = li.OrderLineId,
                    ProductId = li.ProductId,
                    ProductName = products[li.ProductId].Name,
                    ShelfId = li.ShelfId,
                    Quantity = li.Quantity,
                    UnitCost = li.UnitCost
                });
            }

            await _receipts.AddAsync(receipt, ct);
            var dtos = await BuildDtosAsync(new List<GoodsReceipt> { receipt }, ct);
            return dtos.First();
        }

        public async Task<GoodsReceiptDto> PostAsync(Guid receiptId, CancellationToken ct = default)
        {
            var receipt = await _receipts.GetByIdAsync(receiptId, ct)
                          ?? throw new BusinessException("Поступление не найдено.");

            if (receipt.Status != GoodsReceiptStatus.Draft)
                throw new BusinessException("Провести можно только поступление в статусе «Черновик».");

            foreach (var line in receipt.Lines)
            {
                await _stock.ReceiveAsync(
                    line.ProductId, line.ShelfId, line.Quantity,
                    StockMovementType.PurchaseReceipt, DocumentType.GoodsReceipt,
                    receipt.Id, line.Id, null, ct);
            }

            if (receipt.OrderId is { } orderId)
            {
                var order = await _orders.GetByIdAsync(orderId, ct);
                if (order is not null)
                {
                    foreach (var line in receipt.Lines.Where(l => l.OrderLineId.HasValue))
                    {
                        var ol = order.Lines.FirstOrDefault(x => x.Id == line.OrderLineId!.Value);
                        if (ol is null) continue;
                        ol.ReceivedQuantity += line.Quantity;
                        if (ol.ReceivedQuantity >= ol.OrderedQuantity) ol.IsClosed = true;
                    }

                    RecalculateOrderStatus(order);
                    await _orders.UpdateAsync(order, ct);
                }
            }

            receipt.Status = GoodsReceiptStatus.Posted;
            await _receipts.UpdateAsync(receipt, ct);

            var dtos = await BuildDtosAsync(new List<GoodsReceipt> { receipt }, ct);
            return dtos.First();
        }

        public async Task DeleteDraftAsync(Guid receiptId, CancellationToken ct = default)
        {
            var receipt = await _receipts.GetByIdAsync(receiptId, ct)
                          ?? throw new BusinessException("Поступление не найдено.");
            if (receipt.Status != GoodsReceiptStatus.Draft)
                throw new BusinessException("Удалять можно только черновик поступления.");
            await _receipts.DeleteAsync(receiptId, ct);
        }

        private static void RecalculateOrderStatus(PurchaseOrder order)
        {
            if (order.Lines.Count == 0) return;
            var allClosed = order.Lines.All(l => l.ReceivedQuantity >= l.OrderedQuantity);
            var anyReceived = order.Lines.Any(l => l.ReceivedQuantity > 0);

            order.Status = allClosed
                ? PurchaseOrderStatus.Received
                : anyReceived
                    ? PurchaseOrderStatus.PartiallyReceived
                    : order.Status;
        }

        private async Task<string> GenerateNumberAsync(string prefix, CancellationToken ct)
        {
            var all = await _receipts.GetAllAsync(ct);
            var today = DateTime.UtcNow.ToString("yyyyMMdd");
            var n = all.Count(r => r.Number.StartsWith($"{prefix}-{today}-", StringComparison.Ordinal));
            return $"{prefix}-{today}-{n + 1:D4}";
        }

        private async Task<IReadOnlyList<GoodsReceiptDto>> BuildDtosAsync(List<GoodsReceipt> receipts, CancellationToken ct)
        {
            if (receipts.Count == 0) return Array.Empty<GoodsReceiptDto>();

            var suppliers = (await _suppliers.GetAllAsync(ct)).ToDictionary(s => s.Id, s => s.Name);
            var products = (await _products.GetAllAsync(ct)).ToDictionary(p => p.Id, p => p.Name);
            var shelves = (await _shelves.GetAllAsync(ct)).ToDictionary(s => s.Id, s => s.Name);
            var orders = (await _orders.GetAllAsync(ct)).ToDictionary(o => o.Id, o => o.Number);

            return receipts
                .OrderByDescending(r => r.ReceivedAtUtc)
                .Select(r => _mapper.ToDto(
                    r,
                    suppliers.GetValueOrDefault(r.SupplierId, "?"),
                    r.OrderId is { } oid ? orders.GetValueOrDefault(oid) : null,
                    line => (products.GetValueOrDefault(line.ProductId, "?"),
                             shelves.GetValueOrDefault(line.ShelfId, "?"))))
                .ToList();
        }
    }
}
