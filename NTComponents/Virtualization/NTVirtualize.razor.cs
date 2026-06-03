using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.JSInterop;
using NTComponents.Core;
using NTComponents.Virtualization;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace NTComponents;

/// <summary>
///     A component that provides scrolling virtualization for a list of items.
/// </summary>
/// <typeparam name="TItem">The type of the items to be virtualized.</typeparam>
[method: DynamicDependency(nameof(LoadItems))]
public partial class NTVirtualize<TItem>() : TnTPageScriptComponent<NTVirtualize<TItem>> {
    private readonly Dictionary<int, TItem> _itemCache = [];
    private readonly List<int> _cacheIndexesToRemove = [];
    private readonly List<int> _trimCandidateIndexes = [];

    /// <inheritdoc />
    public override string? ElementClass => throw new NotSupportedException();

    /// <inheritdoc />
    public override string? ElementStyle => throw new NotSupportedException();

    /// <summary>
    ///     Gets or sets the content to show when the <see cref="TnTItemsProviderResult{TItem}.TotalItemCount" /> is zero.
    /// </summary>
    [Parameter]
    public RenderFragment? EmptyTemplate { get; set; }

    /// <summary>
    ///     Gets the size of each item in pixels. Defaults to 50px.
    /// </summary>
    [Parameter]
    public float ItemSize { get; set; } = 50f;

    /// <summary>
    ///     Gets or sets the function providing items to the list.
    /// </summary>
    [Parameter]
    public NTVirtualizeItemsProvider<TItem>? ItemsProvider { get; set; }

    /// <summary>
    ///     Gets or sets the item template for the list.
    /// </summary>
    [Parameter]
    public RenderFragment<TItem>? ItemTemplate { get; set; }

    /// <inheritdoc />
    public override string? JsModulePath => "./_content/NTComponents/Virtualization/NTVirtualize.razor.js";

    /// <summary>
    ///     Gets or sets the template for items that have not yet been loaded in memory.
    /// </summary>
    [Parameter]
    public RenderFragment<PlaceholderContext>? LoadingTemplate { get; set; }

    /// <summary>
    ///     Gets or sets optional content rendered inside each spacer element.
    /// </summary>
    [Parameter]
    public RenderFragment<PlaceholderContext>? SpacerTemplate { get; set; }

    /// <summary>
    ///     <para>Gets or sets the maximum number of items that will be rendered, even if the client reports that its viewport is large enough to show more. The default value is 100.</para>
    ///     <para>
    ///         This should only be used as a safeguard against excessive memory usage or large data loads. Do not set this to a smaller number than you expect to fit on a realistic-sized window,
    ///         because that will leave a blank gap below and the user may not be able to see the rest of the content.
    ///     </para>
    /// </summary>
    [Parameter]
    public int MaxItemCount { get; set; } = 100;

    /// <summary>
    ///     Gets or sets a value that determines how many additional items will be rendered before and after the visible region. This help to reduce the frequency of rendering during scrolling.
    ///     However, higher values mean that more elements will be present in the page.
    /// </summary>
    [Parameter]
    public int OverscanCount { get; set; } = 3;

    /// <summary>
    ///     Gets or sets how many additional visible windows of loading placeholders are rendered after the loaded item range.
    /// </summary>
    /// <remarks>
    ///     The default value of 1 renders up to one extra visible window of placeholders, so the user can scroll into already-rendered skeletons while the next item request is being triggered.
    /// </remarks>
    [Parameter]
    public int PlaceholderPreloadWindowCount { get; set; } = 1;

    /// <summary>
    ///     Gets or sets how many additional visible windows are fetched in the background after the visible item range.
    /// </summary>
    [Parameter]
    public int BackgroundPreloadWindowCount { get; set; }

    /// <summary>
    ///     Gets or sets whether cached visible ranges are re-fetched in the background and replaced when the provider returns newer items.
    /// </summary>
    [Parameter]
    public bool RevalidateCachedItems { get; set; }

    /// <summary>
    ///     Gets or sets the maximum number of items kept in the virtualizer cache.
    /// </summary>
    [Parameter]
    public int MaxCachedItemCount { get; set; } = 1_000;

    /// <summary>
    ///     <para>
    ///         Gets or sets the tag name of the HTML element that will be used as the virtualization spacer. One such element will be rendered before the visible items, and one more after them, using
    ///         an explicit "height" style to control the scroll range.
    ///     </para>
    ///     <para>
    ///         The default value is "div". If you are placing the <see cref="Virtualize{TItem}" /> instance inside an element that requires a specific child tag name, consider setting that here. For
    ///         example when rendering inside a "tbody", consider setting <see cref="SpacerElement" /> to the value "tr".
    ///     </para>
    /// </summary>
    [Parameter]
    public string SpacerElement { get; set; } = "div";

    private ElementReference _afterPlaceholder;
    private string _afterSpacerStyle = string.Empty;
    private float _afterSpacerStyleSize = -1f;
    private string _beforeSpacerStyle = string.Empty;
    private float _beforeSpacerStyleSize = -1f;
    private string _defaultPlaceholderStyle = string.Empty;
    private float _defaultPlaceholderStyleItemSize = -1f;
    private int _itemCount;
    private int _itemsBefore;
    private float _itemSize;
    private int _lastRenderedItemCount;
    private int _lastRenderedPlaceholderCount;
    private int _lastReportedItemCount = -1;
    private int _lastReportedRenderedItemCount = -1;
    private int _lastReportedRenderedPlaceholderCount = -1;
    private NTVirtualizeItemsProvider<TItem>? _lastItemsProvider;
    private bool _loading;
    private CancellationTokenSource? _prefetchCts;
    private CancellationTokenSource? _revalidateCts;
    private int _revalidatingCount;
    private int _revalidatingStartIndex = -1;
    private CancellationTokenSource? _refreshCts;
    private Exception? _refreshException;
    private float _spacerAfterSize;
    private float _spacerBeforeSize;
    private int _visibleItemCapacity;

    /// <summary>
    ///     Loads items based on client-side virtualization calculations.
    /// </summary>
    /// <param name="spacerBeforeSize">The size of the spacer before the visible items in pixels.</param>
    /// <param name="spacerAfterSize"> The size of the spacer after the visible items in pixels.</param>
    /// <param name="startIndex">      The start index of the items to request.</param>
    /// <param name="count">           The number of items to request.</param>
    [JSInvokable]
    public void LoadItems(float spacerBeforeSize, float spacerAfterSize, int startIndex, int count) {
        var resolvedStartIndex = Math.Max(0, startIndex);
        var resolvedCount = Math.Max(0, count);
        if (resolvedStartIndex + resolvedCount > _itemCount) {
            resolvedStartIndex = Math.Max(0, _itemCount - resolvedCount);
        }

        var resolvedSpacerBeforeSize = Math.Max(0, spacerBeforeSize);
        var resolvedSpacerAfterSize = Math.Max(0, spacerAfterSize);

        if (resolvedStartIndex != _itemsBefore
            || resolvedCount != _visibleItemCapacity
            || !resolvedSpacerBeforeSize.Equals(_spacerBeforeSize)
            || !resolvedSpacerAfterSize.Equals(_spacerAfterSize)) {
            _itemsBefore = resolvedStartIndex;
            _visibleItemCapacity = resolvedCount;
            _spacerBeforeSize = resolvedSpacerBeforeSize;
            _spacerAfterSize = resolvedSpacerAfterSize;
            _prefetchCts?.Cancel();
            _revalidateCts?.Cancel();
            var refreshTask = RefreshDataCoreAsync(renderOnSuccess: true);

            if (!refreshTask.IsCompleted) {
                _ = InvokeAsync(StateHasChanged);
            }
        }
    }

    /// <summary>
    ///     Asynchronously refreshes the underlying data and re-renders the component when the refresh completes.
    /// </summary>
    /// <returns>A task that represents the asynchronous refresh operation.</returns>
    public async Task RefreshDataAsync() {
        ClearCachedItems(cancelForegroundRefresh: true);
        await RefreshDataCoreAsync(renderOnSuccess: true);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing) {
        if (disposing) {
            CancelAllWork();
        }

        base.Dispose(disposing);
    }

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore() {
        CancelAllWork();
        await base.DisposeAsyncCore();
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender) {
        await base.OnAfterRenderAsync(firstRender);
        if (IsolatedJsModule is not null) {
            if (firstRender) {
                await IsolatedJsModule.InvokeVoidAsync("init", DotNetObjectRef, Element, _afterPlaceholder, _itemSize, OverscanCount, MaxItemCount);
            }

            if (_itemCount != _lastReportedItemCount
                || _lastRenderedItemCount != _lastReportedRenderedItemCount
                || _lastRenderedPlaceholderCount != _lastReportedRenderedPlaceholderCount) {
                _lastReportedItemCount = _itemCount;
                _lastReportedRenderedItemCount = _lastRenderedItemCount;
                _lastReportedRenderedPlaceholderCount = _lastRenderedPlaceholderCount;
                await IsolatedJsModule.InvokeVoidAsync("updateRenderState", DotNetObjectRef, _itemCount, _lastRenderedItemCount, _lastRenderedPlaceholderCount);
            }
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet() {
        if (ItemSize <= 0) {
            throw new InvalidOperationException(
                $"{nameof(NTVirtualize<>)} requires a positive value for parameter '{nameof(ItemSize)}'.");
        }

        if (_itemSize <= 0) {
            _itemSize = ItemSize;
        }
        else if (!ItemSize.Equals(_itemSize)) {
            _itemSize = ItemSize;
            ClearCachedItems(cancelForegroundRefresh: true);
            ResetReportedRenderState();
        }

        if (ItemsProvider is null) {
            throw new InvalidOperationException($"{nameof(NTVirtualize<>)} requires the '{nameof(ItemsProvider)}' parameter to be specified and non-null.");
        }

        if (_lastItemsProvider is not null && ItemsProvider != _lastItemsProvider) {
            SetItemCount(0);
            ClearCachedItems(cancelForegroundRefresh: true);
            ResetReportedRenderState();
        }

        _lastItemsProvider = ItemsProvider;
        LoadingTemplate ??= DefaultPlaceholder;
    }

    private static string CreateSpacerStyle(float spacerSize) => $"height: {Math.Max(0, spacerSize).ToString(CultureInfo.InvariantCulture)}px; flex-shrink: 0;";

    private static string GetCachedSpacerStyle(float spacerSize, ref float cachedSpacerSize, ref string cachedStyle) {
        spacerSize = Math.Max(0, spacerSize);
        if (!spacerSize.Equals(cachedSpacerSize)) {
            cachedSpacerSize = spacerSize;
            cachedStyle = CreateSpacerStyle(spacerSize);
        }

        return cachedStyle;
    }

    private string GetAfterSpacerStyle(int preloadedPlaceholderCount) => GetCachedSpacerStyle(GetAdjustedSpacerAfterSize(preloadedPlaceholderCount), ref _afterSpacerStyleSize, ref _afterSpacerStyle);

    private string GetBeforeSpacerStyle() => GetCachedSpacerStyle(_spacerBeforeSize, ref _beforeSpacerStyleSize, ref _beforeSpacerStyle);

    private float GetAdjustedSpacerAfterSize(int preloadedPlaceholderCount) =>
        Math.Max(0, _spacerAfterSize - Math.Max(0, preloadedPlaceholderCount) * _itemSize);

    private int GetPreloadedPlaceholderCount(int lastItemIndex) {
        if (_itemCount <= 0 || _visibleItemCapacity <= 0 || PlaceholderPreloadWindowCount <= 0) {
            return 0;
        }

        var remainingItemCount = Math.Max(0, _itemCount - Math.Max(0, lastItemIndex));
        var requestedPlaceholderCount = _visibleItemCapacity * PlaceholderPreloadWindowCount;
        return Math.Min(remainingItemCount, requestedPlaceholderCount);
    }

    private int GetBackgroundPreloadCount(int lastItemIndex) {
        if (_itemCount <= 0 || _visibleItemCapacity <= 0 || BackgroundPreloadWindowCount <= 0) {
            return 0;
        }

        var remainingItemCount = Math.Max(0, _itemCount - Math.Max(0, lastItemIndex));
        var requestedItemCount = _visibleItemCapacity * BackgroundPreloadWindowCount;
        return Math.Min(remainingItemCount, requestedItemCount);
    }

    private bool TryGetMissingRange(int startIndex, int count, out int missingStartIndex, out int missingCount) {
        missingStartIndex = -1;
        var missingEndIndex = -1;
        var lastIndex = Math.Max(0, startIndex) + Math.Max(0, count);

        for (var index = Math.Max(0, startIndex); index < lastIndex; index++) {
            if (_itemCache.ContainsKey(index)) {
                continue;
            }

            if (missingStartIndex < 0) {
                missingStartIndex = index;
            }

            missingEndIndex = index;
        }

        missingCount = missingStartIndex < 0 ? 0 : missingEndIndex - missingStartIndex + 1;
        return missingCount > 0;
    }

    private void StoreItems(int startIndex, IReadOnlyCollection<TItem> items) {
        var index = Math.Max(0, startIndex);
        foreach (var item in items) {
            _itemCache[index++] = item;
        }

        TrimItemCache();
    }

    private void SetItemCount(int totalItemCount) {
        _itemCount = Math.Max(0, totalItemCount);
        _cacheIndexesToRemove.Clear();
        foreach (var index in _itemCache.Keys) {
            if (index >= _itemCount) {
                _cacheIndexesToRemove.Add(index);
            }
        }

        foreach (var index in _cacheIndexesToRemove) {
            _itemCache.Remove(index);
        }
    }

    private void TrimItemCache() {
        var minimumCacheSize = Math.Max(1, _visibleItemCapacity * Math.Max(1, PlaceholderPreloadWindowCount + BackgroundPreloadWindowCount + 1));
        var maxCachedItemCount = Math.Max(minimumCacheSize, MaxCachedItemCount);
        if (_itemCache.Count <= maxCachedItemCount) {
            return;
        }

        var activeStartIndex = Math.Max(0, _itemsBefore);
        var activeEndIndex = activeStartIndex + Math.Max(0, _visibleItemCapacity) + GetPreloadedPlaceholderCount(activeStartIndex + Math.Max(0, _visibleItemCapacity));
        var activeCenterIndex = activeStartIndex + Math.Max(0, activeEndIndex - activeStartIndex) / 2;
        _trimCandidateIndexes.Clear();
        foreach (var index in _itemCache.Keys) {
            if (index < activeStartIndex || index >= activeEndIndex) {
                _trimCandidateIndexes.Add(index);
            }
        }

        _trimCandidateIndexes.Sort((left, right) => Math.Abs(right - activeCenterIndex).CompareTo(Math.Abs(left - activeCenterIndex)));
        foreach (var index in _trimCandidateIndexes) {
            if (_itemCache.Count <= maxCachedItemCount) {
                return;
            }

            _itemCache.Remove(index);
        }
    }

    private void ClearCachedItems(bool cancelForegroundRefresh) {
        _itemCache.Clear();
        CancelAndDispose(ref _prefetchCts);
        CancelAndDispose(ref _revalidateCts);
        _revalidatingStartIndex = -1;
        _revalidatingCount = 0;

        if (cancelForegroundRefresh) {
            CancelAndDispose(ref _refreshCts);
        }
    }

    private RenderFragment DefaultPlaceholder(PlaceholderContext context) => (builder) => {
        builder.OpenComponent<TnTSkeleton>(0);
        builder.AddAttribute(10, "style", GetDefaultPlaceholderStyle());
        builder.CloseComponent();
    };

    private string GetDefaultPlaceholderStyle() {
        if (!_itemSize.Equals(_defaultPlaceholderStyleItemSize)) {
            _defaultPlaceholderStyleItemSize = _itemSize;
            _defaultPlaceholderStyle = CreateSpacerStyle(_itemSize);
        }

        return _defaultPlaceholderStyle;
    }

    private async ValueTask RefreshDataCoreAsync(bool renderOnSuccess) {
        CancelAndDispose(ref _refreshCts);

        // Fetch only the missing part of the active range. Cached rows keep rendering while the missing rows remain placeholders.
        var startIndex = _itemsBefore;
        var count = _visibleItemCapacity;
        if (!TryGetMissingRange(startIndex, count, out var missingStartIndex, out var missingCount)) {
            _loading = false;
            StartBackgroundPreload();
            StartBackgroundRevalidation(startIndex, count);
            if (renderOnSuccess) {
                await InvokeAsync(StateHasChanged);
            }

            return;
        }

        _refreshCts = new CancellationTokenSource();
        var cancellationToken = _refreshCts.Token;
        _loading = true;

        var request = new NTVirtualizeItemsProviderRequest<TItem> {
            Count = missingCount,
            StartIndex = missingStartIndex,
            CancellationToken = cancellationToken
        };

        try {
            var result = await ItemsProvider!(request);

            // Only apply result if the task was not canceled.
            if (!cancellationToken.IsCancellationRequested) {
                SetItemCount(result.TotalItemCount);
                UpdateSpacerAfterSizeFromItemCount();
                StoreItems(request.StartIndex, result.Items);
                _loading = false;

                if (renderOnSuccess) {
                    await InvokeAsync(StateHasChanged);
                }

                StartBackgroundPreload();
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { } // No-op; we canceled the operation, so it's fine to suppress this exception
        catch (Exception e) {
            // Cache this exception so the renderer can throw it.
            _refreshException = e;

            // Re-render the component to throw the exception.
            await InvokeAsync(StateHasChanged);
        }
    }

    private void StartBackgroundPreload() {
        if (_loading || ItemsProvider is null || _itemCount <= 0 || _visibleItemCapacity <= 0) {
            return;
        }

        var activeLastItemIndex = Math.Min(_itemsBefore + _visibleItemCapacity, _itemCount);
        var preloadCount = GetBackgroundPreloadCount(activeLastItemIndex);
        if (!TryGetMissingRange(activeLastItemIndex, preloadCount, out var missingStartIndex, out var missingCount)) {
            return;
        }

        CancelAndDispose(ref _prefetchCts);
        _prefetchCts = new CancellationTokenSource();
        _ = InvokeAsync(() => PreloadItemsAsync(missingStartIndex, missingCount, _prefetchCts.Token));
    }

    private async Task PreloadItemsAsync(int startIndex, int count, CancellationToken cancellationToken) {
        try {
            var result = await ItemsProvider!(new NTVirtualizeItemsProviderRequest<TItem> {
                Count = count,
                StartIndex = startIndex,
                CancellationToken = cancellationToken
            });

            if (!cancellationToken.IsCancellationRequested) {
                SetItemCount(result.TotalItemCount);
                UpdateSpacerAfterSizeFromItemCount();
                StoreItems(startIndex, result.Items);
                if (RangeIntersectsCurrentRender(startIndex, count)) {
                    await InvokeAsync(StateHasChanged);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch {
            // Background preloads are opportunistic. The foreground load path will surface provider failures when the range is actually requested.
        }
    }

    private void StartBackgroundRevalidation(int startIndex, int count) {
        if (!RevalidateCachedItems || _loading || ItemsProvider is null || _itemCount <= 0 || count <= 0) {
            return;
        }

        var resolvedStartIndex = Math.Max(0, startIndex);
        var resolvedCount = Math.Min(Math.Max(0, count), Math.Max(0, _itemCount - resolvedStartIndex));
        if (resolvedCount <= 0) {
            return;
        }

        if (_revalidatingStartIndex == resolvedStartIndex && _revalidatingCount == resolvedCount) {
            return;
        }

        CancelAndDispose(ref _revalidateCts);
        _revalidateCts = new CancellationTokenSource();
        _revalidatingStartIndex = resolvedStartIndex;
        _revalidatingCount = resolvedCount;
        _ = InvokeAsync(() => RevalidateItemsAsync(resolvedStartIndex, resolvedCount, _revalidateCts.Token));
    }

    private async Task RevalidateItemsAsync(int startIndex, int count, CancellationToken cancellationToken) {
        try {
            var result = await ItemsProvider!(new NTVirtualizeItemsProviderRequest<TItem> {
                Count = count,
                StartIndex = startIndex,
                CancellationToken = cancellationToken
            });

            if (!cancellationToken.IsCancellationRequested) {
                SetItemCount(result.TotalItemCount);
                UpdateSpacerAfterSizeFromItemCount();
                StoreItems(startIndex, result.Items);
                await InvokeAsync(StateHasChanged);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch {
            // Revalidation is opportunistic. Cached rows stay visible and the foreground load path remains responsible for surfacing provider failures.
        }
        finally {
            if (_revalidatingStartIndex == startIndex && _revalidatingCount == count) {
                _revalidatingStartIndex = -1;
                _revalidatingCount = 0;
            }
        }
    }

    private void UpdateSpacerAfterSizeFromItemCount() {
        if (_itemCount <= 0 || _visibleItemCapacity <= 0) {
            _spacerAfterSize = 0;
            return;
        }

        var itemsAfter = Math.Max(0, _itemCount - _visibleItemCapacity - _itemsBefore);
        _spacerAfterSize = itemsAfter * _itemSize;
    }

    private bool RangeIntersectsCurrentRender(int startIndex, int count) {
        if (count <= 0) {
            return false;
        }

        var visibleLastItemIndex = Math.Min(_itemsBefore + _visibleItemCapacity, _itemCount);
        var renderLastItemIndex = visibleLastItemIndex + GetPreloadedPlaceholderCount(visibleLastItemIndex);
        return startIndex < renderLastItemIndex && startIndex + count > _itemsBefore;
    }

    private void ResetReportedRenderState() {
        _lastReportedItemCount = -1;
        _lastReportedRenderedItemCount = -1;
        _lastReportedRenderedPlaceholderCount = -1;
    }

    private void CancelAllWork() {
        CancelAndDispose(ref _prefetchCts);
        CancelAndDispose(ref _revalidateCts);
        CancelAndDispose(ref _refreshCts);
        _revalidatingStartIndex = -1;
        _revalidatingCount = 0;
    }

    private static void CancelAndDispose(ref CancellationTokenSource? cancellationTokenSource) {
        cancellationTokenSource?.Cancel();
        cancellationTokenSource?.Dispose();
        cancellationTokenSource = null;
    }
}

/// <summary>
///     Represents a request for items in a virtualized list.
/// </summary>
/// <typeparam name="TItem">The type of the items being requested.</typeparam>
public struct NTVirtualizeItemsProviderRequest<TItem>() {

    /// <summary>
    ///     Gets or sets the cancellation token for the request.
    /// </summary>
    public CancellationToken CancellationToken { get; init; }

    /// <summary>
    ///     Gets or sets the maximum number of items to retrieve.
    /// </summary>
    public int? Count { get; set; }

    /// <summary>
    ///     Gets or sets the properties to sort on and their sort directions.
    /// </summary>
    public IReadOnlyCollection<KeyValuePair<string, SortDirection>> SortOnProperties { get; init; } = [];

    /// <summary>
    ///     Gets or sets the start index of the requested items.
    /// </summary>
    public int StartIndex { get; init; }

    /// <summary>
    ///     Implicitly converts a <see cref="TnTItemsProviderRequest" /> to a <see cref="NTVirtualizeItemsProviderRequest{TItem}" />.
    /// </summary>
    /// <param name="request">The items provider request to convert.</param>
    /// <returns>A new <see cref="NTVirtualizeItemsProviderRequest{TItem}" /> with properties copied from the source request.</returns>
    /// <remarks>
    ///     This conversion allows a general-purpose items provider request to be used in virtualization contexts. Note that the <see cref="CancellationToken" /> is set to the default value since it's
    ///     not available in the source request.
    /// </remarks>
    public static implicit operator NTVirtualizeItemsProviderRequest<TItem>(TnTItemsProviderRequest request) {
        return new NTVirtualizeItemsProviderRequest<TItem> {
            Count = request.Count,
            SortOnProperties = [.. request.SortOnProperties],
            StartIndex = request.StartIndex,
            CancellationToken = default
        };
    }

    /// <summary>
    ///     Implicitly converts a <see cref="NTVirtualizeItemsProviderRequest{TItem}" /> to a <see cref="TnTItemsProviderRequest" />.
    /// </summary>
    /// <param name="request">The virtualize items provider request to convert.</param>
    /// <returns>A new <see cref="TnTItemsProviderRequest" /> with properties copied from the source request.</returns>
    /// <remarks>This conversion enables interoperability between the virtualization-specific request type and the general-purpose items provider request type.</remarks>
    public static implicit operator TnTItemsProviderRequest(NTVirtualizeItemsProviderRequest<TItem> request) {
        return new TnTItemsProviderRequest {
            StartIndex = request.StartIndex,
            SortOnProperties = request.SortOnProperties,
            Count = request.Count
        };
    }
}

/// <summary>
///     Represents a method that asynchronously provides a virtualized collection of items based on the specified request parameters.
/// </summary>
/// <remarks>
///     Use this delegate to efficiently load large datasets on demand, such as in scenarios involving UI virtualization or incremental data loading. The provider should return only the items
///     specified by the request to optimize performance and resource usage.
/// </remarks>
/// <typeparam name="TItem">The type of items to be retrieved and provided by the items provider.</typeparam>
/// <param name="request">An object containing parameters that specify how items should be retrieved, such as the range or filtering criteria.</param>
/// <returns>A task that, when completed, provides a result containing the requested items and any associated metadata.</returns>
public delegate ValueTask<TnTItemsProviderResult<TItem>> NTVirtualizeItemsProvider<TItem>(NTVirtualizeItemsProviderRequest<TItem> request);
