using Microsoft.AspNetCore.Components;
using NTComponents.Core;

namespace NTComponents;

/// <summary>
/// Semantic layout shell for header/body/footer page composition.
/// </summary>
public partial class NTLayout : NTLayoutElementBase {
    /// <summary>
    /// Gets or sets the background color override.
    /// </summary>
    [Parameter]
    public TnTColor? BackgroundColor { get; set; }

    /// <summary>
    /// Gets or sets whether the layout should fill the viewport.
    /// </summary>
    [Parameter]
    public bool? FillViewport { get; set; }

    /// <summary>
    /// Gets the resolved viewport marker.
    /// </summary>
    protected string ResolvedFillViewportAttribute => ResolvedFillViewport ? "true" : "false";

    /// <summary>
    /// Gets the resolved layout mode marker.
    /// </summary>
    protected string ResolvedModeAttribute => ResolvedMode.ToString().ToLowerInvariant();

    /// <summary>
    /// Gets or sets the layout mode override.
    /// </summary>
    [Parameter]
    public NTLayoutMode? Mode { get; set; }

    /// <inheritdoc />
    [Parameter]
    public TextAlign? TextAlignment { get; set; }

    /// <summary>
    /// Gets or sets the text color override.
    /// </summary>
    [Parameter]
    public TnTColor? TextColor { get; set; }

    private TnTColor ResolvedBackgroundColor => ResolveValue(BackgroundColor, EffectiveDefaultOptions.Layout.LayoutBackgroundColor);

    private bool ResolvedFillViewport => ResolveValue(FillViewport, EffectiveDefaultOptions.Layout.FillViewport);

    private NTLayoutMode ResolvedMode => ResolveValue(Mode, EffectiveDefaultOptions.Layout.Mode);

    /// <inheritdoc />
    protected override string ResolvedTagName => NormalizeTagName(TagName, EffectiveDefaultOptions.Layout.LayoutTagName, "div");

    private TnTColor ResolvedTextColor => ResolveValue(TextColor, EffectiveDefaultOptions.Layout.LayoutTextColor);

    /// <inheritdoc />
    public override string? ElementClass => CssClassBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddClass("nt-layout")
        .AddClass("nt-layout-root", ResolvedMode == NTLayoutMode.Root)
        .AddClass("nt-layout-nested", ResolvedMode == NTLayoutMode.Nested)
        .AddClass("nt-layout-fill-viewport", ResolvedFillViewport)
        .AddTextAlign(TextAlignment)
        .Build();

    /// <inheritdoc />
    public override string? ElementStyle => CssStyleBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddVariable("nt-layout-background-color", ResolvedBackgroundColor)
        .AddVariable("nt-layout-text-color", ResolvedTextColor)
        .Build();

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, object>? RootStateAttributes => new Dictionary<string, object> {
        ["data-nt-mode"] = ResolvedModeAttribute,
        ["data-nt-fill-viewport"] = ResolvedFillViewportAttribute
    };
}

/// <summary>
/// Controls whether <see cref="NTLayout" /> behaves as a top-level shell or a nested layout container.
/// </summary>
public enum NTLayoutMode {
    /// <summary>
    /// Uses the full root layout shell behavior.
    /// </summary>
    Root,

    /// <summary>
    /// Uses the lightweight nested layout behavior.
    /// </summary>
    Nested
}
