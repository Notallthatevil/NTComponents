type Maybe<T> = T | null | undefined;

interface DotNetVirtualizeRef {
    _id?: number | string;
    invokeMethodAsync(methodName: 'LoadItems', spacerBeforeSize: number, spacerAfterSize: number, startIndex: number, count: number): Promise<unknown> | void;
}

interface VirtualizeDistribution {
    itemsBefore: number;
    unusedItemCapacity: number;
    visibleItemCapacity: number;
}

interface ScrollMetrics {
    containerSize: number;
    scrollTop: number;
}

interface VirtualizeState {
    measureItemSize: boolean;
    itemSizeVersion: number;
    measuredSizes: Map<number, number>;
    itemCount: number;
    itemSize: number;
    itemsBefore: number;
    maxItemCount: number;
    overscanCount: number;
    unusedItemCapacity: number;
    visibleItemCapacity: number;
}

interface AnchorSnapshot {
    index: number;
    offset: number;
    atStart: boolean;
    atEnd: boolean;
    version: number;
}

interface VirtualizeObservers {
    applySizes: () => void;
    invalidateMeasurements: () => void;
    updateOptions: (itemSize: number, overscanCount: number, maxItemCount: number, restorationKey?: string | null) => void;
    scrollToItem: (index: number, operationId: number) => Promise<void>;
    cancelScroll: (operationId?: number) => void;
    captureAnchor: () => AnchorSnapshot;
    restoreAnchor: (snapshot: AnchorSnapshot, index: number, mode: number) => void;
    afterRender: (placeholderCount: number, loading: boolean) => void;
    removeInputListeners: () => void;
    getScrollTop: () => number;
    measureItems: (reset?: boolean, preserveScrollOffset?: boolean, scrollTopBeforeSizing?: number) => boolean;
    resizeObserver: ResizeObserver | null;
    cancelPendingScrollUpdate: () => void;
    cancelScrollPolling: () => void;
    intersectionObserver: IntersectionObserver;
    mutationObserverAfter: MutationObserver;
    mutationObserverBefore: MutationObserver;
    overflowAnchorElement: HTMLElement;
    previousOverflowAnchor: string;
    removeScrollListener: () => void;
    removeScrollPersistenceListeners: () => void;
    requestVisibleItemDistribution: (forceUpdate: boolean) => void;
    restoreScrollPosition: () => boolean;
    state: VirtualizeState;
}

type DotNetVirtualizeRefKey = DotNetVirtualizeRef | number | string;

const observersByDotNetRef = new Map<DotNetVirtualizeRefKey, VirtualizeObservers>();
const scrollPositionsStateKey = '__ntVirtualizeScrollPositions';
const scrollPersistenceInterval = 100;

/**
 * Initializes the virtualization component by setting up intersection observers and mutation observers
 * for the top and bottom spacers to handle dynamic loading of items in a virtualized list.
 */
export function init(dotNetRef: Maybe<DotNetVirtualizeRef>, topSpacer: HTMLElement, bottomSpacer: HTMLElement, itemSize: number, overscanCount: number, maxItemCount: number, scrollRestorationKey?: string | null, rootMargin = 50, initialItemIndex = 0): void {
    if (!dotNetRef) {
        return;
    }

    const activeDotNetRef = dotNetRef;
    const dotNetRefKey = getDotNetRefKey(activeDotNetRef);
    const existingState = observersByDotNetRef.get(dotNetRefKey)?.state;
    dispose(activeDotNetRef);
    if (!topSpacer.isConnected || !bottomSpacer.isConnected) {
        return;
    }

    let scrollContainer = findClosestScrollContainer(topSpacer);
    const overflowAnchorElement = scrollContainer ?? document.documentElement;
    const previousOverflowAnchor = overflowAnchorElement.style.overflowAnchor;
    overflowAnchorElement.style.overflowAnchor = 'none';

    if (isValidTableElement(bottomSpacer.parentElement)) {
        topSpacer.style.display = 'table-row';
        bottomSpacer.style.display = 'table-row';
    }

    const observerRootMargin = `${Math.max(0, rootMargin)}px`;

    const intersectionObserver = new IntersectionObserver(intersectionCallback, {
        root: scrollContainer,
        rootMargin: observerRootMargin,
    });

    intersectionObserver.observe(topSpacer);
    intersectionObserver.observe(bottomSpacer);

    const mutationObserverBefore = createSpacerMutationObserver(topSpacer);
    const mutationObserverAfter = createSpacerMutationObserver(bottomSpacer);
    const scrollUpdateTargets = getScrollUpdateTargets(topSpacer, scrollContainer);
    let resolvedScrollRestorationKey = scrollRestorationKey?.trim() || null;
    let pendingScrollTop = getPersistedScrollPosition(resolvedScrollRestorationKey);
    let scheduledScrollUpdate: number | null = null;
    let lastScrollPersistenceTime = Number.NEGATIVE_INFINITY;
    const scheduleFrame = window.requestAnimationFrame ?? ((callback: FrameRequestCallback) => window.setTimeout(() => callback(performance.now()), 0));
    const cancelFrame = window.cancelAnimationFrame ?? window.clearTimeout;
    const scrollListenerOptions: AddEventListenerOptions = { passive: true, capture: true };
    let lastObservedScrollTop = -1;
    let interactionVersion = 0;
    let expectedScrollTop = getObservedScrollTop();
    const scrollCallback = (): void => {
        const observedScrollTop = getObservedScrollTop();
        if (Math.abs(observedScrollTop - expectedScrollTop) > 1) {
            interactionVersion++;
            expectedScrollTop = observedScrollTop;
            pendingAnchor = null;
            cancelScrollOperation();
        }
        if (scheduledScrollUpdate !== null) {
            return;
        }

        scheduledScrollUpdate = scheduleFrame(() => {
            scheduledScrollUpdate = null;
            requestVisibleItemDistribution(false);
            persistScrollPosition();
        });
    };

    const flushScrollPersistence = (): void => persistScrollPosition(true);

    scrollUpdateTargets.forEach(target => target.addEventListener('scroll', scrollCallback, scrollListenerOptions));
    {
        document.addEventListener('click', flushScrollPersistence, true);
        window.addEventListener('pagehide', flushScrollPersistence);
    }
    const scrollPollInterval = window.setInterval(() => {
        const observedScrollTop = getObservedScrollTop();
        if (observedScrollTop === lastObservedScrollTop) {
            return;
        }

        lastObservedScrollTop = observedScrollTop;
        scrollCallback();
    }, 100);

    const state: VirtualizeState = existingState ?? {
        measureItemSize: false,
        itemSizeVersion: 0,
        measuredSizes: new Map(),
        itemSize: Math.max(0, itemSize ?? 0),
        itemCount: 0,
        itemsBefore: 0,
        visibleItemCapacity: 0,
        unusedItemCapacity: 0,
        overscanCount: Math.max(0, overscanCount ?? 0),
        maxItemCount: Math.max(0, maxItemCount ?? 0),
    };
    state.itemSize = Math.max(0, itemSize ?? 0);
    state.overscanCount = Math.max(0, overscanCount ?? 0);
    state.maxItemCount = Math.max(0, maxItemCount ?? 0);

    const resizeObserver = typeof ResizeObserver === 'undefined' ? null : new ResizeObserver(() => {
        if (measureItems()) {
            requestVisibleItemDistribution(true);
        }
    });
    const measuredElements = new Set<Element>();
    let cumulativeSizes: { index: number; delta: number }[] | null = null;
    let lastRenderLoading = true;
    let lastRenderPlaceholderCount = 0;
    let hasKnownItemCount = state.itemCount > 0;
    let pendingIndex: number | null = initialItemIndex > 0 ? Math.floor(initialItemIndex) : null;
    let pendingOperation: { id: number; resolve: () => void; reject: (error: unknown) => void } | null = null;
    let pendingAnchor: { snapshot: AnchorSnapshot; index: number; mode: number } | null = null;
    if (pendingIndex !== null) {
        pendingScrollTop = null;
    }
    const interruptScroll = (event: Event): void => {
        if (event instanceof KeyboardEvent && !['ArrowUp', 'ArrowDown', 'PageUp', 'PageDown', 'Home', 'End', ' '].includes(event.key)) {
            return;
        }
        interactionVersion++;
        pendingAnchor = null;
        pendingScrollTop = null;
        cancelScrollOperation();
    };
    const inputTarget = scrollContainer ?? window;
    for (const event of ['wheel', 'touchstart', 'pointerdown', 'keydown']) {
        inputTarget.addEventListener(event, interruptScroll, { passive: true });
    }

    function cancelScrollOperation(operationId?: number): void {
        if (operationId !== undefined && pendingOperation?.id !== operationId && !(operationId === 0 && pendingOperation === null)) {
            return;
        }
        pendingIndex = null;
        pendingOperation?.resolve();
        pendingOperation = null;
    }

    function setItemScrollOffset(offset: number): void {
        const metrics = getScrollMetrics(scrollContainer, topSpacer);
        const target = Math.max(0, getObservedScrollTop() + offset - metrics.scrollTop);
        if (scrollContainer) {
            scrollContainer.scrollTop = target;
        }
        else {
            window.scrollTo(0, target);
        }
        expectedScrollTop = getObservedScrollTop();
    }

    function afterRender(placeholderCount: number, loading: boolean): void {
        hasKnownItemCount ||= state.itemCount > 0 || !loading;
        lastRenderLoading = loading;
        lastRenderPlaceholderCount = placeholderCount;
        if (pendingAnchor) {
            const { snapshot, index, mode } = pendingAnchor;
            pendingAnchor = null;
            if (snapshot.version === interactionVersion) {
                const { containerSize } = getScrollMetrics(scrollContainer, topSpacer);
                const offset = mode === 1 && snapshot.atStart ? 0
                    : mode === 2 && snapshot.atEnd ? Math.max(0, itemOffset(state.itemCount) - containerSize)
                    : itemOffset(Math.min(Math.max(0, index), Math.max(0, state.itemCount - 1))) + snapshot.offset;
                setItemScrollOffset(offset);
            }
        }
        if (pendingIndex === null || loading) {
            return;
        }
        if (state.itemCount === 0) {
            if (placeholderCount === 0) {
                cancelScrollOperation();
            }
            return;
        }
        pendingIndex = Math.min(pendingIndex, state.itemCount - 1);
        const renderedStart = Number(topSpacer.getAttribute('data-nt-virtualize-start') ?? state.itemsBefore);
        const renderedEnd = Number(bottomSpacer.getAttribute('data-nt-virtualize-end') ?? state.itemsBefore + state.visibleItemCapacity);
        if (pendingIndex >= renderedStart && pendingIndex < renderedEnd && placeholderCount === 0) {
            const { containerSize } = getScrollMetrics(scrollContainer, topSpacer);
            setItemScrollOffset(Math.min(itemOffset(pendingIndex), Math.max(0, itemOffset(state.itemCount) - containerSize)));
            cancelScrollOperation();
            persistScrollPosition(true);
        }
    }


    function measureItems(reset = false, preserveScrollOffset = false, scrollTopBeforeSizing?: number): boolean {
        const scrollOffset = getScrollMetrics(scrollContainer, topSpacer).scrollTop;
        const anchorIndex = Math.min(itemAtOffset(scrollOffset), Math.max(0, state.itemCount - 1));
        const anchorOffset = itemOffset(anchorIndex);
        const offsetWithinItem = Math.max(0, scrollOffset - anchorOffset);
        const previousScrollTop = scrollTopBeforeSizing ?? getObservedScrollTop();
        if (reset) {
            state.measuredSizes.clear();
            cumulativeSizes = null;
        }
        const sizes = new Map<number, number>();
        const elements = new Set<Element>();
        let row = topSpacer.nextElementSibling;
        while (state.measureItemSize && row && row !== bottomSpacer) {
            const indexAttribute = row.getAttribute('data-nt-virtualize-index');
            if (indexAttribute !== null) {
                const index = Number(indexAttribute);
                if (Number.isInteger(index) && index >= 0) {
                    sizes.set(index, (sizes.get(index) ?? 0) + row.getBoundingClientRect().height);
                    elements.add(row);
                    if (!measuredElements.has(row)) {
                        resizeObserver?.observe(row);
                    }
                }
            }
            row = row.nextElementSibling;
        }
        for (const element of measuredElements) {
            if (!elements.has(element)) {
                resizeObserver?.unobserve(element);
            }
        }
        measuredElements.clear();
        elements.forEach(element => measuredElements.add(element));
        let changed = reset;
        for (const [index, size] of sizes) {
            if (size > 0 && Math.abs((state.measuredSizes.get(index) ?? state.itemSize) - size) > 0.5) {
                state.measuredSizes.set(index, size);
                cumulativeSizes = null;
                changed = true;
            }
        }
        if (changed && pendingScrollTop === null) {
            const anchorSize = state.measuredSizes.get(anchorIndex) ?? state.itemSize;
            const nextOffsetWithinItem = Math.min(offsetWithinItem, Math.max(0, anchorSize - 1));
            const adjustment = itemOffset(anchorIndex) - anchorOffset + nextOffsetWithinItem - offsetWithinItem;
            // Update the currently rendered spacers before adjusting scrollTop. Waiting for
            // the asynchronous Blazor render would move the anchor a second time or clamp it.
            const renderedStart = Number(topSpacer.getAttribute('data-nt-virtualize-start') ?? state.itemsBefore);
            const renderedEnd = Number(bottomSpacer.getAttribute('data-nt-virtualize-end') ?? Math.min(state.itemCount, state.itemsBefore + state.visibleItemCapacity));
            setSpacerSize(topSpacer, itemOffset(renderedStart));
            setSpacerSize(bottomSpacer, itemOffset(state.itemCount) - itemOffset(renderedEnd));
            if (preserveScrollOffset || Math.abs(adjustment) > 0.5) {
                if (scrollContainer) {
                    scrollContainer.scrollTop = previousScrollTop + (preserveScrollOffset ? 0 : adjustment);
                }
                else {
                    window.scrollTo(0, previousScrollTop + (preserveScrollOffset ? 0 : adjustment));
                }
                expectedScrollTop = getObservedScrollTop();
                persistScrollPosition(true);
            }
        }
        return changed;
    }

    function setSpacerSize(spacer: HTMLElement, size: number): void {
        const height = `${Math.max(0, size)}px`;
        spacer.style.height = height;
        if (spacer.firstElementChild instanceof HTMLTableCellElement) {
            spacer.firstElementChild.style.height = height;
        }
    }

    function itemOffset(index: number): number {
        if (cumulativeSizes === null) {
            let delta = 0;
            cumulativeSizes = [...state.measuredSizes].sort(([a], [b]) => a - b).map(([measuredIndex, size]) => ({ index: measuredIndex, delta: delta += size - state.itemSize }));
        }
        let low = 0;
        let high = cumulativeSizes.length;
        while (low < high) {
            const middle = (low + high) >>> 1;
            if (cumulativeSizes[middle]!.index < index) {
                low = middle + 1;
            }
            else {
                high = middle;
            }
        }
        return index * state.itemSize + (low > 0 ? cumulativeSizes[low - 1]!.delta : 0);
    }

    function itemAtOffset(offset: number): number {
        if (state.measuredSizes.size === 0) {
            return Math.floor(offset / state.itemSize);
        }
        let low = 0;
        let high = state.itemCount;
        while (low < high) {
            const middle = Math.ceil((low + high) / 2);
            if (itemOffset(middle) <= offset) {
                low = middle;
            }
            else {
                high = middle - 1;
            }
        }
        return low;
    }

    observersByDotNetRef.set(dotNetRefKey, {
        applySizes: () => applySizes(topSpacer.parentElement),
        invalidateMeasurements: () => { cumulativeSizes = null; },
        updateOptions: (size, overscan, maximum, restorationKey) => {
            const sizeChanged = state.itemSize !== size;
            state.itemSize = Math.max(1, size);
            state.overscanCount = Math.max(0, overscan);
            state.maxItemCount = Math.max(1, maximum);
            const key = restorationKey?.trim() || null;
            if (key !== resolvedScrollRestorationKey) {
                resolvedScrollRestorationKey = key;
                pendingScrollTop = getPersistedScrollPosition(key);
            }
            if (sizeChanged) {
                state.measuredSizes.clear();
                cumulativeSizes = null;
                measureItems();
            }
            restoreScrollPosition();
            requestVisibleItemDistribution(true);
        },
        scrollToItem: (index, operationId) => {
            cancelScrollOperation();
            pendingAnchor = null;
            pendingScrollTop = null;
            pendingIndex = Math.max(0, Math.floor(index));
            return new Promise<void>((resolve, reject) => {
                pendingOperation = { id: operationId, resolve, reject };
                afterRender(lastRenderPlaceholderCount, lastRenderLoading);
                requestVisibleItemDistribution(pendingOperation !== null);
            });
        },
        cancelScroll: cancelScrollOperation,
        captureAnchor: () => {
            const { scrollTop, containerSize } = getScrollMetrics(scrollContainer, topSpacer);
            const index = Math.min(itemAtOffset(scrollTop), Math.max(0, state.itemCount - 1));
            return { index, offset: scrollTop - itemOffset(index), atStart: scrollTop <= 1, atEnd: scrollTop + containerSize >= itemOffset(state.itemCount) - 1, version: interactionVersion };
        },
        restoreAnchor: (snapshot, index, mode) => {
            if (snapshot.version !== interactionVersion) {
                return;
            }
            // Matching one item does not establish that every measured item moved by
            // the same delta (sorting and replacements can also move that identity).
            // Re-measure the new window rather than attach old heights to new items.
            state.measuredSizes.clear();
            cumulativeSizes = null;
            pendingAnchor = { snapshot, index, mode };
        },
        afterRender,
        removeInputListeners: () => {
            for (const event of ['wheel', 'touchstart', 'pointerdown', 'keydown']) {
                inputTarget.removeEventListener(event, interruptScroll);
            }
        },
        getScrollTop: getObservedScrollTop,
        measureItems,
        resizeObserver,
        cancelPendingScrollUpdate: () => {
            if (scheduledScrollUpdate !== null) {
                cancelFrame(scheduledScrollUpdate);
                scheduledScrollUpdate = null;
            }
        },
        cancelScrollPolling: () => window.clearInterval(scrollPollInterval),
        intersectionObserver,
        mutationObserverBefore,
        mutationObserverAfter,
        overflowAnchorElement,
        previousOverflowAnchor,
        removeScrollListener: () => scrollUpdateTargets.forEach(target => target.removeEventListener('scroll', scrollCallback, scrollListenerOptions)),
        removeScrollPersistenceListeners: () => {
            document.removeEventListener('click', flushScrollPersistence, true);
            window.removeEventListener('pagehide', flushScrollPersistence);
        },
        requestVisibleItemDistribution,
        restoreScrollPosition,
        state,
    });

    function createSpacerMutationObserver(spacer: HTMLElement): MutationObserver {
        const observerOptions: MutationObserverInit = { attributes: true, attributeFilter: ['style', 'data-nt-virtualize-size'] };
        const mutationObserver = new MutationObserver((_, observer) => {
            if (isValidTableElement(spacer.parentElement)) {
                observer.disconnect();
                spacer.style.display = 'table-row';
                observer.observe(spacer, observerOptions);
            }

            intersectionObserver.unobserve(spacer);
            intersectionObserver.observe(spacer);

            if (restoreScrollPosition()) {
                requestVisibleItemDistribution(true);
            }
        });

        mutationObserver.observe(spacer, observerOptions);
        return mutationObserver;
    }

    function persistScrollPosition(force = false): void {
        if (!resolvedScrollRestorationKey || pendingScrollTop !== null) {
            return;
        }

        const now = performance.now();
        if (!force && now - lastScrollPersistenceTime < scrollPersistenceInterval) {
            return;
        }

        lastScrollPersistenceTime = now;
        const scrollTop = getObservedScrollTop();
        const historyState = isRecord(history.state) ? history.state : {};
        const existingPositions = isRecord(historyState[scrollPositionsStateKey]) ? historyState[scrollPositionsStateKey] : {};
        if (existingPositions[resolvedScrollRestorationKey] === scrollTop) {
            return;
        }

        try {
            history.replaceState({
                ...historyState,
                [scrollPositionsStateKey]: {
                    ...existingPositions,
                    [resolvedScrollRestorationKey]: scrollTop,
                },
            }, '');
        }
        catch {
            // Scroll restoration must never interrupt virtualization when history state is unavailable.
        }
    }

    function restoreScrollPosition(): boolean {
        if (pendingScrollTop === null) {
            return false;
        }

        if (pendingScrollTop > 0 && state.itemCount === 0) {
            return false;
        }

        scrollContainer ??= findClosestScrollContainer(topSpacer);
        const { containerSize } = getScrollMetrics(scrollContainer, topSpacer);
        const targetScrollTop = Math.min(pendingScrollTop, Math.max(0, itemOffset(state.itemCount) - containerSize));
        const availableScrollTop = scrollContainer
            ? Math.max(0, scrollContainer.scrollHeight - scrollContainer.clientHeight)
            : Math.max(0, document.documentElement.scrollHeight - (window.innerHeight || document.documentElement.clientHeight));
        if (availableScrollTop + 1 < targetScrollTop) {
            return false;
        }

        if (scrollContainer) {
            scrollContainer.scrollTop = targetScrollTop;
        }
        else {
            window.scrollTo(0, targetScrollTop);
        }

        if (Math.abs(getObservedScrollTop() - targetScrollTop) >= 1) {
            return false;
        }

        pendingScrollTop = null;
        expectedScrollTop = getObservedScrollTop();
        return true;
    }

    function intersectionCallback(entries: IntersectionObserverEntry[]): void {
        if (!topSpacer.parentElement || !bottomSpacer.parentElement || !entries.some(entry => entry.isIntersecting)) {
            return;
        }

        requestVisibleItemDistribution(false);
    }

    function requestVisibleItemDistribution(forceUpdate: boolean): void {
        if (!topSpacer.parentElement || !bottomSpacer.parentElement) {
            return;
        }

        scrollContainer ??= findClosestScrollContainer(topSpacer);
        const { scrollTop, containerSize } = getScrollMetrics(scrollContainer, topSpacer);
        const requestedOffset = pendingIndex === null ? scrollTop : itemOffset(hasKnownItemCount ? Math.min(pendingIndex, Math.max(0, state.itemCount - 1)) : pendingIndex);
        const { itemsBefore, visibleItemCapacity, unusedItemCapacity } = calculateItemDistribution(containerSize, requestedOffset);

        updateItemDistribution(itemsBefore, visibleItemCapacity, unusedItemCapacity, forceUpdate);
    }

    function getObservedScrollTop(): number {
        scrollContainer ??= findClosestScrollContainer(topSpacer);
        return scrollContainer ? Math.max(0, scrollContainer.scrollTop) : Math.max(0, window.scrollY || document.documentElement.scrollTop || document.body.scrollTop || 0);
    }

    function calculateItemDistribution(containerSize: number, scrollTop: number): VirtualizeDistribution {
        const maxItemCapacity = state.maxItemCount + state.overscanCount * 2;
        const firstVisibleIndex = itemAtOffset(scrollTop);
        let visibleItemCapacity = Math.ceil(containerSize / state.itemSize) + 2 * state.overscanCount;
        if (state.measureItemSize) {
            // The estimate is a minimum: shorter measured rows need more items to fill the viewport.
            const viewportEnd = scrollTop + containerSize;
            const endIndex = itemAtOffset(viewportEnd);
            const requiredCount = endIndex - firstVisibleIndex + (itemOffset(endIndex) < viewportEnd ? 1 : 0);
            visibleItemCapacity = Math.max(visibleItemCapacity, requiredCount + 2 * state.overscanCount);
        }
        const unusedItemCapacity = Math.max(0, visibleItemCapacity - maxItemCapacity);
        visibleItemCapacity -= unusedItemCapacity;

        return {
            itemsBefore: Math.max(0, firstVisibleIndex - state.overscanCount),
            visibleItemCapacity,
            unusedItemCapacity,
        };
    }

    function updateItemDistribution(itemsBefore: number, visibleItemCapacity: number, unusedItemCapacity: number, forceUpdate = false): void {
        if (hasKnownItemCount && itemsBefore + visibleItemCapacity > state.itemCount) {
            itemsBefore = Math.max(0, state.itemCount - visibleItemCapacity);
        }

        if (itemsBefore === state.itemsBefore
            && visibleItemCapacity === state.visibleItemCapacity
            && unusedItemCapacity === state.unusedItemCapacity
            && !forceUpdate) {
            return;
        }

        state.itemsBefore = itemsBefore;
        state.visibleItemCapacity = visibleItemCapacity;
        state.unusedItemCapacity = unusedItemCapacity;

        const topSpacerSize = itemOffset(itemsBefore);
        const itemsAfter = Math.max(0, state.itemCount - visibleItemCapacity - itemsBefore);
        const bottomSpacerSize = itemOffset(state.itemCount) - itemOffset(state.itemCount - itemsAfter) + unusedItemCapacity * state.itemSize;

        const operation = pendingOperation;
        void Promise.resolve(activeDotNetRef.invokeMethodAsync(
            'LoadItems',
            topSpacerSize,
            bottomSpacerSize,
            itemsBefore,
            visibleItemCapacity)).catch(error => {
                if (operation && pendingOperation === operation) {
                    pendingIndex = null;
                    pendingOperation = null;
                    operation.reject(error);
                }
            });
    }
}

function getDotNetRefKey(dotNetRef: DotNetVirtualizeRef): DotNetVirtualizeRefKey {
    return dotNetRef._id ?? dotNetRef;
}

function getPersistedScrollPosition(scrollRestorationKey: string | null): number | null {
    if (!scrollRestorationKey || !isRecord(history.state)) {
        return null;
    }

    const positions = history.state[scrollPositionsStateKey];
    if (!isRecord(positions)) {
        return null;
    }

    const scrollTop = positions[scrollRestorationKey];
    return typeof scrollTop === 'number' && Number.isFinite(scrollTop) && scrollTop >= 0 ? scrollTop : null;
}

function isRecord(value: unknown): value is Record<string, unknown> {
    return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function isValidTableElement(element: Maybe<Element>): boolean {
    if (!(element instanceof HTMLElement)) {
        return false;
    }

    return ((element instanceof HTMLTableElement && element.style.display === '') || element.style.display === 'table')
        || ((element instanceof HTMLTableSectionElement && element.style.display === '') || element.style.display === 'table-row-group');
}

function getScrollMetrics(scrollContainer: Maybe<HTMLElement>, topSpacer: HTMLElement): ScrollMetrics {
    const topSpacerRect = topSpacer.getBoundingClientRect();
    const viewportTop = scrollContainer ? scrollContainer.getBoundingClientRect().top + scrollContainer.clientTop : 0;
    let headerInset = 0;
    const header = topSpacer.closest('table')?.tHead;
    if (header) {
        for (const cell of header.querySelectorAll('th')) {
            const style = getComputedStyle(cell);
            if (style.position !== 'sticky' || style.top === 'auto') {
                continue;
            }
            const rect = cell.getBoundingClientRect();
            if (rect.top <= viewportTop + (parsePixelValue(style.top) ?? 0) + 1 && rect.bottom > viewportTop) {
                headerInset = Math.max(headerInset, rect.bottom - viewportTop);
            }
        }
    }

    if (scrollContainer) {
        const scrollContainerRect = scrollContainer.getBoundingClientRect();
        let containerSize = scrollContainer.clientHeight;

        // max-height can be larger than clientHeight before initial overflow exists.
        const maxHeight = parsePixelValue(getComputedStyle(scrollContainer).maxHeight);
        if (maxHeight !== null && maxHeight > containerSize) {
            containerSize = maxHeight;
        }

        return {
            scrollTop: Math.max(0, scrollContainerRect.top + scrollContainer.clientTop + headerInset - topSpacerRect.top),
            containerSize: Math.max(0, containerSize - headerInset),
        };
    }

    return {
        scrollTop: Math.max(0, headerInset - topSpacerRect.top),
        containerSize: Math.max(0, (window.innerHeight || document.documentElement.clientHeight) - headerInset),
    };
}

function parsePixelValue(value: string): number | null {
    if (!value || value === 'none') {
        return null;
    }

    const parsed = Number.parseFloat(value);
    return Number.isFinite(parsed) ? parsed : null;
}

function findClosestScrollContainer(element: Maybe<HTMLElement>): HTMLElement | null {
    let currentElement = element;
    while (currentElement && currentElement !== document.body && currentElement !== document.documentElement) {
        if (isScrollableElement(currentElement)) {
            return currentElement;
        }

        currentElement = currentElement.parentElement;
    }

    return null;
}

function isScrollableElement(element: HTMLElement): boolean {
    const overflowY = getComputedStyle(element).overflowY;
    return isScrollableOverflow(overflowY) || (overflowY !== 'visible' && element.scrollHeight > element.clientHeight);
}

function isScrollableOverflow(overflowY: string): boolean {
    return overflowY === 'auto' || overflowY === 'scroll' || overflowY === 'overlay';
}

function getScrollUpdateTargets(element: HTMLElement, scrollContainer: Maybe<HTMLElement>): EventTarget[] {
    const targets = new Set<EventTarget>();
    if (scrollContainer) {
        targets.add(scrollContainer);
    }

    let currentElement: Maybe<HTMLElement> = element;
    while (currentElement && currentElement !== document.body && currentElement !== document.documentElement) {
        targets.add(currentElement);
        currentElement = currentElement.parentElement;
    }

    targets.add(window);
    return Array.from(targets);
}

function dispose(dotNetRef: Maybe<DotNetVirtualizeRef>): void {
    if (!dotNetRef) {
        return;
    }

    const observers = observersByDotNetRef.get(getDotNetRefKey(dotNetRef));
    if (!observers) {
        return;
    }

    observers.cancelScroll();
    observers.removeInputListeners();
    observers.resizeObserver?.disconnect();
    observers.intersectionObserver.disconnect();
    observers.mutationObserverBefore.disconnect();
    observers.mutationObserverAfter.disconnect();
    observers.cancelPendingScrollUpdate();
    observers.cancelScrollPolling();
    observers.removeScrollListener();
    observers.removeScrollPersistenceListeners();

    if (observers.overflowAnchorElement.isConnected) {
        observers.overflowAnchorElement.style.overflowAnchor = observers.previousOverflowAnchor;
    }

    observersByDotNetRef.delete(getDotNetRefKey(dotNetRef));
}

export function updateRenderState(dotNetRef: Maybe<DotNetVirtualizeRef>, itemCount: Maybe<number>, _lastRenderedItemCount: Maybe<number>, _lastRenderedPlaceholderCount: Maybe<number>, measureItemSize = false, itemSizeVersion = 0, loading = false, preserveScrollOffset = false): void {
    if (!dotNetRef) {
        return;
    }

    const observers = observersByDotNetRef.get(getDotNetRefKey(dotNetRef));
    if (!observers) {
        return;
    }

    const previousItemCount = observers.state.itemCount;
    observers.state.itemCount = Math.max(0, itemCount ?? 0);
    const measurementChanged = observers.state.measureItemSize !== measureItemSize;
    observers.state.measureItemSize = measureItemSize;
    const versionChanged = observers.state.itemSizeVersion !== itemSizeVersion;
    if (versionChanged) {
        observers.state.itemSizeVersion = itemSizeVersion;
    }
    for (const index of observers.state.measuredSizes.keys()) {
        if (index >= observers.state.itemCount) {
            observers.state.measuredSizes.delete(index);
            observers.invalidateMeasurements();
        }
    }
    const scrollTopBeforeSizing = preserveScrollOffset ? observers.getScrollTop() : undefined;
    observers.applySizes();
    const sizesChanged = (measureItemSize || measurementChanged) && observers.measureItems(versionChanged || measurementChanged, preserveScrollOffset, scrollTopBeforeSizing);
    observers.afterRender(_lastRenderedPlaceholderCount ?? 0, loading);
    const restoredScrollPosition = observers.restoreScrollPosition();

    observers.requestVisibleItemDistribution(versionChanged || sizesChanged || observers.state.itemCount !== previousItemCount || restoredScrollPosition);
}

function applySizes(element: Maybe<HTMLElement>): void {
    if (!element) {
        return;
    }
    const targets = [...element.querySelectorAll<HTMLElement>('[data-nt-virtualize-size]')];
    if (element.hasAttribute('data-nt-virtualize-size')) {
        targets.push(element);
    }
    for (const target of targets) {
        const size = Number(target.getAttribute('data-nt-virtualize-size'));
        if (Number.isFinite(size) && size >= 0) {
            const height = `${size}px`;
            if (target.style.height !== height) {
                target.style.height = height;
            }
            if (target.style.flexShrink !== '0') {
                target.style.flexShrink = '0';
            }
            if (target.firstElementChild instanceof HTMLTableCellElement && target.firstElementChild.style.height !== height) {
                target.firstElementChild.style.height = height;
            }
            if (target.hasAttribute('data-nt-virtualize-placeholder') && target.style.getPropertyValue('--nt-data-grid-placeholder-row-height') !== height) {
                target.style.setProperty('--nt-data-grid-placeholder-row-height', height);
            }
        }
    }
}

export function updateOptions(dotNetRef: DotNetVirtualizeRef, itemSize: number, overscanCount: number, maxItemCount: number, scrollRestorationKey?: string | null): void {
    observersByDotNetRef.get(getDotNetRefKey(dotNetRef))?.updateOptions(itemSize, overscanCount, maxItemCount, scrollRestorationKey);
}

export function scrollToItem(dotNetRef: DotNetVirtualizeRef, index: number, operationId: number): Promise<void> {
    return observersByDotNetRef.get(getDotNetRefKey(dotNetRef))?.scrollToItem(index, operationId) ?? Promise.resolve();
}

export function cancelScroll(dotNetRef: DotNetVirtualizeRef, operationId: number): void {
    observersByDotNetRef.get(getDotNetRefKey(dotNetRef))?.cancelScroll(operationId);
}

export function captureAnchor(dotNetRef: DotNetVirtualizeRef): AnchorSnapshot | null {
    return observersByDotNetRef.get(getDotNetRefKey(dotNetRef))?.captureAnchor() ?? null;
}

export function restoreAnchor(dotNetRef: DotNetVirtualizeRef, snapshot: AnchorSnapshot, index: number, anchorMode: number): void {
    observersByDotNetRef.get(getDotNetRefKey(dotNetRef))?.restoreAnchor(snapshot, index, anchorMode);
}

export function onLoad(element: Maybe<HTMLElement>, _dotNetRef: Maybe<DotNetVirtualizeRef>): void {
    applySizes(element?.parentElement);
}

export function onUpdate(element: Maybe<HTMLElement>, _dotNetRef: Maybe<DotNetVirtualizeRef>): void {
    applySizes(element?.parentElement);
}

export function onDispose(_element: Maybe<HTMLElement>, dotNetRef: Maybe<DotNetVirtualizeRef>): void {
    dispose(dotNetRef);
}
