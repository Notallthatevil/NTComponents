using Microsoft.AspNetCore.Components;
using NTComponents.Core;

namespace NTComponents;

/// <summary>
///     Clips arbitrary child content to a Material-inspired expressive shape.
/// </summary>
public partial class NTShape : TnTPageScriptComponent<NTShape> {

    /// <summary>
    ///     Determines whether shape changes should morph between the current and next path.
    /// </summary>
    [Parameter]
    public bool AnimateShapeChanges { get; set; }

    /// <summary>
    ///     The content rendered inside the shaped wrapper.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    ///     The selected shape from the expressive catalog.
    /// </summary>
    [Parameter]
    public NTShapeType Shape { get; set; } = NTShapeType.Circle;

    /// <summary>
    ///     The easing curve used when <see cref="AnimateShapeChanges" /> is enabled.
    /// </summary>
    [Parameter]
    public NTMotionEasing TransitionEasing { get; set; } = NTMotionEasing.Emphasized;

    /// <summary>
    ///     The duration used when <see cref="AnimateShapeChanges" /> is enabled.
    /// </summary>
    [Parameter]
    public NTMotionDuration TransitionDuration { get; set; } = NTMotionDuration.Ms500;

    internal string ClipPathId => $"nt-shape-clip-{ComponentIdentifier}";

    internal string ContentStyle => $"clip-path:url(#{ClipPathId});-webkit-clip-path:url(#{ClipPathId});";

    /// <inheritdoc />
    public override string? ElementClass => CssClassBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddClass("nt-shape")
        .Build();

    /// <inheritdoc />
    public override string? ElementStyle => CssStyleBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .Build();

    internal string InitialPathData => NTShapeCatalog.GetPathData(Shape);

    /// <inheritdoc />
    public override string? JsModulePath => "./_content/NTComponents/Shapes/NTShape.razor.js";

    internal int TransitionDurationMilliseconds => TransitionDuration.ToMilliseconds();
}
