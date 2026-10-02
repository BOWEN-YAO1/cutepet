using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace CutePet.Desktop;

public enum PetCharacter { Cat, Tianyi }

internal static class CharacterCatalog
{
    public static string BuiltInId(PetCharacter character) => character == PetCharacter.Tianyi ? "tianyi" : "cat";
    private static readonly Lazy<IReadOnlyList<CharacterPack>> builtIns = new(() => new[] { Embedded("cat"), Embedded("tianyi") });
    public static IReadOnlyList<CharacterPack> BuiltIns => builtIns.Value;
    private static CharacterPack Embedded(string id)
    {
        Stream Open(string file) => Application.GetResourceStream(new Uri(
            "pack://application:,,,/CutePet;component/Characters/Packs/" + id + "/" + file))?.Stream
            ?? throw new InvalidDataException("内置角色资源缺失。");
        using var manifest = Open("character.json");
        return CharacterPackLoader.Load(manifest, Open, builtIn: true);
    }
}
