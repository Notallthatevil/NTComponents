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
    public override string? ElementClass => CssClassBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddClass("nt-navigation-rail-group")
        .AddDisabled(Disabled)
        .Build();

    /// <inheritdoc />
    public override string? ElementStyle => CssStyleBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .Build();

    /// <summary>
    ///     Whether the group starts expanded when the rail is expanded.
    /// </summary>
    [Parameter]
    public bool Expanded { get; set; }

    /// <summary>
    ///     Icon shown in the group trigger.
    /// </summary>
    [Parameter]
    public TnTIcon? Icon { get; set; }

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

    private string EffectiveAriaLabel => string.IsNullOrWhiteSpace(AriaLabel) ? Label : AriaLabel;
    private string? EffectiveElementId => ElementId ?? TryGetStringAttribute("id");
    private string? EffectiveElementLang => ElementLang ?? TryGetStringAttribute("lang");
    private string? EffectiveElementTitle => ElementTitle ?? TryGetStringAttribute("title");
    private string DefaultExpandedText => Expanded ? "true" : "false";
    private string ExpandedText => Parent?.IsExpanded == true && Expanded ? "true" : "false";
    private string PanelAriaLabel => $"{EffectiveAriaLabel} destinations";
    private string PanelId => $"{ComponentIdentifier}-panel";

    private string? TriggerClass => CssClassBuilder.Create("nt-navigation-rail-group-trigger")
        .AddClass("nt-navigation-rail-item")
        .AddClass("nt-navigation-rail-item-button")
        .AddDisabled(Disabled)
        .Build();

    /// <inheritdoc />
    protected override void OnParametersSet() {
        base.OnParametersSet();

        if (string.IsNullOrWhiteSpace(Label)) {
            throw new ArgumentException("NTNavigationRailGroup requires a non-empty Label.", nameof(Label));
        }
    }

    private string? TryGetStringAttribute(string attributeName) {
        return AdditionalAttributes?.TryGetValue(attributeName, out var value) == true
            ? Convert.ToString(value)
            : null;
    }
}
