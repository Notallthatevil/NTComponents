using Microsoft.CodeAnalysis;

namespace NTComponents.Analyzers;

/// <summary>A parameter or attribute passed to a component, as recorded from the render tree.</summary>
internal sealed class ComponentAttribute(string name, IOperation? value, Location location) {
    public string Name { get; } = name;

    /// <summary>The value as written, before conversions and Razor type checks are unwrapped.</summary>
    public IOperation? Value { get; } = value;

    /// <summary>The value when it maps to markup, otherwise the attribute name.</summary>
    public Location Location { get; } = location;
}
