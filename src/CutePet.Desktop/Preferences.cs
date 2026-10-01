using System;
using System.IO;
using System.Text.Json;

namespace CutePet.Desktop;

public sealed record Preferences(double? Left = null, double? Top = null, double Scale = 1,
    bool AlwaysOnTop = true, string? CodexPath = null)
{
    public Preferences Validated() => this with
    {
        Left = Left is double x && double.IsFinite(x) && Math.Abs(x) < 1_000_000 ? x : null,
        Top = Top is double y && double.IsFinite(y) && Math.Abs(y) < 1_000_000 ? y : null,
        Scale = double.IsFinite(Scale) ? Math.Clamp(Scale, 0.8, 1.4) : 1,
        CodexPath = string.IsNullOrWhiteSpace(CodexPath) ? null : CodexPath
    };
}

public sealed class PreferencesStore(string? directory = null)
{
    private readonly string directory = directory ?? Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CutePet");

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
