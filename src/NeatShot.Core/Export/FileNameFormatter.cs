using System.Globalization;
using System.Text;

namespace NeatShot.Core.Export;

public static class FileNameFormatter
{
    private static readonly HashSet<char> InvalidCharacters = [.. Path.GetInvalidFileNameChars()];

    public static string Format(string pattern, DateTimeOffset timestamp)
    {
        ArgumentNullException.ThrowIfNull(pattern);

        var result = new StringBuilder(pattern.Length + 16);
        var index = 0;

        while (index < pattern.Length)
        {
            var open = pattern.IndexOf('{', index);
            if (open < 0)
            {
                Append(result, pattern.AsSpan(index));
                break;
            }

            var close = pattern.IndexOf('}', open);
            if (close < 0)
            {
                Append(result, pattern.AsSpan(index));
                break;
            }

            Append(result, pattern.AsSpan(index, open - index));
            var token = pattern.AsSpan(open + 1, close - open - 1);
            Append(result, Expand(token, timestamp) ?? pattern.AsSpan(open, close - open + 1));
            index = close + 1;
        }

        var name = result.ToString().Trim();
        return name.Length == 0 ? timestamp.ToString("yyyy-MM-dd HH.mm.ss", CultureInfo.InvariantCulture) : name;
    }

    private static string? Expand(ReadOnlySpan<char> token, DateTimeOffset timestamp)
    {
        var format = token switch
        {
            "date" => "yyyy-MM-dd",
            "time" => "HH.mm.ss",
            "year" => "yyyy",
            "month" => "MM",
            "day" => "dd",
            "hour" => "HH",
            "minute" => "mm",
            "second" => "ss",
            _ => null,
        };

        return format is null ? null : timestamp.ToString(format, CultureInfo.InvariantCulture);
    }

    private static void Append(StringBuilder builder, ReadOnlySpan<char> text)
    {
        foreach (var character in text)
        {
            builder.Append(InvalidCharacters.Contains(character) ? '-' : character);
        }
    }
}
