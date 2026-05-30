using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace NTComponents.Analyzers;

/// <summary>
///     Warns when <c>NTFabMenu</c> is configured in a way that the component rejects or remaps at runtime.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NTFabMenuConfigurationAnalyzer : DiagnosticAnalyzer {

    public const string MissingIconDiagnosticId = "NTC1039";
    public const string MissingAriaLabelDiagnosticId = "NTC1040";
    public const string InvisibleColorDiagnosticId = "NTC1041";
    public const string UnsupportedSizeDiagnosticId = "NTC1042";
    public const string InvalidPlacementDiagnosticId = "NTC1043";
    public const string InvalidMenuItemCountDiagnosticId = "NTC1044";
    public const string EmptyMenuItemLabelDiagnosticId = "NTC1045";
    public const string EmptyMenuItemHrefDiagnosticId = "NTC1046";

    private static readonly DiagnosticDescriptor MissingIconRule = new(
        MissingIconDiagnosticId,
        "NTFabMenu icon is required",
        "NTFabMenu requires a non-null Icon parameter",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor MissingAriaLabelRule = new(
        MissingAriaLabelDiagnosticId,
        "NTFabMenu requires an aria label",
        "NTFabMenu requires a non-empty AriaLabel that describes the menu opened by the FAB",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor InvisibleColorRule = new(
        InvisibleColorDiagnosticId,
        "NTFabMenu colors must be visible",
        "NTFabMenu {0} must be a visible color",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor UnsupportedSizeRule = new(
        UnsupportedSizeDiagnosticId,
        "NTFabMenu size is unsupported",
        "NTFabMenu does not support ButtonSize '{0}' and will render with '{1}'",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor InvalidPlacementRule = new(
        InvalidPlacementDiagnosticId,
        "NTFabMenu placement is invalid",
        "NTFabMenu Placement must be Inline, LowerRight, LowerLeft, UpperRight, or UpperLeft",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor InvalidMenuItemCountRule = new(
        InvalidMenuItemCountDiagnosticId,
        "NTFabMenu requires 2 to 6 menu items",
        "NTFabMenu requires 2 to 6 NTFabMenuButtonItem or NTFabMenuAnchorItem children",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor EmptyMenuItemLabelRule = new(
        EmptyMenuItemLabelDiagnosticId,
        "NTFabMenu item label cannot be empty",
        "{0} requires a non-empty Label",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor EmptyMenuItemHrefRule = new(
        EmptyMenuItemHrefDiagnosticId,
        "NTFabMenu anchor item href cannot be empty",
        "NTFabMenuAnchorItem requires a non-empty Href",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [
        MissingIconRule,
        MissingAriaLabelRule,
        InvisibleColorRule,
        UnsupportedSizeRule,
        InvalidPlacementRule,
        InvalidMenuItemCountRule,
        EmptyMenuItemLabelRule,
        EmptyMenuItemHrefRule
    ];

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context) {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(static startContext => {
            var fabMenuType = startContext.Compilation.GetTypeByMetadataName("NTComponents.NTFabMenu");
            var buttonItemType = startContext.Compilation.GetTypeByMetadataName("NTComponents.NTFabMenuButtonItem");
            var anchorItemType = startContext.Compilation.GetTypeByMetadataName("NTComponents.NTFabMenuAnchorItem");
            var colorType = startContext.Compilation.GetTypeByMetadataName("NTComponents.TnTColor");
            var sizeType = startContext.Compilation.GetTypeByMetadataName("NTComponents.Size");
            var placementType = startContext.Compilation.GetTypeByMetadataName("NTComponents.NTFabButtonPlacement");

            if (fabMenuType is null || buttonItemType is null || anchorItemType is null || colorType is null || sizeType is null || placementType is null) {
                return;
            }

            var componentTypes = new ComponentTypes(fabMenuType, buttonItemType, anchorItemType);

            startContext.RegisterSyntaxNodeAction(
                nodeContext => AnalyzeExecutableNode(nodeContext, componentTypes, colorType, sizeType, placementType),
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
        ComponentTypes componentTypes,
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
            if (TryGetOpenedComponent(invocation, context.SemanticModel, componentTypes, out var componentKind)) {
                stack.Push(new ComponentFrame(componentKind, invocation.GetLocation()));
                continue;
            }

            if (IsCloseComponentInvocation(invocation, context.SemanticModel)) {
                if (stack.Count == 0) {
                    continue;
                }

                AnalyzeComponentFrame(context, stack.Pop(), componentTypes, colorType, sizeType, placementType);
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
        ComponentTypes componentTypes,
        INamedTypeSymbol colorType,
        INamedTypeSymbol sizeType,
        INamedTypeSymbol placementType) {
        switch (frame.Kind) {
            case ComponentKind.FabMenu:
                AnalyzeFabMenuFrame(context, frame, componentTypes, colorType, sizeType, placementType);
                break;

            case ComponentKind.ButtonItem:
                AnalyzeMenuItemLabel(context, frame, "NTFabMenuButtonItem");
                break;

            case ComponentKind.AnchorItem:
                AnalyzeMenuItemLabel(context, frame, "NTFabMenuAnchorItem");
                AnalyzeAnchorItemHref(context, frame);
                break;
        }
    }

    private static void AnalyzeFabMenuFrame(
        SyntaxNodeAnalysisContext context,
        ComponentFrame frame,
        ComponentTypes componentTypes,
        INamedTypeSymbol colorType,
        INamedTypeSymbol sizeType,
        INamedTypeSymbol placementType) {
        AnalyzeRequiredIcon(context, frame);
        AnalyzeAriaLabel(context, frame);
        AnalyzeColor(context, frame, colorType, "BackgroundColor");
        AnalyzeColor(context, frame, colorType, "TextColor");
        AnalyzeColor(context, frame, colorType, "SelectedFabBackgroundColor");
        AnalyzeColor(context, frame, colorType, "SelectedFabTextColor");
        AnalyzeColor(context, frame, colorType, "MenuItemBackgroundColor");
        AnalyzeColor(context, frame, colorType, "MenuItemTextColor");
        AnalyzeSize(context, frame, sizeType);
        AnalyzePlacement(context, frame, placementType);
        AnalyzeMenuItems(context, frame, componentTypes);
    }

    private static void AnalyzeRequiredIcon(SyntaxNodeAnalysisContext context, ComponentFrame frame) {
        if (!frame.Attributes.TryGetValue("Icon", out var icon) || IsNullConstant(icon.Operation)) {
            context.ReportDiagnostic(Diagnostic.Create(MissingIconRule, GetAttributeOrComponentLocation(frame, "Icon")));
        }
    }

    private static void AnalyzeAriaLabel(SyntaxNodeAnalysisContext context, ComponentFrame frame) {
        if (TryGetKnownStringState(frame, "AriaLabel", defaultValue: string.Empty, out var state)
            && state == KnownStringState.Empty) {
            context.ReportDiagnostic(Diagnostic.Create(MissingAriaLabelRule, GetAttributeOrComponentLocation(frame, "AriaLabel")));
        }
    }

    private static void AnalyzeColor(SyntaxNodeAnalysisContext context, ComponentFrame frame, INamedTypeSymbol colorType, string attributeName) {
        if (frame.Attributes.TryGetValue(attributeName, out var color)
            && TryGetEnumMemberName(color.Operation, colorType, out var colorName)
            && colorName is "None" or "Transparent") {
            context.ReportDiagnostic(Diagnostic.Create(InvisibleColorRule, color.Location, attributeName));
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
        if (frame.Attributes.TryGetValue("Placement", out var placement)
            && IsInvalidEnumConstant(placement.Operation, placementType)) {
            context.ReportDiagnostic(Diagnostic.Create(InvalidPlacementRule, placement.Location));
        }
    }

    private static void AnalyzeMenuItems(SyntaxNodeAnalysisContext context, ComponentFrame frame, ComponentTypes componentTypes) {
        if (!frame.Attributes.TryGetValue("ChildContent", out var childContent)) {
            context.ReportDiagnostic(Diagnostic.Create(InvalidMenuItemCountRule, frame.Location));
            return;
        }

        if (TryGetStaticMenuItemCount(childContent.Operation?.Syntax, context.SemanticModel, componentTypes, out var actionItemCount)
            && (actionItemCount < 2 || actionItemCount > 6)) {
            context.ReportDiagnostic(Diagnostic.Create(InvalidMenuItemCountRule, childContent.Location));
        }
    }

    private static void AnalyzeMenuItemLabel(SyntaxNodeAnalysisContext context, ComponentFrame frame, string componentName) {
        if (TryGetKnownStringState(frame, "Label", defaultValue: string.Empty, out var labelState)
            && labelState == KnownStringState.Empty) {
            context.ReportDiagnostic(Diagnostic.Create(
                EmptyMenuItemLabelRule,
                GetAttributeOrComponentLocation(frame, "Label"),
                componentName));
        }
    }

    private static void AnalyzeAnchorItemHref(SyntaxNodeAnalysisContext context, ComponentFrame frame) {
        if (TryGetKnownStringState(frame, "Href", defaultValue: string.Empty, out var hrefState)
            && hrefState == KnownStringState.Empty) {
            context.ReportDiagnostic(Diagnostic.Create(EmptyMenuItemHrefRule, GetAttributeOrComponentLocation(frame, "Href")));
        }
    }

    private static bool TryGetStaticMenuItemCount(
        SyntaxNode? syntax,
        SemanticModel semanticModel,
        ComponentTypes componentTypes,
        out int actionItemCount) {
        actionItemCount = 0;

        if (syntax is null) {
            return false;
        }

        var lambda = syntax.DescendantNodesAndSelf().OfType<AnonymousFunctionExpressionSyntax>().FirstOrDefault();
        if (lambda is null || GetBodyNode(lambda) is not { } bodyNode) {
            return false;
        }

        var foundKnownMenuContent = false;
        var invocations = bodyNode
            .DescendantNodes(static node => !IsNestedExecutableBoundary(node))
            .OfType<InvocationExpressionSyntax>();

        foreach (var invocation in invocations) {
            if (!TryGetOpenedComponent(invocation, semanticModel, componentTypes, out var componentKind)) {
                continue;
            }

            if (componentKind is ComponentKind.ButtonItem or ComponentKind.AnchorItem) {
                foundKnownMenuContent = true;
                actionItemCount++;
            }
        }

        return foundKnownMenuContent;
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
        ComponentTypes componentTypes,
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

        componentKind = componentTypes.GetComponentKind(openedComponentType);
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

    private static bool TryGetInvocationTarget(InvocationExpressionSyntax invocation, SemanticModel semanticModel, out IMethodSymbol methodSymbol) {
        var symbol = semanticModel.GetSymbolInfo(invocation).Symbol
            ?? semanticModel.GetSymbolInfo(invocation).CandidateSymbols.FirstOrDefault();

        methodSymbol = symbol as IMethodSymbol ?? null!;
        return methodSymbol is not null;
    }

    private enum ComponentKind {
        None,
        FabMenu,
        ButtonItem,
        AnchorItem
    }

    private enum KnownStringState {
        Unknown,
        Empty,
        NonEmpty
    }

    private sealed class ComponentTypes(
        INamedTypeSymbol fabMenu,
        INamedTypeSymbol buttonItem,
        INamedTypeSymbol anchorItem) {

        public ComponentKind GetComponentKind(ITypeSymbol componentType) {
            if (SymbolEqualityComparer.Default.Equals(componentType, fabMenu)) {
                return ComponentKind.FabMenu;
            }

            if (SymbolEqualityComparer.Default.Equals(componentType, buttonItem)) {
                return ComponentKind.ButtonItem;
            }

            return SymbolEqualityComparer.Default.Equals(componentType, anchorItem)
                ? ComponentKind.AnchorItem
                : ComponentKind.None;
        }
    }

    private sealed class ComponentFrame(ComponentKind kind, Location location) {
        public Dictionary<string, RecordedAttribute> Attributes { get; } = new(StringComparer.Ordinal);

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
