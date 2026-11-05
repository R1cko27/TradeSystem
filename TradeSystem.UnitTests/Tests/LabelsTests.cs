// TradeSystem.UnitTests/Tests/LabelsTests.cs

using TradeSystem.Application.Mappers;
using TradeSystem.Domain.Enums;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TradeSystem.UnitTests.Tests;

/// <summary>Проверки русских названий перечислений.</summary>
[TestClass]
public sealed class LabelsTests
{
    // Ожидаем: штука обозначается «шт.», килограмм «кг», литр «л».
    [TestMethod]
    public void Units_ReturnRussianLabels()
    {
        Check.Equal("шт.", MeasurementUnit.Piece.Of());
        Check.Equal("кг", MeasurementUnit.Kilogram.Of());
        Check.Equal("л", MeasurementUnit.Liter.Of());
    }

    // Ожидаем: метр «м», пачка «уп.», коробка «кор.», другое «др.».
    [TestMethod]
    public void Units_ReturnOtherLabels()
    {
        Check.Equal("м", MeasurementUnit.Meter.Of());
        Check.Equal("уп.", MeasurementUnit.Pack.Of());
        Check.Equal("кор.", MeasurementUnit.Box.Of());
        Check.Equal("др.", MeasurementUnit.Other.Of());
    }

    [TestMethod]
    public void Units_NotSpecified_ReturnsText()
    {
        Check.Equal("не указано", MeasurementUnit.NotSpecified.Of());
    }

    // Ожидаем: черновик «Черновик», подтверждён «Подтверждён», получен «Получен».
    [TestMethod]
    public void OrderStatuses_ReturnRussianLabels()
    {
        Check.Equal("Черновик", PurchaseOrderStatus.Draft.Of());
        Check.Equal("Подтверждён", PurchaseOrderStatus.Confirmed.Of());
        Check.Equal("Получен", PurchaseOrderStatus.Received.Of());
    }

    // Ожидаем: не оплачен «Не оплачен», частично «Частично оплачен», оплачен «Оплачен».
    [TestMethod]
    public void PaymentStatuses_ReturnRussianLabels()
    {
        Check.Equal("Не оплачен", PaymentStatus.Unpaid.Of());
        Check.Equal("Частично оплачен", PaymentStatus.PartiallyPaid.Of());
        Check.Equal("Оплачен", PaymentStatus.Paid.Of());
    }

    [TestMethod]
    public void ReceiptStatuses_ReturnRussianLabels()
    {
        Check.Equal("Черновик", GoodsReceiptStatus.Draft.Of());
        Check.Equal("Проведён", GoodsReceiptStatus.Posted.Of());
    }

    [TestMethod]
    public void SaleStatuses_ReturnRussianLabels()
    {
        Check.Equal("Черновик", SaleStatus.Draft.Of());
        Check.Equal("Завершена", SaleStatus.Completed.Of());
    }

    // Ожидаем: возвращается числовое представление, а не пустая строка.
    [TestMethod]
    public void UnknownValue_FallsBackToNumber()
    {
        var label = ((MeasurementUnit)77).Of();

        Check.Equal("77", label);
    }
}
