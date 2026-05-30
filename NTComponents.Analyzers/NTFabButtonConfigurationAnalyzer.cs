using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace NTComponents.Analyzers;

/// <summary>
///     Warns when <c>NTFabButton</c> is configured in a way that the component rejects or remaps at runtime.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NTFabButtonConfigurationAnalyzer : DiagnosticAnalyzer {

    public const string MissingIconDiagnosticId = "NTC1032";
    public const string MissingIconOnlyAriaLabelDiagnosticId = "NTC1033";
    public const string InvalidLabelDiagnosticId = "NTC1034";
    public const string InvisibleBackgroundDiagnosticId = "NTC1035";
    public const string InvisibleTextColorDiagnosticId = "NTC1036";
    public const string UnsupportedSizeDiagnosticId = "NTC1037";
    public const string InvalidPlacementDiagnosticId = "NTC1038";

    private static readonly DiagnosticDescriptor MissingIconRule = new(
        MissingIconDiagnosticId,
        "NTFabButton icon is required",
        "NTFabButton requires a non-null Icon parameter",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor MissingIconOnlyAriaLabelRule = new(
        MissingIconOnlyAriaLabelDiagnosticId,
        "Icon-only NTFabButton requires an aria label",
        "Icon-only NTFabButton requires a non-empty AriaLabel",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor InvalidLabelRule = new(
        InvalidLabelDiagnosticId,
        "NTFabButton label cannot contain line breaks",
        "NTFabButton Label must not contain line breaks",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor InvisibleBackgroundRule = new(
        InvisibleBackgroundDiagnosticId,
        "NTFabButton background must be visible",
        "NTFabButton BackgroundColor must be a visible container color",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor InvisibleTextColorRule = new(
        InvisibleTextColorDiagnosticId,
        "NTFabButton text color must be visible",
        "NTFabButton TextColor must be a visible content color",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor UnsupportedSizeRule = new(
        UnsupportedSizeDiagnosticId,
        "NTFabButton size is unsupported",
        "NTFabButton does not support ButtonSize '{0}' and will render with '{1}'",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor InvalidPlacementRule = new(
        InvalidPlacementDiagnosticId,
        "NTFabButton placement is invalid",
        "NTFabButton Placement must be Inline, LowerRight, LowerLeft, UpperRight, or UpperLeft",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [
        MissingIconRule,
        MissingIconOnlyAriaLabelRule,
        InvalidLabelRule,
        InvisibleBackgroundRule,
        InvisibleTextColorRule,
        UnsupportedSizeRule,
        InvalidPlacementRule
    ];

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context) {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(static startContext => {
            var fabButtonType = startContext.Compilation.GetTypeByMetadataName("NTComponents.NTFabButton");
            var colorType = startContext.Compilation.GetTypeByMetadataName("NTComponents.TnTColor");
            var sizeType = startContext.Compilation.GetTypeByMetadataName("NTComponents.Size");
            var placementType = startContext.Compilation.GetTypeByMetadataName("NTComponents.NTFabButtonPlacement");

            if (fabButtonType is null || colorType is null || sizeType is null || placementType is null) {
                return;
            }

            startContext.RegisterSyntaxNodeAction(
                nodeContext => AnalyzeExecutableNode(nodeContext, fabButtonType, colorType, sizeType, placementType),
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
        INamedTypeSymbol fabButtonType,
        INamedTypeSymbol colorType,
        INamedTypeSymbol sizeType,
        INamedTypeSymbol placementType) {
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
            if (TryGetOpenedComponent(invocation, context.SemanticModel, fabButtonType, out var isFabButtonComponent)) {
                stack.Push(new ComponentFrame(isFabButtonComponent, invocation.GetLocation()));
                continue;
            }

            if (IsCloseComponentInvocation(invocation, context.SemanticModel)) {
                if (stack.Count == 0) {
                    continue;
                }

                var frame = stack.Pop();
                if (frame.IsFabButton) {
                    AnalyzeComponentFrame(context, frame, colorType, sizeType, placementType);
                }

                continue;
            }

            if (stack.Count == 0 || !stack.Peek().IsFabButton) {
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
        INamedTypeSymbol colorType,
        INamedTypeSymbol sizeType,
        INamedTypeSymbol placementType) {
        AnalyzeRequiredIcon(context, frame);
        AnalyzeIconOnlyAriaLabel(context, frame);
        AnalyzeLabel(context, frame);
        AnalyzeColor(context, frame, colorType, "BackgroundColor", InvisibleBackgroundRule);
        AnalyzeColor(context, frame, colorType, "TextColor", InvisibleTextColorRule);
        AnalyzeSize(context, frame, sizeType);
        AnalyzePlacement(context, frame, placementType);
    }

    private static void AnalyzeRequiredIcon(SyntaxNodeAnalysisContext context, ComponentFrame frame) {
        if (!frame.Attributes.TryGetValue("Icon", out var icon) || IsNullConstant(icon.Operation)) {
            context.ReportDiagnostic(Diagnostic.Create(MissingIconRule, GetAttributeOrComponentLocation(frame, "Icon")));
        }
    }

    private static void AnalyzeIconOnlyAriaLabel(SyntaxNodeAnalysisContext context, ComponentFrame frame) {
        if (!IsKnownIconOnly(frame)) {
            return;
        }

        if (!TryGetKnownStringState(frame, "AriaLabel", defaultValue: string.Empty, out var state)
            || state != KnownStringState.Empty) {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(MissingIconOnlyAriaLabelRule, GetAttributeOrComponentLocation(frame, "AriaLabel")));
    }

    private static bool IsKnownIconOnly(ComponentFrame frame) {
        if (!frame.Attributes.TryGetValue("Label", out var label)) {
            return true;
        }

        if (IsNullConstant(label.Operation)) {
            return true;
        }

        return TryGetStringConstant(label.Operation, out var labelValue) && string.IsNullOrWhiteSpace(labelValue);
    }

    private static void AnalyzeLabel(SyntaxNodeAnalysisContext context, ComponentFrame frame) {
        if (!frame.Attributes.TryGetValue("Label", out var label)
            || !TryGetStringConstant(label.Operation, out var labelValue)
            || (!labelValue.Contains('\r') && !labelValue.Contains('\n'))) {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(InvalidLabelRule, label.Location));
    }

    private static void AnalyzeColor(
        SyntaxNodeAnalysisContext context,
        ComponentFrame frame,
        INamedTypeSymbol colorType,
        string attributeName,
        DiagnosticDescriptor rule) {
        if (frame.Attributes.TryGetValue(attributeName, out var color)
            && TryGetEnumMemberName(color.Operation, colorType, out var colorName)
            && colorName is "None" or "Transparent") {
            context.ReportDiagnostic(Diagnostic.Create(rule, color.Location));
        }
    }

    private static void AnalyzeSize(SyntaxNodeAnalysisContext context, ComponentFrame frame, INamedTypeSymbol sizeType) {
        if (!frame.Attributes.TryGetValue("ButtonSize", out var size)
            || !TryGetEnumMemberName(size.Operation, sizeType, out var sizeName)) {
            return;
        }

        if (sizeName is "Smallest" or "XS") {
            context.ReportDiagnostic(Diagnostic.Create(UnsupportedSizeRule, size.Location, sizeName, "Small"));
            return;
        }

        if (sizeName is "Largest" or "XL") {
            context.ReportDiagnostic(Diagnostic.Create(UnsupportedSizeRule, size.Location, sizeName, "Large"));
        }
    }

    private static void AnalyzePlacement(SyntaxNodeAnalysisContext context, ComponentFrame frame, INamedTypeSymbol placementType) {
        if (!frame.Attributes.TryGetValue("Placement", out var placement)
            || !IsInvalidEnumConstant(placement.Operation, placementType)) {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(InvalidPlacementRule, placement.Location));
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
        INamedTypeSymbol fabButtonType,
        out bool isFabButtonComponent) {
        isFabButtonComponent = false;

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

        isFabButtonComponent = SymbolEqualityComparer.Default.Equals(openedComponentType, fabButtonType);
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

    private static bool IsInvalidEnumConstant(IOperation? operation, INamedTypeSymbol enumType) {
        operation = UnwrapOperation(operation);
        if (operation is null || operation.ConstantValue.HasValue != true) {
            return false;
        }

        var constantValue = Convert.ToInt64(operation.ConstantValue.Value);
        foreach (var field in enumType.GetMembers().OfType<IFieldSymbol>()) {
            if (field.HasConstantValue && Convert.ToInt64(field.ConstantValue) == constantValue) {
                return false;
            }
        }

        return true;
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

    private sealed class ComponentFrame(bool isFabButton, Location location) {
        public Dictionary<string, RecordedAttribute> Attributes { get; } = new(StringComparer.Ordinal);

        public bool IsFabButton { get; } = isFabButton;

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
