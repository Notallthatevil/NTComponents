using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace NTComponents.Analyzers;

/// <summary>One component opened in a render tree, with its parameters and the components rendered inside it.</summary>
internal sealed class ComponentFrame {
    private readonly Dictionary<string, ComponentAttribute> _attributes = new(StringComparer.Ordinal);

    public ComponentFrame(INamedTypeSymbol type, InvocationExpressionSyntax openSyntax, Location location, ComponentFrame? parent, bool isConditional, bool isPageRoot) {
        Type = type;
        OpenSyntax = openSyntax;
        Location = location;
        Parent = parent;
        IsConditional = isConditional;
        IsPageRoot = isPageRoot;
        MetadataName = MetadataNameOf(type);
    }

    public INamedTypeSymbol Type { get; }

    /// <summary>The full metadata name of the generic definition, such as <c>NTComponents.NTDataGrid`1</c>.</summary>
    public string MetadataName { get; }

    public bool IsNTComponent => Type.ContainingAssembly?.Name == "NTComponents";

    /// <summary>The nearest component this one is rendered inside, if the walker can see it.</summary>
    public ComponentFrame? Parent { get; }

    /// <summary>Components rendered directly inside this one, through child content or between its open and close calls.</summary>
    public List<ComponentFrame> Children { get; } = [];

    /// <summary>True when an <c>@if</c>, loop, or switch between this component and its parent may render it zero or many times.</summary>
    public bool IsConditional { get; }

    /// <summary>True for a top-level component in a routable page, where no parent component can supply cascading values.</summary>
    public bool IsPageRoot { get; }

    /// <summary>True when the content renders a fragment the walker cannot see into, such as <c>@ChildContent</c>.</summary>
    public bool RendersOpaqueContent { get; set; }

    /// <summary>True when <c>@attributes</c> or a computed attribute name may supply parameters the walker cannot see.</summary>
    public bool HasSplattedAttributes { get; set; }

    public Location Location { get; private set; }

    public IReadOnlyDictionary<string, ComponentAttribute> Attributes => _attributes;

    internal InvocationExpressionSyntax OpenSyntax { get; }

    public bool Has(string name) => _attributes.ContainsKey(name);

    /// <summary>The attribute's location when it maps to markup, otherwise the component's. Razor maps neither raw HTML attributes nor fragments.</summary>
    public Location LocationOf(string name) => _attributes.TryGetValue(name, out var attribute) && SourceLocations.IsMapped(attribute.Location) ? attribute.Location : Location;

    public bool IsOrDerivesFrom(string metadataName) {
        for (var type = Type.OriginalDefinition; type is not null; type = type.BaseType?.OriginalDefinition) {
            if (MetadataNameOf(type) == metadataName) {
                return true;
            }
        }

        return false;
    }

    public IEnumerable<ComponentFrame> Ancestors() {
        for (var ancestor = Parent; ancestor is not null; ancestor = ancestor.Parent) {
            yield return ancestor;
        }
    }

    public IEnumerable<ComponentFrame> Descendants() => Children.SelectMany(static child => new[] { child }.Concat(child.Descendants()));

    /// <summary>Classifies a string parameter, using <paramref name="defaultValue" /> when it is omitted.</summary>
    public StringState GetStringState(string name, string? defaultValue = null) {
        if (!_attributes.TryGetValue(name, out var attribute)) {
            return HasSplattedAttributes ? StringState.Unknown : Classify(defaultValue);
        }

        return OperationConstants.TryGetString(attribute.Value, out var value) ? Classify(value) : StringState.Unknown;
    }

    /// <summary>Returns a constant bool, <paramref name="defaultValue" /> when omitted, or null when unknown.</summary>
    public bool? GetBool(string name, bool defaultValue = false) {
        if (!_attributes.TryGetValue(name, out var attribute)) {
            return HasSplattedAttributes ? null : defaultValue;
        }

        return OperationConstants.TryGetBool(attribute.Value, out var value) ? value : null;
    }

    /// <summary>
    ///     Returns the constant enum member name, <paramref name="defaultName" /> when omitted or null, or null when the value is
    ///     unknown or not a defined member.
    /// </summary>
    public string? GetEnumName(string name, string? defaultName = null) {
        if (!_attributes.TryGetValue(name, out var attribute)) {
            return HasSplattedAttributes ? null : defaultName;
        }

        if (OperationConstants.IsNull(attribute.Value)) {
            return defaultName;
        }

        return OperationConstants.TryGetEnum(attribute.Value, out var value) ? value.Member?.Name : null;
    }

    /// <summary>Returns a constant number, or null when omitted, null, or unknown.</summary>
    public double? GetNumber(string name) =>
        _attributes.TryGetValue(name, out var attribute) && OperationConstants.TryGetNumber(attribute.Value, out var value) ? value : null;

    /// <summary>True when the parameter is written with a value other than a constant null.</summary>
    public bool HasNonNull(string name) => _attributes.TryGetValue(name, out var attribute) && !OperationConstants.IsNull(attribute.Value);

    /// <summary>True when a required, non-nullable parameter such as Icon is omitted or explicitly null. Any other value counts as present.</summary>
    public bool IsOmittedOrNull(string name) => _attributes.TryGetValue(name, out var attribute) ? OperationConstants.IsNull(attribute.Value) : !HasSplattedAttributes;

    /// <summary>
    ///     Returns true for a RenderFragment written inline (a lambda, as Razor emits for child content), false when the parameter is omitted
    ///     or null, and null when it is a variable that may be null or when <c>@attributes</c> may supply it.
    /// </summary>
    public bool? IsFragmentSet(string name) {
        if (!_attributes.TryGetValue(name, out var attribute)) {
            return HasSplattedAttributes ? null : false;
        }

        if (OperationConstants.IsNull(attribute.Value)) {
            return false;
        }

        var value = OperationConstants.Unwrap(attribute.Value);
        return (value is IDelegateCreationOperation creation ? creation.Target : value) is IAnonymousFunctionOperation ? true : null;
    }

    internal void SetAttribute(ComponentAttribute attribute) {
        _attributes[attribute.Name] = attribute;
        if (!SourceLocations.IsMapped(Location) && SourceLocations.IsMapped(attribute.Location)) {
            Location = attribute.Location;
        }
    }

    internal static string MetadataNameOf(INamedTypeSymbol type) {
        var definition = type.OriginalDefinition;
        var prefix = definition.ContainingType is { } outer
            ? MetadataNameOf(outer) + "+"
            : definition.ContainingNamespace is { IsGlobalNamespace: false } containingNamespace ? containingNamespace.ToDisplayString() + "." : string.Empty;
        return prefix + definition.MetadataName;
    }

    private static StringState Classify(string? value) => string.IsNullOrWhiteSpace(value) ? StringState.Empty : StringState.NonEmpty;
}
