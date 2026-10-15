import { getPicker, initPicker, normalizeColor } from './NTColorPicker.razor.js';

type Maybe<T> = T | null | undefined;
type FinishMode = 'cancel' | 'confirm' | 'dismiss';

interface FieldInstance {
    abort: AbortController;
    changed: boolean;
    input: HTMLInputElement;
    open: boolean;
    openAbort: AbortController | null;
    popover: HTMLElement;
    root: HTMLElement;
    snapshot: string;
    swatch: HTMLElement | null;
    trigger: HTMLButtonElement | null;
    triggerPointerWasOpen: boolean;
}

const ROOT_SELECTOR = '[data-nt-input-color]';
const instances = new Map<HTMLElement, FieldInstance>();

function resolveRoot(element: Maybe<Element>): HTMLElement | null {
    return element?.closest<HTMLElement>(ROOT_SELECTOR) ?? null;
}

function showsAlpha(instance: FieldInstance): boolean {
    return instance.root.dataset.showAlpha === 'true';
}

function pickerRoot(instance: FieldInstance): HTMLElement | null {
    return instance.popover.querySelector<HTMLElement>('[data-nt-color-picker]');
}

function updateSwatch(instance: FieldInstance): string | null {
    const color = normalizeColor(instance.input.value, showsAlpha(instance));
    if (color) {
        instance.swatch?.style.setProperty('--nt-input-color-swatch', color);
    }
    else {
        instance.swatch?.style.removeProperty('--nt-input-color-swatch');
    }

    return color;
}

function setFieldValue(instance: FieldInstance, value: string): void {
    instance.input.value = value;
    updateSwatch(instance);
    instance.input.dispatchEvent(new Event('input', { bubbles: true }));
}

function finish(instance: FieldInstance, mode: FinishMode): void {
    if (!instance.open) {
        return;
    }

    instance.open = false;
    instance.openAbort?.abort();
    instance.openAbort = null;

    if (mode === 'cancel') {
        if (instance.input.value !== instance.snapshot) {
            setFieldValue(instance, instance.snapshot);
        }

        const snapshotColor = normalizeColor(instance.snapshot, showsAlpha(instance));
        if (snapshotColor) {
            getPicker(pickerRoot(instance))?.setValue(snapshotColor);
        }
    }
    else if (instance.changed || instance.input.value !== instance.snapshot) {
        instance.input.dispatchEvent(new Event('change', { bubbles: true }));
    }

    try {
        if (typeof instance.popover.hidePopover === 'function' && instance.popover.matches(':popover-open')) {
            instance.popover.hidePopover();
        }
    }
    catch {
        // The popover was already hidden by light dismiss.
    }

    delete instance.root.dataset.open;
    instance.trigger?.setAttribute('aria-expanded', 'false');
    if (mode !== 'dismiss') {
        instance.trigger?.focus({ preventScroll: true });
    }
}

function open(instance: FieldInstance): void {
    if (instance.open || instance.input.disabled || instance.input.readOnly) {
        return;
    }

    const picker = pickerRoot(instance);
    if (picker) {
        initPicker(picker);
        const color = normalizeColor(instance.input.value, showsAlpha(instance));
        if (color) {
            getPicker(picker)?.setValue(color);
        }
    }

    updateSwatch(instance);
    instance.snapshot = instance.input.value;
    instance.changed = false;
    instance.open = true;
    instance.root.dataset.open = 'true';
    instance.trigger?.setAttribute('aria-expanded', 'true');

    try {
        instance.popover.showPopover?.();
    }
    catch {
        // Already showing or not connected; the open state above still drives the field.
    }

    const openAbort = new AbortController();
    instance.openAbort = openAbort;
    const options = { signal: openAbort.signal };
    document.addEventListener('pointerdown', event => {
        if (!instance.root.contains(event.target as Node | null)) {
            finish(instance, 'dismiss');
        }
    }, options);
    document.addEventListener('keydown', event => {
        if (event.key === 'Escape') {
            finish(instance, 'cancel');
        }
    }, options);

    getPicker(picker)?.focus();
}

function onPickerInput(instance: FieldInstance, event: Event): void {
    const value = (event as CustomEvent<{ value: string }>).detail?.value;
    if (!instance.open || value === undefined) {
        return;
    }

    event.stopPropagation();
    instance.changed = true;
    setFieldValue(instance, value);
}

function onFieldInput(instance: FieldInstance, event: Event): void {
    const color = updateSwatch(instance);
    if (color && instance.open && event.target === instance.input) {
        getPicker(pickerRoot(instance))?.setValue(color);
    }
}

function onFieldChange(instance: FieldInstance): void {
    const color = updateSwatch(instance);
    if (color && color !== instance.input.value) {
        // Normalize before Blazor reads the value so #ABC and ABCDEF post as #aabbcc and #abcdef.
        instance.input.value = color;
    }
}

function createInstance(root: HTMLElement): FieldInstance | null {
    const input = root.querySelector<HTMLInputElement>('input[data-nt-input-color-input]');
    const popover = root.querySelector<HTMLElement>('[data-nt-input-color-popover]');
    if (!input || !popover) {
        return null;
    }

    const instance: FieldInstance = {
        abort: new AbortController(),
        changed: false,
        input,
        open: false,
        openAbort: null,
        popover,
        root,
        snapshot: '',
        swatch: root.querySelector<HTMLElement>('[data-nt-input-color-swatch]'),
        trigger: root.querySelector<HTMLButtonElement>('[data-nt-input-color-trigger]'),
        triggerPointerWasOpen: false,
    };

    const options = { signal: instance.abort.signal };
    input.addEventListener('input', event => onFieldInput(instance, event), options);
    input.addEventListener('change', () => onFieldChange(instance), options);
    input.addEventListener('focus', () => updateSwatch(instance), options);
    instance.trigger?.addEventListener('pointerdown', () => {
        instance.triggerPointerWasOpen = instance.open;
    }, options);
    instance.trigger?.addEventListener('click', () => {
        const wasOpen = instance.triggerPointerWasOpen || instance.open;
        instance.triggerPointerWasOpen = false;
        if (wasOpen) {
            finish(instance, 'dismiss');
        }
        else {
            open(instance);
        }
    }, options);
    popover.addEventListener('nt-color-input', event => onPickerInput(instance, event), options);
    popover.addEventListener('toggle', event => {
        if ((event as Event & { newState?: string }).newState === 'closed') {
            finish(instance, 'dismiss');
        }
    }, options);
    popover.addEventListener('click', event => {
        const action = (event.target as Element | null)?.closest<HTMLElement>('[data-nt-input-color-action]')?.dataset.ntInputColorAction;
        if (action === 'cancel' || action === 'confirm') {
            finish(instance, action);
        }
    }, options);
    return instance;
}

function disposeInstance(root: HTMLElement, instance: FieldInstance): void {
    instance.openAbort?.abort();
    instance.abort.abort();
    instances.delete(root);
}

function cleanupDisconnected(): void {
    for (const [root, instance] of instances) {
        if (!root.isConnected) {
            disposeInstance(root, instance);
        }
    }
}

export function onLoad(element: Maybe<Element>, _dotNetRef?: unknown): void {
    cleanupDisconnected();
    const root = resolveRoot(element);
    if (!root) {
        return;
    }

    let instance = instances.get(root);
    if (!instance) {
        instance = createInstance(root) ?? undefined;
        if (instance) {
            instances.set(root, instance);
        }
    }

    if (instance) {
        updateSwatch(instance);
    }
}

export function onUpdate(element: Maybe<Element>, _dotNetRef?: unknown): void {
    onLoad(element);
}

export function onDispose(element: Maybe<Element>, _dotNetRef?: unknown): void {
    const root = resolveRoot(element);
    const instance = root ? instances.get(root) : undefined;
    if (root && instance) {
        disposeInstance(root, instance);
    }

    cleanupDisconnected();
}
