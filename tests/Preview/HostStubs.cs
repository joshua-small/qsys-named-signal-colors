// Synthetic host boundary for portable tests of the production reader/revalidator.
// These types do not implement or establish Designer compatibility or native undo.
using System.Collections;
using System.Reflection;

namespace System.Windows
{
    public sealed class Application
    {
        public static Application Current { get; } = new();
        public Dispatcher Dispatcher { get; } = new();
    }
    public sealed class Dispatcher { public void VerifyAccess() { } }
}
namespace System.Windows.Media
{
    public readonly record struct Color(byte A, byte R, byte G, byte B)
    {
        public static Color FromRgb(byte r, byte g, byte b) => new(255, r, g, b);
    }
    public static class ColorConverter
    {
        public static object ConvertFromString(string text) => Color.FromRgb(
            Convert.ToByte(text.Substring(1, 2), 16), Convert.ToByte(text.Substring(3, 2), 16), Convert.ToByte(text.Substring(5, 2), 16));
    }
}
namespace QsysMods.Core
{
    public static class Host { public static bool Enabled => true; }
    public static class HostSelection
    {
        public static object GlobalSelection { get; set; } = Array.Empty<object>();
        public static Type HostType(string name) => typeof(HostSelection).Assembly.GetType(name, true)!;
        public static object RequiredProperty(object target, string name) =>
            target.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance)?.GetValue(target)
            ?? throw new MissingMemberException(target.GetType().FullName, name);
        public static object ReadHostState() => new { synthetic = true };
    }
}
namespace QSC.QSys
{
    public static class App { public static Manager Manager = new(); }
    public sealed class Manager
    {
        public bool IsLoading => false;
        public bool IsInUndo => false;
        public bool IsInEndOperation => false;
        public int Starts { get; private set; }
        public int Ends { get; private set; }
        public Guid StartOperation() { Starts++; return Guid.NewGuid(); }
        public void EndOperation(Guid id) { Ends++; }
        public void CancelOperation() { }
    }
}
namespace QSC.QSys.Design
{
    using System.Windows.Media;
    public sealed class Provider
    {
        public string ClassName => "gain";
        public string Label => "Synthetic gain";
        public string CodeName => "gain";
        public Color Color => Color.FromRgb(213, 220, 239);
        public bool CustomColorApplied => false;
        public bool IsAttached { get; set; } = true;
        public bool IsLocked { get; set; }
    }
    public sealed class Pin(string label)
    {
        public Provider Provider = new();
        public int Direction = 0;
        public string CodeName = "input";
        public string Label { get; set; } = label;
        public int WiringDomain => 1;
        public bool IsAttached { get; set; } = true;
        private Color color = Color.FromRgb(213, 220, 239);
        public Color Color
        {
            get => color;
            set
            {
                var info = SignalNameManager.GetInfo(Label);
                foreach (var pin in info.Tx.Concat(info.Rx)) pin.color = value;
            }
        }
    }
    public sealed class Info
    {
        public List<Pin> Tx = new();
        public List<Pin> Rx = new();
    }
    public static class SignalNameManager
    {
        public static IDictionary AllInfos { get; } = new Dictionary<string, Info>(StringComparer.OrdinalIgnoreCase);
        public static Info GetInfo(string name) => (Info)AllInfos[name]!;
    }
}
namespace QSC.QSys.Graphic
{
    public sealed class Pin(Design.Pin pin) { public Design.Pin InnerPin => pin; }
    public sealed class Component { public object obj => throw new Exception("Ignored components must not be expanded."); }
}
namespace QSC.QSys.Graphic.Pins
{
    public sealed class PinLabel(Graphic.Pin pin) { public Graphic.Pin Pin => pin; }
}
