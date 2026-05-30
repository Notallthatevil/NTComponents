using Microsoft.AspNetCore.Components;

namespace NTComponents;

/// <summary>
///     Material 3 header region for layout-level or section-level header content.
/// </summary>
/// <remarks>
///     Defaults to <see cref="NTLayoutTag.Header" />. Set <see cref="NTComponents.Layout.NTLayoutComponentBase.Tag" /> to a neutral or sectioning tag when nesting outside a valid header context.
/// </remarks>
public partial class NTHeader {

    /// <inheritdoc />
    [Parameter]
    public override NTElevation? Elevation { get; set; } = NTElevation.Low;

    /// <summary>
    ///     Applies the sticky header layout state when this component is a direct child of <see cref="NTLayout" /> and makes the sibling <see cref="NTBody" /> the scroll container.
    /// </summary>
    [Parameter]
    public bool FixedPosition { get; set; } = true;

    /// <inheritdoc />
    protected override string ComponentClass => "nt-header";

    /// <inheritdoc />
    protected override string? ComponentStateClass => FixedPosition ? "nt-header-fixed-position" : null;

    /// <inheritdoc />
    protected override NTLayoutTag DefaultTag => NTLayoutTag.Header;

}
