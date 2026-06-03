interface TypeaheadState {
    input: HTMLInputElement;
    onKeyDown: (event: KeyboardEvent) => void;
}

const states = new WeakMap<HTMLElement, TypeaheadState>();

export function onLoad(root: HTMLElement): void {
    onDispose(root);

    const input = root instanceof HTMLInputElement
        ? root
        : root.querySelector<HTMLInputElement>('[data-nt-typeahead-input="true"]');

    if (!input) {
        return;
    }

    const onKeyDown = (event: KeyboardEvent): void => {
        if (event.key === 'Enter' && input.getAttribute('aria-expanded') === 'true') {
            event.preventDefault();
        }
    };

    input.addEventListener('keydown', onKeyDown);
    states.set(root, { input, onKeyDown });
}

export function onDispose(root: HTMLElement): void {
    const state = states.get(root);
    if (!state) {
        return;
    }

    state.input.removeEventListener('keydown', state.onKeyDown);
    states.delete(root);
}

export function scrollActiveOptionIntoView(root: HTMLElement, activeDescendantId: string): void {
    if (!activeDescendantId) {
        return;
    }

    const componentRoot = root.closest<HTMLElement>('.nt-typeahead') ?? root;
    const option = componentRoot.querySelector<HTMLElement>(`#${escapeCssIdentifier(activeDescendantId)}`);
    option?.scrollIntoView({ block: 'nearest' });
}

function escapeCssIdentifier(value: string): string {
    return globalThis.CSS?.escape?.(value)
        ?? value.replace(/[^a-zA-Z0-9_-]/g, character => `\\${character}`);
}
