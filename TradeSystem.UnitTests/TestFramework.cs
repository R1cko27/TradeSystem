// TradeSystem.UnitTests/TestFramework.cs

using System;
using System.Threading.Tasks;

namespace TradeSystem.UnitTests;

/// <summary>Исключение проверки: тест не пройден.</summary>
public sealed class AssertFailedException : Exception
{
    public AssertFailedException(string message) : base(message)
    {
    }
}

/// <summary>Простые проверки для тестов.</summary>
public static class Check
{
    public static void True(bool condition, string message = "ожидалось значение true")
    {
        if (!condition) throw new AssertFailedException(message);
    }

    public static void False(bool condition, string message = "ожидалось значение false")
    {
        if (condition) throw new AssertFailedException(message);
    }

    public static void Equal<T>(T expected, T actual, string? message = null)
    {
        if (!Equals(expected, actual))
            throw new AssertFailedException(message ?? $"ожидалось '{expected}', получено '{actual}'");
    }

    public static void NotNull(object? value, string message = "значение не должно быть null")
    {
        if (value is null) throw new AssertFailedException(message);
    }

    public static void Null(object? value, string message = "ожидалось null")
    {
        if (value is not null) throw new AssertFailedException(message);
    }

    public static void Contains(string expected, string actual, string? message = null)
    {
        if (!actual.Contains(expected, StringComparison.OrdinalIgnoreCase))
            throw new AssertFailedException(message ?? $"строка '{actual}' не содержит '{expected}'");
    }

    public static async Task ThrowsAsync<TException>(Func<Task> action, string? contains = null)
        where TException : Exception
    {
        try
        {
            await action();
        }
        catch (TException ex)
        {
            if (contains is not null &&
                !ex.Message.Contains(contains, StringComparison.OrdinalIgnoreCase))
                throw new AssertFailedException(
                    $"исключение {typeof(TException).Name} с другим текстом: '{ex.Message}', " +
                    $"ожидалось вхождение '{contains}'");
            return;
        }
        catch (Exception ex)
        {
            throw new AssertFailedException(
                $"ожидалось {typeof(TException).Name}, получено {ex.GetType().Name}: {ex.Message}");
        }

        throw new AssertFailedException($"ожидалось {typeof(TException).Name}, но исключение не возникло");
    }
}
