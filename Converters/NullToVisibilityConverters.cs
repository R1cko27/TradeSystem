// Converters/NullToVisibilityConverters.cs

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

// Файл содержит конвертеры NullToVisibilityConverter и NotNullToVisibilityConverter,

namespace TradeSystem.App.Converters;

/// <summary>null → Visible, не null → Collapsed (подсказка «выберите заказ»).</summary>
public sealed class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is null ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>не null → Visible, null → Collapsed (панель деталей).</summary>
public sealed class NotNullToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is not null ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
