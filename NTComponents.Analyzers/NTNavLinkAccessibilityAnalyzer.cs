using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace NTComponents.Analyzers;

/// <summary>
///     Warns when <c>NTNavLink</c> is configured in a way that weakens browser accessibility semantics.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NTNavLinkAccessibilityAnalyzer : DiagnosticAnalyzer {

    public const string EmptyLabelDiagnosticId = "NTC1056";
    public const string ComponentOwnedAriaStateDiagnosticId = "NTC1057";
    public const string DisabledTabIndexDiagnosticId = "NTC1058";

    private static readonly DiagnosticDescriptor EmptyLabelRule = new(
        EmptyLabelDiagnosticId,
        "NTNavLink label cannot be empty",
        "NTNavLink requires a non-empty Label so the anchor has an accessible name",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor ComponentOwnedAriaStateRule = new(
        ComponentOwnedAriaStateDiagnosticId,
        "NTNavLink owns this ARIA state",
        "Do not set '{0}' on NTNavLink. Use NTNavLink parameters and route matching so the component renders this ARIA state correctly.",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor DisabledTabIndexRule = new(
        DisabledTabIndexDiagnosticId,
        "Disabled NTNavLink cannot be focusable",
        "Disabled NTNavLink renders tabindex='-1'. Remove the explicit tabindex or do not disable the link.",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [
        EmptyLabelRule,
        ComponentOwnedAriaStateRule,
        DisabledTabIndexRule
    ];

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context) {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(static startContext => {
            var navLinkType = startContext.Compilation.GetTypeByMetadataName("NTComponents.NTNavLink");
            if (navLinkType is null) {
                return;
            }

            startContext.RegisterSyntaxNodeAction(
                nodeContext => AnalyzeExecutableNode(nodeContext, navLinkType),
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.MethodDeclaration,
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.ConstructorDeclaration,
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.LocalFunctionStatement,
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.ParenthesizedLambdaExpression,
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.SimpleLambdaExpression,
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.AnonymousMethodExpression);
        });
    }

    private static void AnalyzeExecutableNode(SyntaxNodeAnalysisContext context, INamedTypeSymbol navLinkType) {
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
            if (TryGetOpenedComponent(invocation, context.SemanticModel, navLinkType, out var isNtNavLinkComponent)) {
                stack.Push(new ComponentFrame(isNtNavLinkComponent, invocation.GetLocation()));
                continue;
            }

            if (IsCloseComponentInvocation(invocation, context.SemanticModel)) {
                if (stack.Count == 0) {
                    continue;
                }

                var frame = stack.Pop();
                if (frame.IsNtNavLink) {
                    AnalyzeComponentFrame(context, frame);
                }

                continue;
            }

            if (stack.Count == 0 || !stack.Peek().IsNtNavLink) {
                continue;
            }

            if (TryGetComponentAttribute(invocation, context.SemanticModel, out var name, out var attributeValue)) {
                stack.Peek().Attributes[name] = attributeValue;
            }
        }
    }

    private static void AnalyzeComponentFrame(SyntaxNodeAnalysisContext context, ComponentFrame frame) {
        AnalyzeLabel(context, frame);
        AnalyzeComponentOwnedAriaState(context, frame, "aria-current");
        AnalyzeComponentOwnedAriaState(context, frame, "aria-disabled");
        AnalyzeDisabledTabIndex(context, frame);
    }

    private static void AnalyzeLabel(SyntaxNodeAnalysisContext context, ComponentFrame frame) {
        if (!frame.Attributes.TryGetValue("Label", out var label)) {
            context.ReportDiagnostic(Diagnostic.Create(EmptyLabelRule, frame.Location));
            return;
        }

        if (IsNullConstant(label.Operation)
            || (TryGetStringConstant(label.Operation, out var labelValue) && string.IsNullOrWhiteSpace(labelValue))) {
            context.ReportDiagnostic(Diagnostic.Create(EmptyLabelRule, label.Location));
        }
    }

    private static void AnalyzeComponentOwnedAriaState(SyntaxNodeAnalysisContext context, ComponentFrame frame, string attributeName) {
        if (frame.Attributes.TryGetValue(attributeName, out var attribute)) {
            context.ReportDiagnostic(Diagnostic.Create(ComponentOwnedAriaStateRule, attribute.Location, attributeName));
        }
    }

    private static void AnalyzeDisabledTabIndex(SyntaxNodeAnalysisContext context, ComponentFrame frame) {
        if (!frame.Attributes.TryGetValue("Disabled", out var disabled)
            || !TryGetBooleanConstant(disabled.Operation, out var isDisabled)
            || !isDisabled
            || !frame.Attributes.TryGetValue("tabindex", out var tabIndex)) {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(DisabledTabIndexRule, tabIndex.Location));
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
        INamedTypeSymbol navLinkType,
        out bool isNtNavLinkComponent) {
        isNtNavLinkComponent = false;

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

        isNtNavLinkComponent = SymbolEqualityComparer.Default.Equals(openedComponentType, navLinkType);
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

    private sealed class ComponentFrame(bool isNtNavLink, Location location) {
        public Dictionary<string, RecordedAttribute> Attributes { get; } = new(StringComparer.OrdinalIgnoreCase);

        public bool IsNtNavLink { get; } = isNtNavLink;

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
