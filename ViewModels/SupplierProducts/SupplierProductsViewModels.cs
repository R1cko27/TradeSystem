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

/// <summary>
/// Плоский список связей «товар-поставщик» с фильтрами по товару, поставщику,
/// поиском и переключателем показа неактивных связей.
/// Наглядно демонстрирует случай «один товар — несколько поставщиков».
/// </summary>
public partial class SupplierProductsListViewModel : ViewModelBase, IActivatable
{
    private readonly ICatalogService _catalog;
    private IReadOnlyList<SupplierProductDto> _all = Array.Empty<SupplierProductDto>();

    public ObservableCollection<SupplierProductDto> Items { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
    private SupplierProductDto? _selectedItem;

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private bool _showInactive;

    // Фильтры по товару и поставщику: выбраны сами элементы списка, чтобы ComboBox
    [ObservableProperty] private ProductDto? _selectedProductFilter; // Выбранный товар-фильтр
    [ObservableProperty] private SupplierDto? _selectedSupplierFilter; // Выбранный поставщик-фильтр

    // Источники для ComboBox-фильтров (первый элемент — «все»).
    [ObservableProperty] private IReadOnlyList<ProductDto> _productFilters = Array.Empty<ProductDto>();
    [ObservableProperty] private IReadOnlyList<SupplierDto> _supplierFilters = Array.Empty<SupplierDto>();

    public event Action? AddRequested;
    public event Action<Guid?>? EditRequested;

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

    private bool HasSelection => SelectedItem is not null;

    // Все фильтры локальные — не дёргаем хранилище повторно.
    partial void OnSearchTextChanged(string value) => ApplyFilter();
    partial void OnSelectedProductFilterChanged(ProductDto? value) => ApplyFilter();
    partial void OnSelectedSupplierFilterChanged(SupplierDto? value) => ApplyFilter();
    partial void OnShowInactiveChanged(bool value) => ApplyFilter();

    /// <summary>Загрузка без RunAsync: вызывается из мест, где RunAsync уже активен.</summary>
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

    private static IReadOnlyList<ProductDto> BuildProductFilters(IReadOnlyList<ProductDto> real)
    {
        var list = new List<ProductDto>(real.Count + 1)
        {
            new() { Id = Guid.Empty, Name = "— Все товары —" }
        };
        list.AddRange(real);
        return list;
    }

    private static IReadOnlyList<SupplierDto> BuildSupplierFilters(IReadOnlyList<SupplierDto> real)
    {
        var list = new List<SupplierDto>(real.Count + 1)
        {
            new() { Id = Guid.Empty, Name = "— Все поставщики —" }
        };
        list.AddRange(real);
        return list;
    }

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

public partial class SupplierProductEditViewModel : ViewModelBase
{
    private readonly ICatalogService _catalog;
    private Guid? _linkId;

    [ObservableProperty] private bool _isNew = true;
    [ObservableProperty] private Guid _productId;
    [ObservableProperty] private Guid _supplierId;
    [ObservableProperty] private string? _supplierArticle;
    [ObservableProperty] private string _purchasePriceText = string.Empty;
    [ObservableProperty] private string _leadTimeText = string.Empty;
    [ObservableProperty] private string _minOrderText = string.Empty;
    [ObservableProperty] private string _multipleText = string.Empty;
    [ObservableProperty] private bool _isPreferred;
    [ObservableProperty] private bool _isActive = true;
    [ObservableProperty] private string? _note;

    [ObservableProperty] private IReadOnlyList<ProductDto> _products = Array.Empty<ProductDto>();
    [ObservableProperty] private IReadOnlyList<SupplierDto> _suppliers = Array.Empty<SupplierDto>();

    public event Action? CloseRequested;

    public SupplierProductEditViewModel(ICatalogService catalog, IDialogService dialog)
        : base(dialog)
    {
        _catalog = catalog;
    }

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

    private static string? TrimOrNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
