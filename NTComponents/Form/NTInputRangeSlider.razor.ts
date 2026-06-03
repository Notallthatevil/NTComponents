type Maybe<T> = T | null | undefined;

interface RangeSliderState {
    endInput: HTMLInputElement;
    form: HTMLFormElement | null;
    onEndChange: () => void;
    onEndInput: () => void;
    onFormSubmit: () => void;
    onStartChange: () => void;
    onStartInput: () => void;
    root: HTMLElement;
    startInput: HTMLInputElement;
}

const stateByStartInput = new Map<HTMLInputElement, RangeSliderState>();

function getLifecycleRoot(element: Maybe<Element>): HTMLElement | null {
    if (element instanceof HTMLElement && element.classList.contains('nt-slider-range')) {
        return element;
    }

    return element?.closest<HTMLElement>('.nt-slider-range')
        ?? element?.parentElement?.closest<HTMLElement>('.nt-slider-range')
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

function getPercent(value: number, min: number, max: number): number {
    if (max <= min) {
        return 0;
    }

    return ((value - min) / (max - min)) * 100;
}

function setInputValue(input: HTMLInputElement, value: number): void {
    const next = value.toString();
    if (input.value !== next) {
        input.value = next;
    }
}

function setOutputText(output: HTMLOutputElement | null, value: number): void {
    output?.replaceChildren(value.toString());
}

function getBounds(state: RangeSliderState): { end: number; max: number; min: number; start: number } {
    const min = parseNumber(state.startInput.min, 0);
    const max = parseNumber(state.endInput.max, 100);
    let start = clamp(parseNumber(state.startInput.value, min), min, max);
    let end = clamp(parseNumber(state.endInput.value, max), min, max);
    if (start > end) {
        const previousStart = start;
        start = end;
        end = previousStart;
    }

    return { end, max, min, start };
}

function updateRangeSlider(state: RangeSliderState): void {
    const bounds = getBounds(state);
    setInputValue(state.startInput, bounds.start);
    setInputValue(state.endInput, bounds.end);
    state.startInput.max = bounds.end.toString();
    state.endInput.min = bounds.start.toString();
    state.root.style.setProperty('--nt-slider-start-percent', formatPercent(getPercent(bounds.start, bounds.min, bounds.max)));
    state.root.style.setProperty('--nt-slider-end-percent', formatPercent(getPercent(bounds.end, bounds.min, bounds.max)));
    state.root.style.setProperty('--nt-slider-start-gap', '8px');
    state.root.style.setProperty('--nt-slider-end-gap', '8px');
    setOutputText(state.root.querySelector<HTMLOutputElement>('.nt-slider-start-value'), bounds.start);
    setOutputText(state.root.querySelector<HTMLOutputElement>('.nt-slider-end-value'), bounds.end);
}

function updateStart(state: RangeSliderState): void {
    const end = parseNumber(state.endInput.value, parseNumber(state.endInput.max, 100));
    const min = parseNumber(state.startInput.min, 0);
    setInputValue(state.startInput, clamp(parseNumber(state.startInput.value, min), min, end));
    updateRangeSlider(state);
}

function updateEnd(state: RangeSliderState): void {
    const start = parseNumber(state.startInput.value, parseNumber(state.startInput.min, 0));
    const max = parseNumber(state.endInput.max, 100);
    setInputValue(state.endInput, clamp(parseNumber(state.endInput.value, max), start, max));
    updateRangeSlider(state);
}

function createState(root: HTMLElement, startInput: HTMLInputElement, endInput: HTMLInputElement): RangeSliderState {
    const state: RangeSliderState = {
        endInput,
        form: startInput.form ?? endInput.form,
        onEndChange: () => updateEnd(state),
        onEndInput: () => updateEnd(state),
        onFormSubmit: () => updateRangeSlider(state),
        onStartChange: () => updateStart(state),
        onStartInput: () => updateStart(state),
        root,
        startInput,
    };

    startInput.addEventListener('input', state.onStartInput);
    startInput.addEventListener('change', state.onStartChange);
    endInput.addEventListener('input', state.onEndInput);
    endInput.addEventListener('change', state.onEndChange);
    state.form?.addEventListener('submit', state.onFormSubmit);
    updateRangeSlider(state);
    return state;
}

function cleanupState(state: Maybe<RangeSliderState>): void {
    if (!state) {
        return;
    }

    state.startInput.removeEventListener('input', state.onStartInput);
    state.startInput.removeEventListener('change', state.onStartChange);
    state.endInput.removeEventListener('input', state.onEndInput);
    state.endInput.removeEventListener('change', state.onEndChange);
    state.form?.removeEventListener('submit', state.onFormSubmit);
}

function synchronizeRoot(root: Maybe<HTMLElement>): void {
    const startInput = root?.querySelector<HTMLInputElement>('input[type="range"][data-nt-slider-range-start="true"]');
    const endInput = root?.querySelector<HTMLInputElement>('input[type="range"][data-nt-slider-range-end="true"]');
    if (!root || !startInput || !endInput) {
        return;
    }

    const existing = stateByStartInput.get(startInput);
    if (existing) {
        existing.root = root;
        existing.endInput = endInput;
        updateRangeSlider(existing);
        return;
    }

    stateByStartInput.set(startInput, createState(root, startInput, endInput));
}

function cleanupDisconnectedStates(): void {
    for (const [input, state] of stateByStartInput) {
        if (!input.isConnected || !state.endInput.isConnected) {
            cleanupState(state);
            stateByStartInput.delete(input);
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
    for (const slider of Array.from(root.querySelectorAll<HTMLElement>('.nt-slider-range'))) {
        synchronizeRoot(slider);
    }
}

export function onDispose(element: Maybe<Element>): void {
    const root = getLifecycleRoot(element);
    const input = root?.querySelector<HTMLInputElement>('input[type="range"][data-nt-slider-range-start="true"]');

    if (!input) {
        for (const state of stateByStartInput.values()) {
            cleanupState(state);
        }

        stateByStartInput.clear();
        return;
    }

    const state = stateByStartInput.get(input);
    cleanupState(state);
    stateByStartInput.delete(input);
    cleanupDisconnectedStates();
}
