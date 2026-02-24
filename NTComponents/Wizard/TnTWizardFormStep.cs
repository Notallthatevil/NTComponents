using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NTComponents.Wizard;

namespace NTComponents;

/// <summary>
///     Defines the contract for a wizard form step, including properties for managing form appearance, validation, and submission behavior.
/// </summary>
/// <remarks>
///     Implementations of this interface should provide the necessary logic to handle form interactions, including managing the state of the form fields and invoking callbacks on submission events.
/// </remarks>
public interface ITnTWizardFormStep {

    /// <summary>
    ///     The child content of the form step, which is a render fragment that takes an <see cref="EditContext" />.
    /// </summary>
    RenderFragment<EditContext> ChildContent { get; set; }

    /// <summary>
    ///     Sets the form fields to be disabled. When set to <c>true</c>, the form cannot be interacted with.
    /// </summary>
    bool Disabled { get; set; }

    /// <summary>
    ///     The appearance of the form. <see cref="FormAppearance" />
    /// </summary>
    FormAppearance FormAppearance { get; set; }

    /// <summary>
    ///     The name of the form. This is optional.
    /// </summary>
    string? FormName { get; set; }

    /// <summary>
    ///     A value indicating whether to include a <see cref="DataAnnotationsValidator" /> in the form. Defaults to <c>true</c>.
    /// </summary>
    bool IncludeDataAnnotationsValidator { get; set; }

    /// <summary>
    ///     The model object used for data binding in the form.
    /// </summary>
    object Model { get; set; }

    /// <summary>
    ///     The callback to invoke when the form submission is invalid.
    /// </summary>
    EventCallback<object> OnInvalidSubmitCallback { get; set; }

    /// <summary>
    ///     The callback to invoke when the form submission is valid.
    /// </summary>
    EventCallback<object> OnValidSubmitCallback { get; set; }

    /// <summary>
    ///     Sets the form fields to be read-only. When set to <c>true</c>, the form cannot be edited.
    /// </summary>
    bool ReadOnly { get; set; }

    /// <summary>
    ///     Validates the form and invokes the appropriate callback based on the validation result.
    /// </summary>
    /// <returns>A <see cref="Task{TResult}" /> that resolves to <c>true</c> if the form is valid; otherwise, <c>false</c>.</returns>
    Task<bool> FormValidAsync();
}

/// <summary>
///     Represents a wizard step that contains a form in the NTComponents library.
/// </summary>
public class TnTWizardFormStep : TnTWizardStepBase, ITnTWizardFormStep {

    /// <inheritdoc />
    [Parameter, EditorRequired]
    public RenderFragment<EditContext> ChildContent { get; set; } = default!;

    /// <inheritdoc />
    [Parameter]
    public bool Disabled { get; set; }

    /// <inheritdoc />
    [Parameter]
    public FormAppearance FormAppearance { get; set; } = FormAppearance.Outlined;

    /// <inheritdoc />
    [Parameter]
    public string? FormName { get; set; }

    /// <inheritdoc />
    [Parameter]
    public bool IncludeDataAnnotationsValidator { get; set; } = true;

    /// <inheritdoc />
    [Parameter]
    public object Model { get; set; } = default!;

    /// <inheritdoc />
    [Parameter]
    public EventCallback<object> OnInvalidSubmitCallback { get; set; }

    /// <inheritdoc />
    [Parameter]
    public EventCallback<object> OnValidSubmitCallback { get; set; }

    /// <inheritdoc />
    [Parameter]
    public bool ReadOnly { get; set; }

    /// <inheritdoc />
    private TnTForm _form = default!;

    internal EditContext? EditContext => _form?.EditContext;

    /// <summary>
    ///     Performs a trim-safe required-field check without touching form edit context validation messages.
    /// </summary>
    /// <returns><c>true</c> when all required model fields contain values; otherwise <c>false</c>.</returns>
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Used only for UI affordance in wizard button state; runtime model metadata is available in component usage.")]
    internal bool RequiredFieldsSatisfiedWithoutMessageEmission() {
        var model = _form?.Model ?? Model;
        if (model is null) {
            return false;
        }

        foreach (var property in model.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
            if (property.GetIndexParameters().Length > 0 || !property.CanRead) {
                continue;
            }

            if (!property.GetCustomAttributes<RequiredAttribute>(inherit: true).Any()) {
                continue;
            }

            var value = property.GetValue(model);
            if (value is null) {
                return false;
            }

            if (value is string str && string.IsNullOrWhiteSpace(str)) {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> FormValidAsync() {
        if (_form?.EditContext?.Validate() == true) {
            await OnValidSubmitCallback.InvokeAsync(Model);
            return true;
        }
        else {
            await OnInvalidSubmitCallback.InvokeAsync(Model);
            return false;
        }
    }

    /// <inheritdoc />
    public override RenderFragment Render() => new(builder => {
        builder.OpenComponent<TnTForm>(0);
        builder.AddComponentParameter(10, nameof(TnTForm.Model), Model);
        builder.AddComponentParameter(15, nameof(TnTForm.Appearance), FormAppearance);
        builder.AddComponentParameter(16, nameof(TnTForm.Disabled), Disabled);
        builder.AddComponentParameter(17, nameof(TnTForm.ReadOnly), ReadOnly);
        builder.AddAttribute(20, "class", "tnt-wizard-form");
        if (!string.IsNullOrWhiteSpace(FormName)) {
            builder.AddComponentParameter(30, nameof(TnTForm.FormName), FormName);
        }

        builder.SetKey(_internalId);

        builder.AddComponentParameter(60, nameof(TnTForm.ChildContent), new RenderFragment<EditContext>(editContext => new RenderFragment(b => {
            if (IncludeDataAnnotationsValidator) {
                b.OpenComponent<DataAnnotationsValidator>(0);
                b.CloseComponent();
            }

            b.AddContent(10, ChildContent(editContext));
        })));

        builder.AddComponentReferenceCapture(70, component => {
            if (component is TnTForm form) {
                _form = form;
            }
        });
        builder.CloseComponent();
    });

    /// <inheritdoc />
    protected override void OnParametersSet() {
        base.OnParametersSet();
        ArgumentNullException.ThrowIfNull(Model, nameof(Model));
    }
}
