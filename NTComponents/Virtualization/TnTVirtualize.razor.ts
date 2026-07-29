function findClosestScrollContainer(element) {
    while (element) {
        const style = getComputedStyle(element);
        if (style.overflowY !== 'visible') {
            return element;
        }
        element = element.parentElement;
    }
    return null;
}
function infiniteScollingDispose(observer) {
    observer.disconnect();
}
function isValidTableElement(element) {
    if (element === null) {
        return false;
    }
    return ((element instanceof HTMLTableElement && element.style.display === '') || element.style.display === 'table')
        || ((element instanceof HTMLTableSectionElement && element.style.display === '') || element.style.display === 'table-row-group');
}

export function onNewItems(element) {
    if (element && element.observer && element.observer.unobserve && element.observer.observe) {
        element.observer.unobserve(element);
        element.observer.observe(element);
    }
}

export function resetScrollPosition(element) {
    if (element) {
        const root = findClosestScrollContainer(element);
        if (root) {
            root.scrollTop = 0;
        }
    }
}

export function onLoad(element, dotNetRef) {
    const options = {
        root: findClosestScrollContainer(element),
        rootMargin: '64px',
        threshold: 0
    };

    if (isValidTableElement(element.parentElement)) {
        element.style.display = 'table-row';
    }

    element.observer = new IntersectionObserver(async (entries) => {
        for (const entry of entries) {
            if (entry.isIntersecting) {
                element.observer.unobserve(element);
                await dotNetRef.invokeMethodAsync("LoadMoreItems");
            }
        }
    }, options);

    element.observer.observe(element);
    element.dispose = () => infiniteScollingDispose(element.observer);
}

export function onUpdate(element, dotNetRef) {
}

export function onDispose(element, dotNetRef) {
    if (element && element.dispose) {
        element.dispose();
    }
}
