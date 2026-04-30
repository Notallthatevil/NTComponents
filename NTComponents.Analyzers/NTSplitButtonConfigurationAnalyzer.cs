using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace NTComponents.Analyzers;

/// <summary>
///     Warns when <c>NTSplitButton</c> is configured in a way that the component rejects at runtime.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NTSplitButtonConfigurationAnalyzer : DiagnosticAnalyzer {

    public const string EmptyLabelDiagnosticId = "NTC1009";
    public const string MissingActionAriaLabelDiagnosticId = "NTC1010";
    public const string OpaqueBackgroundDiagnosticId = "NTC1011";
    public const string TransparentBackgroundDiagnosticId = "NTC1012";
    public const string InvisibleTextColorDiagnosticId = "NTC1013";
    public const string InvalidElevationDiagnosticId = "NTC1014";
    public const string InvisibleMenuColorDiagnosticId = "NTC1015";
    public const string MissingMenuItemDiagnosticId = "NTC1016";
    public const string EmptyMenuItemLabelDiagnosticId = "NTC1017";
    public const string EmptyMenuItemHrefDiagnosticId = "NTC1018";

    private static readonly DiagnosticDescriptor EmptyLabelRule = new(
        EmptyLabelDiagnosticId,
        "NTSplitButton label cannot be empty without an icon",
        "NTSplitButton requires a non-empty Label unless LeadingIcon is supplied",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor MissingActionAriaLabelRule = new(
        MissingActionAriaLabelDiagnosticId,
        "Icon-only NTSplitButton action needs an accessible label",
        "Icon-only NTSplitButton actions require a non-empty ActionAriaLabel",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor OpaqueBackgroundRule = new(
        OpaqueBackgroundDiagnosticId,
        "NTSplitButton background must be transparent for this variant",
        "NTSplitButton variant '{0}' must use a transparent BackgroundColor",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor TransparentBackgroundRule = new(
        TransparentBackgroundDiagnosticId,
        "NTSplitButton background must be visible for this variant",
        "NTSplitButton variant '{0}' must use a visible container BackgroundColor",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor InvisibleTextColorRule = new(
        InvisibleTextColorDiagnosticId,
        "NTSplitButton text color must be visible",
        "NTSplitButton TextColor must be a visible content color",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor InvalidElevationRule = new(
        InvalidElevationDiagnosticId,
        "NTSplitButton elevation is invalid for this variant",
        "NTSplitButton variant '{0}' cannot use Elevation '{1}'",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor InvisibleMenuColorRule = new(
        InvisibleMenuColorDiagnosticId,
        "NTSplitButton menu colors must be visible",
        "NTSplitButton {0} must be a visible menu color",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor MissingMenuItemRule = new(
        MissingMenuItemDiagnosticId,
        "NTSplitButton menu needs an actionable item",
        "NTSplitButton requires at least one NTSplitButtonButtonItem or NTSplitButtonAnchorItem child",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor EmptyMenuItemLabelRule = new(
        EmptyMenuItemLabelDiagnosticId,
        "NTSplitButton menu item label cannot be empty",
        "{0} requires a non-empty Label",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor EmptyMenuItemHrefRule = new(
        EmptyMenuItemHrefDiagnosticId,
        "NTSplitButton anchor item href cannot be empty",
        "NTSplitButtonAnchorItem requires a non-empty Href",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [
        EmptyLabelRule,
        MissingActionAriaLabelRule,
        OpaqueBackgroundRule,
        TransparentBackgroundRule,
        InvisibleTextColorRule,
        InvalidElevationRule,
        InvisibleMenuColorRule,
        MissingMenuItemRule,
        EmptyMenuItemLabelRule,
        EmptyMenuItemHrefRule
    ];

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context) {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(static startContext => {
            var splitButtonType = startContext.Compilation.GetTypeByMetadataName("NTComponents.NTSplitButton");
            var buttonItemType = startContext.Compilation.GetTypeByMetadataName("NTComponents.NTSplitButtonButtonItem");
            var anchorItemType = startContext.Compilation.GetTypeByMetadataName("NTComponents.NTSplitButtonAnchorItem");
            var dividerItemType = startContext.Compilation.GetTypeByMetadataName("NTComponents.NTSplitButtonDividerItem");
            var buttonVariantType = startContext.Compilation.GetTypeByMetadataName("NTComponents.NTButtonVariant");
            var colorType = startContext.Compilation.GetTypeByMetadataName("NTComponents.TnTColor");
            var elevationType = startContext.Compilation.GetTypeByMetadataName("NTComponents.NTElevation");

            if (splitButtonType is null
                || buttonItemType is null
                || anchorItemType is null
                || dividerItemType is null
                || buttonVariantType is null
                || colorType is null
                || elevationType is null) {
                return;
            }

            var componentTypes = new ComponentTypes(splitButtonType, buttonItemType, anchorItemType, dividerItemType);

            startContext.RegisterSyntaxNodeAction(
                nodeContext => AnalyzeExecutableNode(nodeContext, componentTypes, buttonVariantType, colorType, elevationType),
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.MethodDeclaration,
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.ConstructorDeclaration,
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.LocalFunctionStatement,
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.ParenthesizedLambdaExpression,
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.SimpleLambdaExpression,
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.AnonymousMethodExpression);
        });
    }

    private static void AnalyzeExecutableNode(
        SyntaxNodeAnalysisContext context,
        ComponentTypes componentTypes,
        INamedTypeSymbol buttonVariantType,
        INamedTypeSymbol colorType,
        INamedTypeSymbol elevationType) {
        var bodyNode = GetBodyNode(context.Node);
        if (bodyNode is null) {
            return;
        }

        var invocations = bodyNode
            .DescendantNodes(static node => !IsNestedExecutableBoundary(node))
            .OfType<InvocationExpressionSyntax>()
            .OrderBy(static invocation => invocation.SpanStart);

        var stack = new Stack<ComponentFrame>();

        foreach (var invocation in invocations) {
            if (TryGetOpenedComponent(invocation, context.SemanticModel, componentTypes, out var componentKind)) {
                stack.Push(new ComponentFrame(componentKind, invocation.GetLocation()));
                continue;
            }

            if (IsCloseComponentInvocation(invocation, context.SemanticModel)) {
                if (stack.Count == 0) {
                    continue;
                }

                AnalyzeComponentFrame(context, stack.Pop(), componentTypes, buttonVariantType, colorType, elevationType);
                continue;
            }

            if (stack.Count == 0 || stack.Peek().Kind == ComponentKind.None) {
                continue;
            }

            if (TryGetComponentAttribute(invocation, context.SemanticModel, out var name, out var attributeValue)) {
                stack.Peek().Attributes[name] = attributeValue;
            }
        }
    }

    private static void AnalyzeComponentFrame(
        SyntaxNodeAnalysisContext context,
        ComponentFrame frame,
        ComponentTypes componentTypes,
        INamedTypeSymbol buttonVariantType,
        INamedTypeSymbol colorType,
        INamedTypeSymbol elevationType) {
        switch (frame.Kind) {
            case ComponentKind.SplitButton:
                AnalyzeSplitButtonFrame(context, frame, componentTypes, buttonVariantType, colorType, elevationType);
                break;

            case ComponentKind.ButtonItem:
                AnalyzeMenuItemLabel(context, frame, "NTSplitButtonButtonItem");
                break;

            case ComponentKind.AnchorItem:
                AnalyzeMenuItemLabel(context, frame, "NTSplitButtonAnchorItem");
                AnalyzeAnchorItemHref(context, frame);
                break;
        }
    }

    private static void AnalyzeSplitButtonFrame(
        SyntaxNodeAnalysisContext context,
        ComponentFrame frame,
        ComponentTypes componentTypes,
        INamedTypeSymbol buttonVariantType,
        INamedTypeSymbol colorType,
        INamedTypeSymbol elevationType) {
        var effectiveVariant = GetEffectiveVariant(frame, buttonVariantType);

        AnalyzeActionLabel(context, frame);
        AnalyzeBackgroundColor(context, frame, effectiveVariant, colorType);
        AnalyzeTextColor(context, frame, colorType);
        AnalyzeMenuColors(context, frame, colorType);
        AnalyzeElevation(context, frame, effectiveVariant, elevationType);
        AnalyzeMenuItems(context, frame, componentTypes);
    }

    private static void AnalyzeActionLabel(SyntaxNodeAnalysisContext context, ComponentFrame frame) {
        if (!TryGetKnownStringState(frame, "Label", defaultValue: string.Empty, out var labelState)
            || labelState != KnownStringState.Empty) {
            return;
        }

        var hasLeadingIcon = frame.Attributes.TryGetValue("LeadingIcon", out var leadingIcon)
            && !IsNullConstant(leadingIcon.Operation);
        var hasNonEmptyActionAriaLabel = TryGetKnownStringState(frame, "ActionAriaLabel", defaultValue: null, out var actionAriaLabelState)
            && actionAriaLabelState == KnownStringState.NonEmpty;

        if (!hasLeadingIcon) {
            context.ReportDiagnostic(Diagnostic.Create(EmptyLabelRule, GetAttributeOrComponentLocation(frame, "Label")));
            return;
        }

        if (!hasNonEmptyActionAriaLabel) {
            context.ReportDiagnostic(Diagnostic.Create(MissingActionAriaLabelRule, GetAttributeOrComponentLocation(frame, "ActionAriaLabel")));
        }
    }

    private static void AnalyzeBackgroundColor(
        SyntaxNodeAnalysisContext context,
        ComponentFrame frame,
        string? effectiveVariant,
        INamedTypeSymbol colorType) {
        if (effectiveVariant is null
            || !frame.Attributes.TryGetValue("BackgroundColor", out var backgroundColor)
            || !TryGetEnumMemberName(backgroundColor.Operation, colorType, out var colorName)) {
            return;
        }

        if (effectiveVariant is "Text" or "Outlined") {
            if (colorName != "Transparent") {
                context.ReportDiagnostic(Diagnostic.Create(OpaqueBackgroundRule, backgroundColor.Location, effectiveVariant));
            }

            return;
        }

        if (effectiveVariant is "Elevated" or "Filled" or "Tonal"
            && colorName is "None" or "Transparent") {
            context.ReportDiagnostic(Diagnostic.Create(TransparentBackgroundRule, backgroundColor.Location, effectiveVariant));
        }
    }

    private static void AnalyzeTextColor(SyntaxNodeAnalysisContext context, ComponentFrame frame, INamedTypeSymbol colorType) {
        if (frame.Attributes.TryGetValue("TextColor", out var textColor)
            && TryGetEnumMemberName(textColor.Operation, colorType, out var colorName)
            && colorName is "None" or "Transparent") {
            context.ReportDiagnostic(Diagnostic.Create(InvisibleTextColorRule, textColor.Location));
        }
    }

    private static void AnalyzeMenuColors(SyntaxNodeAnalysisContext context, ComponentFrame frame, INamedTypeSymbol colorType) {
        AnalyzeMenuColor(context, frame, colorType, "MenuBackgroundColor");
        AnalyzeMenuColor(context, frame, colorType, "MenuTextColor");
    }

    private static void AnalyzeMenuColor(SyntaxNodeAnalysisContext context, ComponentFrame frame, INamedTypeSymbol colorType, string attributeName) {
        if (frame.Attributes.TryGetValue(attributeName, out var menuColor)
            && TryGetEnumMemberName(menuColor.Operation, colorType, out var colorName)
            && colorName is "None" or "Transparent") {
            context.ReportDiagnostic(Diagnostic.Create(InvisibleMenuColorRule, menuColor.Location, attributeName));
        }
    }

    private static void AnalyzeElevation(
        SyntaxNodeAnalysisContext context,
        ComponentFrame frame,
        string? effectiveVariant,
        INamedTypeSymbol elevationType) {
        if (effectiveVariant is null
            || !frame.Attributes.TryGetValue("Elevation", out var elevation)
            || !TryGetEnumMemberName(elevation.Operation, elevationType, out var elevationName)) {
            return;
        }

        if (effectiveVariant == "Elevated") {
            if (elevationName == "None") {
                context.ReportDiagnostic(Diagnostic.Create(InvalidElevationRule, elevation.Location, effectiveVariant, elevationName));
            }

            return;
        }

        if (elevationName != "None") {
            context.ReportDiagnostic(Diagnostic.Create(InvalidElevationRule, elevation.Location, effectiveVariant, elevationName));
        }
    }

    private static void AnalyzeMenuItems(SyntaxNodeAnalysisContext context, ComponentFrame frame, ComponentTypes componentTypes) {
        if (!frame.Attributes.TryGetValue("ChildContent", out var childContent)) {
            context.ReportDiagnostic(Diagnostic.Create(MissingMenuItemRule, frame.Location));
            return;
        }

        if (TryGetStaticMenuItemStatus(childContent.Operation?.Syntax, context.SemanticModel, componentTypes, out var hasActionItem)
            && !hasActionItem) {
            context.ReportDiagnostic(Diagnostic.Create(MissingMenuItemRule, childContent.Location));
        }
    }

    private static void AnalyzeMenuItemLabel(SyntaxNodeAnalysisContext context, ComponentFrame frame, string componentName) {
        if (TryGetKnownStringState(frame, "Label", defaultValue: string.Empty, out var labelState)
            && labelState == KnownStringState.Empty) {
            context.ReportDiagnostic(Diagnostic.Create(
                EmptyMenuItemLabelRule,
                GetAttributeOrComponentLocation(frame, "Label"),
                componentName));
        }
    }

    private static void AnalyzeAnchorItemHref(SyntaxNodeAnalysisContext context, ComponentFrame frame) {
        if (TryGetKnownStringState(frame, "Href", defaultValue: string.Empty, out var hrefState)
            && hrefState == KnownStringState.Empty) {
            context.ReportDiagnostic(Diagnostic.Create(EmptyMenuItemHrefRule, GetAttributeOrComponentLocation(frame, "Href")));
        }
    }

    private static string? GetEffectiveVariant(ComponentFrame frame, INamedTypeSymbol buttonVariantType) {
        if (!frame.Attributes.TryGetValue("Variant", out var variantValue)) {
            return "Filled";
        }

        return TryGetEnumMemberName(variantValue.Operation, buttonVariantType, out var variantName)
            ? variantName
            : null;
    }

    private static bool TryGetStaticMenuItemStatus(SyntaxNode? syntax, SemanticModel semanticModel, ComponentTypes componentTypes, out bool hasActionItem) {
        hasActionItem = false;

        if (syntax is null) {
            return false;
        }

        var lambda = syntax.DescendantNodesAndSelf().OfType<AnonymousFunctionExpressionSyntax>().FirstOrDefault();
        if (lambda is null || GetBodyNode(lambda) is not { } bodyNode) {
            return false;
        }

        var foundAnyKnownMenuItem = false;
        var invocations = bodyNode
            .DescendantNodes(static node => !IsNestedExecutableBoundary(node))
            .OfType<InvocationExpressionSyntax>();

        foreach (var invocation in invocations) {
            if (!TryGetOpenedComponent(invocation, semanticModel, componentTypes, out var componentKind)) {
                continue;
            }

            switch (componentKind) {
                case ComponentKind.ButtonItem:
                case ComponentKind.AnchorItem:
                    hasActionItem = true;
                    return true;

                case ComponentKind.DividerItem:
                    foundAnyKnownMenuItem = true;
                    break;
            }
        }

        return foundAnyKnownMenuItem;
    }

    private static Location GetAttributeOrComponentLocation(ComponentFrame frame, string attributeName) {
        return frame.Attributes.TryGetValue(attributeName, out var attribute)
            ? attribute.Location
            : frame.Location;
    }

    private static bool TryGetKnownStringState(ComponentFrame frame, string attributeName, string? defaultValue, out KnownStringState state) {
        if (!frame.Attributes.TryGetValue(attributeName, out var attribute)) {
            state = string.IsNullOrWhiteSpace(defaultValue) ? KnownStringState.Empty : KnownStringState.NonEmpty;
            return true;
        }

        if (!TryGetStringConstant(attribute.Operation, out var value)) {
            state = KnownStringState.Unknown;
            return false;
        }

        state = string.IsNullOrWhiteSpace(value) ? KnownStringState.Empty : KnownStringState.NonEmpty;
        return true;
    }

    private static SyntaxNode? GetBodyNode(SyntaxNode node) {
        return node switch {
            BaseMethodDeclarationSyntax method => method.Body,
            LocalFunctionStatementSyntax localFunction => localFunction.Body,
            ParenthesizedLambdaExpressionSyntax lambda when lambda.Body is BlockSyntax block => block,
            SimpleLambdaExpressionSyntax lambda when lambda.Body is BlockSyntax block => block,
            AnonymousMethodExpressionSyntax anonymousMethod => anonymousMethod.Body,
            _ => null
        };
    }

    private static bool IsNestedExecutableBoundary(SyntaxNode node) {
        return node is BaseMethodDeclarationSyntax
            or LocalFunctionStatementSyntax
            or ParenthesizedLambdaExpressionSyntax
            or SimpleLambdaExpressionSyntax
            or AnonymousMethodExpressionSyntax;
    }

    private static bool TryGetOpenedComponent(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel,
        ComponentTypes componentTypes,
        out ComponentKind componentKind) {
        componentKind = ComponentKind.None;

        if (!TryGetInvocationTarget(invocation, semanticModel, out var methodSymbol)
            || methodSymbol.Name != "OpenComponent"
            || methodSymbol.ContainingType.ToDisplayString() != "Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder") {
            return false;
        }

        ITypeSymbol? openedComponentType = null;
        if (methodSymbol.IsGenericMethod && methodSymbol.TypeArguments.Length == 1) {
            openedComponentType = methodSymbol.TypeArguments[0];
        }
        else if (invocation.ArgumentList.Arguments.Count >= 2) {
            var typeOperation = semanticModel.GetOperation(invocation.ArgumentList.Arguments[1].Expression);
            if (typeOperation is ITypeOfOperation typeOfOperation) {
                openedComponentType = typeOfOperation.TypeOperand;
            }
        }

        if (openedComponentType is null) {
            return false;
        }

        componentKind = componentTypes.GetComponentKind(openedComponentType);
        return true;
    }

    private static bool IsCloseComponentInvocation(InvocationExpressionSyntax invocation, SemanticModel semanticModel) {
        return TryGetInvocationTarget(invocation, semanticModel, out var methodSymbol)
            && methodSymbol.Name == "CloseComponent"
            && methodSymbol.ContainingType.ToDisplayString() == "Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder";
    }

    private static bool TryGetComponentAttribute(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel,
        out string name,
        out RecordedAttribute attribute) {
        name = string.Empty;
        attribute = default;

        if (!TryGetInvocationTarget(invocation, semanticModel, out var methodSymbol)
            || methodSymbol.ContainingType.ToDisplayString() != "Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder"
            || (methodSymbol.Name != "AddAttribute" && methodSymbol.Name != "AddComponentParameter")
            || invocation.ArgumentList.Arguments.Count < 3) {
            return false;
        }

        var nameArgument = invocation.ArgumentList.Arguments[1].Expression;
        var nameConstant = semanticModel.GetConstantValue(nameArgument);
        if (!nameConstant.HasValue || nameConstant.Value is not string attributeName) {
            return false;
        }

        var valueExpression = invocation.ArgumentList.Arguments[2].Expression;
        name = attributeName;
        attribute = new RecordedAttribute(valueExpression.GetLocation(), semanticModel.GetOperation(valueExpression));
        return true;
    }

    private static bool IsNullConstant(IOperation? operation) {
        operation = UnwrapOperation(operation);
        return operation?.ConstantValue.HasValue == true && operation.ConstantValue.Value is null;
    }

    private static bool TryGetStringConstant(IOperation? operation, out string value) {
        value = string.Empty;
        operation = UnwrapOperation(operation);

        if (operation?.ConstantValue.HasValue == true && operation.ConstantValue.Value is string stringValue) {
            value = stringValue;
            return true;
        }

        return false;
    }

    private static bool TryGetEnumMemberName(IOperation? operation, INamedTypeSymbol enumType, out string memberName) {
        memberName = string.Empty;
        operation = UnwrapOperation(operation);

        if (operation is IFieldReferenceOperation fieldReference
            && SymbolEqualityComparer.Default.Equals(fieldReference.Field.ContainingType, enumType)) {
            memberName = fieldReference.Field.Name;
            return true;
        }

        if (operation?.ConstantValue.HasValue != true) {
            return false;
        }

        var constantValue = Convert.ToInt64(operation.ConstantValue.Value);
        foreach (var field in enumType.GetMembers().OfType<IFieldSymbol>()) {
            if (!field.HasConstantValue) {
                continue;
            }

            if (Convert.ToInt64(field.ConstantValue) == constantValue) {
                memberName = field.Name;
                return true;
            }
        }

        return false;
    }

    private static IOperation? UnwrapOperation(IOperation? operation) {
        while (operation is not null) {
            switch (operation) {
                case IConversionOperation conversion:
                    operation = conversion.Operand;
                    continue;
                case IParenthesizedOperation parenthesized:
                    operation = parenthesized.Operand;
                    continue;
                case IInvocationOperation invocation
                    when invocation.TargetMethod.Name == "TypeCheck"
                         && invocation.TargetMethod.ContainingType.ToDisplayString() == "Microsoft.AspNetCore.Components.CompilerServices.RuntimeHelpers"
                         && invocation.Arguments.Length == 1:
                    operation = invocation.Arguments[0].Value;
                    continue;
                default:
                    return operation;
            }
        }

        return null;
    }

    private static bool TryGetInvocationTarget(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel,
        out IMethodSymbol methodSymbol) {
        var symbol = semanticModel.GetSymbolInfo(invocation).Symbol
            ?? semanticModel.GetSymbolInfo(invocation).CandidateSymbols.FirstOrDefault();

        methodSymbol = symbol as IMethodSymbol ?? null!;
        return methodSymbol is not null;
    }

    private enum ComponentKind {
        None,
        SplitButton,
        ButtonItem,
        AnchorItem,
        DividerItem
    }

    private enum KnownStringState {
        Unknown,
        Empty,
        NonEmpty
    }

    private sealed class ComponentTypes(
        INamedTypeSymbol splitButton,
        INamedTypeSymbol buttonItem,
        INamedTypeSymbol anchorItem,
        INamedTypeSymbol dividerItem) {

        public ComponentKind GetComponentKind(ITypeSymbol componentType) {
            if (SymbolEqualityComparer.Default.Equals(componentType, splitButton)) {
                return ComponentKind.SplitButton;
            }

            if (SymbolEqualityComparer.Default.Equals(componentType, buttonItem)) {
                return ComponentKind.ButtonItem;
            }

            if (SymbolEqualityComparer.Default.Equals(componentType, anchorItem)) {
                return ComponentKind.AnchorItem;
            }

            return SymbolEqualityComparer.Default.Equals(componentType, dividerItem)
                ? ComponentKind.DividerItem
                : ComponentKind.None;
        }
    }

    private sealed class ComponentFrame(ComponentKind kind, Location location) {
        public Dictionary<string, RecordedAttribute> Attributes { get; } = new(StringComparer.Ordinal);

        public ComponentKind Kind { get; } = kind;

        public Location Location { get; } = location;
    }

    private readonly struct RecordedAttribute {
        public RecordedAttribute(Location location, IOperation? operation) {
            Location = location;
            Operation = operation;
        }

        public Location Location { get; }

        public IOperation? Operation { get; }
    }
}
