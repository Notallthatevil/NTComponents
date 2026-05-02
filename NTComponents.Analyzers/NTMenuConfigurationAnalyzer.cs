using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace NTComponents.Analyzers;

/// <summary>
///     Warns when <c>NTMenu</c> is configured in a way that produces an inaccessible or empty menu.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NTMenuConfigurationAnalyzer : DiagnosticAnalyzer {

    public const string MissingAriaLabelDiagnosticId = "NTC1047";
    public const string MissingMenuItemDiagnosticId = "NTC1048";
    public const string EmptyMenuItemLabelDiagnosticId = "NTC1049";
    public const string EmptyMenuItemHrefDiagnosticId = "NTC1050";
    public const string InvisibleColorDiagnosticId = "NTC1051";

    private static readonly DiagnosticDescriptor MissingAriaLabelRule = new(
        MissingAriaLabelDiagnosticId,
        "NTMenu requires an aria label",
        "NTMenu requires a non-empty AriaLabel that describes the menu",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor MissingMenuItemRule = new(
        MissingMenuItemDiagnosticId,
        "NTMenu needs an actionable item",
        "NTMenu requires at least one NTMenuButtonItem, NTMenuAnchorItem, or NTMenuSubMenuItem child",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor EmptyMenuItemLabelRule = new(
        EmptyMenuItemLabelDiagnosticId,
        "NTMenu item label cannot be empty",
        "{0} requires a non-empty Label",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor EmptyMenuItemHrefRule = new(
        EmptyMenuItemHrefDiagnosticId,
        "NTMenu anchor item href cannot be empty",
        "NTMenuAnchorItem requires a non-empty Href",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor InvisibleColorRule = new(
        InvisibleColorDiagnosticId,
        "NTMenu colors must be visible",
        "NTMenu {0} must be a visible menu color",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [
        MissingAriaLabelRule,
        MissingMenuItemRule,
        EmptyMenuItemLabelRule,
        EmptyMenuItemHrefRule,
        InvisibleColorRule
    ];

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context) {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(static startContext => {
            var menuType = startContext.Compilation.GetTypeByMetadataName("NTComponents.NTMenu");
            var buttonItemType = startContext.Compilation.GetTypeByMetadataName("NTComponents.NTMenuButtonItem");
            var anchorItemType = startContext.Compilation.GetTypeByMetadataName("NTComponents.NTMenuAnchorItem");
            var dividerItemType = startContext.Compilation.GetTypeByMetadataName("NTComponents.NTMenuDividerItem");
            var labelItemType = startContext.Compilation.GetTypeByMetadataName("NTComponents.NTMenuLabelItem");
            var subMenuItemType = startContext.Compilation.GetTypeByMetadataName("NTComponents.NTMenuSubMenuItem");
            var colorType = startContext.Compilation.GetTypeByMetadataName("NTComponents.TnTColor");

            if (menuType is null
                || buttonItemType is null
                || anchorItemType is null
                || dividerItemType is null
                || labelItemType is null
                || subMenuItemType is null
                || colorType is null) {
                return;
            }

            var componentTypes = new ComponentTypes(menuType, buttonItemType, anchorItemType, dividerItemType, labelItemType, subMenuItemType);

            startContext.RegisterSyntaxNodeAction(
                nodeContext => AnalyzeExecutableNode(nodeContext, componentTypes, colorType),
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
            if (TryGetOpenedComponent(invocation, context.SemanticModel, componentTypes, out var componentKind)) {
                stack.Push(new ComponentFrame(componentKind, invocation.GetLocation()));
                continue;
            }

            if (IsCloseComponentInvocation(invocation, context.SemanticModel)) {
                if (stack.Count == 0) {
                    continue;
                }

                AnalyzeComponentFrame(context, stack.Pop(), componentTypes, colorType);
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
        INamedTypeSymbol colorType) {
        switch (frame.Kind) {
            case ComponentKind.Menu:
                AnalyzeMenuFrame(context, frame, componentTypes, colorType);
                break;

            case ComponentKind.ButtonItem:
                AnalyzeMenuItemLabel(context, frame, "NTMenuButtonItem");
                break;

            case ComponentKind.AnchorItem:
                AnalyzeMenuItemLabel(context, frame, "NTMenuAnchorItem");
                AnalyzeAnchorItemHref(context, frame);
                break;

            case ComponentKind.SubMenuItem:
                AnalyzeMenuItemLabel(context, frame, "NTMenuSubMenuItem");
                break;

            case ComponentKind.LabelItem:
                AnalyzeMenuItemLabel(context, frame, "NTMenuLabelItem");
                break;
        }
    }

    private static void AnalyzeMenuFrame(
        SyntaxNodeAnalysisContext context,
        ComponentFrame frame,
        ComponentTypes componentTypes,
        INamedTypeSymbol colorType) {
        AnalyzeAriaLabel(context, frame);
        AnalyzeColor(context, frame, colorType, "ContainerColor");
        AnalyzeColor(context, frame, colorType, "TextColor");
        AnalyzeColor(context, frame, colorType, "SelectedContainerColor");
        AnalyzeColor(context, frame, colorType, "SelectedTextColor");
        AnalyzeMenuItems(context, frame, componentTypes);
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

    private static void AnalyzeMenuItems(SyntaxNodeAnalysisContext context, ComponentFrame frame, ComponentTypes componentTypes) {
        if (!frame.Attributes.TryGetValue("ChildContent", out var childContent)) {
            context.ReportDiagnostic(Diagnostic.Create(MissingMenuItemRule, frame.Location));
            return;
        }

        if (TryGetStaticMenuItemStatus(childContent.Operation?.Syntax, context.SemanticModel, componentTypes, out var hasActionItem)
            && !hasActionItem) {
            context.ReportDiagnostic(Diagnostic.Create(MissingMenuItemRule, childContent.Location));
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

    private static bool TryGetStaticMenuItemStatus(SyntaxNode? syntax, SemanticModel semanticModel, ComponentTypes componentTypes, out bool hasActionItem) {
        hasActionItem = false;

        if (syntax is null) {
            return false;
        }

        var lambda = syntax.DescendantNodesAndSelf().OfType<AnonymousFunctionExpressionSyntax>().FirstOrDefault();
        if (lambda is null || GetBodyNode(lambda) is not { } bodyNode) {
            return false;
        }

        var foundAnyKnownMenuItem = false;
        var invocations = bodyNode
            .DescendantNodes(static node => !IsNestedExecutableBoundary(node))
            .OfType<InvocationExpressionSyntax>();

        foreach (var invocation in invocations) {
            if (!TryGetOpenedComponent(invocation, semanticModel, componentTypes, out var componentKind)) {
                continue;
            }

            switch (componentKind) {
                case ComponentKind.ButtonItem:
                case ComponentKind.AnchorItem:
                case ComponentKind.SubMenuItem:
                    hasActionItem = true;
                    return true;

                case ComponentKind.DividerItem:
                case ComponentKind.LabelItem:
                    foundAnyKnownMenuItem = true;
                    break;
            }
        }

        return foundAnyKnownMenuItem;
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

    private static bool TryGetInvocationTarget(InvocationExpressionSyntax invocation, SemanticModel semanticModel, out IMethodSymbol methodSymbol) {
        var symbol = semanticModel.GetSymbolInfo(invocation).Symbol
            ?? semanticModel.GetSymbolInfo(invocation).CandidateSymbols.FirstOrDefault();

        methodSymbol = symbol as IMethodSymbol ?? null!;
        return methodSymbol is not null;
    }

    private enum ComponentKind {
        None,
        Menu,
        ButtonItem,
        AnchorItem,
        DividerItem,
        LabelItem,
        SubMenuItem
    }

    private enum KnownStringState {
        Unknown,
        Empty,
        NonEmpty
    }

    private sealed class ComponentTypes(
        INamedTypeSymbol menu,
        INamedTypeSymbol buttonItem,
        INamedTypeSymbol anchorItem,
        INamedTypeSymbol dividerItem,
        INamedTypeSymbol labelItem,
        INamedTypeSymbol subMenuItem) {

        public ComponentKind GetComponentKind(ITypeSymbol componentType) {
            if (SymbolEqualityComparer.Default.Equals(componentType, menu)) {
                return ComponentKind.Menu;
            }

            if (SymbolEqualityComparer.Default.Equals(componentType, buttonItem)) {
                return ComponentKind.ButtonItem;
            }

            if (SymbolEqualityComparer.Default.Equals(componentType, anchorItem)) {
                return ComponentKind.AnchorItem;
            }

            if (SymbolEqualityComparer.Default.Equals(componentType, dividerItem)) {
                return ComponentKind.DividerItem;
            }

            if (SymbolEqualityComparer.Default.Equals(componentType, labelItem)) {
                return ComponentKind.LabelItem;
            }

            return SymbolEqualityComparer.Default.Equals(componentType, subMenuItem)
                ? ComponentKind.SubMenuItem
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
