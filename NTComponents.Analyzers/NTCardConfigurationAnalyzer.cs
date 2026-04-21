using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace NTComponents.Analyzers;

/// <summary>
///     Warns when <c>NTCard</c> is configured in a way that the component normalizes or ignores.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NTCardConfigurationAnalyzer : DiagnosticAnalyzer {

    public const string TransparentBackgroundDiagnosticId = "NTC1001";
    public const string IgnoredElevationDiagnosticId = "NTC1002";

    private static readonly DiagnosticDescriptor TransparentBackgroundRule = new(
        TransparentBackgroundDiagnosticId,
        "Transparent background is invalid for this NTCard variant",
        "NTCard variant '{0}' cannot use a transparent BackgroundColor. Use Outlined or remove the override.",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor IgnoredElevationRule = new(
        IgnoredElevationDiagnosticId,
        "NTCard elevation is ignored for this variant",
        "NTCard Elevation only applies when Variant is Elevated. Current variant is '{0}'.",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [TransparentBackgroundRule, IgnoredElevationRule];

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context) {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(static startContext => {
            var ntCardType = startContext.Compilation.GetTypeByMetadataName("NTComponents.NTCard");
            var cardVariantType = startContext.Compilation.GetTypeByMetadataName("NTComponents.NTCardVariant");
            var colorType = startContext.Compilation.GetTypeByMetadataName("NTComponents.TnTColor");

            if (ntCardType is null || cardVariantType is null || colorType is null) {
                return;
            }

            startContext.RegisterSyntaxNodeAction(
                nodeContext => AnalyzeExecutableNode(nodeContext, ntCardType, cardVariantType, colorType),
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
        INamedTypeSymbol ntCardType,
        INamedTypeSymbol cardVariantType,
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
            if (TryGetOpenedComponent(invocation, context.SemanticModel, ntCardType, out var isNtCardComponent)) {
                stack.Push(new ComponentFrame(isNtCardComponent));
                continue;
            }

            if (IsCloseComponentInvocation(invocation, context.SemanticModel)) {
                if (stack.Count == 0) {
                    continue;
                }

                var frame = stack.Pop();
                if (frame.IsNtCard) {
                    AnalyzeComponentFrame(context, frame, cardVariantType, colorType);
                }

                continue;
            }

            if (stack.Count == 0 || !stack.Peek().IsNtCard) {
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
        INamedTypeSymbol cardVariantType,
        INamedTypeSymbol colorType) {
        var effectiveVariant = GetEffectiveVariant(frame, cardVariantType);

        if (frame.Attributes.TryGetValue("BackgroundColor", out var backgroundColor)
            && TryGetEnumMemberName(backgroundColor.Operation, colorType, out var colorName)
            && colorName == "Transparent"
            && effectiveVariant is not null
            && effectiveVariant != "Outlined") {
            context.ReportDiagnostic(Diagnostic.Create(
                TransparentBackgroundRule,
                backgroundColor.Location,
                effectiveVariant));
        }

        if (frame.Attributes.ContainsKey("Elevation")
            && effectiveVariant is not null
            && effectiveVariant != "Elevated"
            && frame.Attributes["Elevation"].Location is { } elevationLocation) {
            context.ReportDiagnostic(Diagnostic.Create(
                IgnoredElevationRule,
                elevationLocation,
                effectiveVariant));
        }
    }

    private static string? GetEffectiveVariant(ComponentFrame frame, INamedTypeSymbol cardVariantType) {
        if (!frame.Attributes.TryGetValue("Variant", out var variantValue)) {
            return "Filled (default)";
        }

        return TryGetEnumMemberName(variantValue.Operation, cardVariantType, out var variantName)
            ? variantName
            : null;
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
        INamedTypeSymbol ntCardType,
        out bool isNtCardComponent) {
        isNtCardComponent = false;

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

        isNtCardComponent = SymbolEqualityComparer.Default.Equals(openedComponentType, ntCardType);
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

    private sealed class ComponentFrame(bool isNtCard) {
        public Dictionary<string, RecordedAttribute> Attributes { get; } = new(StringComparer.Ordinal);

        public bool IsNtCard { get; } = isNtCard;
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
