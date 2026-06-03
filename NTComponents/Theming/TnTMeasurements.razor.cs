using Microsoft.AspNetCore.Components;

namespace NTComponents;

/// <summary>
///     Represents the theme design component for TnT.
/// </summary>
[Obsolete("TnTMeasurements is obsolete. Use NTHeadDependencies instead.")]
public partial class TnTMeasurements
{
    /// <summary>
    /// Gets or sets the footer height.
    /// </summary>
    [Parameter]
    public double FooterHeight { get; set; } = 64;

    /// <summary>
    /// Gets or sets the header height.
    /// </summary>
    [Parameter]
    public double HeaderHeight { get; set; } = 64;

    /// <summary>
    /// Gets or sets the side navigation width.
    /// </summary>
    [Parameter]
    public double SideNavWidth { get; set; } = 256;

    /// <summary>
    /// Gets or sets the selector where NT measurement and typography tokens are emitted.
    /// </summary>
    [Parameter]
    public string TokenScopeSelector { get; set; } = ":root";

    private string TokenScope => NormalizeTokenScopeSelector(TokenScopeSelector);

    internal static string NormalizeTokenScopeSelector(string? tokenScopeSelector) => string.IsNullOrWhiteSpace(tokenScopeSelector)
        ? ":root"
        : tokenScopeSelector.Trim();
}
