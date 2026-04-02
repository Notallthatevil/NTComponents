using Microsoft.AspNetCore.Components;
using NTComponents.Core;

namespace NTComponents;

/// <summary>
/// Header region for <see cref="NTLayout" />.
/// </summary>
public partial class NTHeader : NTLayoutElementBase {
    /// <summary>
    /// Gets or sets the background color override.
    /// </summary>
    [Parameter]
    public TnTColor? BackgroundColor { get; set; }

    /// <summary>
    /// Gets the resolved sticky marker.
    /// </summary>
    protected string ResolvedStickyAttribute => ResolvedSticky ? "true" : "false";

    /// <summary>
    /// Gets or sets whether the header should stick to the top edge.
    /// </summary>
    [Parameter]
    public bool? Sticky { get; set; }

    /// <inheritdoc />
    [Parameter]
    public TextAlign? TextAlignment { get; set; }

    /// <summary>
    /// Gets or sets the text color override.
    /// </summary>
    [Parameter]
    public TnTColor? TextColor { get; set; }

    private TnTColor ResolvedBackgroundColor => ResolveValue(BackgroundColor, EffectiveDefaultOptions.Layout.HeaderBackgroundColor);

    private bool ResolvedSticky => ResolveValue(Sticky, EffectiveDefaultOptions.Layout.HeaderSticky);

    /// <inheritdoc />
    protected override string ResolvedTagName => NormalizeTagName(TagName, EffectiveDefaultOptions.Layout.HeaderTagName, "header");

    private TnTColor ResolvedTextColor => ResolveValue(TextColor, EffectiveDefaultOptions.Layout.HeaderTextColor);

    /// <inheritdoc />
    public override string? ElementClass => CssClassBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddClass("nt-layout-header")
        .AddClass("nt-layout-header-sticky", ResolvedSticky)
        .AddTextAlign(TextAlignment)
        .Build();

    /// <inheritdoc />
    public override string? ElementStyle => CssStyleBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddVariable("nt-layout-header-background-color", ResolvedBackgroundColor)
        .AddVariable("nt-layout-header-text-color", ResolvedTextColor)
        .Build();

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, object>? RootStateAttributes => new Dictionary<string, object> {
        ["data-nt-sticky"] = ResolvedStickyAttribute
    };
}
