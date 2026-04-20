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

    internal int TransitionDurationMilliseconds => TransitionDuration switch {
        NTMotionDuration.Ms50 => 50,
        NTMotionDuration.Ms100 => 100,
        NTMotionDuration.Ms150 => 150,
        NTMotionDuration.Ms200 => 200,
        NTMotionDuration.Ms250 => 250,
        NTMotionDuration.Ms300 => 300,
        NTMotionDuration.Ms350 => 350,
        NTMotionDuration.Ms400 => 400,
        NTMotionDuration.Ms450 => 450,
        NTMotionDuration.Ms500 => 500,
        NTMotionDuration.Ms550 => 550,
        NTMotionDuration.Ms600 => 600,
        NTMotionDuration.Ms700 => 700,
        NTMotionDuration.Ms800 => 800,
        NTMotionDuration.Ms900 => 900,
        NTMotionDuration.Ms1000 => 1000,
        _ => 500
    };
}
