// Application/Dto/ReportDocument.cs

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace TradeSystem.Application.Dto;

// Таблица отчёта с заголовком и строками; умеет выводиться текстом и в CSV.
public sealed class ReportDocument
{
    // Создаёт документ с заголовком, колонками и строками.
    public ReportDocument(string title, IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> rows)
    {
        Title = title ?? throw new ArgumentNullException(nameof(title));
        Headers = headers ?? Array.Empty<string>();
        Rows = rows ?? Array.Empty<IReadOnlyList<string>>();
    }

    public string Title { get; } // Заголовок документа
    public IReadOnlyList<string> Headers { get; } // Заголовки колонок
    public IReadOnlyList<IReadOnlyList<string>> Rows { get; } // Строки таблицы

    // Формирует выровненную текстовую таблицу для предпросмотра и печати.
    public string ToPlainText()
    {
        var sb = new StringBuilder();
        sb.AppendLine(Title);
        sb.AppendLine(new string('=', Math.Max(Title.Length, 20)));
        sb.AppendLine();

        if (Headers.Count == 0)
        {
            sb.AppendLine("(нет данных)");
            return sb.ToString();
        }

        // Ширина каждой колонки = max(длина заголовка, длина всех значений).
        var widths = new int[Headers.Count];
        for (int c = 0; c < Headers.Count; c++)
            widths[c] = Headers[c].Length;

        foreach (var row in Rows)
        {
            for (int c = 0; c < widths.Length && c < row.Count; c++)
                widths[c] = Math.Max(widths[c], row[c]?.Length ?? 0);
        }

        sb.AppendLine(FormatRow(Headers, widths));
        sb.AppendLine(string.Join("  ", widths.Select(w => new string('-', w))));

        if (Rows.Count == 0)
            sb.AppendLine("(нет данных)");
        else
            foreach (var row in Rows)
                sb.AppendLine(FormatRow(row, widths));

        return sb.ToString();
    }

    // Формирует CSV с разделителем «;» и экранированием кавычек.
    public string ToCsv()
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(";", Headers.Select(EscapeCsv)));
        foreach (var row in Rows)
            sb.AppendLine(string.Join(";", row.Select(EscapeCsv)));
        return sb.ToString();
    }

    // Собирает строку таблицы с колонками фиксированной ширины.
    private static string FormatRow(IReadOnlyList<string> cells, int[] widths)
    {
        var sb = new StringBuilder();
        for (int c = 0; c < widths.Length; c++)
        {
            var text = c < cells.Count ? (cells[c] ?? string.Empty) : string.Empty;
            sb.Append(text.PadRight(widths[c]));
            if (c < widths.Length - 1) sb.Append("  ");
        }
        return sb.ToString().TrimEnd();
    }

    // Экранирует значение для CSV: кавычки удваиваются, поле оборачивается в кавычки.
    private static string EscapeCsv(string? value)
    {
        value ??= string.Empty;
        if (value.Contains('"') || value.Contains(';') || value.Contains('\n') || value.Contains('\r'))
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        return value;
    }
}
