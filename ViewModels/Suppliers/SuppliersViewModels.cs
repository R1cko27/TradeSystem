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

// Список поставщиков с поиском и фильтром активности.
public partial class SuppliersListViewModel : ViewModelBase, IActivatable
{
    private readonly ICatalogService _catalog; // Сервис справочников
    private IReadOnlyList<SupplierDto> _all = Array.Empty<SupplierDto>(); // Полный список до фильтрации

    public ObservableCollection<SupplierDto> Items { get; } = new(); // Отображаемые поставщики

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
    private SupplierDto? _selectedItem; // Выбранный поставщик
    
    [ObservableProperty] private string _searchText = string.Empty; // Строка поиска
    [ObservableProperty] private bool _showInactive; // Показывать неактивных

    public event Action? AddRequested; // Запрос на добавление
    public event Action<Guid?>? EditRequested; // Запрос на редактирование

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

    // Удаляет выбранного поставщика после подтверждения.
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

    private bool HasSelection => SelectedItem is not null; // Есть ли выбранная строка

    // Поиск фильтрует локально; переключатель активности перечитывает из сервиса.
    partial void OnSearchTextChanged(string value) => ApplyFilter();
    partial void OnShowInactiveChanged(bool value) => _ = RunAsync(LoadCoreAsync);

    // Загружает поставщиков без RunAsync: вызывается из уже активного RunAsync.
    private async Task LoadCoreAsync()
    {
        _all = await _catalog.GetSuppliersAsync(ShowInactive);
        ApplyFilter();
    }

    // Фильтрует по названию, телефону и налоговому номеру.
    private void ApplyFilter()
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

// Создание и редактирование поставщика.
public partial class SupplierEditViewModel : ViewModelBase
{
    private readonly ICatalogService _catalog; // Сервис справочников
    private Guid? _supplierId; // Id редактируемого поставщика (null — новый)

    [ObservableProperty] private bool _isNew = true; // Признак новой записи
    [ObservableProperty] private string _name = string.Empty; // Наименование
    [ObservableProperty] private string? _taxId; // Налоговый номер
    [ObservableProperty] private string _phone = string.Empty; // Телефон
    [ObservableProperty] private string? _email; // Электронная почта
    [ObservableProperty] private bool _isActive = true; // Признак активности
    [ObservableProperty] private string? _note; // Примечание

    [ObservableProperty] private string? _country; // Страна
    [ObservableProperty] private string? _region; // Регион/область
    [ObservableProperty] private string? _city; // Город
    [ObservableProperty] private string? _street; // Улица
    [ObservableProperty] private string? _building; // Дом
    [ObservableProperty] private string? _apartment; // Квартира/офис
    [ObservableProperty] private string? _postalCode; // Почтовый индекс

    public event Action? CloseRequested; // Запрос на закрытие

    public SupplierEditViewModel(ICatalogService catalog, IDialogService dialog)
        : base(dialog)
    {
        _catalog = catalog;
    }

    // Загружает форму; id == null — новый поставщик.
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

    // Проверяет поля и сохраняет поставщика с адресом.
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

    // Проверяет обязательные поля.
    private List<string> Validate()
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(Name))
            errors.Add("Название фирмы обязательно.");
        if (string.IsNullOrWhiteSpace(Phone))
            errors.Add("Телефон обязателен.");
        return errors;
    }

    // Очищает форму.
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

    // Обрезает строку; пустая превращается в null.
    private static string? TrimOrNull(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return value.Trim();
    }
}
