// ViewModels/Invoices/InvoiceListViewModel.cs

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TradeSystem.App.ViewModels.Abstractions;
using TradeSystem.Application.Contracts.Services;
using TradeSystem.Application.Dto;
using TradeSystem.Domain.Enums;

namespace TradeSystem.App.ViewModels.Invoices;

public partial class InvoiceListViewModel : ViewModelBase
{
    private readonly IInvoiceService _invoices;
    private readonly IPurchaseOrderService _orders;
    private Guid _orderId;

    [ObservableProperty] private string _orderNumber = string.Empty;
    [ObservableProperty] private string _supplierName = string.Empty;
    [ObservableProperty] private decimal _orderTotal;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PayCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
    private InvoiceDto? _selectedInvoice;

    [ObservableProperty] private string _newNumber = string.Empty;
    [ObservableProperty] private string _newAmountText = string.Empty;
    [ObservableProperty] private string _newDueDateText = string.Empty;

    public ObservableCollection<InvoiceDto> Items { get; } = new();

    public event Action? CloseRequested;

    public InvoiceListViewModel(IInvoiceService invoices, IPurchaseOrderService orders, IDialogService dialog)
        : base(dialog)
    {
        _invoices = invoices;
        _orders = orders;
    }

    public async Task LoadAsync(Guid orderId)
    {
        await RunAsync(async () =>
        {
            _orderId = orderId;
            var order = await _orders.GetOrderAsync(orderId);
            if (order is null)
            {
                Dialog.ShowError("Заказ не найден.");
                CloseRequested?.Invoke();
                return;
            }

            OrderNumber = order.Number;
            SupplierName = order.SupplierName;
            OrderTotal = order.TotalAmount;

            NewNumber = await GenerateNextNumberAsync(orderId);
            NewAmountText = order.TotalAmount.ToString("0.##", CultureInfo.CurrentCulture);
            NewDueDateText = string.Empty;

            await ReloadInvoicesAsync();
        });
    }

    [RelayCommand]
    private Task CreateAsync() => RunAsync(async () =>
    {
        var errors = ValidateNew(out decimal amount, out DateTimeOffset? due);
        if (errors.Count > 0)
        {
            Dialog.ShowError(string.Join(Environment.NewLine, errors));
            return;
        }

        await _invoices.CreateInvoiceAsync(_orderId, NewNumber.Trim(), amount, due, null);
        NewNumber = await GenerateNextNumberAsync(_orderId);
        await ReloadInvoicesAsync();
    });

    [RelayCommand(CanExecute = nameof(CanPay))]
    private Task PayAsync() => RunAsync(async () =>
    {
        if (SelectedInvoice is null) return;
        if (!Dialog.Confirm($"Отметить оплату счёта {SelectedInvoice.Number}?", "Оплата"))
            return;
        await _invoices.MarkPaidAsync(SelectedInvoice.Id);
        await ReloadInvoicesAsync();
    });

    [RelayCommand(CanExecute = nameof(CanDelete))]
    private Task DeleteAsync() => RunAsync(async () =>
    {
        if (SelectedInvoice is null) return;
        if (!Dialog.Confirm($"Удалить счёт {SelectedInvoice.Number}?", "Удаление счёта"))
            return;
        await _invoices.DeleteInvoiceAsync(SelectedInvoice.Id);
        await ReloadInvoicesAsync();
    });

    [RelayCommand]
    private void Close() => CloseRequested?.Invoke();

    private bool CanPay => SelectedInvoice is { } i && i.PaymentStatus != PaymentStatus.Paid && i.PaymentStatus != PaymentStatus.Cancelled;
    private bool CanDelete => SelectedInvoice is { } i && i.PaymentStatus != PaymentStatus.Paid;

    private async Task ReloadInvoicesAsync()
    {
        var list = await _invoices.GetInvoicesByOrderAsync(_orderId);
        Items.Clear();
        foreach (var i in list) Items.Add(i);
        SelectedInvoice = null;
    }

    private List<string> ValidateNew(out decimal amount, out DateTimeOffset? due)
    {
        amount = 0m; due = null;
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(NewNumber)) errors.Add("Укажите номер счёта.");
        if (!decimal.TryParse(NewAmountText?.Trim(), NumberStyles.Number, CultureInfo.CurrentCulture, out amount) || amount <= 0)
            errors.Add("Сумма счёта должна быть положительным числом.");
        if (!string.IsNullOrWhiteSpace(NewDueDateText))
        {
            if (!DateTimeOffset.TryParse(NewDueDateText.Trim(), CultureInfo.CurrentCulture, DateTimeStyles.None, out var d))
                errors.Add("Срок оплаты: введите дату в формате дд.мм.гггг.");
            else due = d;
        }
        return errors;
    }

    private async Task<string> GenerateNextNumberAsync(Guid orderId)
    {
        var existing = await _invoices.GetInvoicesByOrderAsync(orderId);
        var today = DateTime.UtcNow.ToString("yyyyMMdd");
        var n = existing.Count(i => i.Number.StartsWith($"INV-{today}-", StringComparison.Ordinal));
        return $"INV-{today}-{n + 1:D4}";
    }
}
