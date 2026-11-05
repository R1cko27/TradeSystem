// ViewModels/Suppliers/SuppliersViewModels.cs

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using TradeSystem.App.ViewModels.Abstractions;
using TradeSystem.Application.Contracts.Services;
using TradeSystem.Application.Dto;
using TradeSystem.Domain.ValueObjects;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace TradeSystem.App.ViewModels.Suppliers;

public partial class SuppliersListViewModel : ViewModelBase, IActivatable
{
    private readonly ICatalogService _catalog;
    private IReadOnlyList<SupplierDto> _all = Array.Empty<SupplierDto>();

    public ObservableCollection<SupplierDto> Items { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
    private SupplierDto? _selectedItem;
    
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private bool _showInactive;

    public event Action? AddRequested;
    public event Action<Guid?>? EditRequested;

    public SuppliersListViewModel(ICatalogService catalog, IDialogService dialog)
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

        if (!Dialog.Confirm($"Удалить поставщика «{target.Name}»?", "Удаление поставщика"))
            return Task.CompletedTask;

        return RunAsync(async () =>
        {
            await _catalog.DeleteSupplierAsync(target.Id);
            SelectedItem = null;
            await LoadCoreAsync();
        });
    }

    private bool HasSelection => SelectedItem is not null;

    // Поиск фильтрует локально; переключатель активности перечитывает из сервиса.
    partial void OnSearchTextChanged(string value) => ApplyFilter();
    partial void OnShowInactiveChanged(bool value) => _ = RunAsync(LoadCoreAsync);

    /// <summary>Загрузка без RunAsync: вызывается из мест, где RunAsync уже активен.</summary>
    private async Task LoadCoreAsync() // Загрузить поставщиков и применить фильтр
    {
        _all = await _catalog.GetSuppliersAsync(ShowInactive);
        ApplyFilter();
    }

    private void ApplyFilter() // Применить фильтр поиска
    {
        IEnumerable<SupplierDto> query = _all;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var s = SearchText.Trim();
            query = query.Where(x =>
                Contains(x.Name, s) || Contains(x.Phone, s) || Contains(x.TaxId, s));
        }

        Items.Clear();
        foreach (var item in query.OrderBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase))
            Items.Add(item);
    }

    private static bool Contains(string? source, string value) =>
        !string.IsNullOrEmpty(source) && source.Contains(value, StringComparison.CurrentCultureIgnoreCase);
}

public partial class SupplierEditViewModel : ViewModelBase
{
    private readonly ICatalogService _catalog;
    private Guid? _supplierId;

    [ObservableProperty] private bool _isNew = true;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string? _taxId;
    [ObservableProperty] private string _phone = string.Empty;
    [ObservableProperty] private string? _email;
    [ObservableProperty] private bool _isActive = true;
    [ObservableProperty] private string? _note;

    [ObservableProperty] private string? _country;
    [ObservableProperty] private string? _region;
    [ObservableProperty] private string? _city;
    [ObservableProperty] private string? _street;
    [ObservableProperty] private string? _building;
    [ObservableProperty] private string? _apartment;
    [ObservableProperty] private string? _postalCode;

    public event Action? CloseRequested;

    public SupplierEditViewModel(ICatalogService catalog, IDialogService dialog)
        : base(dialog)
    {
        _catalog = catalog;
    }

    public async Task LoadAsync(Guid? id)
    {
        await RunAsync(async () =>
        {
            _supplierId = id;
            IsNew = id is null;

            if (id is null)
            {
                ResetForm();
                return;
            }

            var s = await _catalog.GetSupplierAsync(id.Value);
            if (s is null)
            {
                Dialog.ShowError("Поставщик не найден.");
                ResetForm();
                return;
            }

            Name = s.Name;
            TaxId = s.TaxId;
            Phone = s.Phone;
            Email = s.Email;
            IsActive = s.IsActive;
            Note = s.Note;
            Country = s.Country;
            Region = s.Region;
            City = s.City;
            Street = s.Street;
            Building = s.Building;
            Apartment = s.Apartment;
            PostalCode = s.PostalCode;
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

        var input = new SupplierInput
        {
            Id = _supplierId,
            Name = Name.Trim(),
            TaxId = TrimOrNull(TaxId),
            Phone = Phone.Trim(),
            Email = TrimOrNull(Email),
            IsActive = IsActive,
            Note = TrimOrNull(Note),
            Address = new Address
            {
                Country = TrimOrNull(Country),
                Region = TrimOrNull(Region),
                City = TrimOrNull(City),
                Street = TrimOrNull(Street),
                Building = TrimOrNull(Building),
                Apartment = TrimOrNull(Apartment),
                PostalCode = TrimOrNull(PostalCode)
            }
        };

        if (IsNew)
            await _catalog.CreateSupplierAsync(input);
        else
            await _catalog.UpdateSupplierAsync(input);

        CloseRequested?.Invoke();
    }

    private List<string> Validate()
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(Name))
            errors.Add("Название фирмы обязательно.");
        if (string.IsNullOrWhiteSpace(Phone))
            errors.Add("Телефон обязателен.");
        return errors;
    }

    private void ResetForm()
    {
        Name = string.Empty;
        TaxId = null;
        Phone = string.Empty;
        Email = null;
        IsActive = true;
        Note = null;
        Country = Region = City = Street = Building = Apartment = PostalCode = null;
    }

    private static string? TrimOrNull(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return value.Trim();
    }
}
