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
    private int _itemCount;
    private int _itemsBefore;
    private float _itemSize;
    private int _lastRenderedItemCount;
    private int _lastRenderedPlaceholderCount;
    private IEnumerable<TItem>? _loadedItems;
    private int _loadedItemsStartIndex;
    private bool _loading;
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
            var refreshTask = RefreshDataCoreAsync(renderOnSuccess: true);

            if (!refreshTask.IsCompleted) {
                StateHasChanged();
            }
        }
    }

    /// <summary>
    ///     Asynchronously refreshes the underlying data and re-renders the component when the refresh completes.
    /// </summary>
    /// <returns>A task that represents the asynchronous refresh operation.</returns>
    public async Task RefreshDataAsync() => await RefreshDataCoreAsync(renderOnSuccess: true);

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore() {
        await base.DisposeAsyncCore();
        _refreshCts?.Cancel();
        _refreshCts?.Dispose();
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender) {
        await base.OnAfterRenderAsync(firstRender);
        if (IsolatedJsModule is not null) {
            if (firstRender) {
                await IsolatedJsModule.InvokeVoidAsync("init", DotNetObjectRef, Element, _afterPlaceholder, _itemSize, OverscanCount, MaxItemCount);
            }

            await IsolatedJsModule.InvokeVoidAsync("updateRenderState", DotNetObjectRef, _itemCount, _lastRenderedItemCount, _lastRenderedPlaceholderCount);
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

        if (ItemsProvider is null) {
            throw new InvalidOperationException($"{nameof(NTVirtualize<>)} requires the '{nameof(ItemsProvider)}' parameter to be specified and non-null.");
        }

        LoadingTemplate ??= DefaultPlaceholder;
    }

    private static string GetSpacerStyle(float spacerSize) {
        spacerSize = Math.Max(0, spacerSize);
        return $"height: {spacerSize.ToString(CultureInfo.InvariantCulture)}px; flex-shrink: 0;";
    }

    private RenderFragment DefaultPlaceholder(PlaceholderContext context) => (builder) => {
        builder.OpenComponent<TnTSkeleton>(0);
        builder.AddAttribute(10, "style", $"height: {_itemSize.ToString(CultureInfo.InvariantCulture)}px; flex-shrink: 0;");
        builder.CloseComponent();
    };

    private async ValueTask RefreshDataCoreAsync(bool renderOnSuccess) {
        _refreshCts?.Cancel();
        CancellationToken cancellationToken;

        _refreshCts = new CancellationTokenSource();
        cancellationToken = _refreshCts.Token;
        _loading = true;

        // Fetch items that we're going to render. _itemsBefore is already adjusted for overscan on the left, and _visibleItemCapacity already accounts for overscan on both sides. Load exactly what
        // we're rendering.
        var startIndex = _itemsBefore;
        var count = _visibleItemCapacity;

        var request = new NTVirtualizeItemsProviderRequest<TItem> {
            Count = count,
            StartIndex = startIndex,
            CancellationToken = cancellationToken
        };

        try {
            var result = await ItemsProvider!(request);

            // Only apply result if the task was not canceled.
            if (!cancellationToken.IsCancellationRequested) {
                _itemCount = result.TotalItemCount;
                _loadedItems = result.Items;
                _loadedItemsStartIndex = request.StartIndex;
                _loading = false;

                if (renderOnSuccess) {
                    await InvokeAsync(StateHasChanged);
                }
            }
        }
        catch (OperationCanceledException oce) when (oce.CancellationToken == cancellationToken) { } // No-op; we canceled the operation, so it's fine to suppress this exception
        catch (Exception e) {
            // Cache this exception so the renderer can throw it.
            _refreshException = e;

            // Re-render the component to throw the exception.
            await InvokeAsync(StateHasChanged);
        }
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
