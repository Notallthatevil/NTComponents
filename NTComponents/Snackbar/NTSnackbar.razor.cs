using Microsoft.AspNetCore.Components;
using NTComponents.Core;
using NTComponents.Snackbar;
using static NTComponents.Snackbar.NTSnackbarService;

namespace NTComponents;

/// <summary>
///     Renders snackbars managed by <see cref="INTSnackbarService" />.
/// </summary>
public partial class NTSnackbar(INTSnackbarService service) {
    private const int _closeDelay = 200;

    private const string SsrCloseOnClick =
        "const snackbar = event.currentTarget.closest('.nt-snackbar'); if (snackbar) { snackbar.classList.add('nt-closing'); setTimeout(() => snackbar.remove(), 200); }";

    private readonly INTSnackbarService _service = service;

    /// <summary>
    ///     Controls where the snackbar container is placed in the viewport.
    /// </summary>
    [Parameter]
    public NTSnackbarPosition Position { get; set; } = NTSnackbarPosition.BottomCenter;

    private readonly Dictionary<INTSnackbar, SnackbarMetadata> _snackbarMetadata = [];
    private readonly List<INTSnackbar> _displaySnackbars = [];
    private readonly CancellationTokenSource _tokenSource = new();

    /// <summary>
    ///     Disposes the component and unsubscribes from snackbar service events.
    /// </summary>
    public void Dispose() {
        _tokenSource.Cancel();
        _tokenSource.Dispose();
        _service.OnClose -= OnCloseAsync;
        _service.OnOpen -= OnOpenAsync;
        if (_service is NTSnackbarService snackbarService) {
            snackbarService.QueueChanged -= OnQueueChangedAsync;
        }
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    protected override void OnInitialized() {
        base.OnInitialized();

        if (_service is NTSnackbarService snackbarService) {
            SyncDisplaySnackbars();
            snackbarService.QueueChanged += OnQueueChangedAsync;
        }

        _service.OnOpen += OnOpenAsync;
        _service.OnClose += OnCloseAsync;
    }

    private static string? GetSsrAutoDismissScript(INTSnackbar snackbar, SnackbarMetadata metadata) {
        if (snackbar.Timeout <= 0 || string.IsNullOrWhiteSpace(metadata.Id)) {
            return null;
        }

        var timeoutMilliseconds = (int)TimeSpan.FromSeconds(snackbar.Timeout).TotalMilliseconds;

        return $$"""
                 setTimeout(() => {
                     const snackbar = document.querySelector('#{{metadata.Id}}');
                     if (!snackbar) {
                         return;
                     }

                     snackbar.classList.add('nt-closing');
                     setTimeout(() => snackbar.remove(), {{_closeDelay}});
                 }, {{timeoutMilliseconds}});
                 """;
    }

    private async Task OnActionAsync(INTSnackbar snackbar) {
        await _service.InvokeActionAsync(snackbar);
    }

    private async Task OnCloseAsync(INTSnackbar snackbar) {
        if (!_displaySnackbars.Contains(snackbar)) {
            return;
        }

        if (snackbar is NTSnackbarImplementation implementation) {
            implementation.Closing = true;
        }

        SyncDisplaySnackbars(preserveClosingSnackbar: snackbar);
        await InvokeAsync(StateHasChanged);
        try {
            await Task.Delay(_closeDelay, _tokenSource.Token);
        }
        catch (OperationCanceledException) {
            return;
        }

        if (snackbar is NTSnackbarImplementation closedImplementation) {
            closedImplementation.Closing = false;
        }

        SyncDisplaySnackbars();
        await InvokeAsync(StateHasChanged);
    }

    private async Task OnOpenAsync(INTSnackbar snackbar) {
        SyncDisplaySnackbars(preserveClosingSnackbar: GetClosingSnackbar());
        await InvokeAsync(StateHasChanged);
    }

    private async Task OnQueueChangedAsync() {
        SyncDisplaySnackbars(preserveClosingSnackbar: GetClosingSnackbar());
        await InvokeAsync(StateHasChanged);
    }

    private static string GetSnackbarAttributesClass(INTSnackbar snackbar) {
        return CssClassBuilder.Create()
            .AddClass("nt-snackbar")
            .AddClass("nt-closing", snackbar.Closing)
            .Build() ?? string.Empty;
    }

    private static string GetSnackbarAttributesStyle(INTSnackbar snackbar) {
        return CssStyleBuilder.Create()
            .AddVariable("nt-snackbar-background-color", snackbar.BackgroundColor)
            .AddVariable("nt-snackbar-text-color", snackbar.TextColor)
            .AddVariable("nt-snackbar-action-color", snackbar.ActionColor)
            .Build() ?? string.Empty;
    }

    private static string GetStackItemClass(int depth) {
        return CssClassBuilder.Create()
            .AddClass("nt-snackbar-stack-item")
            .AddClass($"nt-snackbar-stack-depth-{depth}")
            .AddClass("nt-snackbar-front", depth == 0)
            .AddClass("nt-snackbar-queued", depth > 0)
            .Build() ?? string.Empty;
    }

    private static string GetStackItemStyle(int depth) {
        return CssStyleBuilder.Create()
            .AddVariable("nt-snackbar-stack-depth", depth.ToString(System.Globalization.CultureInfo.InvariantCulture))
            .Build() ?? string.Empty;
    }

    private string ContainerClass => CssClassBuilder.Create()
        .AddClass("nt-snackbar-container")
        .AddClass(Position switch {
            NTSnackbarPosition.TopLeftCorner => "nt-snackbar-top-left-corner",
            NTSnackbarPosition.CenterTop => "nt-snackbar-center-top",
            NTSnackbarPosition.TopRightCorner => "nt-snackbar-top-right-corner",
            NTSnackbarPosition.BottomLeftCorner => "nt-snackbar-bottom-left-corner",
            NTSnackbarPosition.BottomRightCorner => "nt-snackbar-bottom-right-corner",
            _ => "nt-snackbar-bottom-center"
        })
        .Build() ?? string.Empty;

    private INTSnackbar? GetClosingSnackbar() {
        return _displaySnackbars.FirstOrDefault(snackbar => snackbar is NTSnackbarImplementation implementation && implementation.Closing);
    }

    private SnackbarMetadata GetMetadata(INTSnackbar snackbar) {
        if (_snackbarMetadata.TryGetValue(snackbar, out var metadata)) {
            return metadata;
        }

        metadata = new SnackbarMetadata() { AutoDismissTask = null, Id = TnTComponentIdentifier.NewId() };
        _snackbarMetadata[snackbar] = metadata;
        return metadata;
    }

    private void ScheduleAutoDismissIfNeeded(INTSnackbar snackbar) {
        if (!RendererInfo.IsInteractive || snackbar.Timeout <= 0) {
            return;
        }

        var metadata = GetMetadata(snackbar);
        if (metadata.AutoDismissTask is not null) {
            return;
        }

        var trackedSnackbar = snackbar;
        metadata.AutoDismissTask = Task.Run(async () => {
            try {
                await Task.Delay((int)TimeSpan.FromSeconds(trackedSnackbar.Timeout).TotalMilliseconds, _tokenSource.Token);
                await InvokeAsync(() => _service.CloseAsync(trackedSnackbar));
            }
            catch (OperationCanceledException) {
            }
        }, _tokenSource.Token);

        _snackbarMetadata[snackbar] = metadata;
    }

    private void SyncDisplaySnackbars(INTSnackbar? preserveClosingSnackbar = null) {
        var nextDisplaySnackbars = new List<INTSnackbar>(3);
        if (preserveClosingSnackbar is not null) {
            nextDisplaySnackbars.Add(preserveClosingSnackbar);
        }

        if (_service is NTSnackbarService snackbarService) {
            foreach (var snackbar in snackbarService.GetCurrentStack(preserveClosingSnackbar is null ? 3 : 2)) {
                if (ReferenceEquals(snackbar, preserveClosingSnackbar)) {
                    continue;
                }

                nextDisplaySnackbars.Add(snackbar);
                if (nextDisplaySnackbars.Count == 3) {
                    break;
                }
            }
        }

        var snackbarsToRemove = _snackbarMetadata.Keys.Where(snackbar => !nextDisplaySnackbars.Contains(snackbar)).ToArray();
        foreach (var snackbar in snackbarsToRemove) {
            _snackbarMetadata.Remove(snackbar);
        }

        foreach (var snackbar in nextDisplaySnackbars) {
            GetMetadata(snackbar);
        }

        _displaySnackbars.Clear();
        _displaySnackbars.AddRange(nextDisplaySnackbars);

        if (_displaySnackbars.Count > 0) {
            ScheduleAutoDismissIfNeeded(_displaySnackbars[0]);
        }
    }

    private struct SnackbarMetadata {
        public required Task? AutoDismissTask { get; set; }
        public required string Id { get; set; }
    }
}
