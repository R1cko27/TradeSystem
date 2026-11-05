// ViewModels/SupplierProducts/SupplierProductsViewModels.cs

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using TradeSystem.App.ViewModels.Abstractions;
using TradeSystem.Application.Contracts.Services;
using TradeSystem.Application.Dto;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace TradeSystem.App.ViewModels.SupplierProducts;

// Плоский список связей «товар — поставщик» с фильтрами.
// Показывает случай, когда у одного товара несколько поставщиков.
public partial class SupplierProductsListViewModel : ViewModelBase, IActivatable
{
    private readonly ICatalogService _catalog; // Сервис справочников
    private IReadOnlyList<SupplierProductDto> _all = Array.Empty<SupplierProductDto>(); // Полный список до фильтрации

    public ObservableCollection<SupplierProductDto> Items { get; } = new(); // Отображаемые связи

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
    private SupplierProductDto? _selectedItem; // Выбранная связь

    [ObservableProperty] private string _searchText = string.Empty; // Строка поиска
    [ObservableProperty] private bool _showInactive; // Показывать неактивные связи

    // Фильтры хранят выбранные элементы списка, чтобы ComboBox показывал названия.
    [ObservableProperty] private ProductDto? _selectedProductFilter; // Выбранный товар-фильтр
    [ObservableProperty] private SupplierDto? _selectedSupplierFilter; // Выбранный поставщик-фильтр

    // Источники для ComboBox-фильтров (первый элемент — «все»).
    [ObservableProperty] private IReadOnlyList<ProductDto> _productFilters = Array.Empty<ProductDto>(); // Товары для фильтра
    [ObservableProperty] private IReadOnlyList<SupplierDto> _supplierFilters = Array.Empty<SupplierDto>(); // Поставщики для фильтра

    public event Action? AddRequested; // Запрос на добавление
    public event Action<Guid?>? EditRequested; // Запрос на редактирование

    public SupplierProductsListViewModel(ICatalogService catalog, IDialogService dialog)
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
    // Удаляет связь после подтверждения.
    private Task DeleteAsync()
    {
        var target = SelectedItem;
        if (target is null) return Task.CompletedTask;

        if (!Dialog.Confirm(
                $"Удалить связь «{target.ProductName} ↔ {target.SupplierName}»?",
                "Удаление поставки"))
            return Task.CompletedTask;

        return RunAsync(async () =>
        {
            await _catalog.UnlinkAsync(target.Id);
            SelectedItem = null;
            await LoadCoreAsync();
        });
    }

    private bool HasSelection => SelectedItem is not null; // Есть ли выбранная строка

    // Все фильтры локальные — не дёргаем хранилище повторно.
    partial void OnSearchTextChanged(string value) => ApplyFilter();
    partial void OnSelectedProductFilterChanged(ProductDto? value) => ApplyFilter();
    partial void OnSelectedSupplierFilterChanged(SupplierDto? value) => ApplyFilter();
    partial void OnShowInactiveChanged(bool value) => ApplyFilter();

    // Загружает связи без RunAsync: вызывается из уже активного RunAsync.
    private async Task LoadCoreAsync()
    {
        _all = await _catalog.GetSupplierProductsAsync();

        var productsTask = _catalog.GetProductsAsync(includeInactive: false);
        var suppliersTask = _catalog.GetSuppliersAsync(includeInactive: false);
        await Task.WhenAll(productsTask, suppliersTask);

        // Запоминаем текущий выбор, чтобы обновление списка его не сбрасывало.
        var productId = SelectedProductFilter?.Id;
        var supplierId = SelectedSupplierFilter?.Id;

        ProductFilters = BuildProductFilters(await productsTask);
        SupplierFilters = BuildSupplierFilters(await suppliersTask);

        SelectedProductFilter = ProductFilters.FirstOrDefault(p => p.Id == productId) ?? ProductFilters[0];
        SelectedSupplierFilter = SupplierFilters.FirstOrDefault(s => s.Id == supplierId) ?? SupplierFilters[0];

        ApplyFilter();
    }

    // Добавляет в начало пункт «Все товары».
    private static IReadOnlyList<ProductDto> BuildProductFilters(IReadOnlyList<ProductDto> real)
    {
        var list = new List<ProductDto>(real.Count + 1)
        {
            new() { Id = Guid.Empty, Name = "— Все товары —" }
        };
        list.AddRange(real);
        return list;
    }

    // Добавляет в начало пункт «Все поставщики».
    private static IReadOnlyList<SupplierDto> BuildSupplierFilters(IReadOnlyList<SupplierDto> real)
    {
        var list = new List<SupplierDto>(real.Count + 1)
        {
            new() { Id = Guid.Empty, Name = "— Все поставщики —" }
        };
        list.AddRange(real);
        return list;
    }

    // Применяет фильтры активности, товара, поставщика и поиска.
    private void ApplyFilter()
    {
        IEnumerable<SupplierProductDto> query = _all;

        if (!ShowInactive)
            query = query.Where(x => x.IsActive);

        if (SelectedProductFilter is { } product && product.Id != Guid.Empty)
            query = query.Where(x => x.ProductId == product.Id);

        if (SelectedSupplierFilter is { } supplier && supplier.Id != Guid.Empty)
            query = query.Where(x => x.SupplierId == supplier.Id);

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var s = SearchText.Trim();
            query = query.Where(x =>
                Contains(x.ProductName, s) ||
                Contains(x.SupplierName, s) ||
                Contains(x.SupplierArticle, s));
        }

        Items.Clear();
        foreach (var item in query
                     .OrderBy(x => x.ProductName, StringComparer.CurrentCultureIgnoreCase)
                     .ThenBy(x => x.SupplierName, StringComparer.CurrentCultureIgnoreCase))
        {
            Items.Add(item);
        }
    }

    private static bool Contains(string? source, string value) =>
        !string.IsNullOrEmpty(source) && source.Contains(value, StringComparison.CurrentCultureIgnoreCase);
}

// Создание и редактирование связи «товар — поставщик».
public partial class SupplierProductEditViewModel : ViewModelBase
{
    private readonly ICatalogService _catalog; // Сервис справочников
    private Guid? _linkId; // Id редактируемой связи (null — новая)

    [ObservableProperty] private bool _isNew = true; // Признак новой записи
    [ObservableProperty] private Guid _productId; // Товар
    [ObservableProperty] private Guid _supplierId; // Поставщик
    [ObservableProperty] private string? _supplierArticle; // Артикул у поставщика
    [ObservableProperty] private string _purchasePriceText = string.Empty; // Цена закупки (текст)
    [ObservableProperty] private string _leadTimeText = string.Empty; // Срок поставки (текст)
    [ObservableProperty] private string _minOrderText = string.Empty; // Минимальная партия (текст)
    [ObservableProperty] private string _multipleText = string.Empty; // Кратность (текст)
    [ObservableProperty] private bool _isPreferred; // Предпочтительный поставщик
    [ObservableProperty] private bool _isActive = true; // Признак активности
    [ObservableProperty] private string? _note; // Примечание

    [ObservableProperty] private IReadOnlyList<ProductDto> _products = Array.Empty<ProductDto>(); // Товары для ComboBox
    [ObservableProperty] private IReadOnlyList<SupplierDto> _suppliers = Array.Empty<SupplierDto>(); // Поставщики для ComboBox

    public event Action? CloseRequested; // Запрос на закрытие

    public SupplierProductEditViewModel(ICatalogService catalog, IDialogService dialog)
        : base(dialog)
    {
        _catalog = catalog;
    }

    // Загружает форму; id == null — новая связь.
    public async Task LoadAsync(Guid? id)
    {
        await RunAsync(async () =>
        {
            _linkId = id;
            IsNew = id is null;

            // Списки для ComboBox (без «все») — грузим активные.
            Products = await _catalog.GetProductsAsync(includeInactive: false);
            Suppliers = await _catalog.GetSuppliersAsync(includeInactive: false);

            if (id is null)
            {
                ResetForm();
                return;
            }

            var all = await _catalog.GetSupplierProductsAsync();
            var link = all.FirstOrDefault(x => x.Id == id.Value);
            if (link is null)
            {
                Dialog.ShowError("Связь не найдена.");
                ResetForm();
                return;
            }

            ProductId = link.ProductId;
            SupplierId = link.SupplierId;
            SupplierArticle = link.SupplierArticle;
            PurchasePriceText = link.PurchasePrice?.ToString(CultureInfo.CurrentCulture) ?? string.Empty;
            LeadTimeText = link.LeadTimeDays?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            MinOrderText = link.MinimumOrderQuantity?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            MultipleText = link.OrderMultiple?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            IsPreferred = link.IsPreferred;
            IsActive = link.IsActive;
            Note = link.Note;
        });
    }

    [RelayCommand]
    private Task SaveAsync() => RunAsync(SaveImplementationAsync);

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke();

    // Проверяет поля и сохраняет связь.
    private async Task SaveImplementationAsync()
    {
        var errors = Validate(out decimal? price, out int? lead, out int? min, out int? mult);
        if (errors.Count > 0)
        {
            Dialog.ShowError(string.Join(Environment.NewLine, errors));
            return;
        }

        var input = new SupplierProductInput
        {
            Id = _linkId,
            ProductId = ProductId,
            SupplierId = SupplierId,
            SupplierArticle = TrimOrNull(SupplierArticle),
            PurchasePrice = price,
            LeadTimeDays = lead,
            MinimumOrderQuantity = min,
            OrderMultiple = mult,
            IsPreferred = IsPreferred,
            IsActive = IsActive,
            Note = TrimOrNull(Note)
        };

        if (IsNew)
            await _catalog.LinkProductToSupplierAsync(input);
        else
            await _catalog.UpdateLinkAsync(input);

        CloseRequested?.Invoke();
    }

    // Проверяет ссылки и разбирает числовые поля.
    private List<string> Validate(
        out decimal? price, out int? lead, out int? min, out int? mult)
    {
        price = lead = min = mult = null;
        var errors = new List<string>();

        if (ProductId == Guid.Empty)
            errors.Add("Выберите товар.");
        if (SupplierId == Guid.Empty)
            errors.Add("Выберите поставщика.");

        if (!TryParseDecimal(PurchasePriceText, out price))
            errors.Add("Цена закупки: введите число или оставьте поле пустым.");
        else if (price < 0)
            errors.Add("Цена закупки не может быть отрицательной.");

        if (!TryParseInt(LeadTimeText, out lead))
            errors.Add("Срок поставки: целое число дней или пусто.");
        else if (lead < 0)
            errors.Add("Срок поставки не может быть отрицательным.");

        if (!TryParseInt(MinOrderText, out min))
            errors.Add("Минимальное количество заказа: целое число или пусто.");
        else if (min < 0)
            errors.Add("Минимальное количество заказа не может быть отрицательным.");

        if (!TryParseInt(MultipleText, out mult))
            errors.Add("Кратность заказа: целое число или пусто.");
        else if (mult is < 1)
            errors.Add("Кратность заказа должна быть не меньше 1.");

        return errors;
    }

    // Очищает форму.
    private void ResetForm()
    {
        ProductId = Guid.Empty;
        SupplierId = Guid.Empty;
        SupplierArticle = null;
        PurchasePriceText = string.Empty;
        LeadTimeText = string.Empty;
        MinOrderText = string.Empty;
        MultipleText = string.Empty;
        IsPreferred = false;
        IsActive = true;
        Note = null;
    }

    // Разбирает число; пустая строка — null.
    private static bool TryParseDecimal(string? text, out decimal? value)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(text)) return true;
        if (decimal.TryParse(text.Trim(), NumberStyles.Number, CultureInfo.CurrentCulture, out var d))
        {
            value = d;
            return true;
        }
        return false;
    }

    // Разбирает целое; пустая строка — null.
    private static bool TryParseInt(string? text, out int? value)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(text)) return true;
        if (int.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var i))
        {
            value = i;
            return true;
        }
        return false;
    }

    // Обрезает строку; пустая превращается в null.
    private static string? TrimOrNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
