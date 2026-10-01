using System;
using System.Windows.Media.Imaging;

namespace CutePet.Desktop;

public enum PetCharacter { Cat, Tianyi }

internal static class CharacterCatalog
{
    public static string Label(PetCharacter character) => character switch
    { PetCharacter.Tianyi => "洛天依（同人）", _ => "小猫" };

    // Embedded and decoded once; switching characters performs no file or network requests.
    public static BitmapImage Tianyi { get; } = LoadTianyi();

    private static BitmapImage LoadTianyi()
    {
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.UriSource = new Uri("pack://application:,,,/CutePet;component/Assets/tianyi-fanart-v1.png");
        image.EndInit();
        image.Freeze();
        return image;
    }
}
