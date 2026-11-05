// ViewModels/ViewModelBase.cs

using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using TradeSystem.App.ViewModels.Abstractions;
using TradeSystem.Application.Exceptions;

namespace TradeSystem.App.ViewModels;

// База ViewModel: флаг занятости и единая обработка ошибок.
public abstract partial class ViewModelBase : ObservableObject
{
    protected readonly IDialogService Dialog; // Сервис диалогов

    [ObservableProperty]
    private bool _isBusy; // Признак выполнения операции

    // Сохраняет сервис диалогов.
    protected ViewModelBase(IDialogService dialog)
    {
        Dialog = dialog ?? throw new ArgumentNullException(nameof(dialog));
    }

    // Выполняет действие, не запуская его повторно, и показывает ошибку пользователю.
    protected async Task RunAsync(Func<Task> action)
    {
        // Защита от повторного запуска.
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            await action();
        }
        catch (BusinessException ex)
        {
            Dialog.ShowError(ex.Message);
        }
        catch (Exception ex)
        {
            Dialog.ShowError($"Непредвиденная ошибка: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
