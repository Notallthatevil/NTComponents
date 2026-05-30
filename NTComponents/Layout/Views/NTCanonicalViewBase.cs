using Microsoft.AspNetCore.Components;
using NTComponents.Core;

namespace NTComponents;

/// <summary>
///     Base class for Material 3 canonical view components.
/// </summary>
public abstract class NTCanonicalViewBase : TnTComponentBase {

    /// <inheritdoc />
    public override string? ElementClass => _elementClass;

    /// <inheritdoc />
    public override string? ElementStyle => _elementStyle;

    /// <summary>
    ///     Whether the view should fill the available block size of its parent.
    /// </summary>
    [Parameter]
    public bool FullHeight { get; set; }

    /// <summary>
    ///     Component-specific CSS class.
    /// </summary>
    protected abstract string ComponentClass { get; }

    /// <summary>
    ///     Attributes rendered on the view root element.
    /// </summary>
    protected IReadOnlyDictionary<string, object> ElementAttributes => _elementAttributes;

    private readonly Dictionary<string, object> _elementAttributes = [];
    private string? _elementClass;
    private string? _elementStyle;

    /// <summary>
    ///     Adds component-specific root classes.
    /// </summary>
    protected virtual void AddComponentClasses(CssClassBuilder builder) {
    }

    /// <summary>
    ///     Adds component-specific root styles.
    /// </summary>
    protected virtual void AddComponentStyles(CssStyleBuilder builder) {
    }

    /// <inheritdoc />
    protected override void OnParametersSet() {
        base.OnParametersSet();

        _elementClass = BuildElementClass();
        _elementStyle = BuildElementStyle();
        BuildElementAttributes();
    }

    private static void SetAttribute(IDictionary<string, object> elementAttributes, string attributeName, object? attributeValue, bool removeWhenNull = true) {
        if (attributeValue is null) {
            if (removeWhenNull) {
                elementAttributes.Remove(attributeName);
            }

            return;
        }

        elementAttributes[attributeName] = attributeValue;
    }

    private void BuildElementAttributes() {
        _elementAttributes.Clear();

        if (AdditionalAttributes is not null) {
            foreach (var (attributeName, attributeValue) in AdditionalAttributes) {
                _elementAttributes[attributeName] = attributeValue;
            }
        }

        SetAttribute(_elementAttributes, "class", _elementClass);
        SetAttribute(_elementAttributes, "style", _elementStyle);
        SetAttribute(_elementAttributes, "autofocus", AutoFocus == true ? true : null);
        SetAttribute(_elementAttributes, "lang", ElementLang, ElementLang is not null);
        SetAttribute(_elementAttributes, "id", ElementId, ElementId is not null);
        SetAttribute(_elementAttributes, "title", ElementTitle, ElementTitle is not null);
    }

    private string BuildElementClass() {
        var builder = CssClassBuilder.Create()
            .AddFromAdditionalAttributes(AdditionalAttributes)
            .AddClass("nt-view")
            .AddClass(ComponentClass)
            .AddClass("nt-view-full-height", FullHeight);

        AddComponentClasses(builder);

        return builder.Build();
    }

    private string? BuildElementStyle() {
        var builder = CssStyleBuilder.Create()
            .AddFromAdditionalAttributes(AdditionalAttributes);

        AddComponentStyles(builder);

        return builder.Build();
    }
}
