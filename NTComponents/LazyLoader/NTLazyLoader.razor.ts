interface ShowCallback {
    invokeMethodAsync(methodName: 'ShowAsync'): Promise<void>;
}

const observers = new Map<Element, IntersectionObserver>();

// Starts a little before the placeholder scrolls into view, so the content is usually ready by the time it appears.
const rootMargin = '400px';

function watch(placeholder: Element, show: () => void): void {
    onDispose(placeholder);

    // A placeholder inside a hidden tab or a closed details element has no visible box, so it intersects only once that content is shown.
    const observer = new IntersectionObserver(entries => {
        if (entries.some(entry => entry.isIntersecting)) {
            onDispose(placeholder);
            show();
        }
    }, { rootMargin });
    observers.set(placeholder, observer);
    observer.observe(placeholder);
}

// Static SSR ships the content inert inside a template, so it is not part of the page until the placeholder is visible.
function watchStaticPlaceholders(): void {
    for (const placeholder of document.querySelectorAll('[data-nt-lazy-loader]')) {
        const template = placeholder.querySelector(':scope > template');
        if (!(template instanceof HTMLTemplateElement)) {
            continue;
        }

        // Enhanced navigation keeps a matching template without updating its content, so take the content now and let each navigation insert a fresh template.
        template.remove();
        const content = template.content;
        watch(placeholder, () => placeholder.replaceWith(content));
    }
}

// Interactive renders pass the placeholder and a .NET reference; static SSR page scripts pass only their own element.
export function onLoad(placeholder: Element | null, dotNetReference?: ShowCallback): void {
    if (!dotNetReference) {
        watchStaticPlaceholders();
    }
    else if (placeholder instanceof Element) {
        watch(placeholder, () => void dotNetReference.invokeMethodAsync('ShowAsync'));
    }
}

export function onUpdate(_placeholder?: Element | null, dotNetReference?: ShowCallback): void {
    // An interactive placeholder never changes; static SSR pages may have new placeholders after enhanced navigation.
    if (!dotNetReference) {
        watchStaticPlaceholders();
    }
}

export function onDispose(placeholder?: Element | null): void {
    // Also drops placeholders that navigation removed, since static SSR pages never dispose them one by one.
    for (const [watched, observer] of observers) {
        if (watched === placeholder || !watched.isConnected) {
            observer.disconnect();
            observers.delete(watched);
        }
    }
}
