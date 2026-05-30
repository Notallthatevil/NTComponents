using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using NTComponents.Core;

namespace NTComponents;

/// <summary>
///     Material 3 navigation link component with button-style variants plus text-link variants for inline content.
/// </summary>
/// <remarks>
///     <para>
///         Button-style variants mirror <see cref="NTButton" /> for navigational actions that need button affordance while still using Blazor's <see cref="NavLink" /> active matching. Use <see
///         cref="NTNavLinkVariant.DefaultAnchor" /> for normal document links and <see cref="NTNavLinkVariant.InlineText" /> for inline text links that should not be underlined.
///     </para>
/// </remarks>
public partial class NTNavLink {

    /// <summary>
    ///     Gets or sets an optional override for the active link container color.
    /// </summary>
    [Parameter]
    public TnTColor? ActiveBackgroundColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for the active link content color.
    /// </summary>
    [Parameter]
    public TnTColor? ActiveTextColor { get; set; }

    /// <summary>
    ///     Gets or sets the optional autofocus attribute value.
    /// </summary>
    [Parameter]
    public bool? AutoFocus { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for button-style link container color.
    /// </summary>
    [Parameter]
    public TnTColor? BackgroundColor { get; set; }

    /// <summary>
    ///     Gets or sets the size of button-style link variants.
    /// </summary>
    [Parameter]
    public Size ButtonSize { get; set; } = Size.Small;

    /// <summary>
    ///     Gets or sets whether the link is disabled. Disabled links remove their <c>href</c> attribute, render <c>aria-disabled</c>, and leave the tab order.
    /// </summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <inheritdoc />
    public ElementReference Element { get; protected set; }

    /// <inheritdoc />
    public string? ElementClass => CssClassBuilder.Create()
        .AddClass(CssClass)
        .AddClass("nt-nav-link")
        .AddClass("nt-nav-link-button-chrome", UsesButtonChrome)
        .AddClass("nt-nav-link-elevated", Variant == NTNavLinkVariant.Elevated)
        .AddClass("nt-nav-link-filled", Variant == NTNavLinkVariant.Filled)
        .AddClass("nt-nav-link-tonal", Variant == NTNavLinkVariant.Tonal)
        .AddClass("nt-nav-link-outlined", Variant == NTNavLinkVariant.Outlined)
        .AddClass("nt-nav-link-text", Variant == NTNavLinkVariant.Text)
        .AddClass("nt-nav-link-default-anchor", Variant == NTNavLinkVariant.DefaultAnchor)
        .AddClass("nt-nav-link-inline-text", Variant == NTNavLinkVariant.InlineText)
        .AddClass("nt-nav-link-shape-round", EffectiveShape == ButtonShape.Round && UsesButtonChrome)
        .AddClass("nt-nav-link-shape-square", EffectiveShape == ButtonShape.Square && UsesButtonChrome)
        .AddElevation(Elevation, UsesButtonChrome)
        .AddSize(ButtonSize)
        .AddDisabled(Disabled)
        .AddClass("active-bg-color", ActiveBackgroundColor.HasValue)
        .AddClass("active-fg-color", ActiveTextColor.HasValue)
        .Build();

    /// <summary>
    ///     Gets or sets the optional id attribute.
    /// </summary>
    [Parameter]
    public string? ElementId { get; set; }

    /// <summary>
    ///     Gets or sets the optional lang attribute.
    /// </summary>
    [Parameter]
    public string? ElementLang { get; set; }

    /// <inheritdoc />
    public string? ElementStyle => _elementStyle;

    /// <summary>
    ///     Gets or sets the optional title attribute.
    /// </summary>
    [Parameter]
    public string? ElementTitle { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for button-style link elevation.
    /// </summary>
    [Parameter]
    public NTElevation? Elevation { get; set; }

    /// <summary>
    ///     Gets or sets whether a ripple effect should be rendered for button-style variants.
    /// </summary>
    [Parameter]
    public bool EnableRipple { get; set; } = true;

    /// <summary>
    ///     Gets or sets an optional hover color for default and inline text link variants.
    /// </summary>
    [Parameter]
    public TnTColor? HoverTextColor { get; set; }

    /// <summary>
    ///     Gets or sets the visible text label rendered by the link.
    /// </summary>
    [Parameter, EditorRequired]
    public string Label { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets an optional leading icon rendered before the label.
    /// </summary>
    [Parameter]
    public TnTIcon? LeadingIcon { get; set; }

    /// <summary>
    ///     Gets or sets the base resting shape for button-style link variants.
    /// </summary>
    [Parameter]
    public ButtonShape Shape { get; set; } = ButtonShape.Round;

    /// <summary>
    ///     Gets or sets whether click events should stop propagating.
    /// </summary>
    [Parameter]
    public bool StopPropagation { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for the link content color.
    /// </summary>
    [Parameter]
    public TnTColor? TextColor { get; set; }

    /// <summary>
    ///     Gets or sets the visual variant of the link.
    /// </summary>
    [Parameter]
    public NTNavLinkVariant Variant { get; set; } = NTNavLinkVariant.Filled;

    /// <summary>
    ///     Gets or sets an optional visited color for default and inline text link variants.
    /// </summary>
    [Parameter]
    public TnTColor? VisitedTextColor { get; set; }

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    internal string? AriaCurrent => _isActive ? "page" : null;

    internal string? AriaDisabled => Disabled ? "true" : null;

    internal string? RenderedTabIndex => Disabled ? "-1" : null;

    internal bool UsesButtonChrome => Variant is NTNavLinkVariant.Elevated or NTNavLinkVariant.Filled or NTNavLinkVariant.Tonal or NTNavLinkVariant.Outlined or NTNavLinkVariant.Text;

    internal bool UsesTextLinkChrome => Variant is NTNavLinkVariant.DefaultAnchor or NTNavLinkVariant.InlineText;

    private IReadOnlyDictionary<string, object>? RenderedAdditionalAttributes => _renderedAdditionalAttributes;

    private ButtonShape EffectiveShape => Shape;
    private bool _backgroundColorWasProvided;
    private string? _elementStyle;
    private bool _elevationWasProvided;
    private bool _isActive;
    private IReadOnlyDictionary<string, object>? _renderedAdditionalAttributes;
    private bool _textColorWasProvided;

    private static readonly IReadOnlyDictionary<string, object> _emptyAdditionalAttributes = new Dictionary<string, object>();

    /// <inheritdoc />
    public override Task SetParametersAsync(ParameterView parameters) {
        _backgroundColorWasProvided = false;
        _elevationWasProvided = false;
        _textColorWasProvided = false;

        foreach (var parameter in parameters) {
            switch (parameter.Name) {
                case nameof(BackgroundColor):
                    _backgroundColorWasProvided = true;
                    break;

                case nameof(Elevation):
                    _elevationWasProvided = true;
                    break;

                case nameof(TextColor):
                    _textColorWasProvided = true;
                    break;
            }
        }

        return base.SetParametersAsync(parameters);
    }

    /// <inheritdoc />
    protected override void OnParametersSet() {
        base.OnParametersSet();
        if (string.IsNullOrWhiteSpace(Label)) {
            throw new ArgumentException("NTNavLink requires a non-empty Label.", nameof(Label));
        }

        _renderedAdditionalAttributes = GetRenderedAdditionalAttributes();
        _isActive = IsActiveRoute();

        if (!_backgroundColorWasProvided || !BackgroundColor.HasValue) {
            BackgroundColor = GetDefaultBackgroundColor();
        }

        if (!_elevationWasProvided || !Elevation.HasValue) {
            Elevation = GetDefaultElevation();
        }

        if (!_textColorWasProvided || !TextColor.HasValue) {
            TextColor = GetDefaultTextColor();
        }

        ValidateVariantColorCombination();
        ValidateVariantElevationCombination();

        _elementStyle = BuildElementStyle();
    }

    private string? BuildElementStyle() {
        return CssStyleBuilder.Create()
            .AddFromAdditionalAttributes(AdditionalAttributes)
            .AddVariable("nt-nav-link-bg", BackgroundColor.ToCssTnTColorVariable(), BackgroundColor.HasValue)
            .AddVariable("nt-nav-link-fg", TextColor.ToCssTnTColorVariable(), TextColor.HasValue)
            .AddVariable("nt-nav-link-hover-fg", HoverTextColor.ToCssTnTColorVariable(), HoverTextColor.HasValue)
            .AddVariable("nt-nav-link-visited-fg", VisitedTextColor.ToCssTnTColorVariable(), VisitedTextColor.HasValue && UsesTextLinkChrome)
            .AddVariable("nt-nav-link-active-bg", ActiveBackgroundColor.ToCssTnTColorVariable(), ActiveBackgroundColor.HasValue)
            .AddVariable("nt-nav-link-active-fg", ActiveTextColor.ToCssTnTColorVariable(), ActiveTextColor.HasValue)
            .Build();
    }

    private IReadOnlyDictionary<string, object>? GetRenderedAdditionalAttributes() {
        if (AdditionalAttributes is null) {
            return null;
        }

        var filteredReservedAttribute = false;
        Dictionary<string, object>? filteredAttributes = null;
        foreach (var (attributeName, attributeValue) in AdditionalAttributes) {
            if (IsReservedRenderedAttribute(attributeName)) {
                filteredReservedAttribute = true;
                continue;
            }

            filteredAttributes ??= [];
            filteredAttributes[attributeName] = attributeValue;
        }

        return filteredAttributes ?? (filteredReservedAttribute ? _emptyAdditionalAttributes : AdditionalAttributes);
    }

    private bool IsReservedRenderedAttribute(string attributeName) {
        if (string.Equals(attributeName, "aria-current", StringComparison.OrdinalIgnoreCase)
            || string.Equals(attributeName, "aria-disabled", StringComparison.OrdinalIgnoreCase)) {
            return true;
        }

        return Disabled
            && (string.Equals(attributeName, "href", StringComparison.OrdinalIgnoreCase)
                || string.Equals(attributeName, "tabindex", StringComparison.OrdinalIgnoreCase));
    }

    private string? GetHrefAttribute() {
        if (AdditionalAttributes is null) {
            return null;
        }

        foreach (var (attributeName, attributeValue) in AdditionalAttributes) {
            if (string.Equals(attributeName, "href", StringComparison.OrdinalIgnoreCase)) {
                return Convert.ToString(attributeValue);
            }
        }

        return null;
    }

    private bool IsActiveRoute() {
        var href = GetHrefAttribute();
        if (string.IsNullOrWhiteSpace(href)) {
            return false;
        }

        Uri targetUri;
        try {
            targetUri = NavigationManager.ToAbsoluteUri(href);
        }
        catch (InvalidOperationException) {
            return false;
        }

        if (!IsUnderBaseUri(targetUri)) {
            return false;
        }

        var currentPath = NormalizePath(NavigationManager.ToBaseRelativePath(NavigationManager.Uri));
        var targetPath = NormalizePath(NavigationManager.ToBaseRelativePath(targetUri.AbsoluteUri));

        if (Match == NavLinkMatch.All) {
            return string.Equals(currentPath, targetPath, StringComparison.OrdinalIgnoreCase);
        }

        return currentPath.StartsWith(targetPath, StringComparison.OrdinalIgnoreCase)
            && (currentPath.Length == targetPath.Length || currentPath[targetPath.Length] == '/');
    }

    private bool IsUnderBaseUri(Uri uri) => uri.AbsoluteUri.StartsWith(NavigationManager.BaseUri, StringComparison.OrdinalIgnoreCase);

    private static string NormalizePath(string path) {
        var queryIndex = path.IndexOfAny(['?', '#']);
        if (queryIndex >= 0) {
            path = path[..queryIndex];
        }

        path = path.Trim('/');
        return path.Length == 0 ? string.Empty : path;
    }

    private TnTColor? GetDefaultBackgroundColor() {
        return Variant switch {
            NTNavLinkVariant.Elevated => TnTColor.SurfaceContainerLow,
            NTNavLinkVariant.Filled => TnTColor.Primary,
            NTNavLinkVariant.Tonal => TnTColor.SecondaryContainer,
            NTNavLinkVariant.Outlined => TnTColor.Transparent,
            NTNavLinkVariant.Text => TnTColor.Transparent,
            NTNavLinkVariant.DefaultAnchor => null,
            NTNavLinkVariant.InlineText => null,
            _ => throw new ArgumentOutOfRangeException(nameof(Variant), Variant, null)
        };
    }

    private NTElevation? GetDefaultElevation() {
        return Variant switch {
            NTNavLinkVariant.Elevated => NTElevation.Lowest,
            NTNavLinkVariant.Filled or NTNavLinkVariant.Tonal or NTNavLinkVariant.Outlined or NTNavLinkVariant.Text => NTElevation.None,
            NTNavLinkVariant.DefaultAnchor or NTNavLinkVariant.InlineText => null,
            _ => throw new ArgumentOutOfRangeException(nameof(Variant), Variant, null)
        };
    }

    private TnTColor? GetDefaultTextColor() {
        return Variant switch {
            NTNavLinkVariant.Elevated => TnTColor.Primary,
            NTNavLinkVariant.Filled => TnTColor.OnPrimary,
            NTNavLinkVariant.Tonal => TnTColor.OnSecondaryContainer,
            NTNavLinkVariant.Outlined => TnTColor.Primary,
            NTNavLinkVariant.Text => TnTColor.Primary,
            NTNavLinkVariant.DefaultAnchor => null,
            NTNavLinkVariant.InlineText => TnTColor.Primary,
            _ => throw new ArgumentOutOfRangeException(nameof(Variant), Variant, null)
        };
    }

    private void ValidateBackgroundColorForVariant() {
        if (!UsesButtonChrome) {
            return;
        }

        if (Variant is NTNavLinkVariant.Text or NTNavLinkVariant.Outlined) {
            if (BackgroundColor != TnTColor.Transparent) {
                throw new InvalidOperationException($"{Variant} navigation links must use a transparent {nameof(BackgroundColor)}.");
            }

            return;
        }

        if (BackgroundColor is TnTColor.None or TnTColor.Transparent) {
            throw new InvalidOperationException($"{Variant} navigation links must use a visible container {nameof(BackgroundColor)}.");
        }
    }

    private void ValidateVariantColorCombination() {
        if (BackgroundColor.HasValue) {
            ValidateBackgroundColorForVariant();
        }

        if (TextColor is TnTColor.None or TnTColor.Transparent) {
            throw new InvalidOperationException($"{nameof(TextColor)} must be a visible content color.");
        }

        if (HoverTextColor is TnTColor.None or TnTColor.Transparent) {
            throw new InvalidOperationException($"{nameof(HoverTextColor)} must be a visible content color.");
        }

        if (UsesTextLinkChrome && VisitedTextColor is (TnTColor.None or TnTColor.Transparent)) {
            throw new InvalidOperationException($"{nameof(VisitedTextColor)} must be a visible content color.");
        }

        if (ActiveTextColor is TnTColor.None or TnTColor.Transparent) {
            throw new InvalidOperationException($"{nameof(ActiveTextColor)} must be a visible content color.");
        }
    }

    private void ValidateVariantElevationCombination() {
        if (!Elevation.HasValue) {
            return;
        }

        if (Variant == NTNavLinkVariant.Elevated) {
            if (Elevation == NTElevation.None) {
                throw new InvalidOperationException($"{nameof(NTNavLinkVariant.Elevated)} navigation links must use a non-zero {nameof(Elevation)}.");
            }

            return;
        }

        if (UsesButtonChrome && Elevation != NTElevation.None) {
            throw new InvalidOperationException($"{Variant} navigation links must use {nameof(NTElevation.None)} {nameof(Elevation)}.");
        }
    }
}

/// <summary>
///     Defines the visual variants available for <see cref="NTNavLink" />.
/// </summary>
/// <remarks>
///     Use the lowest-emphasis variant that still makes the destination clear. Button-style variants are best for prominent navigational actions, while text-link variants are best for ordinary
///     document or inline links where users expect browser-like anchor behavior.
/// </remarks>
public enum NTNavLinkVariant {

    /// <summary>
    ///     Elevated navigation link with a low surface container and shadow, best for navigational actions that need separation from the surrounding surface.
    /// </summary>
    /// <remarks>
    ///     Use sparingly for destination links that sit on visually busy or same-color surfaces. Prefer <see cref="Filled" />, <see cref="Tonal" />, or <see cref="Outlined" /> when elevation is not
    ///     needed. Elevated links should keep a visible container color and non-zero elevation.
    /// </remarks>
    Elevated,

    /// <summary>
    ///     Filled navigation link for the highest-emphasis navigational action in a region.
    /// </summary>
    /// <remarks>
    ///     Use for the primary destination or main route change when the link should read like a call to action. Avoid using several filled links in the same group; too many high-emphasis links make
    ///     the navigation hierarchy harder to scan.
    /// </remarks>
    Filled,

    /// <summary>
    ///     Tonal navigation link using the secondary color container mapping for important, lower-emphasis navigation.
    /// </summary>
    /// <remarks>
    ///     Use for secondary destinations that should still look button-like but should not compete with a filled primary action. Tonal is usually the best default for repeated prominent links in a
    ///     panel, toolbar, or card action area.
    /// </remarks>
    Tonal,

    /// <summary>
    ///     Outlined navigation link for medium-emphasis navigation actions that need a clear boundary without a filled container.
    /// </summary>
    /// <remarks>
    ///     Use when the destination needs to stand apart from surrounding text or controls, but a filled or tonal container would be too strong. Outlined links should keep a transparent background
    ///     and visible content color.
    /// </remarks>
    Outlined,

    /// <summary>
    ///     Text navigation link with button-like sizing and state layer for low-emphasis navigational actions.
    /// </summary>
    /// <remarks>
    ///     Use for compact command-like navigation in button groups, toolbars, cards, dialogs, or dense layouts. Do not use for links embedded in sentence text; use <see cref="InlineText" /> for
    ///     that. Text links should keep a transparent background.
    /// </remarks>
    Text,

    /// <summary>
    ///     Default document anchor styling with underline and browser-like link behavior.
    /// </summary>
    /// <remarks>
    ///     Use for standard page content links, documentation links, and destinations where users should recognize a normal anchor immediately. This variant is the safest choice for body content and
    ///     should preserve visited-link styling when a visited color is provided.
    /// </remarks>
    DefaultAnchor,

    /// <summary>
    ///     Inline text link styling without underline for links inside paragraphs, spans, or other text flows.
    /// </summary>
    /// <remarks>
    ///     Use when a link must sit inside running text without the default underline. Keep the surrounding copy clear enough that the link remains discoverable, and prefer <see cref="DefaultAnchor"
    ///     /> when underline affordance is important for accessibility or dense content.
    /// </remarks>
    InlineText
}
