using Microsoft.AspNetCore.Components;
using NTComponents.Core;

namespace NTComponents;

/// <summary>
///     Material 3 canonical feed view for responsive card and discovery surfaces.
/// </summary>
/// <remarks>
///     <para>
///         Use this view for scan-first or discovery-first surfaces such as news, photos, activity streams, product cards, search results, and other card-like collections where users browse many items.
///     </para>
///     <para>
///         Best practice: treat each direct item as a self-contained feed unit, preserve understandable reading order as columns reflow, and choose a <see cref="MinItemWidth" /> that keeps each item
///         usable before another column is introduced.
///     </para>
///     <para>
///         Do not use this view for selected-item workflows, persistent peer panes, or primary-plus-supporting context. Use <see cref="NTListDetailView" />, <see cref="NTMultiPaneView" />, or <see
///         cref="NTSupportingPaneView" /> for those patterns.
///     </para>
/// </remarks>
public partial class NTFeedView {

    /// <summary>
    ///     Feed items rendered inside the responsive grid.
    /// </summary>
    /// <remarks>Render repeated cards, tiles, articles, or summaries as direct children. Avoid children that require fixed viewport widths or depend on a specific column count.</remarks>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    ///     Minimum inline size for a feed item before the grid adds another column.
    /// </summary>
    /// <remarks>
    ///     Use a value that protects the minimum useful card width. Smaller values increase density; larger values preserve richer card content. Do not use this as a page margin or fixed container width.
    /// </remarks>
    [Parameter]
    public string MinItemWidth { get; set; } = "280px";

    /// <inheritdoc />
    protected override string ComponentClass => "nt-feed-view";

    /// <inheritdoc />
    protected override void AddComponentStyles(CssStyleBuilder builder) {
        builder.AddVariable("nt-feed-view-min-item-width", MinItemWidth);
    }
}