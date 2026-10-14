using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Automation;
using System.Windows.Media;
using ActiproSoftware.Windows.Controls.Editors;
using QsysMods.Core;

namespace Designer.Modules.NamedSignalColors;

public sealed record SignalSettings(Dictionary<string, string> Palette, bool OnlyChangeDefault = false);
public sealed class SignalModule : ModModule
{
    internal static ModSettings<SignalSettings> Settings { get; private set; } = null!;
    private SignalColorWindow? window;
    public SignalModule() => Mods.Register("named-signal-colors", _ => Initialize());
    private void Initialize()
    {
        Settings = Preferences.Register("named-signal-colors", "Named Signal Colors", Defaults, Editor, Validate);
        Commands.Register(new("named-signal-colors/window", "Named Signal Colors…", "Named Signal Colors", () =>
        {
            if (window is not null) { window.Activate(); return; }
            window = new SignalColorWindow(Application.Current.MainWindow, (kind, detail) => Host.RequiredReceipt("named-signal-colors", kind, detail));
            window.Closed += (_, _) => window = null; window.Show();
        }, () => true, InMenu: true));
    }
    private static SignalSettings Defaults()
    {
        var palette = SignalColorRules.Defaults().ToDictionary(p => p.Key.ToString(), p => p.Value);
        // Import once on first successful Preferences save. Never rewrite the legacy palette.
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "QsysSchematicTools", "Designer-10.5", "signal-palette.json");
        if (File.Exists(path))
        {
            var saved = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path)) ?? throw new InvalidDataException("The existing signal palette is empty.");
            foreach (var pair in saved) palette[pair.Key] = pair.Value;
        }
        var result = new SignalSettings(palette); Validate(result); return result;
    }
    private static void Validate(SignalSettings value)
    {
        if (value.Palette is null || value.Palette.Count != Enum.GetValues<SignalRole>().Length) throw new ArgumentException("The signal palette must include every role.");
        foreach (var pair in value.Palette)
        {
            if (!Enum.TryParse<SignalRole>(pair.Key, out var role) || !Enum.IsDefined(role)) throw new ArgumentException("Unknown signal role: " + pair.Key);
            SignalColorRules.Normalize(pair.Value);
        }
    }
    private static PreferenceEditor<SignalSettings> Editor(SignalSettings value)
    {
        var panel = new StackPanel { Margin = new Thickness(16) };
        var only = new CheckBox { Content = "Only change default colors", IsChecked = value.OnlyChangeDefault, Margin = new Thickness(0,0,0,16) }; panel.Children.Add(only);
        var fields = new Dictionary<string, ColorEditBox>();
        foreach (var role in Enum.GetValues<SignalRole>())
        {
            var row = new DockPanel { Margin = new Thickness(0,0,0,8) };
            // Match Designer 10.5's General Preferences trace-color editor.
            var input = new ColorEditBox
            {
                Value = (Color)ColorConverter.ConvertFromString(SignalColorRules.Normalize(value.Palette[role.ToString()])),
                IsAlphaEnabled = false, IsNullAllowed = false, Width = 180
            };
            AutomationProperties.SetName(input, SignalColorRules.Title(role));
            DockPanel.SetDock(input, Dock.Right); row.Children.Add(input);
            row.Children.Add(new TextBlock { Text = SignalColorRules.Title(role), VerticalAlignment = VerticalAlignment.Center }); panel.Children.Add(row); fields[role.ToString()] = input;
        }
        var view = new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        return new(view, () => new(fields.ToDictionary(p => p.Key, p =>
        {
            var color = p.Value.Value ?? throw new ArgumentException("Choose a color for " + p.Key + ".");
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }), only.IsChecked == true));
    }
}
