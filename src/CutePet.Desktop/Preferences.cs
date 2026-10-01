using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CutePet.Desktop;

public enum DetailsMode { Hover, Always, Hidden }
public enum QuotaDock { Bottom, Top, Left, Right }

public sealed record Preferences(double? Left = null, double? Top = null, double Scale = 1,
    bool AlwaysOnTop = true, string? CodexPath = null, DetailsMode Details = DetailsMode.Hover,
    QuotaDock QuotaPosition = QuotaDock.Left, PetCharacter Character = PetCharacter.Cat,
    double? CharacterScale = null, double? QuotaScale = null,
    bool PositionLocked = false, bool StartWithWindows = false, string? CharacterPackId = null)
{
    // Older settings used Scale for the entire widget. Missing independent values inherit that size.
    [JsonIgnore] public double EffectiveCharacterScale => CharacterScale ?? Scale;
    [JsonIgnore] public double EffectiveQuotaScale => QuotaScale ?? Scale;
    public static double ValidateIndependentScale(double value) => double.IsFinite(value) ? Math.Clamp(value, 0.8, 2) : 1;

    public Preferences Validated() => this with
    {
        Left = Left is double x && double.IsFinite(x) && Math.Abs(x) < 1_000_000 ? x : null,
        Top = Top is double y && double.IsFinite(y) && Math.Abs(y) < 1_000_000 ? y : null,
        Scale = double.IsFinite(Scale) ? Math.Clamp(Scale, 0.8, 1.4) : 1,
        CodexPath = string.IsNullOrWhiteSpace(CodexPath) ? null : CodexPath,
        Details = Enum.IsDefined(Details) ? Details : DetailsMode.Hover,
        QuotaPosition = Enum.IsDefined(QuotaPosition) ? QuotaPosition : QuotaDock.Left,
        Character = Enum.IsDefined(Character) ? Character : PetCharacter.Cat,
        CharacterPackId = CharacterPackLoader.ValidId(CharacterPackId) ? CharacterPackId : null,
        CharacterScale = CharacterScale is double character ? ValidateIndependentScale(character) : null,
        QuotaScale = QuotaScale is double quota ? ValidateIndependentScale(quota) : null
    };
}

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
