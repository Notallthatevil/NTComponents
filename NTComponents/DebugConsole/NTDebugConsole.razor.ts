interface DotNetReference {
    invokeMethodAsync(methodName: 'RestoreStateAsync', expanded: boolean): Promise<void>;
    invokeMethodAsync(methodName: 'ToggleVisibleAsync'): Promise<void>;
}

interface Shortcut {
    alt: boolean;
    ctrl: boolean;
    key: string;
    meta: boolean;
    shift: boolean;
}

// Visibility is not stored: the bar has no control to show it again, so a hidden console comes back on the next load.
interface StoredState {
    expanded: boolean;
    height?: number;
}

interface ConsoleState {
    atBottom: boolean;
    busyTimer?: ReturnType<typeof setTimeout>;
    dotNet: DotNetReference;
    handle: HTMLElement | null;
    height?: number;
    jump: HTMLButtonElement | null;
    list: HTMLElement | null;
    listObserver?: MutationObserver;
    resizeObserver?: ResizeObserver;
    root: HTMLElement | null;
    seen: number;
    shortcut: Shortcut | null;
    readonly onHandleDoubleClick: () => void;
    readonly onHandleKeyDown: (event: KeyboardEvent) => void;
    readonly onHandlePointerDown: (event: PointerEvent) => void;
    readonly onJump: () => void;
    readonly onKeyDown: (event: KeyboardEvent) => void;
    readonly onScroll: () => void;
}

export const storageKey = 'nt-debug-console';
export const insetProperty = '--nt-debug-console-inset';

// A list within this many pixels of its end counts as scrolled to the bottom.
const bottomTolerance = 4;
// Screen readers announce the log at most this often, so a burst of entries is read once.
const announceInterval = 2000;
// The expanded sheet can be resized between this height and the viewport height less the gap the styles keep above it.
export const minHeight = 200;
const topGap = 64;
const keyStep = 16;
const largeKeyStep = 64;
const keyCodes: Record<string, string> = { '`': 'Backquote', '-': 'Minus', '=': 'Equal', '.': 'Period', ',': 'Comma', '/': 'Slash', ';': 'Semicolon', '\\': 'Backslash' };
const consoles = new Map<string, ConsoleState>();

export function parseShortcut(value?: string | null): Shortcut | null {
    const text = value?.trim();
    if (!text) {
        return null;
    }

    // A trailing "+" is the plus key itself, as in "Ctrl++".
    const parts = text.endsWith('++') ? [...text.slice(0, -2).split('+'), '+'] : text.split('+');
    const names = parts.map(part => part.trim()).filter(part => part.length > 0);
    const key = names.pop();
    if (!key) {
        return null;
    }

    const modifiers = new Set(names.map(name => name.toLowerCase()));
    return {
        alt: modifiers.has('alt'),
        ctrl: modifiers.has('ctrl') || modifiers.has('control'),
        key: key.toLowerCase(),
        meta: modifiers.has('meta') || modifiers.has('cmd') || modifiers.has('win'),
        shift: modifiers.has('shift')
    };
}

export function matchesShortcut(shortcut: Shortcut | null, event: KeyboardEvent): boolean {
    if (!shortcut || event.ctrlKey !== shortcut.ctrl || event.altKey !== shortcut.alt || event.shiftKey !== shortcut.shift || event.metaKey !== shortcut.meta) {
        return false;
    }

    // Fall back to the physical key, so layouts that type a different character for it still match.
    const code = keyCodes[shortcut.key] ?? (/^[a-z]$/.test(shortcut.key) ? `Key${shortcut.key.toUpperCase()}` : /^[0-9]$/.test(shortcut.key) ? `Digit${shortcut.key}` : undefined);
    return event.key?.toLowerCase() === shortcut.key || (code !== undefined && event.code === code);
}

export function readStoredState(): StoredState | null {
    try {
        const stored = JSON.parse(globalThis.localStorage?.getItem(storageKey) ?? 'null') as Partial<StoredState> | null;
        if (typeof stored?.expanded !== 'boolean') {
            return null;
        }

        const height = typeof stored.height === 'number' && Number.isFinite(stored.height) && stored.height > 0 ? stored.height : undefined;
        return { expanded: stored.expanded, height };
    }
    catch {
        // Storage can be disabled, full, or hold a value written by something else.
        return null;
    }
}

export function saveState(expanded: boolean, height?: number): void {
    try {
        // An unset height is left out, so the sheet keeps its default size.
        globalThis.localStorage?.setItem(storageKey, JSON.stringify({ expanded, height }));
    }
    catch {
        // Remembering the state is a convenience; the console works without it.
    }
}

export function onLoad(id: string, root: HTMLElement | null, dotNet: DotNetReference, shortcut?: string | null): void {
    onDispose(id);

    const stored = readStoredState();
    const state: ConsoleState = {
        atBottom: true,
        dotNet,
        handle: null,
        height: stored?.height,
        jump: null,
        list: null,
        root: null,
        seen: 0,
        shortcut: parseShortcut(shortcut),
        onHandleDoubleClick: () => resize(state, undefined),
        onHandleKeyDown: event => onHandleKeyDown(state, event),
        onHandlePointerDown: event => onHandlePointerDown(state, event),
        onJump: () => jumpToBottom(state),
        onKeyDown: event => {
            if (!event.repeat && matchesShortcut(state.shortcut, event)) {
                event.preventDefault();
                void state.dotNet.invokeMethodAsync('ToggleVisibleAsync').catch(() => undefined);
            }
        },
        onScroll: () => onListScrolled(state)
    };
    consoles.set(id, state);
    document.addEventListener('keydown', state.onKeyDown);
    attach(state, root);

    if (stored) {
        void dotNet.invokeMethodAsync('RestoreStateAsync', stored.expanded).catch(() => undefined);
    }
}

export function onUpdate(id: string, root: HTMLElement | null, shortcut: string | null | undefined, expanded: boolean): void {
    const state = consoles.get(id);
    if (!state) {
        return;
    }

    state.shortcut = parseShortcut(shortcut);
    attach(state, root);
    saveState(expanded, state.height);
}

export function onDispose(id: string): void {
    const state = consoles.get(id);
    if (!state) {
        return;
    }

    consoles.delete(id);
    document.removeEventListener('keydown', state.onKeyDown);
    attach(state, null);
}

export async function copyText(text: string): Promise<boolean> {
    try {
        await navigator.clipboard.writeText(text);
        return true;
    }
    catch {
        return false;
    }
}

function attach(state: ConsoleState, root: HTMLElement | null): void {
    const element = root instanceof HTMLElement && root.isConnected ? root : null;
    if (state.root !== element) {
        state.resizeObserver?.disconnect();
        state.resizeObserver = undefined;
        state.root = element;
        if (element && typeof ResizeObserver !== 'undefined') {
            state.resizeObserver = new ResizeObserver(() => onResized(state));
            state.resizeObserver.observe(element);
        }
    }
    updateInset(state);
    attachHandle(state, element?.querySelector<HTMLElement>('[data-nt-debug-console-resize]') ?? null);
    attachList(state, element?.querySelector<HTMLElement>('[data-nt-debug-console-list]') ?? null);
}

// The handle is rendered only while the console is expanded, so the remembered height applies only then.
function attachHandle(state: ConsoleState, handle: HTMLElement | null): void {
    if (state.handle !== handle) {
        state.handle?.removeEventListener('dblclick', state.onHandleDoubleClick);
        state.handle?.removeEventListener('keydown', state.onHandleKeyDown);
        state.handle?.removeEventListener('pointerdown', state.onHandlePointerDown);
        state.handle = handle;
        handle?.addEventListener('dblclick', state.onHandleDoubleClick);
        handle?.addEventListener('keydown', state.onHandleKeyDown);
        handle?.addEventListener('pointerdown', state.onHandlePointerDown);
    }
    applyHeight(state);
}

function applyHeight(state: ConsoleState): void {
    if (!state.root) {
        return;
    }

    if (state.handle && state.height !== undefined) {
        state.root.style.blockSize = `${clampHeight(state.height)}px`;
    }
    else {
        state.root.style.removeProperty('block-size');
    }
    updateHandle(state);
}

function clampHeight(height: number): number {
    return Math.round(Math.min(Math.max(height, minHeight), maxHeight()));
}

function maxHeight(): number {
    return Math.max(minHeight, window.innerHeight - topGap);
}

function onHandleKeyDown(state: ConsoleState, event: KeyboardEvent): void {
    const current = state.root?.getBoundingClientRect().height ?? minHeight;
    const step = event.shiftKey ? largeKeyStep : keyStep;
    const heights: Record<string, number> = { ArrowUp: current + step, ArrowDown: current - step, Home: minHeight, End: maxHeight() };
    if (event.key in heights) {
        event.preventDefault();
        resize(state, heights[event.key]);
    }
}

function onHandlePointerDown(state: ConsoleState, event: PointerEvent): void {
    const handle = state.handle;
    if (event.button !== 0 || !handle || !state.root) {
        return;
    }

    event.preventDefault();
    const startY = event.clientY;
    const startHeight = state.root.getBoundingClientRect().height;
    const move = (moveEvent: PointerEvent) => {
        state.height = clampHeight(startHeight + startY - moveEvent.clientY);
        applyHeight(state);
    };
    const end = () => {
        handle.removeEventListener('pointermove', move);
        handle.removeEventListener('pointerup', end);
        handle.removeEventListener('pointercancel', end);
        saveState(true, state.height);
    };
    handle.setPointerCapture?.(event.pointerId);
    handle.addEventListener('pointermove', move);
    handle.addEventListener('pointerup', end);
    handle.addEventListener('pointercancel', end);
}

function onResized(state: ConsoleState): void {
    updateInset(state);
    updateHandle(state);
    // Resizing changes the list's height; a reader following the newest entries keeps following them.
    if (state.atBottom) {
        jumpToBottom(state);
    }
}

// Sets the sheet height, or restores the default when height is undefined, and remembers it. Only a shown, expanded console has a handle.
function resize(state: ConsoleState, height: number | undefined): void {
    state.height = height === undefined ? undefined : clampHeight(height);
    applyHeight(state);
    saveState(true, state.height);
}

function updateHandle(state: ConsoleState): void {
    const handle = state.handle;
    if (!handle || !state.root) {
        return;
    }

    const height = Math.round(state.root.getBoundingClientRect().height);
    handle.setAttribute('aria-valuemin', String(minHeight));
    handle.setAttribute('aria-valuemax', String(maxHeight()));
    handle.setAttribute('aria-valuenow', String(height));
    handle.setAttribute('aria-valuetext', `${height} pixels tall`);
}

function attachList(state: ConsoleState, list: HTMLElement | null): void {
    if (state.list === list) {
        return;
    }

    state.list?.removeEventListener('scroll', state.onScroll);
    state.jump?.removeEventListener('click', state.onJump);
    state.listObserver?.disconnect();
    clearTimeout(state.busyTimer);
    state.listObserver = undefined;
    state.busyTimer = undefined;
    state.list = list;
    state.jump = list?.parentElement?.querySelector<HTMLButtonElement>('[data-nt-debug-console-jump]') ?? null;
    if (!list) {
        return;
    }

    list.addEventListener('scroll', state.onScroll, { passive: true });
    state.jump?.addEventListener('click', state.onJump);
    state.listObserver = new MutationObserver(() => onEntriesChanged(state));
    state.listObserver.observe(list, { childList: true });
    jumpToBottom(state);
}

function isAtBottom(list: HTMLElement): boolean {
    return list.scrollHeight - list.scrollTop - list.clientHeight <= bottomTolerance;
}

function jumpToBottom(state: ConsoleState): void {
    if (state.list) {
        state.list.scrollTop = state.list.scrollHeight;
        state.atBottom = true;
        markSeen(state);
    }
}

function lastSequence(list: HTMLElement): number {
    return Number((list.lastElementChild as HTMLElement | null)?.dataset.seq ?? 0) || 0;
}

function markSeen(state: ConsoleState): void {
    state.seen = state.list ? Math.max(state.seen, lastSequence(state.list)) : state.seen;
    updateJump(state);
}

function onEntriesChanged(state: ConsoleState): void {
    const list = state.list;
    if (!list) {
        return;
    }

    // Follow new entries only when the list was already at the bottom; otherwise keep the reader's place.
    if (state.atBottom) {
        jumpToBottom(state);
    }
    else {
        updateJump(state);
    }

    if (state.busyTimer === undefined) {
        list.setAttribute('aria-busy', 'true');
        state.busyTimer = setTimeout(() => {
            state.busyTimer = undefined;
            list.removeAttribute('aria-busy');
        }, announceInterval);
    }
}

function onListScrolled(state: ConsoleState): void {
    if (!state.list) {
        return;
    }

    state.atBottom = isAtBottom(state.list);
    if (state.atBottom) {
        markSeen(state);
    }
}

function updateInset(state: ConsoleState): void {
    const style = document.documentElement.style;
    if (state.root) {
        style.setProperty(insetProperty, `${Math.ceil(state.root.getBoundingClientRect().height)}px`);
    }
    else if (![...consoles.values()].some(other => other !== state && other.root)) {
        style.removeProperty(insetProperty);
    }
}

function updateJump(state: ConsoleState): void {
    const jump = state.jump;
    if (!jump || !state.list) {
        return;
    }

    // Rows are in capture order, so count back from the end until an entry the reader has already seen.
    let count = 0;
    for (let row = state.list.lastElementChild as HTMLElement | null; row && Number(row.dataset.seq) > state.seen; row = row.previousElementSibling as HTMLElement | null) {
        count++;
    }

    jump.hidden = count === 0;
    jump.textContent = count === 0 ? '' : `${count} new ${count === 1 ? 'entry' : 'entries'}`;
}
