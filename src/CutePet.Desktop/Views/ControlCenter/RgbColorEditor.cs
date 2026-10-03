using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Media;

namespace CutePet.Desktop;

internal sealed class RgbColorEditor : INotifyPropertyChanged
{
    private readonly Action<string> apply;
    private string red = "", green = "", blue = "", accepted = "";
    internal RgbColorEditor(string color, Action<string> apply) { this.apply = apply; Sync(color); }
    public event PropertyChangedEventHandler? PropertyChanged;
    public string Red { get => red; set { if (red != value) { red = value; Changed(); } } }
    public string Green { get => green; set { if (green != value) { green = value; Changed(); } } }
    public string Blue { get => blue; set { if (blue != value) { blue = value; Changed(); } } }
    public bool HasError => !Channel(red, out _) || !Channel(green, out _) || !Channel(blue, out _);
    public Brush Preview { get { CenterColors.TryParse(accepted, out var color); var brush = new SolidColorBrush(color); brush.Freeze(); return brush; } }
    private static bool Channel(string value, out byte channel) => byte.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out channel);
    private void Changed()
    {
        if (Channel(red, out var r) && Channel(green, out var g) && Channel(blue, out var b))
        {
            var color = CenterColors.Hex(Color.FromRgb(r, g, b));
            if (color != accepted) { accepted = color; apply(color); }
        }
        Notify();
    }
    internal void Sync(string color, bool force = false)
    {
        if (!force && accepted == color) return;
        if (!CenterColors.TryParse(color, out var rgb)) return;
        accepted = color;
        red = rgb.R.ToString(CultureInfo.InvariantCulture); green = rgb.G.ToString(CultureInfo.InvariantCulture); blue = rgb.B.ToString(CultureInfo.InvariantCulture);
        Notify();
    }
    private void Notify() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
}
