using Microsoft.AspNetCore.Components;
using NTComponents.Core;

namespace NTComponents;

/// <summary>
///     Represents a child item within a TnTAccordion component.
/// </summary>
public partial class TnTAccordionChild {

    /// <summary>
    ///     The content to be rendered inside the accordion child.
    /// </summary>
    [Parameter]
    public RenderFragment ChildContent { get; set; } = default!;

    /// <summary>
    ///     The body color of the content.
    /// </summary>
    [Parameter]
    public TnTColor? ContentBodyColor { get; set; }

    /// <summary>
    ///     The text color of the content.
    /// </summary>
    [Parameter]
    public TnTColor? ContentTextColor { get; set; }

    /// <inheritdoc />
    [Parameter]
    public bool Disabled { get; set; }

    /// <inheritdoc />
    public override string? ElementClass => CssClassBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddClass("tnt-accordion-child")
        .Build();

    /// <inheritdoc />
    [Parameter]
    public string? ElementName { get; set; }

    /// <inheritdoc />
    public override string? ElementStyle => CssStyleBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddVariable("tnt-accordion-child-content-bg-color", ContentBodyColor ?? _parent.ContentBodyColor)
        .AddVariable("tnt-accordion-child-content-fg-color", ContentTextColor ?? _parent.ContentTextColor)
        .Build();

    /// <inheritdoc />
    [Parameter]
    public bool EnableRipple { get; set; } = true;

    /// <summary>
    ///     The body color of the header.
    /// </summary>
    [Parameter]
    public TnTColor? HeaderBodyColor { get; set; }

    /// <summary>
    ///     The text color of the header.
    /// </summary>
    [Parameter]
    public TnTColor? HeaderTextColor { get; set; }

    /// <summary>
    ///     The tint color of the header.
    /// </summary>
    [Parameter]
    public TnTColor? HeaderTintColor { get; set; }

    /// <summary>
    ///     A custom template rendered in place of <see cref="Label" /> inside the accordion header button.
    ///     When set, <see cref="Label" /> is ignored.
    /// </summary>
    [Parameter]
    public RenderFragment? HeaderTemplate { get; set; }

    /// <summary>
    ///     The label of the accordion child. Required when <see cref="HeaderTemplate" /> is not provided.
    /// </summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>
    ///     Callback invoked after the accordion has closed.
    /// </summary>
    [Parameter]
    public EventCallback OnCloseCallback { get; set; }

    /// <summary>
    ///     Callback invoked after the accordion has opened.
    /// </summary>
    [Parameter]
    public EventCallback OnOpenCallback { get; set; }

    /// <inheritdoc />
    [Parameter]
    public TnTColor? OnTintColor { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the accordion child is open by default.
    /// </summary>
    [Parameter]
    public bool OpenByDefault { get; set; }

    /// <summary>
    /// Specifies the sorting order of the accordion child. Lower numbers are rendered first.
    /// </summary>
    [Parameter]
    public int? Order { get; set; }

    /// <summary>
    ///     When set, removes the child content from the DOM when the accordion is closed. When opened, the content is added back to the DOM. Only applies in interactive mode.
    /// </summary>
    [Parameter]
    public bool RemoveContentOnClose { get; set; }

    /// <inheritdoc />
    [Parameter]
    public TnTColor? TintColor { get; set; }

    internal int _elementId = int.MinValue;

    internal bool? _open;
    internal string ContentElementId => $"{ComponentIdentifier}-content";
    internal string HeaderElementId => $"{ComponentIdentifier}-header";

    [CascadingParameter]
    private TnTAccordion _parent { get; set; } = default!;

    /// <summary>
    ///     Closes the accordion child asynchronously.
    /// </summary>
    public async Task CloseAsync() => await _parent.SetAsClosed(_elementId);

    /// <inheritdoc />
    public void Dispose() {
        GC.SuppressFinalize(this);
        _parent.RemoveChild(this);
    }

    /// <inheritdoc />
    protected override void OnInitialized() {
        base.OnInitialized();
        _parent.RegisterChild(this);
        if (_parent.AllowOpenByDefault && OpenByDefault) {
            _open = !_parent.LimitToOneExpanded || _parent.TryReserveDefaultOpen();
        }
        else {
            _open = null;
        }
    }
}
