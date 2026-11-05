// ViewModels/Reports/ReportsViewModels.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TradeSystem.App.ViewModels.Abstractions;
using TradeSystem.Application.Contracts.Services;
using TradeSystem.Application.Dto;
using TradeSystem.Application.Reports;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace TradeSystem.App.ViewModels.Reports;

// Экран отчётов: выбор вида, предпросмотр, печать и экспорт.
public partial class ReportsViewModel : ViewModelBase, IActivatable
{
    private readonly IReportService _reports; // Сервис отчётов
    private readonly ICatalogService _catalog; // Сервис справочников
    private readonly IDocumentOutputService _output; // Сервис печати и сохранения

    private ReportDocument? _document; // Сформированный документ (обычное поле, не биндится)

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSupplierReport))]
    private ReportKind _selectedKind = ReportKind.AllProducts; // Выбранный вид отчёта

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GenerateCommand))]
    private Guid _selectedSupplierId; // Поставщик для отчёта

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PrintCommand))]
    [NotifyCanExecuteChangedFor(nameof(SaveTextCommand))]
    [NotifyCanExecuteChangedFor(nameof(SaveCsvCommand))]
    private string _previewText = string.Empty; // Текст предпросмотра

    [ObservableProperty] private IReadOnlyList<SupplierDto> _suppliers = Array.Empty<SupplierDto>(); // Поставщики для выбора

    public IReadOnlyList<ReportKindOption> AvailableKinds { get; } = new[] // Виды отчётов для ComboBox
    {
        new ReportKindOption(ReportKind.AllProducts, "Список всех товаров"),
        new ReportKindOption(ReportKind.Available, "Товары в наличии"),
        new ReportKindOption(ReportKind.ToReplenish, "Товары к пополнению"),
        new ReportKindOption(ReportKind.SupplierProducts, "Товары данного поставщика")
    };

    public bool IsSupplierReport => SelectedKind == ReportKind.SupplierProducts; // Выбор поставщика виден только для отчёта по поставщику

    // Сохраняет сервисы отчётов, справочников и вывода.
    public ReportsViewModel(
        IReportService reports,
        ICatalogService catalog,
        IDocumentOutputService output,
        IDialogService dialog)
        : base(dialog)
    {
        _reports = reports;
        _catalog = catalog;
        _output = output;
    }

    public async Task OnActivatedAsync() // Загрузить данные при активации ViewModel
    {
        await RunAsync(async () =>
        {
            Suppliers = await _catalog.GetSuppliersAsync(includeInactive: false);
            // Сбрасываем предыдущий документ при входе, чтобы не печатать устаревшее.
            _document = null;
            PreviewText = string.Empty;
        });
    }

    [RelayCommand(CanExecute = nameof(CanGenerate))]
    // Формирует выбранный отчёт.
    private Task GenerateAsync() => RunAsync(GenerateImplementationAsync);

    private bool CanGenerate => // Для отчёта по поставщику нужен выбранный поставщик
        SelectedKind != ReportKind.SupplierProducts || SelectedSupplierId != Guid.Empty;

    // Строит документ в зависимости от вида отчёта.
    private async Task GenerateImplementationAsync()
    {
        ReportDocument doc = SelectedKind switch
        {
            ReportKind.AllProducts => ReportDocuments.FromAllProducts(await _reports.GetAllProductsReportAsync()),
            ReportKind.Available => ReportDocuments.FromAvailableProducts(await _reports.GetAvailableProductsReportAsync()),
            ReportKind.ToReplenish => ReportDocuments.FromToReplenish(await _reports.GetToReplenishReportAsync()),
            ReportKind.SupplierProducts => ReportDocuments.FromSupplierProducts(
                SupplierName(), await _reports.GetSupplierProductsReportAsync(SelectedSupplierId)),
            _ => throw new ArgumentOutOfRangeException()
        };

        _document = doc;
        PreviewText = doc.ToPlainText();
    }

    // Название выбранного поставщика.
    private string SupplierName() =>
        Suppliers.FirstOrDefault(s => s.Id == SelectedSupplierId)?.Name ?? "—";

    [RelayCommand(CanExecute = nameof(HasDocument))]
    // Отправляет документ на печать.
    private void Print()
    {
        if (_document is null) return;

        if (_output.Print(_document))
            Dialog.ShowInfo("Документ отправлен на печать.", "Печать");
        else
            Dialog.ShowWarning("Печать отменена или принтер недоступен.", "Печать");
    }

    [RelayCommand(CanExecute = nameof(HasDocument))]
    // Сохраняет отчёт текстом.
    private void SaveText() => Save(_document!.ToPlainText(), ".txt", "Текстовый файл|*.txt");

    [RelayCommand(CanExecute = nameof(HasDocument))]
    // Сохраняет отчёт в CSV.
    private void SaveCsv() => Save(_document!.ToCsv(), ".csv", "CSV (разделитель ;) |*.csv");

    // Сохраняет содержимое через диалог, делая имя файла безопасным.
    private void Save(string content, string extension, string filter)
    {
        if (_document is null) return;

        var safeTitle = string.Concat(_document.Title.Select(c => c == '/' || c == '\\' || c == ':' || c == '*' || c == '?' || c == '"' || c == '<' || c == '>' || c == '|' ? '_' : c));
        var fileName = safeTitle + extension;

        if (_output.SaveDocument(content, fileName, filter))
            Dialog.ShowInfo($"Документ сохранён: {fileName}", "Экспорт");
        else
            Dialog.ShowWarning("Сохранение отменено или недоступно.", "Экспорт");
    }

    private bool HasDocument => _document is not null; // Есть ли документ для печати и экспорта

    // Смена вида отчёта сбрасывает документ и пересчитывает доступность команд.
    partial void OnSelectedKindChanged(ReportKind value)
    {
        _document = null;
        PreviewText = string.Empty;
    }
}

// Виды справочных отчётов.
public enum ReportKind
{
    AllProducts = 0, // Все товары
    Available = 1, // Товары в наличии
    ToReplenish = 2, // Товары к пополнению
    SupplierProducts = 3 // Товары поставщика
}

// Пара «значение + подпись» для ComboBox.
public sealed record ReportKindOption(ReportKind Value, string Label);
