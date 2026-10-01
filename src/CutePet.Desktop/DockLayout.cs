using System;
using System.Windows;

namespace CutePet.Desktop;

internal sealed record DockLayout(Size Size, Rect Pet, Rect Quota, int Columns)
{
    public static DockLayout For(QuotaDock dock) => dock switch
    {
        QuotaDock.Top => new(new(280, 236), new(38, 54, 230, 178), new(28, 10, 224, 40), 2),
        QuotaDock.Bottom => new(new(280, 234), new(38, 2, 230, 178), new(28, 184, 224, 40), 2),
        QuotaDock.Right => new(new(352, 184), new(12, 2, 230, 178), new(224, 104, 116, 68), 1),
        _ => new(new(352, 184), new(110, 2, 230, 178), new(12, 104, 116, 68), 1)
    };

    // Relative to the character origin, the temporary host includes all four drop targets.
    public static DockLayout DragWorkspace(QuotaDock dock)
    {
        var source = For(dock);
        var pet = new Rect(154, 72, 230, 178);
        return new(new(538, 324), pet, Target(dock, pet.TopLeft), source.Columns);
    }

    public static Rect Target(QuotaDock dock, Point petOrigin)
    {
        var layout = For(dock);
        return new(petOrigin.X + layout.Quota.X - layout.Pet.X,
            petOrigin.Y + layout.Quota.Y - layout.Pet.Y, layout.Quota.Width, layout.Quota.Height);
    }

    public static QuotaDock Nearest(Point quotaCenter, Point petOrigin, QuotaDock current)
    {
        var best = current;
        var distance = Distance(Target(current, petOrigin));
        foreach (var dock in Enum.GetValues<QuotaDock>())
        {
            var candidate = Distance(Target(dock, petOrigin));
            if (candidate < distance - 0.001) { distance = candidate; best = dock; }
        }
        return best;
        double Distance(Rect bounds) => (new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2) - quotaCenter).LengthSquared;
    }

    public static string Label(QuotaDock dock) => dock switch
    { QuotaDock.Top => "上方", QuotaDock.Bottom => "下方", QuotaDock.Right => "右侧", _ => "左侧" };
}
