using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using NTComponents.Core;

namespace NTComponents;

/// <summary>
///     Destination item for <see cref="NTNavigationRail" />.
/// </summary>
/// <remarks>
///     Navigation rail items are static-SSR compatible. Provide <see cref="Href" /> for route navigation. Items without <see cref="Href" /> render as native buttons so apps can opt into
///     server-backed form behavior through additional attributes, but the component does not attach Blazor click handlers or JavaScript.
/// </remarks>
public partial class NTNavigationRailItem : TnTComponentBase {

    /// <summary>
    ///     Optional active icon. When omitted for a selected Material icon, the icon is rendered with the rail's selected filled-symbol class.
    /// </summary>
    [Parameter]
    public TnTIcon? ActiveIcon { get; set; }

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

    /// <summary>
    ///     Whether the destination is disabled.
    /// </summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <inheritdoc />
    public override string? ElementClass => CssClassBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddClass("nt-navigation-rail-item")
        .AddClass("nt-navigation-rail-item-selected", _isSelected)
        .AddClass("nt-navigation-rail-item-link", RendersAnchor)
        .AddClass("nt-navigation-rail-item-button", !RendersAnchor)
        .AddDisabled(Disabled)
        .Build();

    /// <summary>
    ///     Optional native button name when the item renders as a button.
    /// </summary>
    [Parameter]
    public string? ElementName { get; set; }

    /// <inheritdoc />
    public override string? ElementStyle => CssStyleBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .Build();

    /// <summary>
    ///     Route href for static-SSR navigation.
    /// </summary>
    [Parameter]
    public string? Href { get; set; }

    /// <summary>
    ///     Icon shown for the destination.
    /// </summary>
    [Parameter]
    public TnTIcon? Icon { get; set; }

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
    private string? EffectiveAriaLabel => string.IsNullOrWhiteSpace(AriaLabel) || string.Equals(AriaLabel, Label, StringComparison.Ordinal) ? null : AriaLabel;
    private string? EffectiveElementId => ElementId ?? TryGetStringAttribute("id");
    private string? EffectiveElementLang => ElementLang ?? TryGetStringAttribute("lang");
    private string? EffectiveElementTitle => ElementTitle ?? TryGetStringAttribute("title");
    private string? EffectiveIconClass => _isSelected && EffectiveIcon is MaterialIcon ? "nt-nav-rail-selected-icon" : null;
    private TnTIcon? EffectiveIcon => _isSelected && ActiveIcon is not null
        ? GetSelectedMaterialIcon(ActiveIcon)
        : _isSelected && ActiveIconContent is not null
            ? null
            : _isSelected
                ? GetSelectedMaterialIcon(Icon)
                : Icon;
    private RenderFragment? EffectiveIconFragment => _isSelected && ActiveIconContent is not null ? ActiveIconContent : IconContent;
    private string? RenderedHref => Disabled ? null : Href;
    private string? RenderedTabIndex => Disabled && RendersAnchor ? "-1" : null;
    private string RouteMatchText => Match.ToString();
    private bool RendersAnchor => !string.IsNullOrWhiteSpace(Href);
    private string SelectedText => Selected.ToString().ToLowerInvariant();

    private bool _isSelected;
    private IReadOnlyDictionary<string, object>? _renderedAdditionalAttributes;
    private IReadOnlyDictionary<string, object>? RenderedAdditionalAttributes => _renderedAdditionalAttributes;

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

        var targetUri = NavigationManager.ToAbsoluteUri(Href);
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

    private static TnTIcon? GetSelectedMaterialIcon(TnTIcon? icon) => icon is MaterialIcon materialIcon
        ? new MaterialIcon(materialIcon.Icon) {
            Appearance = materialIcon.Appearance,
            Color = materialIcon.Color,
            Size = materialIcon.Size,
            Tooltip = materialIcon.Tooltip
        }
        : icon;

    private static string NormalizePath(string path) {
        var queryIndex = path.IndexOfAny(['?', '#']);
        if (queryIndex >= 0) {
            path = path[..queryIndex];
        }

        path = path.Trim('/');
        return path.Length == 0 ? string.Empty : path;
    }

    private string? TryGetStringAttribute(string attributeName) {
        return AdditionalAttributes?.TryGetValue(attributeName, out var value) == true
            ? Convert.ToString(value)
            : null;
    }
}
