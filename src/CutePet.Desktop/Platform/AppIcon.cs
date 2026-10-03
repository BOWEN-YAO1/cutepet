using System;
using System.Drawing;
using System.Windows;
using System.Windows.Media.Imaging;

namespace CutePet.Desktop;

internal static class AppIcon
{
    private static readonly Uri IconUri = new("pack://application:,,,/Assets/cutepet.ico");
    internal static BitmapFrame WindowIcon { get; } = BitmapFrame.Create(IconUri);
    internal static Icon CreateTrayIcon()
    {
        using var resource = Application.GetResourceStream(IconUri)!.Stream;
        return new Icon(resource, new System.Drawing.Size(32, 32));
    }
}
