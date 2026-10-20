using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Automation;
using System.Windows.Media;
using ActiproSoftware.Windows.Controls.Editors;
using QsysMods.Core;

namespace Designer.Modules.NamedSignalColors;

public sealed class SignalModule : ModModule
{
    internal static ModSettings<SignalSettings> Settings { get; private set; } = null!;
    private SignalColorWindow? window;
    public SignalModule() => Mods.Register("named-signal-colors", _ => Initialize());
    private void Initialize()
    {
        Settings = Preferences.Register("named-signal-colors", "Named Signal Colors", Defaults, Editor, SignalPalette.Validate);
        Commands.Register(new("named-signal-colors/window", "Named Signal Colors…", "Named Signal Colors", () =>
        {
            if (window is not null) { window.Activate(); return; }
            window = new SignalColorWindow(Application.Current.MainWindow, (kind, detail) => Host.RequiredReceipt("named-signal-colors", kind, detail));
            window.Closed += (_, _) => window = null; window.Show();
        }, () => true, InMenu: true));
    }
    private static SignalSettings Defaults()
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "QsysSchematicTools", "Designer-10.5", "signal-palette.json");
        return SignalPalette.LoadLegacy(path);
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
