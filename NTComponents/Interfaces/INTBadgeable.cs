namespace NTComponents.Interfaces;

/// <summary>
///     Represents a component that can display a Material 3 <see cref="NTBadge" /> on its icon.
/// </summary>
/// <remarks>
///     <para>
///         Implementers declare each member as a <c>[Parameter]</c>, render <see cref="NTBadge" /> only when <see cref="ShowBadge" /> is <see langword="true" />, and reference the badge from the
///         host's <c>aria-describedby</c> so assistive technology announces it alongside the host's accessible name.
///     </para>
///     <para>
///         <see cref="NTBadge" /> owns the Material 3 visuals and offsets. The host owns placement: it provides a positioned containing block and describes where its icon sits within it through the
///         <c>--nt-badge-anchor-block-start</c> and <c>--nt-badge-anchor-inline-end</c> CSS custom properties.
///     </para>
/// </remarks>
public interface INTBadgeable {

    /// <summary>
    ///     Gets or sets the accessible description announced for the badge, such as "3 unread messages".
    /// </summary>
    string? BadgeAriaLabel { get; set; }

    /// <summary>
    ///     Gets or sets the badge label. Leave empty for the Material 3 small badge; provide up to four characters, such as "3" or "999+", for the large badge.
    /// </summary>
    string? BadgeContent { get; set; }

    /// <summary>
    ///     Gets or sets whether the badge is rendered.
    /// </summary>
    bool ShowBadge { get; set; }
}
