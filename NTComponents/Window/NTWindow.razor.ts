type WindowTarget = HTMLElement | string | null | undefined;
type WindowVisualState = 'normal' | 'minimized' | 'fullscreen';

interface NTWindowPosition {
    inset: string;
    left: string;
    top: string;
    translate: string;
}

interface NTWindowRuntimeState {
    abortController: AbortController;
    closeTimeout: number | null;
    removeDragListeners: (() => void) | null;
    restorePosition: NTWindowPosition | null;
}

interface NTWindowElement extends HTMLElement {
    __ntWindowRuntimeState?: NTWindowRuntimeState;
}

interface NTWindowPageScriptElement extends HTMLElement {
    __ntWindowElement?: NTWindowElement;
}

interface NTWindowBrowserBridge {
    closeWindow: (target: WindowTarget) => boolean;
    openWindow: (target: WindowTarget) => boolean;
    setWindowState: (target: WindowTarget, state: WindowVisualState) => boolean;
}

declare global {
    interface Window {
        NTWindow: NTWindowBrowserBridge;
    }
}

const closeAnimationDuration = 200;
const dockedWindowGap = 8;
const minimizedWindowHeight = 48;
const openTriggerSelector = '[data-nt-window-open]';
const windowSelector = '[data-nt-window="true"]';
let topWindowZIndex = 1100;

function getWindows(root?: Element | null): NTWindowElement[] {
    if (root instanceof HTMLElement && root.localName === 'tnt-page-script') {
        const pageScript = root as NTWindowPageScriptElement;
        const sibling = root.previousElementSibling;
        if (sibling instanceof HTMLElement && sibling.matches(windowSelector)) {
            pageScript.__ntWindowElement = sibling as NTWindowElement;
        }

        return pageScript.__ntWindowElement ? [pageScript.__ntWindowElement] : [];
    }

    if (root instanceof HTMLElement && root.matches(windowSelector)) {
        return [root as NTWindowElement];
    }

    const scope = root ?? document;
    return Array.from(scope.querySelectorAll<NTWindowElement>(windowSelector));
}

function resolveWindow(target: WindowTarget): NTWindowElement | null {
    if (target instanceof HTMLElement) {
        return target.matches(windowSelector)
            ? target as NTWindowElement
            : target.closest<NTWindowElement>(windowSelector);
    }

    if (typeof target !== 'string' || target.length === 0) {
        return null;
    }

    const element = document.getElementById(target);
    return element instanceof HTMLElement && element.matches(windowSelector)
        ? element as NTWindowElement
        : null;
}

function getVisualState(element: NTWindowElement): WindowVisualState {
    const state = element.dataset.ntWindowState;
    return state === 'minimized' || state === 'fullscreen' ? state : 'normal';
}

function getRuntimeState(element: NTWindowElement): NTWindowRuntimeState {
    if (element.__ntWindowRuntimeState) {
        return element.__ntWindowRuntimeState;
    }

    const state: NTWindowRuntimeState = {
        abortController: new AbortController(),
        closeTimeout: null,
        removeDragListeners: null,
        restorePosition: null
    };
    element.__ntWindowRuntimeState = state;
    return state;
}

function getTitle(element: NTWindowElement): string {
    return element.querySelector<HTMLElement>('.nt-window-title')?.textContent?.trim() || 'window';
}

function captureConfiguredControlLabels(element: NTWindowElement, state: WindowVisualState): void {
    const title = getTitle(element);
    const labels = new Map([
        ['minimize', state === 'minimized' ? `Restore ${title}` : `Minimize ${title}`],
        ['fullscreen', state === 'fullscreen' ? `Restore ${title}` : `Show ${title} fullscreen`]
    ]);

    for (const [action, defaultLabel] of labels) {
        const button = element.querySelector<HTMLButtonElement>(`[data-nt-window-action="${action}"]`);
        if (!button || button.hasAttribute('data-nt-window-aria-label')) {
            continue;
        }

        const label = button.getAttribute('aria-label');
        if (label !== null && label !== defaultLabel) {
            button.setAttribute('data-nt-window-aria-label', label);
        }
    }
}

function updateControlState(element: NTWindowElement, state: WindowVisualState): void {
    const title = getTitle(element);
    const minimizeButton = element.querySelector<HTMLButtonElement>('[data-nt-window-action="minimize"]');
    const fullscreenButton = element.querySelector<HTMLButtonElement>('[data-nt-window-action="fullscreen"]');
    const contentFrame = element.querySelector<HTMLElement>('.nt-window-content-frame');

    if (minimizeButton) {
        const minimized = state === 'minimized';
        minimizeButton.setAttribute('aria-expanded', String(!minimized));
        minimizeButton.setAttribute('aria-label', minimizeButton.getAttribute('data-nt-window-aria-label') ?? (minimized ? `Restore ${title}` : `Minimize ${title}`));
        const icon = minimizeButton.querySelector<HTMLElement>('.tnt-icon');
        if (icon) {
            icon.textContent = minimized ? 'keyboard_arrow_up' : 'minimize';
        }
    }

    if (fullscreenButton) {
        const fullscreen = state === 'fullscreen';
        fullscreenButton.setAttribute('aria-pressed', String(fullscreen));
        fullscreenButton.setAttribute('aria-label', fullscreenButton.getAttribute('data-nt-window-aria-label') ?? (fullscreen ? `Restore ${title}` : `Show ${title} fullscreen`));
        const icon = fullscreenButton.querySelector<HTMLElement>('.tnt-icon');
        if (icon) {
            icon.textContent = fullscreen ? 'fullscreen_exit' : 'fullscreen';
        }
    }

    contentFrame?.setAttribute('aria-hidden', String(state === 'minimized'));
}

function savePosition(element: NTWindowElement, runtimeState: NTWindowRuntimeState): void {
    runtimeState.restorePosition = {
        inset: element.style.inset,
        left: element.style.left,
        top: element.style.top,
        translate: element.style.translate
    };
}

function restorePosition(element: NTWindowElement, runtimeState: NTWindowRuntimeState): void {
    if (!runtimeState.restorePosition) {
        return;
    }

    element.style.inset = runtimeState.restorePosition.inset;
    element.style.left = runtimeState.restorePosition.left;
    element.style.top = runtimeState.restorePosition.top;
    element.style.translate = runtimeState.restorePosition.translate;
    runtimeState.restorePosition = null;
}

function clearPosition(element: NTWindowElement): void {
    element.style.inset = '';
    element.style.left = '';
    element.style.top = '';
    element.style.translate = '';
}

function layoutDockedWindows(excludedElement?: NTWindowElement): void {
    const indexes = new Map<string, number>();
    for (const element of getWindows()) {
        element.style.removeProperty('--nt-window-dock-offset');
        if (element === excludedElement || element.hidden || getVisualState(element) !== 'minimized') {
            continue;
        }

        const position = element.dataset.ntWindowDockPosition || 'bottom-right';
        const index = indexes.get(position) ?? 0;
        element.style.setProperty('--nt-window-dock-offset', `${index * (minimizedWindowHeight + dockedWindowGap)}px`);
        indexes.set(position, index + 1);
    }
}

function setState(element: NTWindowElement, state: WindowVisualState): void {
    const runtimeState = getRuntimeState(element);
    const previousState = getVisualState(element);

    if (previousState === 'normal' && state !== 'normal') {
        savePosition(element, runtimeState);
    }

    if (state === 'normal' && previousState !== 'normal') {
        restorePosition(element, runtimeState);
    } else if (state !== 'normal') {
        clearPosition(element);
    }

    element.dataset.ntWindowState = state;
    element.classList.toggle('nt-window-minimized', state === 'minimized');
    element.classList.toggle('nt-window-fullscreen', state === 'fullscreen');
    updateControlState(element, state);
    layoutDockedWindows();
    bringToFront(element);
    element.dispatchEvent(new CustomEvent('ntwindowstatechange', { detail: { state } }));
}

function syncTopWindowZIndex(elements: NTWindowElement[]): void {
    for (const element of elements) {
        const zIndex = Number.parseInt(element.style.zIndex, 10);
        if (Number.isFinite(zIndex)) {
            topWindowZIndex = Math.max(topWindowZIndex, zIndex);
        }
    }
}

function bringToFront(element: NTWindowElement): void {
    topWindowZIndex += 1;
    element.style.zIndex = String(topWindowZIndex);
}

function openWindow(target: WindowTarget): boolean {
    const element = resolveWindow(target);
    if (!element) {
        return false;
    }

    syncTopWindowZIndex(getWindows());
    initializeWindow(element);
    const runtimeState = getRuntimeState(element);
    if (runtimeState.closeTimeout !== null) {
        window.clearTimeout(runtimeState.closeTimeout);
        runtimeState.closeTimeout = null;
    }

    element.hidden = false;
    element.classList.remove('nt-window-closing');
    bringToFront(element);
    layoutDockedWindows();
    element.focus({ preventScroll: true });
    return true;
}

function closeWindow(target: WindowTarget): boolean {
    const element = resolveWindow(target);
    if (!element || element.hidden) {
        return false;
    }

    const runtimeState = getRuntimeState(element);
    if (runtimeState.closeTimeout !== null) {
        return false;
    }

    element.classList.add('nt-window-closing');
    runtimeState.closeTimeout = window.setTimeout(() => {
        element.hidden = true;
        element.classList.remove('nt-window-closing');
        runtimeState.closeTimeout = null;
        layoutDockedWindows();
        element.dispatchEvent(new CustomEvent('ntwindowclose'));
    }, closeAnimationDuration);
    return true;
}

function setWindowState(target: WindowTarget, state: WindowVisualState): boolean {
    const element = resolveWindow(target);
    if (!element || (state !== 'normal' && state !== 'minimized' && state !== 'fullscreen')) {
        return false;
    }

    setState(element, state);
    return true;
}

function clamp(value: number, minimum: number, maximum: number): number {
    return Math.min(Math.max(value, minimum), maximum);
}

function beginDrag(element: NTWindowElement, event: PointerEvent): void {
    if (event.button !== 0
        || element.dataset.ntWindowDraggable !== 'true'
        || getVisualState(element) !== 'normal'
        || (event.target instanceof Element && event.target.closest('[data-nt-window-action]'))) {
        return;
    }

    event.preventDefault();
    const runtimeState = getRuntimeState(element);
    runtimeState.removeDragListeners?.();

    const bounds = element.getBoundingClientRect();
    const startX = event.clientX;
    const startY = event.clientY;
    const startLeft = bounds.left;
    const startTop = bounds.top;

    element.style.inset = 'auto';
    element.style.left = `${startLeft}px`;
    element.style.top = `${startTop}px`;
    element.style.translate = 'none';
    element.dataset.ntWindowDragging = 'true';

    const move = (moveEvent: PointerEvent): void => {
        const viewportWidth = window.visualViewport?.width ?? window.innerWidth;
        const viewportHeight = window.visualViewport?.height ?? window.innerHeight;
        const nextLeft = clamp(startLeft + moveEvent.clientX - startX, 0, Math.max(0, viewportWidth - bounds.width));
        const nextTop = clamp(startTop + moveEvent.clientY - startY, 0, Math.max(0, viewportHeight - bounds.height));
        element.style.left = `${nextLeft}px`;
        element.style.top = `${nextTop}px`;
    };

    const finish = (): void => {
        window.removeEventListener('pointermove', move);
        window.removeEventListener('pointerup', finish);
        window.removeEventListener('pointercancel', finish);
        delete element.dataset.ntWindowDragging;
        runtimeState.removeDragListeners = null;
    };

    runtimeState.removeDragListeners = finish;
    window.addEventListener('pointermove', move);
    window.addEventListener('pointerup', finish, { once: true });
    window.addEventListener('pointercancel', finish, { once: true });
}

function handleClick(element: NTWindowElement, event: MouseEvent): void {
    const action = event.target instanceof Element
        ? event.target.closest<HTMLElement>('[data-nt-window-action]')?.dataset.ntWindowAction
        : null;

    if (action === 'minimize') {
        setState(element, getVisualState(element) === 'minimized' ? 'normal' : 'minimized');
    } else if (action === 'fullscreen') {
        setState(element, getVisualState(element) === 'fullscreen' ? 'normal' : 'fullscreen');
    } else if (action === 'close') {
        closeWindow(element);
    }
}

function initializeWindow(element: NTWindowElement): boolean {
    const visualState = getVisualState(element);
    captureConfiguredControlLabels(element, visualState);
    if (element.__ntWindowRuntimeState) {
        updateControlState(element, visualState);
        return false;
    }

    const runtimeState = getRuntimeState(element);
    const signal = runtimeState.abortController.signal;
    element.addEventListener('click', event => handleClick(element, event), { signal });
    element.addEventListener('pointerdown', () => bringToFront(element), { signal });
    element.querySelector<HTMLElement>('[data-nt-window-drag-handle="true"]')
        ?.addEventListener('pointerdown', event => beginDrag(element, event), { signal });
    updateControlState(element, visualState);
    return true;
}

function disposeWindow(element: NTWindowElement): void {
    const runtimeState = element.__ntWindowRuntimeState;
    if (!runtimeState) {
        return;
    }

    runtimeState.abortController.abort();
    runtimeState.removeDragListeners?.();
    if (runtimeState.closeTimeout !== null) {
        window.clearTimeout(runtimeState.closeTimeout);
    }

    delete element.__ntWindowRuntimeState;
    layoutDockedWindows(element);
}

export function onLoad(element: Element): void {
    const windows = getWindows(element);
    syncTopWindowZIndex(windows);
    for (const windowElement of windows) {
        if (initializeWindow(windowElement)) {
            bringToFront(windowElement);
        }
    }
    layoutDockedWindows();
}

export function onUpdate(element: Element): void {
    onLoad(element);
}

export function onDispose(element: Element): void {
    for (const windowElement of getWindows(element)) {
        disposeWindow(windowElement);
    }

    if (element instanceof HTMLElement && element.localName === 'tnt-page-script') {
        delete (element as NTWindowPageScriptElement).__ntWindowElement;
    }
}

window.NTWindow = {
    closeWindow,
    openWindow,
    setWindowState
};

document.addEventListener('click', event => {
    const trigger = event.target instanceof Element
        ? event.target.closest<HTMLElement>(openTriggerSelector)
        : null;
    const target = trigger?.dataset.ntWindowOpen;
    if (target) {
        openWindow(target);
    }
});
