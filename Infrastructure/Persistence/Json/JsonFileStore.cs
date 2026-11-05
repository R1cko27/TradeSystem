// Infrastructure/Persistence/Json/JsonFileStore.cs

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace TradeSystem.Infrastructure.Persistence.Json;

// Хранилище списка записей в JSON-файле.
public interface IJsonFileStore<T>
{
    // Читает все записи.
    Task<IReadOnlyList<T>> ReadAsync(CancellationToken cancellationToken = default);

    // Перезаписывает файл.
    Task WriteAsync(IReadOnlyList<T> items, CancellationToken cancellationToken = default);

    // Читает, изменяет и записывает обратно.
    Task UpdateAsync(Func<List<T>, Task> mutateAsync, CancellationToken cancellationToken = default);
}

// JSON-хранилище с последовательным доступом через семафор.
public sealed class JsonFileStore<T> : IJsonFileStore<T>, IDisposable
{
    private readonly string _filePath; // Полный путь к файлу
    private readonly JsonSerializerOptions _options; // Настройки сериализации
    private readonly SemaphoreSlim _gate = new(1, 1); // Семафор для синхронизации доступа к файлу

    // Настраивает сериализацию: camelCase, отступы, enum строками.
    public JsonFileStore(string filePath, bool writeIndented = true)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path is required.", nameof(filePath));
        }

        _filePath = Path.GetFullPath(filePath);

        _options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = writeIndented,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, // Игнорировать null-значения при записи
            Converters =
            {
                new JsonStringEnumConverter()
            }
        };
    }

    // Читает файл под семафором.
    public async Task<IReadOnlyList<T>> ReadAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            return await ReadCoreAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    // Перезаписывает файл под семафором.
    public async Task WriteAsync(IReadOnlyList<T> items, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(items);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            await WriteCoreAsync(items, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    // Читает, даёт изменить список и записывает обратно под семафором.
    public async Task UpdateAsync(Func<List<T>, Task> mutateAsync, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mutateAsync);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var items = await ReadCoreAsync(cancellationToken).ConfigureAwait(false);

            await mutateAsync(items).ConfigureAwait(false);

            await WriteCoreAsync(items, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    // Читает файл без блокировки: вызывается только под семафором.
    private async Task<List<T>> ReadCoreAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_filePath))
        {
            return new List<T>();
        }

        var json = await File.ReadAllTextAsync(_filePath, cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<T>();
        }

        return JsonSerializer.Deserialize<List<T>>(json, _options) ?? new List<T>();
    }

    // Пишет файл без блокировки: вызывается только под семафором.
    private async Task WriteCoreAsync(IReadOnlyList<T> items, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_filePath);

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Сначала пишем во временный файл, затем заменяем основной.
        var tempPath = _filePath + ".tmp";

        var json = JsonSerializer.Serialize(items, _options);

        await File.WriteAllTextAsync(tempPath, json, cancellationToken).ConfigureAwait(false);

        File.Move(tempPath, _filePath, overwrite: true);
    }

    // Освобождает семафор.
    public void Dispose()
    {
        _gate.Dispose();
    }
}

// Настройки хранения JSON.
public sealed class JsonStorageOptions
{
    public string DirectoryPath { get; set; } = "data"; // Папка для JSON-файлов

    public bool WriteIndented { get; set; } = true; // Форматировать JSON с отступами
}

// Имена файлов для каждого типа сущностей.
public static class JsonFileNames
{
    public const string Suppliers = "suppliers.json"; // Поставщики
    public const string Products = "products.json"; // Товары
    public const string Shelves = "shelves.json"; // Полки
    public const string SupplierProducts = "supplier-products.json"; // Связи товара и поставщика
    public const string StockBalances = "stock-balances.json"; // Остатки на полках
    public const string PurchaseOrders = "purchase-orders.json"; // Заказы поставщикам
    public const string Invoices = "invoices.json"; // Счета
    public const string GoodsReceipts = "goods-receipts.json"; // Поступления
    public const string Sales = "sales.json"; // Продажи
    public const string StockMovements = "stock-movements.json"; // Движения товара
}
