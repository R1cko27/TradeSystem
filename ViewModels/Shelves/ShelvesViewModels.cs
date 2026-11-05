// ViewModels/Shelves/ShelvesViewModels.cs

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using TradeSystem.App.ViewModels.Abstractions;
using TradeSystem.Application.Contracts.Services;
using TradeSystem.Application.Dto;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace TradeSystem.App.ViewModels.Shelves;

public partial class ShelvesListViewModel : ViewModelBase, IActivatable
{
    private readonly ICatalogService _catalog;
    private IReadOnlyList<ShelfDto> _all = Array.Empty<ShelfDto>();

    public ObservableCollection<ShelfDto> Items { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
    private ShelfDto? _selectedItem;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private bool _showInactive;

    public event Action? AddRequested;
    public event Action<Guid?>? EditRequested;

    public ShelvesListViewModel(ICatalogService catalog, IDialogService dialog)
        : base(dialog)
    {
        _catalog = catalog;
    }

    public Task OnActivatedAsync() => RunAsync(LoadCoreAsync); // Загрузить данные при активации ViewModel

    [RelayCommand]
    private Task RefreshAsync() => RunAsync(LoadCoreAsync);

    [RelayCommand]
    private void Add() => AddRequested?.Invoke();

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void Edit()
    {
        if (SelectedItem is not null)
            EditRequested?.Invoke(SelectedItem.Id);
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private Task DeleteAsync()
    {
        var target = SelectedItem;
        if (target is null) return Task.CompletedTask;

        if (!Dialog.Confirm($"Удалить полку «{target.Name}» ({target.Code})?", "Удаление полки"))
            return Task.CompletedTask;

        return RunAsync(async () =>
        {
            await _catalog.DeleteShelfAsync(target.Id);
            SelectedItem = null;
            await LoadCoreAsync();
        });
    }

    private bool HasSelection => SelectedItem is not null;

    partial void OnSearchTextChanged(string value) => ApplyFilter();
    partial void OnShowInactiveChanged(bool value) => _ = RunAsync(LoadCoreAsync);

    /// <summary>Загрузка без RunAsync: вызывается из мест, где RunAsync уже активен.</summary>
    private async Task LoadCoreAsync() // Загрузить полки и применить фильтр
    {
        _all = await _catalog.GetShelvesAsync(ShowInactive);
        ApplyFilter();
    }

    private void ApplyFilter() // Применить фильтр поиска
    {
        IEnumerable<ShelfDto> query = _all;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var s = SearchText.Trim();
            query = query.Where(x =>
                Contains(x.Code, s) || Contains(x.Name, s) || Contains(x.Location, s));
        }

        Items.Clear();
        foreach (var item in query.OrderBy(x => x.Code, StringComparer.CurrentCultureIgnoreCase))
            Items.Add(item);
    }

    private static bool Contains(string? source, string value) =>
        !string.IsNullOrEmpty(source) && source.Contains(value, StringComparison.CurrentCultureIgnoreCase);
}

public partial class ShelfEditViewModel : ViewModelBase
{
    private readonly ICatalogService _catalog;
    private Guid? _shelfId;

    [ObservableProperty] private bool _isNew = true;
    [ObservableProperty] private string _code = string.Empty;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string? _location;
    [ObservableProperty] private bool _isActive = true;
    [ObservableProperty] private string? _note;

    public event Action? CloseRequested;

    public ShelfEditViewModel(ICatalogService catalog, IDialogService dialog)
        : base(dialog)
    {
        _catalog = catalog;
    }

    public async Task LoadAsync(Guid? id)
    {
        await RunAsync(async () =>
        {
            _shelfId = id;
            IsNew = id is null;

            if (id is null)
            {
                ResetForm();
                return;
            }

            var sh = await _catalog.GetShelfAsync(id.Value);
            if (sh is null)
            {
                Dialog.ShowError("Полка не найдена.");
                ResetForm();
                return;
            }

            Code = sh.Code;
            Name = sh.Name;
            Location = sh.Location;
            IsActive = sh.IsActive;
            Note = sh.Note;
        });
    }

    [RelayCommand]
    private Task SaveAsync() => RunAsync(SaveImplementationAsync);

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke();

    private async Task SaveImplementationAsync()
    {
        var errors = Validate();
        if (errors.Count > 0)
        {
            Dialog.ShowError(string.Join(Environment.NewLine, errors));
            return;
        }

        var input = new ShelfInput
        {
            Id = _shelfId,
            Code = Code.Trim(),
            Name = Name.Trim(),
            Location = TrimOrNull(Location),
            IsActive = IsActive,
            Note = TrimOrNull(Note)
        };

        if (IsNew)
            await _catalog.CreateShelfAsync(input);
        else
            await _catalog.UpdateShelfAsync(input);

        CloseRequested?.Invoke();
    }

    private List<string> Validate()
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(Code))
            errors.Add("Код полки обязателен.");
        if (string.IsNullOrWhiteSpace(Name))
            errors.Add("Название полки обязательно.");
        return errors;
    }

    private void ResetForm()
    {
        Code = string.Empty;
        Name = string.Empty;
        Location = null;
        IsActive = true;
        Note = null;
    }

    private static string? TrimOrNull(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return value.Trim();
    }
}
