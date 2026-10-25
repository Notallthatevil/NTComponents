using Microsoft.CodeAnalysis;

namespace NTComponents.Analyzers;

/// <summary>Rules for NTVirtualize. Each one mirrors a check NTVirtualize.OnParametersSet throws for.</summary>
internal static class NTVirtualizeRules {

    public static readonly DiagnosticDescriptor MissingItemsProvider = new(
        "NTC1097",
        "NTVirtualize requires an ItemsProvider",
        "NTVirtualize requires the ItemsProvider parameter to be specified and non-null",
        RuleCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1097");

    public static readonly DiagnosticDescriptor InvalidItemSize = new(
        "NTC1098",
        "NTVirtualize ItemSize must be positive",
        "NTVirtualize ItemSize must be a finite value greater than zero",
        RuleCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1098");

    public static void Register(ComponentRuleRegistry registry) {
        registry.Describe(MissingItemsProvider, InvalidItemSize);
        registry.On("NTComponents.NTVirtualize`1", Analyze);
    }

    private static void Analyze(ComponentContext context) {
        var frame = context.Frame;
        var missing = frame.Attributes.TryGetValue("ItemsProvider", out var provider) ? OperationConstants.IsNull(provider.Value) : !frame.HasSplattedAttributes;
        if (missing) {
            context.Report(MissingItemsProvider, frame.LocationOf("ItemsProvider"));
        }

        if (frame.GetNumber("ItemSize") is { } size && (double.IsNaN(size) || double.IsInfinity(size) || size <= 0)) {
            context.Report(InvalidItemSize, frame.LocationOf("ItemSize"));
        }
    }
}
