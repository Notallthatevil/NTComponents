type Maybe<T> = T | null | undefined;

function getInput(element: Maybe<Element>): HTMLInputElement | null {
    if (element instanceof HTMLInputElement && element.dataset.ntBrowserTimeZone === 'true') {
        return element;
    }

    const input = element?.previousElementSibling;
    return input instanceof HTMLInputElement && input.dataset.ntBrowserTimeZone === 'true' ? input : null;
}

function resolveTimeZoneId(): string | null {
    try {
        const timeZoneId = Intl.DateTimeFormat().resolvedOptions().timeZone;
        const normalizedTimeZoneId = typeof timeZoneId === 'string' ? timeZoneId.trim() : '';
        return normalizedTimeZoneId || null;
    }
    catch {
        return null;
    }
}

function synchronizeInput(input: Maybe<HTMLInputElement>, notifyWhenUnchanged = false): void {
    if (!input?.isConnected) {
        return;
    }

    const timeZoneId = resolveTimeZoneId() ?? input.value;
    if (!timeZoneId) {
        return;
    }

    if (input.value !== timeZoneId) {
        input.value = timeZoneId;
        input.dispatchEvent(new Event('change', { bubbles: true }));
    }
    else if (notifyWhenUnchanged) {
        input.dispatchEvent(new Event('change', { bubbles: true }));
    }
}

export function onLoad(element: Maybe<Element>): void {
    synchronizeInput(getInput(element), element instanceof HTMLInputElement);
}

export function onUpdate(element: Maybe<Element>): void {
    synchronizeInput(getInput(element));
}

export function onDispose(): void {
}
