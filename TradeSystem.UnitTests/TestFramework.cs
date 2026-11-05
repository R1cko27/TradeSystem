// TradeSystem.UnitTests/TestFramework.cs

using System;
using System.Threading.Tasks;

namespace TradeSystem.UnitTests;

// Исключение провалившейся проверки.
public sealed class AssertFailedException : Exception
{
    public AssertFailedException(string message) : base(message)
    {
    }
}

// Минимальный набор проверок без внешних библиотек.
public static class Check
{
    // Проверяет, что условие истинно.
    public static void True(bool condition, string message = "ожидалось значение true")
    {
        if (!condition) throw new AssertFailedException(message);
    }

    // Проверяет, что условие ложно.
    public static void False(bool condition, string message = "ожидалось значение false")
    {
        if (condition) throw new AssertFailedException(message);
    }

    // Сравнивает ожидаемое и фактическое значения.
    public static void Equal<T>(T expected, T actual, string? message = null)
    {
        if (!Equals(expected, actual))
            throw new AssertFailedException(message ?? $"ожидалось '{expected}', получено '{actual}'");
    }

    // Проверяет, что значение не null.
    public static void NotNull(object? value, string message = "значение не должно быть null")
    {
        if (value is null) throw new AssertFailedException(message);
    }

    // Проверяет, что значение null.
    public static void Null(object? value, string message = "ожидалось null")
    {
        if (value is not null) throw new AssertFailedException(message);
    }

    // Проверяет вхождение подстроки без учёта регистра.
    public static void Contains(string expected, string actual, string? message = null)
    {
        if (!actual.Contains(expected, StringComparison.OrdinalIgnoreCase))
            throw new AssertFailedException(message ?? $"строка '{actual}' не содержит '{expected}'");
    }

    // Проверяет, что действие бросает ожидаемое исключение.
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
