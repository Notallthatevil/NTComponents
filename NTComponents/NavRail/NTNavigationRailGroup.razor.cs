using Microsoft.AspNetCore.Components;
using NTComponents.Core;

using NTComponents.CodeDocumentation;
namespace NTComponents;

/// <summary>
///     Toggleable group for <see cref="NTNavigationRail" /> destinations.
/// </summary>
/// <remarks>
///     Groups render as temporary popover surfaces while the rail is collapsed. When the rail is expanded, the same content becomes an inline disclosure region that can contain destinations or
///     additional nested groups.
/// </remarks>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.InteractiveRequired,
    CompatibilitySummary = "Requires navigation-rail browser enhancement for group expansion.",
    CompatibilityDetails = "The group content is rendered, but the trigger does not have a complete native fallback target. Expanded inline groups, collapsed-rail popovers, and nested group behavior depend on NTNavigationRail script.")]
public partial class NTNavigationRailGroup : NTComponentBase {

    /// <summary>
    ///     Accessible label override. Omit when <see cref="Label" /> already describes the group.
    /// </summary>
    [Parameter]
    public string? AriaLabel { get; set; }

    /// <summary>
    ///     Nested navigation content, usually <see cref="NTNavigationRailItem" /> or <see cref="NTNavigationRailGroup" /> children.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    ///     Whether the group trigger is disabled.
    /// </summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <inheritdoc />
    public override string? ElementClass => _elementClass;

    /// <inheritdoc />
    public override string? ElementStyle => _elementStyle;

    /// <summary>
    ///     Whether the group starts expanded when the rail is expanded.
    /// </summary>
    [Parameter]
    public bool Expanded { get; set; }

    /// <summary>
    ///     Icon shown in the group trigger.
    /// </summary>
    [Parameter]
    public NTIcon? Icon { get; set; }

    /// <summary>
    ///     Icon fragment shown when <see cref="Icon" /> is not supplied.
    /// </summary>
    [Parameter]
    public RenderFragment? IconContent { get; set; }

    /// <summary>
    ///     Visible group label.
    /// </summary>
    [Parameter, EditorRequired]
    public string Label { get; set; } = string.Empty;

    [CascadingParameter]
    private NTNavigationRail? Parent { get; set; }

    private string DefaultExpandedText => Expanded ? "true" : "false";
    private string ExpandedText => Parent?.IsExpanded == true && Expanded ? "true" : "false";

    private string _effectiveAriaLabel = string.Empty;
    private string? _effectiveElementId;
    private string? _effectiveElementLang;
    private string? _effectiveElementTitle;
    private string? _elementClass;
    private string? _elementStyle;
    private string _panelAriaLabel = string.Empty;
    private string? _triggerClass;

    private string PanelId => $"{ComponentIdentifier}-panel";

    /// <inheritdoc />
    protected override void OnParametersSet() {
        base.OnParametersSet();

        if (string.IsNullOrWhiteSpace(Label)) {
            throw new ArgumentException("NTNavigationRailGroup requires a non-empty Label.", nameof(Label));
        }

        _effectiveAriaLabel = string.IsNullOrWhiteSpace(AriaLabel) ? Label : AriaLabel;
        _effectiveElementId = ElementId ?? TryGetStringAttribute("id");
        _effectiveElementLang = ElementLang ?? TryGetStringAttribute("lang");
        _effectiveElementTitle = ElementTitle ?? TryGetStringAttribute("title");
        _panelAriaLabel = $"{_effectiveAriaLabel} destinations";
        _elementClass = CssClassBuilder.Create()
            .AddFromAdditionalAttributes(AdditionalAttributes)
            .AddClass("nt-navigation-rail-group")
            .AddDisabled(Disabled)
            .Build();
        _elementStyle = CssStyleBuilder.Create()
            .AddFromAdditionalAttributes(AdditionalAttributes)
            .Build();
        _triggerClass = CssClassBuilder.Create("nt-navigation-rail-group-trigger")
            .AddClass("nt-navigation-rail-item")
            .AddClass("nt-navigation-rail-item-button")
            .AddDisabled(Disabled)
            .Build();
    }

    private string? TryGetStringAttribute(string attributeName) {
        return AdditionalAttributes?.TryGetValue(attributeName, out var value) == true
            ? Convert.ToString(value)
            : null;
    }
}
