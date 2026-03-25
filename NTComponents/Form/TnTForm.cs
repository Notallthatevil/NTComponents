using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using System.Diagnostics.CodeAnalysis;

namespace NTComponents;

/// <summary>
///     Defines the contract for a TnT form, exposing appearance, disabled, and read-only state. Implementations provide metadata for form rendering and state management in NTComponents.
/// </summary>
public interface ITnTForm {

    /// <summary>
    ///     Gets the visual appearance of the form, which controls its style and layout.
    /// </summary>
    FormAppearance Appearance { get; }

    /// <summary>
    ///     Gets a value indicating whether the form is disabled. When true, user input is blocked.
    /// </summary>
    bool Disabled { get; }

    /// <summary>
    ///     Gets a value indicating whether the form is read-only. When true, input fields cannot be edited but may be copied.
    /// </summary>
    bool ReadOnly { get; }
}

/// <summary>
///     A Blazor form component that extends <see cref="EditForm" /> and implements <see cref="ITnTForm" />. Provides additional parameters for appearance, disabled, and read-only state, and supplies
///     itself as a cascading value for child components to access form metadata and state. This class is sealed for performance as it is not intended to be inherited.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class TnTForm : EditForm, ITnTForm {

    /// <inheritdoc />
    [Parameter]
    public FormAppearance Appearance { get; set; }

    /// <inheritdoc />
    [Parameter]
    public bool Disabled { get; set; }

    /// <inheritdoc />
    [Parameter]
    public bool ReadOnly { get; set; }

    /// <inheritdoc />
    protected override void OnInitialized() {
        base.OnInitialized();
        if (RendererInfo.IsInteractive && AdditionalAttributes?.TryGetValue("novalidate", out var _) != true) {
            if (AdditionalAttributes is null) {
                AdditionalAttributes = new Dictionary<string, object>() { { "novalidate", true } };
            }
            else {
                var attributes = new Dictionary<string, object>(AdditionalAttributes) { { "novalidate", true } };
                AdditionalAttributes = attributes;
            }
        }
    }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder) {
        builder.OpenComponent<CascadingValue<ITnTForm>>(0);
        builder.AddComponentParameter(10, nameof(CascadingValue<>.Value), this);
        builder.AddComponentParameter(20, nameof(CascadingValue<>.IsFixed), true);
        builder.AddComponentParameter(30, nameof(CascadingValue<>.ChildContent), new RenderFragment(base.BuildRenderTree));
        builder.CloseComponent();
    }
}

/// <summary>
///     Specifies the appearance of a form.
/// </summary>
public enum FormAppearance {

    /// <summary>
    ///     The form has an outlined appearance.
    /// </summary>
    Outlined,

    /// <summary>
    ///     The form has a filled appearance.
    /// </summary>
    Filled,

    /// <summary>
    ///     Same as <see cref="Outlined" /> but with more compact spacing."/&gt;
    /// </summary>
    OutlinedCompact,

    /// <summary>
    ///     Same as <see cref="Filled" /> but with more compact spacing."/&gt;
    /// </summary>
    FilledCompact,

    /// <summary>
    ///     Same as <see cref="OutlinedCompact" /> but with xs spacing, targeting a total element height of approximately 26px.
    /// </summary>
    OutlinedXS,

    /// <summary>
    ///     Same as <see cref="FilledCompact" /> but with xs spacing, targeting a total element height of approximately 26px.
    /// </summary>
    FilledXS
}