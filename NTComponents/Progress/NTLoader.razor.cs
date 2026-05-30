using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using NTComponents.Core;
using NTComponents.Ext;
using NTComponents.Interfaces;

namespace NTComponents;

/// <summary>
///     Material 3 shape loading indicator.
/// </summary>
/// <remarks>
///     <para>
///         Use <see cref="NTLoader" /> for short indeterminate waits that use the Material 3 morphing shape spinner. For determinate progress, or for longer waits where progress is knowable, use
///         <see cref="NTProgress" />.
///     </para>
///     <para>
///         The initial loader shell is static SSR-friendly: it emits the starting shape, ARIA label, data attributes, and a page script tag. The morphing shape animation is enhanced by the isolated
///         JavaScript module after the page reaches the browser. In a static SSR page with scripts disabled, users still receive the accessible loading indicator but not the shape morphing enhancement.
///     </para>
///     <para>
///         Keep <see cref="AnimationDuration" /> at or above the minimum duration and provide at least two shapes when overriding <see cref="Shapes" /> for an animated loader. A single-shape animated
///         sequence has no visible morphing.
///     </para>
/// </remarks>
public partial class NTLoader : TnTDisposableComponentBase, ITnTPageScriptComponent<NTLoader> {

    /// <summary>
    ///     Whether the loading indicator motion is active.
    /// </summary>
    /// <remarks>
    ///     Static CSS state renders without an interactive Blazor render mode. Shape sequence updates are handled by the loader's isolated JavaScript enhancement.
    /// </remarks>
    [Parameter]
    public bool Animate { get; set; } = true;

    /// <summary>
    ///     Duration for one full shape step.
    /// </summary>
    /// <remarks>
    ///     Values below the loader minimum are clamped to keep motion legible. Prefer passing a duration of at least <c>400ms</c>.
    /// </remarks>
    [Parameter]
    public TimeSpan AnimationDuration { get; set; } = TimeSpan.FromMilliseconds(ShapeStepIntervalMilliseconds);

    /// <summary>
    ///     Accessible label used when no <c>aria-label</c> or <c>aria-labelledby</c> attribute is provided.
    /// </summary>
    [Parameter]
    public string? AriaLabel { get; set; } = "Loading";

    /// <summary>
    ///     Optional id of visible text that labels the loading indicator.
    /// </summary>
    [Parameter]
    public string? AriaLabelledBy { get; set; }

    /// <summary>
    ///     Color used by the moving indicator shape.
    /// </summary>
    [Parameter]
    public TnTColor Color { get; set; } = TnTColor.Primary;

    /// <summary>
    ///     Container color used by the contained variant.
    /// </summary>
    [Parameter]
    public TnTColor ContainerColor { get; set; } = TnTColor.PrimaryContainer;

    /// <inheritdoc />
    public override string? ElementClass => CssClassBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddClass("nt-loader")
        .AddClass(Variant == NTLoaderVariant.Contained ? "nt-loader-contained" : "nt-loader-uncontained")
        .AddClass("nt-loader-animated", Animate)
        .AddSize(Size)
        .Build();

    /// <inheritdoc />
    public override string? ElementStyle => CssStyleBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddVariable("nt-loader-indicator-color", Color.ToCssTnTColorVariable())
        .AddVariable("nt-loader-container-color", ContainerColor.ToCssTnTColorVariable())
        .AddVariable("nt-loader-size", SizeValue!, !string.IsNullOrWhiteSpace(SizeValue))
        .Build();

    /// <inheritdoc />
    public string? JsModulePath => LoaderJsModulePath;

    /// <inheritdoc />
    public DotNetObjectReference<NTLoader>? DotNetObjectRef { get; private set; }

    /// <inheritdoc />
    public IJSObjectReference? IsolatedJsModule { get; private set; }

    /// <summary>
    ///     The JSRuntime instance used for loader shape interop.
    /// </summary>
    [Inject]
    protected IJSRuntime JSRuntime { get; private set; } = default!;

    /// <summary>
    ///     Shape sequence used by the single morphing indicator mark.
    /// </summary>
    /// <remarks>
    ///     Leave this unset to use the Material shape sequence. When overriding it for an animated loader, provide at least two shapes so the loader can visibly morph.
    /// </remarks>
    [Parameter]
    public IReadOnlyList<NTShapeType>? Shapes { get; set; }

    /// <summary>
    ///     Duration used when morphing from one sequence step to the next.
    /// </summary>
    [Parameter]
    public NTMotionDuration ShapeTransitionDuration { get; set; } = NTMotionDuration.Ms700;

    /// <summary>
    ///     Easing used when morphing from one sequence step to the next.
    /// </summary>
    [Parameter]
    public NTMotionEasing ShapeTransitionEasing { get; set; } = NTMotionEasing.Emphasized;

    /// <summary>
    ///     Whether to render the loading indicator.
    /// </summary>
    [Parameter]
    public bool Show { get; set; } = true;

    /// <summary>
    ///     Token size for the loading indicator.
    /// </summary>
    [Parameter]
    public Size Size { get; set; } = Size.Medium;

    /// <summary>
    ///     Optional CSS size value for the shape loader.
    /// </summary>
    [Parameter]
    public string? SizeValue { get; set; }

    /// <summary>
    ///     Loading indicator visual treatment.
    /// </summary>
    [Parameter]
    public NTLoaderVariant Variant { get; set; } = NTLoaderVariant.Uncontained;

    private int EffectiveAnimationDurationMs => Math.Max(MinimumAnimationDurationMilliseconds, (int)Math.Round(AnimationDuration.TotalMilliseconds));
    private string? EffectiveAriaLabel => GetAdditionalAttributeValue("aria-label") ?? (!string.IsNullOrWhiteSpace(EffectiveAriaLabelledBy) ? null : AriaLabel);
    private string? EffectiveAriaLabelledBy => GetAdditionalAttributeValue("aria-labelledby") ?? AriaLabelledBy;
    private string EffectiveRole => GetAdditionalAttributeValue("role") ?? "progressbar";
    private string InitialShapePathData => NTShapeCatalog.GetPathData(_initialShape);
    private string ShapeClipPathId => $"nt-loader-shape-clip-{ComponentIdentifier}";
    private string ShapeContentStyle => $"clip-path:url(#{ShapeClipPathId});-webkit-clip-path:url(#{ShapeClipPathId});background-color:var(--nt-shape-content-background, transparent);";
    private string ShapeSequenceValue => _shapeSequenceValue;
    private IReadOnlyList<NTShapeType> _effectiveShapes = _defaultShapes;
    private NTShapeType _initialShape = NTShapeType.Hexagon;
    private string _shapeSequenceValue = string.Empty;
    private const string LoaderJsModulePath = "./_content/NTComponents/Progress/NTLoader.razor.js";
    private const int MinimumAnimationDurationMilliseconds = 400;
    private const int ShapeStepIntervalMilliseconds = 1250;

    private static readonly NTShapeType[] _defaultShapes = [
        NTShapeType.Hexagon,
        NTShapeType.TwelveSidedCookie,
        NTShapeType.SoftBurst,
        NTShapeType.Oval,
        NTShapeType.Pentagon,
        NTShapeType.Semicircle,
        NTShapeType.Puffy,
        NTShapeType.SixSidedCookie,
        NTShapeType.Sunny,
        NTShapeType.NineSidedCookie,
        NTShapeType.Boom,
        NTShapeType.Bun,
        NTShapeType.SevenSidedCookie,
        NTShapeType.Burst,
        NTShapeType.EightLeafClover,
        NTShapeType.Fan,
        NTShapeType.Flower,
        NTShapeType.Gem,
        NTShapeType.Pill,
        NTShapeType.PixelCircle,
        NTShapeType.PuffyDiamond,
        NTShapeType.Slanted,
        NTShapeType.FourLeafClover,
        NTShapeType.SoftBoom,
        NTShapeType.VerySunny,
        NTShapeType.FourSidedCookie
    ];

    /// <inheritdoc />
    protected override void OnParametersSet() {
        base.OnParametersSet();

        _effectiveShapes = Shapes is { Count: > 0 } ? Shapes : _defaultShapes;
        _initialShape = _effectiveShapes[0];
        _shapeSequenceValue = string.Join(' ', _effectiveShapes.Select(shape => ((int)shape).ToString(CultureInfo.InvariantCulture)));
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender) {
        await base.OnAfterRenderAsync(firstRender);

        if (!Show) {
            await DisposeShapeInteropAsync();
            return;
        }

        try {
            DotNetObjectRef ??= DotNetObjectReference.Create(this);

            if (IsolatedJsModule is null) {
                IsolatedJsModule = await JSRuntime.ImportIsolatedJs(this, JsModulePath);
                await (IsolatedJsModule?.InvokeVoidAsync("onLoad", Element, DotNetObjectRef) ?? ValueTask.CompletedTask);
            }

            await (IsolatedJsModule?.InvokeVoidAsync("onUpdate", Element, DotNetObjectRef) ?? ValueTask.CompletedTask);
        }
        catch (JSDisconnectedException) {
            // JS runtime was disconnected, safe to ignore during render.
        }
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing) {
        if (disposing) {
            DotNetObjectRef?.Dispose();
            DotNetObjectRef = null;
        }

        base.Dispose(disposing);
    }

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore() {
        await DisposeShapeInteropAsync().ConfigureAwait(false);
        await base.DisposeAsyncCore().ConfigureAwait(false);
    }

    private async ValueTask DisposeShapeInteropAsync() {
        if (IsolatedJsModule is not null) {
            try {
                await IsolatedJsModule.InvokeVoidAsync("onDispose", Element, DotNetObjectRef);
                await IsolatedJsModule.DisposeAsync().ConfigureAwait(false);
            }
            catch (JSDisconnectedException) {
                // JS runtime was disconnected, safe to ignore during disposal.
            }

            IsolatedJsModule = null;
        }

        DotNetObjectRef?.Dispose();
        DotNetObjectRef = null;
    }

    private string? GetAdditionalAttributeValue(string name)
        => AdditionalAttributes?.TryGetValue(name, out var value) == true && value is not null
            ? value.ToString()
            : null;
}

/// <summary>
///     Shape loader visual treatments.
/// </summary>
public enum NTLoaderVariant {

    /// <summary>
    ///     Loading shape without a container.
    /// </summary>
    Uncontained,

    /// <summary>
    ///     Loading shape inside a colored container.
    /// </summary>
    Contained
}
