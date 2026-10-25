using Microsoft.CodeAnalysis;

namespace NTComponents.Analyzers;

/// <summary>
///     Rules for NT form inputs and NTForm. The attribute table, accessibility checks and adjusted-value checks live in
///     <see cref="NTInputOwnedAttributeRules" />, <see cref="NTInputAccessibilityRules" /> and <see cref="NTInputAdjustmentRules" />.
/// </summary>
internal static class NTInputRules {
    private const string BooleanInputBase = "NTComponents.NTBooleanInputBase";

    public static readonly DiagnosticDescriptor PhoneMaskRequiresTel = new(
        "NTC1059",
        "NTInputText PhoneMask requires tel input",
        "NTInputText PhoneMask is ignored unless InputType is TextInputType.Tel",
        RuleCategory.Usage,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1059");

    public static readonly DiagnosticDescriptor InvalidPhoneMask = new(
        "NTC1060",
        "NTInputText PhoneMask must contain digit placeholders",
        "NTInputText PhoneMask should contain at least one '#' digit placeholder",
        RuleCategory.Usage,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1060");

    public static readonly DiagnosticDescriptor MissingValueExpression = new(
        "NTC1062",
        "Validation-bound NT inputs require ValueExpression",
        "{0} sets Value or ValueChanged without ValueExpression; use @bind-Value or provide ValueExpression so EditContext validation can associate messages with the field",
        RuleCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1062");

    public static readonly DiagnosticDescriptor BooleanInputRequiredAttribute = new(
        "NTC1063",
        "Use Required for NT boolean input validation",
        "Use Required on {0} instead of a raw 'required' attribute so the component can add EditContext validation messages",
        RuleCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1063");

    public static readonly DiagnosticDescriptor EmptyRequiredSupportingText = new(
        "NTC1064",
        "NTForm required supporting text cannot be empty",
        "NTForm has ShowRequiredSupportingText enabled but RequiredSupportingText is empty",
        RuleCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1064");

    public static readonly DiagnosticDescriptor InvalidVisibleLines = new(
        "NTC1104",
        "NTTextArea visible line counts are invalid",
        "NTTextArea {0}",
        RuleCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1104");

    public static readonly DiagnosticDescriptor SelectMultiple = new(
        "NTC1105",
        "NTSelect does not support multi-select",
        "NTSelect throws when the 'multiple' attribute is set; use NTCombobox for multi-select",
        RuleCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1105");

    public static readonly DiagnosticDescriptor UnsupportedTypeArgument = new(
        "NTC1106",
        "Component does not support this type argument",
        "{0} does not support type argument '{1}'",
        RuleCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1106");

    public static void Register(ComponentRuleRegistry registry) {
        registry.Describe(PhoneMaskRequiresTel, InvalidPhoneMask, MissingValueExpression, BooleanInputRequiredAttribute, EmptyRequiredSupportingText, InvalidVisibleLines, SelectMultiple, UnsupportedTypeArgument);
        registry.On("NTComponents.NTInputText", AnalyzePhoneMask);
        registry.On("NTComponents.NTFormControlBaseCore`1", AnalyzeValueExpression);
        registry.On(BooleanInputBase, AnalyzeBooleanRequired);
        registry.On("NTComponents.NTForm", AnalyzeRequiredSupportingText);
        registry.On("NTComponents.NTTextArea", AnalyzeVisibleLines);
        registry.On("NTComponents.NTSelect`1", AnalyzeSelect);
        registry.On("NTComponents.NTInputDateTime`1", AnalyzeDateTimeType);
        NTInputOwnedAttributeRules.Register(registry);
        NTInputAccessibilityRules.Register(registry);
        NTInputAdjustmentRules.Register(registry);
    }

    private static void AnalyzePhoneMask(ComponentContext context) {
        var frame = context.Frame;
        if (!frame.Has("PhoneMask")) {
            return;
        }

        var inputType = frame.GetEnumName("InputType", "Text");
        if (inputType is null) {
            return;
        }

        var phoneMask = frame.Attributes["PhoneMask"];
        if (inputType != "Tel") {
            context.Report(PhoneMaskRequiresTel, Where(frame, "PhoneMask"));
        }
        else if (OperationConstants.TryGetString(phoneMask.Value, out var mask) && (mask is null || mask.IndexOf('#') < 0)) {
            context.Report(InvalidPhoneMask, Where(frame, "PhoneMask"));
        }
    }

    private static void AnalyzeValueExpression(ComponentContext context) {
        var frame = context.Frame;
        if (frame.HasSplattedAttributes || frame.Has("ValueExpression") || !(frame.Has("ValueChanged") || frame.Has("Value"))) {
            return;
        }

        context.Report(MissingValueExpression, Where(frame, frame.Has("ValueChanged") ? "ValueChanged" : "Value"), frame.Type.Name);
    }

    private static void AnalyzeBooleanRequired(ComponentContext context) {
        var frame = context.Frame;
        if (frame.Attributes.TryGetValue("required", out var required)) {
            context.Report(BooleanInputRequiredAttribute, Where(frame, "required"), frame.Type.Name);
        }
    }

    private static void AnalyzeRequiredSupportingText(ComponentContext context) {
        var frame = context.Frame;
        if (frame.GetBool("ShowRequiredSupportingText") == true && frame.Has("RequiredSupportingText") && frame.GetStringState("RequiredSupportingText") == StringState.Empty) {
            context.Report(EmptyRequiredSupportingText, Where(frame, "RequiredSupportingText"));
        }
    }

    /// <summary>Mirrors the checks NTTextArea.OnParametersSet throws for. MaxVisibleLines defaults to 5, so a large MinVisibleLines alone can throw.</summary>
    private static void AnalyzeVisibleLines(ComponentContext context) {
        var frame = context.Frame;
        var min = NumberOrDefault(frame, "MinVisibleLines", 2);
        var max = NumberOrDefault(frame, "MaxVisibleLines", 5);
        if (min < 1) {
            context.Report(InvalidVisibleLines, Where(frame, "MinVisibleLines"), "MinVisibleLines must be a positive number");
        }
        else if (max < 1) {
            context.Report(InvalidVisibleLines, Where(frame, "MaxVisibleLines"), "MaxVisibleLines must be a positive number when specified");
        }
        else if (max < min) {
            context.Report(InvalidVisibleLines, Where(frame, "MaxVisibleLines"), "MaxVisibleLines must be greater than or equal to MinVisibleLines");
        }
    }

    private static void AnalyzeSelect(ComponentContext context) {
        var frame = context.Frame;
        foreach (var attribute in frame.Attributes.Values) {
            if (string.Equals(attribute.Name, "multiple", StringComparison.OrdinalIgnoreCase)) {
                context.Report(SelectMultiple, Where(frame, attribute.Name));
            }
        }

        // NTSelect<T> accepts string and value types, including Nullable<T>; any other reference type throws.
        if (SingleTypeArgument(frame) is { IsReferenceType: true, TypeKind: not (TypeKind.TypeParameter or TypeKind.Error or TypeKind.Dynamic), SpecialType: not SpecialType.System_String } argument) {
            context.Report(UnsupportedTypeArgument, frame.Location, "NTSelect", argument.ToDisplayString());
        }
    }

    private static void AnalyzeDateTimeType(ComponentContext context) {
        var frame = context.Frame;
        if (SingleTypeArgument(frame) is not { TypeKind: not (TypeKind.TypeParameter or TypeKind.Error or TypeKind.Dynamic) } argument) {
            return;
        }

        var underlying = UnderlyingType(argument);
        if (!IsSystemType(underlying, "DateTime", "DateTimeOffset", "DateOnly", "TimeOnly")) {
            context.Report(UnsupportedTypeArgument, frame.Location, "NTInputDateTime", argument.ToDisplayString());
        }
    }

    /// <summary>The attribute's location, or the component's when Razor leaves the attribute unmapped (raw string attributes have no markup mapping).</summary>
    internal static Location Where(ComponentFrame frame, string name) => SourceLocations.Prefer(frame.Location, [frame.LocationOf(name)]);

    internal static ITypeSymbol? SingleTypeArgument(ComponentFrame frame) => frame.Type.TypeArguments.Length == 1 ? frame.Type.TypeArguments[0] : null;

    /// <summary>Strips <c>Nullable&lt;T&gt;</c>, like the components' <c>Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T)</c>.</summary>
    internal static ITypeSymbol UnderlyingType(ITypeSymbol type) =>
        type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable ? nullable.TypeArguments[0] : type;

    internal static bool IsSystemType(ITypeSymbol type, params string[] names) =>
        type is INamedTypeSymbol { ContainingNamespace: { Name: "System", ContainingNamespace.IsGlobalNamespace: true } } && Array.IndexOf(names, type.Name) >= 0;

    /// <summary>The constant value of a numeric parameter, its default when omitted, or null when unknown or an explicit null.</summary>
    internal static double? NumberOrDefault(ComponentFrame frame, string name, double defaultValue) =>
        frame.Has(name) ? frame.GetNumber(name) : frame.HasSplattedAttributes ? null : defaultValue;
}
