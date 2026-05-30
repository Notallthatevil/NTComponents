using Microsoft.AspNetCore.Components;
using NTComponents.Core;

namespace NTComponents;

/// <summary>
///     Represents a Material 3 divider that groups content in lists, cards, layouts, and other containers.
/// </summary>
/// <remarks>
///     <para>Use dividers to group related content and reinforce hierarchy. Prefer open space when spacing alone can communicate the relationship.</para>
///     <para>
///         Best practices: keep dividers visible but not bold, use them sparingly, and choose <see cref="NTDividerVariant.Inset" /> or <see cref="NTDividerVariant.MiddleInset" /> only when the
///         divider is separating related items within a section. Use <see cref="LayoutDirection.Vertical" /> for larger-screen side-by-side layouts.
///     </para>
/// </remarks>
public partial class NTDivider {

    /// <summary>
    ///     Gets or sets the divider color.
    /// </summary>
    /// <remarks>Material 3 uses <see cref="TnTColor.OutlineVariant" /> for the divider color role. Override only when the divider needs to follow a surrounding component contract.</remarks>
    [Parameter]
    public TnTColor? Color { get; set; } = TnTColor.OutlineVariant;

    /// <summary>
    ///     Gets or sets the divider orientation.
    /// </summary>
    /// <remarks>Use horizontal dividers for lists, cards, and stacked content. Use vertical dividers for side-by-side content on larger screens.</remarks>
    [Parameter]
    public LayoutDirection Direction { get; set; } = LayoutDirection.Horizontal;

    /// <inheritdoc />
    public override string? ElementClass => CssClassBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddClass("nt-divider")
        .AddClass($"nt-divider-{Direction.ToCssString()}")
        .AddClass($"nt-divider-{Variant.ToCssClass()}", Direction == LayoutDirection.Horizontal)
        .Build();

    /// <inheritdoc />
    public override string? ElementStyle => CssStyleBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddVariable("nt-divider-color", Color.GetValueOrDefault().ToCssTnTColorVariable(), Color.HasValue)
        .Build();

    /// <summary>
    ///     Gets or sets the horizontal divider layout.
    /// </summary>
    /// <remarks>
    ///     <see cref="NTDividerVariant.FullWidth" /> separates larger sections. <see cref="NTDividerVariant.Inset" /> and <see cref="NTDividerVariant.MiddleInset" /> separate related nested content
    ///     and align with leading content such as icons or avatars.
    /// </remarks>
    [Parameter]
    public NTDividerVariant Variant { get; set; } = NTDividerVariant.FullWidth;

    private string AriaOrientation => Direction.ToCssString();
}

/// <summary>
///     Defines Material 3 horizontal divider layouts.
/// </summary>
public enum NTDividerVariant {

    /// <summary>
    ///     Full-width divider for separating larger sections or interactive and non-interactive areas.
    /// </summary>
    FullWidth,

    /// <summary>
    ///     Inset divider with a 16dp leading inset and no trailing inset.
    /// </summary>
    Inset,

    /// <summary>
    ///     Middle-inset divider with 16dp leading and trailing insets.
    /// </summary>
    MiddleInset
}

internal static class NTDividerVariantExtensions {

    public static string ToCssClass(this NTDividerVariant variant) {
        return variant switch {
            NTDividerVariant.FullWidth => "full-width",
            NTDividerVariant.Inset => "inset",
            NTDividerVariant.MiddleInset => "middle-inset",
            _ => throw new InvalidOperationException($"{variant} is not a valid value of {nameof(NTDividerVariant)}")
        };
    }
}