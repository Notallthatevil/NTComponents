using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using NTComponents.Core;

namespace NTComponents;

/// <summary>
///     Defines the appearance options available for TnTIcon components.
/// </summary>
public enum IconAppearance {

    /// <summary>
    ///     Default icon style with standard rounded edges.
    /// </summary>
    Default,

    /// <summary>
    ///     Filled icon style for Material Symbols.
    /// </summary>
    Filled,

    /// <summary>
    ///     Outlined icon style with transparent fill and defined edges.
    /// </summary>
    Outlined,

    /// <summary>
    ///     Round icon style with fully rounded corners and edges.
    /// </summary>
    Round,

    /// <summary>
    ///     Sharp icon style with angular corners and straight edges.
    /// </summary>
    Sharp
}

/// <summary>
///     Defines the size options available for TnTIcon components.
/// </summary>
public enum IconSize {

    /// <summary>
    ///     Small icon size, suitable for compact UI elements.
    /// </summary>
    Small,

    /// <summary>
    ///     Medium icon size, the default size for most UI contexts.
    /// </summary>
    Medium,

    /// <summary>
    ///     Large icon size, suitable for prominent UI elements.
    /// </summary>
    Large,

    /// <summary>
    ///     Extra large icon size, suitable for featured or hero UI elements.
    /// </summary>
    ExtraLarge
}

/// <summary>
///     Specifies the type of icon to be used.
/// </summary>
public enum IconType {

    /// <summary>
    ///     Represents Material Icons.
    /// </summary>
    MaterialIcons,

    /// <summary>
    ///     Represents Font Awesome icons.
    /// </summary>
    FontAwesome
}

/// <summary>
///     Base component for rendering icons with configurable appearance, size, and color.
/// </summary>
/// <remarks>
///     TnTIcon serves as a foundational component for rendering various icon styles within the NTComponents library. It supports different appearances, sizes and colors to match design requirements.
/// </remarks>
public abstract class TnTIcon : TnTComponentBase {

    /// <summary>
    ///     Gets or sets the appearance style of the icon.
    /// </summary>
    /// <remarks>Controls the visual style of the icon such as Default, Outlined, Round, or Sharp.</remarks>
    [Parameter]
    public IconAppearance Appearance { get; set; } = IconAppearance.Default;

    /// <summary>
    ///     Gets or sets the color of the icon.
    /// </summary>
    /// <remarks>Uses the TnTColor system to define the icon's color. Defaults to OnSurface.</remarks>
    [Parameter]
    public TnTColor Color { get; set; } = TnTColor.OnSurface;

    /// <summary>
    ///     Gets or sets the icon content to be displayed.
    /// </summary>
    /// <remarks>This is a required parameter that specifies the actual icon content to render.</remarks>
    [Parameter, EditorRequired]
    public string Icon { get; set; } = default!;

    /// <summary>
    ///     Gets or sets the size of the icon.
    /// </summary>
    /// <remarks>Defines the display size of the icon from predefined options. Defaults to Medium.</remarks>
    [Parameter]
    public IconSize Size { get; set; } = IconSize.Medium;

    /// <summary>
    ///     The content to display as a tooltip for the component.
    /// </summary>
    [Parameter]
    public RenderFragment? Tooltip { get; set; }

    /// <summary>
    ///     Additional CSS class to be applied to the icon element.
    /// </summary>
    internal string? AdditionalClass { get; set; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="TnTIcon" /> class with the specified icon.
    /// </summary>
    /// <param name="icon">The icon content to be displayed.</param>
    internal TnTIcon(string icon) => Icon = icon;

    /// <summary>
    ///     Initializes a new instance of the <see cref="TnTIcon" /> class.
    /// </summary>
    protected TnTIcon() { }

    /// <summary>
    ///     Implicitly converts an icon to its string representation.
    /// </summary>
    /// <param name="icon">The icon to convert.</param>
    /// <returns>The string representation of the icon.</returns>
    public static implicit operator string(TnTIcon icon) => icon.Icon;

    /// <summary>
    ///     Creates a render fragment for this icon.
    /// </summary>
    /// <returns>A <see cref="RenderFragment" /> that can be used to render this icon.</returns>
    public RenderFragment Render(string? additionalClass = null, bool suppressTitle = false) {
        return new(builder => {
            if (additionalClass is not null) {
                AdditionalClass = additionalClass;
            }

            BuildRenderTree(builder, suppressTitle);
        });
    }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder) => BuildRenderTree(builder, suppressTitle: false);

    private void BuildRenderTree(RenderTreeBuilder builder, bool suppressTitle) {
        builder.OpenElement(0, "span");
        builder.AddMultipleAttributes(10, AdditionalAttributes);
        builder.AddAttribute(20, "class", ElementClass);
        builder.AddAttribute(30, "style", ElementStyle);
        builder.AddAttribute(40, "id", ElementId);
        builder.AddAttribute(50, "title", !suppressTitle && Tooltip is null ? ElementTitle ?? Icon : null);
        builder.AddElementReferenceCapture(60, e => Element = e);

        if (Tooltip is not null) {
            builder.OpenComponent<TnTTooltip>(70);
            builder.AddComponentParameter(80, nameof(TnTTooltip.ChildContent), Tooltip);
            builder.CloseComponent();
        }

        builder.AddContent(90, Icon);

        builder.CloseElement();
    }
}
