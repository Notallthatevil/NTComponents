import { copyText, insetProperty, matchesShortcut, minHeight, onDispose, onLoad, onUpdate, parseShortcut, readStoredState, saveState, storageKey } from '../NTDebugConsole.razor.js';
import { jest } from '@jest/globals';

const id = 'console-1';

function createReference() {
    return { invokeMethodAsync: jest.fn(() => Promise.resolve()) };
}

// jsdom has no layout, so each list reports the scroll geometry the test gives it.
function createConsole({ rows = 0, clientHeight = 100, rowHeight = 20 } = {}) {
    const root = document.createElement('section');
    root.getBoundingClientRect = () => ({ height: 48 });
    const body = document.createElement('div');
    const list = document.createElement('div');
    list.setAttribute('data-nt-debug-console-list', '');
    const jump = document.createElement('button');
    jump.setAttribute('data-nt-debug-console-jump', '');
    jump.hidden = true;
    body.append(list, jump);
    root.append(body);
    document.body.append(root);

    let scrollTop = 0;
    Object.defineProperty(list, 'clientHeight', { configurable: true, get: () => clientHeight });
    Object.defineProperty(list, 'scrollHeight', { configurable: true, get: () => list.children.length * rowHeight });
    Object.defineProperty(list, 'scrollTop', {
        configurable: true,
        get: () => scrollTop,
        set: value => { scrollTop = Math.max(0, Math.min(value, list.scrollHeight - clientHeight)); }
    });

    let sequence = 0;
    const addRows = count => {
        for (let i = 0; i < count; i++) {
            const row = document.createElement('div');
            row.dataset.seq = String(++sequence);
            list.append(row);
        }
    };
    addRows(rows);
    return { root, list, jump, addRows };
}

function scrollTo(list, value) {
    list.scrollTop = value;
    list.dispatchEvent(new Event('scroll'));
}

const mutationsDelivered = () => new Promise(resolve => setTimeout(resolve, 0));

function pressKey(init) {
    const event = new KeyboardEvent('keydown', { bubbles: true, cancelable: true, ...init });
    document.dispatchEvent(event);
    return event;
}

describe('NTDebugConsole scrolling', () => {
    beforeEach(() => {
        document.body.innerHTML = '';
        localStorage.clear();
    });

    afterEach(() => onDispose(id));

    test('opens scrolled to the newest entry', () => {
        const { root, list } = createConsole({ rows: 20 });

        onLoad(id, root, createReference(), 'Ctrl+`');

        expect(list.scrollTop).toBe(list.scrollHeight - list.clientHeight);
    });

    test('follows new entries while the list is at the bottom', async () => {
        const { root, list, jump, addRows } = createConsole({ rows: 20 });
        onLoad(id, root, createReference(), 'Ctrl+`');

        addRows(3);
        await mutationsDelivered();

        expect(list.scrollTop).toBe(list.scrollHeight - list.clientHeight);
        expect(jump.hidden).toBe(true);
    });

    test('counts within a few pixels of the end as the bottom', async () => {
        const { root, list, addRows } = createConsole({ rows: 20 });
        onLoad(id, root, createReference(), 'Ctrl+`');
        scrollTo(list, list.scrollHeight - list.clientHeight - 3);

        addRows(1);
        await mutationsDelivered();

        expect(list.scrollTop).toBe(list.scrollHeight - list.clientHeight);
    });

    test('keeps its position and offers a jump button when scrolled up', async () => {
        const { root, list, jump, addRows } = createConsole({ rows: 20 });
        onLoad(id, root, createReference(), 'Ctrl+`');
        scrollTo(list, 40);

        addRows(3);
        await mutationsDelivered();

        expect(list.scrollTop).toBe(40);
        expect(jump.hidden).toBe(false);
        expect(jump.textContent).toBe('3 new entries');
    });

    test('names a single new entry in the singular', async () => {
        const { root, list, jump, addRows } = createConsole({ rows: 20 });
        onLoad(id, root, createReference(), 'Ctrl+`');
        scrollTo(list, 0);

        addRows(1);
        await mutationsDelivered();

        expect(jump.textContent).toBe('1 new entry');
    });

    test('the jump button scrolls to the bottom and hides itself', async () => {
        const { root, list, jump, addRows } = createConsole({ rows: 20 });
        onLoad(id, root, createReference(), 'Ctrl+`');
        scrollTo(list, 0);
        addRows(2);
        await mutationsDelivered();

        jump.click();

        expect(list.scrollTop).toBe(list.scrollHeight - list.clientHeight);
        expect(jump.hidden).toBe(true);
    });

    test('scrolling back to the bottom marks the new entries as seen', async () => {
        const { root, list, jump, addRows } = createConsole({ rows: 20 });
        onLoad(id, root, createReference(), 'Ctrl+`');
        scrollTo(list, 0);
        addRows(2);
        await mutationsDelivered();

        scrollTo(list, list.scrollHeight);

        expect(jump.hidden).toBe(true);
    });

    test('rows a filter shows again are not counted as new', async () => {
        const { root, list, jump } = createConsole({ rows: 20 });
        onLoad(id, root, createReference(), 'Ctrl+`');
        scrollTo(list, 0);
        const hidden = list.firstElementChild;

        hidden.remove();
        list.prepend(hidden);
        await mutationsDelivered();

        expect(jump.hidden).toBe(true);
    });

    test('marks the log busy so screen readers announce a burst once', async () => {
        jest.useFakeTimers();
        try {
            const { root, list, addRows } = createConsole({ rows: 5 });
            onLoad(id, root, createReference(), 'Ctrl+`');

            addRows(1);
            await Promise.resolve();
            jest.advanceTimersByTime(0);
            expect(list.getAttribute('aria-busy')).toBe('true');

            jest.advanceTimersByTime(2000);
            expect(list.hasAttribute('aria-busy')).toBe(false);
        }
        finally {
            jest.useRealTimers();
        }
    });

    test('attaches to the list when the sheet is expanded later', async () => {
        const collapsed = document.createElement('section');
        document.body.append(collapsed);
        onLoad(id, collapsed, createReference(), 'Ctrl+`');
        const { root, list, jump, addRows } = createConsole({ rows: 20 });

        onUpdate(id, root, 'Ctrl+`', true);
        scrollTo(list, 0);
        addRows(1);
        await mutationsDelivered();

        expect(jump.hidden).toBe(false);
    });
});

describe('NTDebugConsole shortcut', () => {
    beforeEach(() => {
        document.body.innerHTML = '';
        localStorage.clear();
    });

    afterEach(() => onDispose(id));

    test('Ctrl+` toggles visibility by default', () => {
        const reference = createReference();
        onLoad(id, null, reference, 'Ctrl+`');

        const event = pressKey({ key: '`', code: 'Backquote', ctrlKey: true });

        expect(reference.invokeMethodAsync).toHaveBeenCalledWith('ToggleVisibleAsync');
        expect(event.defaultPrevented).toBe(true);
    });

    test('matches the physical key on layouts that type another character', () => {
        const reference = createReference();
        onLoad(id, null, reference, 'Ctrl+`');

        pressKey({ key: '^', code: 'Backquote', ctrlKey: true });

        expect(reference.invokeMethodAsync).toHaveBeenCalledWith('ToggleVisibleAsync');
    });

    test('ignores the key without its modifiers, with extra modifiers, and while repeating', () => {
        const reference = createReference();
        onLoad(id, null, reference, 'Ctrl+`');

        pressKey({ key: '`', code: 'Backquote' });
        pressKey({ key: '`', code: 'Backquote', ctrlKey: true, shiftKey: true });
        pressKey({ key: '`', code: 'Backquote', ctrlKey: true, repeat: true });

        expect(reference.invokeMethodAsync).not.toHaveBeenCalled();
    });

    test('uses a configured shortcut and applies a changed one', () => {
        const reference = createReference();
        onLoad(id, null, reference, 'Ctrl+Shift+L');

        pressKey({ key: 'L', code: 'KeyL', ctrlKey: true, shiftKey: true });
        onUpdate(id, null, 'Alt+D', false);
        pressKey({ key: 'L', code: 'KeyL', ctrlKey: true, shiftKey: true });
        pressKey({ key: 'd', code: 'KeyD', altKey: true });

        expect(reference.invokeMethodAsync).toHaveBeenCalledTimes(2);
    });

    test('an empty shortcut turns the shortcut off', () => {
        const reference = createReference();
        onLoad(id, null, reference, '');

        pressKey({ key: '`', code: 'Backquote', ctrlKey: true });

        expect(reference.invokeMethodAsync).not.toHaveBeenCalled();
    });

    test('parses modifiers and the plus key', () => {
        expect(parseShortcut('Control+Alt+Meta+K')).toEqual({ alt: true, ctrl: true, key: 'k', meta: true, shift: false });
        expect(parseShortcut('Ctrl++')).toEqual({ alt: false, ctrl: true, key: '+', meta: false, shift: false });
        expect(parseShortcut('  ')).toBeNull();
        expect(matchesShortcut(null, new KeyboardEvent('keydown', { key: '`' }))).toBe(false);
    });
});

describe('NTDebugConsole persistence', () => {
    beforeEach(() => {
        document.body.innerHTML = '';
        localStorage.clear();
    });

    afterEach(() => {
        onDispose(id);
        jest.restoreAllMocks();
    });

    test('restores the remembered expansion on load and ignores a stored visibility', () => {
        // Older versions stored visibility; a console hidden that way must not stay hidden with no way to show it.
        localStorage.setItem(storageKey, JSON.stringify({ visible: false, expanded: true }));
        const reference = createReference();

        onLoad(id, null, reference, 'Ctrl+`');

        expect(reference.invokeMethodAsync).toHaveBeenCalledWith('RestoreStateAsync', true);
        expect(reference.invokeMethodAsync).toHaveBeenCalledTimes(1);
    });

    test('does not restore anything when nothing valid is stored', () => {
        localStorage.setItem(storageKey, '{"visible":"yes"}');
        const reference = createReference();

        onLoad(id, null, reference, 'Ctrl+`');

        expect(reference.invokeMethodAsync).not.toHaveBeenCalled();
    });

    test('remembers the state on every update', () => {
        onLoad(id, null, createReference(), 'Ctrl+`');

        onUpdate(id, null, 'Ctrl+`', true);

        expect(JSON.parse(localStorage.getItem(storageKey))).toEqual({ expanded: true });
    });

    test('works when storage throws', () => {
        jest.spyOn(Storage.prototype, 'getItem').mockImplementation(() => { throw new Error('SecurityError'); });
        jest.spyOn(Storage.prototype, 'setItem').mockImplementation(() => { throw new Error('QuotaExceededError'); });
        const reference = createReference();

        expect(() => {
            onLoad(id, null, reference, 'Ctrl+`');
            onUpdate(id, null, 'Ctrl+`', false);
        }).not.toThrow();
        expect(readStoredState()).toBeNull();
        expect(() => saveState(true)).not.toThrow();
        expect(reference.invokeMethodAsync).not.toHaveBeenCalled();
    });
});

describe('NTDebugConsole page inset and disposal', () => {
    beforeEach(() => {
        document.body.innerHTML = '';
        localStorage.clear();
        document.documentElement.style.removeProperty(insetProperty);
    });

    afterEach(() => onDispose(id));

    test('exposes its height on the document root while shown', () => {
        const { root } = createConsole();

        onLoad(id, root, createReference(), 'Ctrl+`');

        expect(document.documentElement.style.getPropertyValue(insetProperty)).toBe('48px');
    });

    test('removes the inset when hidden', () => {
        const { root } = createConsole();
        onLoad(id, root, createReference(), 'Ctrl+`');

        onUpdate(id, null, 'Ctrl+`', false);

        expect(document.documentElement.style.getPropertyValue(insetProperty)).toBe('');
    });

    test('disposing removes the shortcut, the observers, and the inset', async () => {
        const reference = createReference();
        const { root, list, jump, addRows } = createConsole({ rows: 20 });
        onLoad(id, root, reference, 'Ctrl+`');
        scrollTo(list, 0);

        onDispose(id);
        pressKey({ key: '`', code: 'Backquote', ctrlKey: true });
        addRows(2);
        await mutationsDelivered();

        expect(reference.invokeMethodAsync).not.toHaveBeenCalled();
        expect(jump.hidden).toBe(true);
        expect(document.documentElement.style.getPropertyValue(insetProperty)).toBe('');
    });

    test('ignores updates and disposal for an unknown console', () => {
        expect(() => {
            onUpdate('missing', null, 'Ctrl+`', false);
            onDispose('missing');
        }).not.toThrow();
    });
});

describe('NTDebugConsole copy', () => {
    const originalClipboard = navigator.clipboard;

    afterEach(() => {
        Object.defineProperty(navigator, 'clipboard', { configurable: true, value: originalClipboard });
    });

    test('copies text to the clipboard', async () => {
        const writeText = jest.fn(() => Promise.resolve());
        Object.defineProperty(navigator, 'clipboard', { configurable: true, value: { writeText } });

        await expect(copyText('12:00:00.000 INFO [App] Saved')).resolves.toBe(true);
        expect(writeText).toHaveBeenCalledWith('12:00:00.000 INFO [App] Saved');
    });

    test('reports failure when the clipboard is unavailable', async () => {
        Object.defineProperty(navigator, 'clipboard', { configurable: true, value: undefined });

        await expect(copyText('text')).resolves.toBe(false);
    });
});

describe('NTDebugConsole resizing', () => {
    // jsdom has no layout, so the sheet reports its inline height, or a 320px default.
    function createExpandedConsole() {
        const console = createConsole();
        console.root.getBoundingClientRect = () => ({ height: parseFloat(console.root.style.blockSize) || 320 });
        const handle = document.createElement('div');
        handle.setAttribute('data-nt-debug-console-resize', '');
        console.root.prepend(handle);
        return { ...console, handle };
    }

    function pointer(target, type, clientY) {
        const PointerCtor = window.PointerEvent ?? MouseEvent;
        target.dispatchEvent(new PointerCtor(type, { bubbles: true, cancelable: true, button: 0, clientY }));
    }

    function pressHandle(handle, init) {
        const event = new KeyboardEvent('keydown', { bubbles: true, cancelable: true, ...init });
        handle.dispatchEvent(event);
        return event;
    }

    const stored = () => JSON.parse(localStorage.getItem(storageKey));

    beforeEach(() => {
        document.body.innerHTML = '';
        localStorage.clear();
        window.innerHeight = 768;
    });

    afterEach(() => onDispose(id));

    test('dragging the handle up grows the sheet and remembers the height', () => {
        const { root, handle } = createExpandedConsole();
        onLoad(id, root, createReference(), 'Ctrl+`');

        pointer(handle, 'pointerdown', 500);
        pointer(handle, 'pointermove', 400);
        pointer(handle, 'pointerup', 400);

        expect(root.style.blockSize).toBe('420px');
        expect(handle.getAttribute('aria-valuenow')).toBe('420');
        expect(stored()).toEqual({ expanded: true, height: 420 });
    });

    test('dragging stops at the minimum and the viewport limit', () => {
        const { root, handle } = createExpandedConsole();
        onLoad(id, root, createReference(), 'Ctrl+`');

        pointer(handle, 'pointerdown', 500);
        pointer(handle, 'pointermove', 1000);
        expect(root.style.blockSize).toBe(`${minHeight}px`);

        pointer(handle, 'pointermove', -1000);
        pointer(handle, 'pointerup', -1000);
        expect(root.style.blockSize).toBe('704px');
        expect(handle.getAttribute('aria-valuemax')).toBe('704');
    });

    test('moving the pointer after release does not resize', () => {
        const { root, handle } = createExpandedConsole();
        onLoad(id, root, createReference(), 'Ctrl+`');

        pointer(handle, 'pointerdown', 500);
        pointer(handle, 'pointerup', 500);
        pointer(handle, 'pointermove', 300);

        // A press without movement keeps the default size, and later moves change nothing.
        expect(root.style.blockSize).toBe('');
    });

    test('arrow keys, Home, and End resize from the keyboard', () => {
        const { root, handle } = createExpandedConsole();
        onLoad(id, root, createReference(), 'Ctrl+`');

        expect(pressHandle(handle, { key: 'ArrowUp' }).defaultPrevented).toBe(true);
        expect(root.style.blockSize).toBe('336px');
        pressHandle(handle, { key: 'ArrowDown', shiftKey: true });
        expect(root.style.blockSize).toBe('272px');
        pressHandle(handle, { key: 'Home' });
        expect(root.style.blockSize).toBe(`${minHeight}px`);
        pressHandle(handle, { key: 'End' });
        expect(root.style.blockSize).toBe('704px');
        expect(pressHandle(handle, { key: 'Tab' }).defaultPrevented).toBe(false);
        expect(stored().height).toBe(704);
    });

    test('double-clicking the handle restores the default height', () => {
        localStorage.setItem(storageKey, JSON.stringify({ expanded: true, height: 500 }));
        const { root, handle } = createExpandedConsole();
        onLoad(id, root, createReference(), 'Ctrl+`');
        expect(root.style.blockSize).toBe('500px');

        handle.dispatchEvent(new MouseEvent('dblclick', { bubbles: true }));

        expect(root.style.blockSize).toBe('');
        expect(stored()).toEqual({ expanded: true });
    });

    test('the remembered height applies only while expanded and survives updates', () => {
        localStorage.setItem(storageKey, JSON.stringify({ expanded: false, height: 500 }));
        const { root, handle } = createExpandedConsole();
        handle.remove();
        onLoad(id, root, createReference(), 'Ctrl+`');
        expect(root.style.blockSize).toBe('');

        root.prepend(handle);
        onUpdate(id, root, 'Ctrl+`', true);
        expect(root.style.blockSize).toBe('500px');

        handle.remove();
        onUpdate(id, root, 'Ctrl+`', false);
        expect(root.style.blockSize).toBe('');
        expect(stored()).toEqual({ expanded: false, height: 500 });
    });

    test('ignores a stored height that is not a positive number', () => {
        localStorage.setItem(storageKey, JSON.stringify({ expanded: true, height: 'tall' }));

        expect(readStoredState()).toEqual({ expanded: true });
    });

    test('disposing detaches the handle', () => {
        const { root, handle } = createExpandedConsole();
        onLoad(id, root, createReference(), 'Ctrl+`');
        onDispose(id);

        pressHandle(handle, { key: 'ArrowUp' });

        expect(root.style.blockSize).toBe('');
    });
});
