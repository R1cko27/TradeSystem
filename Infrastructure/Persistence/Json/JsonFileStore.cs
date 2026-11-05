// Infrastructure/Persistence/Json/JsonFileStore.cs

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace TradeSystem.Infrastructure.Persistence.Json;

public interface IJsonFileStore<T>
{
    Task<IReadOnlyList<T>> ReadAsync(CancellationToken cancellationToken = default);

    Task WriteAsync(IReadOnlyList<T> items, CancellationToken cancellationToken = default);

    Task UpdateAsync(Func<List<T>, Task> mutateAsync, CancellationToken cancellationToken = default);
}

public sealed class JsonFileStore<T> : IJsonFileStore<T>, IDisposable
{
    private readonly string _filePath;
    private readonly JsonSerializerOptions _options;
    private readonly SemaphoreSlim _gate = new(1, 1); // Семафор для синхронизации доступа к файлу

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

    private async Task<List<T>> ReadCoreAsync(CancellationToken cancellationToken) // Внутреннее чтение данных из файла без блокировки
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

    private async Task WriteCoreAsync(IReadOnlyList<T> items, CancellationToken cancellationToken) // Внутренняя запись данных в файл без блокировки
    {
        var directory = Path.GetDirectoryName(_filePath);

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var tempPath = _filePath + ".tmp";

        var json = JsonSerializer.Serialize(items, _options);

        await File.WriteAllTextAsync(tempPath, json, cancellationToken).ConfigureAwait(false);

        File.Move(tempPath, _filePath, overwrite: true);
    }

    public void Dispose()
    {
        _gate.Dispose();
    }
}

public sealed class JsonStorageOptions
{
    public string DirectoryPath { get; set; } = "data";

    public bool WriteIndented { get; set; } = true;
}

public static class JsonFileNames
{
    public const string Suppliers = "suppliers.json";
    public const string Products = "products.json";
    public const string Shelves = "shelves.json";
    public const string SupplierProducts = "supplier-products.json";
    public const string StockBalances = "stock-balances.json";
    public const string PurchaseOrders = "purchase-orders.json";
    public const string Invoices = "invoices.json";
    public const string GoodsReceipts = "goods-receipts.json";
    public const string Sales = "sales.json";
    public const string StockMovements = "stock-movements.json";
}
