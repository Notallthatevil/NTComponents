using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using NTComponents.Core;
using NTComponents.Ext;
using NTComponents.Popover;

namespace NTComponents;

/// <summary>
///     Renders a single floating popover window.
/// </summary>
public partial class NTPopoverWindow : IAsyncDisposable {
    private const string JsModulePath = "./_content/NTComponents/Popover/NTPopoverWindow.razor.js";
    private PopoverDismissAction _dismissAction;
    private readonly DotNetObjectReference<NTPopoverWindow> _dotNetObjectReference;
    private readonly IJSRuntime _jsRuntime;
    private readonly INTPopoverService _popoverService;
    private readonly string _descriptionElementId = TnTComponentIdentifier.NewId();
    private readonly string _titleElementId = TnTComponentIdentifier.NewId();
    private bool _hasAnimatedFromLauncher;
    private bool _isEntering = true;
    private int _lastHighlightRequestId;
    private IJSObjectReference? _jsModule;
    private ElementReference _windowElement;

    /// <summary>
    ///     Initializes a new instance of the <see cref="NTPopoverWindow" /> class.
    /// </summary>
    /// <param name="jsRuntime">The JavaScript runtime.</param>
    /// <param name="popoverService">The popover service.</param>
    public NTPopoverWindow(IJSRuntime jsRuntime, INTPopoverService popoverService) {
        _jsRuntime = jsRuntime;
        _popoverService = popoverService;
        _dotNetObjectReference = DotNetObjectReference.Create(this);
    }

    /// <summary>
    ///     Gets or sets the popover handle being rendered.
    /// </summary>
    [Parameter, EditorRequired]
    public INTPopoverHandle Popover { get; set; } = default!;

    /// <summary>
    ///     Gets or sets a value indicating whether the popover should animate from the launcher strip.
    /// </summary>
    [Parameter]
    public bool AnimateFromLauncher { get; set; }

    /// <summary>
    ///     Gets or sets a callback invoked after a launcher-enter animation completes.
    /// </summary>
    [Parameter]
    public EventCallback<string> OnLauncherEnterAnimationCompleted { get; set; }

    private string? DescriptionElementId => string.IsNullOrWhiteSpace(Popover.Options.Description) ? null : _descriptionElementId;

    private string DisplayTitle => string.IsNullOrWhiteSpace(Popover.Options.Title) ? "Window" : Popover.Options.Title!;

    private bool IsDismissPending => _dismissAction is not PopoverDismissAction.None;

    private int HighlightRequestId => Popover is INTPopoverHighlightState highlightState
        ? highlightState.HighlightRequestId
        : 0;

    private string ElementClass => CssClassBuilder.Create("nt-popover")
        .AddClass("nt-popover--dragging", false)
        .AddClass("nt-popover--entering", _isEntering && !AnimateFromLauncher)
        .AddClass("nt-popover--hidden", !Popover.IsVisible)
        // Hide uses a JS WAAPI animation (animatePopoverToLauncher) and needs no CSS leaving class.
        // Only Close relies on the CSS exit animation to signal when to complete.
        .AddClass("nt-popover--leaving", _dismissAction == PopoverDismissAction.Close)
        .AddClass(Popover.Options.ElementClass)
        .Build();

    private string? ElementStyle => CssStyleBuilder.Create()
        .Add(Popover.Options.ElementStyle!)
        .AddStyle("left", $"{Popover.Left.ToString(System.Globalization.CultureInfo.InvariantCulture)}px")
        .AddStyle("top", $"{Popover.Top.ToString(System.Globalization.CultureInfo.InvariantCulture)}px")
        .AddStyle("z-index", Popover.ZIndex.ToString(System.Globalization.CultureInfo.InvariantCulture))
        .AddVariable("nt-popover-bg-color", Popover.Options.BackgroundColor)
        .AddVariable("nt-popover-fg-color", Popover.Options.TextColor)
        .AddVariable("nt-popover-width", Popover.Options.Width)
        .AddVariable("nt-popover-height", Popover.Options.Height)
        .AddVariable("nt-popover-min-width", Popover.Options.MinWidth)
        .AddVariable("nt-popover-min-height", Popover.Options.MinHeight)
        .AddVariable("nt-popover-max-width", Popover.Options.MaxWidth)
        .AddVariable("nt-popover-max-height", Popover.Options.MaxHeight)
        .Build();

    /// <inheritdoc />
    public async ValueTask DisposeAsync() {
        if (_jsModule is not null) {
            try {
                await _jsModule.InvokeVoidAsync("disposePopoverWindow", _windowElement);
                await _jsModule.DisposeAsync();
            }
            catch (JSDisconnectedException) {
            }
        }

        _dotNetObjectReference.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender) {
        await base.OnAfterRenderAsync(firstRender);
        if (!RendererInfo.IsInteractive) {
            return;
        }

        try {
            var highlightRequestId = HighlightRequestId;
            if (firstRender) {
                _lastHighlightRequestId = highlightRequestId;
                _jsModule = await _jsRuntime.ImportIsolatedJs(this, JsModulePath);
                await _jsModule.InvokeVoidAsync("initializePopoverWindow", _windowElement, _dotNetObjectReference, CreateClientOptions(), AnimateFromLauncher);
            }
            else if (_jsModule is not null) {
                await _jsModule.InvokeVoidAsync("updatePopoverWindow", _windowElement, CreateClientOptions());

                if (highlightRequestId != _lastHighlightRequestId) {
                    _lastHighlightRequestId = highlightRequestId;
                    _isEntering = false;
                    await _jsModule.InvokeVoidAsync("highlightPopoverWindow", _windowElement);
                }
            }

            if (AnimateFromLauncher && !_hasAnimatedFromLauncher && _jsModule is not null) {
                _hasAnimatedFromLauncher = true;
                _isEntering = false;
                await _jsModule.InvokeVoidAsync("animatePopoverFromLauncher", _windowElement, Popover.ElementId);

                if (OnLauncherEnterAnimationCompleted.HasDelegate) {
                    await OnLauncherEnterAnimationCompleted.InvokeAsync(Popover.ElementId);
                }
            }
        }
        catch (JSDisconnectedException) {
        }
    }

    /// <summary>
    ///     Invoked by JavaScript when the popover should be activated.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [JSInvokable]
    public async Task NotifyActivated() {
        await Popover.BringToFrontAsync();
    }

    /// <summary>
    ///     Invoked by JavaScript when the popover position changes after a drag operation.
    /// </summary>
    /// <param name="left">The new left position in pixels.</param>
    /// <param name="top">The new top position in pixels.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [JSInvokable]
    public async Task NotifyPositionChanged(double left, double top) {
        await _popoverService.UpdatePositionAsync(Popover, left, top);
    }

    private object CreateClientOptions() => new {
        allowDragging = Popover.Options.AllowDragging,
        left = Popover.Left,
        top = Popover.Top,
        viewportPadding = 16
    };

    private async Task CloseAsync() {
        if (IsDismissPending) {
            return;
        }

        _isEntering = false;
        _dismissAction = PopoverDismissAction.Close;
        await InvokeAsync(StateHasChanged);

        if (!RendererInfo.IsInteractive || _jsModule is null) {
            _dismissAction = PopoverDismissAction.None;
            await Popover.CloseAsync();
            return;
        }

        try {
            await _jsModule.InvokeVoidAsync("waitForCloseAnimation", _windowElement);
            _dismissAction = PopoverDismissAction.None;
            await Popover.CloseAsync();
        }
        catch (JSDisconnectedException) {
            _dismissAction = PopoverDismissAction.None;
            await Popover.CloseAsync();
        }
    }

    private async Task HideAsync() {
        if (IsDismissPending) {
            return;
        }

        _isEntering = false;
        _dismissAction = PopoverDismissAction.Hide;
        await InvokeAsync(StateHasChanged);

        if (!RendererInfo.IsInteractive || _jsModule is null) {
            _dismissAction = PopoverDismissAction.None;
            await Popover.HideAsync();
            return;
        }

        try {
            await _jsModule.InvokeVoidAsync("animatePopoverToLauncher", _windowElement, Popover.ElementId);
            _dismissAction = PopoverDismissAction.None;
            await Popover.HideAsync();
        }
        catch (JSDisconnectedException) {
            _dismissAction = PopoverDismissAction.None;
            await Popover.HideAsync();
        }
    }

    /// <summary>
    ///     Invoked by JavaScript when the popover enter animation has completed.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [JSInvokable]
    public async Task NotifyEnterAnimationCompleted() {
        _isEntering = false;
        await InvokeAsync(StateHasChanged);
    }

    private RenderFragment RenderContent() {
        return builder => {
            if (Popover.ChildContent is not null) {
                builder.AddContent(0, Popover.ChildContent);
                return;
            }

            if (Popover.Type is null) {
                return;
            }

            builder.OpenComponent(10, Popover.Type);
#pragma warning disable CS8620
            builder.AddMultipleAttributes(20, Popover.Parameters);
#pragma warning restore CS8620
            builder.CloseComponent();
        };
    }

    private async Task OnHeaderKeyDownAsync(KeyboardEventArgs args) {
        if (!Popover.Options.AllowDragging) {
            return;
        }

        var step = args.ShiftKey ? 48 : 16;
        var left = Popover.Left;
        var top = Popover.Top;

        switch (args.Key) {
            case "ArrowLeft":
                left -= step;
                break;
            case "ArrowRight":
                left += step;
                break;
            case "ArrowUp":
                top -= step;
                break;
            case "ArrowDown":
                top += step;
                break;
            default:
                return;
        }

        await Popover.BringToFrontAsync();
        await _popoverService.UpdatePositionAsync(Popover, left, top);
    }

    private async Task OnPointerDownAsync() => await Popover.BringToFrontAsync();

    private async Task OnWindowKeyDownAsync(KeyboardEventArgs args) {
        if (args.Key == "Escape" && Popover.Options.CloseOnEscape) {
            await CloseAsync();
        }
    }

    private enum PopoverDismissAction {
        None,
        Hide,
        Close
    }
}
