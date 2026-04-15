using Microsoft.AspNetCore.Components;
using System.Diagnostics.CodeAnalysis;
using NTComponents.Core;

namespace NTComponents.Popover;

internal interface INTPopoverHighlightState {
    int HighlightRequestId { get; }
}

/// <summary>
///     Provides the default scoped implementation of <see cref="INTPopoverService" />.
/// </summary>
internal sealed class NTPopoverService : INTPopoverService {
    private const int BaseZIndex = 1200;
    private const int DefaultOffset = 24;
    private const int DefaultStartLeft = 24;
    private const int DefaultStartTop = 88;
    private const int OffsetCycle = 6;

    private readonly List<PopoverHandle> _popovers = [];
    private int _nextDefaultOffsetIndex;
    private int _nextZIndex;

    /// <inheritdoc />
    public event INTPopoverService.PopoversChangedCallback? OnChanged;

    /// <inheritdoc />
    public async Task BringToFrontAsync(INTPopoverHandle popover) {
        if (popover is not PopoverHandle handle || !_popovers.Contains(handle)) {
            return;
        }

        var highestZIndex = _popovers.MaxBy(existingPopover => existingPopover.ZIndex)?.ZIndex ?? BaseZIndex;
        if (handle.ZIndex >= highestZIndex) {
            return;
        }

        var nextZIndex = BaseZIndex + ++_nextZIndex;
        handle.ZIndex = nextZIndex;
        await NotifyChangedAsync();
    }

    /// <inheritdoc />
    public async Task CloseAsync(INTPopoverHandle popover) {
        if (popover is not PopoverHandle handle) {
            return;
        }

        if (_popovers.Remove(handle)) {
            await NotifyChangedAsync();
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<INTPopoverHandle> GetPopovers() {
        return _popovers
            .Cast<INTPopoverHandle>()
            .ToArray();
    }

    /// <inheritdoc />
    public async Task HideAsync(INTPopoverHandle popover) {
        if (popover is not PopoverHandle handle || !_popovers.Contains(handle) || !handle.IsVisible) {
            return;
        }

        handle.IsVisible = false;
        await NotifyChangedAsync();
    }

    /// <inheritdoc />
    public Task<INTPopoverHandle> OpenAsync(RenderFragment renderFragment, NTPopoverOptions? options = null) {
        ArgumentNullException.ThrowIfNull(renderFragment, nameof(renderFragment));

        var resolvedOptions = options ?? new();
        var existingHandleTask = TryReuseExistingAsync(resolvedOptions);
        if (existingHandleTask is not null) {
            return existingHandleTask;
        }

        var handle = CreateHandle(resolvedOptions) with {
            ChildContent = renderFragment
        };

        _popovers.Add(handle);
        return NotifyOpenedAsync(handle);
    }

    /// <inheritdoc />
    public Task<INTPopoverHandle> OpenAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TComponent>(NTPopoverOptions? options = null, IReadOnlyDictionary<string, object?>? parameters = null) where TComponent : IComponent {
        var resolvedOptions = options ?? new();
        var existingHandleTask = TryReuseExistingAsync(resolvedOptions);
        if (existingHandleTask is not null) {
            return existingHandleTask;
        }

        var handle = CreateHandle(resolvedOptions) with {
            Parameters = parameters,
            Type = typeof(TComponent)
        };

        _popovers.Add(handle);
        return NotifyOpenedAsync(handle);
    }

    /// <inheritdoc />
    public async Task ShowAsync(INTPopoverHandle popover) {
        if (popover is not PopoverHandle handle || !_popovers.Contains(handle)) {
            return;
        }

        handle.IsVisible = true;
        handle.ZIndex = BaseZIndex + ++_nextZIndex;
        await NotifyChangedAsync();
    }

    /// <inheritdoc />
    public async Task UpdatePositionAsync(INTPopoverHandle popover, double left, double top) {
        if (popover is not PopoverHandle handle || !_popovers.Contains(handle)) {
            return;
        }

        var normalizedLeft = Math.Round(left, 2);
        var normalizedTop = Math.Round(top, 2);
        if (Math.Abs(handle.Left - normalizedLeft) < 0.01 && Math.Abs(handle.Top - normalizedTop) < 0.01) {
            return;
        }

        handle.Left = normalizedLeft;
        handle.Top = normalizedTop;
        await NotifyChangedAsync();
    }

    private Task<INTPopoverHandle>? TryReuseExistingAsync(NTPopoverOptions options) {
        if (string.IsNullOrWhiteSpace(options.InstanceKey)) {
            return null;
        }

        var existingHandle = _popovers.FirstOrDefault(popover =>
            string.Equals(popover.Options.InstanceKey, options.InstanceKey, StringComparison.Ordinal));

        return existingHandle is null
            ? null
            : ReuseExistingAsync(existingHandle);
    }

    private PopoverHandle CreateHandle(NTPopoverOptions options) {
        var offsetIndex = _nextDefaultOffsetIndex++ % OffsetCycle;

        return new PopoverHandle(this) {
            IsVisible = true,
            Left = options.InitialLeft ?? DefaultStartLeft + (offsetIndex * DefaultOffset),
            Options = options,
            Top = options.InitialTop ?? DefaultStartTop + (offsetIndex * DefaultOffset),
            ZIndex = BaseZIndex + ++_nextZIndex
        };
    }

    private async Task NotifyChangedAsync() => await (OnChanged?.Invoke() ?? Task.CompletedTask);

    private async Task<INTPopoverHandle> NotifyOpenedAsync(PopoverHandle handle) {
        await NotifyChangedAsync();
        return handle;
    }

    private async Task<INTPopoverHandle> ReuseExistingAsync(PopoverHandle handle) {
        if (!handle.IsVisible) {
            await ShowAsync(handle);
            return handle;
        }

        handle.HighlightRequestId++;
        var currentZIndex = handle.ZIndex;
        await BringToFrontAsync(handle);
        if (handle.ZIndex == currentZIndex) {
            await NotifyChangedAsync();
        }

        return handle;
    }

    private sealed record PopoverHandle(NTPopoverService Service) : INTPopoverHandle, INTPopoverHighlightState {

        /// <inheritdoc />
        public RenderFragment? ChildContent { get; init; }

        /// <inheritdoc />
        public string ElementId { get; init; } = TnTComponentIdentifier.NewId();

        /// <inheritdoc />
        public bool IsVisible { get; set; }

        /// <inheritdoc />
        public int HighlightRequestId { get; set; }

        /// <inheritdoc />
        public double Left { get; set; }

        /// <inheritdoc />
        public NTPopoverOptions Options { get; init; } = new();

        /// <inheritdoc />
        public IReadOnlyDictionary<string, object?>? Parameters { get; init; }

        /// <inheritdoc />
        public double Top { get; set; }

        /// <inheritdoc />
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        public Type? Type { get; init; }

        /// <inheritdoc />
        public int ZIndex { get; set; }

        /// <inheritdoc />
        public Task BringToFrontAsync() => Service.BringToFrontAsync(this);

        /// <inheritdoc />
        public Task CloseAsync() => Service.CloseAsync(this);

        /// <inheritdoc />
        public Task HideAsync() => Service.HideAsync(this);

        /// <inheritdoc />
        public Task ShowAsync() => Service.ShowAsync(this);
    }
}
