using System.IO;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        if (args.Length != 2) throw new ArgumentException("Usage: IconBuilder Brand.xaml output-directory");
        using var source = File.OpenRead(args[0]);
        var image = (DrawingImage)((ResourceDictionary)XamlReader.Load(source))["BrandIcon"];
        image.Freeze();
        Directory.CreateDirectory(args[1]);
        var sizes = new[] { 16, 20, 24, 32, 40, 48, 64, 128, 256 };
        var frames = sizes.Select(size => Render(image, size)).ToArray();
        using var file = File.Create(Path.Combine(args[1], "cutepet.ico"));
        using var writer = new BinaryWriter(file);
        writer.Write((ushort)0); writer.Write((ushort)1); writer.Write((ushort)sizes.Length);
        var offset = 6 + sizes.Length * 16;
        for (var i = 0; i < sizes.Length; i++)
        {
            writer.Write((byte)(sizes[i] == 256 ? 0 : sizes[i])); writer.Write((byte)(sizes[i] == 256 ? 0 : sizes[i]));
            writer.Write((byte)0); writer.Write((byte)0); writer.Write((ushort)1); writer.Write((ushort)32);
            writer.Write(frames[i].Length); writer.Write(offset); offset += frames[i].Length;
        }
        foreach (var frame in frames) writer.Write(frame);
        File.WriteAllBytes(Path.Combine(args[1], "cutepet.png"), Render(image, 512));
    }
    private static byte[] Render(DrawingImage image, int size)
    {
        var visual = new DrawingVisual();
        using (var draw = visual.RenderOpen()) draw.DrawImage(image, new Rect(0, 0, size, size));
        var bitmap = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32); bitmap.Render(visual);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = new MemoryStream(); encoder.Save(stream); return stream.ToArray();
    }
}
