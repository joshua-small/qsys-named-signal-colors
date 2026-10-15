using System.Reflection;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using ActiproSoftware.Windows.Controls.Editors;
using Designer.Modules.NamedSignalColors;
using QsysMods.Core;

internal static class Program
{
    // Runs on Windows without opening Designer or reading/writing user settings.
    [STAThread]
    private static void Main()
    {
        var original = new SignalSettings(SignalColorRules.Defaults().ToDictionary(p => p.Key.ToString(), p => p.Value));
        original.Palette["NearEnd"] = "#0123ab";
        original.Palette["FarEnd"] = "#000000";
        original.Palette["AecReference"] = "#FFFFFF";
        var snapshot = new Dictionary<string, string>(original.Palette);
        var editor = Edit(original);
        Require(editor.View is ScrollViewer { VerticalScrollBarVisibility: ScrollBarVisibility.Auto }, "All roles must be reachable by scrolling.");
        var fields = Fields(editor);
        Require(fields.Count == original.Palette.Count, "Every role requires a native picker.");
        foreach (var role in Enum.GetValues<SignalRole>())
        {
            var field = fields[SignalColorRules.Title(role)];
            Require(!field.IsAlphaEnabled && !field.IsNullAllowed && field.HasSwatch && field.HasText,
                "The native editor must support opaque colors, swatches and exact entry.");
            Require(field.Value == (Color)ColorConverter.ConvertFromString(original.Palette[role.ToString()]), "Saved role color was lost.");
        }
        Require(editor.Read().Palette.All(p => p.Value == snapshot[p.Key].ToUpperInvariant()), "Unedited palette did not round-trip.");
        var nearEnd = fields[SignalColorRules.Title(SignalRole.NearEnd)];
        nearEnd.Value = Color.FromRgb(0x09, 0xA0, 0xF3);
        var only = Preferences.Logical(editor.View).OfType<CheckBox>().Single();
        only.IsChecked = true;
        var saved = editor.Read();
        Require(saved.Palette["NearEnd"] == "#09A0F3" && saved.OnlyChangeDefault, "Picker change did not produce the expected settings.");
        Require(saved.Palette.Where(p => p.Key != "NearEnd").All(p => p.Value == snapshot[p.Key].ToUpperInvariant()), "Editing one role changed another.");
        Require(original.Palette.All(p => p.Value == snapshot[p.Key]) && !original.OnlyChangeDefault, "Draft editing mutated the original settings.");
        var reopened = Edit(original).Read();
        Require(reopened.Palette["NearEnd"] == "#0123AB" && !reopened.OnlyChangeDefault, "Discarding a draft retained changes.");
        var accepted = Edit(saved).Read();
        Require(accepted.Palette["NearEnd"] == "#09A0F3" && accepted.OnlyChangeDefault, "Accepted values did not reopen correctly.");
        Console.WriteLine($"Passed native preference editor checks for {fields.Count} roles: saved colors, RGB round-trip, edit isolation and discarded/accepted drafts.");
    }

    private static PreferenceEditor<SignalSettings> Edit(SignalSettings value) =>
        (PreferenceEditor<SignalSettings>)typeof(SignalModule).GetMethod("Editor", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [value])!;
    private static Dictionary<string, ColorEditBox> Fields(PreferenceEditor<SignalSettings> editor) =>
        Preferences.Logical(editor.View).OfType<ColorEditBox>().ToDictionary(AutomationProperties.GetName);
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
