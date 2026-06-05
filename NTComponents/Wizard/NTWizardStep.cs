using Microsoft.AspNetCore.Components;
using NTComponents.Wizard;

using NTComponents.CodeDocumentation;
namespace NTComponents;

/// <summary>
///     Represents a single step in the NT wizard component.
/// </summary>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.InteractiveRequired,
    CompatibilitySummary = "Participates in parent component rendering and inherits the parent interaction model.",
    CompatibilityDetails = "The step content can be rendered by NTWizard, but activation, skipping, and navigation state changes depend on the interactive parent wizard.")]
public class NTWizardStep : NTWizardStepBase {

    /// <summary>
    ///     The content to be rendered inside the wizard step.
    /// </summary>
    [Parameter, EditorRequired]
    public RenderFragment ChildContent { get; set; } = default!;

    /// <inheritdoc />
    public override RenderFragment Render() => new(builder => builder.AddContent(0, ChildContent));
}
