using Microsoft.AspNetCore.Components;
using NTComponents.Core;

using NTComponents.CodeDocumentation;
namespace NTComponents;

/// <summary>
///     Content pane and header metadata for <see cref="NTTabView" />.
/// </summary>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.ProgressivelyEnhanced,
    CompatibilitySummary = "Participates in parent component rendering and inherits the parent interaction model.",
    CompatibilityDetails = "The selected panel content is present in the static response. Tab switching, query-string activation after load, and keyboard navigation depend on the parent tab-view script.")]
public partial class NTTab {
    private int _sequence;

    /// <summary>
    ///     Gets or sets the content rendered inside the tab panel.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    ///     Gets or sets whether the tab is disabled.
    /// </summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <inheritdoc />
    public override string? ElementClass => CssClassBuilder.Create("nt-tab-view-panel")
        .AddClass("nt-tab-view-panel-selected", IsInitiallySelected)
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .Build();

    /// <inheritdoc />
    public override string? ElementStyle => CssStyleBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .Build();

    /// <summary>
    ///     Gets or sets optional accessible label text for icon-only or otherwise ambiguous tabs.
    /// </summary>
    [Parameter]
    public string? AccessibilityLabel { get; set; }

    /// <summary>
    ///     Gets or sets optional aria-label text for icon-only or otherwise ambiguous tabs.
    /// </summary>
    [Parameter]
    public string? AriaLabel { get; set; }

    /// <summary>
    ///     Gets or sets optional tooltip content for the tab header.
    /// </summary>
    [Parameter]
    public RenderFragment? HeaderTooltip { get; set; }

    /// <summary>
    ///     Gets or sets the icon displayed above or beside the tab label.
    /// </summary>
    [Parameter]
    public TnTIcon? Icon { get; set; }

    /// <summary>
    ///     Gets or sets the visible tab label.
    /// </summary>
    [Parameter, EditorRequired]
    public string Label { get; set; } = default!;

    /// <summary>
    ///     Gets or sets a stable value used by query-string activation and tab selection. Defaults to <see cref="ElementName" />, then a normalized <see cref="Label" />.
    /// </summary>
    [Parameter]
    public string? Value { get; set; }

    /// <inheritdoc />
    [Parameter]
    public string? ElementName { get; set; }

    [CascadingParameter]
    private NTTabView _context { get; set; } = default!;

    internal bool IsInitiallySelected => _context.IsInitiallySelected(this);

    internal string? ResolvedAriaLabel => AriaLabel ?? AccessibilityLabel;

    internal string ResolvedPanelId => ElementId ?? $"{_context.ResolvedElementId}-panel-{ResolvedDomValue}-{ResolvedIndex}";

    internal int ResolvedSequence {
        get => _sequence;
        set => _sequence = value;
    }

    internal string ResolvedTabId => $"{_context.ResolvedElementId}-tab-{ResolvedDomValue}-{ResolvedIndex}";

    internal string ResolvedValue => !string.IsNullOrWhiteSpace(Value) ? Value : !string.IsNullOrWhiteSpace(ElementName) ? ElementName : NormalizeValue(Label);

    private string ResolvedDomValue => NormalizeValue(ResolvedValue);

    private string ResolvedIndex => Math.Max(0, _context.GetTabIndex(this)).ToString();

    /// <inheritdoc />
    public void Dispose() {
        _context?.RemoveTab(this);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    protected override void OnInitialized() {
        base.OnInitialized();
        if (_context is null) {
            throw new InvalidOperationException($"An {nameof(NTTab)} must be a child of {nameof(NTTabView)}.");
        }

        _context.AddTab(this);
    }

    /// <inheritdoc />
    protected override void OnParametersSet() {
        base.OnParametersSet();
        _context?.SetTabSequence(this, _context.GetNextRenderSequence());
    }

    private static string NormalizeValue(string value) {
        var normalized = new string(value.Trim().ToLowerInvariant().Select(character => char.IsLetterOrDigit(character) ? character : '-').ToArray()).Trim('-');
        while (normalized.Contains("--", StringComparison.Ordinal)) {
            normalized = normalized.Replace("--", "-", StringComparison.Ordinal);
        }

        return string.IsNullOrWhiteSpace(normalized) ? "tab" : normalized;
    }
}
