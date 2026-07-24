using Microsoft.AspNetCore.Components;
using NTComponents.CodeDocumentation;
using NTComponents.Core;

namespace NTComponents;

/// <summary>
///     Floating Material 3 surface for arbitrary content that can be dragged, minimized, expanded to the viewport, or closed.
/// </summary>
/// <remarks>
///     <para>
///         Use <see cref="NTWindow" /> for a free-standing temporary working surface above the page. For windows that survive route
///         navigation, place <see cref="NTWindowHost" /> in a persistent layout and open content with <see cref="INTWindowService" />.
///     </para>
///     <para>
///         Static SSR emits the complete title, controls, content, and page-script marker. The browser module progressively
///         enhances the static markup with dragging and window controls without requiring a Blazor interactive render mode.
///         Interactive render modes additionally receive the state and close callbacks.
///     </para>
/// </remarks>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.ProgressivelyEnhanced,
    CompatibilitySummary = "Renders a complete floating window and progressively enhances it without requiring Blazor interactivity.",
    CompatibilityDetails = "Static SSR emits the title, controls, arbitrary child content, initial state, and page script. The browser module supplies drag, minimize, fullscreen, and close behavior; Blazor callbacks are also invoked when an interactive render mode is present.")]
public partial class NTWindow : NTComponentBase {

    /// <summary>
    ///     The static web asset path for the window JavaScript module.
    /// </summary>
    public const string JsModulePathValue = "./_content/NTComponents/Window/NTWindow.razor.js";

    /// <summary>
    ///     Gets or sets the content rendered inside the window.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    ///     Gets or sets where the window moves when minimized.
    /// </summary>
    [Parameter]
    public NTWindowDockPosition DockPosition { get; set; } = NTWindowDockPosition.BottomRight;

    /// <summary>
    ///     Gets or sets the accessible label for the close button. Defaults to <c>Close {Title}</c>.
    /// </summary>
    [Parameter]
    public string? CloseButtonAriaLabel { get; set; }

    /// <summary>
    ///     Gets or sets whether the window can be moved by dragging its header.
    /// </summary>
    [Parameter]
    public bool Draggable { get; set; } = true;

    /// <inheritdoc />
    public override string? ElementClass => CssClassBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddClass("nt-window")
        .AddClass("nt-window-minimized", State == NTWindowState.Minimized)
        .AddClass("nt-window-fullscreen", State == NTWindowState.Fullscreen)
        .Build();

    /// <inheritdoc />
    public override string? ElementStyle => CssStyleBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .Build();

    /// <summary>
    ///     Gets or sets the accessible label for the fullscreen toggle. A state-specific label is used by default.
    /// </summary>
    [Parameter]
    public string? FullscreenButtonAriaLabel { get; set; }

    /// <summary>
    ///     Gets or sets the accessible label for the minimize toggle. A state-specific label is used by default.
    /// </summary>
    [Parameter]
    public string? MinimizeButtonAriaLabel { get; set; }

    /// <summary>
    ///     Gets or sets whether the window is rendered.
    /// </summary>
    [Parameter]
    public bool Open { get; set; } = true;

    /// <summary>
    ///     Gets or sets the callback invoked when <see cref="Open" /> changes.
    /// </summary>
    [Parameter]
    public EventCallback<bool> OpenChanged { get; set; }

    /// <summary>
    ///     Gets or sets the callback invoked after the close control is activated.
    /// </summary>
    [Parameter]
    public EventCallback OnClosed { get; set; }

    /// <summary>
    ///     Gets or sets the current window state.
    /// </summary>
    [Parameter]
    public NTWindowState State { get; set; }

    /// <summary>
    ///     Gets or sets the callback invoked when <see cref="State" /> changes.
    /// </summary>
    [Parameter]
    public EventCallback<NTWindowState> StateChanged { get; set; }

    /// <summary>
    ///     Gets or sets the visible and accessible window title.
    /// </summary>
    [Parameter, EditorRequired]
    public string Title { get; set; } = string.Empty;

    private string CloseAriaLabel => CloseButtonAriaLabel ?? $"Close {Title}";
    private string ContentExpanded => (State != NTWindowState.Minimized).ToString().ToLowerInvariant();
    private string ContentHidden => (State == NTWindowState.Minimized).ToString().ToLowerInvariant();
    private string ContentId => $"{ComponentIdentifier}-content";
    private string DockPositionValue => DockPosition switch {
        NTWindowDockPosition.TopLeft => "top-left",
        NTWindowDockPosition.TopCenter => "top-center",
        NTWindowDockPosition.TopRight => "top-right",
        NTWindowDockPosition.BottomLeft => "bottom-left",
        NTWindowDockPosition.BottomCenter => "bottom-center",
        NTWindowDockPosition.BottomRight => "bottom-right",
        _ => throw new ArgumentOutOfRangeException(nameof(DockPosition), DockPosition, "The window dock position is not supported.")
    };
    private string DraggableValue => Draggable.ToString().ToLowerInvariant();
    private string FullscreenAriaLabel => FullscreenButtonAriaLabel ?? (State == NTWindowState.Fullscreen ? $"Restore {Title}" : $"Show {Title} fullscreen");
    private TnTIcon FullscreenIcon => State == NTWindowState.Fullscreen ? MaterialIcon.FullscreenExit : MaterialIcon.Fullscreen;
    private string FullscreenPressed => (State == NTWindowState.Fullscreen).ToString().ToLowerInvariant();
    private string MinimizeAriaLabel => MinimizeButtonAriaLabel ?? (State == NTWindowState.Minimized ? $"Restore {Title}" : $"Minimize {Title}");
    private TnTIcon MinimizeIcon => State == NTWindowState.Minimized ? MaterialIcon.KeyboardArrowUp : MaterialIcon.Minimize;
    private string StateValue => State.ToString().ToLowerInvariant();
    private string TitleId => $"{ComponentIdentifier}-title";

    /// <inheritdoc />
    protected override void OnParametersSet() {
        base.OnParametersSet();

        if (string.IsNullOrWhiteSpace(Title)) {
            throw new ArgumentException("NTWindow requires a non-empty Title.", nameof(Title));
        }

        if (State is not (NTWindowState.Normal or NTWindowState.Minimized or NTWindowState.Fullscreen)) {
            throw new ArgumentOutOfRangeException(nameof(State), State, "The window state is not supported.");
        }

        _ = DockPositionValue;
    }

    private async Task CloseAsync() {
        Open = false;
        await OpenChanged.InvokeAsync(false);
        await OnClosed.InvokeAsync();
    }

    private Task ToggleFullscreenAsync() => SetStateAsync(State == NTWindowState.Fullscreen ? NTWindowState.Normal : NTWindowState.Fullscreen);

    private Task ToggleMinimizedAsync() => SetStateAsync(State == NTWindowState.Minimized ? NTWindowState.Normal : NTWindowState.Minimized);

    private async Task SetStateAsync(NTWindowState state) {
        State = state;
        await StateChanged.InvokeAsync(state);
    }
}
