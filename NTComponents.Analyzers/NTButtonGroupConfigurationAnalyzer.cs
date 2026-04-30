using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace NTComponents.Analyzers;

/// <summary>
///     Warns when <c>NTButtonGroup</c> is configured in a way that the component rejects at runtime.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NTButtonGroupConfigurationAnalyzer : DiagnosticAnalyzer {

    public const string TextSelectableDiagnosticId = "NTC1019";
    public const string OpaqueBackgroundDiagnosticId = "NTC1020";
    public const string TransparentBackgroundDiagnosticId = "NTC1021";
    public const string InvisibleTextColorDiagnosticId = "NTC1022";
    public const string TransparentSelectedBackgroundDiagnosticId = "NTC1023";
    public const string InvisibleSelectedTextColorDiagnosticId = "NTC1024";
    public const string MissingIconOnlyAriaLabelDiagnosticId = "NTC1025";

    private static readonly DiagnosticDescriptor TextSelectableRule = new(
        TextSelectableDiagnosticId,
        "Text NTButtonGroup cannot be selectable",
        "NTButtonGroup variant 'Text' does not support selectable behavior",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor OpaqueBackgroundRule = new(
        OpaqueBackgroundDiagnosticId,
        "NTButtonGroup background must be transparent for this variant",
        "NTButtonGroup variant '{0}' must use a transparent BackgroundColor",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor TransparentBackgroundRule = new(
        TransparentBackgroundDiagnosticId,
        "NTButtonGroup background must be visible for this variant",
        "NTButtonGroup variant '{0}' must use a visible container BackgroundColor",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor InvisibleTextColorRule = new(
        InvisibleTextColorDiagnosticId,
        "NTButtonGroup text color must be visible",
        "NTButtonGroup TextColor must be a visible content color",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor TransparentSelectedBackgroundRule = new(
        TransparentSelectedBackgroundDiagnosticId,
        "NTButtonGroup selected background must be visible",
        "Selectable NTButtonGroup variant '{0}' must use a visible selected container SelectedBackgroundColor",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor InvisibleSelectedTextColorRule = new(
        InvisibleSelectedTextColorDiagnosticId,
        "NTButtonGroup selected text color must be visible",
        "Selectable NTButtonGroup SelectedTextColor must be a visible selected content color",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor MissingIconOnlyAriaLabelRule = new(
        MissingIconOnlyAriaLabelDiagnosticId,
        "Icon-only NTButtonGroup item needs an accessible label",
        "NTButtonGroupItem requires a non-empty AriaLabel when rendering an icon-only item",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [
        TextSelectableRule,
        OpaqueBackgroundRule,
        TransparentBackgroundRule,
        InvisibleTextColorRule,
        TransparentSelectedBackgroundRule,
        InvisibleSelectedTextColorRule,
        MissingIconOnlyAriaLabelRule
    ];

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context) {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(static startContext => {
            var buttonGroupType = startContext.Compilation.GetTypeByMetadataName("NTComponents.NTButtonGroup`1");
            var buttonGroupItemType = startContext.Compilation.GetTypeByMetadataName("NTComponents.NTButtonGroupItem`1");
            var buttonVariantType = startContext.Compilation.GetTypeByMetadataName("NTComponents.NTButtonVariant");
            var selectionModeType = startContext.Compilation.GetTypeByMetadataName("NTComponents.NTButtonGroupSelectionMode");
            var colorType = startContext.Compilation.GetTypeByMetadataName("NTComponents.TnTColor");

            if (buttonGroupType is null
                || buttonGroupItemType is null
                || buttonVariantType is null
                || selectionModeType is null
                || colorType is null) {
                return;
            }

            startContext.RegisterSyntaxNodeAction(
                nodeContext => AnalyzeExecutableNode(nodeContext, buttonGroupType, buttonGroupItemType, buttonVariantType, selectionModeType, colorType),
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
        INamedTypeSymbol buttonGroupType,
        INamedTypeSymbol buttonGroupItemType,
        INamedTypeSymbol buttonVariantType,
        INamedTypeSymbol selectionModeType,
        INamedTypeSymbol colorType) {
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
            if (TryGetOpenedComponent(invocation, context.SemanticModel, buttonGroupType, buttonGroupItemType, out var componentKind)) {
                stack.Push(new ComponentFrame(componentKind));
                continue;
            }

            if (IsCloseComponentInvocation(invocation, context.SemanticModel)) {
                if (stack.Count == 0) {
                    continue;
                }

                AnalyzeComponentFrame(context, stack.Pop(), buttonVariantType, selectionModeType, colorType);
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
        INamedTypeSymbol buttonVariantType,
        INamedTypeSymbol selectionModeType,
        INamedTypeSymbol colorType) {
        switch (frame.Kind) {
            case ComponentKind.ButtonGroup:
                AnalyzeButtonGroupFrame(context, frame, buttonVariantType, selectionModeType, colorType);
                break;

            case ComponentKind.ButtonGroupItem:
                AnalyzeButtonGroupItemFrame(context, frame);
                break;
        }
    }

    private static void AnalyzeButtonGroupFrame(
        SyntaxNodeAnalysisContext context,
        ComponentFrame frame,
        INamedTypeSymbol buttonVariantType,
        INamedTypeSymbol selectionModeType,
        INamedTypeSymbol colorType) {
        var effectiveVariant = GetEffectiveVariant(frame, buttonVariantType);
        var effectiveSelectionMode = GetEffectiveSelectionMode(frame, selectionModeType);
        var isSelectable = effectiveSelectionMode is null || effectiveSelectionMode != "None";

        AnalyzeTextSelectable(context, frame, effectiveVariant, effectiveSelectionMode);
        AnalyzeBackgroundColor(context, frame, effectiveVariant, colorType);
        AnalyzeSelectedBackgroundColor(context, frame, effectiveVariant, isSelectable, colorType);
        AnalyzeTextColor(context, frame, colorType);
        AnalyzeSelectedTextColor(context, frame, isSelectable, colorType);
    }

    private static void AnalyzeButtonGroupItemFrame(SyntaxNodeAnalysisContext context, ComponentFrame frame) {
        if (!frame.Attributes.TryGetValue("Icon", out var icon)
            || !IsKnownNonNull(icon.Operation)) {
            return;
        }

        if (TryGetKnownStringState(frame, "Label", out var labelState)
            && labelState == KnownStringState.Empty
            && TryGetKnownStringState(frame, "AriaLabel", out var ariaLabelState)
            && ariaLabelState == KnownStringState.Empty) {
            context.ReportDiagnostic(Diagnostic.Create(MissingIconOnlyAriaLabelRule, GetAttributeOrComponentLocation(frame, "AriaLabel", icon.Location)));
        }
    }

    private static void AnalyzeTextSelectable(SyntaxNodeAnalysisContext context, ComponentFrame frame, string? effectiveVariant, string? effectiveSelectionMode) {
        if (effectiveVariant == "Text" && effectiveSelectionMode != "None") {
            context.ReportDiagnostic(Diagnostic.Create(TextSelectableRule, GetAttributeOrComponentLocation(frame, "SelectionMode", frame.Attributes["Variant"].Location)));
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

    private static void AnalyzeSelectedBackgroundColor(
        SyntaxNodeAnalysisContext context,
        ComponentFrame frame,
        string? effectiveVariant,
        bool isSelectable,
        INamedTypeSymbol colorType) {
        if (effectiveVariant is null
            || !isSelectable
            || effectiveVariant == "Text"
            || !frame.Attributes.TryGetValue("SelectedBackgroundColor", out var selectedBackgroundColor)
            || !TryGetEnumMemberName(selectedBackgroundColor.Operation, colorType, out var colorName)
            || colorName is not ("None" or "Transparent")) {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(TransparentSelectedBackgroundRule, selectedBackgroundColor.Location, effectiveVariant));
    }

    private static void AnalyzeTextColor(SyntaxNodeAnalysisContext context, ComponentFrame frame, INamedTypeSymbol colorType) {
        if (frame.Attributes.TryGetValue("TextColor", out var textColor)
            && TryGetEnumMemberName(textColor.Operation, colorType, out var colorName)
            && colorName is "None" or "Transparent") {
            context.ReportDiagnostic(Diagnostic.Create(InvisibleTextColorRule, textColor.Location));
        }
    }

    private static void AnalyzeSelectedTextColor(SyntaxNodeAnalysisContext context, ComponentFrame frame, bool isSelectable, INamedTypeSymbol colorType) {
        if (!isSelectable) {
            return;
        }

        if (frame.Attributes.TryGetValue("SelectedTextColor", out var selectedTextColor)
            && TryGetEnumMemberName(selectedTextColor.Operation, colorType, out var colorName)
            && colorName is "None" or "Transparent") {
            context.ReportDiagnostic(Diagnostic.Create(InvisibleSelectedTextColorRule, selectedTextColor.Location));
        }
    }

    private static string? GetEffectiveVariant(ComponentFrame frame, INamedTypeSymbol buttonVariantType) {
        if (!frame.Attributes.TryGetValue("Variant", out var variantValue)) {
            return "Tonal";
        }

        return TryGetEnumMemberName(variantValue.Operation, buttonVariantType, out var variantName)
            ? variantName
            : null;
    }

    private static string? GetEffectiveSelectionMode(ComponentFrame frame, INamedTypeSymbol selectionModeType) {
        if (!frame.Attributes.TryGetValue("SelectionMode", out var selectionModeValue)) {
            return "Single";
        }

        return TryGetEnumMemberName(selectionModeValue.Operation, selectionModeType, out var selectionModeName)
            ? selectionModeName
            : null;
    }

    private static Location GetAttributeOrComponentLocation(ComponentFrame frame, string attributeName, Location fallback) {
        return frame.Attributes.TryGetValue(attributeName, out var attribute)
            ? attribute.Location
            : fallback;
    }

    private static bool TryGetKnownStringState(ComponentFrame frame, string attributeName, out KnownStringState state) {
        if (!frame.Attributes.TryGetValue(attributeName, out var attribute)) {
            state = KnownStringState.Empty;
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
        INamedTypeSymbol buttonGroupType,
        INamedTypeSymbol buttonGroupItemType,
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

        if (IsSameGenericDefinition(openedComponentType, buttonGroupType)) {
            componentKind = ComponentKind.ButtonGroup;
            return true;
        }

        if (IsSameGenericDefinition(openedComponentType, buttonGroupItemType)) {
            componentKind = ComponentKind.ButtonGroupItem;
            return true;
        }

        return true;
    }

    private static bool IsSameGenericDefinition(ITypeSymbol type, INamedTypeSymbol genericDefinition) {
        return type is INamedTypeSymbol namedType
            && SymbolEqualityComparer.Default.Equals(namedType.OriginalDefinition, genericDefinition);
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

    private static bool IsKnownNonNull(IOperation? operation) {
        operation = UnwrapOperation(operation);
        return operation switch {
            IObjectCreationOperation => true,
            IFieldReferenceOperation => true,
            _ => operation?.ConstantValue.HasValue == true && operation.ConstantValue.Value is not null
        };
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
        ButtonGroup,
        ButtonGroupItem
    }

    private enum KnownStringState {
        Unknown,
        Empty,
        NonEmpty
    }

    private sealed class ComponentFrame(ComponentKind kind) {
        public Dictionary<string, RecordedAttribute> Attributes { get; } = new(StringComparer.Ordinal);

        public ComponentKind Kind { get; } = kind;
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
