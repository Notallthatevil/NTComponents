using Microsoft.AspNetCore.Components;

namespace NTComponents.Interfaces;

/// <summary>
///     Simple interface that represents a TnTComponent. Each TnTComponent can be thought of as a single Html Element.
/// </summary>
public interface INTComponentBase : IComponent {

    /// <summary>
    ///     Contains all additional attributes provided to this component that do not match a parameter.
    ///     These are unmatched attributes passed from the parent component.
    /// </summary>
    IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>
    ///     Gets or sets whether the component should automatically receive focus when rendered.
    ///     <see href="https://developer.mozilla.org/en-US/docs/Web/HTML/Global_attributes/autofocus" />
    /// </summary>
    bool? AutoFocus { get; set; }

    /// <summary>
    ///     A reference to the main HTML element of this component. This is set after the component is rendered.
    /// </summary>
    ElementReference Element { get; }

    /// <summary>
    ///     The base CSS class applied to the root element of this component.
    ///     <see href="https://developer.mozilla.org/en-US/docs/Web/HTML/Global_attributes/class" />
    /// </summary>
    string? ElementClass { get; }

    /// <summary>
    ///     If set, applies this Id to the root HTML tag for this component.
    ///     <see href="https://developer.mozilla.org/en-US/docs/Web/HTML/Global_attributes/id" />
    /// </summary>
    string? ElementId { get; }

    /// <summary>
    ///     The language of the root HTML tag for this component.
    ///     <see href="https://developer.mozilla.org/en-US/docs/Web/HTML/Global_attributes/lang" />
    /// </summary>
    string? ElementLang { get; }

    /// <summary>
    ///     Additional inline CSS styles applied to the root element for this component.
    ///     <see href="https://developer.mozilla.org/en-US/docs/Web/HTML/Global_attributes/style" />
    /// </summary>
    string? ElementStyle { get; }

    /// <summary>
    ///     The HTML <c>title</c> attribute for the root element of this component.
    ///     <see href="https://developer.mozilla.org/en-US/docs/Web/HTML/Global_attributes/title" />
    /// </summary>
    string? ElementTitle { get; }
}

/// <summary>
///     Obsolete compatibility alias for <see cref="INTComponentBase" />.
/// </summary>
[Obsolete("ITnTComponentBase is obsolete. Use INTComponentBase instead.")]
public interface ITnTComponentBase : INTComponentBase {

    /// <inheritdoc cref="INTComponentBase.AdditionalAttributes" />
    new IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    IReadOnlyDictionary<string, object>? INTComponentBase.AdditionalAttributes {
        get => AdditionalAttributes;
        set => AdditionalAttributes = value;
    }

    /// <inheritdoc cref="INTComponentBase.AutoFocus" />
    new bool? AutoFocus { get; set; }

    bool? INTComponentBase.AutoFocus {
        get => AutoFocus;
        set => AutoFocus = value;
    }

    /// <inheritdoc cref="INTComponentBase.Element" />
    new ElementReference Element { get; }

    ElementReference INTComponentBase.Element => Element;

    /// <inheritdoc cref="INTComponentBase.ElementClass" />
    new string? ElementClass { get; }

    string? INTComponentBase.ElementClass => ElementClass;

    /// <inheritdoc cref="INTComponentBase.ElementId" />
    new string? ElementId { get; }

    string? INTComponentBase.ElementId => ElementId;

    /// <inheritdoc cref="INTComponentBase.ElementLang" />
    new string? ElementLang { get; }

    string? INTComponentBase.ElementLang => ElementLang;

    /// <inheritdoc cref="INTComponentBase.ElementStyle" />
    new string? ElementStyle { get; }

    string? INTComponentBase.ElementStyle => ElementStyle;

    /// <inheritdoc cref="INTComponentBase.ElementTitle" />
    new string? ElementTitle { get; }

    string? INTComponentBase.ElementTitle => ElementTitle;
}
