type Maybe<T> = T | null | undefined;
type ColorFormat = 'hex' | 'hsl' | 'rgb';

export interface Rgba { a: number; b: number; g: number; r: number }
export interface Hsv { h: number; s: number; v: number }
export interface Hsl { h: number; l: number; s: number }

export interface ColorPickerHandle {
    focus(): void;
    getValue(): string;
    setValue(value: string): boolean;
}

interface PickerInstance {
    a: number;
    abort: AbortController;
    committed: string;
    dragging: boolean;
    format: ColorFormat;
    h: number;
    lastAttribute: string | null;
    root: HTMLElement;
    s: number;
    showAlpha: boolean;
    v: number;
    value: string;
}

const ROOT_SELECTOR = '[data-nt-color-picker]';
const HEX_PATTERN = /^#?([0-9a-f]{3}|[0-9a-f]{6}|[0-9a-f]{8})$/i;
const INTEGER_PATTERN = /^\d{1,3}$/;
const instances = new Map<HTMLElement, PickerInstance>();

function clamp(value: number, min: number, max: number): number {
    return Math.min(Math.max(value, min), max);
}

function roundTo(value: number, digits = 4): number {
    const factor = 10 ** digits;
    return Math.round(value * factor) / factor;
}

function toHexByte(value: number): string {
    return clamp(Math.round(value), 0, 255).toString(16).padStart(2, '0');
}

export function hsvToRgb(h: number, s: number, v: number): { b: number; g: number; r: number } {
    const hue = ((h % 360) + 360) % 360;
    const chroma = v * s;
    const x = chroma * (1 - Math.abs(((hue / 60) % 2) - 1));
    const m = v - chroma;
    let r = chroma;
    let g = x;
    let b = 0;
    switch (Math.floor(hue / 60)) {
        case 1: r = x; g = chroma; break;
        case 2: r = 0; g = chroma; b = x; break;
        case 3: r = 0; g = x; b = chroma; break;
        case 4: r = x; g = 0; b = chroma; break;
        case 5: r = chroma; g = 0; b = x; break;
    }

    return { r: Math.round((r + m) * 255), g: Math.round((g + m) * 255), b: Math.round((b + m) * 255) };
}

export function rgbToHsv(r: number, g: number, b: number): Hsv {
    const red = r / 255;
    const green = g / 255;
    const blue = b / 255;
    const max = Math.max(red, green, blue);
    const delta = max - Math.min(red, green, blue);
    let h = 0;
    if (delta !== 0) {
        if (max === red) {
            h = ((green - blue) / delta) % 6;
        }
        else if (max === green) {
            h = ((blue - red) / delta) + 2;
        }
        else {
            h = ((red - green) / delta) + 4;
        }
    }

    return { h: (h * 60 + 360) % 360, s: max === 0 ? 0 : delta / max, v: max };
}

export function hsvToHsl(h: number, s: number, v: number): Hsl {
    const l = v * (1 - s / 2);
    const denominator = Math.min(l, 1 - l);
    return { h, s: denominator === 0 ? 0 : (v - l) / denominator, l };
}

export function hslToHsv(h: number, s: number, l: number): Hsv {
    const v = l + s * Math.min(l, 1 - l);
    return { h, s: v === 0 ? 0 : 2 * (1 - l / v), v };
}

export function rgbToHsl(r: number, g: number, b: number): Hsl {
    const { h, s, v } = rgbToHsv(r, g, b);
    return hsvToHsl(h, s, v);
}

export function hslToRgb(h: number, s: number, l: number): { b: number; g: number; r: number } {
    const hsv = hslToHsv(h, s, l);
    return hsvToRgb(hsv.h, hsv.s, hsv.v);
}

/** Parses #rgb, #rrggbb, or #rrggbbaa (case-insensitive, # optional). Returns null for anything else. */
export function parseColor(text: Maybe<string>): Rgba | null {
    const match = HEX_PATTERN.exec((text ?? '').trim());
    if (!match) {
        return null;
    }

    let digits = match[1].toLowerCase();
    if (digits.length === 3) {
        digits = [...digits].map(digit => digit + digit).join('');
    }

    return {
        r: Number.parseInt(digits.slice(0, 2), 16),
        g: Number.parseInt(digits.slice(2, 4), 16),
        b: Number.parseInt(digits.slice(4, 6), 16),
        a: digits.length === 8 ? Number.parseInt(digits.slice(6, 8), 16) / 255 : 1,
    };
}

/** Formats as lowercase #rrggbb, or #rrggbbaa when alpha is shown and below 100%. */
export function formatColor(color: Rgba, showAlpha: boolean): string {
    const base = `#${toHexByte(color.r)}${toHexByte(color.g)}${toHexByte(color.b)}`;
    const alphaByte = Math.round(clamp(color.a, 0, 1) * 255);
    return showAlpha && alphaByte < 255 ? base + toHexByte(alphaByte) : base;
}

export function normalizeColor(text: Maybe<string>, showAlpha: boolean): string | null {
    const color = parseColor(text);
    return color ? formatColor(color, showAlpha) : null;
}

function resolveRoot(element: Maybe<Element>): HTMLElement | null {
    if (!element) {
        return null;
    }

    if (element.matches(ROOT_SELECTOR)) {
        return element as HTMLElement;
    }

    const sibling = element.previousElementSibling;
    if (sibling?.matches(ROOT_SELECTOR)) {
        return sibling as HTMLElement;
    }

    return element.closest<HTMLElement>(ROOT_SELECTOR);
}

function find<T extends HTMLElement>(instance: PickerInstance, selector: string): T | null {
    return instance.root.querySelector<T>(selector);
}

function isDisabled(instance: PickerInstance): boolean {
    return instance.root.dataset.disabled === 'true';
}

function isEmbedded(instance: PickerInstance): boolean {
    return instance.root.dataset.ntColorEmbedded === 'true';
}

function currentColor(instance: PickerInstance): Rgba {
    return { ...hsvToRgb(instance.h, instance.s, instance.v), a: instance.showAlpha ? instance.a : 1 };
}

function currentHex(instance: PickerInstance): string {
    return formatColor(currentColor(instance), instance.showAlpha);
}

/** Pass `formatChanged: false` while dragging or stepping: the format buttons and field groups only depend on the active format. */
function render(instance: PickerInstance, formatChanged = true): void {
    const { root } = instance;
    const color = currentColor(instance);
    const saturation = Math.round(instance.s * 100);
    const brightness = Math.round(instance.v * 100);
    const alphaPercent = Math.round(color.a * 100);
    const solid = formatColor({ ...color, a: 1 }, false);

    root.style.setProperty('--nt-color-picker-hue', `hsl(${Math.round(instance.h)} 100% 50%)`);
    root.style.setProperty('--nt-color-picker-solid', solid);
    root.style.setProperty('--nt-color-picker-color', `rgb(${color.r} ${color.g} ${color.b} / ${roundTo(color.a, 3)})`);
    root.dataset.format = instance.format;

    const area = find(instance, '[data-nt-color-area]');
    if (area) {
        area.style.setProperty('--nt-color-area-x', `${roundTo(instance.s * 100, 2)}%`);
        area.style.setProperty('--nt-color-area-y', `${roundTo((1 - instance.v) * 100, 2)}%`);
        area.setAttribute('aria-valuenow', String(saturation));
        area.setAttribute('aria-valuetext', `Saturation ${saturation}%, brightness ${brightness}%`);
    }

    const hue = find<HTMLInputElement>(instance, '[data-nt-color-hue]');
    if (hue) {
        hue.value = String(Math.round(instance.h));
    }

    const alpha = find<HTMLInputElement>(instance, '[data-nt-color-alpha]');
    if (alpha) {
        alpha.value = String(alphaPercent);
    }

    if (formatChanged) {
        for (const button of root.querySelectorAll<HTMLElement>('[data-nt-color-format]')) {
            button.setAttribute('aria-pressed', String(button.dataset.ntColorFormat === instance.format));
        }

        for (const fieldset of root.querySelectorAll<HTMLElement>('[data-nt-color-fieldset]')) {
            fieldset.hidden = !(fieldset.dataset.ntColorFieldset ?? '').split(' ').includes(instance.format);
        }
    }

    const hsl = hsvToHsl(instance.h, instance.s, instance.v);
    const values: Record<string, string> = {
        hex: formatColor(color, instance.showAlpha),
        r: String(color.r),
        g: String(color.g),
        b: String(color.b),
        h: String(Math.round(hsl.h) % 360),
        s: String(Math.round(hsl.s * 100)),
        l: String(Math.round(hsl.l * 100)),
        a: String(alphaPercent),
    };
    for (const field of root.querySelectorAll<HTMLInputElement>('[data-nt-color-field]')) {
        field.value = values[field.dataset.ntColorField ?? ''] ?? '';
    }
}

function emitValue(instance: PickerInstance, type: 'change' | 'input'): void {
    const hidden = find<HTMLInputElement>(instance, '[data-nt-color-value]');
    const value = currentHex(instance);
    if (type === 'input' ? value === instance.value : value === instance.committed) {
        return;
    }

    instance.value = value;
    if (hidden) {
        hidden.value = value;
    }

    if (type === 'change') {
        instance.committed = value;
    }

    instance.root.dispatchEvent(new CustomEvent(`nt-color-${type}`, { bubbles: true, detail: { value } }));
    if (!isEmbedded(instance)) {
        hidden?.dispatchEvent(new Event(type, { bubbles: true }));
    }
}

function applyHsv(instance: PickerInstance, hsv: Partial<Hsv> & { a?: number }, commit: boolean): void {
    instance.h = hsv.h ?? instance.h;
    instance.s = clamp(hsv.s ?? instance.s, 0, 1);
    instance.v = clamp(hsv.v ?? instance.v, 0, 1);
    instance.a = clamp(hsv.a ?? instance.a, 0, 1);
    render(instance, false);
    emitValue(instance, 'input');
    if (commit) {
        emitValue(instance, 'change');
    }
}

function setFromColor(instance: PickerInstance, color: Rgba): void {
    const hsv = rgbToHsv(color.r, color.g, color.b);
    instance.h = hsv.s === 0 || hsv.v === 0 ? instance.h : hsv.h;
    instance.s = hsv.v === 0 ? instance.s : hsv.s;
    instance.v = hsv.v;
    instance.a = instance.showAlpha ? color.a : 1;
}

function setValue(instance: PickerInstance, text: Maybe<string>): boolean {
    const color = parseColor(text);
    if (!color) {
        return false;
    }

    if (formatColor(color, instance.showAlpha) !== currentHex(instance)) {
        setFromColor(instance, color);
    }

    instance.value = instance.committed = currentHex(instance);
    const hidden = find<HTMLInputElement>(instance, '[data-nt-color-value]');
    if (hidden) {
        hidden.value = instance.value;
    }

    render(instance);
    return true;
}

function parseChannel(text: string, max: number): number | null {
    const trimmed = text.trim();
    if (!INTEGER_PATTERN.test(trimmed)) {
        return null;
    }

    const value = Number.parseInt(trimmed, 10);
    return value <= max ? value : null;
}

function commitField(instance: PickerInstance, field: HTMLInputElement): void {
    const key = field.dataset.ntColorField ?? '';
    const color = currentColor(instance);
    const hsl = hsvToHsl(instance.h, instance.s, instance.v);
    let valid = true;

    if (key === 'hex') {
        const parsed = parseColor(field.value);
        if (parsed) {
            setFromColor(instance, parsed);
        }
        else {
            valid = false;
        }
    }
    else if (key === 'r' || key === 'g' || key === 'b') {
        const channel = parseChannel(field.value, 255);
        if (channel === null) {
            valid = false;
        }
        else {
            setFromColor(instance, { ...color, [key]: channel });
        }
    }
    else if (key === 'h' || key === 's' || key === 'l') {
        const channel = parseChannel(field.value, key === 'h' ? 360 : 100);
        if (channel === null) {
            valid = false;
        }
        else {
            const next = { ...hsl, [key]: key === 'h' ? channel : channel / 100 };
            const hsv = hslToHsv(next.h, next.s, next.l);
            instance.h = hsv.h;
            instance.s = clamp(hsv.s, 0, 1);
            instance.v = clamp(hsv.v, 0, 1);
        }
    }
    else if (key === 'a') {
        const channel = parseChannel(field.value, 100);
        if (channel === null) {
            valid = false;
        }
        else {
            instance.a = channel / 100;
        }
    }

    render(instance);
    if (valid) {
        emitValue(instance, 'input');
        emitValue(instance, 'change');
    }
}

function updateFromPointer(instance: PickerInstance, area: HTMLElement, event: MouseEvent, commit: boolean): void {
    const rect = area.getBoundingClientRect();
    if (rect.width <= 0 || rect.height <= 0) {
        return;
    }

    applyHsv(instance, {
        s: clamp((event.clientX - rect.left) / rect.width, 0, 1),
        v: 1 - clamp((event.clientY - rect.top) / rect.height, 0, 1),
    }, commit);
}

function onPointerDown(instance: PickerInstance, event: MouseEvent & { pointerId?: number }): void {
    const area = (event.target as Element | null)?.closest<HTMLElement>('[data-nt-color-area]');
    if (!area || isDisabled(instance) || (event.button ?? 0) !== 0) {
        return;
    }

    event.preventDefault();
    area.focus({ preventScroll: true });
    try {
        area.setPointerCapture?.(event.pointerId as number);
    }
    catch {
        // Synthetic pointers cannot be captured; dragging still works while the pointer stays over the area.
    }

    instance.dragging = true;
    updateFromPointer(instance, area, event, false);
}

function onPointerMove(instance: PickerInstance, event: MouseEvent): void {
    const area = (event.target as Element | null)?.closest<HTMLElement>('[data-nt-color-area]');
    if (area && instance.dragging) {
        updateFromPointer(instance, area, event, false);
    }
}

function onPointerEnd(instance: PickerInstance, event: MouseEvent & { pointerId?: number }): void {
    const area = (event.target as Element | null)?.closest<HTMLElement>('[data-nt-color-area]');
    if (!area || !instance.dragging) {
        return;
    }

    instance.dragging = false;
    try {
        area.releasePointerCapture?.(event.pointerId as number);
    }
    catch {
        // The capture was already released.
    }

    emitValue(instance, 'change');
}

function onKeyDown(instance: PickerInstance, event: KeyboardEvent): void {
    const target = event.target as HTMLElement | null;
    if (!target || isDisabled(instance)) {
        return;
    }

    if (target.matches('[data-nt-color-field]')) {
        if (event.key === 'Enter') {
            event.preventDefault();
            commitField(instance, target as HTMLInputElement);
        }
        return;
    }

    if (!target.matches('[data-nt-color-area]')) {
        return;
    }

    const step = event.shiftKey ? 0.1 : 0.01;
    let { s, v } = instance;
    switch (event.key) {
        case 'ArrowLeft': s -= step; break;
        case 'ArrowRight': s += step; break;
        case 'ArrowUp': v += step; break;
        case 'ArrowDown': v -= step; break;
        case 'Home': s = 0; break;
        case 'End': s = 1; break;
        case 'PageUp': v += 0.1; break;
        case 'PageDown': v -= 0.1; break;
        default: return;
    }

    event.preventDefault();
    applyHsv(instance, { s: roundTo(clamp(s, 0, 1)), v: roundTo(clamp(v, 0, 1)) }, true);
}

function onInput(instance: PickerInstance, event: Event): void {
    const target = event.target as HTMLInputElement | null;
    if (!target || target.dataset.ntColorValue !== undefined) {
        return;
    }

    event.stopPropagation();
    if (target.matches('[data-nt-color-hue]')) {
        applyHsv(instance, { h: Number(target.value) }, false);
    }
    else if (target.matches('[data-nt-color-alpha]')) {
        applyHsv(instance, { a: Number(target.value) / 100 }, false);
    }
}

function onChange(instance: PickerInstance, event: Event): void {
    const target = event.target as HTMLInputElement | null;
    if (!target || target.dataset.ntColorValue !== undefined) {
        return;
    }

    event.stopPropagation();
    if (target.matches('[data-nt-color-hue], [data-nt-color-alpha]')) {
        emitValue(instance, 'change');
    }
    else if (target.matches('[data-nt-color-field]')) {
        commitField(instance, target);
    }
}

function onClick(instance: PickerInstance, event: MouseEvent): void {
    const button = (event.target as Element | null)?.closest<HTMLElement>('[data-nt-color-format]');
    if (!button || isDisabled(instance)) {
        return;
    }

    instance.format = (button.dataset.ntColorFormat as ColorFormat) ?? 'hex';
    render(instance);
}

function createInstance(root: HTMLElement): PickerInstance {
    const instance: PickerInstance = {
        a: 1,
        abort: new AbortController(),
        committed: '',
        dragging: false,
        format: 'hex',
        h: 0,
        lastAttribute: null,
        root,
        s: 0,
        showAlpha: root.dataset.showAlpha === 'true',
        v: 0,
        value: '',
    };

    const options = { signal: instance.abort.signal };
    root.addEventListener('pointerdown', event => onPointerDown(instance, event), options);
    root.addEventListener('pointermove', event => onPointerMove(instance, event as MouseEvent), options);
    root.addEventListener('pointerup', event => onPointerEnd(instance, event as MouseEvent), options);
    root.addEventListener('pointercancel', event => onPointerEnd(instance, event as MouseEvent), options);
    root.addEventListener('keydown', event => onKeyDown(instance, event), options);
    root.addEventListener('input', event => onInput(instance, event), options);
    root.addEventListener('change', event => onChange(instance, event), options);
    root.addEventListener('click', event => onClick(instance, event), options);
    return instance;
}

function synchronize(instance: PickerInstance): void {
    instance.showAlpha = instance.root.dataset.showAlpha === 'true';
    const attribute = instance.root.dataset.value ?? '';
    if (attribute !== instance.lastAttribute) {
        instance.lastAttribute = attribute;
        if (attribute) {
            setValue(instance, attribute);
        }
    }

    render(instance);
}

function cleanupDisconnected(): void {
    for (const [root, instance] of instances) {
        if (!root.isConnected) {
            instance.abort.abort();
            instances.delete(root);
        }
    }
}

export function initPicker(root: HTMLElement): void {
    cleanupDisconnected();
    let instance = instances.get(root);
    if (!instance) {
        instance = createInstance(root);
        instances.set(root, instance);
    }

    synchronize(instance);
}

export function getPicker(root: Maybe<Element>): ColorPickerHandle | null {
    const element = root instanceof HTMLElement ? (root.matches(ROOT_SELECTOR) ? root : root.querySelector<HTMLElement>(ROOT_SELECTOR)) : null;
    const instance = element ? instances.get(element) : undefined;
    if (!instance) {
        return null;
    }

    return {
        focus: () => find(instance, '[data-nt-color-area]')?.focus({ preventScroll: true }),
        getValue: () => instance.value,
        setValue: value => setValue(instance, value),
    };
}

export function onLoad(element: Maybe<Element>, _dotNetRef?: unknown): void {
    const root = resolveRoot(element);
    if (root) {
        initPicker(root);
    }
}

export function onUpdate(element: Maybe<Element>, _dotNetRef?: unknown): void {
    onLoad(element);
}

export function onDispose(element: Maybe<Element>, _dotNetRef?: unknown): void {
    const root = resolveRoot(element);
    const instance = root ? instances.get(root) : undefined;
    if (instance && root) {
        instance.abort.abort();
        instances.delete(root);
    }

    cleanupDisconnected();
}
