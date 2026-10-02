using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CutePet.Desktop;

// Render both real WPF trees into one review image; no desktop screenshot or user window movement.
internal static class WindowPreview
{
    internal static RenderTargetBitmap Capture(MainWindow window, double dpi, double? motion = null, double width = 0, double height = 0)
    {
        var card = window.QuotaHost;
        var petImage = Surface((FrameworkElement)window.Content, window.Width, window.Height, dpi);
        var cardImage = Surface((FrameworkElement)card.Content, card.Width, card.Height, dpi);
        width = width > 0 ? width : window.Width + card.Width + 24;
        height = height > 0 ? height : Math.Max(window.Height, card.Height) + 8;
        var scene = new DrawingVisual();
        using (var draw = scene.RenderOpen())
        {
            draw.DrawRectangle(new SolidColorBrush(Color.FromRgb(239, 245, 244)), null, new Rect(0, 0, width, height));
            draw.DrawImage(cardImage, new Rect(8, Math.Max(0, height - card.Height - 8), card.Width, card.Height));
            draw.DrawImage(petImage, new Rect(motion is double x ? 250 + x : card.Width + 16, 0, window.Width, window.Height));
        }
        var result = new RenderTargetBitmap((int)Math.Ceiling(width * dpi / 96), (int)Math.Ceiling(height * dpi / 96), dpi, dpi, PixelFormats.Pbgra32);
        result.Render(scene);
        return result;
    }
    internal static RenderTargetBitmap Surface(FrameworkElement visual, double width, double height, double dpi)
    {
        visual.Measure(new Size(width, height));
        visual.Arrange(new Rect(0, 0, width, height));
        visual.UpdateLayout();
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(width * dpi / 96), (int)Math.Ceiling(height * dpi / 96), dpi, dpi, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        return bitmap;
    }
}
