namespace NTComponents.CodeDocumentation;

/// <summary>
///     Describes generated documentation metadata that cannot be inferred from XML comments alone.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum, AllowMultiple = false, Inherited = false)]
public sealed class NTDocumentationAttribute : Attribute {

    /// <summary>
    ///     Gets or sets the component render compatibility classification.
    /// </summary>
    public NTComponentRenderCompatibility RenderCompatibility { get; init; } = NTComponentRenderCompatibility.Unknown;

    /// <summary>
    ///     Gets or sets short compatibility text for badges and summaries.
    /// </summary>
    public string? CompatibilitySummary { get; init; }

    /// <summary>
    ///     Gets or sets detailed render compatibility guidance.
    /// </summary>
    public string? CompatibilityDetails { get; init; }
}

/// <summary>
///     Describes how a component behaves when rendered without an interactive Blazor circuit or WebAssembly runtime.
/// </summary>
public enum NTComponentRenderCompatibility {

    /// <summary>
    ///     Compatibility has not been reviewed yet.
    /// </summary>
    Unknown,

    /// <summary>
    ///     The component requires Blazor interactivity for its primary behavior.
    /// </summary>
    InteractiveRequired,

    /// <summary>
    ///     The component renders useful static HTML and can be used in static SSR.
    /// </summary>
    SsrCompatible,

    /// <summary>
    ///     The component renders useful static HTML and adds richer behavior after browser enhancement.
    /// </summary>
    ProgressivelyEnhanced
}
