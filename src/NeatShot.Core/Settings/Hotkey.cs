using System.Globalization;

namespace NeatShot.Core.Settings;

public readonly record struct Hotkey(HotkeyModifiers Modifiers, int VirtualKey)
{
    private const int KeyF1 = 0x70;
    private const int KeyDigit0 = 0x30;
    private const int KeyA = 0x41;

    private static readonly Dictionary<string, int> NamedKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Space"] = 0x20,
        ["Enter"] = 0x0D,
        ["Tab"] = 0x09,
        ["Escape"] = 0x1B,
        ["PrintScreen"] = 0x2C,
        ["Insert"] = 0x2D,
        ["Delete"] = 0x2E,
        ["Home"] = 0x24,
        ["End"] = 0x23,
        ["PageUp"] = 0x21,
        ["PageDown"] = 0x22,
    };

    public static Hotkey Parse(string text)
    {
        if (TryParse(text, out var hotkey))
        {
            return hotkey;
        }

        throw new FormatException($"'{text}' is not a valid hotkey.");
    }

    public static bool TryParse(string? text, out Hotkey hotkey)
    {
        hotkey = default;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var parts = text.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return false;
        }

        var modifiers = HotkeyModifiers.None;
        for (var i = 0; i < parts.Length - 1; i++)
        {
            if (!TryParseModifier(parts[i], out var modifier))
            {
                return false;
            }

            modifiers |= modifier;
        }

        if (!TryParseKey(parts[^1], out var key))
        {
            return false;
        }

        hotkey = new Hotkey(modifiers, key);
        return true;
    }

    public override string ToString()
    {
        var parts = new List<string>(5);
        if (Modifiers.HasFlag(HotkeyModifiers.Control))
        {
            parts.Add("Ctrl");
        }

        if (Modifiers.HasFlag(HotkeyModifiers.Alt))
        {
            parts.Add("Alt");
        }

        if (Modifiers.HasFlag(HotkeyModifiers.Shift))
        {
            parts.Add("Shift");
        }

        if (Modifiers.HasFlag(HotkeyModifiers.Windows))
        {
            parts.Add("Win");
        }

        parts.Add(KeyName(VirtualKey));
        return string.Join('+', parts);
    }

    private static string KeyName(int key)
    {
        if (key is (>= KeyA and < KeyA + 26) or (>= KeyDigit0 and < KeyDigit0 + 10))
        {
            return ((char)key).ToString();
        }

        if (key is >= KeyF1 and < KeyF1 + 24)
        {
            return "F" + (key - KeyF1 + 1).ToString(CultureInfo.InvariantCulture);
        }

        foreach (var (name, code) in NamedKeys)
        {
            if (code == key)
            {
                return name;
            }
        }

        return "0x" + key.ToString("X2", CultureInfo.InvariantCulture);
    }

    private static bool TryParseModifier(string text, out HotkeyModifiers modifier)
    {
        modifier = text.ToUpperInvariant() switch
        {
            "CTRL" or "CONTROL" => HotkeyModifiers.Control,
            "ALT" => HotkeyModifiers.Alt,
            "SHIFT" => HotkeyModifiers.Shift,
            "WIN" or "WINDOWS" or "SUPER" => HotkeyModifiers.Windows,
            _ => HotkeyModifiers.None,
        };
        return modifier != HotkeyModifiers.None;
    }

    private static bool TryParseKey(string text, out int key)
    {
        key = 0;
        if (text.Length == 1)
        {
            var c = char.ToUpperInvariant(text[0]);
            if (!char.IsAsciiLetterOrDigit(c))
            {
                return false;
            }

            key = c;
            return true;
        }

        if (text.Length is 2 or 3
            && char.ToUpperInvariant(text[0]) == 'F'
            && int.TryParse(text.AsSpan(1), NumberStyles.None, CultureInfo.InvariantCulture, out var number)
            && number is >= 1 and <= 24)
        {
            key = KeyF1 + number - 1;
            return true;
        }

        return NamedKeys.TryGetValue(text, out key);
    }
}
