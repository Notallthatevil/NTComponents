#pragma warning disable CS0618
using Microsoft.AspNetCore.Components;

namespace NTComponents.Form;

/// <summary>
///     Defines the contract for a form item component in the NTComponents library.
///     <para>This interface provides a standardized set of properties for form-related UI elements, enabling consistent appearance, state management, and integration with parent forms.</para>
///     <para>
///         Implementations of <see cref="IFormItem" /> can represent input fields, labels, or any component that participates in a form layout. The properties allow for customization of visual style,
///         enable/disable/read-only state, and access to parent form context.
///     </para>
/// </summary>
[System.Obsolete("This legacy Form element is obsolete. Use the NT form components instead.")]
public interface IFormItem {

    /// <summary>
    ///     Gets or sets additional HTML attributes to be applied to the rendered element of the form item. This enables support for custom attributes, ARIA attributes, and extensibility for
    ///     accessibility or automation.
    /// </summary>
    IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>
    ///     Gets or sets the visual appearance of the form item, such as filled or outlined style. This property controls the overall look and feel of the component.
    /// </summary>
    FormAppearance Appearance { get; set; }

    /// <summary>
    ///     Gets or sets the background color of the form item, allowing for theme or context-specific styling.
    /// </summary>
    TnTColor? BackgroundColor { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the form item is disabled. When true, the item does not accept user input and appears visually inactive.
    /// </summary>
    bool Disabled { get; set; }

    /// <summary>
    ///     Gets the <see cref="ElementReference" /> for the rendered element of the form item. This can be used for JS interop or programmatic focus.
    /// </summary>
    ElementReference Element { get; }

    /// <summary>
    ///     Gets the computed CSS class string for the form item, reflecting its current state and appearance.
    /// </summary>
    string FormCssClass { get; }

    /// <summary>
    ///     Gets the computed inline CSS style string for the form item, if any.
    /// </summary>
    string? FormCssStyle { get; }

    /// <summary>
    ///     Gets or sets the parent <see cref="TnTForm" /> instance that contains this form item. This enables the item to inherit form-level appearance and state.
    /// </summary>
    TnTForm? ParentForm { get; set; }

    /// <summary>
    ///     Gets the visual appearance of the parent form, if available. This allows the item to align its style with the form container.
    /// </summary>
    FormAppearance? ParentFormAppearance { get; }

    /// <summary>
    ///     Gets a value indicating whether the parent form is disabled, if available. When true, the item should also appear disabled.
    /// </summary>
    bool? ParentFormDisabled { get; }

    /// <summary>
    ///     Gets a value indicating whether the parent form is read-only, if available. When true, the item should not allow editing.
    /// </summary>
    bool? ParentFormReadOnly { get; }

    /// <summary>
    ///     Gets or sets a value indicating whether the form item is read-only. When true, the item displays its value but does not allow user edits.
    /// </summary>
    bool ReadOnly { get; set; }

    /// <summary>
    ///     Gets or sets the text (foreground) color of the form item, allowing for theme or context-specific styling.
    /// </summary>
    TnTColor? TextColor { get; set; }
}