using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using NTComponents.Core;

namespace NTComponents;

/// <summary>
///     Represents a bounded label tag for categories, statuses, and metadata chips.
/// </summary>
public partial class NTTag {

    /// <summary>
    ///     Gets or sets the tag background color.
    /// </summary>
    [Parameter]
    public TnTColor BackgroundColor { get; set; } = TnTColor.SecondaryContainer;

    /// <summary>
    ///     Gets or sets the tag content.
    /// </summary>
    [Parameter]
    public RenderFragment ChildContent { get; set; } = default!;

    /// <summary>
    ///     Gets or sets an icon rendered after the tag content.
    /// </summary>
    [Parameter]
    public TnTIcon? EndIcon { get; set; }

    /// <inheritdoc />
    public override string? ElementClass => CssClassBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddClass("nt-tag")
        .AddTextAlign(TextAlignment)
        .Build();

    /// <inheritdoc />
    public override string? ElementStyle => CssStyleBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddVariable("nt-tag-background-color", BackgroundColor)
        .AddVariable("nt-tag-text-color", TextColor)
        .Build();

    /// <summary>
    ///     Gets or sets an icon rendered before the tag content.
    /// </summary>
    [Parameter]
    public TnTIcon? StartIcon { get; set; }

    /// <summary>
    ///     Gets or sets the text alignment within the tag.
    /// </summary>
    [Parameter]
    public TextAlign? TextAlignment { get; set; } = TextAlign.Center;

    /// <summary>
    ///     Gets or sets the tag text color.
    /// </summary>
    [Parameter]
    public TnTColor TextColor { get; set; } = TnTColor.OnSecondaryContainer;
}
