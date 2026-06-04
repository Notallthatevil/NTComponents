using Microsoft.AspNetCore.Components;
using NTComponents.Core;

using NTComponents.CodeDocumentation;
namespace NTComponents;

/// <summary>
///     Material 3 tab view that renders static tab markup and enhances selection, query-string activation, keyboard behavior, and active-indicator placement with TypeScript.
/// </summary>
/// <remarks>
///     <para>
///         Use <see cref="NTTabView" /> to organize related content panes as peer destinations. The initial HTML remains usable during static SSR, and the browser module progressively enhances the
///         rendered markup without requiring Blazor event handlers.
///     </para>
///     <para>
///         Set <see cref="Name" /> to give the tab view a stable query-string key. For example, <c>Name="details"</c> lets <c>?details=history</c> select the tab whose
///         <see cref="NTTab.Value" /> is <c>history</c>. Use <see cref="QueryParameterName" /> when the URL key should differ from the rendered name.
///     </para>
/// </remarks>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.ProgressivelyEnhanced,
    CompatibilitySummary = "Renders tab markup and enhances selection with TypeScript.",
    CompatibilityDetails = "Static SSR emits the tablist and initially selected panel. The browser module adds tab switching, query-string updates, keyboard behavior, and active-indicator placement.")]
public partial class NTTabView {
    /// <summary>
    ///     Gets the isolated JavaScript module path for <see cref="NTTabView" />.
    /// </summary>
    public const string JsModulePathValue = "./_content/NTComponents/TabView/NTTabView.razor.js";

    internal readonly List<NTTab> _tabs = [];

    private readonly Dictionary<NTTab, int> _tabIndexes = [];
    private readonly List<NTTab> _orderedTabs = [];
    private int _renderSequence;

    private string? ResolvedQueryParameterName => QueryParameterName ?? Name;

    private string UpdateQueryAttribute => UpdateQueryString ? "true" : "false";

    internal string ResolvedElementId => Id ?? ElementId ?? ComponentIdentifier;

    internal IReadOnlyList<NTTab> OrderedTabs => _orderedTabs;

    internal int GetTabIndex(NTTab tab) => _tabIndexes.GetValueOrDefault(tab, 0);

    internal int GetNextRenderSequence() => _renderSequence++;

    internal void ResetRenderSequence() => _renderSequence = 0;

    internal void SetTabSequence(NTTab tab, int sequence) {
        if (tab.ResolvedSequence == sequence) {
            return;
        }

        tab.ResolvedSequence = sequence;
        RefreshTabOrder();
        if (_tabs.Contains(tab)) {
            _ = InvokeAsync(StateHasChanged);
        }
    }

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    /// <summary>
    ///     Gets or sets the accessible label for the tab list.
    /// </summary>
    [Parameter]
    public string AriaLabel { get; set; } = "Tabs";

    /// <summary>
    ///     Gets or sets the child tabs rendered inside the tab view.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    ///     Gets or sets the tab view element id. This is an alias for component-style usage; the base component element id remains supported.
    /// </summary>
    [Parameter]
    public string? Id { get; set; }

    /// <inheritdoc />
    public override string? ElementClass => CssClassBuilder.Create("nt-tab-view")
        .AddClass("nt-tab-view-secondary", Variant == NTTabViewVariant.Secondary)
        .AddClass("nt-tab-view-compact", Compact)
        .AddClass("nt-tab-view-full-width", FullWidth)
        .AddClass("nt-tab-view-align-center", TabAlignment == NTTabViewTabAlignment.Center)
        .AddClass("nt-tab-view-align-end", TabAlignment == NTTabViewTabAlignment.End)
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .Build();

    /// <inheritdoc />
    public override string? ElementStyle => CssStyleBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddVariable("nt-tab-view-indicator-color", ActiveIndicatorColor)
        .AddVariable("nt-tab-view-tab-gap", TabGap!, !string.IsNullOrWhiteSpace(TabGap))
        .Build();

    /// <summary>
    ///     Gets or sets the tab view name used as the default query-string parameter key and rendered name attribute.
    /// </summary>
    [Parameter]
    public string? Name { get; set; }

    /// <summary>
    ///     Gets or sets the query-string parameter used to activate a tab by value. Defaults to <see cref="Name" />.
    /// </summary>
    [Parameter]
    public string? QueryParameterName { get; set; }

    /// <summary>
    ///     Gets or sets whether user tab selection writes the selected tab value back to the URL query string.
    /// </summary>
    [Parameter]
    public bool UpdateQueryString { get; set; } = true;

    /// <summary>
    ///     Gets or sets the initially selected tab value before query-string enhancement runs.
    /// </summary>
    [Parameter]
    public string? SelectedValue { get; set; }

    /// <summary>
    ///     Gets or sets the tab view variant.
    /// </summary>
    [Parameter]
    public NTTabViewVariant Variant { get; set; } = NTTabViewVariant.Primary;

    /// <summary>
    ///     Gets or sets whether tabs stretch evenly across the available width instead of sizing to content and scrolling when needed.
    /// </summary>
    [Parameter]
    public bool FullWidth { get; set; }

    /// <summary>
    ///     Gets or sets how non-full-width tab items align inside the tab list when there is extra horizontal space.
    /// </summary>
    [Parameter]
    public NTTabViewTabAlignment TabAlignment { get; set; } = NTTabViewTabAlignment.Start;

    /// <summary>
    ///     Gets or sets whether the tab items use a reduced-height compact layout.
    /// </summary>
    [Parameter]
    public bool Compact { get; set; }

    /// <summary>
    ///     Gets or sets the horizontal gap between tab items. Use any valid CSS length, such as <c>8px</c>, <c>0.5rem</c>, or <c>var(--space-2)</c>.
    /// </summary>
    [Parameter]
    public string? TabGap { get; set; }

    /// <summary>
    ///     Gets or sets the active indicator color.
    /// </summary>
    [Parameter]
    public TnTColor ActiveIndicatorColor { get; set; } = TnTColor.Primary;

    internal void AddTab(NTTab tab) {
        if (!_tabs.Contains(tab)) {
            _tabs.Add(tab);
            RefreshTabOrder();
            _ = InvokeAsync(StateHasChanged);
        }
    }

    internal void RemoveTab(NTTab tab) {
        if (_tabs.Remove(tab)) {
            RefreshTabOrder();
            _ = InvokeAsync(StateHasChanged);
        }
    }

    internal bool IsInitiallySelected(NTTab tab) => ReferenceEquals(tab, GetInitiallySelectedTab());

    internal NTTab? GetInitiallySelectedTab() {
        var firstEnabledTab = OrderedTabs.FirstOrDefault(candidate => !candidate.Disabled);
        if (firstEnabledTab is null) {
            return null;
        }

        var selectedValue = GetInitialSelectedValue();
        return !string.IsNullOrWhiteSpace(selectedValue)
            ? OrderedTabs.FirstOrDefault(tab => !tab.Disabled && string.Equals(tab.ResolvedValue, selectedValue, StringComparison.OrdinalIgnoreCase)) ?? firstEnabledTab
            : firstEnabledTab;
    }

    private string? GetInitialSelectedValue() => GetQuerySelectedValue() ?? SelectedValue;

    private string? GetQuerySelectedValue() {
        var parameterName = ResolvedQueryParameterName;
        if (string.IsNullOrWhiteSpace(parameterName) || NavigationManager is null) {
            return null;
        }

        return TryGetQueryValue(NavigationManager.Uri, parameterName, out var value) ? value : null;
    }

    private static bool TryGetQueryValue(string uri, string parameterName, out string? value) {
        value = null;

        var questionMarkIndex = uri.IndexOf('?', StringComparison.Ordinal);
        if (questionMarkIndex < 0 || questionMarkIndex == uri.Length - 1) {
            return false;
        }

        var fragmentIndex = uri.IndexOf('#', questionMarkIndex + 1);
        var query = fragmentIndex < 0 ? uri[(questionMarkIndex + 1)..] : uri[(questionMarkIndex + 1)..fragmentIndex];
        foreach (var segment in query.Split('&', StringSplitOptions.RemoveEmptyEntries)) {
            var equalsIndex = segment.IndexOf('=', StringComparison.Ordinal);
            var rawName = equalsIndex < 0 ? segment : segment[..equalsIndex];
            if (!string.Equals(DecodeQueryValue(rawName), parameterName, StringComparison.OrdinalIgnoreCase)) {
                continue;
            }

            var rawValue = equalsIndex < 0 ? string.Empty : segment[(equalsIndex + 1)..];
            value = DecodeQueryValue(rawValue);
            return !string.IsNullOrWhiteSpace(value);
        }

        return false;
    }

    private static string DecodeQueryValue(string value) => Uri.UnescapeDataString(value.Replace('+', ' '));

    internal void RefreshTabOrder() {
        _orderedTabs.Clear();
        _orderedTabs.AddRange(_tabs.OrderBy(tab => tab.ResolvedSequence));
        _tabIndexes.Clear();
        for (var index = 0; index < _orderedTabs.Count; index++) {
            _tabIndexes[_orderedTabs[index]] = index;
        }
    }

    private string BuildTabClass(NTTab tab, bool selected) {
        return CssClassBuilder.Create("nt-tab-view-tab")
            .AddClass("nt-tab-view-tab-selected", selected)
            .AddClass("nt-tab-view-tab-disabled", tab.Disabled)
            .AddClass("nt-tab-view-tab-with-icon", tab.Icon is not null)
            .Build();
    }
}

/// <summary>
///     Material 3 tab view variants.
/// </summary>
public enum NTTabViewVariant {
    /// <summary>
    ///     Primary tabs for top-level app destinations or major content groups.
    /// </summary>
    Primary,

    /// <summary>
    ///     Secondary tabs for related content inside an existing content area.
    /// </summary>
    Secondary
}

/// <summary>
///     Horizontal alignment options for tab items inside an <see cref="NTTabView" />.
/// </summary>
public enum NTTabViewTabAlignment {
    /// <summary>
    ///     Aligns tabs to the inline start.
    /// </summary>
    Start,

    /// <summary>
    ///     Centers tabs when the tab list has extra horizontal space.
    /// </summary>
    Center,

    /// <summary>
    ///     Aligns tabs to the inline end when the tab list has extra horizontal space.
    /// </summary>
    End
}
