using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using NTComponents.CodeDocumentation;
using NTComponents.Core;

namespace NTComponents;

/// <summary>
///     An <see cref="NTTab" /> that can pop out of its <see cref="NTTabView" /> into a floating <see cref="NTWindow" /> and return to its original position when the window closes.
/// </summary>
/// <remarks>
///     <para>
///         While popped out, the tab's header leaves the tab list and the tab is skipped for selection, but it stays registered in declaration order, so closing the window
///         returns it to the same position regardless of how many other tabs are popped out or in what order they return. The content keeps its component state across pop-out
///         and return.
///     </para>
///     <para>
///         Pop out with the panel corner button (see <see cref="PopoutButtonPlacement" />), the tab header context menu, or by binding <see cref="PoppedOut" />. The floating
///         window is rendered in place with <c>position: fixed</c>, so avoid ancestors that create a containing block for fixed elements, such as <c>transform</c>, <c>filter</c>,
///         or <c>contain</c>.
///     </para>
/// </remarks>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.ProgressivelyEnhanced,
    CompatibilitySummary = "Renders as a normal tab panel; popping out and returning require an interactive render mode.",
    CompatibilityDetails = "Static SSR emits the same panel markup as NTTab. The pop-out button and header menu item render only in interactive render modes, and returning a closed window to the tab view requires the interactive close callback.")]
public partial class NTPopoutTab {
    private RenderFragment? _headerMenuContent;
    private NTWindowState _windowState;

    /// <inheritdoc />
    public override string? ElementClass => PoppedOut
        ? CssClassBuilder.Create("nt-popout-tab-window").AddFromAdditionalAttributes(AdditionalAttributes).Build()
        : base.ElementClass;

    /// <summary>
    ///     Gets or sets the accessible label and tooltip for the pop-out button and header menu item. Defaults to <c>Open {Label} in a window</c>.
    /// </summary>
    [Parameter]
    public string? PopoutButtonAriaLabel { get; set; }

    /// <summary>
    ///     Gets or sets which panel corner shows the pop-out button, or <see cref="NTPopoutTabButtonPlacement.None" /> to rely on the header context menu or <see cref="PoppedOut" />.
    /// </summary>
    [Parameter]
    public NTPopoutTabButtonPlacement PopoutButtonPlacement { get; set; } = NTPopoutTabButtonPlacement.TopEnd;

    /// <summary>
    ///     Gets or sets whether the tab is shown in a floating window instead of the tab view.
    /// </summary>
    [Parameter]
    public bool PoppedOut { get; set; }

    /// <summary>
    ///     Gets or sets the callback invoked when <see cref="PoppedOut" /> changes because the user popped the tab out or closed its window.
    /// </summary>
    [Parameter]
    public EventCallback<bool> PoppedOutChanged { get; set; }

    /// <summary>
    ///     Gets or sets where the popped-out window moves when minimized.
    /// </summary>
    [Parameter]
    public NTWindowDockPosition WindowDockPosition { get; set; } = NTWindowDockPosition.BottomRight;

    internal override RenderFragment? HeaderMenuContent => RendererInfo.IsInteractive ? _headerMenuContent ??= BuildHeaderMenuContent : HeaderContextMenu;

    internal override bool IsDetached => PoppedOut;

    private string PopoutButtonClass => PopoutButtonPlacement switch {
        NTPopoutTabButtonPlacement.TopStart => "nt-popout-tab-action nt-popout-tab-action-top-start",
        NTPopoutTabButtonPlacement.BottomEnd => "nt-popout-tab-action nt-popout-tab-action-bottom-end",
        NTPopoutTabButtonPlacement.BottomStart => "nt-popout-tab-action nt-popout-tab-action-bottom-start",
        _ => "nt-popout-tab-action"
    };

    private string ResolvedPopoutButtonAriaLabel => PopoutButtonAriaLabel ?? $"Open {Label} in a window";

    private bool ShowPopoutButton => !PoppedOut && PopoutButtonPlacement != NTPopoutTabButtonPlacement.None && RendererInfo.IsInteractive;

    // Docked, the window renders inline and carries the tab-panel contract; floating, it only marks itself so the tab view can release it.
    private Dictionary<string, object> WindowAttributes {
        get {
            var attributes = AdditionalAttributes is null ? [] : new Dictionary<string, object>(AdditionalAttributes);
            attributes["class"] = ElementClass!;

            if (PoppedOut) {
                attributes["data-nt-tab-detached"] = "true";
                return attributes;
            }

            attributes["role"] = "tabpanel";
            attributes["aria-labelledby"] = ResolvedTabId;
            attributes["tabindex"] = "0";
            attributes["data-nt-tab-panel"] = "true";
            attributes["data-nt-tab-value"] = ResolvedValue;
            attributes["data-nt-tab-sequence"] = ResolvedSequence;
            if (!IsInitiallySelected) {
                attributes["hidden"] = true;
            }

            return attributes;
        }
    }

    private void BuildHeaderMenuContent(RenderTreeBuilder builder) {
        builder.OpenComponent<NTMenuButtonItem>(0);
        builder.AddComponentParameter(1, nameof(NTMenuButtonItem.Label), ResolvedPopoutButtonAriaLabel);
        builder.AddComponentParameter(2, nameof(NTMenuButtonItem.Icon), MaterialIcon.OpenInNew);
        builder.AddComponentParameter(3, nameof(NTMenuButtonItem.OnClickCallback), EventCallback.Factory.Create<MouseEventArgs>(this, PopOutAsync));
        builder.CloseComponent();
        if (HeaderContextMenu is not null) {
            builder.OpenComponent<NTMenuDividerItem>(4);
            builder.CloseComponent();
            builder.AddContent(5, HeaderContextMenu);
        }
    }

    private Task HandleWindowOpenChanged(bool open) => open ? Task.CompletedTask : SetPoppedOutAsync(false);

    private Task PopOutAsync() => SetPoppedOutAsync(true);

    private async Task SetPoppedOutAsync(bool poppedOut) {
        if (PoppedOut == poppedOut) {
            return;
        }

        PoppedOut = poppedOut;
        if (poppedOut) {
            _windowState = NTWindowState.Normal;
        }

        _context.NotifyTabDetachedChanged();
        await PoppedOutChanged.InvokeAsync(poppedOut);
    }
}
