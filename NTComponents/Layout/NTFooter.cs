using Microsoft.AspNetCore.Components;
using NTComponents.Core;

namespace NTComponents;

/// <summary>
/// Footer region for <see cref="NTLayout" />.
/// </summary>
public partial class NTFooter : NTLayoutElementBase {
    /// <summary>
    /// Gets or sets the background color override.
    /// </summary>
    [Parameter]
    public TnTColor? BackgroundColor { get; set; }

    /// <summary>
    /// Gets or sets the elevation override.
    /// </summary>
    [Parameter]
    public int? Elevation { get; set; }

    /// <summary>
    /// Gets the resolved sticky marker.
    /// </summary>
    protected string ResolvedStickyAttribute => ResolvedSticky ? "true" : "false";

    /// <summary>
    /// Gets or sets whether the footer should stick to the bottom edge.
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

    private TnTColor ResolvedBackgroundColor => ResolveValue(BackgroundColor, EffectiveDefaultOptions.Layout.FooterBackgroundColor);

    private int ResolvedElevation => ResolveValue(Elevation, EffectiveDefaultOptions.Layout.FooterElevation);

    private bool ResolvedSticky => ResolveValue(Sticky, EffectiveDefaultOptions.Layout.FooterSticky);

    /// <inheritdoc />
    protected override string ResolvedTagName => NormalizeTagName(TagName, EffectiveDefaultOptions.Layout.FooterTagName, "footer");

    private TnTColor ResolvedTextColor => ResolveValue(TextColor, EffectiveDefaultOptions.Layout.FooterTextColor);

    /// <inheritdoc />
    public override string? ElementClass => CssClassBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddClass("nt-layout-footer")
        .AddClass("nt-layout-footer-sticky", ResolvedSticky)
        .AddElevation(ResolvedElevation)
        .AddTextAlign(TextAlignment)
        .Build();

    /// <inheritdoc />
    public override string? ElementStyle => CssStyleBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddVariable("nt-layout-footer-background-color", ResolvedBackgroundColor)
        .AddVariable("nt-layout-footer-text-color", ResolvedTextColor)
        .Build();

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, object>? RootStateAttributes => new Dictionary<string, object> {
        ["data-nt-sticky"] = ResolvedStickyAttribute
    };
}
