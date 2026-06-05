interface NTSupportingPaneRegistration {
    registered: true;
}

type NTSupportingPaneViewElement = HTMLElement & {
    __ntSupportingPaneRegistration?: NTSupportingPaneRegistration;
};

const viewSelector = '[data-nt-supporting-pane-view]';
const modeControlSelector = '[data-nt-supporting-pane-mode-control]';
const modeLabelSelector = '[data-nt-supporting-pane-mode-label]';
const stackedClass = 'nt-supporting-pane-view-stacked';
const hideOnSmallScreensClass = 'nt-supporting-pane-view-hide-on-small-screens';

let globalSyncScheduled = false;
let globalClickHandlerRegistered = false;
const registeredViews = new Set<NTSupportingPaneViewElement>();

function getViews(element?: Element | null): NTSupportingPaneViewElement[] {
    if (element instanceof HTMLElement) {
        const view = element.matches(viewSelector)
            ? element
            : element.closest<NTSupportingPaneViewElement>(viewSelector);

        if (view) {
            return [view as NTSupportingPaneViewElement];
        }

        return Array.from(element.querySelectorAll<NTSupportingPaneViewElement>(viewSelector));
    }

    return Array.from(document.querySelectorAll<NTSupportingPaneViewElement>(viewSelector));
}

function getControlTarget(control: HTMLElement): NTSupportingPaneViewElement | null {
    const targetId = control.getAttribute('data-nt-supporting-pane-target');

    if (targetId) {
        const target = document.getElementById(targetId);
        return target?.matches(viewSelector) ? target as NTSupportingPaneViewElement : null;
    }

    return control.closest('section, article, main, body')?.querySelector<NTSupportingPaneViewElement>(viewSelector) ?? null;
}

function normalizeMode(mode: string | null | undefined): string {
    switch ((mode ?? '').toLowerCase()) {
        case 'stacked':
            return 'Stacked';
        case 'hideonsmallscreens':
            return 'HideOnSmallScreens';
        default:
            return 'Auto';
    }
}

function setMode(view: HTMLElement, mode: string | null | undefined): void {
    const normalizedMode = normalizeMode(mode);

    view.dataset.ntSupportingPaneMode = normalizedMode;
    view.classList.toggle(stackedClass, normalizedMode === 'Stacked');
    view.classList.toggle(hideOnSmallScreensClass, normalizedMode === 'HideOnSmallScreens');
}

function updateControls(view: HTMLElement): void {
    const mode = normalizeMode(view.dataset.ntSupportingPaneMode);
    const owner = view.closest('section, article, main, body') ?? document;

    owner.querySelectorAll<HTMLElement>(modeControlSelector).forEach(control => {
        const targetId = control.getAttribute('data-nt-supporting-pane-target');

        if (targetId ? targetId !== view.id : getControlTarget(control) !== view) {
            return;
        }

        const controlMode = control.getAttribute('data-nt-supporting-pane-mode-control');

        const active = normalizeMode(controlMode) === mode;
        control.classList.toggle('demo-active', active);
        control.dataset.ntSupportingPaneActive = active ? 'true' : 'false';

        const ariaPressed = active ? 'true' : 'false';
        if (control.getAttribute('aria-pressed') !== ariaPressed) {
            control.setAttribute('aria-pressed', ariaPressed);
        }
    });

    owner.querySelectorAll<HTMLElement>(modeLabelSelector).forEach(label => {
        const targetId = label.getAttribute('data-nt-supporting-pane-target');

        if (!targetId || targetId === view.id) {
            if (label.textContent !== mode) {
                label.textContent = mode;
            }
        }
    });
}

function applyState(view: HTMLElement, mode: string): void {
    setMode(view, mode);
    updateControls(view);

    view.dispatchEvent(new CustomEvent('nt-supporting-pane-change', {
        bubbles: true,
        detail: {
            mode: normalizeMode(mode)
        }
    }));
}

function shouldLetBrowserHandleClick(event: MouseEvent): boolean {
    return event.defaultPrevented
        || event.button !== 0
        || event.altKey
        || event.ctrlKey
        || event.metaKey
        || event.shiftKey;
}

function handleDocumentClick(event: MouseEvent): void {
    if (shouldLetBrowserHandleClick(event)) {
        return;
    }

    const target = event.target instanceof Element ? event.target : null;
    const control = target?.closest<HTMLElement>(modeControlSelector);

    if (!control) {
        return;
    }

    const view = getControlTarget(control);

    if (!view || !registeredViews.has(view)) {
        return;
    }

    event.preventDefault();
    applyState(view, normalizeMode(control.getAttribute('data-nt-supporting-pane-mode-control')));
}

function ensureGlobalClickHandler(): void {
    if (globalClickHandlerRegistered) {
        return;
    }

    document.addEventListener('click', handleDocumentClick);
    globalClickHandlerRegistered = true;
}

function removeGlobalClickHandlerIfUnused(): void {
    if (!globalClickHandlerRegistered || registeredViews.size > 0) {
        return;
    }

    document.removeEventListener('click', handleDocumentClick);
    globalClickHandlerRegistered = false;
}

function pruneDisconnectedViews(): void {
    registeredViews.forEach(view => {
        if (!view.isConnected) {
            delete view.__ntSupportingPaneRegistration;
            registeredViews.delete(view);
        }
    });

    removeGlobalClickHandlerIfUnused();
}

function enhanceView(view: NTSupportingPaneViewElement): void {
    if (view.__ntSupportingPaneRegistration) {
        setMode(view, view.dataset.ntSupportingPaneMode);
        updateControls(view);
        return;
    }

    setMode(view, view.dataset.ntSupportingPaneMode);
    registeredViews.add(view);
    ensureGlobalClickHandler();
    view.__ntSupportingPaneRegistration = {
        registered: true
    };

    updateControls(view);
}

function disposeView(view: NTSupportingPaneViewElement): void {
    const registration = view.__ntSupportingPaneRegistration;

    if (!registration) {
        return;
    }

    registeredViews.delete(view);
    removeGlobalClickHandlerIfUnused();
    delete view.__ntSupportingPaneRegistration;
}

function sync(element?: Element | null): void {
    pruneDisconnectedViews();
    getViews(element).forEach(enhanceView);
}

function scheduleGlobalSync(): void {
    if (globalSyncScheduled) {
        return;
    }

    globalSyncScheduled = true;

    queueMicrotask(() => {
        globalSyncScheduled = false;
        sync();
    });
}

export function onLoad(element?: Element | null): void {
    sync(element);
}

export function onUpdate(element?: Element | null): void {
    if (element == null) {
        scheduleGlobalSync();
        return;
    }

    sync(element);
}

export function onDispose(element?: Element | null): void {
    getViews(element).forEach(disposeView);
    pruneDisconnectedViews();
}
