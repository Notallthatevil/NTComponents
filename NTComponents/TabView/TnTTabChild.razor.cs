using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using NTComponents.Core;
using NTComponents.Interfaces;

namespace NTComponents;

/// <summary>
///     Represents a child tab component within a <see cref="TnTTabView" />.
/// </summary>
public partial class TnTTabChild {

    /// <summary>
    ///     Gets or sets the content to be rendered inside the tab.
    /// </summary>
    [Parameter]
    public RenderFragment ChildContent { get; set; } = default!;

    /// <inheritdoc />
    [Parameter]
    public bool Disabled { get; set; }

    /// <inheritdoc />
    public override string? ElementClass => CssClassBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddClass("tnt-tab-child")
        .AddDisabled(Disabled)
        .Build();

    /// <inheritdoc />
    [Parameter]
    public string? ElementName { get; set; }

    /// <inheritdoc />
    public override string? ElementStyle => CssStyleBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .Build();

    /// <inheritdoc />
    [Parameter]
    public bool EnableRipple { get; set; }

    /// <summary>
    ///     Gets or sets the icon to be displayed in the tab header.
    /// </summary>
    [Parameter]
    public TnTIcon? Icon { get; set; }

    /// <summary>
    ///     Gets or sets the label for the tab.
    /// </summary>
    [Parameter, EditorRequired]
    public string Label { get; set; } = default!;

    /// <inheritdoc />
    [Parameter]
    public TnTColor? OnTintColor { get; set; }

    /// <summary>
    ///     Gets or sets the tooltip for the tab header.
    /// </summary>
    [Parameter]
    public RenderFragment? HeaderTooltip { get; set; }

    /// <inheritdoc />
    [Parameter]
    public TnTColor? TintColor { get; set; }

    /// <summary>
    ///     Gets or sets the parent <see cref="TnTTabView" /> context.
    /// </summary>
    [CascadingParameter]
    private TnTTabView _context { get; set; } = default!;

    internal string HeaderElementId => $"{ComponentIdentifier}-tab";
    internal string PanelDomId => ElementId ?? PanelElementId;
    internal string PanelElementId => $"{ComponentIdentifier}-panel";

    /// <summary>
    ///     Disposes the tab child and removes it from the parent context.
    /// </summary>
    public void Dispose() {
        _context.RemoveTabChild(this);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    protected override void OnInitialized() {
        base.OnInitialized();
        if (_context is null) {
            throw new InvalidOperationException($"A {nameof(TnTTabChild)} must be a child of {nameof(TnTTabView)}");
        }
        _context.AddTabChild(this);
    }
}
