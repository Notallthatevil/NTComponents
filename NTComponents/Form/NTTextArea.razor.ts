type Maybe<T> = T | null | undefined;

interface TextAreaState {
    input: HTMLTextAreaElement;
    mutationObserver: MutationObserver | null;
    onInput: () => void;
    resizeObserver: ResizeObserver | null;
    resizeTargets: Element[];
}

interface NTComponentsTextAreaRuntime {
    autoGrowTextArea?: (input: Element | null | undefined) => void;
}

const autoGrowRetryDelay = 16;
const autoGrowRetryLimit = 20;
const autoGrowRetryByInput = new WeakMap<Element, number>();
const stateByInput = new Map<HTMLTextAreaElement, TextAreaState>();

function getRuntime(): NTComponentsTextAreaRuntime {
    return (window as typeof window & { NTComponents?: NTComponentsTextAreaRuntime }).NTComponents ?? {};
}

function cancelAutoGrowRetry(input: Element): void {
    const retryHandle = autoGrowRetryByInput.get(input);
    if (retryHandle === undefined) {
        return;
    }

    window.clearTimeout(retryHandle);
    autoGrowRetryByInput.delete(input);
}

function autoGrowInput(input: Maybe<Element>, attempt = 0): void {
    if (!(input instanceof Element)) {
        return;
    }

    const autoGrowTextArea = getRuntime().autoGrowTextArea;
    if (typeof autoGrowTextArea === 'function') {
        cancelAutoGrowRetry(input);
        autoGrowTextArea(input);
        return;
    }

    if (attempt >= autoGrowRetryLimit || !input.isConnected) {
        return;
    }

    cancelAutoGrowRetry(input);
    autoGrowRetryByInput.set(input, window.setTimeout(() => autoGrowInput(input, attempt + 1), autoGrowRetryDelay));
}

function getLifecycleInput(element: Maybe<Element>): HTMLTextAreaElement | null {
    if (element instanceof HTMLTextAreaElement) {
        return element;
    }

    const previous = element?.previousElementSibling;
    if (previous instanceof HTMLTextAreaElement) {
        return previous;
    }

    return element?.parentElement?.querySelector<HTMLTextAreaElement>('textarea[data-nt-textarea-autogrow="true"]') ?? null;
}

function getResizeTargets(input: HTMLTextAreaElement): Element[] {
    const root = input.closest<HTMLElement>('.nt-input');
    const controlContainer = root?.querySelector<HTMLElement>(':scope .nt-input-control-container');
    const candidates: Maybe<Element>[] = [
        controlContainer,
        root,
    ];
    return Array.from(new Set(candidates.filter((target): target is Element => target instanceof Element)));
}

function cleanupState(state: Maybe<TextAreaState>, clearInput = true): void {
    if (!state) {
        return;
    }

    state.input.removeEventListener('input', state.onInput);
    state.resizeObserver?.disconnect();
    state.mutationObserver?.disconnect();
    cancelAutoGrowRetry(state.input);
    if (clearInput) {
        autoGrowInput(state.input);
    }
}

function createState(input: HTMLTextAreaElement): TextAreaState {
    const state: TextAreaState = {
        input,
        mutationObserver: typeof MutationObserver === 'function'
            ? new MutationObserver(() => synchronizeInput(input))
            : null,
        onInput: () => autoGrowInput(input),
        resizeObserver: typeof ResizeObserver === 'function'
            ? new ResizeObserver(() => autoGrowInput(input))
            : null,
        resizeTargets: getResizeTargets(input),
    };

    input.addEventListener('input', state.onInput);
    state.mutationObserver?.observe(input, { attributeFilter: ['data-nt-textarea-autogrow'], attributes: true });
    for (const target of state.resizeTargets) {
        state.resizeObserver?.observe(target);
    }

    autoGrowInput(input);
    return state;
}

function synchronizeInput(input: Maybe<HTMLTextAreaElement>): void {
    if (!input) {
        return;
    }

    const existing = stateByInput.get(input);
    if (!input.isConnected || input.dataset.ntTextareaAutogrow !== 'true') {
        cleanupState(existing);
        stateByInput.delete(input);
        if (!existing) {
            autoGrowInput(input);
        }
        return;
    }

    if (existing) {
        autoGrowInput(input);
        return;
    }

    stateByInput.set(input, createState(input));
}

function cleanupInactiveStates(): void {
    for (const [input, state] of stateByInput) {
        if (!input.isConnected || input.dataset.ntTextareaAutogrow !== 'true') {
            cleanupState(state);
            stateByInput.delete(input);
        }
    }
}

export function onLoad(element: Maybe<Element>): void {
    cleanupInactiveStates();
    synchronizeInput(getLifecycleInput(element));
}

export function onUpdate(element: Maybe<Element>): void {
    cleanupInactiveStates();
    synchronizeInput(getLifecycleInput(element));
}

export function enhanceAll(root: ParentNode = document): void {
    cleanupInactiveStates();
    for (const input of Array.from(root.querySelectorAll<HTMLTextAreaElement>('textarea[data-nt-textarea-autogrow="true"]'))) {
        synchronizeInput(input);
    }
}

export function onDispose(element: Maybe<Element>): void {
    const input = getLifecycleInput(element);

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
    cleanupInactiveStates();
}
