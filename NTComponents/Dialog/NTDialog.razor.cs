using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;
using NTComponents.Core;
using NTComponents.Ext;

using NTComponents.CodeDocumentation;
namespace NTComponents;

/// <summary>
/// Material 3 modal dialog built on the native HTML <c> dialog</c> element.
/// </summary>
/// <remarks>
/// <para> Do use <see cref="NTDialog" /> for short, modal tasks that require a user decision, confirmation, focused
/// content review, or a small amount of form input before returning to the current workflow. Keep dialog copy concise
/// and make the primary action clear. </para> <para> Do place the dialog near the page or layout content that owns it
/// and open it with a component reference, native <c> command</c>/<c>commandfor</c> attributes, or JavaScript by id.
/// The dialog content is ordinary Razor child content, so callers do not need to register root components or use a
/// dialog service for owned dialog content. </para> <para> Do provide a meaningful <see cref="Title" /> for assistive
/// technology and keep action buttons in <see cref="Buttons" />. Use the optional <see cref="Icon" /> only when it adds
/// quick recognition; icon dialogs are center-aligned by design. </para> <para> Do not use dialogs for nonblocking
/// status, long reading experiences, broad navigation, or workflows that should remain visible beside the page. Prefer
/// inline content, a page, a sheet, or another persistent surface when users need background context while working.
/// </para> <para> By default, Escape can request closure and backdrop clicks do not close the dialog. Use
/// <see cref="OnOpen" /> and <see cref="OnClose" /> to cancel lifecycle transitions when validation, unsaved work, or
/// application policy requires the dialog to remain open. </para>
/// </remarks>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.ProgressivelyEnhanced,
    CompatibilitySummary = "Renders a native dialog element and enhances lifecycle control with JavaScript.",
    CompatibilityDetails = "Static SSR can render the dialog markup, including an initially open dialog. Programmatic open and close APIs, cancelable lifecycle callbacks, backdrop behavior, and Escape handling require the browser module.")]
public partial class NTDialog {
    /// <summary>
    /// Gets the isolated JavaScript module path for <see cref="NTDialog" />.
    /// </summary>
    public const string JsModulePathValue = "./_content/NTComponents/Dialog/NTDialog.razor.js";

    private readonly RenderFragment _defaultButtons;
    private long _childContentRenderKey;
    private NTDialogParameters _dialogParameters = [];
    private PendingOpenRequest? _pendingOpenRequest;
    private bool _renderChildContent;

    /// <summary>
    /// Initializes a new instance of the <see cref="NTDialog" /> class.
    /// </summary>
    public NTDialog() => _defaultButtons = BuildDefaultButtons;

    private string CloseOnBackdropAttribute => CloseOnBackdrop ? "true" : "false";

    private string CloseOnEscapeAttribute => CloseOnEscape ? "true" : "false";

    private string ResolvedElementId => Id ?? ElementId ?? ComponentIdentifier;

    private string? SupportingTextId => string.IsNullOrWhiteSpace(SupportingText) ? null : $"{ResolvedElementId}-supporting-text";

    private string? TitleId => ShouldRenderTitle ? $"{ResolvedElementId}-title" : null;

    private string ActionsClass => CssClassBuilder.Create("nt-dialog-actions")
        .AddClass("nt-dialog-actions-space-between", ButtonSpacing == NTDialogButtonSpacing.SpaceBetween)
        .Build();

    private bool ShouldRenderChildContent => !RendererInfo.IsInteractive || Open || _renderChildContent;

    private bool ShouldRenderTitle => !string.IsNullOrWhiteSpace(Title) || (TitleContent is not null && ShouldRenderChildContent);

    /// <summary>
    /// Gets or sets the dialog action buttons. Defaults to a single native close button.
    /// </summary>
    /// <remarks>
    /// Keep dialog actions short, place the dismissive action before the confirming action, and use native <c>
    /// command="request-close"</c> with <c> commandfor</c> when possible so JavaScript can coordinate the cancelable
    /// close lifecycle.
    /// </remarks>
    [Parameter]
    public RenderFragment<NTDialogParameters>? Buttons { get; set; }

    /// <summary>
    /// Gets or sets how the dialog action buttons are spaced inside the actions row.
    /// </summary>
    [Parameter]
    public NTDialogButtonSpacing ButtonSpacing { get; set; } = NTDialogButtonSpacing.End;

    /// <summary>
    /// Gets or sets the dialog body content.
    /// </summary>
    /// <remarks>
    /// Use this for the focused content or form fields the dialog owns. Long content is constrained to the body scroll
    /// container so the title and actions remain visible.
    /// </remarks>
    [Parameter]
    public RenderFragment<NTDialogParameters>? ChildContent { get; set; }

    /// <summary>
    /// Gets or sets the accessible label for the optional close icon button.
    /// </summary>
    [Parameter]
    public string CloseButtonAriaLabel { get; set; } = "Close dialog";

    /// <summary>
    /// Gets or sets whether backdrop clicks request dialog closure.
    /// </summary>
    /// <remarks>
    /// Defaults to <see langword="false" /> so accidental outside clicks do not dismiss modal work. Enable only for
    /// lightweight dialogs where losing the current dialog state is acceptable.
    /// </remarks>
    [Parameter]
    public bool CloseOnBackdrop { get; set; }

    /// <summary>
    /// Gets or sets whether Escape requests dialog closure.
    /// </summary>
    /// <remarks>
    /// Defaults to <see langword="true" /> for native modal keyboard behavior. Use <see cref="OnClose" /> when Escape
    /// should be conditionally blocked by validation or unsaved changes.
    /// </remarks>
    [Parameter]
    public bool CloseOnEscape { get; set; } = true;

    /// <summary>
    /// Gets or sets the dialog element id. This is an alias for component-style usage; the base component element id
    /// remains supported.
    /// </summary>
    [Parameter]
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the dialog container elevation.
    /// </summary>
    /// <remarks>
    /// Defaults to <see cref="NTElevation.Medium" /> to keep the modal container visually separated from the scrimmed
    /// page surface. Lower it only when the surrounding surface hierarchy already provides enough separation.
    /// </remarks>
    [Parameter]
    public NTElevation Elevation { get; set; } = NTElevation.Medium;

    /// <inheritdoc />
    public override string? ElementClass => CssClassBuilder.Create("nt-dialog")
        .AddElevation(Elevation)
        .AddClass("nt-dialog-has-icon", Icon is not null)
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .Build();

    /// <inheritdoc />
    public override string? ElementStyle => AdditionalAttributes?.GetValueOrDefault("style")?.ToString();

    /// <inheritdoc />
    public override string? JsModulePath => JsModulePathValue;

    /// <summary>
    /// Gets or sets optional icon content rendered above the dialog title.
    /// </summary>
    /// <remarks>
    /// Icons are decorative and rendered with <c> aria-hidden</c>. Use an icon for quick recognition of a dialog
    /// category or outcome, not as the only way to communicate severity or meaning.
    /// </remarks>
    [Parameter]
    public RenderFragment? Icon { get; set; }

    /// <summary>
    /// Gets or sets whether the dialog is rendered open during static markup rendering.
    /// </summary>
    /// <remarks>
    /// Use this for static SSR scenarios where the initial HTML should include an open dialog. Interactive opening
    /// after render should use <see cref="OpenAsync(CancellationToken)" />, native command attributes, or the
    /// JavaScript module.
    /// </remarks>
    [Parameter]
    public bool Open { get; set; }

    /// <summary>
    /// Invoked before an open request continues. Set <see cref="NTDialogEventArgs.Cancel" /> to <see langword="true" />
    /// to stop the dialog from opening.
    /// </summary>
    /// <remarks>
    /// Use this for guards that decide whether a requested dialog should open. Keep work short because the native open
    /// is waiting for this callback.
    /// </remarks>
    [Parameter]
    public EventCallback<NTDialogEventArgs> OnOpen { get; set; }

    /// <summary>
    /// Invoked after an open request has been accepted and before the native dialog is opened.
    /// </summary>
    [Parameter]
    public EventCallback<NTDialogEventArgs> OnOpening { get; set; }

    /// <summary>
    /// Invoked after the native dialog has opened.
    /// </summary>
    [Parameter]
    public EventCallback<NTDialogEventArgs> OnOpened { get; set; }

    /// <summary>
    /// Invoked before a close request continues. Set <see cref="NTDialogEventArgs.Cancel" /> to <see langword="true" />
    /// to stop the dialog from closing.
    /// </summary>
    /// <remarks>
    /// Use this for validation, dirty-state confirmation, or policy checks that must prevent closure. If the close is
    /// allowed, <see cref="OnClosing" /> and <see cref="OnClosed" /> continue the lifecycle.
    /// </remarks>
    [Parameter]
    public EventCallback<NTDialogEventArgs> OnClose { get; set; }

    /// <summary>
    /// Invoked after a close request has been accepted and before the native dialog is closed.
    /// </summary>
    [Parameter]
    public EventCallback<NTDialogEventArgs> OnClosing { get; set; }

    /// <summary>
    /// Invoked after the native dialog has closed.
    /// </summary>
    [Parameter]
    public EventCallback<NTDialogEventArgs> OnClosed { get; set; }

    /// <summary>
    /// Gets or sets whether the header close icon button is rendered.
    /// </summary>
    [Parameter]
    public bool ShowCloseButton { get; set; }

    /// <summary>
    /// Gets or sets optional supporting text rendered below the title.
    /// </summary>
    /// <remarks>
    /// Keep supporting text brief. If the explanation needs multiple paragraphs, prefer putting that content in
    /// <see cref="ChildContent" /> so the body scroll and divider behavior applies.
    /// </remarks>
    [Parameter]
    public string? SupportingText { get; set; }

    /// <summary>
    /// Gets or sets the dialog title.
    /// </summary>
    /// <remarks>
    /// Provide a concise title that names the decision or task. The title is used as the dialog label through <c>
    /// aria-labelledby</c>.
    /// </remarks>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets custom title content rendered in place of <see cref="Title" />.
    /// </summary>
    /// <remarks>
    /// The template receives the current dialog parameters supplied through <see cref="OpenAsync(NTDialogParameters?, CancellationToken)" />
    /// or <see cref="RefreshAsync(NTDialogParameters?, CancellationToken)" />. Keep the rendered content concise because it remains the dialog label through <c>aria-labelledby</c>.
    /// </remarks>
    [Parameter]
    public RenderFragment<NTDialogParameters>? TitleContent { get; set; }

    /// <summary>
    /// Gets the default action button content.
    /// </summary>
    protected RenderFragment DefaultButtons => _defaultButtons;

    private void BuildDefaultButtons(RenderTreeBuilder builder) {
        builder.OpenElement(0, "button");
        builder.AddAttribute(1, "type", "button");
        builder.AddAttribute(2, "class", "nt-dialog-button");
        builder.AddAttribute(3, "command", "request-close");
        builder.AddAttribute(4, "commandfor", ResolvedElementId);
        builder.AddContent(5, "Close");
        builder.CloseElement();
    }

    /// <summary>
    /// Opens the dialog when the component is interactive.
    /// </summary>
    public async ValueTask<bool> OpenAsync(CancellationToken cancellationToken = default) {
        return await OpenAsync([], cancellationToken);
    }

    /// <summary>
    /// Opens the dialog when the component is interactive and supplies parameters to the dialog body template.
    /// </summary>
    public async ValueTask<bool> OpenAsync(NTDialogParameters? parameters, CancellationToken cancellationToken = default) {
        if (IsolatedJsModule is null) {
            throw new InvalidOperationException($"{nameof(NTDialog)} cannot be opened from .NET until it has rendered interactively.");
        }

        if (_pendingOpenRequest is not null) {
            return false;
        }

        if (await IsolatedJsModule.InvokeAsync<bool>("isOpen", cancellationToken, Element)) {
            return false;
        }

        if (!await RequestOpenAsync()) {
            return false;
        }

        return await QueueOpenAfterRender(parameters ?? [], cancellationToken).WaitAsync(cancellationToken);
    }

    /// <summary>
    /// Closes the dialog when the component is interactive.
    /// </summary>
    public async ValueTask<bool> CloseAsync(string? returnValue = null, CancellationToken cancellationToken = default) {
        if (IsolatedJsModule is null) {
            throw new InvalidOperationException($"{nameof(NTDialog)} cannot be closed from .NET until it has rendered interactively.");
        }

        if (!await IsolatedJsModule.InvokeAsync<bool>("isOpen", cancellationToken, Element)) {
            return false;
        }

        if (!await RequestCloseAsync(returnValue)) {
            return false;
        }

        return await IsolatedJsModule.InvokeAsync<bool>("closeDialogFromBlazor", cancellationToken, Element, returnValue);
    }

    /// <summary>
    /// Forces the dialog body and its child content to rerender.
    /// </summary>
    public async ValueTask RefreshAsync(CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();

        _childContentRenderKey++;
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Forces the dialog body and its child content to rerender with updated dialog parameters.
    /// </summary>
    /// <param name="parameters">
    /// Parameters to assign to the dialog body template. This replaces the current parameters, so callers should include
    /// any existing values that should be preserved. Null is treated as an empty parameter collection.
    /// </param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    public async ValueTask RefreshAsync(NTDialogParameters? parameters, CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();

        _dialogParameters = parameters ?? [];
        await RefreshAsync(cancellationToken);
    }

    /// <summary>
    /// Requests dialog opening from JavaScript.
    /// </summary>
    [JSInvokable]
    public async Task<bool> RequestOpenFromJavaScript() {
        if (IsolatedJsModule is null || _pendingOpenRequest is not null || await IsolatedJsModule.InvokeAsync<bool>("isOpen", Element)) {
            return false;
        }

        if (!await RequestOpenAsync()) {
            return false;
        }

        _ = QueueOpenAfterRender([], CancellationToken.None);
        return false;
    }

    /// <summary>
    /// Notifies the component that JavaScript opened the dialog.
    /// </summary>
    [JSInvokable]
    public async Task NotifyOpenedFromJavaScript() {
        await NotifyOpenedAsync(null);
    }

    /// <summary>
    /// Requests dialog closure from JavaScript.
    /// </summary>
    [JSInvokable]
    public async Task<bool> RequestCloseFromJavaScript(string? returnValue) {
        return await RequestCloseAsync(returnValue);
    }

    /// <summary>
    /// Notifies the component that JavaScript closed the dialog.
    /// </summary>
    [JSInvokable]
    public async Task NotifyClosedFromJavaScript(string? returnValue) {
        await NotifyClosedAsync(returnValue);
    }

    private async Task<bool> RequestOpenAsync() {
        var args = new NTDialogEventArgs(ResolvedElementId);
        await OnOpen.InvokeAsync(args);
        if (args.Cancel) {
            return false;
        }

        await OnOpening.InvokeAsync(args);
        return true;
    }

    private async Task NotifyOpenedAsync(string? returnValue) {
        var args = new NTDialogEventArgs(ResolvedElementId, returnValue);
        await OnOpened.InvokeAsync(args);
    }

    private async Task<bool> RequestCloseAsync(string? returnValue) {
        var args = new NTDialogEventArgs(ResolvedElementId, returnValue);
        await OnClose.InvokeAsync(args);
        if (args.Cancel) {
            return false;
        }

        await OnClosing.InvokeAsync(args);
        return true;
    }

    private async Task NotifyClosedAsync(string? returnValue) {
        var args = new NTDialogEventArgs(ResolvedElementId, returnValue);
        await OnClosed.InvokeAsync(args);
        if (RendererInfo.IsInteractive) {
            _renderChildContent = false;
            _dialogParameters = [];
            await InvokeAsync(StateHasChanged);
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender) {
        await base.OnAfterRenderAsync(firstRender);

        if (_pendingOpenRequest is null) {
            return;
        }

        var pendingOpenRequest = _pendingOpenRequest;
        _pendingOpenRequest = null;

        if (pendingOpenRequest.Completion.Task.IsCompleted) {
            return;
        }

        try {
            if (IsolatedJsModule is null) {
                pendingOpenRequest.Completion.TrySetException(new InvalidOperationException($"{nameof(NTDialog)} cannot be opened from .NET until it has rendered interactively."));
                return;
            }

            var opened = await IsolatedJsModule.InvokeAsync<bool>("openDialogFromBlazor", pendingOpenRequest.CancellationToken, Element);
            if (opened) {
                await NotifyOpenedAsync(null);
            }

            pendingOpenRequest.Completion.TrySetResult(opened);
        }
        catch (Exception exception) {
            pendingOpenRequest.Completion.TrySetException(exception);
        }
    }

    private Task<bool> QueueOpenAfterRender(NTDialogParameters parameters, CancellationToken cancellationToken) {
        _renderChildContent = true;
        _dialogParameters = parameters;
        _childContentRenderKey++;

        var pendingOpenRequest = new PendingOpenRequest(cancellationToken);
        _pendingOpenRequest = pendingOpenRequest;

        _ = InvokeAsync(StateHasChanged);
        return pendingOpenRequest.Completion.Task;
    }

    private sealed class PendingOpenRequest(CancellationToken cancellationToken) {
        public CancellationToken CancellationToken { get; } = cancellationToken;

        public TaskCompletionSource<bool> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}

/// <summary>
/// Event data for <see cref="NTDialog" /> lifecycle callbacks.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class NTDialogEventArgs(string dialogId, string? returnValue = null) {
    /// <summary>
    /// Gets or sets whether a cancelable lifecycle action should stop.
    /// </summary>
    public bool Cancel { get; set; }

    /// <summary>
    /// Gets the dialog element id.
    /// </summary>
    public string DialogId { get; } = dialogId;

    /// <summary>
    /// Gets the native dialog return value when closing.
    /// </summary>
    public string? ReturnValue { get; } = returnValue;
}

/// <summary>
/// Spacing options for <see cref="NTDialog" /> action buttons.
/// </summary>
public enum NTDialogButtonSpacing {
    /// <summary>
    /// Places dialog action buttons at the inline end of the actions row.
    /// </summary>
    End,

    /// <summary>
    /// Distributes dialog action buttons with the first action at the inline start and the last action at the inline end.
    /// </summary>
    SpaceBetween
}
