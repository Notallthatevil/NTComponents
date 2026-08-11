using Microsoft.AspNetCore.Components;
using NTComponents.CodeDocumentation;

namespace NTComponents;

/// <summary>
///     Captures the browser's time-zone identifier in a hidden form field.
/// </summary>
/// <remarks>
///     The initial server render cannot know a browser-only value, so the hidden field renders <see cref="FallbackValue" />
///     when <see cref="Value" /> is null. After the markup reaches the browser, progressive enhancement replaces that value
///     with the identifier returned by <c>Intl.DateTimeFormat().resolvedOptions().timeZone</c>. Static SSR forms submit the
///     resulting value by name, while interactive components receive it through <see cref="ValueChanged" />.
/// </remarks>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.ProgressivelyEnhanced,
    CompatibilitySummary = "Renders a hidden form field with a UTC fallback during static SSR and replaces it with the browser time zone after page load.",
    CompatibilityDetails = "The initial server render uses FallbackValue because no browser time zone is available. Browser enhancement replaces the named field when detection succeeds and raises ValueChanged with the detected or fallback value when Blazor is interactive.")]
public partial class NTBrowserTimeZone {
    private const string BrowserTimeZoneJsModulePath = "./_content/NTComponents/Form/NTBrowserTimeZone.razor.js";

    /// <inheritdoc />
    public override string? ElementClass => null;

    /// <inheritdoc />
    public override string? ElementStyle => null;

    /// <summary>
    ///     Gets or sets the name used when a static SSR form submits the hidden field.
    /// </summary>
    [Parameter]
    public string ElementName { get; set; } = "BrowserTimeZoneId";

    /// <summary>
    ///     Gets or sets the value used when no browser time-zone identifier is available. Set to <see langword="null" /> to preserve an unknown value.
    /// </summary>
    [Parameter]
    public string? FallbackValue { get; set; } = "UTC";

    /// <summary>
    ///     Gets or sets the browser time-zone identifier.
    /// </summary>
    [Parameter]
    public string? Value { get; set; }

    /// <summary>
    ///     Gets or sets the callback raised when the browser time-zone identifier is captured.
    /// </summary>
    [Parameter]
    public EventCallback<string?> ValueChanged { get; set; }

    private string? EffectiveValue => Value ?? FallbackValue;

    /// <summary>
    ///     Gets the browser module path used for static SSR enhancement.
    /// </summary>
    public override string JsModulePath => BrowserTimeZoneJsModulePath;

    private Task OnValueChangedAsync(ChangeEventArgs eventArgs) => ValueChanged.InvokeAsync(eventArgs.Value?.ToString());
}
