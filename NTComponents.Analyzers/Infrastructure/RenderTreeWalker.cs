using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace NTComponents.Analyzers;

/// <summary>
///     Rebuilds the component tree from <c>RenderTreeBuilder</c> calls in one member, following child content lambdas so each
///     component knows its parent and children. Each frame is handed to <c>onClose</c> once its parameters and children are known.
/// </summary>
internal sealed class RenderTreeWalker {
    private static readonly HashSet<string> BuilderMethodNames = new(StringComparer.Ordinal) {
        "OpenComponent", "CloseComponent", "OpenElement", "CloseElement", "AddAttribute", "AddComponentParameter", "AddContent", "AddMultipleAttributes"
    };

    private readonly SemanticModel _model;
    private readonly INamedTypeSymbol _builderType;
    private readonly Action<ComponentFrame> _onClose;
    private readonly CancellationToken _cancellationToken;
    private readonly SyntaxNode _member;
    private readonly bool _isPageBuild;
    private readonly HashSet<SyntaxNode> _claimedLambdas = [];

    private RenderTreeWalker(SyntaxNodeAnalysisContext context, INamedTypeSymbol builderType, Action<ComponentFrame> onClose) {
        _model = context.SemanticModel;
        _builderType = builderType;
        _onClose = onClose;
        _cancellationToken = context.CancellationToken;
        _member = context.Node;
        _isPageBuild = _member is MethodDeclarationSyntax { Identifier.ValueText: "BuildRenderTree" }
            && _model.GetDeclaredSymbol(_member, _cancellationToken)?.ContainingType?.GetAttributes()
                .Any(static attribute => attribute.AttributeClass?.ToDisplayString() == "Microsoft.AspNetCore.Components.RouteAttribute") == true;
    }

    /// <summary>Walks a member declaration. Members without an <c>OpenComponent</c> call are skipped before any semantic work.</summary>
    public static void Walk(SyntaxNodeAnalysisContext context, INamedTypeSymbol builderType, Action<ComponentFrame> onClose) {
        if (context.Node.DescendantNodes().OfType<InvocationExpressionSyntax>().Any(static invocation => NameOf(invocation) == "OpenComponent")) {
            new RenderTreeWalker(context, builderType, onClose).WalkRegion(context.Node, owner: null);
        }
    }

    private void WalkRegion(SyntaxNode region, ComponentFrame? owner) {
        // A null entry is an open element; attributes only belong to a component on top of the stack.
        var open = new Stack<ComponentFrame?>();
        foreach (var node in region.DescendantNodes(node => node == region || !IsBoundary(node))) {
            _cancellationToken.ThrowIfCancellationRequested();
            if (IsBoundary(node)) {
                if (!_claimedLambdas.Contains(node)) {
                    WalkRegion(node, owner: null);
                }

                continue;
            }

            if (node is not InvocationExpressionSyntax invocation) {
                continue;
            }

            if (!BuilderMethodNames.Contains(NameOf(invocation))
                || _model.GetSymbolInfo(invocation, _cancellationToken).Symbol is not IMethodSymbol method
                || !SymbolEqualityComparer.Default.Equals(method.ContainingType, _builderType)) {
                // A helper that receives the builder renders content the walker cannot see.
                if ((open.FirstOrDefault(static entry => entry is not null) ?? owner) is { } target && PassesBuilder(invocation)) {
                    target.RendersOpaqueContent = true;
                }

                continue;
            }

            var arguments = invocation.ArgumentList.Arguments;
            var current = open.Count > 0 ? open.Peek() : null;
            var parent = open.FirstOrDefault(static entry => entry is not null) ?? owner;
            switch (method.Name) {
                case "OpenComponent":
                    var type = OpenedType(method, arguments) as INamedTypeSymbol ?? _model.Compilation.ObjectType;
                    var frame = new ComponentFrame(type, invocation, OpenLocation(invocation), parent, IsConditional(invocation, region, parent), _isPageBuild && region == _member && parent is null);
                    parent?.Children.Add(frame);
                    open.Push(frame);
                    break;

                case "OpenElement":
                    open.Push(null);
                    break;

                case "CloseComponent" or "CloseElement":
                    if (open.Count > 0 && open.Pop() is { } closed) {
                        _onClose(closed);
                    }

                    break;

                case "AddAttribute" or "AddComponentParameter" when current is not null && arguments.Count >= 3:
                    if (_model.GetConstantValue(arguments[1].Expression, _cancellationToken).Value is string name) {
                        var value = _model.GetOperation(arguments[2].Expression, _cancellationToken);
                        current.SetAttribute(new ComponentAttribute(name, value, AttributeLocation(arguments[1].Expression, arguments[2].Expression, value)));
                    }
                    else {
                        // A computed name may supply any parameter, just like @attributes.
                        current.HasSplattedAttributes = true;
                    }

                    MarkOpaqueUnlessWalked(arguments[2].Expression, current);
                    break;

                case "AddMultipleAttributes" when current is not null:
                    current.HasSplattedAttributes = true;
                    break;

                case "AddContent" when parent is not null && arguments.Count >= 2:
                    MarkOpaqueUnlessWalked(arguments[1].Expression, parent);
                    break;
            }
        }
    }

    /// <summary>Walks the lambdas in <paramref name="value" />; a render fragment with no lambda to walk makes the owner's content opaque.</summary>
    private void MarkOpaqueUnlessWalked(ExpressionSyntax value, ComponentFrame owner) {
        if (!WalkFragments(value, owner) && IsRenderFragment(value)) {
            owner.RendersOpaqueContent = true;
        }
    }

    /// <summary>Walks the lambdas in a parameter value as content of <paramref name="owner" />. Returns false when there were none.</summary>
    private bool WalkFragments(ExpressionSyntax value, ComponentFrame owner) {
        var found = false;
        foreach (var lambda in value.DescendantNodesAndSelf(node => node == value || !IsBoundary(node)).Where(IsBoundary).ToArray()) {
            // Templates such as RenderFragment<T> nest the builder lambda inside a context lambda.
            var body = lambda;
            _claimedLambdas.Add(body);
            while (body is AnonymousFunctionExpressionSyntax { ExpressionBody: AnonymousFunctionExpressionSyntax inner }) {
                body = inner;
                _claimedLambdas.Add(body);
            }

            WalkRegion(body, owner);
            found = true;
        }

        return found;
    }

    private bool PassesBuilder(InvocationExpressionSyntax invocation) =>
        invocation.ArgumentList.Arguments.Any(argument => SymbolEqualityComparer.Default.Equals(_model.GetTypeInfo(argument.Expression, _cancellationToken).Type, _builderType));

    private bool IsRenderFragment(ExpressionSyntax value) =>
        _model.GetTypeInfo(value, _cancellationToken).Type?.OriginalDefinition.ToDisplayString() is "Microsoft.AspNetCore.Components.RenderFragment" or "Microsoft.AspNetCore.Components.RenderFragment<TValue>";

    private ITypeSymbol? OpenedType(IMethodSymbol method, SeparatedSyntaxList<ArgumentSyntax> arguments) {
        if (method.IsGenericMethod) {
            return method.TypeArguments[0];
        }

        return arguments.Count >= 2 && _model.GetOperation(arguments[1].Expression, _cancellationToken) is ITypeOfOperation typeOf ? typeOf.TypeOperand : null;
    }

    private static Location OpenLocation(InvocationExpressionSyntax invocation) {
        var typeArguments = (invocation.Expression as MemberAccessExpressionSyntax)?.Name is GenericNameSyntax generic
            ? generic.TypeArgumentList.Arguments.SelectMany(static argument => argument.DescendantNodesAndSelf()).Select(static node => node.GetLocation())
            : [];
        return SourceLocations.Prefer(invocation.GetLocation(), typeArguments);
    }

    private static Location AttributeLocation(ExpressionSyntax name, ExpressionSyntax value, IOperation? operation) =>
        SourceLocations.Prefer(value.GetLocation(), [OperationConstants.Unwrap(operation)?.Syntax.GetLocation(), name.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().LastOrDefault()?.GetLocation()]);

    private static bool IsConditional(InvocationExpressionSyntax invocation, SyntaxNode region, ComponentFrame? parent) {
        for (var node = invocation.Parent; node is not null && node != region; node = node.Parent) {
            if (parent is not null && node.SyntaxTree == parent.OpenSyntax.SyntaxTree && node.Span.Contains(parent.OpenSyntax.Span)) {
                return false;
            }

            if (node is IfStatementSyntax or SwitchStatementSyntax or SwitchExpressionSyntax or ConditionalExpressionSyntax
                or CommonForEachStatementSyntax or ForStatementSyntax or WhileStatementSyntax or DoStatementSyntax) {
                return true;
            }
        }

        return false;
    }

    private static bool IsBoundary(SyntaxNode node) => node is AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax;

    private static string NameOf(InvocationExpressionSyntax invocation) => invocation.Expression switch {
        MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.ValueText,
        SimpleNameSyntax name => name.Identifier.ValueText,
        _ => string.Empty
    };
}
