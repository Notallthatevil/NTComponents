using Microsoft.AspNetCore.Components;
using NTComponents.Core;

namespace NTComponents;

/// <summary>
/// Main content region for <see cref="NTLayout" />.
/// </summary>
public partial class NTBody : NTLayoutElementBase {
    /// <summary>
    /// Gets or sets the background color override.
    /// </summary>
    [Parameter]
    public TnTColor? BackgroundColor { get; set; }

    /// <summary>
    /// Gets the resolved scroll marker.
    /// </summary>
    protected string ResolvedScrollableAttribute => ResolvedScrollable ? "true" : "false";

    /// <summary>
    /// Gets or sets whether the body region scrolls.
    /// </summary>
    [Parameter]
    public bool? Scrollable { get; set; }

    /// <inheritdoc />
    [Parameter]
    public TextAlign? TextAlignment { get; set; }

    /// <summary>
    /// Gets or sets the text color override.
    /// </summary>
    [Parameter]
    public TnTColor? TextColor { get; set; }

    private TnTColor ResolvedBackgroundColor => ResolveValue(BackgroundColor, EffectiveDefaultOptions.Layout.BodyBackgroundColor);

    private bool ResolvedScrollable => ResolveValue(Scrollable, EffectiveDefaultOptions.Layout.BodyScrollable);

    /// <inheritdoc />
    protected override string ResolvedTagName => NormalizeTagName(TagName, EffectiveDefaultOptions.Layout.BodyTagName, "main");

    private TnTColor ResolvedTextColor => ResolveValue(TextColor, EffectiveDefaultOptions.Layout.BodyTextColor);

    /// <inheritdoc />
    public override string? ElementClass => CssClassBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddClass("nt-layout-body")
        .AddClass("nt-layout-body-scrollable", ResolvedScrollable)
        .AddTextAlign(TextAlignment)
        .Build();

    /// <inheritdoc />
    public override string? ElementStyle => CssStyleBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddVariable("nt-layout-body-background-color", ResolvedBackgroundColor)
        .AddVariable("nt-layout-body-text-color", ResolvedTextColor)
        .Build();

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, object>? RootStateAttributes => new Dictionary<string, object> {
        ["data-nt-scrollable"] = ResolvedScrollableAttribute
    };
}
