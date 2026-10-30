using System.Text;
using System.Text.Json;
using Designer.Modules.NamedSignalColors;

int passed = 0;
var directory = Path.Combine(Path.GetTempPath(), "signal-palette-tests-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(directory);
try
{
    Check("missing legacy file uses defaults without creating a file", () =>
    {
        var path = Path.Combine(directory, "missing.json");
        var settings = SignalPalette.LoadLegacy(path);
        Equal(24, settings.Palette.Count);
        Equal(false, settings.OnlyChangeDefault);
        Equal("#41F8FF", settings.Palette["NearEnd"]);
        Equal("#FF001D", settings.Palette["GlobalMute"]);
        Equal("#3DF436", settings.Palette["Control"]);
        Equal(false, File.Exists(path));
    });

    Fixture("all custom roles survive import", CustomJson(), path =>
    {
        var settings = SignalPalette.LoadLegacy(path);
        Equal(24, settings.Palette.Count);
        Equal(false, settings.OnlyChangeDefault);
        foreach (var role in Enum.GetValues<SignalRole>())
            Equal(CustomColor(role), settings.Palette[role.ToString()]);
    });

    Fixture("partial palette retains overrides and fills missing roles",
        "{\r\n  \"NearEnd\": \" #a1B2c3 \",\r\n  \"Phone\": \"#102030\"\r\n}\r\n", path =>
    {
        var settings = SignalPalette.LoadLegacy(path);
        Equal(24, settings.Palette.Count);
        Equal(" #a1B2c3 ", settings.Palette["NearEnd"]);
        Equal("#102030", settings.Palette["Phone"]);
        Equal("#41F8FF", SignalColorRules.Defaults()[SignalRole.NearEnd]);
        Equal("#0080FF", settings.Palette["Bluetooth"]);
        Equal(false, settings.OnlyChangeDefault);
    });

    Fixture("empty object retains defaults", "{}", path =>
    {
        var settings = SignalPalette.LoadLegacy(path);
        Equal(24, settings.Palette.Count);
        Equal("#FF0000", settings.Palette["TimerTrigger"]);
    });

    Fixture("reads and edits do not share palette dictionaries", "{\"Bluetooth\":\"#123456\"}", path =>
    {
        var first = SignalPalette.LoadLegacy(path);
        var second = SignalPalette.LoadLegacy(path);
        first.Palette["Bluetooth"] = "#ABCDEF";
        Equal("#123456", second.Palette["Bluetooth"]);
        Equal("#123456", SignalPalette.LoadLegacy(path).Palette["Bluetooth"]);
    });

    Fixture("imported settings survive a JSON round trip", CustomJson(), path =>
    {
        var imported = SignalPalette.LoadLegacy(path);
        var reopened = JsonSerializer.Deserialize<SignalSettings>(JsonSerializer.Serialize(imported))!;
        SignalPalette.Validate(reopened);
        Equal(false, reopened.OnlyChangeDefault);
        foreach (var pair in imported.Palette) Equal(pair.Value, reopened.Palette[pair.Key]);
    });

    foreach (var (title, json) in new[]
    {
        ("empty file", ""),
        ("truncated object", "{\"Phone\":"),
        ("array root", "[]"),
        ("numeric color", "{\"Phone\":123}"),
        ("trailing content", "{\"Phone\":\"#112233\"} extra")
    })
        Fixture("reject " + title, json, path => Throws<JsonException>(() => SignalPalette.LoadLegacy(path)));
    Fixture("reject null root", "null", path => Throws<InvalidDataException>(() => SignalPalette.LoadLegacy(path)));
    foreach (var key in new[] { "UnknownRole", "phone", "9" })
        Fixture("reject unsupported legacy key " + key, JsonSerializer.Serialize(new Dictionary<string, string> { [key] = "#112233" }),
            path => Throws<ArgumentException>(() => SignalPalette.LoadLegacy(path)));
    foreach (var color in new[] { "red", "#FFF", "#11223344", "#GG0011", "112233", "" })
        Fixture("reject invalid color " + color, JsonSerializer.Serialize(new { Phone = color }),
            path => Throws<ArgumentException>(() => SignalPalette.LoadLegacy(path)));

    Check("reject incomplete saved palette", () =>
        Throws<ArgumentException>(() => SignalPalette.Validate(new(new() { ["Phone"] = "#112233" }))));
    Check("reject null saved palette", () =>
        Throws<ArgumentException>(() => SignalPalette.Validate(new(null!))));

    Fixture("imported role colors remain distinct during inference", CustomJson(), path =>
    {
        var palette = Typed(SignalPalette.LoadLegacy(path));
        foreach (var (name, role) in new[]
        {
            ("Mute", SignalRole.Control), ("Privacy Mute", SignalRole.GlobalMute),
            ("LED Mute", SignalRole.LedMute), ("PhoneMute", SignalRole.Phone),
            ("ConfMute", SignalRole.Conference), ("HIDMute", SignalRole.HidMute)
        })
            Equal(CustomColor(role).ToUpperInvariant(),
                SignalColorRules.Choose([Pin(name)], palette).Color);
    });

    Fixture("current colors do not select the role", CustomJson(), path =>
    {
        var palette = Typed(SignalPalette.LoadLegacy(path));
        Equal(CustomColor(SignalRole.Phone).ToUpperInvariant(),
            SignalColorRules.Choose([Pin("PhoneMute", CustomColor(SignalRole.LedMute))], palette).Color);
    });

    Fixture("conflicting evidence needs an explicit role", CustomJson(), path =>
    {
        var palette = Typed(SignalPalette.LoadLegacy(path));
        var pins = new[] { Pin("LED Mute Phone Mute") };
        Equal<string?>(null, SignalColorRules.Choose(pins, palette).Color);
        Equal(CustomColor(SignalRole.HidMute).ToUpperInvariant(),
            SignalColorRules.Choose(pins, palette, SignalRole.HidMute).Color);
    });

    Fixture("default-only filtering protects the entire mixed group", CustomJson(), path =>
    {
        var palette = Typed(SignalPalette.LoadLegacy(path));
        var pins = new[] { Pin("PhoneMute"), Pin("PhoneMute", "#123456") };
        var before = pins.ToArray();
        Equal<string?>(null, SignalColorRules.Choose(pins, palette, defaultColorsOnly: true).Color);
        Equal<string?>(null, SignalColorRules.Choose(pins, palette, SignalRole.HidMute, true).Color);
        Equal(CustomColor(SignalRole.Phone).ToUpperInvariant(), SignalColorRules.Choose(pins, palette).Color);
        Equal(CustomColor(SignalRole.HidMute).ToUpperInvariant(),
            SignalColorRules.Choose([Pin("PhoneMute", "#d5dcef")], palette, SignalRole.HidMute, true).Color);
        Equal(true, before.SequenceEqual(pins));
    });

    Console.WriteLine($"Passed {passed} palette migration and inference regression cases.");
}
finally
{
    Directory.Delete(directory, recursive: true);
}

void Check(string title, Action action)
{
    try { action(); passed++; }
    catch (Exception error) { throw new Exception(title, error); }
}

void Fixture(string title, string json, Action<string> action) => Check(title, () =>
{
    var path = Path.Combine(directory, "legacy.json");
    var bytes = Encoding.UTF8.GetBytes(json);
    File.WriteAllBytes(path, bytes);
    try { action(path); }
    finally
    {
        if (!File.ReadAllBytes(path).SequenceEqual(bytes))
            throw new Exception("The legacy source file changed.");
    }
});

static void Equal<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new Exception($"Expected {expected}; got {actual}.");
}

static void Throws<T>(Action action) where T : Exception
{
    try { action(); }
    catch (T) { return; }
    throw new Exception("Expected " + typeof(T).Name);
}

static string CustomColor(SignalRole role) => "#" + (0x10203a + (int)role * 0x10203).ToString("x6");
static string CustomJson() => JsonSerializer.Serialize(Enum.GetValues<SignalRole>().ToDictionary(r => r.ToString(), CustomColor));
static Dictionary<SignalRole, string> Typed(SignalSettings settings) =>
    settings.Palette.ToDictionary(p => Enum.Parse<SignalRole>(p.Key), p => p.Value);
static SignalPinEvidence Pin(string name, string current = "#D5DCEF") =>
    new(name, 2, 2, "gain", "Gain", "output", "#D5DCEF", false, current);
