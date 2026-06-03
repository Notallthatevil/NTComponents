const observersByDotNetRef = new Map();
export function init(dotNetRef, topSpacer, bottomSpacer, itemSize, overscanCount, maxItemCount, rootMargin = 50) {
    const dotNetRefKey = getDotNetRefKey(dotNetRef);
    const existingState = observersByDotNetRef.get(dotNetRefKey)?.state;
    dispose(dotNetRef);
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
    let scheduledScrollUpdate = null;
    const scheduleFrame = window.requestAnimationFrame ?? ((callback) => window.setTimeout(() => callback(performance.now()), 0));
    const cancelFrame = window.cancelAnimationFrame ?? window.clearTimeout;
    const scrollListenerOptions = { passive: true, capture: true };
    let lastObservedScrollTop = -1;
    const scrollCallback = () => {
        if (scheduledScrollUpdate !== null) {
            return;
        }
        scheduledScrollUpdate = scheduleFrame(() => {
            scheduledScrollUpdate = null;
            requestVisibleItemDistribution(false);
        });
    };
    scrollUpdateTargets.forEach(target => target.addEventListener('scroll', scrollCallback, scrollListenerOptions));
    const scrollPollInterval = window.setInterval(() => {
        const observedScrollTop = getObservedScrollTop();
        if (observedScrollTop === lastObservedScrollTop) {
            return;
        }
        lastObservedScrollTop = observedScrollTop;
        scrollCallback();
    }, 100);
    const state = existingState ?? {
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
    observersByDotNetRef.set(dotNetRefKey, {
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
        requestVisibleItemDistribution,
        state,
    });
    function createSpacerMutationObserver(spacer) {
        const observerOptions = { attributes: true, attributeFilter: ['style'] };
        const mutationObserver = new MutationObserver((_, observer) => {
            if (isValidTableElement(spacer.parentElement)) {
                observer.disconnect();
                spacer.style.display = 'table-row';
                observer.observe(spacer, observerOptions);
            }
            intersectionObserver.unobserve(spacer);
            intersectionObserver.observe(spacer);
        });
        mutationObserver.observe(spacer, observerOptions);
        return mutationObserver;
    }
    function intersectionCallback(entries) {
        if (!topSpacer.parentElement || !bottomSpacer.parentElement || !entries.some(entry => entry.isIntersecting)) {
            return;
        }
        requestVisibleItemDistribution(false);
    }
    function requestVisibleItemDistribution(forceUpdate) {
        if (!topSpacer.parentElement || !bottomSpacer.parentElement) {
            return;
        }
        scrollContainer ??= findClosestScrollContainer(topSpacer);
        const { scrollTop, containerSize } = getScrollMetrics(scrollContainer, topSpacer);
        const { itemsBefore, visibleItemCapacity, unusedItemCapacity } = calculateItemDistribution(containerSize, scrollTop);
        updateItemDistribution(itemsBefore, visibleItemCapacity, unusedItemCapacity, forceUpdate);
    }
    function getObservedScrollTop() {
        scrollContainer ??= findClosestScrollContainer(topSpacer);
        return scrollContainer ? Math.max(0, scrollContainer.scrollTop) : Math.max(0, window.scrollY || document.documentElement.scrollTop || document.body.scrollTop || 0);
    }
    function calculateItemDistribution(containerSize, scrollTop) {
        const maxItemCapacity = state.maxItemCount + state.overscanCount * 2;
        let visibleItemCapacity = Math.ceil(containerSize / state.itemSize) + 2 * state.overscanCount;
        const unusedItemCapacity = Math.max(0, visibleItemCapacity - maxItemCapacity);
        visibleItemCapacity -= unusedItemCapacity;
        return {
            itemsBefore: Math.max(0, Math.floor(scrollTop / state.itemSize) - state.overscanCount),
            visibleItemCapacity,
            unusedItemCapacity,
        };
    }
    function updateItemDistribution(itemsBefore, visibleItemCapacity, unusedItemCapacity, forceUpdate = false) {
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
        const topSpacerSize = itemsBefore * state.itemSize;
        const itemsAfter = Math.max(0, state.itemCount - visibleItemCapacity - itemsBefore);
        const bottomSpacerSize = (itemsAfter + unusedItemCapacity) * state.itemSize;
        void dotNetRef.invokeMethodAsync('LoadItems', topSpacerSize, bottomSpacerSize, itemsBefore, visibleItemCapacity);
    }
}
function getDotNetRefKey(dotNetRef) {
    return dotNetRef._id ?? dotNetRef;
}
function isValidTableElement(element) {
    if (!(element instanceof HTMLElement)) {
        return false;
    }
    return ((element instanceof HTMLTableElement && element.style.display === '') || element.style.display === 'table')
        || ((element instanceof HTMLTableSectionElement && element.style.display === '') || element.style.display === 'table-row-group');
}
function getScrollMetrics(scrollContainer, topSpacer) {
    const topSpacerRect = topSpacer.getBoundingClientRect();
    if (scrollContainer) {
        const scrollContainerRect = scrollContainer.getBoundingClientRect();
        let containerSize = scrollContainer.clientHeight;
        const maxHeight = parsePixelValue(getComputedStyle(scrollContainer).maxHeight);
        if (maxHeight !== null && maxHeight > containerSize) {
            containerSize = maxHeight;
        }
        return {
            scrollTop: Math.max(0, scrollContainer.scrollTop),
            containerSize,
        };
    }
    return {
        scrollTop: Math.max(0, -topSpacerRect.top),
        containerSize: window.innerHeight || document.documentElement.clientHeight,
    };
}
function parsePixelValue(value) {
    if (!value || value === 'none') {
        return null;
    }
    const parsed = Number.parseFloat(value);
    return Number.isFinite(parsed) ? parsed : null;
}
function findClosestScrollContainer(element) {
    let currentElement = element;
    while (currentElement && currentElement !== document.body && currentElement !== document.documentElement) {
        if (isScrollableElement(currentElement)) {
            return currentElement;
        }
        currentElement = currentElement.parentElement;
    }
    return null;
}
function isScrollableElement(element) {
    const overflowY = getComputedStyle(element).overflowY;
    return isScrollableOverflow(overflowY) || (overflowY !== 'visible' && element.scrollHeight > element.clientHeight);
}
function isScrollableOverflow(overflowY) {
    return overflowY === 'auto' || overflowY === 'scroll' || overflowY === 'overlay';
}
function getScrollUpdateTargets(element, scrollContainer) {
    const targets = new Set();
    if (scrollContainer) {
        targets.add(scrollContainer);
    }
    let currentElement = element;
    while (currentElement && currentElement !== document.body && currentElement !== document.documentElement) {
        targets.add(currentElement);
        currentElement = currentElement.parentElement;
    }
    targets.add(window);
    return Array.from(targets);
}
function dispose(dotNetRef) {
    if (!dotNetRef) {
        return;
    }
    const observers = observersByDotNetRef.get(getDotNetRefKey(dotNetRef));
    if (!observers) {
        return;
    }
    observers.intersectionObserver.disconnect();
    observers.mutationObserverBefore.disconnect();
    observers.mutationObserverAfter.disconnect();
    observers.cancelPendingScrollUpdate();
    observers.cancelScrollPolling();
    observers.removeScrollListener();
    if (observers.overflowAnchorElement.isConnected) {
        observers.overflowAnchorElement.style.overflowAnchor = observers.previousOverflowAnchor;
    }
    observersByDotNetRef.delete(getDotNetRefKey(dotNetRef));
}
export function updateRenderState(dotNetRef, itemCount, _lastRenderedItemCount, _lastRenderedPlaceholderCount) {
    if (!dotNetRef) {
        return;
    }
    const observers = observersByDotNetRef.get(getDotNetRefKey(dotNetRef));
    if (!observers) {
        return;
    }
    const previousItemCount = observers.state.itemCount;
    observers.state.itemCount = Math.max(0, itemCount ?? 0);
    if (observers.state.itemCount !== previousItemCount) {
        observers.requestVisibleItemDistribution(true);
    }
}
export function onLoad(_element, _dotNetRef) {
}
export function onUpdate(_element, _dotNetRef) {
}
export function onDispose(_element, dotNetRef) {
    dispose(dotNetRef);
}
