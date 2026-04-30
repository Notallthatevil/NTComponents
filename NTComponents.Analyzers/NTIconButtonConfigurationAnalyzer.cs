using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace NTComponents.Analyzers;

/// <summary>
///     Warns when <c>NTIconButton</c> is configured in a way that the component rejects at runtime.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NTIconButtonConfigurationAnalyzer : DiagnosticAnalyzer {

    public const string MissingIconDiagnosticId = "NTC1026";
    public const string EmptyAriaLabelDiagnosticId = "NTC1027";
    public const string OpaqueBackgroundDiagnosticId = "NTC1028";
    public const string TransparentBackgroundDiagnosticId = "NTC1029";
    public const string InvisibleTextColorDiagnosticId = "NTC1030";
    public const string InvalidElevationDiagnosticId = "NTC1031";

    private static readonly DiagnosticDescriptor MissingIconRule = new(
        MissingIconDiagnosticId,
        "NTIconButton icon is required",
        "NTIconButton requires a non-null Icon parameter",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor EmptyAriaLabelRule = new(
        EmptyAriaLabelDiagnosticId,
        "NTIconButton aria label cannot be empty",
        "NTIconButton requires a non-empty AriaLabel",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor OpaqueBackgroundRule = new(
        OpaqueBackgroundDiagnosticId,
        "NTIconButton background must be transparent for this variant",
        "NTIconButton variant '{0}' must use a transparent BackgroundColor",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor TransparentBackgroundRule = new(
        TransparentBackgroundDiagnosticId,
        "NTIconButton background must be visible for this variant",
        "NTIconButton variant '{0}' must use a visible container BackgroundColor",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor InvisibleTextColorRule = new(
        InvisibleTextColorDiagnosticId,
        "NTIconButton text color must be visible",
        "NTIconButton TextColor must be a visible icon color",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor InvalidElevationRule = new(
        InvalidElevationDiagnosticId,
        "NTIconButton elevation is invalid for this variant",
        "NTIconButton variant '{0}' cannot use Elevation '{1}'",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [
        MissingIconRule,
        EmptyAriaLabelRule,
        OpaqueBackgroundRule,
        TransparentBackgroundRule,
        InvisibleTextColorRule,
        InvalidElevationRule
    ];

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context) {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(static startContext => {
            var iconButtonType = startContext.Compilation.GetTypeByMetadataName("NTComponents.NTIconButton");
            var buttonVariantType = startContext.Compilation.GetTypeByMetadataName("NTComponents.NTButtonVariant");
            var colorType = startContext.Compilation.GetTypeByMetadataName("NTComponents.TnTColor");
            var elevationType = startContext.Compilation.GetTypeByMetadataName("NTComponents.NTElevation");

            if (iconButtonType is null || buttonVariantType is null || colorType is null || elevationType is null) {
                return;
            }

            startContext.RegisterSyntaxNodeAction(
                nodeContext => AnalyzeExecutableNode(nodeContext, iconButtonType, buttonVariantType, colorType, elevationType),
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
        INamedTypeSymbol iconButtonType,
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
            if (TryGetOpenedComponent(invocation, context.SemanticModel, iconButtonType, out var isIconButtonComponent)) {
                stack.Push(new ComponentFrame(isIconButtonComponent, invocation.GetLocation()));
                continue;
            }

            if (IsCloseComponentInvocation(invocation, context.SemanticModel)) {
                if (stack.Count == 0) {
                    continue;
                }

                var frame = stack.Pop();
                if (frame.IsIconButton) {
                    AnalyzeComponentFrame(context, frame, buttonVariantType, colorType, elevationType);
                }

                continue;
            }

            if (stack.Count == 0 || !stack.Peek().IsIconButton) {
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
        INamedTypeSymbol colorType,
        INamedTypeSymbol elevationType) {
        var effectiveVariant = GetEffectiveVariant(frame, buttonVariantType);

        AnalyzeRequiredIcon(context, frame);
        AnalyzeAriaLabel(context, frame);
        AnalyzeBackgroundColor(context, frame, effectiveVariant, colorType);
        AnalyzeTextColor(context, frame, colorType);
        AnalyzeElevation(context, frame, effectiveVariant, elevationType);
    }

    private static void AnalyzeRequiredIcon(SyntaxNodeAnalysisContext context, ComponentFrame frame) {
        if (!frame.Attributes.TryGetValue("Icon", out var icon) || IsNullConstant(icon.Operation)) {
            context.ReportDiagnostic(Diagnostic.Create(MissingIconRule, GetAttributeOrComponentLocation(frame, "Icon")));
        }
    }

    private static void AnalyzeAriaLabel(SyntaxNodeAnalysisContext context, ComponentFrame frame) {
        if (TryGetKnownStringState(frame, "AriaLabel", defaultValue: string.Empty, out var state)
            && state == KnownStringState.Empty) {
            context.ReportDiagnostic(Diagnostic.Create(EmptyAriaLabelRule, GetAttributeOrComponentLocation(frame, "AriaLabel")));
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
            if (effectiveVariant == "Outlined" && IsSelectedToggle(frame)) {
                if (colorName is "None" or "Transparent") {
                    context.ReportDiagnostic(Diagnostic.Create(TransparentBackgroundRule, backgroundColor.Location, effectiveVariant));
                }

                return;
            }

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

    private static bool IsSelectedToggle(ComponentFrame frame) {
        return frame.Attributes.TryGetValue("IsToggleButton", out var isToggleButton)
            && TryGetBooleanConstant(isToggleButton.Operation, out var isToggle)
            && isToggle
            && frame.Attributes.TryGetValue("Selected", out var selected)
            && TryGetBooleanConstant(selected.Operation, out var isSelected)
            && isSelected;
    }

    private static void AnalyzeTextColor(SyntaxNodeAnalysisContext context, ComponentFrame frame, INamedTypeSymbol colorType) {
        if (frame.Attributes.TryGetValue("TextColor", out var textColor)
            && TryGetEnumMemberName(textColor.Operation, colorType, out var colorName)
            && colorName is "None" or "Transparent") {
            context.ReportDiagnostic(Diagnostic.Create(InvisibleTextColorRule, textColor.Location));
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

    private static string? GetEffectiveVariant(ComponentFrame frame, INamedTypeSymbol buttonVariantType) {
        if (!frame.Attributes.TryGetValue("Variant", out var variantValue)) {
            return "Text";
        }

        return TryGetEnumMemberName(variantValue.Operation, buttonVariantType, out var variantName)
            ? variantName
            : null;
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
        INamedTypeSymbol iconButtonType,
        out bool isIconButtonComponent) {
        isIconButtonComponent = false;

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

        isIconButtonComponent = SymbolEqualityComparer.Default.Equals(openedComponentType, iconButtonType);
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

    private static bool TryGetBooleanConstant(IOperation? operation, out bool value) {
        value = false;
        operation = UnwrapOperation(operation);

        if (operation?.ConstantValue.HasValue == true && operation.ConstantValue.Value is bool boolValue) {
            value = boolValue;
            return true;
        }

        return false;
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

    private enum KnownStringState {
        Unknown,
        Empty,
        NonEmpty
    }

    private sealed class ComponentFrame(bool isIconButton, Location location) {
        public Dictionary<string, RecordedAttribute> Attributes { get; } = new(StringComparer.Ordinal);

        public bool IsIconButton { get; } = isIconButton;

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
