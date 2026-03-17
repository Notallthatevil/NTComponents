using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System.Diagnostics.CodeAnalysis;

namespace NTComponents;

/// <summary>
///     Declares a single option inside <see cref="NTInputSelect{TInputType}" />.
/// </summary>
/// <typeparam name="TInputType">The bound value type.</typeparam>
public sealed class NTInputSelectOption<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TInputType> : ComponentBase, IDisposable {

    /// <summary>
    ///     The content rendered for this option in the dropdown. When omitted, <see cref="Label" /> is rendered.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    ///     The display label used for searching and the default rendered text.
    /// </summary>
    [Parameter, EditorRequired]
    public string? Label { get; set; }

    /// <summary>
    ///     The bound value for this option.
    /// </summary>
    [Parameter, EditorRequired]
    public TInputType? Value { get; set; }

    /// <summary>
    ///     Gets the parent select context.
    /// </summary>
    [CascadingParameter]
    private NTInputSelect<TInputType> _context { get; set; } = default!;

    /// <inheritdoc />
    public void Dispose() {
        _context.RemoveOptionChild(this);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    protected override void OnInitialized() {
        base.OnInitialized();
        if (_context is null) {
            throw new InvalidOperationException($"A {nameof(NTInputSelectOption<TInputType>)} must be a child of {nameof(NTInputSelect<TInputType>)}");
        }

        _context.AddOptionChild(this);
    }

    /// <inheritdoc />
    protected override void OnParametersSet() {
        base.OnParametersSet();
        _context.NotifyOptionChildChanged(this);
    }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder) { }
}
