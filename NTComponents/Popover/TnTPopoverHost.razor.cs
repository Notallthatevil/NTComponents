using NTComponents.Core;

namespace NTComponents;

/// <summary>
///     Renders active popover windows and hidden-window launchers managed by <see cref="ITnTPopoverService" />.
/// </summary>
public partial class TnTPopoverHost(ITnTPopoverService popoverService) {
    private readonly ITnTPopoverService _popoverService = popoverService;
    private IReadOnlyList<ITnTPopoverHandle> _hiddenPopovers = [];
    private readonly HashSet<string> _restoringPopoverIds = [];
    private IReadOnlyList<ITnTPopoverHandle> _visiblePopovers = [];

    /// <summary>
    ///     Disposes the host and unsubscribes from popover service events.
    /// </summary>
    public void Dispose() {
        _popoverService.OnChanged -= OnChangedAsync;
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    protected override void OnInitialized() {
        base.OnInitialized();
        RefreshPopovers();
        _popoverService.OnChanged += OnChangedAsync;
    }

    private static string GetTitle(ITnTPopoverHandle popover) {
        return string.IsNullOrWhiteSpace(popover.Options.Title) ? "Window" : popover.Options.Title!;
    }

    private static string GetLauncherClass(bool isRestoring) {
        return CssClassBuilder.Create("tnt-popover-host__launcher")
            .AddClass("tnt-popover-host__launcher--restoring", isRestoring)
            .Build();
    }

    private async Task OnChangedAsync() {
        RefreshPopovers();
        await InvokeAsync(StateHasChanged);
    }

    private async Task OnLauncherEnterAnimationCompletedAsync(string popoverElementId) {
        if (!_restoringPopoverIds.Remove(popoverElementId)) {
            return;
        }

        RefreshPopovers();
        await InvokeAsync(StateHasChanged);
    }

    private void RefreshPopovers() {
        var popovers = _popoverService.GetPopovers();
        _visiblePopovers = popovers
            .Where(popover => popover.IsVisible)
            .ToArray();

        _hiddenPopovers = popovers
            .Where(popover => !popover.IsVisible || _restoringPopoverIds.Contains(popover.ElementId))
            .OrderBy(popover => GetTitle(popover), StringComparer.Ordinal)
            .ToArray();
    }

    private async Task RestoreAsync(ITnTPopoverHandle popover) {
        if (!_restoringPopoverIds.Add(popover.ElementId)) {
            return;
        }

        RefreshPopovers();
        await InvokeAsync(StateHasChanged);
        await popover.ShowAsync();
    }
}
