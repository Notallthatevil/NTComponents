using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using NTComponents.Core;

namespace NTComponents;

/// <summary>
///     Material 3 split button that pairs a primary action with a related menu action.
/// </summary>
/// <remarks>
///     <para>
///         Split buttons are made from a leading button for the direct action and a trailing menu button for related alternatives. Use them when the primary action is clear, but nearby secondary
///         actions should remain available without taking equal visual weight.
///     </para>
///     <para>
///         Material 3 split buttons support elevated, filled, tonal, and outlined variants. Text split buttons are intentionally rejected because the split affordance depends on visible button
///         containers. The trailing button morphs to a full pill shape while expanded, matching the Material 3 activation guidance.
///     </para>
///     <para>
///         Override <see cref="BackgroundColor" />, <see cref="TextColor" />, <see cref="MenuBackgroundColor" />, and <see cref="MenuTextColor" /> only when the resulting contrast remains clear.
///         Elevated split buttons must keep non-zero elevation; other variants must remain flat.
///     </para>
/// </remarks>
public partial class NTSplitButton {

    /// <summary>
    ///     Gets or sets the accessible label for the leading action when the visible label is omitted or needs more context.
    /// </summary>
    [Parameter]
    public string? ActionAriaLabel { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for the split button container color.
    /// </summary>
    [Parameter]
    public TnTColor? BackgroundColor { get; set; }

    /// <summary>
    ///     Gets or sets the size of both split button segments.
    /// </summary>
    [Parameter]
    public Size ButtonSize { get; set; } = Size.Small;

    /// <summary>
    ///     Gets or sets the menu content rendered when the trailing segment is expanded.
    /// </summary>
    /// <remarks>
    ///     Prefer children with <c>role="menuitem"</c> or native menu-item semantics. The component supplies the menu container, expansion state, and outside-click close behavior.
    /// </remarks>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    ///     Gets or sets whether clicking inside the menu panel closes it.
    /// </summary>
    [Parameter]
    public bool CloseOnMenuContentClick { get; set; } = true;

    /// <summary>
    ///     Gets or sets whether the split button is disabled.
    /// </summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <inheritdoc />
    public override string? ElementClass => CssClassBuilder.Create("nt-split-button")
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddClass("nt-split-button-expanded", Expanded)
        .AddClass("nt-split-button-elevated", Variant == NTButtonVariant.Elevated)
        .AddClass("nt-split-button-filled", Variant == NTButtonVariant.Filled)
        .AddClass("nt-split-button-tonal", Variant == NTButtonVariant.Tonal)
        .AddClass("nt-split-button-outlined", Variant == NTButtonVariant.Outlined)
        .AddClass("nt-split-button-shape-round", Shape == ButtonShape.Round)
        .AddClass("nt-split-button-shape-square", Shape == ButtonShape.Square)
        .AddSize(ButtonSize)
        .AddDisabled(Disabled)
        .Build();

    /// <summary>
    ///     Gets or sets the optional name attribute for the leading action button.
    /// </summary>
    [Parameter]
    public string? ElementName { get; set; }

    /// <inheritdoc />
    public override string? ElementStyle => CssStyleBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddVariable("nt-split-button-bg", BackgroundColor.ToCssTnTColorVariable(), BackgroundColor.HasValue)
        .AddVariable("nt-split-button-fg", TextColor.ToCssTnTColorVariable(), TextColor.HasValue)
        .AddVariable("nt-split-button-menu-bg", MenuBackgroundColor.ToCssTnTColorVariable(), MenuBackgroundColor.HasValue)
        .AddVariable("nt-split-button-menu-fg", MenuTextColor.ToCssTnTColorVariable(), MenuTextColor.HasValue)
        .Build();

    /// <inheritdoc />
    public override string? JsModulePath => "./_content/NTComponents/Buttons/NTSplitButton.razor.js";

    /// <summary>
    ///     Gets or sets an optional override for the split button elevation.
    /// </summary>
    [Parameter]
    public NTElevation? Elevation { get; set; }

    /// <summary>
    ///     Gets or sets whether the trailing menu button is expanded.
    /// </summary>
    [Parameter]
    public bool Expanded { get; set; }

    /// <summary>
    ///     Gets or sets the callback invoked when <see cref="Expanded" /> changes.
    /// </summary>
    [Parameter]
    public EventCallback<bool> ExpandedChanged { get; set; }

    /// <summary>
    ///     Gets or sets whether ripple effects should render on both segments.
    /// </summary>
    [Parameter]
    public bool EnableRipple { get; set; } = true;

    /// <summary>
    ///     Gets or sets the visible text label for the leading action.
    /// </summary>
    [Parameter]
    public string Label { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets an optional icon rendered before the leading action label.
    /// </summary>
    [Parameter]
    public TnTIcon? LeadingIcon { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for the menu panel container color.
    /// </summary>
    [Parameter]
    public TnTColor? MenuBackgroundColor { get; set; }

    /// <summary>
    ///     Gets or sets whether the trailing menu segment is disabled.
    /// </summary>
    [Parameter]
    public bool MenuButtonDisabled { get; set; }

    /// <summary>
    ///     Gets or sets the accessible label for the trailing menu button.
    /// </summary>
    [Parameter]
    public string? MenuButtonLabel { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for the menu panel content color.
    /// </summary>
    [Parameter]
    public TnTColor? MenuTextColor { get; set; }

    /// <summary>
    ///     Gets or sets the callback invoked by the leading action segment.
    /// </summary>
    [Parameter]
    public EventCallback<MouseEventArgs> OnClickCallback { get; set; }

    /// <summary>
    ///     Gets or sets the resting shape family for the split button segments.
    /// </summary>
    [Parameter]
    public ButtonShape Shape { get; set; } = ButtonShape.Round;

    /// <summary>
    ///     Gets or sets whether click events should stop propagating.
    /// </summary>
    [Parameter]
    public bool StopPropagation { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for the split button content color.
    /// </summary>
    [Parameter]
    public TnTColor? TextColor { get; set; }

    /// <summary>
    ///     Gets or sets the native button type for the leading action.
    /// </summary>
    [Parameter]
    public ButtonType Type { get; set; }

    /// <summary>
    ///     Gets or sets the split button visual variant.
    /// </summary>
    [Parameter]
    public NTButtonVariant Variant { get; set; } = NTButtonVariant.Filled;

    private string? ActionButtonAriaLabel => string.IsNullOrWhiteSpace(ActionAriaLabel) ? null : ActionAriaLabel;
    private string ActionButtonClass => CssClassBuilder.Create("nt-split-button-segment nt-split-button-leading")
        .AddElevation(Elevation)
        .Build();
    private string ExpandedAttribute => Expanded.ToString().ToLowerInvariant();
    private string EffectiveActionLabel => !string.IsNullOrWhiteSpace(Label) ? Label : ActionAriaLabel ?? "action";
    private string EffectiveMenuAriaLabel => string.IsNullOrWhiteSpace(MenuButtonLabel) ? $"More options for {EffectiveActionLabel}" : MenuButtonLabel;
    private bool HasMenuItems => _menuItems.Any(static item => item is not NTSplitButtonDividerItem);
    private bool HasVisibleLabel => !string.IsNullOrWhiteSpace(Label);
    private bool IsMenuButtonDisabled => Disabled || MenuButtonDisabled;
    private string MenuAnchorName => $"--{_generatedMenuId}-anchor";
    private string MenuButtonClass => CssClassBuilder.Create("nt-split-button-segment nt-split-button-trailing")
        .AddElevation(Elevation)
        .Build();
    private string MenuButtonStyle => $"anchor-name: {MenuAnchorName};";
    private string MenuId => $"{(string.IsNullOrWhiteSpace(ElementId) ? _generatedMenuId : ElementId)}-menu";
    private string MenuPanelStyle => $"position-anchor: {MenuAnchorName};";
    private string? MenuPopoverTarget => IsMenuButtonDisabled ? null : MenuId;
    private static TnTIcon MenuIcon => MaterialIcon.ArrowDropDown;

    private static readonly RenderFragment ButtonRipple = NTRipple.Render();
    private readonly string _generatedMenuId = $"nt-split-button-{Guid.NewGuid():N}";
    private readonly List<ISplitButtonItem> _menuItems = [];
    private readonly HashSet<ISplitButtonItem> _menuItemSet = [];
    private bool _backgroundColorWasProvided;
    private bool _elevationWasProvided;
    private bool _menuBackgroundColorWasProvided;
    private bool _menuTextColorWasProvided;
    private bool _textColorWasProvided;

    /// <inheritdoc />
    public override Task SetParametersAsync(ParameterView parameters) {
        _backgroundColorWasProvided = false;
        _elevationWasProvided = false;
        _menuBackgroundColorWasProvided = false;
        _menuTextColorWasProvided = false;
        _textColorWasProvided = false;

        foreach (var parameter in parameters) {
            switch (parameter.Name) {
                case nameof(BackgroundColor):
                    _backgroundColorWasProvided = true;
                    break;

                case nameof(Elevation):
                    _elevationWasProvided = true;
                    break;

                case nameof(MenuBackgroundColor):
                    _menuBackgroundColorWasProvided = true;
                    break;

                case nameof(MenuTextColor):
                    _menuTextColorWasProvided = true;
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

        if (string.IsNullOrWhiteSpace(Label) && LeadingIcon is null) {
            throw new ArgumentException("NTSplitButton requires a non-empty Label unless a LeadingIcon is supplied.", nameof(Label));
        }

        if (string.IsNullOrWhiteSpace(Label) && string.IsNullOrWhiteSpace(ActionAriaLabel)) {
            throw new ArgumentException("Icon-only NTSplitButton actions require a non-empty ActionAriaLabel.", nameof(ActionAriaLabel));
        }

        if (Variant == NTButtonVariant.Text) {
            throw new InvalidOperationException("Text split buttons are not supported by Material 3.");
        }

        if (!_backgroundColorWasProvided || !BackgroundColor.HasValue) {
            BackgroundColor = GetDefaultBackgroundColor();
        }

        if (!_elevationWasProvided || !Elevation.HasValue) {
            Elevation = GetDefaultElevation();
        }

        if (!_textColorWasProvided || !TextColor.HasValue) {
            TextColor = GetDefaultTextColor();
        }

        if (!_menuBackgroundColorWasProvided || !MenuBackgroundColor.HasValue) {
            MenuBackgroundColor = TnTColor.SurfaceContainer;
        }

        if (!_menuTextColorWasProvided || !MenuTextColor.HasValue) {
            MenuTextColor = TnTColor.OnSurface;
        }

        ValidateVariantColorCombination();
        ValidateVariantElevationCombination();
        ValidateMenuColors();
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender) {
        if (firstRender && !HasMenuItems) {
            throw new InvalidOperationException($"NTSplitButton requires at least one {nameof(NTSplitButtonButtonItem)} or {nameof(NTSplitButtonAnchorItem)} child. Use {nameof(NTButton)} for a single action.");
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    /// <summary>
    ///     Receives native popover state changes from the browser.
    /// </summary>
    [JSInvokable]
    public Task NotifySplitButtonExpandedChanged(bool expanded) => SetExpandedAsync(expanded);

    /// <summary>
    ///     Tracks a registered menu item so the split button can render a constrained menu layout.
    /// </summary>
    internal void RegisterMenuItem(ISplitButtonItem item) {
        if (item is not null && _menuItemSet.Add(item)) {
            _menuItems.Add(item);
            StateHasChanged();
        }
    }

    /// <summary>
    ///     Unregisters a menu item when it is removed.
    /// </summary>
    internal void UnregisterMenuItem(ISplitButtonItem item) {
        if (item is not null && _menuItemSet.Remove(item) && _menuItems.Remove(item)) {
            StateHasChanged();
        }
    }

    private async Task CloseMenuAsync() {
        if (RendererInfo.IsInteractive && IsolatedJsModule is not null) {
            try {
                await IsolatedJsModule.InvokeVoidAsync("setExpanded", Element, false);
            }
            catch (JSDisconnectedException) {
                // The circuit can disconnect between the click and interop.
            }
        }

        if (Expanded) {
            await SetExpandedAsync(false);
        }
    }

    private TnTColor GetDefaultBackgroundColor() {
        return Variant switch {
            NTButtonVariant.Elevated => TnTColor.SurfaceContainerLow,
            NTButtonVariant.Filled => TnTColor.Primary,
            NTButtonVariant.Tonal => TnTColor.SecondaryContainer,
            NTButtonVariant.Outlined => TnTColor.Transparent,
            NTButtonVariant.Text => TnTColor.Transparent,
            _ => throw new ArgumentOutOfRangeException(nameof(Variant), Variant, null)
        };
    }

    private NTElevation GetDefaultElevation() {
        return Variant == NTButtonVariant.Elevated ? NTElevation.Lowest : NTElevation.None;
    }

    private TnTColor GetDefaultTextColor() {
        return Variant switch {
            NTButtonVariant.Elevated => TnTColor.Primary,
            NTButtonVariant.Filled => TnTColor.OnPrimary,
            NTButtonVariant.Tonal => TnTColor.OnSecondaryContainer,
            NTButtonVariant.Outlined => TnTColor.Primary,
            NTButtonVariant.Text => TnTColor.Primary,
            _ => throw new ArgumentOutOfRangeException(nameof(Variant), Variant, null)
        };
    }

    private async Task HandleActionClickAsync(MouseEventArgs args) {
        if (Disabled) {
            return;
        }

        await CloseMenuAsync();
        await OnClickCallback.InvokeAsync(args);
    }

    private async Task HandleMenuContentClickAsync(MouseEventArgs _) {
        if (CloseOnMenuContentClick) {
            await CloseMenuAsync();
        }
    }

    internal async Task HandleMenuButtonItemClickAsync(NTSplitButtonButtonItem item, MouseEventArgs args) {
        if (IsMenuItemDisabled(item)) {
            return;
        }

        await item.OnClickCallback.InvokeAsync(args);

        if (CloseOnMenuContentClick) {
            await SetExpandedAsync(false);
        }
    }

    private async Task HandleMenuKeyDownAsync(KeyboardEventArgs args) {
        if (args.Key == "Escape") {
            await CloseMenuAsync();
        }
    }

    private async Task SetExpandedAsync(bool expanded) {
        if (Expanded == expanded) {
            return;
        }

        Expanded = expanded;
        await ExpandedChanged.InvokeAsync(expanded);
    }

    internal string? GetMenuItemAriaDisabled(ISplitButtonItem item) => IsMenuItemDisabled(item) ? "true" : null;

    internal string GetMenuItemAriaLabel(ISplitButtonItem item) => item.AriaLabel ?? item.Label;

    internal string GetMenuItemClass(ISplitButtonItem item) => CssClassBuilder.Create("nt-split-button-menu-item")
        .AddClass(GetMenuItemAdditionalClass(item))
        .AddClass("nt-split-button-menu-item-disabled", IsMenuItemDisabled(item))
        .Build();

    internal string GetMenuDividerClass(NTSplitButtonDividerItem item) => CssClassBuilder.Create("nt-split-button-menu-divider")
        .AddClass(GetMenuItemAdditionalClass(item))
        .AddClass("nt-split-button-menu-divider-inset", item.Inset)
        .Build();

    private static string? GetMenuItemAdditionalClass(ISplitButtonItem item) => item.AdditionalAttributes?.TryGetValue("class", out var @class) == true ? @class?.ToString() : null;

    internal static IEnumerable<KeyValuePair<string, object>>? GetMenuItemAdditionalAttributes(ISplitButtonItem item) =>
        item.AdditionalAttributes?
            .Where(static attribute => attribute.Value is not null)
            .Where(static attribute => !string.Equals(attribute.Key, "class", StringComparison.OrdinalIgnoreCase))
            .Select(static attribute => new KeyValuePair<string, object>(attribute.Key, attribute.Value!));

    internal static bool HasNativeOnClick(ISplitButtonItem item) => item.AdditionalAttributes?.Keys.Any(key => string.Equals(key, "onclick", StringComparison.OrdinalIgnoreCase)) == true;

    internal string? GetMenuAnchorItemHref(NTSplitButtonAnchorItem item) => IsMenuItemDisabled(item) ? null : item.Href;

    internal string? GetMenuItemPopoverTarget(ISplitButtonItem item) => CloseOnMenuContentClick && !IsMenuItemDisabled(item) ? MenuId : null;

    internal string? GetMenuItemTabIndex(ISplitButtonItem item) => IsMenuItemDisabled(item) ? "-1" : null;

    internal bool IsMenuItemDisabled(ISplitButtonItem item) => Disabled || item.Disabled;

    internal RenderFragment RenderMenuItemContent(ISplitButtonItem item) => builder => {
        var sequence = 0;
        if (item.Icon is not null) {
            builder.OpenElement(sequence++, "span");
            builder.AddAttribute(sequence++, "class", "nt-split-button-menu-item-icon");
            builder.AddAttribute(sequence++, "aria-hidden", "true");
            builder.AddContent(sequence++, item.Icon.Render());
            builder.CloseElement();
        }

        builder.OpenElement(sequence++, "span");
        builder.AddAttribute(sequence++, "class", "nt-split-button-menu-item-label");
        builder.AddContent(sequence++, item.Label);
        builder.CloseElement();
    };

    private void ValidateBackgroundColorForVariant() {
        if (!BackgroundColor.HasValue) {
            return;
        }

        if (Variant == NTButtonVariant.Outlined) {
            if (BackgroundColor != TnTColor.Transparent) {
                throw new InvalidOperationException($"{Variant} split buttons must use a transparent {nameof(BackgroundColor)}.");
            }

            return;
        }

        if (BackgroundColor is TnTColor.None or TnTColor.Transparent) {
            throw new InvalidOperationException($"{Variant} split buttons must use a visible container {nameof(BackgroundColor)}.");
        }
    }

    private void ValidateMenuColors() {
        if (MenuBackgroundColor is TnTColor.None or TnTColor.Transparent) {
            throw new InvalidOperationException($"{nameof(MenuBackgroundColor)} must be a visible menu container color.");
        }

        if (MenuTextColor is TnTColor.None or TnTColor.Transparent) {
            throw new InvalidOperationException($"{nameof(MenuTextColor)} must be a visible menu content color.");
        }
    }

    private void ValidateVariantColorCombination() {
        ValidateBackgroundColorForVariant();

        if (TextColor is TnTColor.None or TnTColor.Transparent) {
            throw new InvalidOperationException($"{nameof(TextColor)} must be a visible split button content color.");
        }
    }

    private void ValidateVariantElevationCombination() {
        if (!Elevation.HasValue) {
            return;
        }

        if (Variant == NTButtonVariant.Elevated) {
            if (Elevation == NTElevation.None) {
                throw new InvalidOperationException($"{nameof(NTButtonVariant.Elevated)} split buttons must use a non-zero {nameof(Elevation)}.");
            }

            return;
        }

        if (Elevation != NTElevation.None) {
            throw new InvalidOperationException($"{Variant} split buttons must use {nameof(NTElevation.None)} {nameof(Elevation)}.");
        }
    }
}
