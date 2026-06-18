using Microsoft.AspNetCore.Components;
using NTComponents.CodeDocumentation;
using NTComponents.Core;

namespace NTComponents;

/// <summary>
///     Span override for one child inside <see cref="NTFormFieldGridView" /> or <see cref="NTFormSectionView" />.
/// </summary>
/// <remarks>
///     Wrap controls that need more or less room than the grid default, such as full-width notes, compact state/ZIP fields,
///     or wide upload/editor controls. This component only changes layout span and leaves the child control behavior intact.
/// </remarks>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.SsrCompatible,
    CompatibilitySummary = "Renders a static form field layout span wrapper.",
    CompatibilityDetails = "The override emits CSS custom properties consumed by NTFormFieldGridView and does not require Blazor interactivity.")]
public partial class NTFormFieldLayoutSpan : NTComponentBase {
    private readonly Dictionary<string, object> _elementAttributes = [];
    private string? _elementClass;
    private string? _elementStyle;

    /// <summary>
    ///     Field content rendered inside the layout span.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    ///     Preset responsive span for this field.
    /// </summary>
    [Parameter]
    public NTFormFieldSpan Span { get; set; } = NTFormFieldSpan.Auto;

    /// <summary>
    ///     Optional explicit span, from 1 to 12 columns, used below medium widths.
    /// </summary>
    [Parameter]
    public int? SmallColumns { get; set; }

    /// <summary>
    ///     Optional explicit span, from 1 to 12 columns, used at medium widths.
    /// </summary>
    [Parameter]
    public int? MediumColumns { get; set; }

    /// <summary>
    ///     Optional explicit span, from 1 to 12 columns, used at expanded widths.
    /// </summary>
    [Parameter]
    public int? LargeColumns { get; set; }

    /// <inheritdoc />
    public override string? ElementClass => _elementClass;

    /// <inheritdoc />
    public override string? ElementStyle => _elementStyle;

    private IReadOnlyDictionary<string, object> ElementAttributes => _elementAttributes;

    /// <inheritdoc />
    protected override void OnParametersSet() {
        base.OnParametersSet();

        var span = ResolveSpan(Span);
        var smallSpan = NormalizeColumns(SmallColumns ?? span.Small);
        var mediumSpan = NormalizeColumns(MediumColumns ?? span.Medium);
        var largeSpan = NormalizeColumns(LargeColumns ?? span.Large);

        _elementClass = CssClassBuilder.Create()
            .AddFromAdditionalAttributes(AdditionalAttributes)
            .AddClass("nt-form-field-layout-span")
            .AddClass($"nt-form-field-layout-span-{Span.ToCssClassName()}")
            .Build();

        _elementStyle = CssStyleBuilder.Create()
            .AddFromAdditionalAttributes(AdditionalAttributes)
            .AddVariable("nt-form-field-layout-span-small", smallSpan.ToString(System.Globalization.CultureInfo.InvariantCulture))
            .AddVariable("nt-form-field-layout-span-medium", mediumSpan.ToString(System.Globalization.CultureInfo.InvariantCulture))
            .AddVariable("nt-form-field-layout-span-large", largeSpan.ToString(System.Globalization.CultureInfo.InvariantCulture))
            .Build();

        BuildElementAttributes();
    }

    private void BuildElementAttributes() {
        _elementAttributes.Clear();
        if (AdditionalAttributes is not null) {
            foreach (var (attributeName, attributeValue) in AdditionalAttributes) {
                _elementAttributes[attributeName] = attributeValue;
            }
        }

        _elementAttributes["class"] = _elementClass!;
        _elementAttributes["style"] = _elementStyle!;

        if (AutoFocus == true) {
            _elementAttributes["autofocus"] = true;
        }

        SetAttribute("id", ElementId);
        SetAttribute("lang", ElementLang);
        SetAttribute("title", ElementTitle);
    }

    private void SetAttribute(string attributeName, string? value) {
        if (value is null) {
            _elementAttributes.Remove(attributeName);
            return;
        }

        _elementAttributes[attributeName] = value;
    }

    private static int NormalizeColumns(int columns) => Math.Clamp(columns, 1, 12);

    private static FieldSpan ResolveSpan(NTFormFieldSpan span) =>
        span switch {
            NTFormFieldSpan.Compact => new(12, 4, 3),
            NTFormFieldSpan.OneQuarter => new(12, 6, 3),
            NTFormFieldSpan.OneThird => new(12, 6, 4),
            NTFormFieldSpan.Half => new(12, 6, 6),
            NTFormFieldSpan.TwoThirds => new(12, 12, 8),
            NTFormFieldSpan.ThreeQuarters => new(12, 12, 9),
            NTFormFieldSpan.Full => new(12, 12, 12),
            _ => new(12, 6, 4)
        };

    private readonly record struct FieldSpan(int Small, int Medium, int Large);
}

/// <summary>
///     Responsive width presets for fields inside <see cref="NTFormFieldGridView" /> or <see cref="NTFormSectionView" />.
/// </summary>
public enum NTFormFieldSpan {
    /// <summary>
    ///     Uses the parent grid default.
    /// </summary>
    Auto,

    /// <summary>
    ///     Uses a compact span at expanded widths, suitable for short values such as state, ZIP, quantity, or code fields.
    /// </summary>
    Compact,

    /// <summary>
    ///     Uses one quarter of the row at expanded widths.
    /// </summary>
    OneQuarter,

    /// <summary>
    ///     Uses one third of the row at expanded widths.
    /// </summary>
    OneThird,

    /// <summary>
    ///     Uses one half of the row at medium and expanded widths.
    /// </summary>
    Half,

    /// <summary>
    ///     Uses two thirds of the row at expanded widths.
    /// </summary>
    TwoThirds,

    /// <summary>
    ///     Uses three quarters of the row at expanded widths.
    /// </summary>
    ThreeQuarters,

    /// <summary>
    ///     Uses the full row at every width.
    /// </summary>
    Full
}

internal static class NTFormFieldSpanExtensions {
    public static string ToCssClassName(this NTFormFieldSpan span) =>
        span switch {
            NTFormFieldSpan.OneQuarter => "one-quarter",
            NTFormFieldSpan.OneThird => "one-third",
            NTFormFieldSpan.TwoThirds => "two-thirds",
            NTFormFieldSpan.ThreeQuarters => "three-quarters",
            _ => span.ToString().ToLowerInvariant()
        };
}
