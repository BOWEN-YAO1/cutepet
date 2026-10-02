using System;
using System.Windows;

namespace CutePet.Desktop;

internal sealed record DockLayout(Size Size, Rect Pet, Rect Quota, int Columns)
{
    public static DockLayout For(QuotaDock dock, double characterScale = 1, double quotaScale = 1)
    {
        var petWidth = 230 * characterScale;
        var petHeight = 178 * characterScale;
        var horizontal = dock is QuotaDock.Top or QuotaDock.Bottom;
        var quotaWidth = (horizontal ? 224 : 116) * quotaScale;
        var quotaHeight = (horizontal ? 40 : 68) * quotaScale;
        if (horizontal)
        {
            var width = Math.Max(280 * characterScale, quotaWidth + 24);
            var petX = (width - petWidth) / 2 + 13 * characterScale;
            var petY = dock == QuotaDock.Top ? quotaHeight + 14 : 2;
            var quotaY = dock == QuotaDock.Top ? 10 : petHeight + 6;
            return new(new(width, Math.Max(petY + petHeight + 4, quotaY + quotaHeight + 10)),
                new(petX, petY, petWidth, petHeight), new((width - quotaWidth) / 2, quotaY, quotaWidth, quotaHeight), 2);
        }
        var overlap = 18 * characterScale; // Existing character art has transparent side padding.
        var sidePetX = dock == QuotaDock.Right ? 12 : 12 + quotaWidth - overlap;
        var sideQuotaX = dock == QuotaDock.Right ? 12 + petWidth - overlap : 12;
        var sideQuotaY = Math.Max(2, 2 + petHeight - quotaHeight - 8);
        return new(new(petWidth + quotaWidth - overlap + 24, Math.Max(petHeight + 2, sideQuotaY + quotaHeight) + 4),
            new(sidePetX, 2, petWidth, petHeight), new(sideQuotaX, sideQuotaY, quotaWidth, quotaHeight), 1);
    }

    // Relative to the character origin, the temporary host includes all four drop targets.
    public static DockLayout DragWorkspace(QuotaDock dock, double characterScale = 1, double quotaScale = 1)
    {
        var source = For(dock, characterScale, quotaScale);
        var bounds = new Rect(new Point(), source.Pet.Size);
        foreach (var candidate in Enum.GetValues<QuotaDock>())
            bounds.Union(Target(candidate, new Point(), characterScale, quotaScale));
        var pet = new Rect(new Point(12 - bounds.Left, 12 - bounds.Top), source.Pet.Size);
        return new(new(bounds.Width + 24, bounds.Height + 24), pet,
            Target(dock, pet.TopLeft, characterScale, quotaScale), source.Columns);
    }

    public static Rect Target(QuotaDock dock, Point petOrigin, double characterScale = 1, double quotaScale = 1)
    {
        var layout = For(dock, characterScale, quotaScale);
        return new(petOrigin.X + layout.Quota.X - layout.Pet.X,
            petOrigin.Y + layout.Quota.Y - layout.Pet.Y, layout.Quota.Width, layout.Quota.Height);
    }

    public static QuotaDock Nearest(Point quotaCenter, Point petOrigin, QuotaDock current,
        double characterScale = 1, double quotaScale = 1)
    {
        var best = current;
        var distance = Distance(Target(current, petOrigin, characterScale, quotaScale));
        foreach (var dock in Enum.GetValues<QuotaDock>())
        {
            var candidate = Distance(Target(dock, petOrigin, characterScale, quotaScale));
            if (candidate < distance - 0.001) { distance = candidate; best = dock; }
        }
        return best;
        double Distance(Rect bounds) => (new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2) - quotaCenter).LengthSquared;
    }

    public static string Label(QuotaDock dock) => dock switch
    { QuotaDock.Top => "上方", QuotaDock.Bottom => "下方", QuotaDock.Right => "右侧", _ => "左侧" };
}
