using Microsoft.AspNetCore.Components;

using NTComponents.CodeDocumentation;
namespace NTComponents.Wizard;

/// <summary>
///     Represents the base class for a wizard step in an <see cref="NTWizard" />.
/// </summary>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.InteractiveRequired,
    CompatibilitySummary = "Participates in parent component rendering and inherits the parent interaction model.",
    CompatibilityDetails = "Derived steps participate in NTWizard navigation and validation. The workflow depends on interactive parent wizard state.")]
public abstract class NTWizardStepBase : ComponentBase, IDisposable {

    /// <summary>
    ///     The icon associated with the wizard step.
    /// </summary>
    [Parameter]
    public TnTIcon? Icon { get; set; }

    /// <summary>
    ///     Gets or sets whether this step is already complete.
    /// </summary>
    [Parameter]
    public bool Completed { get; set; }

    /// <summary>
    ///     Gets or sets whether this step cannot be entered from wizard navigation.
    /// </summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>
    ///     Gets or sets whether this step is currently invalid.
    /// </summary>
    [Parameter]
    public bool HasError { get; set; }

    /// <summary>
    ///     Gets or sets whether this step can be skipped by the user.
    /// </summary>
    [Parameter]
    public bool Optional { get; set; }

    /// <summary>
    ///     Gets or sets whether this step has been skipped.
    /// </summary>
    [Parameter]
    public bool Skipped { get; set; }

    /// <summary>
    ///     The subtitle of the wizard step.
    /// </summary>
    [Parameter]
    public string? SubTitle { get; set; } = string.Empty;

    /// <summary>
    ///     The title of the wizard step. This parameter is required.
    /// </summary>
    [Parameter, EditorRequired]
    public string Title { get; set; } = default!;

    /// <summary>
    ///     Gets or sets an asynchronous validation callback for this step.
    /// </summary>
    [Parameter]
    public Func<Task<bool>>? ValidateAsync { get; set; }

    internal int _internalId = -1;

    /// <summary>
    ///     The parent <see cref="NTWizard" /> component this step belongs to.
    /// </summary>
    [CascadingParameter]
    protected NTWizard Wizard { get; set; } = default!;

    /// <inheritdoc />
    public void Dispose() {
        Wizard?.RemoveChildStep(this);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     Renders the content of the wizard step.
    /// </summary>
    /// <returns>A <see cref="RenderFragment" /> representing the content of the step.</returns>
    public abstract RenderFragment Render();

    internal virtual async Task<bool> ValidateStepAsync() {
        if (ValidateAsync is null) {
            return !HasError;
        }

        return await ValidateAsync.Invoke();
    }

    /// <inheritdoc />
    protected override void OnInitialized() {
        base.OnInitialized();
        if (Wizard is null) {
            throw new InvalidOperationException($"{nameof(NTWizardStep)} must be used within a {nameof(NTWizard)} component.");
        }

        Wizard.AddChildStep(this);
    }
}
