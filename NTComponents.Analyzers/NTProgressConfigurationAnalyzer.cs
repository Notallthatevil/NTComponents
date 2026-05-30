using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace NTComponents.Analyzers;

/// <summary>
///     Warns when progress and loading indicators are configured in ways that rely on runtime fallback behavior.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NTProgressConfigurationAnalyzer : DiagnosticAnalyzer {

    public const string NonPositiveMaxDiagnosticId = "NTC1052";
    public const string OutOfRangeValueDiagnosticId = "NTC1053";
    public const string ShortLoaderAnimationDiagnosticId = "NTC1054";
    public const string SingleShapeLoaderDiagnosticId = "NTC1055";

    private static readonly DiagnosticDescriptor NonPositiveMaxRule = new(
        NonPositiveMaxDiagnosticId,
        "NTProgress Max must be positive",
        "NTProgress Max should be a positive value; non-positive values fall back to 100",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor OutOfRangeValueRule = new(
        OutOfRangeValueDiagnosticId,
        "NTProgress Value should be within range",
        "NTProgress Value should be between 0 and Max; out-of-range values are clamped at render time",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor ShortLoaderAnimationRule = new(
        ShortLoaderAnimationDiagnosticId,
        "NTLoader animation duration is too short",
        "NTLoader AnimationDuration should be at least 400ms; shorter durations are clamped",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor SingleShapeLoaderRule = new(
        SingleShapeLoaderDiagnosticId,
        "Animated NTLoader needs multiple shapes",
        "Animated NTLoader Shapes should contain at least two shapes; a single-shape sequence cannot visibly morph",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [
        NonPositiveMaxRule,
        OutOfRangeValueRule,
        ShortLoaderAnimationRule,
        SingleShapeLoaderRule
    ];

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context) {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(static startContext => {
            var progressType = startContext.Compilation.GetTypeByMetadataName("NTComponents.NTProgress");
            var loaderType = startContext.Compilation.GetTypeByMetadataName("NTComponents.NTLoader");

            if (progressType is null && loaderType is null) {
                return;
            }

            startContext.RegisterSyntaxNodeAction(
                nodeContext => AnalyzeExecutableNode(nodeContext, progressType, loaderType),
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
        INamedTypeSymbol? progressType,
        INamedTypeSymbol? loaderType) {
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
            if (TryGetOpenedComponent(invocation, context.SemanticModel, progressType, loaderType, out var componentKind)) {
                stack.Push(new ComponentFrame(componentKind, invocation.GetLocation()));
                continue;
            }

            if (IsCloseComponentInvocation(invocation, context.SemanticModel)) {
                if (stack.Count == 0) {
                    continue;
                }

                AnalyzeComponentFrame(context, stack.Pop());
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

    private static void AnalyzeComponentFrame(SyntaxNodeAnalysisContext context, ComponentFrame frame) {
        switch (frame.Kind) {
            case ComponentKind.Progress:
                AnalyzeProgressFrame(context, frame);
                break;

            case ComponentKind.Loader:
                AnalyzeLoaderFrame(context, frame);
                break;
        }
    }

    private static void AnalyzeProgressFrame(SyntaxNodeAnalysisContext context, ComponentFrame frame) {
        double? max = null;
        if (frame.Attributes.TryGetValue("Max", out var maxAttribute)
            && TryGetNumericConstant(maxAttribute.Operation, out var maxValue)) {
            if (maxValue <= 0) {
                context.ReportDiagnostic(Diagnostic.Create(NonPositiveMaxRule, maxAttribute.Location));
                return;
            }

            max = maxValue;
        }

        if (!frame.Attributes.TryGetValue("Value", out var valueAttribute)
            || !TryGetNumericConstant(valueAttribute.Operation, out var value)) {
            return;
        }

        var effectiveMax = max ?? 100.0;
        if (value < 0 || value > effectiveMax) {
            context.ReportDiagnostic(Diagnostic.Create(OutOfRangeValueRule, valueAttribute.Location));
        }
    }

    private static void AnalyzeLoaderFrame(SyntaxNodeAnalysisContext context, ComponentFrame frame) {
        if (frame.Attributes.TryGetValue("AnimationDuration", out var animationDuration)
            && TryGetTimeSpanMilliseconds(animationDuration.Operation, out var milliseconds)
            && milliseconds < 400) {
            context.ReportDiagnostic(Diagnostic.Create(ShortLoaderAnimationRule, animationDuration.Location));
        }

        var isAnimated = !frame.Attributes.TryGetValue("Animate", out var animate)
            || !TryGetBooleanConstant(animate.Operation, out var animateValue)
            || animateValue;

        if (!isAnimated
            || !frame.Attributes.TryGetValue("Shapes", out var shapes)
            || !TryGetArrayElementCount(shapes.Operation, out var count)
            || count != 1) {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(SingleShapeLoaderRule, shapes.Location));
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
        INamedTypeSymbol? progressType,
        INamedTypeSymbol? loaderType,
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

        if (progressType is not null && SymbolEqualityComparer.Default.Equals(openedComponentType, progressType)) {
            componentKind = ComponentKind.Progress;
            return true;
        }

        if (loaderType is not null && SymbolEqualityComparer.Default.Equals(openedComponentType, loaderType)) {
            componentKind = ComponentKind.Loader;
            return true;
        }

        return false;
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

    private static bool TryGetNumericConstant(IOperation? operation, out double value) {
        value = 0;
        operation = UnwrapOperation(operation);

        if (operation?.ConstantValue.HasValue != true || operation.ConstantValue.Value is null) {
            return false;
        }

        value = Convert.ToDouble(operation.ConstantValue.Value, System.Globalization.CultureInfo.InvariantCulture);
        return true;
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

    private static bool TryGetTimeSpanMilliseconds(IOperation? operation, out double milliseconds) {
        milliseconds = 0;
        operation = UnwrapOperation(operation);

        if (operation is IInvocationOperation invocation
            && invocation.TargetMethod.Name == "FromMilliseconds"
            && invocation.TargetMethod.ContainingType.ToDisplayString() == "System.TimeSpan"
            && invocation.Arguments.Length == 1
            && TryGetNumericConstant(invocation.Arguments[0].Value, out var argumentMilliseconds)) {
            milliseconds = argumentMilliseconds;
            return true;
        }

        if (operation is IObjectCreationOperation objectCreation
            && objectCreation.Type?.ToDisplayString() == "System.TimeSpan"
            && objectCreation.Arguments.Length == 3
            && TryGetNumericConstant(objectCreation.Arguments[0].Value, out var hours)
            && TryGetNumericConstant(objectCreation.Arguments[1].Value, out var minutes)
            && TryGetNumericConstant(objectCreation.Arguments[2].Value, out var seconds)) {
            milliseconds = TimeSpan.FromSeconds((hours * 3600) + (minutes * 60) + seconds).TotalMilliseconds;
            return true;
        }

        return false;
    }

    private static bool TryGetArrayElementCount(IOperation? operation, out int count) {
        count = 0;
        operation = UnwrapOperation(operation);

        if (operation is IArrayCreationOperation arrayCreation) {
            count = arrayCreation.Initializer?.ElementValues.Length ?? 0;
            return true;
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

    private static bool TryGetInvocationTarget(InvocationExpressionSyntax invocation, SemanticModel semanticModel, out IMethodSymbol methodSymbol) {
        var symbol = semanticModel.GetSymbolInfo(invocation).Symbol
            ?? semanticModel.GetSymbolInfo(invocation).CandidateSymbols.FirstOrDefault();

        methodSymbol = symbol as IMethodSymbol ?? null!;
        return methodSymbol is not null;
    }

    private enum ComponentKind {
        None,
        Progress,
        Loader
    }

    private readonly struct RecordedAttribute {
        public RecordedAttribute(Location location, IOperation? operation) {
            Location = location;
            Operation = operation;
        }

        public Location Location { get; }
        public IOperation? Operation { get; }
    }

    private sealed class ComponentFrame(ComponentKind kind, Location location) {
        public Dictionary<string, RecordedAttribute> Attributes { get; } = new(StringComparer.Ordinal);
        public ComponentKind Kind { get; } = kind;
        public Location Location { get; } = location;
    }
}
