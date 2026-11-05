// Application/Reports/ReportDocuments.cs

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TradeSystem.Application.Dto;

namespace TradeSystem.Application.Reports;

// Фабрика справочных документов из DTO: чистые функции без состояния.
public static class ReportDocuments
{
    private static readonly CultureInfo Cul = CultureInfo.CurrentCulture; // Текущая культура для форматирования чисел

    // Все товары: остатки, минимумы, поставщики и признак пополнения.
    public static ReportDocument FromAllProducts(IReadOnlyList<ProductDto> items)
    {
        var headers = new[] { "Наименование", "Артикул", "Ед.", "Остаток", "Мин.", "Целевой", "Поставщиков", "Пополнить" };

        var rows = items.Select(p => new[]
        {
            p.Name,
            p.Article,
            p.UnitLabel,
            Num(p.CurrentStock),
            Num(p.MinimumStockQuantity),
            p.TargetStockQuantity is { } t ? Num(t) : "—",
            Num(p.SuppliersCount),
            YesNo(p.NeedsReplenish)
        }).ToList();

        return new ReportDocument("Список всех товаров", headers, rows);
    }

    // Товары в наличии: остаток и полки хранения.
    public static ReportDocument FromAvailableProducts(IReadOnlyList<ProductDto> items)
    {
        var headers = new[] { "Наименование", "Артикул", "Ед.", "Остаток", "Полка хранения" };

        var rows = items.Select(p => new[]
        {
            p.Name,
            p.Article,
            p.UnitLabel,
            Num(p.CurrentStock),
            string.IsNullOrWhiteSpace(p.StorageShelves) ? "—" : p.StorageShelves
        }).ToList();

        return new ReportDocument("Товары, имеющиеся в наличии", headers, rows);
    }

    // Товары к пополнению: минимум и рекомендуемое количество.
    public static ReportDocument FromToReplenish(IReadOnlyList<LowStockProductDto> items)
    {
        var headers = new[] { "Наименование", "Ед.", "Текущий остаток", "Минимум", "Рекомендуется заказать" };

        var rows = items.Select(p => new[]
        {
            p.ProductName,
            p.UnitLabel,
            Num(p.CurrentStock),
            Num(p.MinimumStockQuantity),
            Num(p.RecommendedOrderQuantity)
        }).ToList();

        return new ReportDocument("Товары, количество которых необходимо пополнить", headers, rows);
    }

    // Товары выбранного поставщика с условиями закупки.
    public static ReportDocument FromSupplierProducts(string supplierName, IReadOnlyList<SupplierProductDto> items)
    {
        var headers = new[] { "Товар", "Артикул поставщика", "Цена закупки", "Срок, дн.", "Мин. заказ", "Кратность", "Предпочт." };

        var rows = items.Select(s => new[]
        {
            s.ProductName,
            s.SupplierArticle ?? "—",
            s.PurchasePrice is { } price ? price.ToString("N2", Cul) : "—",
            s.LeadTimeDays is { } d ? Num(d) : "—",
            s.MinimumOrderQuantity is { } m ? Num(m) : "—",
            s.OrderMultiple is { } k ? Num(k) : "—",
            YesNo(s.IsPreferred)
        }).ToList();

        return new ReportDocument($"Товары, поставляемые: {supplierName}", headers, rows);
    }

    // Число в текущей культуре.
    private static string Num(int value) => value.ToString(Cul);
    private static string YesNo(bool value) => value ? "Да" : ""; // «Да» для true, иначе пустая строка
}
