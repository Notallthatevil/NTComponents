let externalClickCallbacks = {};
let mouseDownInside = {};

export function externalClickCallbackRegister(element, dotNetObjectRef) {
    if (dotNetObjectRef) {
        const id = dotNetObjectRef._id;
        // If already registered for this id, deregister first to avoid duplicate handlers
        if (externalClickCallbacks[id]) {
            externalClickCallbacks[id]();
            delete externalClickCallbacks[id];
        }
        function getIsInside(clientX, clientY) {
            if (!element || !element.getBoundingClientRect) return false;
            const rect = element.getBoundingClientRect();
            return (
                clientX >= rect.left &&
                clientX <= rect.right &&
                clientY >= rect.top &&
                clientY <= rect.bottom
            );
        }
        function onMouseDown(event) {
            mouseDownInside[id] = getIsInside(event.clientX, event.clientY);
        }
        function onClick(event) {
            const isClickInside = getIsInside(event.clientX, event.clientY);

            // Trigger only if both the start (mousedown) and end (click) were outside
            if (!mouseDownInside[id] && !isClickInside) {
                dotNetObjectRef.invokeMethodAsync('OnClick');
            }

            // Clean up stored target state
            delete mouseDownInside[id];
        }
        externalClickCallbacks[id] = function () {
            window.removeEventListener('mousedown', onMouseDown);
            window.removeEventListener('click', onClick);
            delete mouseDownInside[id];
        };
        window.addEventListener('mousedown', onMouseDown);
        window.addEventListener('click', onClick);
    }
}

export function externalClickCallbackDeregister(dotNetObjectRef) {
    const id = dotNetObjectRef?._id;
    if (id && externalClickCallbacks[id]) {
        externalClickCallbacks[id]();
        delete externalClickCallbacks[id];
    }
}
export function onLoad(element, dotnNetRef) {
}

export function onUpdate(element, dotnNetRef) {
}

export function onDispose(element, dotnNetRef) {
}
