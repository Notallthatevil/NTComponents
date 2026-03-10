const dispatcherObserversByDotNetIdPropname = Symbol();

/**
 * Initializes the virtualization component by setting up intersection observers and mutation observers
 * for the top and bottom spacers to handle dynamic loading of items in a virtualized list.
 * @param {object} dotNetRef - Reference to the .NET component instance.
 * @param {HTMLElement} topSpacer - The top spacer element used for virtualization.
 * @param {HTMLElement} bottomSpacer - The bottom spacer element used for virtualization.
 * @param {number} itemSize - The size of each item in pixels.
 * @param {number} overscanCount - The number of extra items to render outside the visible area.
 * @param {number} maxItemCount - The maximum number of items to render.
 * @param {number} rootMargin - The margin around the root for intersection observation (default 50px).
 */
export function init(dotNetRef, topSpacer, bottomSpacer, itemSize, overscanCount, maxItemCount, rootMargin = 50) {
    const scrollContainer = findClosestScrollContainer(topSpacer);
    (scrollContainer || document.documentElement).style.overflowAnchor = 'none';

    const rangeBetweenSpacers = document.createRange();
    let topSpacerTriggered = false;

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

    // State object holding virtualization parameters and current rendering state
    const state = {
        itemSize: Math.max(0, itemSize ?? 0),
        measuredItemSize: Math.max(0, itemSize ?? 0),
        hasStableItemSize: false,
        lastRenderedItemCount: 0,
        lastRenderedPlaceholderCount: 0,
        itemCount: 0,
        itemsBefore: 0,
        visibleItemCapacity: 0,
        unusedItemCapacity: 0,
        overscanCount: Math.max(0, overscanCount ?? 0),
        maxItemCount: Math.max(0, maxItemCount ?? 0),
    };

    const { observersByDotNetObjectId, id } = getObserversMapEntry(dotNetRef);
    observersByDotNetObjectId[id] = {
        intersectionObserver,
        mutationObserverBefore,
        mutationObserverAfter,
        state,
    };

    /**
     * Creates a mutation observer for a spacer element to handle changes in its style attributes,
     * particularly for table elements, and re-observes it with the intersection observer.
     * @param {HTMLElement} spacer - The spacer element to observe.
     * @returns {MutationObserver} The created mutation observer.
     */
    function createSpacerMutationObserver(spacer) {
        const observerOptions = { attributes: true, attributeFilter: ['style'] };

        const mutationObserver = new MutationObserver((mutations, observer) => {
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

    /**
     * Callback function for intersection observer events. Calculates and updates the item distribution
     * based on the current scroll position and spacer separation when spacers intersect the viewport.
     * @param {IntersectionObserverEntry[]} entries - The intersection observer entries.
     */
    function intersectionCallback(entries) {
        if (!topSpacer.parentElement || !bottomSpacer.parentElement) {
            return;
        }

        rangeBetweenSpacers.setStartAfter(topSpacer);
        rangeBetweenSpacers.setEndBefore(bottomSpacer);
        const hasIntersection = entries.some((entry) => entry.isIntersecting);
        if (!hasIntersection) {
            return;
        }

        if (entries[0].target === topSpacer) {
            topSpacerTriggered = true;
        }
        else {
            topSpacerTriggered = false;
        }
        const spacerSeparation = rangeBetweenSpacers.getBoundingClientRect().height;
        const { scrollTop, containerSize } = getScrollMetrics(scrollContainer, topSpacer);
        const { itemsBefore, visibleItemCapacity, unusedItemCapacity } = calculateItemDistribution(
            spacerSeparation,
            containerSize,
            scrollTop);

        updateItemDistribution(itemsBefore, visibleItemCapacity, unusedItemCapacity);
    }
    /**
     * Calculates the distribution of items based on spacer separation, container size, and scroll position.
     * @param {number} spacerSeparation - The height between top and bottom spacers.
     * @param {number} containerSize - The height of the scroll container.
     * @param {number} scrollTop - The current scroll top position.
     * @returns {object} An object containing itemsBefore, visibleItemCapacity, and unusedItemCapacity.
     */
    function calculateItemDistribution(spacerSeparation, containerSize, scrollTop) {
        const maxItemCapacity = state.maxItemCount + state.overscanCount * 2;
        let visibleItemCapacity = Math.ceil(containerSize / state.itemSize) + 2 * state.overscanCount;
        const unusedItemCapacity = Math.max(0, visibleItemCapacity - maxItemCapacity);
        visibleItemCapacity -= unusedItemCapacity;
        const itemsBefore = Math.max(0, Math.floor(scrollTop / state.itemSize) - state.overscanCount);

        return {
            itemsBefore,
            visibleItemCapacity,
            unusedItemCapacity,
        };
    }

    /**
     * Updates the item distribution state and invokes the .NET method to load items if changes occurred.
     * @param {number} itemsBefore - Number of items before the visible range.
     * @param {number} visibleItemCapacity - Number of items that can be visible.
     * @param {number} unusedItemCapacity - Excess capacity not used.
     */
    function updateItemDistribution(itemsBefore, visibleItemCapacity, unusedItemCapacity) {
        if (itemsBefore + visibleItemCapacity > state.itemCount) {
            itemsBefore = Math.max(0, state.itemCount - visibleItemCapacity);
        }

        if (itemsBefore !== state.itemsBefore
            || visibleItemCapacity !== state.visibleItemCapacity
            || unusedItemCapacity !== state.unusedItemCapacity) {
            state.itemsBefore = itemsBefore;
            state.visibleItemCapacity = visibleItemCapacity;
            state.unusedItemCapacity = unusedItemCapacity;
            const topSpacerSize = itemsBefore * state.itemSize;
            const itemsAfter = Math.max(0, state.itemCount - visibleItemCapacity - itemsBefore);
            const bottomSpacerSize = (itemsAfter + unusedItemCapacity) * state.itemSize;

            const spacerTriggered = topSpacerTriggered ? 'TopSpacer' : 'BottomSpacer';

            dotNetRef.invokeMethodAsync(
                'LoadItems',
                topSpacerSize,
                bottomSpacerSize,
                itemsBefore,
                visibleItemCapacity);
        }
    }

    /**
     * Checks if the given element is a valid table or table section element for virtualization.
     * @param {HTMLElement} element - The element to check.
     * @returns {boolean} True if the element is a valid table element.
     */
    function isValidTableElement(element) {
        if (!element) {
            return false;
        }

        return ((element instanceof HTMLTableElement && element.style.display === '') || element.style.display === 'table')
            || ((element instanceof HTMLTableSectionElement && element.style.display === '') || element.style.display === 'table-row-group');
    }

    /**
     * Retrieves the scroll metrics (scrollTop and containerSize) for the given scroll container.
     * If no container is provided, uses the document element.
     * @param {HTMLElement} scrollContainer - The scroll container element.
     * @returns {object} An object containing scrollTop and containerSize.
     */
    function getScrollMetrics(scrollContainer, topSpacer) {
        const topSpacerRect = topSpacer.getBoundingClientRect();

        if (scrollContainer) {
            const scrollContainerRect = scrollContainer.getBoundingClientRect();
            return {
                scrollTop: Math.max(0, scrollContainerRect.top - topSpacerRect.top),
                containerSize: scrollContainer.clientHeight,
            };
        }

        return {
            scrollTop: Math.max(0, -topSpacerRect.top),
            containerSize: window.innerHeight || document.documentElement.clientHeight,
        };
    }
}
/**
* Finds the closest scrollable container element by traversing up the DOM tree.
* @param {HTMLElement} element - The starting element.
* @returns {HTMLElement|null} The closest scroll container or null if none found.
*/
function findClosestScrollContainer(element) {
    if (!element || element === document.body || element === document.documentElement) {
        return null;
    }

    const style = getComputedStyle(element);

    if (isScrollableOverflow(style.overflowY) && element !== document.body && element !== document.documentElement) {
        return element;
    }

    return findClosestScrollContainer(element.parentElement);
}

function isScrollableOverflow(overflowY) {
    return overflowY === 'auto' || overflowY === 'scroll' || overflowY === 'overlay';
}

/**
 * Retrieves or creates the observers map entry for the given .NET reference.
 * @param {object} dotNetRef - Reference to the .NET component instance.
 * @returns {object} An object containing observersByDotNetObjectId and id.
 */
function getObserversMapEntry(dotNetRef) {
    const dotNetRefDispatcher = dotNetRef['_callDispatcher'];
    const dotNetRefId = dotNetRef['_id'];
    dotNetRefDispatcher[dispatcherObserversByDotNetIdPropname] ??= {};

    return {
        observersByDotNetObjectId: dotNetRefDispatcher[dispatcherObserversByDotNetIdPropname],
        id: dotNetRefId,
    };
}

/**
 * Disposes the observers and cleans up resources for the given .NET reference.
 * @param {object} dotNetRef - Reference to the .NET component instance.
 */
function dispose(dotNetRef) {
    const { observersByDotNetObjectId, id } = getObserversMapEntry(dotNetRef);
    const observers = observersByDotNetObjectId[id];

    if (observers) {
        observers.intersectionObserver.disconnect();
        observers.mutationObserverBefore.disconnect();
        observers.mutationObserverAfter.disconnect();

        dotNetRef.dispose();

        delete observersByDotNetObjectId[id];
    }
}

/**
 * Updates the render state with the latest item counts.
 * @param {object} dotNetRef - Reference to the .NET component instance.
 * @param {number} itemCount - The total number of items.
 * @param {number} lastRenderedItemCount - The number of items last rendered.
 * @param {number} lastRenderedPlaceholderCount - The number of placeholders last rendered.
 */
export function updateRenderState(dotNetRef, itemCount, lastRenderedItemCount, lastRenderedPlaceholderCount) {
    const { observersByDotNetObjectId, id } = getObserversMapEntry(dotNetRef);
    const observers = observersByDotNetObjectId[id];

    if (!observers?.state) {
        return;
    }

    observers.state.itemCount = Math.max(0, itemCount ?? 0);
    observers.state.lastRenderedItemCount = Math.max(0, lastRenderedItemCount ?? 0);
    observers.state.lastRenderedPlaceholderCount = Math.max(0, lastRenderedPlaceholderCount ?? 0);
}

/**
 * Lifecycle hook called when the component is loaded. Currently no-op.
 * @param {HTMLElement} element - The component element.
 * @param {object} dotNetRef - Reference to the .NET component instance.
 */
export function onLoad(element, dotNetRef) {
}

/**
 * Lifecycle hook called when the component is updated. Currently no-op.
 * @param {HTMLElement} element - The component element.
 * @param {object} dotNetRef - Reference to the .NET component instance.
 */
export function onUpdate(element, dotNetRef) {
}

/**
 * Lifecycle hook called when the component is disposed. Cleans up observers.
 * @param {HTMLElement} element - The component element.
 * @param {object} dotNetRef - Reference to the .NET component instance.
 */
export function onDispose(element, dotNetRef) {
    dispose(dotNetRef);
}
