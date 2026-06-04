using NTComponents.CodeDocumentation;

namespace NTComponents;

/// <summary>
///     Renders one file row inside <see cref="NTFileUpload" />.
/// </summary>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.ProgressivelyEnhanced,
    CompatibilitySummary = "Renders file upload item progress markup through NTFileUpload.",
    CompatibilityDetails = "The item can render current file status markup, but file selection, IBrowserFile data, progress updates, and upload callbacks depend on the parent upload component's interactive workflow.")]
public partial class NTFileUploadItem;
