using Microsoft.AspNetCore.Components;

namespace NTComponents;

/// <summary>
///     Material 3 body region for page or nested layout content inside an <see cref="NTLayout" />.
/// </summary>
/// <remarks>
///     Defaults to <see cref="NTLayoutTag.Main" />. Set <see cref="NTComponents.Layout.NTLayoutComponentBase.Tag" /> to a neutral or sectioning tag when nesting.
/// </remarks>
public partial class NTBody {

    /// <summary>
    ///     Enables rounded corners on the body scroll container.
    /// </summary>
    [Parameter]
    public bool RoundedCorners { get; set; } = true;

    /// <inheritdoc />
    protected override string ComponentClass => "nt-body";

    /// <inheritdoc />
    protected override string? ComponentStateClass => RoundedCorners ? "nt-body-rounded" : null;

    /// <inheritdoc />
    protected override NTLayoutTag DefaultTag => NTLayoutTag.Main;

}
