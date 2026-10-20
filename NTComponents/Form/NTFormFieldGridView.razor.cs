using Microsoft.AspNetCore.Components;
using NTComponents.CodeDocumentation;
using NTComponents.Core;

namespace NTComponents;

/// <summary>
///     Responsive layout primitive for NT form fields.
/// </summary>
/// <remarks>
///     Use <see cref="NTFormFieldGridView" /> inside forms or form sections when fields should reflow from a single column to
///     balanced multi-column rows. The grid owns only layout: it does not create a form, validate values, or change input
///     behavior. Direct children get the default responsive span; wrap a field in <see cref="NTFormFieldLayoutSpan" />
///     when it needs a different span.
/// </remarks>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.SsrCompatible,
    CompatibilitySummary = "Renders static responsive field layout markup.",
    CompatibilityDetails = "The grid is CSS-only and does not require Blazor interactivity. Descendant fields keep their own render-mode requirements.")]
public partial class NTFormFieldGridView : NTComponentBase {
    private readonly Dictionary<string, object> _elementAttributes = [];
    private string? _elementClass;
    private string? _elementStyle;
    private int _effectiveMaxColumns = 3;

    /// <summary>
    ///     Fields or layout spans rendered as direct grid children.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    ///     Maximum number of default-width fields per row at expanded widths.
    /// </summary>
    /// <remarks>
    ///     Values are normalized to 1, 2, 3, 4, or 6. The default of 3 keeps ordinary text fields readable while still using
    ///     expanded horizontal space efficiently.
    /// </remarks>
    [Parameter]
    public int MaxColumns { get; set; } = 3;

    /// <summary>
    ///     Gap between columns. Defaults to <c>16px</c>.
    /// </summary>
    [Parameter]
    public string ColumnGap { get; set; } = "16px";

    /// <summary>
    ///     Gap between rows. Defaults to <c>16px</c>.
    /// </summary>
    [Parameter]
    public string RowGap { get; set; } = "16px";

    /// <inheritdoc />
    public override string? ElementClass => _elementClass;

    /// <inheritdoc />
    public override string? ElementStyle => _elementStyle;

    private IReadOnlyDictionary<string, object> ElementAttributes => _elementAttributes;

    /// <inheritdoc />
    protected override void OnParametersSet() {
        base.OnParametersSet();

        _effectiveMaxColumns = NormalizeMaxColumns(MaxColumns);
        _elementClass = CssClassBuilder.Create()
            .AddFromAdditionalAttributes(AdditionalAttributes)
            .AddClass("nt-form-field-grid-view")
            .AddClass($"nt-form-field-grid-view-max-{_effectiveMaxColumns}")
            .Build();

        _elementStyle = CssStyleBuilder.Create()
            .AddFromAdditionalAttributes(AdditionalAttributes)
            .AddVariable("nt-form-field-grid-view-column-gap", ColumnGap, !string.IsNullOrWhiteSpace(ColumnGap))
            .AddVariable("nt-form-field-grid-view-row-gap", RowGap, !string.IsNullOrWhiteSpace(RowGap))
            .Build();

        BuildElementAttributes();
    }

    private void BuildElementAttributes() => NTFormElementAttributes.Build(_elementAttributes, AdditionalAttributes, _elementClass!, _elementStyle, AutoFocus, ElementId, ElementLang, ElementTitle);

    private static int NormalizeMaxColumns(int maxColumns) =>
        maxColumns switch {
            <= 1 => 1,
            2 => 2,
            3 => 3,
            4 or 5 => 4,
            _ => 6
        };
}
