using System;
using System.Windows.Media.Imaging;

namespace CutePet.Desktop;

public enum PetCharacter { Cat, Tianyi }

internal static class CharacterCatalog
{
    public static string Label(PetCharacter character) => character switch
    { PetCharacter.Tianyi => "洛天依（同人）", _ => "小猫" };

    // Embedded and decoded once; switching characters performs no file or network requests.
    public static BitmapImage Tianyi { get; } = Load("tianyi-fanart-v1.png");
    private static readonly Lazy<BitmapImage> closed = new(() => Load("tianyi-blink-v1.png"));
    private static readonly Lazy<BitmapImage> wave = new(() => Load("tianyi-wave-v1.png"));
    private static readonly Lazy<BitmapImage> low = new(() => Load("tianyi-low-v1.png"));
    public static BitmapImage Frame(CharacterFrame frame) => frame switch
    { CharacterFrame.Closed => closed.Value, CharacterFrame.Wave => wave.Value, CharacterFrame.Low => low.Value, _ => Tianyi };

    private static BitmapImage Load(string file)
    {
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.UriSource = new Uri("pack://application:,,,/CutePet;component/Assets/" + file);
        image.EndInit();
        image.Freeze();
        return image;
    }
}
