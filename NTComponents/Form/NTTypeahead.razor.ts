interface TypeaheadState {
    documentListenersAttached: boolean;
    input: HTMLInputElement;
    menu: HTMLElement | null;
    onDocumentScroll: (event: Event) => void;
    onKeyDown: (event: KeyboardEvent) => void;
    onWindowResize: (event: UIEvent) => void;
    root: HTMLElement;
}

const states = new Map<HTMLElement, TypeaheadState>();
const menuLayerClass = 'nt-combobox-menu-layer';
const menuViewportMargin = 8;
const menuViewportOffset = 4;
const menuPreferredMaxHeight = 320;

export function onLoad(root: HTMLElement): void {
    cleanupDisconnectedStates();
    onDispose(root);

    const input = root instanceof HTMLInputElement
        ? root
        : root.querySelector<HTMLInputElement>('[data-nt-typeahead-input="true"]');

    if (!input) {
        return;
    }

    const state: TypeaheadState = {
        documentListenersAttached: false,
        input,
        menu: null,
        onDocumentScroll: () => { },
        onKeyDown: () => { },
        onWindowResize: () => { },
        root: queryRoot(input),
    };

    state.onKeyDown = (event: KeyboardEvent): void => {
        if (event.key === 'Enter' && input.getAttribute('aria-expanded') === 'true') {
            event.preventDefault();
        }
    };

    state.onDocumentScroll = () => {
        updateMenuPlacement(state);
    };

    state.onWindowResize = () => {
        updateMenuPlacement(state);
    };

    input.addEventListener('keydown', state.onKeyDown);
    states.set(root, state);
    updateElements(state);
}

export function onUpdate(root: HTMLElement): void {
    cleanupDisconnectedStates();

    const state = states.get(root);
    if (!state) {
        onLoad(root);
        return;
    }

    updateElements(state);
}

export function onDispose(root: HTMLElement): void {
    const state = states.get(root);
    if (!state) {
        cleanupDisconnectedStates();
        return;
    }

    state.input.removeEventListener('keydown', state.onKeyDown);
    detachDocumentListeners(state);
    hideMenuSurface(state);
    states.delete(root);
    cleanupDisconnectedStates();
}

export function scrollActiveOptionIntoView(root: HTMLElement, activeDescendantId: string): void {
    if (!activeDescendantId) {
        return;
    }

    const componentRoot = queryRoot(root);
    const option = componentRoot.querySelector<HTMLElement>(`#${escapeCssIdentifier(activeDescendantId)}`);
    option?.scrollIntoView({ block: 'nearest' });
}

function updateElements(state: TypeaheadState): void {
    state.root = queryRoot(state.input);
    state.menu = state.root.querySelector<HTMLElement>('[data-nt-typeahead-menu="true"]');
    const shouldBeOpen = state.input.getAttribute('aria-expanded') === 'true' && state.menu?.hidden === false;
    if (shouldBeOpen) {
        showMenuSurface(state);
        updateMenuPlacement(state);
        attachDocumentListeners(state);
    }
    else {
        detachDocumentListeners(state);
        hideMenuSurface(state);
    }
}

function queryRoot(element: HTMLElement): HTMLElement {
    return element.closest<HTMLElement>('.nt-typeahead') ?? element;
}

function updateMenuPlacement(state: TypeaheadState): void {
    if (!state.menu || state.menu.hidden) {
        return;
    }

    const rootRect = state.root.getBoundingClientRect();
    const viewportWidth = window.innerWidth || document.documentElement.clientWidth;
    const viewportHeight = window.innerHeight || document.documentElement.clientHeight;
    const maxViewportWidth = Math.max(0, viewportWidth - menuViewportMargin * 2);
    const width = Math.min(rootRect.width, maxViewportWidth);
    const left = Math.min(Math.max(rootRect.left, menuViewportMargin), Math.max(menuViewportMargin, viewportWidth - width - menuViewportMargin));
    const spaceBelow = Math.max(0, viewportHeight - rootRect.bottom - menuViewportMargin - menuViewportOffset);
    const spaceAbove = Math.max(0, rootRect.top - menuViewportMargin - menuViewportOffset);
    const openAbove = spaceBelow < menuPreferredMaxHeight && spaceAbove > spaceBelow;
    const availableSpace = openAbove ? spaceAbove : spaceBelow;
    const maxHeight = Math.min(menuPreferredMaxHeight, availableSpace);
    const height = Math.min(state.menu.scrollHeight || maxHeight, maxHeight);
    const top = openAbove ? rootRect.top - menuViewportOffset - height : rootRect.bottom + menuViewportOffset;

    state.menu.classList.toggle('nt-combobox-menu-above', openAbove);
    state.menu.classList.add(menuLayerClass);
    state.menu.style.left = `${left}px`;
    state.menu.style.maxHeight = `${maxHeight}px`;
    state.menu.style.top = `${Math.max(menuViewportMargin, top)}px`;
    state.menu.style.width = `${width}px`;
}

function showMenuSurface(state: TypeaheadState): void {
    if (!state.menu) {
        return;
    }

    if (typeof state.menu.showPopover === 'function' && !isMenuPopoverOpen(state.menu)) {
        try {
            state.menu.showPopover();
        }
        catch {
            // Already-open or unsupported popover transitions should not block the fallback positioned menu.
        }
    }
}

function hideMenuSurface(state: TypeaheadState): void {
    if (!state.menu) {
        return;
    }

    if (typeof state.menu.hidePopover === 'function' && isMenuPopoverOpen(state.menu)) {
        try {
            state.menu.hidePopover();
        }
        catch {
            // Best-effort cleanup only.
        }
    }

    state.menu.classList.remove(menuLayerClass, 'nt-combobox-menu-above');
    state.menu.style.removeProperty('left');
    state.menu.style.removeProperty('max-height');
    state.menu.style.removeProperty('top');
    state.menu.style.removeProperty('width');
}

function attachDocumentListeners(state: TypeaheadState): void {
    if (state.documentListenersAttached) {
        return;
    }

    document.addEventListener('scroll', state.onDocumentScroll, true);
    window.addEventListener('resize', state.onWindowResize);
    state.documentListenersAttached = true;
}

function detachDocumentListeners(state: TypeaheadState): void {
    if (!state.documentListenersAttached) {
        return;
    }

    document.removeEventListener('scroll', state.onDocumentScroll, true);
    window.removeEventListener('resize', state.onWindowResize);
    state.documentListenersAttached = false;
}

function cleanupDisconnectedStates(): void {
    for (const [root, state] of states) {
        if (!state.input.isConnected) {
            state.input.removeEventListener('keydown', state.onKeyDown);
            detachDocumentListeners(state);
            hideMenuSurface(state);
            states.delete(root);
        }
    }
}

function isMenuPopoverOpen(menu: HTMLElement): boolean {
    try {
        return menu.matches(':popover-open');
    }
    catch {
        return false;
    }
}

function escapeCssIdentifier(value: string): string {
    return globalThis.CSS?.escape?.(value)
        ?? value.replace(/[^a-zA-Z0-9_-]/g, character => `\\${character}`);
}
