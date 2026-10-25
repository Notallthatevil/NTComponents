using Microsoft.CodeAnalysis;

namespace NTComponents.Analyzers;

/// <summary>A constant enum value and the member it names, if any.</summary>
internal readonly struct EnumConstant(INamedTypeSymbol type, IFieldSymbol? member) {
    public INamedTypeSymbol Type { get; } = type;

    /// <summary>The named member, or null when the value is an undefined cast such as <c>(NTColor)99</c>.</summary>
    public IFieldSymbol? Member { get; } = member;

    public bool IsFlags => Type.GetAttributes().Any(static attribute => attribute.AttributeClass?.ToDisplayString() == "System.FlagsAttribute");
}
