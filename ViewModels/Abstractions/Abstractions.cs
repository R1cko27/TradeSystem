// ViewModels/Abstractions/Abstractions.cs

using System.Threading.Tasks;
using TradeSystem.Application.Dto;

namespace TradeSystem.App.ViewModels.Abstractions;

/// <summary>
/// Интерфейс для ViewModel, которым нужно перезагружать данные при активации.
/// </summary>
public interface IActivatable
{
    Task OnActivatedAsync(); // Перезагрузить данные при активации ViewModel
}

/// <summary>
/// Абстракция пользовательских диалогов. ViewModel не знает про MessageBox/WPF —
/// только про этот контракт. Реализация живёт в Views (UI-адаптер).
/// </summary>
public interface IDialogService
{
    void ShowInfo(string message, string title = "Информация");
    void ShowWarning(string message, string title = "Внимание");
    void ShowError(string message, string title = "Ошибка");
    bool Confirm(string message, string title = "Подтверждение");
}

/// <summary>
/// Доставка сформированного документа вовне: на принтер или в файл.
/// Обе операции требуют UI-взаимодействия (выбор принтера / пути), поэтому
/// контракт живёт в Presentation-абстракциях, а реализация — в Views.
/// ViewModel не знает про PrintDialog/SaveFileDialog/System.IO.
/// </summary>
public interface IDocumentOutputService
{
    /// <summary>Печатает документ через системный диалог. true — отправлено, false — отменено/недоступно.</summary>
    bool Print(ReportDocument document); // Напечатать документ через системный диалог

    /// <summary>
    /// Сохраняет готовый контент в файл через диалог «Сохранение как».
    /// Контент формирует вызывающая сторона (ViewModel), чтобы логика формата
    /// не утекала в Presentation. true — сохранено, false — отменено/ошибка.
    /// </summary>
    bool SaveDocument(string content, string suggestedFileName, string filter);
}
