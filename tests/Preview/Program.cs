using Designer.Modules.NamedSignalColors;
using QsysMods.Core;
using QSC.QSys.Design;
using GraphicPin = QSC.QSys.Graphic.Pin;

int passed = 0;
foreach (var count in new[] { 256, 257, 1024 })
{
    Check($"entire-design preview includes all {count} groups", () =>
    {
        Fixture(count);
        Equal(count, Read(SignalColorScope.EntireDesign).Groups.Length);
        Equal(0, QSC.QSys.App.Manager.Starts);
    });
    Check($"selection preview includes all {count} groups and ignores other graphics", () =>
    {
        var graphics = Fixture(count);
        HostSelection.GlobalSelection = graphics.Concat(new object[] { new QSC.QSys.Graphic.Component(), new object() }).ToArray();
        var preview = Read();
        Equal(count, preview.Groups.Length);
        Equal(count, preview.Selection.Length);
    });
}
Check("Apply revalidates and colors 300 groups in one operation", () =>
{
    Fixture(300);
    var preview = Read();
    var receipts = new List<string>();
    SignalColoring.Apply(preview, (kind, _) => { receipts.Add(kind); return kind; });
    Equal(1, QSC.QSys.App.Manager.Starts);
    Equal(1, QSC.QSys.App.Manager.Ends);
    Equal("signal-color-completed", receipts.Last());
    foreach (var group in preview.Groups)
        foreach (var pin in group.Pins) Equal("#FF001D", SignalColoring.Hex(((Pin)pin.Pin).Color));
});
Check("ignored selection changes do not block Apply", () =>
{
    var graphics = Fixture(300);
    HostSelection.GlobalSelection = graphics.Append(new QSC.QSys.Graphic.Component()).ToArray();
    var preview = Read();
    HostSelection.GlobalSelection = graphics.Reverse().Append(new object()).ToArray();
    SignalColoring.Apply(preview, (_, _) => "receipt");
    Equal(1, QSC.QSys.App.Manager.Starts);
});
Check("relevant selection changes reject Apply before an operation", () =>
{
    var graphics = Fixture(300);
    var preview = Read();
    HostSelection.GlobalSelection = graphics.Skip(1).ToArray();
    RejectApply(preview);
});
Check("changed group membership rejects Apply before an operation", () =>
{
    Fixture(300);
    var preview = Read();
    SignalNameManager.GetInfo("Signal 0000").Rx.Add(new Pin("Signal 0000"));
    RejectApply(preview);
});
Check("changed pin evidence rejects Apply before an operation", () =>
{
    Fixture(300);
    var preview = Read();
    SignalNameManager.GetInfo("Signal 0000").Tx[0].CodeName = "changed";
    RejectApply(preview);
});
Check("changed colors reject Apply before an operation", () =>
{
    Fixture(300);
    var preview = Read();
    SignalNameManager.GetInfo("Signal 0000").Tx[0].Color = System.Windows.Media.Color.FromRgb(1, 2, 3);
    RejectApply(preview);
});
Check("ignored-only selection is empty without falling back to the entire design", () =>
{
    Fixture(300);
    HostSelection.GlobalSelection = new object[] { new QSC.QSys.Graphic.Component(), new object(), new GraphicPin(new Pin("")) };
    var preview = Read();
    Equal(0, preview.Groups.Length);
    SignalColoring.Apply(preview, (_, _) => throw new Exception("No mutation receipt expected."));
    Equal(0, QSC.QSys.App.Manager.Starts);
});
Check("the 256 associated-pins-per-group safeguard remains", () =>
{
    Fixture(1);
    var group = SignalNameManager.GetInfo("Signal 0000");
    group.Rx.AddRange(Enumerable.Range(0, 256).Select(_ => new Pin("Signal 0000")));
    Throws(() => Read(SignalColorScope.EntireDesign), "256 associated pins");
});
Check("4096 total pins are accepted without a group-count cutoff", () =>
{
    Fixture(4096);
    Equal(4096, Read(SignalColorScope.EntireDesign).Groups.Length);
});
Check("the total-associated-pin safeguard remains", () =>
{
    Fixture(4097);
    Throws(() => Read(SignalColorScope.EntireDesign), "4096");
});
Console.WriteLine($"Passed {passed} production preview/revalidation checks against a synthetic host.");

object[] Fixture(int count)
{
    SignalNameManager.AllInfos.Clear();
    QSC.QSys.App.Manager = new();
    var graphics = Enumerable.Range(0, count).Select(i =>
    {
        var name = $"Signal {i:D4}";
        var pin = new Pin(name);
        SignalNameManager.AllInfos.Add(name, new Info { Tx = new() { pin } });
        return (object)new GraphicPin(pin);
    }).ToArray();
    HostSelection.GlobalSelection = graphics;
    return graphics;
}
SignalColoring.Preview Read(SignalColorScope scope = SignalColorScope.Selection) =>
    SignalColoring.Read(SignalColorRules.Defaults(), SignalRole.GlobalMute, scope);
void RejectApply(SignalColoring.Preview preview)
{
    Throws(() => SignalColoring.Apply(preview, (_, _) => throw new Exception("Receipt before rejection.")), "Preview again");
    Equal(0, QSC.QSys.App.Manager.Starts);
}
void Throws(Action action, string message)
{
    try { action(); }
    catch (InvalidOperationException error) when (error.Message.Contains(message, StringComparison.Ordinal)) { return; }
    throw new Exception("Expected rejection containing: " + message);
}
void Equal<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception($"Expected {expected}, got {actual}.");
}
void Check(string name, Action action)
{
    try { action(); passed++; Console.WriteLine("PASS " + name); }
    catch (Exception error) { throw new Exception("FAIL " + name, error); }
}
