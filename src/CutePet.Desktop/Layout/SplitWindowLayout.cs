using System.Windows;

namespace CutePet.Desktop;

internal static class SplitWindowLayout
{
    internal static DockLayout For(Preferences settings)
    {
        var size = new Size(230 * settings.EffectiveCharacterScale, 178 * settings.EffectiveCharacterScale);
        var horizontal = settings.QuotaPosition is QuotaDock.Top or QuotaDock.Bottom;
        return new(size, new(new Point(), size),
            new(0, 0, (horizontal ? 224 : 116) * settings.EffectiveQuotaScale,
                (horizontal ? 40 : 68) * settings.EffectiveQuotaScale), horizontal ? 2 : 1);
    }
    internal static Point QuotaOffset(Preferences settings, double dpiX, double dpiY)
    {
        // Reuse the old four-direction spacing only when explicitly placing a card near the pet.
        var old = DockLayout.For(settings.QuotaPosition, settings.EffectiveCharacterScale, settings.EffectiveQuotaScale);
        return new((old.Quota.X - old.Pet.X) * dpiX, (old.Quota.Y - old.Pet.Y) * dpiY);
    }
    internal static Preferences Migrate(Preferences settings, double dpiX, double dpiY)
    {
        if (settings.IndependentWindows) return settings;
        var old = DockLayout.For(settings.QuotaPosition, settings.EffectiveCharacterScale, settings.EffectiveQuotaScale);
        return settings with { IndependentWindows = true,
            Left = settings.Left is double x ? x + old.Pet.X * dpiX : null,
            Top = settings.Top is double y ? y + old.Pet.Y * dpiY : null,
            QuotaLeft = settings.QuotaLeft ?? (settings.Left is double qx ? qx + old.Quota.X * dpiX : null),
            QuotaTop = settings.QuotaTop ?? (settings.Top is double qy ? qy + old.Quota.Y * dpiY : null) };
    }
}
