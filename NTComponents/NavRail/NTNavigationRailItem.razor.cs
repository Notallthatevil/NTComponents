using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using NTComponents.Core;
using NTComponents.Interfaces;

using NTComponents.CodeDocumentation;
namespace NTComponents;

/// <summary>
///     Destination item for <see cref="NTNavigationRail" />.
/// </summary>
/// <remarks>
///     Navigation rail items are static-SSR compatible. Provide <see cref="Href" /> for route navigation. Items without <see cref="Href" /> render as native buttons so apps can opt into
///     server-backed form behavior through additional attributes, but the component does not attach Blazor click handlers or JavaScript.
/// </remarks>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.SsrCompatible,
    CompatibilitySummary = "Renders native navigation links or buttons for static SSR.",
    CompatibilityDetails = "Href-backed items navigate as ordinary anchors and button items can participate in native forms through attributes. Route-selected state updates require a new render.")]
public partial class NTNavigationRailItem : NTComponentBase, INTBadgeable {

    /// <summary>
    ///     Optional active icon. When omitted for a selected Material icon, the icon is rendered with the rail's selected filled-symbol class.
    /// </summary>
    [Parameter]
    public NTIcon? ActiveIcon { get; set; }

    /// <summary>
    ///     Optional active icon fragment. Used only when <see cref="ActiveIcon" /> is not supplied.
    /// </summary>
    [Parameter]
    public RenderFragment? ActiveIconContent { get; set; }

    /// <summary>
    ///     Accessible label override. Omit when <see cref="Label" /> is already specific enough.
    /// </summary>
    [Parameter]
    public string? AriaLabel { get; set; }

    /// <inheritdoc />
    [Parameter]
    public string? BadgeAriaLabel { get; set; }

    /// <inheritdoc />
    [Parameter]
    public string? BadgeContent { get; set; }

    /// <summary>
    ///     Whether the destination is disabled.
    /// </summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <inheritdoc />
    public override string? ElementClass => _elementClass;

    /// <summary>
    ///     Optional native button name when the item renders as a button.
    /// </summary>
    [Parameter]
    public string? ElementName { get; set; }

    /// <inheritdoc />
    public override string? ElementStyle => _elementStyle;

    /// <summary>
    ///     Route href for static-SSR navigation.
    /// </summary>
    [Parameter]
    public string? Href { get; set; }

    /// <summary>
    ///     Icon shown for the destination.
    /// </summary>
    [Parameter]
    public NTIcon? Icon { get; set; }

    /// <summary>
    ///     Icon fragment shown when <see cref="Icon" /> is not supplied.
    /// </summary>
    [Parameter]
    public RenderFragment? IconContent { get; set; }

    /// <summary>
    ///     Visible destination label.
    /// </summary>
    [Parameter, EditorRequired]
    public string Label { get; set; } = string.Empty;

    /// <summary>
    ///     Route match behavior used when the parent rail enables route matching.
    /// </summary>
    [Parameter]
    public NavLinkMatch Match { get; set; }

    /// <summary>
    ///     Explicit selected state. Use sparingly when selection is not route- or value-driven.
    /// </summary>
    [Parameter]
    public bool Selected { get; set; }

    /// <inheritdoc />
    /// <remarks>The badge is anchored to the top-end corner of the item icon using Material 3 offsets.</remarks>
    [Parameter]
    public bool ShowBadge { get; set; }

    /// <summary>
    ///     Native button type when the item renders as a button.
    /// </summary>
    [Parameter]
    public ButtonType Type { get; set; } = ButtonType.Button;

    /// <summary>
    ///     Native button value when the item renders as a button.
    /// </summary>
    [Parameter]
    public string? Value { get; set; }

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    private string? AriaCurrent => _isSelected && !Disabled ? "page" : null;
    private string? AriaDisabled => Disabled && RendersAnchor ? "true" : null;
    private string BadgeId => $"{ComponentIdentifier}-badge";
    private string? EffectiveIconClass => _isSelected && _effectiveIcon is MaterialIcon ? "nt-nav-rail-selected-icon" : null;
    private RenderFragment? EffectiveIconFragment => _isSelected && ActiveIconContent is not null ? ActiveIconContent : IconContent;
    private string? RenderedHref => Disabled ? null : Href;
    private string? RenderedTabIndex => Disabled && RendersAnchor ? "-1" : null;
    private string RouteMatchText => Match.ToString();
    private bool RendersAnchor => !string.IsNullOrWhiteSpace(Href);
    private string SelectedText => Selected ? "true" : "false";

    private string? _ariaDescribedBy;
    private string? _effectiveAriaLabel;
    private string? _effectiveElementId;
    private string? _effectiveElementLang;
    private string? _effectiveElementTitle;
    private NTIcon? _effectiveIcon;
    private string? _elementClass;
    private string? _elementStyle;
    private bool _isSelected;
    private IReadOnlyDictionary<string, object>? _renderedAdditionalAttributes;

    private static readonly HashSet<string> _reservedAttributeNames = new(StringComparer.OrdinalIgnoreCase) {
        "aria-current",
        "aria-disabled",
        "aria-label",
        "class",
        "disabled",
        "data-nt-navigation-rail-match",
        "data-nt-navigation-rail-selected",
        "href",
        "id",
        "lang",
        "name",
        "style",
        "tabindex",
        "title",
        "type",
        "value"
    };

    /// <inheritdoc />
    protected override void OnParametersSet() {
        base.OnParametersSet();

        if (string.IsNullOrWhiteSpace(Label)) {
            throw new ArgumentException("NTNavigationRailItem requires a non-empty Label.", nameof(Label));
        }

        _renderedAdditionalAttributes = FilterAdditionalAttributes();
        _isSelected = Selected || IsSelectedByRoute();

        _effectiveAriaLabel = string.IsNullOrWhiteSpace(AriaLabel) || string.Equals(AriaLabel, Label, StringComparison.Ordinal) ? null : AriaLabel;
        _effectiveElementId = ElementId ?? TryGetStringAttribute("id");
        _effectiveElementLang = ElementLang ?? TryGetStringAttribute("lang");
        _effectiveElementTitle = ElementTitle ?? TryGetStringAttribute("title");
        _effectiveIcon = _isSelected && ActiveIcon is not null
            ? GetSelectedMaterialIcon(ActiveIcon)
            : _isSelected && ActiveIconContent is not null
                ? null
                : _isSelected
                    ? GetSelectedMaterialIcon(Icon)
                    : Icon;
        _ariaDescribedBy = NTBadge.GetDescribedBy(_renderedAdditionalAttributes?.GetValueOrDefault("aria-describedby"), ShowBadge && _effectiveAriaLabel is not null, ShowBadge ? BadgeId : string.Empty);
        _elementClass = CssClassBuilder.Create()
            .AddFromAdditionalAttributes(AdditionalAttributes)
            .AddClass("nt-navigation-rail-item")
            .AddClass("nt-navigation-rail-item-selected", _isSelected)
            .AddClass("nt-navigation-rail-item-link", RendersAnchor)
            .AddClass("nt-navigation-rail-item-button", !RendersAnchor)
            .AddDisabled(Disabled)
            .Build();
        _elementStyle = CssStyleBuilder.Create()
            .AddFromAdditionalAttributes(AdditionalAttributes)
            .Build();
    }

    private IReadOnlyDictionary<string, object>? FilterAdditionalAttributes() {
        if (AdditionalAttributes is null) {
            return null;
        }

        Dictionary<string, object>? filteredAttributes = null;

        foreach (var (attributeName, attributeValue) in AdditionalAttributes) {
            if (_reservedAttributeNames.Contains(attributeName)) {
                continue;
            }

            filteredAttributes ??= [];
            filteredAttributes[attributeName] = attributeValue;
        }

        return filteredAttributes;
    }

    private bool IsSelectedByRoute() {
        if (!RendersAnchor || Disabled || string.IsNullOrWhiteSpace(Href)) {
            return false;
        }

        return NTNavLink.IsActive(NavigationManager, NavigationManager.ToAbsoluteUri(Href).AbsoluteUri, Match);
    }

    private static NTIcon? GetSelectedMaterialIcon(NTIcon? icon) => icon is MaterialIcon materialIcon
        ? new MaterialIcon(materialIcon.Icon) {
            Appearance = materialIcon.Appearance,
            Color = materialIcon.Color,
            Size = materialIcon.Size,
            Tooltip = materialIcon.Tooltip
        }
        : icon;

    private string? TryGetStringAttribute(string attributeName) {
        return AdditionalAttributes?.TryGetValue(attributeName, out var value) == true
            ? Convert.ToString(value)
            : null;
    }
}
