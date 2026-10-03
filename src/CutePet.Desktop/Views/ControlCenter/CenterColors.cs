using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace CutePet.Desktop;

internal static class CenterColors
{
    internal const string DefaultTheme = "#E8A1B6";
    internal const string DefaultFont = "#583A45";

    internal static string Validate(string? value, string fallback) => TryParse(value, out var color) ? Hex(color) : fallback;
    internal static string Hex(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    internal static bool TryParse(string? value, out Color color)
    {
        color = default;
        if (value is not { Length: 7 } || value[0] != '#' ||
            !uint.TryParse(value.AsSpan(1), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var rgb)) return false;
        color = Color.FromRgb((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);
        return true;
    }

    internal static void Apply(ResourceDictionary resources, Preferences settings)
    {
        TryParse(Validate(settings.ThemeColor, DefaultTheme), out var theme);
        TryParse(Validate(settings.FontColor, DefaultFont), out var ink);
        var background = Mix(theme, Colors.White, .91);
        Set("CenterInk", ink);
        Set("CenterMuted", Mix(ink, background, .18));
        Set("CenterAccent", theme);
        Set("CenterBackground", background);
        Set("CenterSurface", Mix(theme, Colors.White, .97));
        Set("CenterSoft", Mix(theme, Colors.White, .66));
        Set("CenterLine", Mix(theme, Colors.White, .50));
        Set("CenterTrack", Mix(theme, Colors.White, .78));
        Set("CenterFocus", Mix(theme, ink, .50));
        Set("CenterInput", Luminance(ink) > .5 ? Mix(theme, Colors.Black, .72) : Colors.White);
        // Very light fonts need a darker primary button to remain readable.
        Set("CenterPrimaryFill", Luminance(ink) > .5 ? Mix(theme, Colors.Black, .72) : theme);
        void Set(string key, Color color) { var brush = new SolidColorBrush(color); brush.Freeze(); resources[key] = brush; }
    }

    private static Color Mix(Color first, Color second, double weight) => Color.FromRgb(
        BlendChannel(first.R, second.R, weight), BlendChannel(first.G, second.G, weight), BlendChannel(first.B, second.B, weight));
    private static byte BlendChannel(byte first, byte second, double weight) => (byte)Math.Round(first + (second - first) * weight);
    private static double Luminance(Color color) => (.2126 * color.R + .7152 * color.G + .0722 * color.B) / 255;
}
