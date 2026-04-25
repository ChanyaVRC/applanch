using System.Globalization;
using System.Runtime.InteropServices;

namespace applanch.Theming;

/// <summary>
/// Represents a theme color with ARGB components.
/// </summary>
[StructLayout(LayoutKind.Explicit)]
public readonly record struct ThemeColor
{
    public ThemeColor(byte a, byte r, byte g, byte b)
    {
        A = a;
        R = r;
        G = g;
        B = b;
    }

    [field: FieldOffset(3)]
    public byte A { get; }

    [field: FieldOffset(2)]
    public byte R { get; }

    [field: FieldOffset(1)]
    public byte G { get; }

    [field: FieldOffset(0)]
    public byte B { get; }

    [FieldOffset(0)]
    private readonly uint _littleEndianArgb;

    /// <summary>
    /// Gets the color as ARGB uint value.
    /// </summary>
    internal uint Argb
    {
        get
        {
            if (BitConverter.IsLittleEndian)
            {
                return _littleEndianArgb;
            }
            return ((uint)A << 24) | ((uint)R << 16) | ((uint)G << 8) | B;
        }
    }

    /// <summary>
    /// Gets the color in hex format (#RRGGBB or #AARRGGBB).
    /// </summary>
    public string Hex => A == byte.MaxValue
        ? $"#{R:X2}{G:X2}{B:X2}"
        : $"#{A:X2}{R:X2}{G:X2}{B:X2}";

    /// <summary>
    /// Parses a color from a string (hex format).
    /// </summary>
    public static ThemeColor Parse(string value)
    {
        if (TryParse(value, out var color))
        {
            return color;
        }

        throw new FormatException($"Invalid theme color format: '{value}'.");
    }

    /// <summary>
    /// Tries to parse a color from a string.
    /// </summary>
    public static bool TryParse(string? value, out ThemeColor color) =>
        TryParse(value.AsSpan(), out color);

    /// <summary>
    /// Tries to parse a color from a span of characters.
    /// </summary>
    public static bool TryParse(ReadOnlySpan<char> value, out ThemeColor color)
    {
        color = default;

        if (value.IsEmpty)
        {
            return false;
        }

        if (value[0] == '#')
        {
            value = value[1..];
        }

        if (value.Length != 6 && value.Length != 8)
        {
            return false;
        }

        if (!uint.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgb))
        {
            return false;
        }

        color = new ThemeColor(
            value.Length == 6 ? byte.MaxValue : (byte)((rgb >> 24) & 0xFF),
            (byte)((rgb >> 16) & 0xFF),
            (byte)((rgb >> 8) & 0xFF),
            (byte)(rgb & 0xFF));
        return true;
    }

    public override string ToString() => Hex;

    public override int GetHashCode() => _littleEndianArgb.GetHashCode();

    public bool Equals(ThemeColor other) => _littleEndianArgb == other._littleEndianArgb;
}
