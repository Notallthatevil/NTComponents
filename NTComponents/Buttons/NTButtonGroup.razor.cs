using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using NTComponents.Core;
using System.Globalization;
using System.Linq.Expressions;
using System.Text;

namespace NTComponents;

/// <summary>
/// Represents a Material 3 button group that adds selection, shape, and width interactions to buttons.
/// </summary>
/// <remarks>
/// <para> Use a button group when several related buttons should be perceived as one control surface. Material 3 button
/// groups are intended for related actions or choices that benefit from shared shape, motion, and width interactions.
/// </para> <para> Use <see cref="NTButtonGroupDisplayType.Disconnected" /> for the default group: actions remain
/// visually separate, but the spacing and coordinated motion communicate that they belong together. Use
/// <see cref="NTButtonGroupDisplayType.Connected" /> for compact option sets such as filters, sort modes, or view
/// switches; Material 3 positions connected button groups as the replacement for segmented buttons. </para> <para> Keep
/// each group focused. Avoid mixing unrelated actions, unrelated selection models, or actions with very different
/// consequences in the same group. Prefer a short, scannable set of labels and use icon-only items only when the icon
/// is familiar and <see cref="NTButtonGroupItem{TObjectType}.AriaLabel" /> supplies a clear accessible name. </para>
/// <para> Match <see cref="SelectionMode" /> to behavior: use <see cref="NTButtonGroupSelectionMode.Single" /> for one
/// active choice, <see cref="NTButtonGroupSelectionMode.Multiple" /> for independent toggles in the same set, and
/// <see cref="NTButtonGroupSelectionMode.None" /> for action-only groups. Use <see cref="SelectionRequired" /> only
/// when clearing all choices would leave the surrounding UI in an invalid or ambiguous state. </para>
/// </remarks>
public partial class NTButtonGroup<TObjectType> : TnTComponentBase {

    /// <summary>
    /// Provides an accessible label for the group container.
    /// </summary>
    /// <remarks>
    /// Provide a concise label whenever the visible surrounding text does not already describe the group. This is
    /// especially important for connected groups and icon-heavy groups because assistive technologies need context for
    /// the shared choice set.
    /// </remarks>
    [Parameter]
    public string? AriaLabel { get; set; }

    /// <summary>
    /// Gets or sets an optional override for the resting button container color.
    /// </summary>
    [Parameter]
    public TnTColor? BackgroundColor { get; set; }

    /// <summary>
    /// The size applied to every button inside the group.
    /// </summary>
    /// <remarks>
    /// Prefer one size across the group so the set scans as a single control. Item-level size overrides are supported
    /// for expressive emphasis, but should be used sparingly and only when the hierarchy remains obvious.
    /// </remarks>
    [Parameter]
    public Size ButtonSize { get; set; } = Size.Small;

    /// <summary>
    /// The child content that defines the group items.
    /// </summary>
    /// <remarks>
    /// Add <see cref="NTButtonGroupItem{TObjectType}" /> children only. Button groups can contain labeled buttons,
    /// leading-icon buttons, and icon-only buttons; end icons are intentionally not rendered.
    /// </remarks>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Disables every button inside the group.
    /// </summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>
    /// When true the ripple animation is suppressed on the child buttons.
    /// </summary>
    [Parameter]
    public bool DisableRipple { get; set; }

    /// <summary>
    /// Determines whether the group uses separated buttons or a visually connected button set.
    /// </summary>
    /// <remarks>
    /// Prefer <see cref="NTButtonGroupDisplayType.Disconnected" /> for related actions that should keep individual
    /// button identity. Prefer <see cref="NTButtonGroupDisplayType.Connected" /> for segmented-button-style option sets
    /// where the shared boundary communicates one compact choice group.
    /// </remarks>
    [Parameter]
    public NTButtonGroupDisplayType DisplayType { get; set; } = NTButtonGroupDisplayType.Disconnected;

    /// <summary>
    /// The cascading edit context from an <see cref="EditForm" />.
    /// </summary>
    [CascadingParameter]
    public EditContext? EditContext { get; set; }

    /// <inheritdoc />
    public override string? ElementClass => CssClassBuilder.Create("nt-button-group")
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddClass("nt-button-group-disconnected", DisplayType == NTButtonGroupDisplayType.Disconnected)
        .AddClass("nt-button-group-connected", DisplayType == NTButtonGroupDisplayType.Connected)
        .AddClass("nt-button-group-full-width", FullWidth)
        .AddClass("nt-button-group-round", Shape == ButtonShape.Round)
        .AddClass("nt-button-group-square", Shape == ButtonShape.Square)
        .AddClass("nt-button-group-elevated", Variant == NTButtonVariant.Elevated)
        .AddClass("nt-button-group-filled", Variant == NTButtonVariant.Filled)
        .AddClass("nt-button-group-tonal", Variant == NTButtonVariant.Tonal)
        .AddClass("nt-button-group-outlined", Variant == NTButtonVariant.Outlined)
        .AddClass("nt-button-group-text", Variant == NTButtonVariant.Text)
        .AddSize(ButtonSize)
        .AddClass(EditContext?.FieldCssClass(in _fieldIdentifier), EditContext is not null)
        .Build();

    /// <inheritdoc />
    public override string? ElementStyle => CssStyleBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddVariable("nt-button-group-bg", BackgroundColor.ToCssTnTColorVariable(), BackgroundColor.HasValue)
        .AddVariable("nt-button-group-fg", TextColor.ToCssTnTColorVariable(), TextColor.HasValue)
        .AddVariable("nt-button-group-selected-bg", SelectedBackgroundColor.ToCssTnTColorVariable(), SelectedBackgroundColor.HasValue)
        .AddVariable("nt-button-group-selected-fg", SelectedTextColor.ToCssTnTColorVariable(), SelectedTextColor.HasValue)
        .Build();

    /// <summary>
    /// Indicates whether ripples should render on each button.
    /// </summary>
    [Parameter]
    public bool EnableRipple { get; set; } = true;

    /// <summary>
    /// The color used to indicate an error state in the user interface.
    /// </summary>
    [Parameter]
    public TnTColor ErrorColor { get; set; } = TnTColor.Error;

    /// <summary>
    /// Expands the button group to fill the available inline space.
    /// </summary>
    /// <remarks>
    /// Leave this unset when the group should size to its content. Enable it for responsive toolbars, dense filter
    /// rows, or layouts where every item should share the available width.
    /// </remarks>
    [Parameter]
    public bool FullWidth { get; set; }

    /// <summary>
    /// The color used to on <see cref="ErrorColor" />.
    /// </summary>
    [Parameter]
    public TnTColor OnErrorColor { get; set; } = TnTColor.OnError;

    /// <summary>
    /// Invoked whenever selection toggles and passes the impacted item.
    /// </summary>
    [Parameter]
    public EventCallback<NTButtonGroupItem<TObjectType>> OnSelectionChanged { get; set; }

    /// <summary>
    /// Gets or sets an optional override for the selected button container color.
    /// </summary>
    [Parameter]
    public TnTColor? SelectedBackgroundColor { get; set; }

    /// <summary>
    /// The key that represents the currently selected button in single-select mode.
    /// </summary>
    [Parameter]
    public TObjectType? SelectedKey { get; set; }

    /// <summary>
    /// Invoked when the selected key changes in single-select mode.
    /// </summary>
    [Parameter]
    public EventCallback<TObjectType?> SelectedKeyChanged { get; set; }

    /// <summary>
    /// The expression used to identify the field being bound.
    /// </summary>
    [Parameter]
    public Expression<Func<TObjectType?>>? SelectedKeyExpression { get; set; }

    /// <summary>
    /// The keys that represent selected buttons in multi-select mode.
    /// </summary>
    [Parameter]
    public IReadOnlyCollection<TObjectType> SelectedKeys { get; set; } = Array.Empty<TObjectType>();

    /// <summary>
    /// Invoked when the selected key collection changes in multi-select mode.
    /// </summary>
    [Parameter]
    public EventCallback<IReadOnlyCollection<TObjectType>> SelectedKeysChanged { get; set; }

    /// <summary>
    /// Gets or sets an optional override for the selected button content color.
    /// </summary>
    [Parameter]
    public TnTColor? SelectedTextColor { get; set; }

    /// <summary>
    /// Determines whether the group uses single-select or multi-select behavior.
    /// </summary>
    /// <remarks>
    /// Use <see cref="NTButtonGroupSelectionMode.Single" /> for one selected destination, mode, or view. Use
    /// <see cref="NTButtonGroupSelectionMode.Multiple" /> for independent filters or toggles that can be combined. Use
    /// <see cref="NTButtonGroupSelectionMode.None" /> when the buttons perform actions and should not expose selected
    /// state.
    /// </remarks>
    [Parameter]
    public NTButtonGroupSelectionMode SelectionMode { get; set; } = NTButtonGroupSelectionMode.Single;

    /// <summary>
    /// Determines whether selected buttons can be cleared.
    /// </summary>
    /// <remarks>
    /// Use this when at least one option must always be active, such as a required view mode or filter scope. Avoid it
    /// for optional filters or action-only groups where people should be able to return to no selection.
    /// </remarks>
    [Parameter]
    public bool SelectionRequired { get; set; }

    /// <summary>
    /// The base shape applied to all buttons. Selected toggle buttons morph to the opposite shape.
    /// </summary>
    /// <remarks>
    /// Use round shapes for the default Material button-group feel. Use square shapes when the surrounding interface is
    /// denser or more container-like, and let selection provide the contrasting shape state.
    /// </remarks>
    [Parameter]
    public ButtonShape Shape { get; set; } = ButtonShape.Round;

    /// <summary>
    /// Stops bubbling on every button click inside the group.
    /// </summary>
    [Parameter]
    public bool StopPropagation { get; set; } = true;

    /// <summary>
    /// Gets or sets an optional override for the resting button content color.
    /// </summary>
    [Parameter]
    public TnTColor? TextColor { get; set; }

    /// <summary>
    /// The default visual variant applied to every item.
    /// </summary>
    /// <remarks>
    /// Prefer <see cref="NTButtonVariant.Tonal" /> or <see cref="NTButtonVariant.Outlined" /> for most groups. Use
    /// <see cref="NTButtonVariant.Filled" /> or <see cref="NTButtonVariant.Elevated" /> when the whole group needs
    /// stronger emphasis. Text buttons are not supported because button groups require visible button containers.
    /// </remarks>
    [Parameter]
    public NTButtonVariant Variant { get; set; } = NTButtonVariant.Tonal;

    private bool _backgroundColorWasProvided;
    private string? _name => _fieldIdentifier.FieldName;
    private bool _selectedBackgroundColorWasProvided;
    private bool _selectedTextColorWasProvided;
    private bool _textColorWasProvided;
    private bool IsMultiSelect => SelectionMode == NTButtonGroupSelectionMode.Multiple;
    private bool IsRippleEnabled => EnableRipple && !DisableRipple;
    private bool IsSelectable => SelectionMode != NTButtonGroupSelectionMode.None;

    private static readonly RenderFragment ButtonInteractionScript = builder => {
        builder.OpenElement(0, "script");
        builder.AddMarkupContent(1, """
        (function (script) {
            window.NTComponents?.startButtonInteraction?.(script);
        })(document.currentScript);
        """);
        builder.CloseElement();
    };

    private static readonly RenderFragment ButtonRipple = NTRipple.Render();
    private static readonly EqualityComparer<TObjectType> KeyComparer = EqualityComparer<TObjectType>.Default;
    private readonly List<NTButtonGroupItem<TObjectType>> _items = [];
    private readonly HashSet<NTButtonGroupItem<TObjectType>> _itemSet = [];
    private FieldIdentifier _fieldIdentifier;
    private HashSet<TObjectType> _selectedKeySet = new(KeyComparer);

    /// <summary>
    /// Tracks a registered item so the group can render it.
    /// </summary>
    internal void RegisterItem(NTButtonGroupItem<TObjectType> item) {
        if (item is not null && _itemSet.Add(item)) {
            _items.Add(item);
            EnsureSelectionIsSet();
            StateHasChanged();
        }
    }

    /// <summary>
    /// Unregisters an item when it is removed from the group.
    /// </summary>
    internal void UnregisterItem(NTButtonGroupItem<TObjectType> item) {
        if (item is not null && _itemSet.Remove(item) && _items.Remove(item)) {
            if (IsMultiSelect) {
                var selectedKeys = SelectedKeys.Where(key => !EqualityComparer<TObjectType>.Default.Equals(key, item.Key)).ToArray();
                if (selectedKeys.Length != SelectedKeys.Count) {
                    SetSelectedKeys(selectedKeys);
                }
            }
            else if (SelectedKey is not null && EqualityComparer<TObjectType>.Default.Equals(SelectedKey, item.Key)) {
                SelectedKey = default;
            }
            StateHasChanged();
        }
    }

    /// <inheritdoc />
    public override Task SetParametersAsync(ParameterView parameters) {
        _backgroundColorWasProvided = false;
        _selectedBackgroundColorWasProvided = false;
        _selectedTextColorWasProvided = false;
        _textColorWasProvided = false;

        foreach (var parameter in parameters) {
            switch (parameter.Name) {
                case nameof(BackgroundColor):
                    _backgroundColorWasProvided = true;
                    break;

                case nameof(SelectedBackgroundColor):
                    _selectedBackgroundColorWasProvided = true;
                    break;

                case nameof(SelectedTextColor):
                    _selectedTextColorWasProvided = true;
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

        SelectedKeys ??= Array.Empty<TObjectType>();
        RebuildSelectedKeySet();

        if (EditContext is not null && SelectedKeyExpression is not null) {
            _fieldIdentifier = FieldIdentifier.Create(SelectedKeyExpression);
        }

        EnsureSelectionIsSet();
        EnsureEffectiveColors();
        ValidateVariantColorCombination();
    }

    private static string FormatFormValue(TObjectType? key) => Convert.ToString(key, CultureInfo.InvariantCulture) ?? string.Empty;

    private static string GetAriaPressed(bool isSelected) => isSelected ? "true" : "false";

    private static string GetDefaultItemElevationClass(NTButtonVariant variant) => variant == NTButtonVariant.Elevated ? "nt-elevation-lowest" : "nt-elevation-none";

    private static string GetIconWidthClassSuffix(NTIconButtonAppearance iconWidth) => iconWidth switch {
        NTIconButtonAppearance.Narrow => "narrow",
        NTIconButtonAppearance.Default => "default",
        NTIconButtonAppearance.Wide => "wide",
        _ => "default"
    };

    /// <summary>
    /// Ensures a default or required item is selected when no explicit selection exists.
    /// </summary>
    private void EnsureSelectionIsSet() {
        if (!IsSelectable) {
            return;
        }

        if (IsMultiSelect) {
            if (SelectedKeys.Count > 0) {
                return;
            }

            var defaultKeys = _items.Where(item => item.IsDefaultSelected).Select(item => item.Key).ToArray();
            if (defaultKeys.Length > 0) {
                SetSelectedKeys(defaultKeys);
                return;
            }

            if (SelectionRequired && _items.FirstOrDefault(item => !item.Disabled) is { } firstEnabledItem) {
                SetSelectedKeys([firstEnabledItem.Key]);
            }

            return;
        }

        if (SelectedKey is not null) {
            return;
        }

        var defaultItem = _items.FirstOrDefault(item => item.IsDefaultSelected);
        if (defaultItem is not null) {
            SelectedKey = defaultItem.Key;
        }
        else if (SelectionRequired && _items.FirstOrDefault(item => !item.Disabled) is { } firstEnabledItem) {
            SelectedKey = firstEnabledItem.Key;
        }
    }

    private ButtonShape GetEffectiveItemShape(NTButtonGroupItem<TObjectType> item, bool isSelected) {
        var shape = Shape;
        return isSelected
            ? shape == ButtonShape.Round ? ButtonShape.Square : ButtonShape.Round
            : shape;
    }

    private string GetItemAriaLabel(NTButtonGroupItem<TObjectType> item) => item.AriaLabel ?? item.Label ?? Convert.ToString(item.Key, CultureInfo.InvariantCulture) ?? string.Empty;

    private string GetItemLabel(NTButtonGroupItem<TObjectType> item) => item.Label ?? GetItemAriaLabel(item);

    private string GetItemPosition(int itemIndex) {
        if (_items.Count == 1) {
            return "btn-group-single";
        }

        if (itemIndex == 0) {
            return "btn-group-first";
        }

        return itemIndex == _items.Count - 1 ? "btn-group-last" : "btn-group-middle";
    }

    private ItemRenderState GetItemRenderState(NTButtonGroupItem<TObjectType> item, int itemIndex) {
        var buttonClass = new StringBuilder("nt-btn-grp-btn");

        if (itemIndex == 0) {
            buttonClass.Append(" nt-btn-grp-btn-first");
        }

        if (itemIndex == _items.Count - 1) {
            buttonClass.Append(" nt-btn-grp-btn-last");
        }

        var isSelected = IsSelectedItem(item);
        var isDisabled = IsButtonDisabled(item);
        var isIconOnly = string.IsNullOrWhiteSpace(item.Label);
        var variant = Variant;
        var effectiveShape = GetEffectiveItemShape(item, isSelected);
        var buttonSize = ButtonSize;
        var isBeforeSelected = itemIndex < _items.Count - 1 && IsSelectedItem(_items[itemIndex + 1]);
        var itemPosition = GetItemPosition(itemIndex);

        return new ItemRenderState(isDisabled, isIconOnly, buttonClass.ToString(), isIconOnly ? GetItemAriaLabel(item) : null, IsSelectable ? GetAriaPressed(isSelected) : null, isIconOnly ? "nt-icon-button-icon" : "nt-button-icon", isIconOnly ? null : GetItemLabel(item));
    }

    private async Task HandleItemClickAsync(NTButtonGroupItem<TObjectType> item) {
        if (Disabled || item.Disabled) {
            return;
        }

        if (!IsSelectable) {
            await OnSelectionChanged.InvokeAsync(item);
            return;
        }

        if (IsMultiSelect) {
            await UpdateMultiSelectionAsync(item);
        }
        else {
            await UpdateSingleSelectionAsync(item);
        }
    }

    private void EnsureEffectiveColors() {
        if (!_backgroundColorWasProvided || !BackgroundColor.HasValue) {
            BackgroundColor = GetDefaultBackgroundColor();
        }

        if (!_textColorWasProvided || !TextColor.HasValue) {
            TextColor = GetDefaultTextColor();
        }

        if (!_selectedBackgroundColorWasProvided || !SelectedBackgroundColor.HasValue) {
            SelectedBackgroundColor = GetDefaultSelectedBackgroundColor();
        }

        if (!_selectedTextColorWasProvided || !SelectedTextColor.HasValue) {
            SelectedTextColor = GetDefaultSelectedTextColor();
        }
    }

    private TnTColor GetDefaultBackgroundColor() {
        return Variant switch {
            NTButtonVariant.Elevated => TnTColor.SurfaceContainerLow,
            NTButtonVariant.Filled => TnTColor.SurfaceContainer,
            NTButtonVariant.Tonal => TnTColor.SecondaryContainer,
            NTButtonVariant.Outlined => TnTColor.Transparent,
            NTButtonVariant.Text => TnTColor.Transparent,
            _ => throw new ArgumentOutOfRangeException(nameof(Variant), Variant, null)
        };
    }

    private TnTColor GetDefaultSelectedBackgroundColor() {
        return Variant switch {
            NTButtonVariant.Elevated => TnTColor.Primary,
            NTButtonVariant.Filled => TnTColor.Primary,
            NTButtonVariant.Tonal => TnTColor.Secondary,
            NTButtonVariant.Outlined => TnTColor.InverseSurface,
            NTButtonVariant.Text => TnTColor.Transparent,
            _ => throw new ArgumentOutOfRangeException(nameof(Variant), Variant, null)
        };
    }

    private TnTColor GetDefaultSelectedTextColor() {
        return Variant switch {
            NTButtonVariant.Elevated => TnTColor.OnPrimary,
            NTButtonVariant.Filled => TnTColor.OnPrimary,
            NTButtonVariant.Tonal => TnTColor.OnSecondary,
            NTButtonVariant.Outlined => TnTColor.InverseOnSurface,
            NTButtonVariant.Text => TnTColor.Primary,
            _ => throw new ArgumentOutOfRangeException(nameof(Variant), Variant, null)
        };
    }

    private TnTColor GetDefaultTextColor() {
        return Variant switch {
            NTButtonVariant.Elevated => TnTColor.Primary,
            NTButtonVariant.Filled => TnTColor.OnSurfaceVariant,
            NTButtonVariant.Tonal => TnTColor.OnSecondaryContainer,
            NTButtonVariant.Outlined => TnTColor.OnSurfaceVariant,
            NTButtonVariant.Text => TnTColor.Primary,
            _ => throw new ArgumentOutOfRangeException(nameof(Variant), Variant, null)
        };
    }

    private static bool IsTransparentContainerColor(TnTColor color) => color is TnTColor.None or TnTColor.Transparent;

    private static bool IsVisibleContentColor(TnTColor? color) => color is not (null or TnTColor.None or TnTColor.Transparent);

    private void ValidateBackgroundColorForVariant() {
        if (!BackgroundColor.HasValue) {
            return;
        }

        if (Variant is NTButtonVariant.Text or NTButtonVariant.Outlined) {
            if (BackgroundColor != TnTColor.Transparent) {
                throw new InvalidOperationException($"{Variant} button groups must use a transparent {nameof(BackgroundColor)}.");
            }

            return;
        }

        if (IsTransparentContainerColor(BackgroundColor.Value)) {
            throw new InvalidOperationException($"{Variant} button groups must use a visible container {nameof(BackgroundColor)}.");
        }
    }

    private void ValidateSelectedBackgroundColorForVariant() {
        if (!SelectedBackgroundColor.HasValue || !IsSelectable) {
            return;
        }

        if (Variant == NTButtonVariant.Text) {
            if (SelectedBackgroundColor != TnTColor.Transparent) {
                throw new InvalidOperationException($"{Variant} button groups must use a transparent {nameof(SelectedBackgroundColor)}.");
            }

            return;
        }

        if (IsTransparentContainerColor(SelectedBackgroundColor.Value)) {
            throw new InvalidOperationException($"{Variant} selected button groups must use a visible container {nameof(SelectedBackgroundColor)}.");
        }
    }

    private void ValidateVariantColorCombination() {
        if (Variant == NTButtonVariant.Text && IsSelectable) {
            throw new InvalidOperationException("Text button groups do not support selectable behavior.");
        }

        ValidateBackgroundColorForVariant();
        ValidateSelectedBackgroundColorForVariant();

        if (!IsVisibleContentColor(TextColor)) {
            throw new InvalidOperationException($"{nameof(TextColor)} must be a visible content color.");
        }

        if (IsSelectable && !IsVisibleContentColor(SelectedTextColor)) {
            throw new InvalidOperationException($"{nameof(SelectedTextColor)} must be a visible selected content color.");
        }
    }

    private bool IsButtonDisabled(NTButtonGroupItem<TObjectType> item) => Disabled || item.Disabled;

    private bool IsSelectedItem(NTButtonGroupItem<TObjectType> item) => IsMultiSelect
        ? IsSelectable && _selectedKeySet.Contains(item.Key)
        : IsSelectable && SelectedKey is not null && KeyComparer.Equals(SelectedKey, item.Key);

    private void RebuildSelectedKeySet() {
        _selectedKeySet = SelectedKeys.Count == 0
            ? new HashSet<TObjectType>(KeyComparer)
            : new HashSet<TObjectType>(SelectedKeys, KeyComparer);
    }

    private void SetSelectedKeys(IReadOnlyCollection<TObjectType> selectedKeys) {
        SelectedKeys = selectedKeys;
        RebuildSelectedKeySet();
    }

    private async Task UpdateMultiSelectionAsync(NTButtonGroupItem<TObjectType> item) {
        var selectedKeys = SelectedKeys.ToList();
        var selectedIndex = selectedKeys.FindIndex(key => EqualityComparer<TObjectType>.Default.Equals(key, item.Key));

        if (selectedIndex >= 0) {
            if (SelectionRequired && selectedKeys.Count == 1) {
                return;
            }

            selectedKeys.RemoveAt(selectedIndex);
        }
        else {
            selectedKeys.Add(item.Key);
        }

        SetSelectedKeys(selectedKeys);
        await SelectedKeysChanged.InvokeAsync(SelectedKeys);

        if (EditContext is not null && _fieldIdentifier.FieldName is not null) {
            EditContext.NotifyFieldChanged(_fieldIdentifier);
        }

        await OnSelectionChanged.InvokeAsync(item);
    }

    private async Task UpdateSelectionAsync(TObjectType? nextKey, NTButtonGroupItem<TObjectType>? item = null) {
        if ((nextKey is null && SelectedKey is null) || nextKey?.Equals(SelectedKey) == true) {
            return;
        }

        SelectedKey = nextKey;

        await SelectedKeyChanged.InvokeAsync(nextKey);

        if (EditContext is not null && _fieldIdentifier.FieldName is not null) {
            EditContext.NotifyFieldChanged(_fieldIdentifier);
        }

        if (item is not null) {
            await OnSelectionChanged.InvokeAsync(item);
        }
    }

    private async Task UpdateSingleSelectionAsync(NTButtonGroupItem<TObjectType> item) {
        var isSelected = IsSelectedItem(item);
        var nextKey = isSelected && !SelectionRequired ? default : item.Key;
        await UpdateSelectionAsync(nextKey, item);
    }

    private readonly record struct ItemRenderState(bool IsDisabled, bool IsIconOnly, string ButtonClass, string? AriaLabel, string? AriaPressed, string IconClass, string? Label);
}

/// <summary>
/// Controls how the buttons in an <see cref="NTButtonGroup{TObjectType}" /> relate to each other.
/// </summary>
/// <remarks>
/// Button groups organize related buttons and add shape, motion, and width interactions between them. Choose
/// <see cref="Disconnected" /> when buttons should remain visually separate, or <see cref="Connected" /> when adjacent
/// buttons should read as one compact set.
/// </remarks>
public enum NTButtonGroupDisplayType {

    /// <summary>
    /// Renders the disconnected button group variant with visible space between buttons.
    /// </summary>
    /// <remarks>
    /// Use <see cref="Disconnected" /> for the default button-group presentation: related actions or choices that
    /// should feel grouped, but still read as individual buttons. Prefer it when the actions do not replace a segmented
    /// control, when the options mix different button or icon-button affordances, or when the surrounding layout
    /// benefits from clearer separation. This variant still supports the button group shape, motion, and width
    /// interactions described by Material 3.
    /// </remarks>
    Disconnected = 0,

    /// <summary>
    /// Renders the Material 3 connected button group variant with adjacent buttons visually joined.
    /// </summary>
    /// <remarks>
    /// Use <see cref="Connected" /> when the buttons behave like a compact set of mutually related options, filters,
    /// sort modes, or view switches. Material 3 positions connected button groups as the replacement for segmented
    /// buttons, which are no longer recommended in the Expressive update. Prefer this variant for single-select,
    /// multi-select, or selection-required groups where the shared boundary communicates that all buttons belong to one
    /// choice set.
    /// </remarks>
    Connected = 1
}

/// <summary>
/// Controls whether an <see cref="NTButtonGroup{TObjectType}" /> allows one or many selected items.
/// </summary>
public enum NTButtonGroupSelectionMode {

    /// <summary>
    /// A maximum of one item can be selected.
    /// </summary>
    Single,

    /// <summary>
    /// Multiple items can be selected.
    /// </summary>
    Multiple,

    /// <summary>
    /// Items behave as action buttons without selected toggle state.
    /// </summary>
    None
}
