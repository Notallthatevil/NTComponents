type NTAnimationRepeat = 'once' | 'reverse' | 'replay';
type NTAnimationVisualState = 'hidden' | 'visible' | 'reset';

interface DotNetReference {
    invokeMethodAsync(methodName: string): Promise<unknown>;
}

interface NTAnimationState {
    config: string;
    repeat: NTAnimationRepeat;
    threshold: number;
    rootMargin: string;
    observerKey?: string;
    ratio?: number;
    dotNetRef?: DotNetReference;
}

interface SharedObserver {
    observer: IntersectionObserver;
    count: number;
}

const selector = '.nt-animation[data-nt-animation-trigger="visible"]';
const animations = new Map<HTMLElement, NTAnimationState>();
const observers = new Map<string, SharedObserver>();

function readRepeat(value: string | undefined): NTAnimationRepeat {
    return value === 'reverse' || value === 'replay' ? value : 'once';
}

function readThreshold(value: string | undefined): number {
    const parsed = Number.parseFloat(value ?? '');
    return Number.isFinite(parsed) ? Math.min(Math.max(parsed, 0), 1) : 0.35;
}

// Content larger than the root can never reach a high intersection ratio, so the threshold is applied to the part of it that fits.
function requiredRatio(threshold: number, width: number, height: number, rootWidth: number, rootHeight: number): number {
    const fit = Math.min(1, rootWidth / width || 1) * Math.min(1, rootHeight / height || 1);
    return Math.floor(threshold * fit * 100 + 1e-9) / 100;
}

function isInViewport(element: Element): boolean {
    const rect = element.getBoundingClientRect();
    return rect.bottom >= 0 && rect.right >= 0 && rect.top <= window.innerHeight && rect.left <= window.innerWidth;
}

function notify(state: NTAnimationState | undefined, methodName: string): void {
    state?.dotNetRef?.invokeMethodAsync(methodName).catch(() => { });
}

function setVisualState(element: HTMLElement, next: NTAnimationVisualState): void {
    const previous = element.dataset.ntAnimationState;
    if (previous === next) {
        return;
    }

    element.dataset.ntAnimationState = next;
    if (next === 'visible') {
        notify(animations.get(element), 'NotifyEnteredAsync');
    }
    else if (previous === 'visible') {
        notify(animations.get(element), 'NotifyExitedAsync');
    }
}

function unobserve(element: HTMLElement, state: NTAnimationState): void {
    const key = state.observerKey;
    const shared = key ? observers.get(key) : undefined;
    state.observerKey = undefined;
    if (!key || !shared) {
        return;
    }

    shared.observer.unobserve(element);
    if (--shared.count === 0) {
        shared.observer.disconnect();
        observers.delete(key);
    }
}

function observe(element: HTMLElement, state: NTAnimationState, ratio: number): void {
    unobserve(element, state);
    const key = `${state.rootMargin}|${ratio}`;
    let shared = observers.get(key);
    if (!shared) {
        shared = {
            observer: new IntersectionObserver(onIntersection, { rootMargin: state.rootMargin, threshold: ratio > 0 ? [0, ratio] : 0 }),
            count: 0
        };
        observers.set(key, shared);
    }

    shared.observer.observe(element);
    shared.count++;
    state.observerKey = key;
    state.ratio = ratio;
}

function onIntersection(entries: IntersectionObserverEntry[]): void {
    for (const entry of entries) {
        const element = entry.target as HTMLElement;
        const state = animations.get(element);
        if (!state?.observerKey) {
            continue;
        }

        const root = entry.rootBounds;
        const ratio = root ? requiredRatio(state.threshold, entry.boundingClientRect.width, entry.boundingClientRect.height, root.width, root.height) : state.threshold;
        if (ratio !== state.ratio) {
            // The content or root changed size; observing again delivers a fresh entry for the new ratio.
            observe(element, state, ratio);
            continue;
        }

        if (entry.isIntersecting && entry.intersectionRatio >= ratio - 0.005) {
            setVisualState(element, 'visible');
            if (state.repeat === 'once') {
                unobserve(element, state);
            }
        }
        else if (element.dataset.ntAnimationState === 'visible') {
            if (state.repeat === 'reverse') {
                setVisualState(element, 'hidden');
            }
            else if (state.repeat === 'replay' && !entry.isIntersecting) {
                setVisualState(element, 'reset');
            }
        }
    }
}

function cleanup(element: HTMLElement): void {
    const state = animations.get(element);
    if (!state) {
        return;
    }

    unobserve(element, state);
    animations.delete(element);
    // Manual wrappers own their state attribute through Blazor rendering.
    if (element.dataset.ntAnimationTrigger !== 'manual') {
        delete element.dataset.ntAnimationState;
    }
}

function cleanupDisconnected(): void {
    for (const element of animations.keys()) {
        if (!element.isConnected) {
            cleanup(element);
        }
    }
}

function synchronize(element: HTMLElement, dotNetRef?: DotNetReference): void {
    if (!element.isConnected || !element.matches(selector)) {
        cleanup(element);
        return;
    }

    const repeat = readRepeat(element.dataset.ntAnimationRepeat);
    const threshold = readThreshold(element.dataset.ntAnimationThreshold);
    const rootMargin = element.dataset.ntAnimationRootMargin || '0px';
    const config = `${repeat}|${threshold}|${rootMargin}`;
    const existing = animations.get(element);
    const wantsNotify = element.hasAttribute('data-nt-animation-notify');
    const nextRef = wantsNotify ? dotNetRef ?? existing?.dotNetRef : undefined;

    if (existing?.config === config) {
        const attached = nextRef && !existing.dotNetRef;
        existing.dotNetRef = nextRef;
        if (attached && element.dataset.ntAnimationState === 'visible') {
            notify(existing, 'NotifyEnteredAsync');
        }
        return;
    }

    if (existing) {
        unobserve(element, existing);
    }

    const state: NTAnimationState = { config, repeat, threshold, rootMargin, dotNetRef: nextRef };
    animations.set(element, state);

    if (!element.dataset.ntAnimationState) {
        // Content the user can already see is shown as-is; animating it now would flash it out and back in.
        setVisualState(element, isInViewport(element) ? 'visible' : 'reset');
    }
    else if (nextRef && !existing?.dotNetRef && element.dataset.ntAnimationState === 'visible') {
        notify(state, 'NotifyEnteredAsync');
    }

    if (repeat === 'once' && element.dataset.ntAnimationState === 'visible') {
        return;
    }

    try {
        const rect = element.getBoundingClientRect();
        observe(element, state, requiredRatio(threshold, rect.width, rect.height, window.innerWidth, window.innerHeight));
    }
    catch {
        // An invalid rootMargin makes IntersectionObserver throw; never leave content hidden because of it.
        setVisualState(element, 'visible');
    }
}

function isAnimationElement(element: unknown): element is HTMLElement {
    return element instanceof HTMLElement && element.classList.contains('nt-animation');
}

export function enhanceAll(root: ParentNode = document): void {
    cleanupDisconnected();
    for (const element of root.querySelectorAll<HTMLElement>(selector)) {
        synchronize(element);
    }
}

export function onLoad(element?: Element | null, dotNetRef?: DotNetReference): void {
    onUpdate(element, dotNetRef);
}

export function onUpdate(element?: Element | null, dotNetRef?: DotNetReference): void {
    if (isAnimationElement(element)) {
        synchronize(element, dotNetRef);
        return;
    }

    enhanceAll(document);
}

export function onDispose(element?: Element | null): void {
    if (isAnimationElement(element)) {
        cleanup(element);
        return;
    }

    // Page-script elements are shared by every wrapper on the page, so only release wrappers that left the DOM.
    cleanupDisconnected();
}
