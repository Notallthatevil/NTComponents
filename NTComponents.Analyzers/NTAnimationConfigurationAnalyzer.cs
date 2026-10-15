using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace NTComponents.Analyzers;

/// <summary>
///     Warns when NTAnimation is configured with parameters that do not apply to its trigger mode.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NTAnimationConfigurationAnalyzer : DiagnosticAnalyzer {

    public const string ShownRequiresManualDiagnosticId = "NTC1080";
    public const string VisibleOnlyParameterDiagnosticId = "NTC1081";
    public const string ThresholdOutOfRangeDiagnosticId = "NTC1082";

    private const string BuilderType = "Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder";
    private static readonly string[] VisibleOnlyParameters = ["Repeat", "Threshold", "RootMargin", "OnEntered", "OnExited"];

    private static readonly DiagnosticDescriptor ShownRequiresManualRule = new(
        ShownRequiresManualDiagnosticId,
        "NTAnimation Shown requires Manual trigger",
        "NTAnimation Shown only applies when Trigger is Manual",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor VisibleOnlyParameterRule = new(
        VisibleOnlyParameterDiagnosticId,
        "NTAnimation parameter requires Visible trigger",
        "NTAnimation {0} only applies when Trigger is Visible",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor ThresholdOutOfRangeRule = new(
        ThresholdOutOfRangeDiagnosticId,
        "NTAnimation Threshold must be between 0 and 1",
        "NTAnimation Threshold must be between 0 and 1; out-of-range values are clamped at runtime",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [ShownRequiresManualRule, VisibleOnlyParameterRule, ThresholdOutOfRangeRule];

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context) {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(static startContext => {
            var animationType = startContext.Compilation.GetTypeByMetadataName("NTComponents.NTAnimation");
            if (animationType is null) {
                return;
            }

            startContext.RegisterSyntaxNodeAction(
                nodeContext => AnalyzeExecutableNode(nodeContext, animationType),
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.MethodDeclaration,
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.ConstructorDeclaration,
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.LocalFunctionStatement,
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.ParenthesizedLambdaExpression,
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.SimpleLambdaExpression,
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.AnonymousMethodExpression);
        });
    }

    private static void AnalyzeExecutableNode(SyntaxNodeAnalysisContext context, INamedTypeSymbol animationType) {
        SyntaxNode? bodyNode = context.Node switch {
            BaseMethodDeclarationSyntax method => method.Body,
            LocalFunctionStatementSyntax localFunction => localFunction.Body,
            ParenthesizedLambdaExpressionSyntax lambda => lambda.Body as BlockSyntax,
            SimpleLambdaExpressionSyntax lambda => lambda.Body as BlockSyntax,
            AnonymousMethodExpressionSyntax anonymousMethod => anonymousMethod.Body,
            _ => null
        };
        if (bodyNode is null) {
            return;
        }

        var invocations = bodyNode
            .DescendantNodes(static node => node is not (BaseMethodDeclarationSyntax or LocalFunctionStatementSyntax or ParenthesizedLambdaExpressionSyntax or SimpleLambdaExpressionSyntax or AnonymousMethodExpressionSyntax))
            .OfType<InvocationExpressionSyntax>()
            .OrderBy(static invocation => invocation.SpanStart);

        // A null entry is an open component that is not NTAnimation.
        var stack = new Stack<Dictionary<string, (Location Location, IOperation? Operation)>?>();

        foreach (var invocation in invocations) {
            var symbol = context.SemanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
            if (symbol?.ContainingType.ToDisplayString() != BuilderType) {
                continue;
            }

            var arguments = invocation.ArgumentList.Arguments;
            switch (symbol.Name) {
                case "OpenComponent":
                    ITypeSymbol? opened = symbol.IsGenericMethod && symbol.TypeArguments.Length == 1
                        ? symbol.TypeArguments[0]
                        : arguments.Count >= 2 && context.SemanticModel.GetOperation(arguments[1].Expression) is ITypeOfOperation typeOf ? typeOf.TypeOperand : null;
                    stack.Push(SymbolEqualityComparer.Default.Equals(opened, animationType) ? new(StringComparer.Ordinal) : null);
                    break;

                case "CloseComponent":
                    if (stack.Count > 0 && stack.Pop() is { } attributes) {
                        AnalyzeAnimation(context, attributes);
                    }

                    break;

                case "AddAttribute" or "AddComponentParameter":
                    if (stack.Count > 0 && stack.Peek() is { } current
                        && arguments.Count >= 3
                        && context.SemanticModel.GetConstantValue(arguments[1].Expression).Value is string name) {
                        current[name] = (arguments[2].Expression.GetLocation(), context.SemanticModel.GetOperation(arguments[2].Expression));
                    }

                    break;
            }
        }
    }

    private static void AnalyzeAnimation(SyntaxNodeAnalysisContext context, Dictionary<string, (Location Location, IOperation? Operation)> attributes) {
        if (attributes.TryGetValue("Threshold", out var threshold)
            && Unwrap(threshold.Operation) is { ConstantValue: { HasValue: true, Value: { } constant } }
            && constant is not string
            && Convert.ToDouble(constant, System.Globalization.CultureInfo.InvariantCulture) is var value
            && !(value >= 0 && value <= 1)) {
            context.ReportDiagnostic(Diagnostic.Create(ThresholdOutOfRangeRule, threshold.Location));
        }

        // Trigger defaults to Visible when not set; a non-constant Trigger is unknown.
        var trigger = attributes.TryGetValue("Trigger", out var triggerAttribute) ? GetEnumMemberName(Unwrap(triggerAttribute.Operation)) : "Visible";
        if (trigger is null) {
            return;
        }

        if (trigger != "Manual" && attributes.TryGetValue("Shown", out var shown)) {
            context.ReportDiagnostic(Diagnostic.Create(ShownRequiresManualRule, shown.Location));
        }

        if (trigger == "Visible") {
            return;
        }

        foreach (var parameter in VisibleOnlyParameters) {
            if (attributes.TryGetValue(parameter, out var attribute)) {
                context.ReportDiagnostic(Diagnostic.Create(VisibleOnlyParameterRule, attribute.Location, parameter));
            }
        }
    }

    private static string? GetEnumMemberName(IOperation? operation) {
        if (operation?.ConstantValue is not { HasValue: true, Value: { } constant } || operation.Type?.TypeKind != TypeKind.Enum) {
            return null;
        }

        return operation.Type.GetMembers().OfType<IFieldSymbol>().FirstOrDefault(field => field.HasConstantValue && Equals(field.ConstantValue, constant))?.Name;
    }

    private static IOperation? Unwrap(IOperation? operation) {
        while (operation is not null) {
            switch (operation) {
                case IConversionOperation conversion:
                    operation = conversion.Operand;
                    continue;
                case IParenthesizedOperation parenthesized:
                    operation = parenthesized.Operand;
                    continue;
                case IInvocationOperation { TargetMethod: { Name: "TypeCheck", ContainingType: { } type } } invocation
                    when type.ToDisplayString() == "Microsoft.AspNetCore.Components.CompilerServices.RuntimeHelpers" && invocation.Arguments.Length == 1:
                    operation = invocation.Arguments[0].Value;
                    continue;
                default:
                    return operation;
            }
        }

        return null;
    }
}
