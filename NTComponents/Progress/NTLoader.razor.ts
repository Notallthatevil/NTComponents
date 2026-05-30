import { onUpdate as updateShape } from '../Shapes/NTShape.razor.js';

interface NTLoaderState {
    animation?: Animation;
    intervalId?: number;
    nextShapeIndex: number;
    restartKey?: string;
    sequence?: number[];
    sequenceKey?: string;
}

interface NTLoaderElement extends HTMLElement {
    __ntLoaderState?: NTLoaderState;
}

let globalSyncScheduled = false;

function getState(element: NTLoaderElement): NTLoaderState {
    element.__ntLoaderState ??= {
        nextShapeIndex: 1
    };

    return element.__ntLoaderState;
}

function getShapeElement(element: NTLoaderElement): HTMLElement | null {
    return element.matches('nt-shape.nt-loader-shape')
        ? element
        : element.querySelector<HTMLElement>('nt-shape.nt-loader-shape');
}

function getLoaderElements(element?: Element | null): NTLoaderElement[] {
    if (element instanceof HTMLElement) {
        const loaderElement = element.matches('.nt-loader')
            ? element
            : element.closest<NTLoaderElement>('.nt-loader');

        if (loaderElement) {
            return [loaderElement as NTLoaderElement];
        }

        const shapeElement = getShapeElement(element as NTLoaderElement);
        const shapeLoaderElement = shapeElement?.closest<NTLoaderElement>('.nt-loader');
        return shapeLoaderElement ? [shapeLoaderElement] : [];
    }

    return Array.from(document.querySelectorAll<NTLoaderElement>('.nt-loader'));
}

function getSequence(element: NTLoaderElement, state: NTLoaderState): number[] {
    const sequenceKey = element.dataset.shapeSequence ?? '';

    if (state.sequenceKey === sequenceKey && state.sequence) {
        return state.sequence;
    }

    state.sequenceKey = sequenceKey;
    state.sequence = sequenceKey
        .split(/\s+/)
        .map(value => Number.parseInt(value, 10))
        .filter(value => Number.isInteger(value));

    return state.sequence;
}

function getIntervalMs(element: NTLoaderElement): number {
    const parsed = Number.parseInt(element.dataset.shapeIntervalMs ?? '', 10);
    return Number.isFinite(parsed) && parsed > 0 ? parsed : 400;
}

function getTransitionDurationMs(element: NTLoaderElement): number {
    const parsed = Number.parseInt(element.dataset.transitionDurationMs ?? '', 10);
    return Number.isFinite(parsed) && parsed > 0 ? parsed : 500;
}

function prefersReducedMotion(): boolean {
    return typeof window.matchMedia === 'function'
        && window.matchMedia('(prefers-reduced-motion: reduce)').matches;
}

function shouldAnimate(element: NTLoaderElement, sequence: number[]): boolean {
    return element.dataset.animate === 'true' && sequence.length > 1 && !prefersReducedMotion();
}

function applyShape(shapeElement: HTMLElement, shape: number): void {
    shapeElement.dataset.shape = shape.toString();
    updateShape(shapeElement);
}

function stop(element: NTLoaderElement): void {
    const state = getState(element);

    if (state.intervalId != null) {
        window.clearInterval(state.intervalId);
        state.intervalId = undefined;
    }

    state.animation?.cancel();
    state.animation = undefined;
}

function startSpinAnimation(state: NTLoaderState, shapeElement: HTMLElement, transitionDurationMs: number): void {
    if (typeof shapeElement.animate !== 'function') {
        return;
    }

    state.animation?.cancel();
    state.animation = shapeElement.animate(
        [
            { transform: 'rotate(0deg)' },
            { transform: 'rotate(720deg)' }
        ],
        {
            duration: transitionDurationMs,
            easing: 'cubic-bezier(0.4, 0, 0.2, 1)',
            fill: 'none',
            iterations: 1
        }
    );

    state.animation.addEventListener('finish', () => {
        if (state.animation?.playState === 'finished') {
            state.animation = undefined;
        }
    }, { once: true });
}

function start(element: NTLoaderElement, sequence: number[], intervalMs: number, transitionDurationMs: number): void {
    const state = getState(element);
    const shapeElement = getShapeElement(element);

    if (!shapeElement) {
        stop(element);
        return;
    }

    stop(element);
    state.nextShapeIndex = 1;

    const runCycle = () => {
        const currentShapeElement = getShapeElement(element);

        if (!currentShapeElement || !currentShapeElement.isConnected) {
            stop(element);
            return;
        }

        startSpinAnimation(state, currentShapeElement, transitionDurationMs);
        const nextShape = sequence[state.nextShapeIndex % sequence.length];

        applyShape(currentShapeElement, nextShape);
        state.nextShapeIndex = (state.nextShapeIndex + 1) % sequence.length;
    };

    runCycle();
    state.intervalId = window.setInterval(runCycle, intervalMs);
}

function syncLoader(loader: NTLoaderElement): void {
    const state = getState(loader);
    const sequence = getSequence(loader, state);
    const intervalMs = getIntervalMs(loader);
    const transitionDurationMs = getTransitionDurationMs(loader);
    const animate = shouldAnimate(loader, sequence);
    const restartKey = `${animate}|${intervalMs}|${transitionDurationMs}|${state.sequenceKey}`;
    const shapeElement = getShapeElement(loader);

    if (!shapeElement) {
        state.restartKey = undefined;
        stop(loader);
        return;
    }

    if (state.restartKey === restartKey) {
        return;
    }

    state.restartKey = restartKey;

    if (sequence.length > 0) {
        applyShape(shapeElement, sequence[0]);
    }

    if (!animate) {
        stop(loader);
        return;
    }

    start(loader, sequence, intervalMs, transitionDurationMs);
}

function sync(element?: Element | null): void {
    for (const loader of getLoaderElements(element)) {
        syncLoader(loader);
    }
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

export function onDispose(element: Element | null | undefined): void {
    for (const loader of getLoaderElements(element)) {
        stop(loader);
        delete loader.__ntLoaderState;
    }
}

export function onLoad(element: Element | null | undefined): void {
    void element;
}

export function onUpdate(element: Element | null | undefined): void {
    if (element == null) {
        scheduleGlobalSync();
        return;
    }

    sync(element);
}
