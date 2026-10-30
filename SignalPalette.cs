using System.IO;
using System.Text.Json;

namespace Designer.Modules.NamedSignalColors;

public sealed record SignalSettings(Dictionary<string, string> Palette, bool OnlyChangeDefault = false);

internal static class SignalPalette
{
    internal static SignalSettings LoadLegacy(string path)
    {
        var palette = SignalColorRules.Defaults().ToDictionary(p => p.Key.ToString(), p => p.Value);
        // Import once on first successful Preferences save. Never rewrite the legacy palette.
        if (File.Exists(path))
        {
            var saved = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path))
                ?? throw new InvalidDataException("The existing signal palette is empty.");
            foreach (var pair in saved) palette[pair.Key] = pair.Value;
        }
        var result = new SignalSettings(palette); Validate(result); return result;
    }

    internal static void Validate(SignalSettings value)
    {
        if (value.Palette is null || value.Palette.Count != Enum.GetValues<SignalRole>().Length)
            throw new ArgumentException("The signal palette must include every role.");
        foreach (var pair in value.Palette)
        {
            if (!Enum.TryParse<SignalRole>(pair.Key, out var role) || !Enum.IsDefined(role))
                throw new ArgumentException("Unknown signal role: " + pair.Key);
            SignalColorRules.Normalize(pair.Value);
        }
    }
}
