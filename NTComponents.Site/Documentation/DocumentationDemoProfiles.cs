using NTComponents.GeneratedDocumentation;
using System.Text.RegularExpressions;

namespace NTComponents.Site.Documentation;

internal static class DocumentationDemoProfiles {
    private static readonly IReadOnlyDictionary<string, string[]> PreferredParameters = new Dictionary<string, string[]>(StringComparer.Ordinal) {
        ["NTAccordion"] = ["Variant", "Appearance", "LimitToOneExpanded", "GroupName", "Separated"],
        ["NTAnimation"] = ["Animation", "Once", "Delay", "EnterDuration", "EnterEasing", "AnimateOut", "ExitDuration", "ExitEasing"],
        ["NTAutocomplete"] = ["AllowCustomValue", "MenuItemAppearance", "EmptyText", "CustomValueOptionFormat"],
        ["NTButton"] = ["Label", "Variant", "LeadingIcon", "Shape", "Elevation", "IsToggleButton", "Selected"],
        ["NTButtonGroup"] = ["SelectionMode", "SelectedKey", "DisplayType", "Variant", "ButtonSize", "SelectionRequired", "Disabled", "FullWidth"],
        ["NTCard"] = ["Variant", "Elevation", "CornerRadius"],
        ["NTCarousel"] = ["Appearance", "PreferredItemWidth", "ItemHeight", "AllowDragging", "EnableSnapping", "AutoPlayInterval", "AriaLabel"],
        ["NTChip"] = ["Label", "Variant", "LeadingIcon", "Selectable", "Selected", "Removable", "Appearance", "Disabled"],
        ["NTCombobox"] = ["Searchable", "MenuItemAppearance", "SelectedTextSeparator", "EmptyText"],
        ["NTContainerView"] = ["EnableOnThisPageNavigation", "OnThisPageLabel", "OnThisPageTitle", "OnThisPageAriaLabel"],
        ["NTContextMenu"] = ["AriaLabel", "Appearance", "CloseOnContentClick", "Disabled", "LongPressDelay", "Elevation"],
        ["NTDataGrid"] = ["Caption", "Appearance", "Density", "ShowPagination", "PageSize", "PageSizeOptions", "AllowMultiSort", "Virtualize"],
        ["NTDialog"] = ["Title", "SupportingText", "ShowCloseButton", "CloseOnBackdrop", "CloseOnEscape", "ButtonSpacing", "Elevation", "Open"],
        ["NTDivider"] = ["Direction", "Variant", "Color"],
        ["NTFabButton"] = ["Label", "AriaLabel", "ButtonSize", "Placement", "Elevation"],
        ["NTFabMenu"] = ["AriaLabel", "Placement", "ButtonSize", "Expanded", "CloseOnMenuContentClick", "Disabled", "Elevation"],
        ["NTFeedView"] = ["MinItemWidth"],
        ["NTFileUpload"] = ["ChooseButtonText", "Multiple", "Accept", "MaximumFileCount", "MaximumFileSize", "AutoUpload", "ShowUploadButton", "UploadButtonText"],
        ["NTForm"] = ["FormName", "Enhance"],
        ["NTFormFieldGridView"] = ["MaxColumns", "ColumnGap", "RowGap"],
        ["NTFormFieldLayoutSpan"] = ["Span", "SmallColumns", "MediumColumns", "LargeColumns"],
        ["NTFormSectionView"] = ["Heading", "Description", "UseFieldset", "MaxColumns"],
        ["NTHeadDependencies"] = [],
        ["NTIconButton"] = ["AriaLabel", "Icon", "Variant", "Shape", "Width", "Elevation", "IsToggleButton", "Selected"],
        ["NTInputCheckbox"] = ["Label", "Indeterminate", "Variant"],
        ["NTInputColor"] = ["Label"],
        ["NTInputCurrency"] = ["Label"],
        ["NTInputDateTime"] = ["Label", "Format", "EnableCustomPicker", "MonthOnly", "PickerTriggerIcon"],
        ["NTInputNumeric"] = ["Label"],
        ["NTInputRadioGroup"] = ["Label", "GroupName", "Appearance"],
        ["NTInputRangeSlider"] = ["Label", "Min", "Max", "Step", "Size", "ShowValueIndicator", "ShowStops", "StartHandleLabel", "EndHandleLabel"],
        ["NTInputSlider"] = ["Label", "Min", "Max", "Step", "Variant", "Orientation", "Size", "ShowValueIndicator", "ShowStops"],
        ["NTInputSwitch"] = ["Label", "Variant", "ShowHandleIcon"],
        ["NTInputText"] = ["Label", "InputType", "PhoneMask"],
        ["NTLayout"] = [],
        ["NTListDetailView"] = ["Mode", "DetailVisible", "ListPaneLabel", "DetailPaneLabel"],
        ["NTLoader"] = ["Variant", "Size", "Color", "Animate", "Show", "AriaLabel", "AnimationDuration"],
        ["NTMenu"] = ["AriaLabel", "Appearance", "CloseOnContentClick", "Disabled", "Elevation", "AnchorName", "AnchorSelector"],
        ["NTMultiPaneView"] = ["PaneCount", "MinPaneWidth", "EnforceEvenSizing"],
        ["NTNavigationRail"] = ["OpenByDefault", "CollapseBehavior", "IndicatorStyle", "ShowDivider", "HideRailOnXSScreens", "Elevation", "MenuAriaLabel"],
        ["NTNavLink"] = ["Label", "Href", "Variant", "LeadingIcon", "ButtonSize", "Shape", "Disabled", "Elevation"],
        ["NTProgress"] = ["Value", "Variant", "Size", "Show", "Animate", "TrackVisible", "AriaLabel", "Max"],
        ["NTRichTextEditor"] = ["Label", "StartIcon", "EndIcon", "ShowInputLength", "ErrorMessage", "ToolbarButtons", "Tools"],
        ["NTScheduler"] = ["View", "Date", "StartViewOn", "ShowDescription", "HideDateControls", "AllowDraggingEvents", "AutoUpdateEventsOnDrop", "HourRowHeight", "AriaLabel"],
        ["NTSelect"] = ["Label"],
        ["NTShape"] = ["Shape", "AnimateShapeChanges", "TransitionDuration", "TransitionEasing"],
        ["NTSkeleton"] = ["Shape", "Width", "Height", "Animation", "CornerRadius", "Show", "HideFromAssistiveTechnology"],
        ["NTSnackbar"] = ["Position"],
        ["NTSplitButton"] = ["Label", "LeadingIcon", "Variant", "ButtonSize", "Shape", "MenuButtonLabel", "Expanded", "Disabled", "Elevation"],
        ["NTSupportingPaneView"] = ["Mode", "PrimaryPaneLabel", "SupportingPaneLabel"],
        ["NTTabView"] = ["SelectedValue", "Variant", "TabAlignment", "FullWidth", "Compact", "TabGap", "AriaLabel", "UpdateQueryString"],
        ["NTTag"] = ["Label", "TextAlignment", "Elevation"],
        ["NTTextArea"] = ["Label", "AutoGrow", "MinVisibleLines", "MaxVisibleLines", "SizeByContent"],
        ["NTThemeToggle"] = ["DefaultTheme", "DefaultContrast", "AllowThemeSelection", "AllowContrastSelection", "Hide"],
        ["NTToast"] = ["Position"],
        ["NTTooltip"] = ["Variant", "ShowDelay", "HideDelay"],
        ["NTTypeahead"] = ["SearchText", "MinimumSearchLength", "MaxResults", "DebounceMilliseconds", "MenuItemAppearance", "EmptyText", "LoadingText", "ResetSelectionOnInput", "ResetValueOnEscape"],
        ["NTVirtualize"] = ["ItemSize", "OverscanCount", "MaxItemCount", "PlaceholderPreloadWindowCount", "BackgroundPreloadWindowCount", "RevalidateCachedItems", "MaxCachedItemCount", "ScrollRestorationKey"],
        ["NTWindow"] = ["Title", "Open", "State", "DockPosition", "Draggable", "CloseButtonAriaLabel", "MinimizeButtonAriaLabel", "FullscreenButtonAriaLabel"],
        ["NTWindowHost"] = ["AriaLabel"],
        ["NTWizard"] = ["Title", "ActiveStepIndex", "NavigationMode", "LayoutDirection", "VerticalOnSmallScreens", "AllowSkippingOptionalSteps", "InvalidFormButtonBehavior", "PushNavigationToBottom"]
    };

    private static readonly string[] GeneralPriority = [
        "Label", "Title", "Value", "SelectedValue", "SearchText", "Heading", "Description", "SupportingText", "Placeholder", "Caption", "Variant", "Appearance", "Mode", "Shape", "Size", "ButtonSize", "Disabled", "ReadOnly", "Required", "Selected", "Open"
    ];

    public static SandboxParameterDocumentation Apply(string componentName, SandboxParameterDocumentation parameter) => parameter with {
        ControlGroup = ResolveGroup(parameter),
        ControlOrder = ResolveOrder(componentName, parameter)
    };

    public static string BuildStringDefault(string componentName, PropertyDocumentation parameter, string? componentDefault) {
        var configuredValue = (componentName, parameter.Name) switch {
            ("NTAccordion", "GroupName") => "docs-accordion-example",
            ("NTAnimation", "RootMargin") => "0px 0px -10% 0px",
            ("NTButton", "Label") => "Save changes",
            ("NTDialog", "CloseButtonAriaLabel") => "Close dialog",
            ("NTDialog", "Id") => "docs-example-dialog",
            ("NTDialog", "SupportingText") => "Confirm the changes before continuing.",
            ("NTDialog", "Title") => "Review changes",
            ("NTFabButton", "Label") => "Create",
            ("NTFileUpload", "ChooseButtonText") => "Choose files",
            ("NTFormSectionView", "Description") => "Provide the information used to contact you.",
            ("NTFormSectionView", "Heading") => "Contact details",
            ("NTMenu", "AnchorName") => "--docs-menu-anchor",
            ("NTMenu", "AnchorSelector") => "#docs-menu-trigger",
            ("NTSkeleton", "Height") => "6rem",
            ("NTSkeleton", "Width") => "100%",
            ("NTSplitButton", "Label") => "Save",
            ("NTSplitButton", "MenuButtonLabel") => "More save options",
            ("NTTabView", "Id") => "docs-example-tabs",
            ("NTTag", "Label") => "New",
            ("NTWindow", "Title") => "Project notes",
            ("NTWizard", "Title") => "Create your profile",
            _ => null
        };
        if (configuredValue is not null) {
            return configuredValue;
        }

        if (!string.IsNullOrWhiteSpace(componentDefault)) {
            return componentDefault;
        }

        var componentLabel = BuildFriendlyName(componentName);
        return parameter.Name switch {
            "AriaLabel" => $"{componentLabel} example",
            "ElementTitle" => $"Example {componentLabel.ToLowerInvariant()}",
            "EmptyText" => "No matching items",
            "Href" when parameter.IsEditorRequired => "#example",
            "Label" => $"{componentLabel} example",
            "Placeholder" => "Enter a value",
            "Src" => "./_content/NTComponents/NTComponents.lib.module.js",
            "SupportingText" => "Helpful context for this example.",
            "Title" => $"Example {componentLabel.ToLowerInvariant()}",
            _ => string.Empty
        };
    }

    public static string BuildChildContent(string componentName) => componentName switch {
        "NTCard" => "Project status and recent activity.",
        "NTContainerView" => "A document-like page with related sections.",
        "NTDivider" => string.Empty,
        "NTFeedView" => "A responsive collection of recent updates.",
        "NTShape" => "Featured",
        "NTTag" => "New",
        _ => $"{BuildFriendlyName(componentName)} content"
    };

    public static string BuildInputValue(string componentName) => componentName switch {
        "NTInputText" => "Ada Lovelace",
        "NTRichTextEditor" => "A concise example with meaningful content.",
        "NTTextArea" => "A short example message.",
        _ => "Example value"
    };

    public static object BuildNumericDefault(string componentName, string parameterName, Type valueType, object? componentDefault) =>
        (componentName, parameterName) switch {
            ("NTCarousel", "AutoPlayInterval") => 5d,
            ("NTCarousel", "MaxLargeItemWidth") => 360,
            _ => componentDefault ?? Activator.CreateInstance(valueType)!
        };

    private static SandboxControlGroup ResolveGroup(SandboxParameterDocumentation parameter) {
        var name = parameter.Property.Name;
        if (!parameter.IsSupported || IsAdvanced(name)) {
            return SandboxControlGroup.Advanced;
        }

        if (name.StartsWith("Aria", StringComparison.Ordinal) || name is "Role" or "HideFromAssistiveTechnology" or "UseRegionRole") {
            return SandboxControlGroup.Accessibility;
        }

        if (IsContent(name)) {
            return SandboxControlGroup.Content;
        }

        if (IsAppearance(name)) {
            return SandboxControlGroup.Appearance;
        }

        return SandboxControlGroup.Behavior;
    }

    private static int ResolveOrder(string componentName, SandboxParameterDocumentation parameter) {
        var requiredOffset = parameter.Property.IsEditorRequired ? -1_000 : 0;
        if (PreferredParameters.TryGetValue(componentName, out var preferred)) {
            var preferredIndex = Array.IndexOf(preferred, parameter.Property.Name);
            if (preferredIndex >= 0) {
                return requiredOffset + preferredIndex;
            }
        }

        var generalIndex = Array.IndexOf(GeneralPriority, parameter.Property.Name);
        var order = generalIndex >= 0 ? 100 + generalIndex : 500;
        if (parameter.Property.Name.EndsWith("Color", StringComparison.Ordinal)) {
            order += 200;
        }

        if (!parameter.IsSupported) {
            order += 500;
        }

        return requiredOffset + order;
    }

    private static bool IsContent(string name) => name is
        "Accept" or "Caption" or "ChooseButtonText" or "Description" or "EmptyText" or "Heading" or "Label" or "LoadingText" or "MenuButtonLabel" or "NoResultsText" or "Placeholder" or "ReadyText" or "SearchText" or "SelectedTextSeparator" or "SupportingText" or "Title" or "TodayButtonLabel" or "UploadButtonText";

    private static bool IsAppearance(string name) =>
        name.EndsWith("Color", StringComparison.Ordinal) ||
        name.EndsWith("Icon", StringComparison.Ordinal) ||
        name.EndsWith("Size", StringComparison.Ordinal) ||
        name is "Animation" or "Appearance" or "ButtonSpacing" or "Compact" or "CornerRadius" or "Density" or "Direction" or "DisplayType" or "Elevation" or "FullWidth" or "Height" or "IndicatorStyle" or "LayoutDirection" or "MenuItemAppearance" or "Orientation" or "Placement" or "Shape" or "TabAlignment" or "TextAlignment" or "Variant" or "Width";

    private static bool IsAdvanced(string name) =>
        name.StartsWith("Element", StringComparison.Ordinal) ||
        name.EndsWith("Expression", StringComparison.Ordinal) ||
        name.EndsWith("Parser", StringComparison.Ordinal) ||
        name.EndsWith("Selector", StringComparison.Ordinal) ||
        name.EndsWith("Template", StringComparison.Ordinal) ||
        name.Contains("Css", StringComparison.Ordinal) ||
        name.Contains("QueryParameter", StringComparison.Ordinal) ||
        name is "AdditionalAttributes" or "AnchorName" or "AnchorSelector" or "Comparer" or "FormName" or "Id" or "ItemsProvider" or "Name" or "Popover" or "Role" or "RowKey" or "ScrollRestorationKey" or "SelectedKeys" or "SpacerElement" or "ThemesRoot" or "TimeZone";

    private static string BuildFriendlyName(string componentName) {
        var name = componentName.StartsWith("NT", StringComparison.Ordinal) ? componentName[2..] : componentName;
        return Regex.Replace(name, "(?<=[a-z0-9])(?=[A-Z])", " ");
    }
}
