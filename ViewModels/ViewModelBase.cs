// ViewModels/ViewModelBase.cs

using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using TradeSystem.App.ViewModels.Abstractions;
using TradeSystem.Application.Exceptions;

namespace TradeSystem.App.ViewModels;

/// <summary>
/// Общая база всех ViewModel: флаг занятости и единая обработка ошибок
/// бизнес-правил (BusinessException) и непредвиденных сбоев.
/// </summary>
public abstract partial class ViewModelBase : ObservableObject
{
    protected readonly IDialogService Dialog;

    [ObservableProperty]
    private bool _isBusy;

    protected ViewModelBase(IDialogService dialog)
    {
        Dialog = dialog ?? throw new ArgumentNullException(nameof(dialog));
    }

    /// <summary>
    /// Выполняет асинхронное действие, скрывая шаблон try/catch/finally
    /// и переводя исключения в понятные пользователю сообщения.
    /// </summary>
    protected async Task RunAsync(Func<Task> action)
    {
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
