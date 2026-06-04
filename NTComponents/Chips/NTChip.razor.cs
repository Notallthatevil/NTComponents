using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using NTComponents.Core;

using NTComponents.CodeDocumentation;
namespace NTComponents;

/// <summary>
///     Material 3 chip component for compact actions, filters, user-entered values, and suggestions.
/// </summary>
/// <remarks>
///     Chips are compact controls that help people complete the current task faster. Use <see cref="NTChipVariant.Assist" /> for contextual actions, <see cref="NTChipVariant.Filter" /> for collection
///     filters, <see cref="NTChipVariant.Input" /> for user-entered values, and <see cref="NTChipVariant.Suggestion" /> for product-authored suggestions. Keep labels short, prefer 20 characters or
///     fewer where practical, and keep at least 8 CSS pixels between chips. The visible Material 3 chip container is 32dp high with an 8dp corner radius, 18dp icons, 16dp horizontal padding without
///     icons, 8dp padding when icons are present, and 8dp between content elements. The component keeps a 48 by 48 CSS pixel interaction target around the visible container for accessibility.
///     Selectable chips render native checkboxes, action chips render native buttons, and link chips render anchors, so static server rendering still produces meaningful HTML without JavaScript.
///     Filter chips can render a menu through <see cref="MenuContent" />; this uses native popover targeting with <see cref="NTMenu" /> so the chip can open a dropdown from static server-rendered
///     markup. See <see cref="NTChipVariant" /> for variant-specific usage guidance and best practices.
/// </remarks>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.ProgressivelyEnhanced,
    CompatibilitySummary = "Renders native chip buttons, anchors, checkboxes, and popover triggers in static SSR.",
    CompatibilityDetails = "Static SSR preserves link, button, checkbox, and menu-trigger markup. Blazor callbacks, selected-state binding, removal callbacks, and menu synchronization require interactive enhancement.")]
public partial class NTChip : NTComponentBase {
    private bool _appearanceWasProvided;
    private bool _backgroundColorWasProvided;
    private bool _disabledBackgroundColorWasProvided;
    private bool _disabledIconColorWasProvided;
    private bool _disabledOutlineColorWasProvided;
    private bool _disabledTextColorWasProvided;
    private bool _focusOutlineColorWasProvided;
    private bool _iconColorWasProvided;
    private bool _outlineColorWasProvided;
    private bool _selectableWasProvided;
    private bool _selectedBackgroundColorWasProvided;
    private bool _selectedIconColorWasProvided;
    private bool _selectedOutlineColorWasProvided;
    private bool _selectedWasProvided;
    private bool _selectedStateLayerColorWasProvided;
    private bool _selectedTextColorWasProvided;
    private bool _stateLayerColorWasProvided;
    private bool _textColorWasProvided;
    private bool _selected;

    /// <summary>
    ///     Gets or sets the chip container treatment.
    /// </summary>
    [Parameter]
    public NTChipAppearance Appearance { get; set; } = NTChipAppearance.Outlined;

    /// <summary>
    ///     Gets or sets an accessible label override for icon-only or context-dependent chips.
    /// </summary>
    [Parameter]
    public string? AriaLabel { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for the unselected chip container color.
    /// </summary>
    [Parameter]
    public TnTColor? BackgroundColor { get; set; }

    /// <summary>
    ///     Gets or sets whether the chip is disabled.
    /// </summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for the disabled chip container color.
    /// </summary>
    [Parameter]
    public TnTColor? DisabledBackgroundColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for disabled leading, trailing, and remove icon color.
    /// </summary>
    [Parameter]
    public TnTColor? DisabledIconColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for the disabled outlined chip border color.
    /// </summary>
    [Parameter]
    public TnTColor? DisabledOutlineColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for disabled chip label color.
    /// </summary>
    [Parameter]
    public TnTColor? DisabledTextColor { get; set; }

    /// <inheritdoc />
    public override string? ElementClass => CssClassBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddClass("nt-chip")
        .AddClass("nt-chip-assist", Variant == NTChipVariant.Assist)
        .AddClass("nt-chip-filter", Variant == NTChipVariant.Filter)
        .AddClass("nt-chip-input", Variant == NTChipVariant.Input)
        .AddClass("nt-chip-suggestion", Variant == NTChipVariant.Suggestion)
        .AddClass("nt-chip-outlined", EffectiveAppearance == NTChipAppearance.Outlined)
        .AddClass("nt-chip-elevated", EffectiveAppearance == NTChipAppearance.Elevated)
        .AddClass("nt-chip-selected", CurrentSelected)
        .AddClass("nt-chip-selectable", EffectiveSelectable)
        .AddClass("nt-chip-with-menu", HasMenu)
        .AddClass("nt-chip-removable", ShowRemoveButton)
        .AddClass("nt-chip-two-action", IsTwoAction)
        .AddClass("nt-chip-with-leading", EffectiveLeadingIcon is not null)
        .AddClass("nt-chip-with-selected-leading", IsSelectedLeadingIcon)
        .AddClass("nt-chip-with-trailing", EffectiveTrailingIcon is not null)
        .AddClass("nt-chip-with-link", !string.IsNullOrWhiteSpace(Href))
        .AddDisabled(Disabled)
        .AddCornerRadius(NTCornerRadius.Small)
        .Build();

    /// <summary>
    ///     Gets or sets the optional form field name used by selectable chips and button chips.
    /// </summary>
    [Parameter]
    public string? ElementName { get; set; }

    /// <inheritdoc />
    public override string? ElementStyle => CssStyleBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddVariable("nt-chip-bg", BackgroundColor.ToCssTnTColorVariable(), _backgroundColorWasProvided && BackgroundColor.HasValue)
        .AddVariable("nt-chip-fg", TextColor.ToCssTnTColorVariable(), _textColorWasProvided && TextColor.HasValue)
        .AddVariable("nt-chip-icon", IconColor.ToCssTnTColorVariable(), _iconColorWasProvided && IconColor.HasValue)
        .AddVariable("nt-chip-outline", OutlineColor.ToCssTnTColorVariable(), _outlineColorWasProvided && OutlineColor.HasValue)
        .AddVariable("nt-chip-selected-bg", SelectedBackgroundColor.ToCssTnTColorVariable(), _selectedBackgroundColorWasProvided && SelectedBackgroundColor.HasValue)
        .AddVariable("nt-chip-selected-fg", SelectedTextColor.ToCssTnTColorVariable(), _selectedTextColorWasProvided && SelectedTextColor.HasValue)
        .AddVariable("nt-chip-selected-icon", SelectedIconColor.ToCssTnTColorVariable(), _selectedIconColorWasProvided && SelectedIconColor.HasValue)
        .AddVariable("nt-chip-selected-outline", SelectedOutlineColor.ToCssTnTColorVariable(), _selectedOutlineColorWasProvided && SelectedOutlineColor.HasValue)
        .AddVariable("nt-chip-state-layer", StateLayerColor.ToCssTnTColorVariable(), _stateLayerColorWasProvided && StateLayerColor.HasValue)
        .AddVariable("nt-chip-selected-state-layer", SelectedStateLayerColor.ToCssTnTColorVariable(), _selectedStateLayerColorWasProvided && SelectedStateLayerColor.HasValue)
        .AddVariable("nt-chip-focus-outline", FocusOutlineColor.ToCssTnTColorVariable(), _focusOutlineColorWasProvided && FocusOutlineColor.HasValue)
        .AddVariable("nt-chip-disabled-bg", DisabledBackgroundColor.ToCssTnTColorVariable(), _disabledBackgroundColorWasProvided && DisabledBackgroundColor.HasValue)
        .AddVariable("nt-chip-disabled-fg", DisabledTextColor.ToCssTnTColorVariable(), _disabledTextColorWasProvided && DisabledTextColor.HasValue)
        .AddVariable("nt-chip-disabled-icon", DisabledIconColor.ToCssTnTColorVariable(), _disabledIconColorWasProvided && DisabledIconColor.HasValue)
        .AddVariable("nt-chip-disabled-outline", DisabledOutlineColor.ToCssTnTColorVariable(), _disabledOutlineColorWasProvided && DisabledOutlineColor.HasValue)
        .Build();

    /// <summary>
    ///     Gets or sets an optional override for the chip elevation.
    /// </summary>
    [Parameter]
    public NTElevation? Elevation { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for the focused chip outline color.
    /// </summary>
    [Parameter]
    public TnTColor? FocusOutlineColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional URL. When provided, non-selectable chips render as anchors.
    /// </summary>
    [Parameter]
    public string? Href { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for leading, trailing, and remove icon color.
    /// </summary>
    [Parameter]
    public TnTColor? IconColor { get; set; }

    /// <summary>
    ///     Gets or sets the visible text label.
    /// </summary>
    [Parameter, EditorRequired]
    public string Label { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets an optional leading icon or avatar.
    /// </summary>
    [Parameter]
    public TnTIcon? LeadingIcon { get; set; }

    /// <summary>
    ///     Gets or sets whether menu item clicks should close the dropdown menu.
    /// </summary>
    [Parameter]
    public bool CloseOnMenuContentClick { get; set; } = true;

    /// <summary>
    ///     Gets or sets the visual density used by a chip dropdown menu.
    /// </summary>
    [Parameter]
    public NTMenuAppearance MenuAppearance { get; set; } = NTMenuAppearance.Standard;

    /// <summary>
    ///     Gets or sets an accessible label for the dropdown menu. Defaults to <c>{Label} options</c>.
    /// </summary>
    [Parameter]
    public string? MenuAriaLabel { get; set; }

    /// <summary>
    ///     Gets or sets the dropdown content rendered in an anchored <see cref="NTMenu" />.
    /// </summary>
    /// <remarks>
    ///     Use this for filter chips whose value is chosen from a dropdown list. The chip renders as a native popover trigger and no longer renders a checkbox; update <see cref="Selected" /> or the
    ///     label from the selected menu item when the menu choice should be reflected on the chip.
    /// </remarks>
    [Parameter]
    public RenderFragment? MenuContent { get; set; }

    /// <summary>
    ///     Gets or sets the click callback for action and link chips.
    /// </summary>
    [Parameter]
    public EventCallback<MouseEventArgs> OnClickCallback { get; set; }

    /// <summary>
    ///     Gets or sets the callback invoked when the remove affordance is activated.
    /// </summary>
    [Parameter]
    public EventCallback<MouseEventArgs> OnRemoveCallback { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for the outlined chip border color.
    /// </summary>
    [Parameter]
    public TnTColor? OutlineColor { get; set; }

    /// <summary>
    ///     Gets or sets whether the chip can be removed.
    /// </summary>
    [Parameter]
    public bool Removable { get; set; }

    /// <summary>
    ///     Gets or sets the remove button accessible label. Defaults to <c>Remove {Label}</c>.
    /// </summary>
    [Parameter]
    public string? RemoveAriaLabel { get; set; }

    /// <summary>
    ///     Gets or sets the anchor relationship attribute when <see cref="Href" /> is provided.
    /// </summary>
    [Parameter]
    public string? Rel { get; set; }

    /// <summary>
    ///     Gets or sets whether the chip is selected.
    /// </summary>
    [Parameter]
    public bool Selected { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for the selected chip container color.
    /// </summary>
    [Parameter]
    public TnTColor? SelectedBackgroundColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for selected leading, trailing, and remove icon color.
    /// </summary>
    [Parameter]
    public TnTColor? SelectedIconColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for the selected outlined chip border color.
    /// </summary>
    [Parameter]
    public TnTColor? SelectedOutlineColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for the selected hover, focus, and pressed state layer color.
    /// </summary>
    [Parameter]
    public TnTColor? SelectedStateLayerColor { get; set; }

    /// <summary>
    ///     Gets or sets the callback invoked when <see cref="Selected" /> changes.
    /// </summary>
    [Parameter]
    public EventCallback<bool> SelectedChanged { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for the selected chip content color.
    /// </summary>
    [Parameter]
    public TnTColor? SelectedTextColor { get; set; }

    /// <summary>
    ///     Gets or sets whether the chip is selectable. Filter chips are selectable by default.
    /// </summary>
    [Parameter]
    public bool? Selectable { get; set; }

    /// <summary>
    ///     Gets or sets whether selected filter chips render the leading check icon.
    /// </summary>
    [Parameter]
    public bool ShowSelectedIcon { get; set; } = true;

    /// <summary>
    ///     Gets or sets an optional override for the hover, focus, and pressed state layer color.
    /// </summary>
    [Parameter]
    public TnTColor? StateLayerColor { get; set; }

    /// <summary>
    ///     Gets or sets the anchor target when <see cref="Href" /> is provided.
    /// </summary>
    [Parameter]
    public string? Target { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for the unselected chip content color.
    /// </summary>
    [Parameter]
    public TnTColor? TextColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional trailing icon.
    /// </summary>
    [Parameter]
    public TnTIcon? TrailingIcon { get; set; }

    /// <summary>
    ///     Gets or sets the native button type for action chips.
    /// </summary>
    [Parameter]
    public ButtonType Type { get; set; }

    /// <summary>
    ///     Gets or sets the Material 3 chip role.
    /// </summary>
    /// <remarks>
    ///     Choose the variant by intent, not by visual style. Use <see cref="NTChipVariant.Assist" /> for contextual actions, <see cref="NTChipVariant.Filter" /> for selectable filtering criteria,
    ///     <see cref="NTChipVariant.Input" /> for user-entered values that may be removed, and <see cref="NTChipVariant.Suggestion" /> for product-authored suggestions.
    /// </remarks>
    [Parameter]
    public NTChipVariant Variant { get; set; } = NTChipVariant.Assist;

    private EventCallback<MouseEventArgs> ActionClickCallback => OnClickCallback.HasDelegate ? EventCallback.Factory.Create<MouseEventArgs>(this, HandleClickAsync) : default;
    private string? AriaDisabled => Disabled ? "true" : null;
    private string? ContainerClass => CssClassBuilder.Create("nt-chip-container")
        .AddElevation(EffectiveElevation)
        .Build();
    private string? DisabledTabIndex => Disabled ? "-1" : null;
    private NTChipAppearance EffectiveAppearance => !_appearanceWasProvided && Variant == NTChipVariant.Suggestion ? NTChipAppearance.Elevated : Appearance;
    private NTElevation EffectiveElevation => Elevation ?? (EffectiveAppearance == NTChipAppearance.Elevated ? NTElevation.Lowest : NTElevation.None);
    private string? EffectiveHref => Disabled ? null : Href;
    private string EffectiveMenuAriaLabel => string.IsNullOrWhiteSpace(MenuAriaLabel) ? $"{Label} options" : MenuAriaLabel;
    private TnTIcon? EffectiveLeadingIcon => IsSelectedLeadingIcon ? MaterialIcon.Check : LeadingIcon;
    private TnTIcon? EffectiveTrailingIcon => HasMenu && TrailingIcon is null ? MaterialIcon.ArrowDropDown : TrailingIcon;
    private string EffectiveRemoveAriaLabel => string.IsNullOrWhiteSpace(RemoveAriaLabel) ? $"Remove {Label}" : RemoveAriaLabel;
    private bool EffectiveSelectable => !HasMenu && (Selectable ?? Variant == NTChipVariant.Filter);
    private bool CurrentSelected => _selectedWasProvided ? Selected : _selected;
    private bool HasPrimaryAction => HasMenu || OnClickCallback.HasDelegate || !string.IsNullOrWhiteSpace(Href);
    private bool HasMenu => MenuContent is not null;
    private bool IsSelectedLeadingIcon => ShowSelectedIcon && Variant == NTChipVariant.Filter && CurrentSelected;
    private bool IsTwoAction => ShowRemoveButton && HasPrimaryAction;
    private string LeadingIconClass => IsSelectedLeadingIcon ? "nt-chip-icon nt-chip-leading nt-chip-selected-leading" : "nt-chip-icon nt-chip-leading";
    private EventCallback<MouseEventArgs> LinkClickCallback => OnClickCallback.HasDelegate ? EventCallback.Factory.Create<MouseEventArgs>(this, HandleClickAsync) : default;
    private string MenuAnchorName => $"--{StableElementId}-menu-anchor";
    private string MenuAnchorSelector => $"#{MenuButtonId}";
    private string MenuButtonId => $"{StableElementId}-menu-button";
    private string MenuButtonStyle => $"anchor-name: {MenuAnchorName};";
    private EventCallback<MouseEventArgs> MenuClickCallback => OnClickCallback.HasDelegate ? EventCallback.Factory.Create<MouseEventArgs>(this, HandleClickAsync) : default;
    private string MenuId => $"{StableElementId}-menu";
    private string? MenuPopoverTarget => Disabled ? null : MenuId;
    private EventCallback<KeyboardEventArgs> RemoveKeyDownCallback => ShowRemoveButton ? EventCallback.Factory.Create<KeyboardEventArgs>(this, HandleRemoveKeyDownAsync) : default;
    private string? SelectedAriaPressed => CurrentSelected ? "true" : "false";
    private bool ShowRemoveButton => Removable || Variant == NTChipVariant.Input || OnRemoveCallback.HasDelegate;
    private string StableElementId => string.IsNullOrWhiteSpace(ElementId) ? ComponentIdentifier : ElementId;

    /// <inheritdoc />
    public override Task SetParametersAsync(ParameterView parameters) {
        _appearanceWasProvided = false;
        _backgroundColorWasProvided = false;
        _disabledBackgroundColorWasProvided = false;
        _disabledIconColorWasProvided = false;
        _disabledOutlineColorWasProvided = false;
        _disabledTextColorWasProvided = false;
        _focusOutlineColorWasProvided = false;
        _iconColorWasProvided = false;
        _outlineColorWasProvided = false;
        _selectableWasProvided = false;
        _selectedBackgroundColorWasProvided = false;
        _selectedIconColorWasProvided = false;
        _selectedOutlineColorWasProvided = false;
        _selectedWasProvided = false;
        _selectedStateLayerColorWasProvided = false;
        _selectedTextColorWasProvided = false;
        _stateLayerColorWasProvided = false;
        _textColorWasProvided = false;

        foreach (var parameter in parameters) {
            switch (parameter.Name) {
                case nameof(Appearance):
                    _appearanceWasProvided = true;
                    break;

                case nameof(BackgroundColor):
                    _backgroundColorWasProvided = true;
                    break;

                case nameof(DisabledBackgroundColor):
                    _disabledBackgroundColorWasProvided = true;
                    break;

                case nameof(DisabledIconColor):
                    _disabledIconColorWasProvided = true;
                    break;

                case nameof(DisabledOutlineColor):
                    _disabledOutlineColorWasProvided = true;
                    break;

                case nameof(DisabledTextColor):
                    _disabledTextColorWasProvided = true;
                    break;

                case nameof(FocusOutlineColor):
                    _focusOutlineColorWasProvided = true;
                    break;

                case nameof(IconColor):
                    _iconColorWasProvided = true;
                    break;

                case nameof(OutlineColor):
                    _outlineColorWasProvided = true;
                    break;

                case nameof(Selectable):
                    _selectableWasProvided = true;
                    break;

                case nameof(SelectedBackgroundColor):
                    _selectedBackgroundColorWasProvided = true;
                    break;

                case nameof(Selected):
                    _selectedWasProvided = true;
                    break;

                case nameof(SelectedIconColor):
                    _selectedIconColorWasProvided = true;
                    break;

                case nameof(SelectedOutlineColor):
                    _selectedOutlineColorWasProvided = true;
                    break;

                case nameof(SelectedStateLayerColor):
                    _selectedStateLayerColorWasProvided = true;
                    break;

                case nameof(SelectedTextColor):
                    _selectedTextColorWasProvided = true;
                    break;

                case nameof(StateLayerColor):
                    _stateLayerColorWasProvided = true;
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
            throw new ArgumentException("NTChip requires a non-empty Label.", nameof(Label));
        }

        if (EffectiveSelectable && !string.IsNullOrWhiteSpace(Href)) {
            throw new InvalidOperationException("Selectable chips cannot also render as links.");
        }

        if (HasMenu && !string.IsNullOrWhiteSpace(Href)) {
            throw new InvalidOperationException("Menu chips cannot also render as links.");
        }

        if (HasMenu && _selectableWasProvided && Selectable == true) {
            throw new InvalidOperationException("Menu chips cannot also render as selectable checkboxes.");
        }

        if (_selectedWasProvided) {
            _selected = Selected;
        }
    }

    private async Task HandleClickAsync(MouseEventArgs args) {
        if (Disabled) {
            return;
        }

        await OnClickCallback.InvokeAsync(args);
    }

    private async Task HandleRemoveAsync(MouseEventArgs args) {
        if (Disabled) {
            return;
        }

        await OnRemoveCallback.InvokeAsync(args);
    }

    private async Task HandleRemoveKeyDownAsync(KeyboardEventArgs args) {
        if (Disabled || !ShowRemoveButton || args.Key is not ("Delete" or "Backspace")) {
            return;
        }

        await OnRemoveCallback.InvokeAsync(new MouseEventArgs());
    }

    private async Task HandleSelectedChangedAsync(ChangeEventArgs args) {
        if (Disabled) {
            return;
        }

        if (args.Value is bool value) {
            _selected = value;
            await SelectedChanged.InvokeAsync(value);
        }
    }

}

/// <summary>
///     Defines the Material 3 chip role rendered by <see cref="NTChip" />.
/// </summary>
/// <remarks>
///     <para>
///         Material 3 chips expose compact, contextual options without the weight of full buttons, checkboxes, or form fields. Choose the variant from the user's job: use <see cref="NTChipVariant.Assist" /> to
///         perform a contextual action, <see cref="NTChipVariant.Filter" /> to select or deselect filtering criteria, <see cref="NTChipVariant.Input" /> to represent user-entered values, and <see
///         cref="NTChipVariant.Suggestion" /> to offer product-generated next steps.
///     </para>
///     <para>
///         Keep chip labels concise, preferably 20 characters or fewer. Place related chips inline, allow them to wrap when space is limited, and keep at least 8 CSS pixels between visible chip
///         containers. The visible chip may be smaller, but the interactive target should remain at least 48 by 48 CSS pixels.
///     </para>
///     <para>
///         Do not use chips as a replacement for page-level primary actions, long-form navigation, or dense data tables. When the choice is central to form submission or needs full validation, prefer
///         the matching form control instead.
///     </para>
/// </remarks>
public enum NTChipVariant {
    /// <summary>
    ///     Contextual action chip for helping people complete the current task.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Use assist chips for lightweight, context-aware actions such as adding an event to a calendar, opening directions, calling a contact, or applying an automated action suggested by the
    ///         surrounding content. They are best when the action is helpful but not the primary action on the page.
    ///     </para>
    ///     <para>
    ///         Assist chips render as native buttons unless <see cref="NTChip.Href" /> is supplied, in which case they render as anchors for static navigation. Prefer a leading icon when it makes the
    ///         action easier to recognize, and avoid using assist chips for destructive, final, or high-emphasis actions that deserve a button.
    ///     </para>
    /// </remarks>
    Assist,

    /// <summary>
    ///     Selectable chip for narrowing or toggling a set of content.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Use filter chips for tags, categories, attributes, and other criteria that refine a collection, search result, or view. They are a good alternative to checkboxes or toggle buttons when
    ///         the options are compact and context-specific.
    ///     </para>
    ///     <para>
    ///         Filter chips are selectable by default and render a native checkbox. Use multi-select filter chips when multiple criteria can apply at the same time. For a mutually exclusive choice,
    ///         keep the set clearly labeled and consider whether radio buttons or segmented buttons would communicate the relationship more directly.
    ///     </para>
    ///     <para>
    ///         Use <see cref="NTChip.MenuContent" /> when a filter value is chosen from a dropdown list. The dropdown form renders as a native popover trigger and should use menu item text that lines up
    ///         with the chip label users see after selection.
    ///     </para>
    /// </remarks>
    Filter,

    /// <summary>
    ///     User-entered value chip, usually with a remove action.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Use input chips for discrete pieces of information that the user has entered or selected, such as contacts, recipients, tags, uploaded items, or selected filter tokens inside a search
    ///         field. They should represent existing input, not ask the user to make a new choice.
    ///     </para>
    ///     <para>
    ///         Input chips show a remove affordance by default through <see cref="NTChip.Removable" /> behavior. If the chip also performs another action, keep the remove action separately labeled
    ///         with <see cref="NTChip.RemoveAriaLabel" /> so assistive technology can distinguish the two actions.
    ///     </para>
    /// </remarks>
    Input,

    /// <summary>
    ///     Product-generated suggestion chip for helping people refine intent or choose a next step.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Use suggestion chips for dynamic suggestions such as quick replies, suggested searches, recommended filters, or likely next actions. They work best when the product is offering a small
    ///         set of optional paths rather than asking the user to complete a required form choice.
    ///     </para>
    ///     <para>
    ///         Suggestion chips default to the elevated appearance in <see cref="NTChip" />. Write labels as short nouns or phrases, and avoid presenting stale, irrelevant, or too many suggestions at
    ///         once. If a suggestion navigates to another page, supply <see cref="NTChip.Href" /> so the chip renders as a native anchor.
    ///     </para>
    /// </remarks>
    Suggestion
}

/// <summary>
///     Defines the Material 3 chip container treatment rendered by <see cref="NTChip" />.
/// </summary>
public enum NTChipAppearance {
    /// <summary>
    ///     Uses a transparent container with an outline.
    /// </summary>
    Outlined,

    /// <summary>
    ///     Uses a low surface container and elevation.
    /// </summary>
    Elevated
}
