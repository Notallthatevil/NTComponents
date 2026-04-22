using System.Diagnostics.CodeAnalysis;
using NTComponents.Interfaces;

namespace NTComponents.Core;

/// <summary>
///     A builder class for constructing CSS class strings.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CssClassBuilder {
    private readonly HashSet<string> _classes = [];

    /// <summary>
    ///     Creates a new instance of <see cref="CssClassBuilder" /> with an optional default class.
    /// </summary>
    /// <param name="defaultClass">The default CSS class to add.</param>
    /// <returns>A new instance of <see cref="CssClassBuilder" />.</returns>
    public static CssClassBuilder Create(string defaultClass = "tnt-components") => new CssClassBuilder().AddClass(defaultClass);

    /// <summary>
    ///     Adds a CSS class for background color.
    /// </summary>
    /// <param name="color">The color to use.</param>
    /// <returns>The current instance of <see cref="CssClassBuilder" />.</returns>
    public CssClassBuilder AddBackgroundColor(TnTColor? color) => AddClass($"tnt-bg-color-{color?.ToCssClassName() ?? string.Empty}", color is not null and not TnTColor.None);

    /// <summary>
    ///     Adds a CSS class if the specified condition is met.
    /// </summary>
    /// <param name="className">The CSS class to add.</param>
    /// <param name="when">     The condition to check.</param>
    /// <returns>The current instance of <see cref="CssClassBuilder" />.</returns>
    public CssClassBuilder AddClass(string? className, bool? when = true) {
        if (!string.IsNullOrWhiteSpace(className) && when == true) {
            _classes.Add(className);
        }
        return this;
    }

    /// <summary>
    ///     Adds a CSS class for NT corner radius.
    /// </summary>
    /// <param name="cornerRadius">The NT corner radius token.</param>
    /// <param name="enabled">Whether the class should be added.</param>
    /// <returns>The current instance of <see cref="CssClassBuilder" />.</returns>
    public CssClassBuilder AddCornerRadius(NTCornerRadius cornerRadius, bool enabled = true) => AddClass(cornerRadius.ToCssClass(), enabled);

    /// <summary>
    ///     Adds a CSS class for disabled state.
    /// </summary>
    /// <param name="disabled">Whether the component is disabled.</param>
    /// <returns>The current instance of <see cref="CssClassBuilder" />.</returns>
    public CssClassBuilder AddDisabled(bool disabled) => AddClass("tnt-disabled", disabled);

    /// <summary>
    ///     Adds a CSS class for elevation.
    /// </summary>
    /// <param name="elevation">The elevation level.</param>
    /// <returns>The current instance of <see cref="CssClassBuilder" />.</returns>
    public CssClassBuilder AddElevation(int elevation) => AddClass($"tnt-elevation-{Math.Clamp(elevation, 0, 10)}", elevation >= 0);

    /// <summary>
    ///     Adds a CSS class for NT elevation.
    /// </summary>
    /// <param name="elevation">The NT elevation level.</param>
    /// <param name="enabled">Whether the class should be added.</param>
    /// <returns>The current instance of <see cref="CssClassBuilder" />.</returns>
    public CssClassBuilder AddElevation(NTElevation elevation, bool enabled = true) => AddClass(elevation.ToCssClass(), enabled);

    /// <summary>
    ///     Adds a CSS class for filled state.
    /// </summary>
    /// <param name="enabled">Whether the filled state is enabled.</param>
    /// <returns>The current instance of <see cref="CssClassBuilder" />.</returns>
    public CssClassBuilder AddFilled(bool enabled = true) => AddClass("tnt-filled", enabled);

    /// <summary>
    ///     Adds a CSS class for foreground color.
    /// </summary>
    /// <param name="color">The color to use.</param>
    /// <returns>The current instance of <see cref="CssClassBuilder" />.</returns>
    public CssClassBuilder AddForegroundColor(TnTColor? color) => AddClass($"tnt-fg-color-{color?.ToCssClassName() ?? string.Empty}", color is not null and not TnTColor.None);

    /// <summary>
    ///     Adds CSS classes from additional attributes.
    /// </summary>
    /// <param name="additionalAttributes">The additional attributes.</param>
    /// <returns>The current instance of <see cref="CssClassBuilder" />.</returns>
    public CssClassBuilder AddFromAdditionalAttributes(IReadOnlyDictionary<string, object>? additionalAttributes) => additionalAttributes?.TryGetValue("class", out var @class) == true && @class is not null ? AddClass(@class.ToString()) : this;

    /// <summary>
    ///     Adds a CSS class for on-tint color.
    /// </summary>
    /// <param name="color">The color to use.</param>
    /// <returns>The current instance of <see cref="CssClassBuilder" />.</returns>
    public CssClassBuilder AddOnTintColor(TnTColor? color) => AddClass($"tnt-on-tint-color-{color?.ToCssClassName() ?? string.Empty}", color is not null and not TnTColor.None);

    /// <summary>
    ///     Adds a CSS class for ripple effect.
    /// </summary>
    /// <param name="enabled">Whether the ripple effect is enabled.</param>
    /// <returns>The current instance of <see cref="CssClassBuilder" />.</returns>
    public CssClassBuilder AddRipple(bool enabled = true) => AddClass("tnt-ripple", enabled);

    /// <summary>
    ///     Adds a CSS class for size.
    /// </summary>
    /// <param name="size">The size to use.</param>
    /// <returns>The current instance of <see cref="CssClassBuilder" />.</returns>
    public CssClassBuilder AddSize(Size? size) {
        if (size is null) {
            return this;
        }
        var sizeSuffix = size switch {
            Size.Smallest => "xs",
            Size.Small => "s",
            Size.Medium => "m",
            Size.Large => "l",
            Size.Largest => "xl",
            _ => string.Empty
        };

        return AddClass($"tnt-size-{sizeSuffix}", size is not null);
    }

    /// <summary>
    ///     Adds a CSS class for text alignment.
    /// </summary>
    /// <param name="textAlign">The text alignment to use.</param>
    /// <returns>The current instance of <see cref="CssClassBuilder" />.</returns>
    public CssClassBuilder AddTextAlign(TextAlign? textAlign) => AddClass("tnt-text-align-" + textAlign.ToCssString(), textAlign != null);

    /// <summary>
    ///     Adds a CSS class for tint color.
    /// </summary>
    /// <param name="color">The color to use.</param>
    /// <returns>The current instance of <see cref="CssClassBuilder" />.</returns>
    public CssClassBuilder AddTintColor(TnTColor? color) => AddClass($"tnt-tint-color-{color?.ToCssClassName() ?? string.Empty}", color is not null and not TnTColor.None);

    /// <summary>
    ///     Builds the CSS class string.
    /// </summary>
    /// <returns>The constructed CSS class string.</returns>
    public string Build() => string.Join(' ', _classes).Trim();
}
