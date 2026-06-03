interface NTAnimationState {
    hasEntered: boolean;
    observer?: IntersectionObserver;
}

interface NTAnimationElement extends HTMLElement {
    __ntAnimationState?: NTAnimationState;
}

const observedAnimations = new Set<NTAnimationElement>();

function getAnimationElements(root: ParentNode = document): NTAnimationElement[] {
    return Array.from(root.querySelectorAll<NTAnimationElement>('[data-nt-animation="true"]'));
}

function parseBoolean(value: string | undefined, fallback: boolean): boolean {
    if (value == null) {
        return fallback;
    }

    return value.toLowerCase() === 'true';
}

function parseThreshold(value: string | undefined): number {
    const parsed = Number.parseFloat(value ?? '');
    return Number.isFinite(parsed) ? Math.min(Math.max(parsed, 0), 1) : 0.35;
}

function isLikelyInViewport(element: Element): boolean {
    const rect = element.getBoundingClientRect();
    const viewportHeight = window.innerHeight || document.documentElement.clientHeight;
    const viewportWidth = window.innerWidth || document.documentElement.clientWidth;
    return rect.bottom >= 0 && rect.right >= 0 && rect.top <= viewportHeight && rect.left <= viewportWidth;
}

function reveal(element: NTAnimationElement): void {
    element.classList.remove('nt-animation-exiting');
    element.classList.add('nt-animation-visible');
    element.__ntAnimationState!.hasEntered = true;
}

function hide(element: NTAnimationElement): void {
    element.classList.add('nt-animation-exiting');
    element.classList.remove('nt-animation-visible');
}

function cleanupAnimation(element: NTAnimationElement): void {
    element.__ntAnimationState?.observer?.disconnect();
    delete element.__ntAnimationState;
    observedAnimations.delete(element);
}

function synchronizeAnimation(element: NTAnimationElement): void {
    if (!element.isConnected) {
        cleanupAnimation(element);
        return;
    }

    const animateOut = parseBoolean(element.dataset.ntAnimationAnimateOut, false);
    const once = parseBoolean(element.dataset.ntAnimationOnce, true);
    const threshold = parseThreshold(element.dataset.ntAnimationThreshold);
    const rootMargin = element.dataset.ntAnimationRootMargin || '0px';

    cleanupAnimation(element);

    const state: NTAnimationState = {
        hasEntered: element.classList.contains('nt-animation-visible')
    };

    element.__ntAnimationState = state;
    observedAnimations.add(element);

    if (isLikelyInViewport(element)) {
        reveal(element);
    }

    element.classList.add('nt-animation-enhanced');

    if (!('IntersectionObserver' in window)) {
        reveal(element);
        return;
    }

    state.observer = new IntersectionObserver(entries => {
        for (const entry of entries) {
            const target = entry.target as NTAnimationElement;
            const targetState = target.__ntAnimationState;

            if (entry.isIntersecting) {
                reveal(target);
                if (once && !animateOut) {
                    targetState?.observer?.unobserve(target);
                }

                continue;
            }

            if (animateOut && targetState?.hasEntered) {
                hide(target);
            }
        }
    }, {
        root: null,
        rootMargin,
        threshold
    });

    state.observer.observe(element);
}

function cleanupDisconnectedAnimations(): void {
    for (const element of observedAnimations) {
        if (!element.isConnected) {
            cleanupAnimation(element);
        }
    }
}

export function enhanceAll(root: ParentNode = document): void {
    cleanupDisconnectedAnimations();
    for (const element of getAnimationElements(root)) {
        synchronizeAnimation(element);
    }
}

export function onDispose(element?: Element | null): void {
    if (element instanceof HTMLElement && element.dataset.ntAnimation === 'true') {
        cleanupAnimation(element as NTAnimationElement);
        return;
    }

    for (const animation of getAnimationElements(document)) {
        cleanupAnimation(animation);
    }
}

export function onLoad(element?: Element | null): void {
    onUpdate(element);
}

export function onUpdate(element?: Element | null): void {
    if (element instanceof HTMLElement && element.dataset.ntAnimation === 'true') {
        synchronizeAnimation(element as NTAnimationElement);
        return;
    }

    enhanceAll(document);
}
