using System;
using System.IO;
using System.Text.Json;

namespace CutePet.Desktop;

public sealed class PreferencesStore(string? directory = null)
{
    private readonly string directory = directory ?? Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CutePet");

    internal string CharacterDirectory => Path.Combine(directory, "Characters");

    public Preferences Load()
    {
        try { return (JsonSerializer.Deserialize<Preferences>(File.ReadAllText(Path.Combine(directory, "settings.json")))
            ?? new Preferences()).Validated(); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException) { return new(); }
    }

    public bool Save(Preferences settings)
    {
        var temporary = Path.Combine(directory, "settings.pending.json");
        try
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(temporary, JsonSerializer.Serialize(settings.Validated(), new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temporary, Path.Combine(directory, "settings.json"), overwrite: true);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { return false; }
    }
}
