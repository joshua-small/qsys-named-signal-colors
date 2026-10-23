using System.Collections;
using System.Reflection;

namespace Designer.Modules.NamedSignalColors;

internal static class SignalSelection
{
    internal sealed record Snapshot(object[] Graphics, HashSet<object> Pins);

    private static object? Property(object instance, string name)
    {
        var property = instance.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance)
            ?? throw new MissingMemberException(instance.GetType().FullName, name);
        return property.GetValue(instance);
    }

    private static object Required(object instance, string name) => Property(instance, name)
        ?? throw new MissingMemberException(instance.GetType().FullName, name);

    internal static string Label(object pin) => Property(pin, "Label") as string ?? "";

    internal static Snapshot Read(IEnumerable selection, Type graphicPin, Type pinLabel, int limit)
    {
        var graphics = new HashSet<object>(ReferenceEqualityComparer.Instance);
        var pins = new HashSet<object>(ReferenceEqualityComparer.Instance);
        foreach (var graphic in selection)
        {
            object pin;
            if (graphicPin.IsInstanceOfType(graphic)) pin = Required(graphic, "InnerPin");
            else if (pinLabel.IsInstanceOfType(graphic)) pin = Required(Required(graphic, "Pin"), "InnerPin");
            else continue;
            if (string.IsNullOrWhiteSpace(Label(pin))) continue;
            graphics.Add(graphic);
            pins.Add(pin);
            if (graphics.Count > limit || pins.Count > limit)
                throw new InvalidOperationException($"This preview exceeds {limit} selected Named Signal tags/pins. Nothing changed; use a smaller selection.");
        }
        // Only relevant graphics participate in Apply's selection revalidation.
        return new Snapshot(graphics.ToArray(), pins);
    }
}
