const observerMap = new Map();

function intersectCallback(entries, ob) {
    entries.forEach(entry => {
        if (entry.isIntersecting) {
            const value = observerMap.get(entry.target);
            if (value && value.dotNetRef) {
                value.dotNetRef.invokeMethodAsync("BeginLoad");
            }
            ob.unobserve(entry.target);
        }
    });
}

export function onLoad(element, dotNetRef) {
    if (element && dotNetRef) {
        const options = {
            rootMargin: "32px",
            threshold: 1.0,
        };

        if (!observerMap.get(element)) {
            const observer = new IntersectionObserver(intersectCallback, options);
            observerMap.set(element, {
                observer: observer,
                dotNetRef: dotNetRef
            });

            observer.observe(element);
        }
    }
}

export function onUpdate(element, dotNetRef) {
}

export function onDispose(element, dotNetRef) {
    if (element) {
        const observerValue = observerMap.get(element);
        if (observerValue) {
            observerValue.observer.disconnect();
            observerMap.delete(element);
        }
    }
}
