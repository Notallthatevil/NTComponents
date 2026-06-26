using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using System.Diagnostics.CodeAnalysis;

using NTComponents.CodeDocumentation;
namespace NTComponents;

/// <summary>
///     Provides form-level defaults for NT input components.
/// </summary>
/// <remarks>
///     <para>
///         Use <see cref="NTForm" /> to keep text-field appearance, density, disabled state, read-only state, and
///         required-field supporting text consistent across a form region.
///     </para>
///     <para>
///         Material 3 text-field guidance treats filled and outlined fields as functionally equivalent variants. Choose the
///         variant that fits the surrounding UI, then keep that choice consistent within a form or section instead of mixing
///         filled and outlined fields side by side.
///     </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.SsrCompatible,
    CompatibilitySummary = "Renders form markup for static SSR.",
    CompatibilityDetails = "The form can participate in native posts when controls emit names. EditContext validation events, submit callbacks, and bound model updates require an interactive render mode.")]
public sealed class NTForm : EditForm {
    private readonly RenderFragment _childContent;

    /// <summary>
    ///     Initializes a new instance of the <see cref="NTForm" /> class.
    /// </summary>
    public NTForm() {
        _childContent = builder => base.BuildRenderTree(builder);
    }

    /// <summary>
    ///     Gets or sets the visual appearance used by descendant NT input components.
    /// </summary>
    /// <remarks>
    ///     Use <see cref="NTFormAppearance.Outlined" /> for lower-emphasis forms and dense form regions. Use
    ///     <see cref="NTFormAppearance.Filled" /> when the field should draw more visual attention, such as in a short form
    ///     or dialog.
    /// </remarks>
    [Parameter]
    public NTFormAppearance Appearance { get; set; } = NTFormAppearance.Outlined;

    /// <summary>
    ///     Gets or sets the density used by descendant NT input components.
    /// </summary>
    /// <remarks>
    ///     Prefer <see cref="NTFormDensity.Comfortable" /> or <see cref="NTFormDensity.Standard" /> for general forms. Use
    ///     <see cref="NTFormDensity.Dense" /> only when the user has chosen a denser layout or when the surrounding UI is
    ///     built for scanning large amounts of data.
    /// </remarks>
    [Parameter]
    public NTFormDensity Density { get; set; } = NTFormDensity.Standard;

    /// <summary>
    ///     Gets or sets a value indicating whether descendant NT text-entry components should bind on input by default.
    /// </summary>
    /// <remarks>
    ///     Enable this when a form region needs real-time validation, previews, or filtering. Leave it disabled for ordinary
    ///     forms so each keystroke does not force model updates and validation work.
    /// </remarks>
    [Parameter]
    public bool BindOnInput { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether descendant NT input components are disabled.
    /// </summary>
    /// <remarks>
    ///     Disabled fields are removed from normal editing and validation flow. Prefer read-only fields when the value still
    ///     needs to remain readable, selectable, or submitted with the form.
    /// </remarks>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether descendant NT input components are read-only.
    /// </summary>
    /// <remarks>
    ///     Read-only fields should be clearly labeled and pre-filled. Use read-only instead of disabled when people need to
    ///     review immutable values as part of a form.
    /// </remarks>
    [Parameter]
    public bool ReadOnly { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether required inputs should show required supporting text.
    /// </summary>
    /// <remarks>
    ///     Enable this when required fields are not already explained by a form-level note. Keep the supporting text brief so
    ///     it does not compete with validation messages.
    /// </remarks>
    [Parameter]
    public bool ShowRequiredSupportingText { get; set; }

    /// <summary>
    ///     Gets or sets the supporting text shown for required inputs when <see cref="ShowRequiredSupportingText" /> is
    ///     true.
    /// </summary>
    [Parameter]
    public string RequiredSupportingText { get; set; } = "Required";

    [CascadingParameter]
    private NTForm? ParentForm { get; set; }

    /// <inheritdoc />
    protected override void OnInitialized() {
        ThrowIfNested();

        base.OnInitialized();
        if (IsInteractiveRenderer() && AdditionalAttributes?.ContainsKey("novalidate") != true) {
            AdditionalAttributes = AdditionalAttributes is null
                ? new Dictionary<string, object> { ["novalidate"] = true }
                : new Dictionary<string, object>(AdditionalAttributes) { ["novalidate"] = true };
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet() {
        ThrowIfNested();
        base.OnParametersSet();
        if (EditContext is not null) {
            NTFieldCssClassProvider.Configure(EditContext);
        }
    }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder) {
        builder.OpenComponent<CascadingValue<NTForm>>(0);
        builder.AddComponentParameter(10, nameof(CascadingValue<NTForm>.Value), this);
        builder.AddComponentParameter(20, nameof(CascadingValue<NTForm>.ChildContent), _childContent);
        builder.CloseComponent();
    }

    private bool IsInteractiveRenderer() {
        try {
            return RendererInfo.IsInteractive;
        }
        catch (InvalidOperationException) {
            return false;
        }
        catch (Exception ex) when (ex.GetType().FullName == "Bunit.Rendering.MissingRendererInfoException") {
            return false;
        }
    }

    private void ThrowIfNested() {
        if (ParentForm is not null) {
            throw new InvalidOperationException($"{nameof(NTForm)} cannot be nested inside another {nameof(NTForm)}.");
        }
    }
}

/// <summary>
///     Specifies the visual appearance of NT input fields.
/// </summary>
/// <remarks>
///     Filled and outlined fields have the same input behavior. The choice is visual: keep one variant consistent within a
///     form region, or separate variants into distinct sections when both are needed on the same screen.
/// </remarks>
public enum NTFormAppearance {

    /// <summary>
    ///     Filled input fields with a container fill and active bottom indicator.
    /// </summary>
    /// <remarks>
    ///     Filled fields have stronger visual emphasis and work well for short forms, dialogs, or focused editing surfaces.
    /// </remarks>
    Filled,

    /// <summary>
    ///     Outlined input fields with a rounded container outline and floating label notch.
    /// </summary>
    /// <remarks>
    ///     Outlined fields have lower visual emphasis and are a good default for longer forms where many fields appear
    ///     together.
    /// </remarks>
    Outlined
}

/// <summary>
///     Specifies the density of NT input fields.
/// </summary>
/// <remarks>
///     Density changes field height, spacing, and for dense fields the type scale. Do not use dense fields as the default
///     for general-purpose forms; provide a way back to a larger density when dense mode is user selectable.
/// </remarks>
public enum NTFormDensity {

    /// <summary>
    ///     Strict Material 3 text-field sizing.
    /// </summary>
    /// <remarks>
    ///     Comfortable uses the Material text-field target height and spacing for broad touch and pointer usability.
    /// </remarks>
    Comfortable,

    /// <summary>
    ///     Default NTComponents density with tighter spacing and Material 3 typography.
    /// </summary>
    /// <remarks>
    ///     Standard is intended for application forms that need more compact spacing while preserving the same type scale as
    ///     Comfortable.
    /// </remarks>
    Standard,

    /// <summary>
    ///     Dense fields with reduced spacing and typography reduced by one type scale.
    /// </summary>
    /// <remarks>
    ///     Dense is intended for high-information layouts and should be opt-in because it reduces the field target below the
    ///     default Material recommendation.
    /// </remarks>
    Dense
}
