using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace NTComponents.Analyzers;

/// <summary>
///     Warns when <c>NTInputBase</c>-derived inputs are configured in ways that bypass the component-owned input contract.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NTInputConfigurationAnalyzer : DiagnosticAnalyzer {

    public const string PhoneMaskRequiresTelDiagnosticId = "NTC1059";
    public const string InvalidPhoneMaskDiagnosticId = "NTC1060";
    public const string ComponentOwnedInputAttributeDiagnosticId = "NTC1061";

    private static readonly ImmutableDictionary<string, string> ComponentOwnedInputAttributes = ImmutableDictionary.CreateRange(
        StringComparer.Ordinal,
        new[] {
            new KeyValuePair<string, string>("aria-describedby", "SupportingText, PrefixText, SuffixText, or validation text"),
            new KeyValuePair<string, string>("aria-errormessage", "ErrorText or validation text"),
            new KeyValuePair<string, string>("aria-invalid", "ErrorText or EditContext validation"),
            new KeyValuePair<string, string>("autocomplete", "AutoComplete"),
            new KeyValuePair<string, string>("autofocus", "AutoFocus"),
            new KeyValuePair<string, string>("disabled", "Disabled"),
            new KeyValuePair<string, string>("id", "ElementId"),
            new KeyValuePair<string, string>("oninput", "BindOnInput or a specialized input component"),
            new KeyValuePair<string, string>("placeholder", "Placeholder"),
            new KeyValuePair<string, string>("readonly", "ReadOnly"),
            new KeyValuePair<string, string>("title", "ElementTitle"),
            new KeyValuePair<string, string>("type", "the typed input component or InputType parameter"),
            new KeyValuePair<string, string>("value", "Value or @bind-Value")
        });

    private static readonly DiagnosticDescriptor PhoneMaskRequiresTelRule = new(
        PhoneMaskRequiresTelDiagnosticId,
        "NTInputText PhoneMask requires tel input",
        "NTInputText PhoneMask is ignored unless InputType is TextInputType.Tel",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor InvalidPhoneMaskRule = new(
        InvalidPhoneMaskDiagnosticId,
        "NTInputText PhoneMask must contain digit placeholders",
        "NTInputText PhoneMask should contain at least one '#' digit placeholder",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor ComponentOwnedInputAttributeRule = new(
        ComponentOwnedInputAttributeDiagnosticId,
        "NTInputBase owns this native input attribute",
        "Do not set '{0}' directly on {1}. Use {2} so NTInputBase can keep accessibility, binding, validation, and SSR form-post behavior consistent.",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [
        PhoneMaskRequiresTelRule,
        InvalidPhoneMaskRule,
        ComponentOwnedInputAttributeRule
    ];

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context) {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(static startContext => {
            var inputBaseType = startContext.Compilation.GetTypeByMetadataName("NTComponents.NTInputBase`1");
            if (inputBaseType is null) {
                return;
            }

            var inputTextType = startContext.Compilation.GetTypeByMetadataName("NTComponents.NTInputText");
            var textInputType = startContext.Compilation.GetTypeByMetadataName("NTComponents.TextInputType");

            startContext.RegisterSyntaxNodeAction(
                nodeContext => AnalyzeExecutableNode(nodeContext, inputBaseType, inputTextType, textInputType),
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
        INamedTypeSymbol inputBaseType,
        INamedTypeSymbol? inputTextType,
        INamedTypeSymbol? textInputType) {
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
            if (TryGetOpenedComponent(invocation, context.SemanticModel, inputBaseType, inputTextType, out var componentFrame)) {
                stack.Push(componentFrame);
                continue;
            }

            if (IsCloseComponentInvocation(invocation, context.SemanticModel)) {
                if (stack.Count == 0) {
                    continue;
                }

                var frame = stack.Pop();
                if (frame.IsInputComponent) {
                    AnalyzeComponentFrame(context, frame, textInputType);
                }

                continue;
            }

            if (stack.Count == 0 || !stack.Peek().IsInputComponent) {
                continue;
            }

            if (TryGetComponentAttribute(invocation, context.SemanticModel, out var name, out var attributeValue)) {
                stack.Peek().Attributes[name] = attributeValue;
            }
        }
    }

    private static void AnalyzeComponentFrame(SyntaxNodeAnalysisContext context, ComponentFrame frame, INamedTypeSymbol? textInputType) {
        AnalyzeComponentOwnedInputAttributes(context, frame);

        if (frame.Kind == ComponentKind.InputText && textInputType is not null) {
            AnalyzePhoneMask(context, frame, textInputType);
        }
    }

    private static void AnalyzeComponentOwnedInputAttributes(SyntaxNodeAnalysisContext context, ComponentFrame frame) {
        foreach (var attribute in ComponentOwnedInputAttributes) {
            if (!frame.Attributes.TryGetValue(attribute.Key, out var recordedAttribute)) {
                continue;
            }

            context.ReportDiagnostic(Diagnostic.Create(ComponentOwnedInputAttributeRule, recordedAttribute.Location, attribute.Key, frame.ComponentName, attribute.Value));
        }
    }

    private static void AnalyzePhoneMask(SyntaxNodeAnalysisContext context, ComponentFrame frame, INamedTypeSymbol textInputType) {
        if (!frame.Attributes.TryGetValue("PhoneMask", out var phoneMask)) {
            return;
        }

        var effectiveInputType = GetEffectiveTextInputType(frame, textInputType);
        if (effectiveInputType is null) {
            return;
        }

        if (effectiveInputType != "Tel") {
            context.ReportDiagnostic(Diagnostic.Create(PhoneMaskRequiresTelRule, phoneMask.Location));
            return;
        }

        if (IsNullConstant(phoneMask.Operation)
            || (TryGetStringConstant(phoneMask.Operation, out var mask) && !ContainsDigitPlaceholder(mask))) {
            context.ReportDiagnostic(Diagnostic.Create(InvalidPhoneMaskRule, phoneMask.Location));
        }
    }

    private static string? GetEffectiveTextInputType(ComponentFrame frame, INamedTypeSymbol textInputType) {
        if (!frame.Attributes.TryGetValue("InputType", out var inputType)) {
            return "Text";
        }

        return TryGetEnumMemberName(inputType.Operation, textInputType, out var inputTypeName)
            ? inputTypeName
            : null;
    }

    private static bool ContainsDigitPlaceholder(string? mask) => !string.IsNullOrWhiteSpace(mask) && mask.Contains("#", StringComparison.Ordinal);

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
        INamedTypeSymbol inputBaseType,
        INamedTypeSymbol? inputTextType,
        out ComponentFrame componentFrame) {
        componentFrame = default!;

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

        var isInputText = inputTextType is not null && SymbolEqualityComparer.Default.Equals(openedComponentType, inputTextType);
        var isInputBase = IsOrDerivesFrom(openedComponentType, inputBaseType);
        var kind = isInputText ? ComponentKind.InputText : isInputBase ? ComponentKind.InputBase : ComponentKind.Other;
        componentFrame = new ComponentFrame(kind, openedComponentType.Name, invocation.GetLocation());
        return true;
    }

    private static bool IsOrDerivesFrom(ITypeSymbol type, INamedTypeSymbol baseType) {
        var current = type as INamedTypeSymbol;
        while (current is not null) {
            if (SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, baseType)) {
                return true;
            }

            current = current.BaseType;
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
        Other,
        InputBase,
        InputText
    }

    private sealed class ComponentFrame(ComponentKind kind, string componentName, Location location) {
        public Dictionary<string, RecordedAttribute> Attributes { get; } = new(StringComparer.Ordinal);

        public string ComponentName { get; } = componentName;

        public ComponentKind Kind { get; } = kind;

        public bool IsInputComponent => Kind is ComponentKind.InputBase or ComponentKind.InputText;

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
