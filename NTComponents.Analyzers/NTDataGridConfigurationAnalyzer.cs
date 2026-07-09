using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace NTComponents.Analyzers;

/// <summary>
///     Warns when <c>NTDataGrid</c> or its columns are configured in a way that can fail at runtime.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NTDataGridConfigurationAnalyzer : DiagnosticAnalyzer {

    public const string DuplicateSourceDiagnosticId = "NTC1065";
    public const string MissingSourceDiagnosticId = "NTC1066";
    public const string VirtualizedPaginationDiagnosticId = "NTC1067";
    public const string ComputedPropertySortDiagnosticId = "NTC1068";

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

    private static readonly DiagnosticDescriptor ComputedPropertySortRule = new(
        ComputedPropertySortDiagnosticId,
        "Computed aggregate properties cannot be translated reliably",
        "NTPropertyColumn Property '{0}' combines other instance members and may fail when sorting a database-backed IQueryable; use NTTemplateColumn with SortBy targeting mapped properties",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [
        DuplicateSourceRule,
        MissingSourceRule,
        VirtualizedPaginationRule,
        ComputedPropertySortRule
    ];

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context) {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(static startContext => {
            var ntDataGridType = startContext.Compilation.GetTypeByMetadataName("NTComponents.NTDataGrid`1");
            var ntPropertyColumnType = startContext.Compilation.GetTypeByMetadataName("NTComponents.NTPropertyColumn`2");
            if (ntDataGridType is null && ntPropertyColumnType is null) {
                return;
            }

            startContext.RegisterSyntaxNodeAction(
                nodeContext => AnalyzeExecutableNode(nodeContext, ntDataGridType, ntPropertyColumnType),
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.MethodDeclaration,
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.ConstructorDeclaration,
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.LocalFunctionStatement,
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.ParenthesizedLambdaExpression,
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.SimpleLambdaExpression,
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.AnonymousMethodExpression);
        });
    }

    private static void AnalyzeExecutableNode(SyntaxNodeAnalysisContext context, INamedTypeSymbol? ntDataGridType, INamedTypeSymbol? ntPropertyColumnType) {
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
            if (TryGetOpenedComponent(invocation, context.SemanticModel, ntDataGridType, ntPropertyColumnType, out var componentKind)) {
                stack.Push(new ComponentFrame(componentKind, invocation.GetLocation()));
                continue;
            }

            if (IsCloseComponentInvocation(invocation, context.SemanticModel)) {
                if (stack.Count == 0) {
                    continue;
                }

                var frame = stack.Pop();
                if (frame.Kind == ComponentKind.DataGrid) {
                    AnalyzeDataGridFrame(context, frame);
                }
                else if (frame.Kind == ComponentKind.PropertyColumn) {
                    AnalyzePropertyColumnFrame(context, frame);
                }

                continue;
            }

            if (stack.Count == 0 || stack.Peek().Kind == ComponentKind.Other) {
                continue;
            }

            if (TryGetComponentAttribute(invocation, context.SemanticModel, out var name, out var attributeValue)) {
                stack.Peek().Attributes[name] = attributeValue;
            }
        }
    }

    private static void AnalyzeDataGridFrame(SyntaxNodeAnalysisContext context, ComponentFrame frame) {
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

    private static void AnalyzePropertyColumnFrame(SyntaxNodeAnalysisContext context, ComponentFrame frame) {
        if (!frame.Attributes.TryGetValue("Property", out var attribute)
            || TryGetSelectedProperty(attribute.Operation) is not { } property
            || !IsComputedAggregateProperty(property, context.CancellationToken)) {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(ComputedPropertySortRule, attribute.Location, property.Name));
    }

    private static IPropertySymbol? TryGetSelectedProperty(IOperation? operation) {
        operation = UnwrapOperation(operation);
        if (operation is IDelegateCreationOperation delegateCreation) {
            operation = UnwrapOperation(delegateCreation.Target);
        }

        if (operation is not IAnonymousFunctionOperation anonymousFunction) {
            return null;
        }

        var returnedValues = anonymousFunction.Body
            .DescendantsAndSelf()
            .OfType<IReturnOperation>()
            .Where(static returnOperation => returnOperation.ReturnedValue is not null)
            .Select(static returnOperation => returnOperation.ReturnedValue!)
            .Take(2)
            .ToArray();

        return returnedValues.Length == 1
            && UnwrapOperation(returnedValues[0]) is IPropertyReferenceOperation propertyReference
                ? propertyReference.Property
                : null;
    }

    private static bool IsComputedAggregateProperty(IPropertySymbol property, CancellationToken cancellationToken) {
        foreach (var syntaxReference in property.DeclaringSyntaxReferences) {
            if (syntaxReference.GetSyntax(cancellationToken) is not PropertyDeclarationSyntax propertyDeclaration) {
                continue;
            }

            SyntaxNode? getterImplementation = propertyDeclaration.ExpressionBody?.Expression;
            if (getterImplementation is null) {
                var getter = propertyDeclaration.AccessorList?.Accessors.FirstOrDefault(static accessor => accessor.IsKind(SyntaxKind.GetAccessorDeclaration));
                getterImplementation = getter?.ExpressionBody?.Expression ?? (SyntaxNode?)getter?.Body;
            }

            if (getterImplementation is null) {
                continue;
            }

            var dependencies = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
            var shadowedNames = new HashSet<string>(
                getterImplementation
                    .DescendantNodes(static node => !IsNestedExecutableBoundary(node))
                    .OfType<VariableDeclaratorSyntax>()
                    .Select(static variable => variable.Identifier.ValueText),
                StringComparer.Ordinal);

            foreach (var identifier in getterImplementation.DescendantNodesAndSelf(static node => !IsNestedExecutableBoundary(node)).OfType<IdentifierNameSyntax>()) {
                var name = identifier.Identifier.ValueText;
                if (shadowedNames.Contains(name) || !IsDirectMemberIdentifier(identifier)) {
                    continue;
                }

                for (var containingType = property.ContainingType; containingType is not null; containingType = containingType.BaseType) {
                    foreach (var member in containingType.GetMembers(name)) {
                        if (member is not (IPropertySymbol or IFieldSymbol)
                            || member.IsStatic
                            || SymbolEqualityComparer.Default.Equals(member, property)
                            || !dependencies.Add(member)) {
                            continue;
                        }

                        if (dependencies.Count >= 2) {
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }

    private static bool IsDirectMemberIdentifier(IdentifierNameSyntax identifier) =>
        identifier.Parent is not MemberAccessExpressionSyntax memberAccess
        || memberAccess.Name != identifier
        || memberAccess.Expression is ThisExpressionSyntax or BaseExpressionSyntax;

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

    private static bool TryGetOpenedComponent(InvocationExpressionSyntax invocation, SemanticModel semanticModel, INamedTypeSymbol? ntDataGridType, INamedTypeSymbol? ntPropertyColumnType, out ComponentKind componentKind) {
        componentKind = ComponentKind.Other;

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

        if (SymbolEqualityComparer.Default.Equals(openedNamedType.OriginalDefinition, ntDataGridType)) {
            componentKind = ComponentKind.DataGrid;
        }
        else if (SymbolEqualityComparer.Default.Equals(openedNamedType.OriginalDefinition, ntPropertyColumnType)) {
            componentKind = ComponentKind.PropertyColumn;
        }

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

    private sealed class ComponentFrame(ComponentKind kind, Location location) {
        public Dictionary<string, RecordedAttribute> Attributes { get; } = new(StringComparer.Ordinal);

        public ComponentKind Kind { get; } = kind;

        public Location Location { get; } = location;
    }

    private enum ComponentKind {
        Other,
        DataGrid,
        PropertyColumn
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
