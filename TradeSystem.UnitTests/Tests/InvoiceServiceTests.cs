// TradeSystem.UnitTests/Tests/InvoiceServiceTests.cs

using System;
using System.Linq;
using System.Threading.Tasks;
using TradeSystem.Application.Exceptions;
using TradeSystem.Domain.Entities;
using TradeSystem.Domain.Enums;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TradeSystem.UnitTests.Tests;

/// <summary>Проверки счетов на оплату.</summary>
[TestClass]
public sealed class InvoiceServiceTests
{
    // Ожидаем: исключение «Заказ не найден».
    [TestMethod]
    public async Task Create_UnknownOrder_Throws()
    {
        var context = new TestContext();

        await Check.ThrowsAsync<BusinessException>(
            () => context.InvoiceService.CreateInvoiceAsync(Guid.NewGuid(), "INV-1", 100m, null, null),
            "Заказ не найден");
    }

    [TestMethod]
    public async Task Create_EmptyNumber_Throws()
    {
        var context = new TestContext();
        var supplier = context.AddSupplier();
        var order = context.AddOrder(supplier.Id);

        await Check.ThrowsAsync<BusinessException>(
            () => context.InvoiceService.CreateInvoiceAsync(order.Id, " ", 100m, null, null),
            "Номер счёта");
    }

    [TestMethod]
    public async Task Create_ZeroAmount_Throws()
    {
        var context = new TestContext();
        var supplier = context.AddSupplier();
        var order = context.AddOrder(supplier.Id);

        await Check.ThrowsAsync<BusinessException>(
            () => context.InvoiceService.CreateInvoiceAsync(order.Id, "INV-1", 0m, null, null),
            "Сумма счёта");
    }

    // Ожидаем: статус «Не оплачен», сумма и номер заказа перенесены в DTO.
    [TestMethod]
    public async Task Create_Valid_ReturnsUnpaidInvoice()
    {
        var context = new TestContext();
        var supplier = context.AddSupplier();
        var order = context.AddOrder(supplier.Id);

        var dto = await context.InvoiceService.CreateInvoiceAsync(order.Id, "INV-1", 250m, null, null);

        Check.Equal(PaymentStatus.Unpaid, dto.PaymentStatus);
        Check.Equal(250m, dto.Amount);
        Check.Equal(order.Number, dto.OrderNumber);
    }

    // Ожидаем: статус «Оплачен» и заполненная дата оплаты.
    [TestMethod]
    public async Task MarkPaid_SetsStatusAndDate()
    {
        var context = new TestContext();
        var supplier = context.AddSupplier();
        var order = context.AddOrder(supplier.Id);
        var invoice = await context.InvoiceService.CreateInvoiceAsync(order.Id, "INV-1", 100m, null, null);

        var dto = await context.InvoiceService.MarkPaidAsync(invoice.Id);

        Check.Equal(PaymentStatus.Paid, dto.PaymentStatus);
        Check.NotNull(dto.PaidAtUtc, "дата оплаты должна быть заполнена");
    }

    // Ожидаем: исключение «Счёт не найден».
    [TestMethod]
    public async Task MarkPaid_Unknown_Throws()
    {
        var context = new TestContext();

        await Check.ThrowsAsync<BusinessException>(
            () => context.InvoiceService.MarkPaidAsync(Guid.NewGuid()),
            "Счёт не найден");
    }

    // Ожидаем: исключение о запрете оплаты отменённого счёта.
    [TestMethod]
    public async Task MarkPaid_Cancelled_Throws()
    {
        var context = new TestContext();
        var invoice = context.Invoices.Seed(new Invoice
        {
            Number = "INV-1",
            Amount = 100m,
            PaymentStatus = PaymentStatus.Cancelled
        });

        await Check.ThrowsAsync<BusinessException>(
            () => context.InvoiceService.MarkPaidAsync(invoice.Id),
            "отменённый счёт");
    }

    [TestMethod]
    public async Task Delete_Paid_Throws()
    {
        var context = new TestContext();
        var invoice = context.Invoices.Seed(new Invoice
        {
            Number = "INV-1",
            Amount = 100m,
            PaymentStatus = PaymentStatus.Paid
        });

        await Check.ThrowsAsync<BusinessException>(
            () => context.InvoiceService.DeleteInvoiceAsync(invoice.Id),
            "оплаченный счёт");
    }

    [TestMethod]
    public async Task Delete_Unpaid_Removes()
    {
        var context = new TestContext();
        var invoice = context.Invoices.Seed(new Invoice { Number = "INV-1", Amount = 100m });

        await context.InvoiceService.DeleteInvoiceAsync(invoice.Id);

        Check.Equal(0, context.Invoices.Items.Count);
    }

    // Ожидаем: возвращаются счета только указанного заказа.
    [TestMethod]
    public async Task GetByOrder_FiltersInvoices()
    {
        var context = new TestContext();
        var supplier = context.AddSupplier();
        var first = context.AddOrder(supplier.Id);
        var second = context.AddOrder(supplier.Id);
        await context.InvoiceService.CreateInvoiceAsync(first.Id, "INV-1", 100m, null, null);
        await context.InvoiceService.CreateInvoiceAsync(second.Id, "INV-2", 200m, null, null);

        var items = await context.InvoiceService.GetInvoicesByOrderAsync(first.Id);

        Check.Equal(1, items.Count);
        Check.Equal("INV-1", items[0].Number);
    }
}
