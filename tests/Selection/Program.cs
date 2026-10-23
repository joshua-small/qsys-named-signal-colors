using Designer.Modules.NamedSignalColors;

int passed = 0;

Check("mixed selection retains only Named Signal tags and named pins", () =>
{
    var tx = new TestPin("Music");
    var rx = new TestPin("Page");
    var pin = new GraphicPin(tx);
    var tag = new PinLabel(new GraphicPin(rx));
    var result = Read(new object[] { new Component(), pin, new object(), tag, new GraphicPin(new TestPin("")) });
    SameReferences(new object[] { pin, tag }, result.Graphics);
    SameReferences(new object[] { tx, rx }, result.Pins);
});

Check("components are ignored without inspecting or expanding their pins", () =>
{
    var result = Read(new object[] { new Component() });
    Empty(result);
});

Check("empty selection is a valid empty preview input", () => Empty(Read(Array.Empty<object>())));

Check("unrelated objects with signal-like properties are ignored", () =>
    Empty(Read(new object[] { new { InnerPin = new TestPin("Music"), Label = "Music" }, new object() })));

Check("null empty and whitespace pin labels are ignored", () =>
{
    foreach (var label in new string?[] { null, "", "  \t\r\n" })
        Empty(Read(new object[] { new GraphicPin(new TestPin(label)), new PinLabel(new GraphicPin(new TestPin(label))) }));
});

Check("tag and pin representations share one pin while retaining selection identities", () =>
{
    var pin = new TestPin("Music");
    var graphic = new GraphicPin(pin);
    var tag = new PinLabel(graphic);
    var result = Read(new object[] { tag, graphic });
    SameReferences(new object[] { graphic, tag }, result.Graphics);
    SameReferences(new object[] { pin }, result.Pins);
});

Check("repeated selection references are counted once", () =>
{
    var graphic = new GraphicPin(new TestPin("Music"));
    var result = Read(Enumerable.Repeat<object>(graphic, 5000).ToArray(), 1);
    Equal(1, result.Graphics.Length);
    Equal(1, result.Pins.Count);
});

Check("different pins with equal values retain distinct reference identities", () =>
{
    var first = new TestPin("Music");
    var second = new TestPin("Music");
    Equal(true, first.Equals(second));
    var result = Read(new object[] { new GraphicPin(first), new GraphicPin(second) });
    SameReferences(new object[] { first, second }, result.Pins);
});

Check("derived graphic and tag types use the supported representations", () =>
{
    var first = new TestPin("Music");
    var second = new TestPin("Page");
    var result = Read(new object[] { new DerivedGraphicPin(first), new DerivedPinLabel(new GraphicPin(second)) });
    SameReferences(new object[] { first, second }, result.Pins);
});

Check("ignored graphics do not consume the relevant selection bound", () =>
{
    var graphic = new GraphicPin(new TestPin("Music"));
    var ignored = Enumerable.Range(0, 5000).Select(i => i % 2 == 0
        ? (object)new Component() : new GraphicPin(new TestPin("")));
    var result = Read(ignored.Append(graphic).ToArray(), 1);
    SameReferences(new object[] { graphic }, result.Graphics);
});

Check("the exact relevant selection bound is accepted", () =>
{
    var graphics = Enumerable.Range(0, 4096).Select(i => (object)new GraphicPin(new TestPin("Signal " + i))).ToArray();
    var result = Read(graphics);
    Equal(4096, result.Graphics.Length);
    Equal(4096, result.Pins.Count);
});

Check("exceeding the relevant selection bound rejects the complete selection", () =>
{
    var graphics = Enumerable.Range(0, 4097).Select(i => (object)new GraphicPin(new TestPin("Signal " + i))).ToArray();
    Throws<InvalidOperationException>(() => Read(graphics), "4096 selected Named Signal");
});

Check("a supported graphic with a missing inner pin is not silently skipped", () =>
    Throws<MissingMemberException>(() => Read(new object[] { new GraphicPin(null) }), "InnerPin"));

Check("a supported graphic with a missing label property is rejected", () =>
    Throws<MissingMemberException>(() => Read(new object[] { new GraphicPin(new object()) }), "Label"));

Check("a supported tag with a missing pin is rejected", () =>
    Throws<MissingMemberException>(() => Read(new object[] { new PinLabel(null) }), "Pin"));

Check("a supported tag with an invalid pin representation is rejected", () =>
    Throws<MissingMemberException>(() => Read(new object[] { new PinLabel(new object()) }), "InnerPin"));

Check("changing ignored objects preserves the relevant revalidation snapshot", () =>
{
    var graphic = new GraphicPin(new TestPin("Music"));
    var before = Read(new object[] { new Component(), graphic });
    var after = Read(new object[] { graphic, new object(), new GraphicPin(new TestPin("")) });
    SameReferences(before.Graphics, after.Graphics);
    SameReferences(before.Pins, after.Pins);
});

Check("changing selected Named Signals changes the relevant revalidation snapshot", () =>
{
    var first = new GraphicPin(new TestPin("Music"));
    var second = new PinLabel(new GraphicPin(new TestPin("Page")));
    var before = Read(new object[] { first, second });
    var after = Read(new object[] { first, new Component() });
    Equal(2, before.Graphics.Length);
    SameReferences(new object[] { first }, after.Graphics);
    Equal(false, before.Pins.SetEquals(after.Pins));
});

Check("an unnamed pin becomes relevant when it receives a name", () =>
{
    var pin = new TestPin("");
    var graphic = new GraphicPin(pin);
    Empty(Read(new object[] { graphic }));
    pin.Label = "Music";
    SameReferences(new object[] { graphic }, Read(new object[] { graphic }).Graphics);
    pin.Label = "";
    Empty(Read(new object[] { graphic }));
});

Console.WriteLine($"Passed {passed} selection checks.");

SignalSelection.Snapshot Read(object[] values, int limit = 4096) =>
    SignalSelection.Read(values, typeof(GraphicPin), typeof(PinLabel), limit);

void Check(string name, Action action)
{
    try { action(); passed++; Console.WriteLine("PASS " + name); }
    catch (Exception error) { throw new Exception("FAIL " + name, error); }
}

void Empty(SignalSelection.Snapshot result)
{
    Equal(0, result.Graphics.Length);
    Equal(0, result.Pins.Count);
}

void Equal<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new Exception($"Expected {expected}, got {actual}.");
}

void SameReferences(IEnumerable<object> expected, IEnumerable<object> actual)
{
    if (!new HashSet<object>(expected, ReferenceEqualityComparer.Instance).SetEquals(actual))
        throw new Exception("Object reference sets differ.");
}

void Throws<T>(Action action, string message) where T : Exception
{
    try { action(); }
    catch (T error)
    {
        if (!error.Message.Contains(message, StringComparison.Ordinal))
            throw new Exception("Unexpected error: " + error.Message);
        return;
    }
    throw new Exception("Expected " + typeof(T).Name);
}

sealed record TestPin(string? Name)
{
    public string? Label { get; set; } = Name;
}
class GraphicPin(object? pin) { public object? InnerPin => pin; }
class PinLabel(object? pin) { public object? Pin => pin; }
sealed class DerivedGraphicPin(object pin) : GraphicPin(pin);
sealed class DerivedPinLabel(object pin) : PinLabel(pin);
sealed class Component
{
    public object obj => throw new Exception("An ignored component must not be inspected.");
}
