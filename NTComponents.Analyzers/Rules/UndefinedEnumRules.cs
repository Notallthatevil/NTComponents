using Microsoft.CodeAnalysis;

namespace NTComponents.Analyzers;

/// <summary>Rules for undefined enum values, such as <c>(NTElevation)99</c>, passed to NT components.</summary>
internal static class UndefinedEnumRules {

    public static readonly DiagnosticDescriptor UndefinedValue = new(
        "NTC1089",
        "NT component enum parameters must use a defined value",
        "{0} {1} must be a defined {2} value",
        RuleCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1089");

    /// <summary>Parameters that already have a dedicated undefined-value rule (NTC1038, NTC1043, NTC1074).</summary>
    private static readonly HashSet<string> DedicatedRules = new(StringComparer.Ordinal) {
        "NTComponents.NTFabButton.Placement",
        "NTComponents.NTFabMenu.Placement",
        "NTComponents.NTCarousel.Appearance"
    };

    public static void Register(ComponentRuleRegistry registry) {
        registry.Describe(UndefinedValue);
        registry.OnAny(static context => {
            var frame = context.Frame;
            if (frame.IsNTComponent) {
                foreach (var attribute in frame.Attributes.Values) {
                    if (OperationConstants.TryGetEnum(attribute.Value, out var value)
                        && value.Member is null
                        && !value.IsFlags
                        && !DedicatedRules.Contains(frame.MetadataName + "." + attribute.Name)) {
                        context.Report(UndefinedValue, attribute.Location, frame.Type.Name, attribute.Name, value.Type.Name);
                    }
                }
            }
        });
    }
}
