using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace NTComponents.Analyzers;

/// <summary>
///     Warns when <c>NTDataGrid</c> is configured in a way that the component rejects at runtime.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NTDataGridConfigurationAnalyzer : DiagnosticAnalyzer {

    public const string DuplicateSourceDiagnosticId = "NTC1065";
    public const string MissingSourceDiagnosticId = "NTC1066";
    public const string VirtualizedPaginationDiagnosticId = "NTC1067";

    private static readonly DiagnosticDescriptor DuplicateSourceRule = new(
        DuplicateSourceDiagnosticId,
        "NTDataGrid cannot use both Items and ItemsProvider",
        "NTDataGrid requires either Items or ItemsProvider, not both",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor MissingSourceRule = new(
        MissingSourceDiagnosticId,
        "NTDataGrid requires a data source",
        "NTDataGrid requires Items or ItemsProvider",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor VirtualizedPaginationRule = new(
        VirtualizedPaginationDiagnosticId,
        "NTDataGrid cannot combine virtualization and pagination",
        "NTDataGrid does not support using Virtualize and ShowPagination together",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [
        DuplicateSourceRule,
        MissingSourceRule,
        VirtualizedPaginationRule
    ];

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context) {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(static startContext => {
            var ntDataGridType = startContext.Compilation.GetTypeByMetadataName("NTComponents.NTDataGrid`1");
            if (ntDataGridType is null) {
                return;
            }

            startContext.RegisterSyntaxNodeAction(
                nodeContext => AnalyzeExecutableNode(nodeContext, ntDataGridType),
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.MethodDeclaration,
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.ConstructorDeclaration,
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.LocalFunctionStatement,
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.ParenthesizedLambdaExpression,
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.SimpleLambdaExpression,
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.AnonymousMethodExpression);
        });
    }

    private static void AnalyzeExecutableNode(SyntaxNodeAnalysisContext context, INamedTypeSymbol ntDataGridType) {
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
            if (TryGetOpenedComponent(invocation, context.SemanticModel, ntDataGridType, out var isNtDataGridComponent)) {
                stack.Push(new ComponentFrame(isNtDataGridComponent, invocation.GetLocation()));
                continue;
            }

            if (IsCloseComponentInvocation(invocation, context.SemanticModel)) {
                if (stack.Count == 0) {
                    continue;
                }

                var frame = stack.Pop();
                if (frame.IsNtDataGrid) {
                    AnalyzeComponentFrame(context, frame);
                }

                continue;
            }

            if (stack.Count == 0 || !stack.Peek().IsNtDataGrid) {
                continue;
            }

            if (TryGetComponentAttribute(invocation, context.SemanticModel, out var name, out var attributeValue)) {
                stack.Peek().Attributes[name] = attributeValue;
            }
        }
    }

    private static void AnalyzeComponentFrame(SyntaxNodeAnalysisContext context, ComponentFrame frame) {
        var hasItems = HasSuppliedNonNullAttribute(frame, "Items");
        var hasItemsProvider = HasSuppliedNonNullAttribute(frame, "ItemsProvider");

        if (hasItems && hasItemsProvider) {
            context.ReportDiagnostic(Diagnostic.Create(DuplicateSourceRule, GetAttributeOrComponentLocation(frame, "ItemsProvider")));
        }
        else if (!hasItems && !hasItemsProvider) {
            context.ReportDiagnostic(Diagnostic.Create(MissingSourceRule, frame.Location));
        }

        if (TryGetBooleanConstant(frame, "Virtualize", out var virtualize)
            && virtualize
            && TryGetBooleanConstant(frame, "ShowPagination", out var showPagination)
            && showPagination) {
            context.ReportDiagnostic(Diagnostic.Create(VirtualizedPaginationRule, GetAttributeOrComponentLocation(frame, "ShowPagination")));
        }
    }

    private static bool HasSuppliedNonNullAttribute(ComponentFrame frame, string attributeName) =>
        frame.Attributes.TryGetValue(attributeName, out var attribute) && !IsNullConstant(attribute.Operation);

    private static bool TryGetBooleanConstant(ComponentFrame frame, string attributeName, out bool value) {
        value = false;
        if (!frame.Attributes.TryGetValue(attributeName, out var attribute)) {
            return false;
        }

        var operation = UnwrapOperation(attribute.Operation);
        if (operation?.ConstantValue.HasValue == true && operation.ConstantValue.Value is bool boolValue) {
            value = boolValue;
            return true;
        }

        return false;
    }

    private static Location GetAttributeOrComponentLocation(ComponentFrame frame, string attributeName) =>
        frame.Attributes.TryGetValue(attributeName, out var attribute)
            ? attribute.Location
            : frame.Location;

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

    private static bool TryGetOpenedComponent(InvocationExpressionSyntax invocation, SemanticModel semanticModel, INamedTypeSymbol ntDataGridType, out bool isNtDataGridComponent) {
        isNtDataGridComponent = false;

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

        if (openedComponentType is not INamedTypeSymbol openedNamedType) {
            return false;
        }

        isNtDataGridComponent = SymbolEqualityComparer.Default.Equals(openedNamedType.OriginalDefinition, ntDataGridType);
        return true;
    }

    private static bool IsCloseComponentInvocation(InvocationExpressionSyntax invocation, SemanticModel semanticModel) {
        return TryGetInvocationTarget(invocation, semanticModel, out var methodSymbol)
            && methodSymbol.Name == "CloseComponent"
            && methodSymbol.ContainingType.ToDisplayString() == "Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder";
    }

    private static bool TryGetComponentAttribute(InvocationExpressionSyntax invocation, SemanticModel semanticModel, out string name, out RecordedAttribute attribute) {
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

    private sealed class ComponentFrame(bool isNtDataGrid, Location location) {
        public Dictionary<string, RecordedAttribute> Attributes { get; } = new(StringComparer.Ordinal);

        public bool IsNtDataGrid { get; } = isNtDataGrid;

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
