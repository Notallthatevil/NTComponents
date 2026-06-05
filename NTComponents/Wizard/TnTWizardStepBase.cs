using Microsoft.AspNetCore.Components;

namespace NTComponents.Wizard;

/// <summary>
///     Represents the base class for a wizard step in the NTComponents library.
/// </summary>
[Obsolete("TnTWizardStepBase is obsolete. Use NTWizardStepBase instead.")]
public abstract class TnTWizardStepBase : ComponentBase, IDisposable {

    /// <summary>
    ///     The icon associated with the wizard step.
    /// </summary>
    [Parameter]
    public TnTIcon? Icon { get; set; }

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
    ///     Internal identifier for the wizard step.
    /// </summary>
    internal int _internalId = -1;

    /// <summary>
    ///     The parent <see cref="TnTWizard" /> component this step belongs to.
    /// </summary>
    [CascadingParameter]
    protected TnTWizard Wizard { get; set; } = default!;

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

    /// <inheritdoc />
    protected override void OnInitialized() {
        base.OnInitialized();
        if (Wizard is null) {
            throw new InvalidOperationException($"{nameof(TnTWizardStep)} must be used within a {nameof(TnTWizard)} component.");
        }

        Wizard.AddChildStep(this);
    }
}
