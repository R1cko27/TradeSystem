// Application/Services/PurchaseOrderService.cs

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
    public interface IPurchaseOrderService
    {
        Task<IReadOnlyList<PurchaseOrderDto>> GetOrdersAsync(CancellationToken ct = default);
        Task<PurchaseOrderDto?> GetOrderAsync(Guid id, CancellationToken ct = default);

        Task<PurchaseOrderDto> CreateOrderAsync(CreatePurchaseOrderInput input, CancellationToken ct = default);

        Task<AutoOrderPreviewDto> PreviewAutoOrdersAsync(AutoOrderRequest? request = null, CancellationToken ct = default);
        Task<AutoOrderResult> AutoGenerateOrdersAsync(AutoOrderRequest? request = null, CancellationToken ct = default);

        Task ConfirmOrderAsync(Guid orderId, CancellationToken ct = default);
        Task CancelOrderAsync(Guid orderId, CancellationToken ct = default);
        Task CloseOrderAsync(Guid orderId, CancellationToken ct = default);
        Task DeleteOrderAsync(Guid orderId, CancellationToken ct = default);
    }
}

namespace TradeSystem.Application.Services
{
    public sealed class PurchaseOrderService : IPurchaseOrderService
    {
        private readonly DtoMapper _mapper; // Преобразование в DTO
        private readonly IPurchaseOrderRepository _orders; // Репозиторий заказов
        private readonly IProductRepository _products; // Репозиторий товаров
        private readonly ISupplierRepository _suppliers; // Репозиторий поставщиков
        private readonly ISupplierProductRepository _supplierProducts; // Репозиторий связей товара и поставщика
        private readonly IInvoiceRepository _invoices; // Репозиторий счетов
        private readonly IGoodsReceiptRepository _receipts; // Репозиторий поступлений
        private readonly IStockService _stock; // Складской сервис

        public PurchaseOrderService(
            IPurchaseOrderRepository orders,
            IProductRepository products,
            ISupplierRepository suppliers,
            ISupplierProductRepository supplierProducts,
            IInvoiceRepository invoices,
            IGoodsReceiptRepository receipts,
            IStockService stock,
            DtoMapper mapper)
        {
            _orders = orders;
            _products = products;
            _suppliers = suppliers;
            _supplierProducts = supplierProducts;
            _invoices = invoices;
            _receipts = receipts;
            _stock = stock;
                    _mapper = mapper;
        }

        // Заказы с поставщиком, статусом оплаты и позициями.
        public async Task<IReadOnlyList<PurchaseOrderDto>> GetOrdersAsync(CancellationToken ct = default)
        {
            var orders = await _orders.GetAllAsync(ct);
            return await BuildDtosAsync(orders.ToList(), ct);
        }

        // Один заказ по идентификатору.
        public async Task<PurchaseOrderDto?> GetOrderAsync(Guid id, CancellationToken ct = default)
        {
            var o = await _orders.GetByIdAsync(id, ct);
            if (o is null) return null;
            var list = await BuildDtosAsync(new List<PurchaseOrder> { o }, ct);
            return list.FirstOrDefault();
        }

        // Создаёт черновик заказа, проверяя поставщика, товары и связи.
        public async Task<PurchaseOrderDto> CreateOrderAsync(CreatePurchaseOrderInput input, CancellationToken ct = default)
        {
            if (input.SupplierId == Guid.Empty) throw new BusinessException("Не выбран поставщик.");
            if (input.Lines.Count == 0) throw new BusinessException("Заказ не содержит позиций.");
            if (await _suppliers.GetByIdAsync(input.SupplierId, ct) is null)
                throw new BusinessException("Поставщик не найден.");

            var links = (await _supplierProducts.GetAllAsync(ct))
                .Where(l => l.SupplierId == input.SupplierId && l.IsActive)
                .ToDictionary(l => l.ProductId);

            var products = (await _products.GetAllAsync(ct)).ToDictionary(p => p.Id);

            var order = new PurchaseOrder
            {
                Number = await GenerateNumberAsync("PO", ct),
                SupplierId = input.SupplierId,
                OrderDateUtc = DateTimeOffset.UtcNow,
                Status = PurchaseOrderStatus.Draft,
                Comment = input.Comment
            };

            decimal total = 0m;
            foreach (var li in input.Lines)
            {
                if (li.Quantity <= 0) throw new BusinessException("Количество в позиции должно быть больше нуля.");
                if (!products.TryGetValue(li.ProductId, out var product))
                    throw new BusinessException("Товар в позиции не найден.");
                if (!links.TryGetValue(li.ProductId, out var link))
                    throw new BusinessException($"Товар «{product.Name}» не поставляется выбранным поставщиком.");

                var unitPrice = li.UnitPrice ?? link.PurchasePrice ?? 0m;
                var line = new PurchaseOrderLine
                {
                    ProductId = product.Id,
                    SupplierProductId = link.Id,
                    ProductName = product.Name,
                    Unit = product.Unit,
                    OrderedQuantity = li.Quantity,
                    ReceivedQuantity = 0,
                    UnitPrice = unitPrice,
                    TotalPrice = unitPrice * li.Quantity
                };
                order.Lines.Add(line);
                total += line.TotalPrice;
            }

            order.TotalAmount = total;
            await _orders.AddAsync(order, ct);

            var dtos = await BuildDtosAsync(new List<PurchaseOrder> { order }, ct);
            return dtos.First();
        }

        // Строит план автоформирования без записи в хранилище.
        public async Task<AutoOrderPreviewDto> PreviewAutoOrdersAsync(AutoOrderRequest? request = null, CancellationToken ct = default)
        {
            request ??= new AutoOrderRequest();

            var needed = await CollectNeedsAsync(request, ct);
            var linksByProduct = await GetActiveLinksByProductAsync(ct);
            var suppliers = (await _suppliers.GetAllAsync(ct)).ToDictionary(s => s.Id, s => s.Name);
            var products = (await _products.GetAllAsync(ct)).ToDictionary(p => p.Id);

            var rows = new List<PreviewProductDto>();
            var warnings = new List<string>();

            foreach (var (productId, baseQty) in needed)
            {
                if (!linksByProduct.TryGetValue(productId, out var candidates) || candidates.Count == 0)
                {
                    var name = products.GetValueOrDefault(productId) is { } pp ? pp.Name : productId.ToString();
                    warnings.Add($"Товар «{name}» не имеет активного поставщика — будет пропущен.");
                    continue;
                }

                var options = candidates
                    .Select(c => BuildOption(c, baseQty, suppliers))
                    .OrderByDescending(o => o.IsPreferred)
                    .ThenBy(o => o.PurchasePrice ?? decimal.MaxValue)
                    .ThenBy(o => o.LeadTimeDays ?? int.MaxValue)
                    .ToList();

                var recommended = ChooseSupplier(productId, candidates, request.ExplicitSupplierChoice);

                rows.Add(new PreviewProductDto
                {
                    ProductId = productId,
                    ProductName = products.GetValueOrDefault(productId)?.Name ?? "?",
                    UnitLabel = products.GetValueOrDefault(productId)?.Unit.Of() ?? "",
                    CurrentStock = (await _stock.GetTotalStockAsync(productId, ct)),
                    MinimumStockQuantity = products.GetValueOrDefault(productId)?.MinimumStockQuantity ?? 0,
                    BaseNeededQuantity = baseQty,
                    Options = options,
                    RecommendedSupplierId = recommended?.SupplierId
                });
            }

            return new AutoOrderPreviewDto
            {
                Products = rows.OrderBy(r => r.ProductName, StringComparer.CurrentCultureIgnoreCase).ToList(),
                Warnings = warnings
            };
        }

        // Создаёт черновики заказов по дефициту, группируя позиции по поставщикам.
        public async Task<AutoOrderResult> AutoGenerateOrdersAsync(AutoOrderRequest? request = null, CancellationToken ct = default)
        {
            request ??= new AutoOrderRequest();
            var result = new AutoOrderResult();

            var needed = await CollectNeedsAsync(request, ct);
            if (needed.Count == 0) return result;

            var linksByProduct = await GetActiveLinksByProductAsync(ct);
            var productsForNames = (await _products.GetAllAsync(ct)).ToDictionary(p => p.Id);

            var bySupplier = new Dictionary<Guid, List<(Guid productId, int qty, SupplierProduct link)>>();

            foreach (var (productId, baseQty) in needed)
            {
                if (!linksByProduct.TryGetValue(productId, out var candidates) || candidates.Count == 0)
                {
                    var name = productsForNames.GetValueOrDefault(productId)?.Name ?? productId.ToString();
                    result.Warnings.Add($"Товар «{name}» не имеет активного поставщика — пропущен.");
                    continue;
                }

                var chosen = ChooseSupplier(productId, candidates, request.ExplicitSupplierChoice)!;
                var qty = AdjustQuantity(baseQty, chosen);

                if (!bySupplier.TryGetValue(chosen.SupplierId, out var list))
                    bySupplier[chosen.SupplierId] = list = new();

                list.Add((productId, qty, chosen));
            }

            foreach (var (supplierId, positions) in bySupplier)
            {
                var order = new PurchaseOrder
                {
                    Number = await GenerateNumberAsync("PO", ct),
                    SupplierId = supplierId,
                    OrderDateUtc = DateTimeOffset.UtcNow,
                    Status = PurchaseOrderStatus.Draft,
                    Comment = "Сформирован автоматически по дефициту."
                };

                decimal total = 0m;
                foreach (var (pid, qty, link) in positions)
                {
                    var product = productsForNames[pid];
                    var unitPrice = link.PurchasePrice ?? 0m;
                    var line = new PurchaseOrderLine
                    {
                        ProductId = pid,
                        SupplierProductId = link.Id,
                        ProductName = product.Name,
                        Unit = product.Unit,
                        OrderedQuantity = qty,
                        ReceivedQuantity = 0,
                        UnitPrice = unitPrice,
                        TotalPrice = unitPrice * qty
                    };
                    order.Lines.Add(line);
                    total += line.TotalPrice;
                }

                order.TotalAmount = total;
                await _orders.AddAsync(order, ct);

                var dtos = await BuildDtosAsync(new List<PurchaseOrder> { order }, ct);
                result.Orders.Add(dtos.First());
            }

            return result;
        }

        // Подтверждает черновик заказа.
        public async Task ConfirmOrderAsync(Guid orderId, CancellationToken ct = default)
        {
            var order = await _orders.GetByIdAsync(orderId, ct) ?? throw new BusinessException("Заказ не найден.");
            if (order.Status != PurchaseOrderStatus.Draft)
                throw new BusinessException("Подтвердить можно только заказ в статусе «Черновик».");
            order.Status = PurchaseOrderStatus.Confirmed;
            await _orders.UpdateAsync(order, ct);
        }

        // Отменяет черновик заказа.
        public async Task CancelOrderAsync(Guid orderId, CancellationToken ct = default)
        {
            var order = await _orders.GetByIdAsync(orderId, ct) ?? throw new BusinessException("Заказ не найден.");
            if (order.Status != PurchaseOrderStatus.Draft)
                throw new BusinessException("Отменить можно только заказ в статусе «Черновик».");
            order.Status = PurchaseOrderStatus.Cancelled;
            await _orders.UpdateAsync(order, ct);
        }

        // Закрывает полностью полученный заказ.
        public async Task CloseOrderAsync(Guid orderId, CancellationToken ct = default)
        {
            var order = await _orders.GetByIdAsync(orderId, ct) ?? throw new BusinessException("Заказ не найден.");

            // Закрывается только заказ, по которому всё заказанное уже получено.
            if (order.Status != PurchaseOrderStatus.Received)
                throw new BusinessException("Закрыть можно только заказ в статусе «Получен».");

            order.Status = PurchaseOrderStatus.Closed;
            await _orders.UpdateAsync(order, ct);
        }

        // Удаляет черновик, если на него нет ссылок из счетов и поступлений.
        public async Task DeleteOrderAsync(Guid orderId, CancellationToken ct = default)
        {
            var order = await _orders.GetByIdAsync(orderId, ct) ?? throw new BusinessException("Заказ не найден.");
            if (order.Status != PurchaseOrderStatus.Draft)
                throw new BusinessException("Удалять можно только черновик заказа.");

            // На заказ могут ссылаться счета и поступления: удаление сделало бы эти ссылки «висячими».
            if ((await _invoices.GetAllAsync(ct)).Any(i => i.OrderId == orderId))
                throw new BusinessException("Нельзя удалить заказ: по нему выставлены счета.");

            if ((await _receipts.GetAllAsync(ct)).Any(r => r.OrderId == orderId))
                throw new BusinessException("Нельзя удалить заказ: по нему уже есть поступления.");

            await _orders.DeleteAsync(orderId, ct);
        }

        // Считает потребность по дефициту и дополнительным товарам.
        private async Task<Dictionary<Guid, int>> CollectNeedsAsync(AutoOrderRequest request, CancellationToken ct)
        {
            var low = await _stock.GetLowStockProductsAsync(ct);
            var needed = low.ToDictionary(x => x.ProductId, x => x.RecommendedOrderQuantity);

            if (request.AdditionalProductIds is { Count: > 0 })
            {
                var products = (await _products.GetAllAsync(ct)).ToDictionary(p => p.Id);
                var stockMap = await _stock.GetTotalStocksAsync(ct);
                foreach (var pid in request.AdditionalProductIds)
                {
                    if (needed.ContainsKey(pid)) continue;
                    if (!products.TryGetValue(pid, out var p)) continue;
                    needed[pid] = ComputeRecommendedQuantity(p, stockMap.GetValueOrDefault(pid));
                }
            }

            return needed;
        }

        // Активные связи, сгруппированные по товару.
        private async Task<Dictionary<Guid, List<SupplierProduct>>> GetActiveLinksByProductAsync(CancellationToken ct)
            => (await _supplierProducts.GetAllAsync(ct))
                .Where(l => l.IsActive)
                .GroupBy(l => l.ProductId)
                .ToDictionary(g => g.Key, g => g.ToList());

        // Рекомендуемое количество до целевого остатка или удвоенного минимума.
        private static int ComputeRecommendedQuantity(Product p, int current)
        {
            var target = p.TargetStockQuantity ?? (p.MinimumStockQuantity > 0 ? p.MinimumStockQuantity * 2 : 1);
            return Math.Max(1, target - current);
        }

        // Вариант поставщика для предпросмотра.
        private PreviewSupplierOptionDto BuildOption(SupplierProduct c, int baseQty, Dictionary<Guid, string> suppliers)
        {
            var adjusted = AdjustQuantity(baseQty, c);
            var price = c.PurchasePrice ?? 0m;
            return new PreviewSupplierOptionDto
            {
                SupplierId = c.SupplierId,
                SupplierName = suppliers.GetValueOrDefault(c.SupplierId, "?"),
                SupplierProductId = c.Id,
                PurchasePrice = c.PurchasePrice,
                LeadTimeDays = c.LeadTimeDays,
                MinimumOrderQuantity = c.MinimumOrderQuantity,
                OrderMultiple = c.OrderMultiple,
                IsPreferred = c.IsPreferred,
                AdjustedQuantity = adjusted,
                LineAmount = price * adjusted
            };
        }

        // Выбирает поставщика: явный, предпочтительный, затем дешёвый и быстрый.
        private static SupplierProduct? ChooseSupplier(
            Guid productId,
            List<SupplierProduct> candidates,
            Dictionary<Guid, Guid>? explicitChoice)
        {
            if (explicitChoice is not null && explicitChoice.TryGetValue(productId, out var forcedId))
            {
                var forced = candidates.FirstOrDefault(c => c.SupplierId == forcedId);
                if (forced is not null) return forced;
            }

            return candidates
                .OrderByDescending(c => c.IsPreferred)
                .ThenBy(c => c.PurchasePrice ?? decimal.MaxValue)
                .ThenBy(c => c.LeadTimeDays ?? int.MaxValue)
                .FirstOrDefault();
        }

        // Подгоняет количество под минимальную партию и кратность.
        private static int AdjustQuantity(int requested, SupplierProduct link)
        {
            var qty = requested;
            if (link.MinimumOrderQuantity is { } min && min > 0)
                qty = Math.Max(qty, min);
            if (link.OrderMultiple is { } mult && mult > 1)
                qty = (int)(Math.Ceiling(qty / (double)mult) * mult);
            return Math.Max(1, qty);
        }

        // Номер вида PO-ГГГГММДД-НННН с порядковым номером за день.
        private async Task<string> GenerateNumberAsync(string prefix, CancellationToken ct)
        {
            var all = await _orders.GetAllAsync(ct);
            var today = DateTime.UtcNow.ToString("yyyyMMdd");
            var countToday = all.Count(o => o.Number.StartsWith($"{prefix}-{today}-", StringComparison.Ordinal));
            return $"{prefix}-{today}-{countToday + 1:D4}";
        }

        // Подставляет поставщика и статус оплаты по счетам.
        private async Task<IReadOnlyList<PurchaseOrderDto>> BuildDtosAsync(List<PurchaseOrder> orders, CancellationToken ct)
        {
            if (orders.Count == 0) return Array.Empty<PurchaseOrderDto>();

            var suppliers = (await _suppliers.GetAllAsync(ct)).ToDictionary(s => s.Id, s => s.Name);
            var invoices = await _invoices.GetAllAsync(ct);
            var invoicesByOrder = invoices
                .GroupBy(i => i.OrderId)
                .ToDictionary(g => g.Key, g => g.ToList());

            return orders
                .OrderByDescending(o => o.OrderDateUtc)
                .Select(o => _mapper.ToDto(
                    o,
                    suppliers.GetValueOrDefault(o.SupplierId, "?"),
                    ComputePaymentStatus(o.TotalAmount, invoicesByOrder.GetValueOrDefault(o.Id))))
                .ToList();
        }
        // Статус оплаты заказа по связанным счетам.
        private static PaymentStatus ComputePaymentStatus(decimal orderTotal, List<Invoice>? invoices)
        {
            if (invoices is null || invoices.Count == 0) return PaymentStatus.Unpaid;

            var active = invoices.Where(i => i.PaymentStatus != PaymentStatus.Cancelled).ToList();
            if (active.Count == 0) return PaymentStatus.Unpaid;

            var invoicedTotal = active.Sum(i => i.Amount);
            var paidTotal = active.Where(i => i.PaymentStatus == PaymentStatus.Paid).Sum(i => i.Amount);

            if (paidTotal <= 0m) return PaymentStatus.Unpaid;

            var target = orderTotal > 0m ? orderTotal : invoicedTotal;
            return paidTotal >= target ? PaymentStatus.Paid : PaymentStatus.PartiallyPaid;
        }
    }
}
