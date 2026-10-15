using Microsoft.AspNetCore.Components;
using NTComponents.CodeDocumentation;
using NTComponents.Core;

namespace NTComponents;

/// <summary>
///     A Material 3 styled color picker panel with a saturation/brightness area, hue and opacity sliders, and hex, RGB, and HSL entry.
/// </summary>
/// <remarks>
///     <para>
///         Use <see cref="NTColorPicker" /> when a color should be chosen inline on a page. Use <see cref="NTInputColor" /> when the picker should open from a form field.
///     </para>
///     <para>
///         Values are lowercase <c>#rrggbb</c> strings. When <see cref="ShowAlpha" /> is enabled and opacity is below 100%, the value is <c>#rrggbbaa</c>. Short <c>#rgb</c> values and uppercase
///         digits are accepted and normalized. Dragging or stepping the controls writes to a hidden input and dispatches <c>input</c> events, and committing a value dispatches <c>change</c>, so
///         <see cref="ValueChanged" /> fires and a plain form post carries the value when <see cref="Name" /> is set.
///     </para>
/// </remarks>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.ProgressivelyEnhanced,
    CompatibilitySummary = "Renders the picker panel and a hidden value input; the browser module drives all interaction.",
    CompatibilityDetails = "Static SSR renders the panel markup and posts the hidden input when Name is set. Pointer and keyboard interaction, format switching, and live preview come from the browser module, and ValueChanged requires an interactive render mode.")]
public partial class NTColorPicker {

    private static readonly (string Key, string Label)[] RgbChannels = [("r", "R"), ("g", "G"), ("b", "B")];
    private static readonly (string Key, string Label)[] HslChannels = [("h", "H"), ("s", "S"), ("l", "L")];

    /// <summary>
    ///     Gets or sets the accessible name of the picker group.
    /// </summary>
    [Parameter]
    public string AriaLabel { get; set; } = "Color picker";

    /// <summary>
    ///     Gets or sets a value indicating whether the picker is disabled.
    /// </summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <inheritdoc />
    public override string? ElementClass => CssClassBuilder.Create("nt-color-picker")
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddClass("nt-color-picker-disabled", Disabled)
        .Build();

    /// <inheritdoc />
    public override string? ElementStyle => CssStyleBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .Build();

    /// <inheritdoc />
    public override string? JsModulePath => "./_content/NTComponents/Form/NTColorPicker.razor.js";

    /// <summary>
    ///     Gets or sets the name of the hidden input that posts the value in form submissions.
    /// </summary>
    [Parameter]
    public string? Name { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the opacity slider and alpha entry are shown.
    /// </summary>
    [Parameter]
    public bool ShowAlpha { get; set; }

    /// <summary>
    ///     Gets or sets the selected color as <c>#rrggbb</c> or <c>#rrggbbaa</c>.
    /// </summary>
    [Parameter]
    public string? Value { get; set; }

    /// <summary>
    ///     Gets or sets a callback invoked when the selected color is committed.
    /// </summary>
    [Parameter]
    public EventCallback<string?> ValueChanged { get; set; }

    private bool IsEmbedded => AdditionalAttributes?.ContainsKey("data-nt-color-embedded") == true;

    /// <inheritdoc />
    protected override Task OnAfterRenderAsync(bool firstRender) {
        // An NTInputColor popover drives its embedded picker through its own module, so it needs no separate interop module.
        return IsEmbedded ? Task.CompletedTask : base.OnAfterRenderAsync(firstRender);
    }

    private string? AreaTabIndex => Disabled ? "-1" : "0";
    private string? DisabledAttribute => Disabled ? "true" : null;
    private string ShowAlphaAttribute => ShowAlpha ? "true" : "false";

    private Task OnValueChangedAsync(ChangeEventArgs args) {
        Value = args.Value?.ToString();
        return ValueChanged.InvokeAsync(Value);
    }
}
