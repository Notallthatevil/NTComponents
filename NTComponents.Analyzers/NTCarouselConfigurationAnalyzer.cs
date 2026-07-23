using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace NTComponents.Analyzers;

/// <summary>
///     Warns when <c>NTCarousel</c> or <c>NTCarouselItem</c> is configured with values rejected or ignored at runtime.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NTCarouselConfigurationAnalyzer : DiagnosticAnalyzer {

    public const string EmptyCarouselAriaLabelDiagnosticId = "NTC1069";
    public const string NonPositiveCarouselValueDiagnosticId = "NTC1070";
    public const string FullScreenSnappingDiagnosticId = "NTC1071";
    public const string EmptyItemAriaLabelDiagnosticId = "NTC1072";
    public const string InvalidItemAspectRatioDiagnosticId = "NTC1073";
    public const string UndefinedAppearanceDiagnosticId = "NTC1074";

    private static readonly DiagnosticDescriptor EmptyCarouselAriaLabelRule = new(
        EmptyCarouselAriaLabelDiagnosticId,
        "NTCarousel requires an accessible name",
        "NTCarousel AriaLabel cannot be empty because it provides the carousel accessible name",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor NonPositiveCarouselValueRule = new(
        NonPositiveCarouselValueDiagnosticId,
        "NTCarousel numeric value must be positive",
        "NTCarousel {0} must be a finite value greater than zero",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor FullScreenSnappingRule = new(
        FullScreenSnappingDiagnosticId,
        "Full-screen NTCarousel requires snapping",
        "NTCarousel appearance 'FullScreen' requires EnableSnapping to remain enabled",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor EmptyItemAriaLabelRule = new(
        EmptyItemAriaLabelDiagnosticId,
        "NTCarouselItem requires an accessible name",
        "NTCarouselItem AriaLabel cannot be empty because it provides the slide accessible name",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor InvalidItemAspectRatioRule = new(
        InvalidItemAspectRatioDiagnosticId,
        "NTCarouselItem aspect ratio is outside the Material range",
        "NTCarouselItem AspectRatio must be finite and between 9:16 and 16:9",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor UndefinedAppearanceRule = new(
        UndefinedAppearanceDiagnosticId,
        "NTCarousel appearance must be defined",
        "NTCarousel Appearance must be a defined CarouselAppearance value",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [
        EmptyCarouselAriaLabelRule,
        NonPositiveCarouselValueRule,
        FullScreenSnappingRule,
        EmptyItemAriaLabelRule,
        InvalidItemAspectRatioRule,
        UndefinedAppearanceRule
    ];

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context) {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(static startContext => {
            var carouselType = startContext.Compilation.GetTypeByMetadataName("NTComponents.NTCarousel");
            var carouselItemType = startContext.Compilation.GetTypeByMetadataName("NTComponents.NTCarouselItem");
            var appearanceType = startContext.Compilation.GetTypeByMetadataName("NTComponents.CarouselAppearance");

            if (carouselType is null && carouselItemType is null) {
                return;
            }

            startContext.RegisterSyntaxNodeAction(
                nodeContext => AnalyzeExecutableNode(nodeContext, carouselType, carouselItemType, appearanceType),
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.MethodDeclaration,
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.ConstructorDeclaration,
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.LocalFunctionStatement,
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.ParenthesizedLambdaExpression,
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.SimpleLambdaExpression,
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.AnonymousMethodExpression);
        });
    }

    private static void AnalyzeExecutableNode(SyntaxNodeAnalysisContext context, INamedTypeSymbol? carouselType, INamedTypeSymbol? carouselItemType, INamedTypeSymbol? appearanceType) {
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
            if (TryGetOpenedComponent(invocation, context.SemanticModel, carouselType, carouselItemType, out var componentKind)) {
                stack.Push(new ComponentFrame(componentKind, invocation.GetLocation()));
                continue;
            }

            if (IsCloseComponentInvocation(invocation, context.SemanticModel)) {
                if (stack.Count > 0) {
                    AnalyzeComponentFrame(context, stack.Pop(), appearanceType);
                }
                continue;
            }

            if (stack.Count > 0 && TryGetComponentAttribute(invocation, context.SemanticModel, out var name, out var attribute)) {
                stack.Peek().Attributes[name] = attribute;
            }
        }
    }

    private static void AnalyzeComponentFrame(SyntaxNodeAnalysisContext context, ComponentFrame frame, INamedTypeSymbol? appearanceType) {
        switch (frame.Kind) {
            case ComponentKind.Carousel:
                AnalyzeRequiredLabel(context, frame, EmptyCarouselAriaLabelRule);
                AnalyzePositiveValue(context, frame, "AutoPlayInterval");
                AnalyzePositiveValue(context, frame, "ItemHeight");
                AnalyzePositiveValue(context, frame, "MaxLargeItemWidth");
                AnalyzePositiveValue(context, frame, "PreferredItemWidth");
                AnalyzeAppearance(context, frame, appearanceType);
                break;

            case ComponentKind.Item:
                AnalyzeRequiredLabel(context, frame, EmptyItemAriaLabelRule);
                AnalyzeAspectRatio(context, frame);
                break;
        }
    }

    private static void AnalyzeRequiredLabel(SyntaxNodeAnalysisContext context, ComponentFrame frame, DiagnosticDescriptor rule) {
        if (!frame.Attributes.TryGetValue("AriaLabel", out var label)) {
            context.ReportDiagnostic(Diagnostic.Create(rule, frame.Location));
            return;
        }

        if (IsNullConstant(label.Operation)
            || (TryGetStringConstant(label.Operation, out var value) && string.IsNullOrWhiteSpace(value))) {
            context.ReportDiagnostic(Diagnostic.Create(rule, label.Location));
        }
    }

    private static void AnalyzePositiveValue(SyntaxNodeAnalysisContext context, ComponentFrame frame, string attributeName) {
        if (!frame.Attributes.TryGetValue(attributeName, out var attribute)
            || !TryGetNumericConstant(attribute.Operation, out var value)
            || value > 0 && !double.IsNaN(value) && !double.IsInfinity(value)) {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(NonPositiveCarouselValueRule, attribute.Location, attributeName));
    }

    private static void AnalyzeAppearance(SyntaxNodeAnalysisContext context, ComponentFrame frame, INamedTypeSymbol? appearanceType) {
        if (appearanceType is null
            || !frame.Attributes.TryGetValue("Appearance", out var appearance)
            || !TryGetEnumMemberName(appearance.Operation, appearanceType, out var appearanceName, out var isDefined)) {
            return;
        }

        if (!isDefined) {
            context.ReportDiagnostic(Diagnostic.Create(UndefinedAppearanceRule, appearance.Location));
            return;
        }

        if (appearanceName == "FullScreen"
            && frame.Attributes.TryGetValue("EnableSnapping", out var snapping)
            && TryGetBooleanConstant(snapping.Operation, out var isEnabled)
            && !isEnabled) {
            context.ReportDiagnostic(Diagnostic.Create(FullScreenSnappingRule, snapping.Location));
        }
    }

    private static void AnalyzeAspectRatio(SyntaxNodeAnalysisContext context, ComponentFrame frame) {
        if (!frame.Attributes.TryGetValue("AspectRatio", out var aspectRatio)
            || !TryGetNumericConstant(aspectRatio.Operation, out var value)
            || value >= 9d / 16d && value <= 16d / 9d && !double.IsNaN(value) && !double.IsInfinity(value)) {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(InvalidItemAspectRatioRule, aspectRatio.Location));
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

    private static bool TryGetOpenedComponent(InvocationExpressionSyntax invocation, SemanticModel semanticModel, INamedTypeSymbol? carouselType, INamedTypeSymbol? carouselItemType, out ComponentKind componentKind) {
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
        else if (invocation.ArgumentList.Arguments.Count >= 2
            && semanticModel.GetOperation(invocation.ArgumentList.Arguments[1].Expression) is ITypeOfOperation typeOfOperation) {
            openedComponentType = typeOfOperation.TypeOperand;
        }

        if (openedComponentType is null) {
            return false;
        }

        if (carouselType is not null && SymbolEqualityComparer.Default.Equals(openedComponentType, carouselType)) {
            componentKind = ComponentKind.Carousel;
            return true;
        }

        if (carouselItemType is not null && SymbolEqualityComparer.Default.Equals(openedComponentType, carouselItemType)) {
            componentKind = ComponentKind.Item;
            return true;
        }

        return false;
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
            || methodSymbol.Name is not ("AddAttribute" or "AddComponentParameter")
            || invocation.ArgumentList.Arguments.Count < 3) {
            return false;
        }

        var nameExpression = invocation.ArgumentList.Arguments[1].Expression;
        var nameConstant = semanticModel.GetConstantValue(nameExpression);
        if (!nameConstant.HasValue || nameConstant.Value is not string attributeName) {
            return false;
        }

        var valueExpression = invocation.ArgumentList.Arguments[2].Expression;
        name = attributeName;
        attribute = new RecordedAttribute(valueExpression.GetLocation(), semanticModel.GetOperation(valueExpression));
        return true;
    }

    private static bool TryGetEnumMemberName(IOperation? operation, INamedTypeSymbol enumType, out string? memberName, out bool isDefined) {
        memberName = null;
        isDefined = false;
        if (!TryGetIntegralConstant(operation, out var value)) {
            return false;
        }

        foreach (var field in enumType.GetMembers().OfType<IFieldSymbol>()) {
            if (!field.HasConstantValue || !TryConvertToInt64(field.ConstantValue, out var fieldValue) || fieldValue != value) {
                continue;
            }

            memberName = field.Name;
            isDefined = true;
            break;
        }

        return true;
    }

    private static bool TryGetIntegralConstant(IOperation? operation, out long value) {
        value = 0;
        operation = UnwrapOperation(operation);
        return operation?.ConstantValue.HasValue == true && TryConvertToInt64(operation.ConstantValue.Value, out value);
    }

    private static bool TryConvertToInt64(object? value, out long result) {
        switch (value) {
            case byte byteValue:
                result = byteValue;
                return true;
            case sbyte sbyteValue:
                result = sbyteValue;
                return true;
            case short shortValue:
                result = shortValue;
                return true;
            case ushort ushortValue:
                result = ushortValue;
                return true;
            case int intValue:
                result = intValue;
                return true;
            case uint uintValue:
                result = uintValue;
                return true;
            case long longValue:
                result = longValue;
                return true;
            default:
                result = 0;
                return false;
        }
    }

    private static bool TryGetNumericConstant(IOperation? operation, out double value) {
        value = 0;
        operation = UnwrapOperation(operation);
        if (operation?.ConstantValue.HasValue != true) {
            return false;
        }

        switch (operation.ConstantValue.Value) {
            case byte byteValue:
                value = byteValue;
                return true;
            case sbyte sbyteValue:
                value = sbyteValue;
                return true;
            case short shortValue:
                value = shortValue;
                return true;
            case ushort ushortValue:
                value = ushortValue;
                return true;
            case int intValue:
                value = intValue;
                return true;
            case uint uintValue:
                value = uintValue;
                return true;
            case long longValue:
                value = longValue;
                return true;
            case ulong ulongValue:
                value = ulongValue;
                return true;
            case float floatValue:
                value = floatValue;
                return true;
            case double doubleValue:
                value = doubleValue;
                return true;
            case decimal decimalValue:
                value = (double)decimalValue;
                return true;
            default:
                return false;
        }
    }

    private static bool TryGetBooleanConstant(IOperation? operation, out bool value) {
        operation = UnwrapOperation(operation);
        if (operation?.ConstantValue.HasValue == true && operation.ConstantValue.Value is bool boolValue) {
            value = boolValue;
            return true;
        }

        value = false;
        return false;
    }

    private static bool TryGetStringConstant(IOperation? operation, out string value) {
        operation = UnwrapOperation(operation);
        if (operation?.ConstantValue.HasValue == true && operation.ConstantValue.Value is string stringValue) {
            value = stringValue;
            return true;
        }

        value = string.Empty;
        return false;
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

    private enum ComponentKind {
        None,
        Carousel,
        Item
    }

    private sealed class ComponentFrame(ComponentKind kind, Location location) {
        public Dictionary<string, RecordedAttribute> Attributes { get; } = new(StringComparer.OrdinalIgnoreCase);

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
