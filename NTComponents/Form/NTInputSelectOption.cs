#pragma warning disable CS0618
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System.Diagnostics.CodeAnalysis;

using NTComponents.CodeDocumentation;
namespace NTComponents;

/// <summary>
///     Declares a single option inside obsolete <see cref="NTInputSelect{TInputType}" />.
/// </summary>
/// <typeparam name="TInputType">The bound value type.</typeparam>
[System.Obsolete("NTInputSelectOption is obsolete because NTInputSelect is obsolete. Use NTAutocompleteOption with NTAutocomplete or native option content with NTSelect.")]
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.InteractiveRequired,
    CompatibilitySummary = "Obsolete option metadata for the obsolete NTInputSelect component.",
    CompatibilityDetails = "The option contributes metadata to obsolete NTInputSelect, whose legacy searchable selection behavior requires interactive Blazor and page script support. Use NTAutocompleteOption with NTAutocomplete for searchable text suggestions or native option content with NTSelect for single-select choices.")]
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
