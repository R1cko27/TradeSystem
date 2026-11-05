// ViewModels/Abstractions/Abstractions.cs

using System.Threading.Tasks;
using TradeSystem.Application.Dto;

namespace TradeSystem.App.ViewModels.Abstractions;

// Экран, который перезагружает данные при активации.
public interface IActivatable
{
    Task OnActivatedAsync(); // Перезагрузить данные при активации ViewModel
}

// Диалоги, оторванные от WPF: ViewModel не знает о MessageBox.
// Реализация живёт в Presentation-слое.
public interface IDialogService
{
    void ShowInfo(string message, string title = "Информация");
    void ShowWarning(string message, string title = "Внимание");
    void ShowError(string message, string title = "Ошибка");
    bool Confirm(string message, string title = "Подтверждение");
}

// Печать и сохранение документа через UI-диалоги.
public interface IDocumentOutputService
{
    // Печатает документ; true — отправлено, false — отменено или недоступно.
    bool Print(ReportDocument document);

    // Сохраняет готовый текст в файл через диалог; true — сохранено.
    bool SaveDocument(string content, string suggestedFileName, string filter);
}
