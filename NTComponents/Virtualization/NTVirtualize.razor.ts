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

interface VirtualizeObservers {
    measureItems: (reset?: boolean) => boolean;
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
export function init(dotNetRef: Maybe<DotNetVirtualizeRef>, topSpacer: HTMLElement, bottomSpacer: HTMLElement, itemSize: number, overscanCount: number, maxItemCount: number, scrollRestorationKey?: string | null, rootMargin = 50): void {
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
    const resolvedScrollRestorationKey = scrollRestorationKey?.trim() || null;
    let pendingScrollTop = getPersistedScrollPosition(resolvedScrollRestorationKey);
    let scheduledScrollUpdate: number | null = null;
    let lastScrollPersistenceTime = Number.NEGATIVE_INFINITY;
    const scheduleFrame = window.requestAnimationFrame ?? ((callback: FrameRequestCallback) => window.setTimeout(() => callback(performance.now()), 0));
    const cancelFrame = window.cancelAnimationFrame ?? window.clearTimeout;
    const scrollListenerOptions: AddEventListenerOptions = { passive: true, capture: true };
    let lastObservedScrollTop = -1;
    const scrollCallback = (): void => {
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
    if (resolvedScrollRestorationKey) {
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

    function measureItems(reset = false): boolean {
        const scrollOffset = getScrollMetrics(scrollContainer, topSpacer).scrollTop;
        const anchorIndex = Math.min(itemAtOffset(scrollOffset), Math.max(0, state.itemCount - 1));
        const anchorOffset = itemOffset(anchorIndex);
        const offsetWithinItem = Math.max(0, scrollOffset - anchorOffset);
        const previousScrollTop = getObservedScrollTop();
        if (reset) {
            state.measuredSizes.clear();
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
            if (Math.abs(adjustment) > 0.5) {
                if (scrollContainer) {
                    scrollContainer.scrollTop = previousScrollTop + adjustment;
                }
                else {
                    window.scrollTo(0, previousScrollTop + adjustment);
                }
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
        let offset = index * state.itemSize;
        for (const [measuredIndex, size] of state.measuredSizes) {
            if (measuredIndex < index) {
                offset += size - state.itemSize;
            }
        }
        return offset;
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
        const observerOptions: MutationObserverInit = { attributes: true, attributeFilter: ['style'] };
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
        const { itemsBefore, visibleItemCapacity, unusedItemCapacity } = calculateItemDistribution(containerSize, scrollTop);

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
        if (itemsBefore + visibleItemCapacity > state.itemCount) {
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

        void activeDotNetRef.invokeMethodAsync(
            'LoadItems',
            topSpacerSize,
            bottomSpacerSize,
            itemsBefore,
            visibleItemCapacity);
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

    if (scrollContainer) {
        const scrollContainerRect = scrollContainer.getBoundingClientRect();
        let containerSize = scrollContainer.clientHeight;

        // max-height can be larger than clientHeight before initial overflow exists.
        const maxHeight = parsePixelValue(getComputedStyle(scrollContainer).maxHeight);
        if (maxHeight !== null && maxHeight > containerSize) {
            containerSize = maxHeight;
        }

        return {
            scrollTop: Math.max(0, scrollContainerRect.top + scrollContainer.clientTop - topSpacerRect.top),
            containerSize,
        };
    }

    return {
        scrollTop: Math.max(0, -topSpacerRect.top),
        containerSize: window.innerHeight || document.documentElement.clientHeight,
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

export function updateRenderState(dotNetRef: Maybe<DotNetVirtualizeRef>, itemCount: Maybe<number>, _lastRenderedItemCount: Maybe<number>, _lastRenderedPlaceholderCount: Maybe<number>, measureItemSize = false, itemSizeVersion = 0): void {
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
        }
    }
    const sizesChanged = (measureItemSize || measurementChanged) && observers.measureItems(versionChanged || measurementChanged);
    const restoredScrollPosition = observers.restoreScrollPosition();

    if (versionChanged || sizesChanged || observers.state.itemCount !== previousItemCount || restoredScrollPosition) {
        observers.requestVisibleItemDistribution(true);
    }
}

export function onLoad(_element: Maybe<HTMLElement>, _dotNetRef: Maybe<DotNetVirtualizeRef>): void {
}

export function onUpdate(_element: Maybe<HTMLElement>, _dotNetRef: Maybe<DotNetVirtualizeRef>): void {
}

export function onDispose(_element: Maybe<HTMLElement>, dotNetRef: Maybe<DotNetVirtualizeRef>): void {
    dispose(dotNetRef);
}
