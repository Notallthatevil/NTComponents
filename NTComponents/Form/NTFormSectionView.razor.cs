using Microsoft.AspNetCore.Components;
using NTComponents.CodeDocumentation;
using NTComponents.Core;

namespace NTComponents;

/// <summary>
///     Primitive form section view with a responsive field grid.
/// </summary>
/// <remarks>
///     Use <see cref="NTFormSectionView" /> to group related fields with a heading, optional description, and responsive
///     field layout. It does not create an <see cref="NTForm" />, validate input, or own persistence. Use
///     <see cref="UseFieldset" /> when the group should be announced as one related form field group.
/// </remarks>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.SsrCompatible,
    CompatibilitySummary = "Renders static semantic form-section layout.",
    CompatibilityDetails = "The section view is CSS-only and does not require Blazor interactivity. Descendant fields keep their own render-mode requirements.")]
public partial class NTFormSectionView : NTComponentBase {
    private readonly Dictionary<string, object> _elementAttributes = [];
    private string? _elementClass;
    private string? _elementStyle;

    /// <summary>
    ///     Section heading text.
    /// </summary>
    [Parameter]
    public string? Heading { get; set; }

    /// <summary>
    ///     Short section description rendered below the heading.
    /// </summary>
    [Parameter]
    public string? Description { get; set; }

    /// <summary>
    ///     Field content rendered inside the section field grid.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    ///     Renders the section as a native <c>fieldset</c> with a <c>legend</c>.
    /// </summary>
    [Parameter]
    public bool UseFieldset { get; set; }

    /// <summary>
    ///     Maximum number of default-width fields per row at expanded widths.
    /// </summary>
    [Parameter]
    public int MaxColumns { get; set; } = 3;

    /// <inheritdoc />
    public override string? ElementClass => _elementClass;

    /// <inheritdoc />
    public override string? ElementStyle => _elementStyle;

    private IReadOnlyDictionary<string, object> ElementAttributes => _elementAttributes;

    private bool HasHeading => !string.IsNullOrWhiteSpace(Heading);

    private bool HasHeaderContent => HasHeading || Description is not null;

    private string HeadingId => $"{RootId}-heading";

    private string DescriptionId => $"{RootId}-description";

    private string RootId => string.IsNullOrWhiteSpace(ElementId) ? $"nt-form-section-view-{ComponentIdentifier}" : ElementId;

    private string? AriaLabelledBy => HasHeading ? HeadingId : null;

    private string? DescribedBy => Description is null ? null : DescriptionId;

    /// <inheritdoc />
    protected override void OnParametersSet() {
        base.OnParametersSet();

        _elementClass = CssClassBuilder.Create()
            .AddFromAdditionalAttributes(AdditionalAttributes)
            .AddClass("nt-form-section-view")
            .AddClass("nt-form-section-view-fieldset", UseFieldset)
            .Build();

        _elementStyle = CssStyleBuilder.Create()
            .AddFromAdditionalAttributes(AdditionalAttributes)
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
        if (!string.IsNullOrWhiteSpace(_elementStyle)) {
            _elementAttributes["style"] = _elementStyle!;
        }
        else {
            _elementAttributes.Remove("style");
        }

        _elementAttributes["id"] = RootId;
        if (AutoFocus == true) {
            _elementAttributes["autofocus"] = true;
        }

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
}
