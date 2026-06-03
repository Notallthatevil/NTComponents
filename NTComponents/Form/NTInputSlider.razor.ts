type Maybe<T> = T | null | undefined;

interface SliderState {
    input: HTMLInputElement;
    onChange: () => void;
    onInput: () => void;
    root: HTMLElement;
}

const stateByInput = new Map<HTMLInputElement, SliderState>();

function getLifecycleRoot(element: Maybe<Element>): HTMLElement | null {
    if (element instanceof HTMLElement && element.classList.contains('nt-slider') && !element.classList.contains('nt-slider-range')) {
        return element;
    }

    return element?.closest<HTMLElement>('.nt-slider:not(.nt-slider-range)')
        ?? element?.parentElement?.closest<HTMLElement>('.nt-slider:not(.nt-slider-range)')
        ?? null;
}

function parseNumber(value: string | null | undefined, fallback: number): number {
    const parsed = Number.parseFloat(value ?? '');
    return Number.isFinite(parsed) ? parsed : fallback;
}

function clamp(value: number, min: number, max: number): number {
    if (max <= min) {
        return min;
    }

    return Math.min(Math.max(value, min), max);
}

function formatPercent(percent: number): string {
    return `${Number.isFinite(percent) ? Math.round(percent * 1000) / 1000 : 0}%`;
}

function getValuePercent(input: HTMLInputElement): number {
    const min = parseNumber(input.min, 0);
    const max = parseNumber(input.max, 100);
    const value = clamp(parseNumber(input.value, min), min, max);
    if (max <= min) {
        return 0;
    }

    return ((value - min) / (max - min)) * 100;
}

function updateInsetIcon(root: HTMLElement, percent: number): void {
    const icon = root.querySelector<HTMLElement>('.nt-slider-inset-icon');
    if (!icon) {
        return;
    }

    const isActive = percent >= 24;
    root.style.setProperty('--nt-slider-inset-icon-position', isActive ? '16px' : 'calc(var(--nt-slider-end-percent) + 20px)');
    icon.classList.toggle('nt-slider-inset-icon-active', isActive);
    icon.classList.toggle('nt-slider-inset-icon-inactive', !isActive);
}

function updateSlider(root: HTMLElement, input: HTMLInputElement): void {
    const min = parseNumber(input.min, 0);
    const max = parseNumber(input.max, 100);
    const value = clamp(parseNumber(input.value, min), min, max);
    if (input.value !== value.toString()) {
        input.value = value.toString();
    }

    const percent = getValuePercent(input);
    const isCentered = root.classList.contains('nt-slider-centered');
    const startPercent = isCentered ? Math.min(50, percent) : 0;
    const endPercent = isCentered ? Math.max(50, percent) : percent;

    root.style.setProperty('--nt-slider-start-percent', formatPercent(startPercent));
    root.style.setProperty('--nt-slider-end-percent', formatPercent(endPercent));
    root.style.setProperty('--nt-slider-start-gap', isCentered && startPercent < 50 ? '8px' : '0px');
    root.style.setProperty('--nt-slider-end-gap', isCentered ? (endPercent > 50 ? '8px' : '0px') : '8px');
    root.querySelector<HTMLOutputElement>('.nt-slider-value-indicator')?.replaceChildren(input.value);
    updateInsetIcon(root, percent);
}

function createState(root: HTMLElement, input: HTMLInputElement): SliderState {
    const state: SliderState = {
        input,
        onChange: () => updateSlider(root, input),
        onInput: () => updateSlider(root, input),
        root,
    };

    input.addEventListener('input', state.onInput);
    input.addEventListener('change', state.onChange);
    updateSlider(root, input);
    return state;
}

function cleanupState(state: Maybe<SliderState>): void {
    if (!state) {
        return;
    }

    state.input.removeEventListener('input', state.onInput);
    state.input.removeEventListener('change', state.onChange);
}

function synchronizeRoot(root: Maybe<HTMLElement>): void {
    const input = root?.querySelector<HTMLInputElement>('input[type="range"][data-nt-slider-input="true"]');
    if (!root || !input) {
        return;
    }

    const existing = stateByInput.get(input);
    if (existing) {
        existing.root = root;
        updateSlider(root, input);
        return;
    }

    stateByInput.set(input, createState(root, input));
}

function cleanupDisconnectedStates(): void {
    for (const [input, state] of stateByInput) {
        if (!input.isConnected) {
            cleanupState(state);
            stateByInput.delete(input);
        }
    }
}

export function onLoad(element: Maybe<Element>): void {
    cleanupDisconnectedStates();
    synchronizeRoot(getLifecycleRoot(element));
}

export function onUpdate(element: Maybe<Element>): void {
    cleanupDisconnectedStates();
    synchronizeRoot(getLifecycleRoot(element));
}

export function enhanceAll(root: ParentNode = document): void {
    cleanupDisconnectedStates();
    for (const slider of Array.from(root.querySelectorAll<HTMLElement>('.nt-slider:not(.nt-slider-range)'))) {
        synchronizeRoot(slider);
    }
}

export function onDispose(element: Maybe<Element>): void {
    const root = getLifecycleRoot(element);
    const input = root?.querySelector<HTMLInputElement>('input[type="range"][data-nt-slider-input="true"]');

    if (!input) {
        for (const state of stateByInput.values()) {
            cleanupState(state);
        }

        stateByInput.clear();
        return;
    }

    const state = stateByInput.get(input);
    cleanupState(state);
    stateByInput.delete(input);
    cleanupDisconnectedStates();
}
