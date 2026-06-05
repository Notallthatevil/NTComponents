using Microsoft.AspNetCore.Components;
using NTComponents.Wizard;

namespace NTComponents;

/// <summary>
///     Represents a single step in the TnT wizard component.
/// </summary>
[Obsolete("TnTWizardStep is obsolete. Use NTWizardStep instead.")]
public class TnTWizardStep : TnTWizardStepBase {

    /// <summary>
    ///     The content to be rendered inside the wizard step.
    /// </summary>
    [Parameter, EditorRequired]
    public RenderFragment ChildContent { get; set; } = default!;

    /// <inheritdoc />
    public override RenderFragment Render() => new(builder => builder.AddContent(0, ChildContent));
}
