/**
 * @jest-environment jsdom
 */
import { jest } from '@jest/globals';
import { onDispose, onLoad, onUpdate, scrollActiveOptionIntoView } from '../NTTypeahead.razor.js';

describe('NTTypeahead browser behavior', () => {
    let originalScrollIntoView;
    let scrollIntoViewMock;

    beforeEach(() => {
        originalScrollIntoView = HTMLElement.prototype.scrollIntoView;
        scrollIntoViewMock = jest.fn();
        Object.defineProperty(HTMLElement.prototype, 'scrollIntoView', {
            configurable: true,
            value: scrollIntoViewMock,
        });
    });

    afterEach(() => {
        jest.restoreAllMocks();
        if (originalScrollIntoView) {
            Object.defineProperty(HTMLElement.prototype, 'scrollIntoView', {
                configurable: true,
                value: originalScrollIntoView,
            });
        }
        else {
            delete HTMLElement.prototype.scrollIntoView;
        }

        document.body.innerHTML = '';
    });

    function createFixture(rect = {
        bottom: 120,
        height: 48,
        left: 32,
        right: 272,
        top: 72,
        width: 240,
        x: 32,
        y: 72,
    }) {
        const root = document.createElement('div');
        root.className = 'nt-input nt-typeahead';
        root.getBoundingClientRect = () => ({ ...rect, toJSON: () => undefined });

        const input = document.createElement('input');
        input.dataset.ntTypeaheadInput = 'true';
        input.setAttribute('role', 'combobox');
        input.setAttribute('aria-expanded', 'false');
        root.appendChild(input);

        const menu = document.createElement('div');
        menu.className = 'nt-combobox-menu';
        menu.dataset.ntTypeaheadMenu = 'true';
        menu.hidden = true;
        menu.setAttribute('aria-hidden', 'true');
        menu.setAttribute('popover', 'manual');

        const list = document.createElement('ul');
        list.className = 'nt-combobox-list';
        menu.appendChild(list);

        const first = document.createElement('button');
        first.id = 'city-option-1';
        first.className = 'nt-combobox-option';
        list.appendChild(first);

        const second = document.createElement('button');
        second.id = 'city-option-2';
        second.className = 'nt-combobox-option nt-combobox-option-active';
        list.appendChild(second);

        root.appendChild(menu);
        document.body.appendChild(root);

        return { input, menu, root, second };
    }

    test('prevents enter submit while the popup is expanded', () => {
        const { input } = createFixture();
        input.setAttribute('aria-expanded', 'true');
        onLoad(input);

        const event = new KeyboardEvent('keydown', { bubbles: true, cancelable: true, key: 'Enter' });
        input.dispatchEvent(event);

        expect(event.defaultPrevented).toBe(true);
    });

    test('does not prevent enter submit after dispose', () => {
        const { input } = createFixture();
        input.setAttribute('aria-expanded', 'true');
        onLoad(input);
        onDispose(input);

        const event = new KeyboardEvent('keydown', { bubbles: true, cancelable: true, key: 'Enter' });
        input.dispatchEvent(event);

        expect(event.defaultPrevented).toBe(false);
    });

    test('opens manual popover menu as fixed viewport layer on load', () => {
        const { input, menu } = createFixture();
        const showPopover = jest.fn();
        menu.showPopover = showPopover;
        menu.matches = jest.fn(() => false);
        input.setAttribute('aria-expanded', 'true');
        menu.hidden = false;

        onLoad(input);

        expect(showPopover).toHaveBeenCalledTimes(1);
        expect(menu.classList.contains('nt-combobox-menu-layer')).toBe(true);
        expect(menu.style.left).toBe('32px');
        expect(menu.style.top).toBe('124px');
        expect(menu.style.width).toBe('240px');
        expect(menu.style.maxHeight).toBe('320px');
    });

    test('opens manual popover menu as fixed viewport layer after async update', () => {
        const { input, menu } = createFixture();
        const showPopover = jest.fn();
        menu.showPopover = showPopover;
        menu.matches = jest.fn(() => false);

        onLoad(input);
        input.setAttribute('aria-expanded', 'true');
        menu.hidden = false;
        menu.setAttribute('aria-hidden', 'false');
        onUpdate(input);

        expect(showPopover).toHaveBeenCalledTimes(1);
        expect(menu.classList.contains('nt-combobox-menu-layer')).toBe(true);
        expect(menu.style.left).toBe('32px');
        expect(menu.style.top).toBe('124px');
        expect(menu.style.width).toBe('240px');
        expect(menu.style.maxHeight).toBe('320px');
    });

    test('attaches scroll and resize listeners only while menu is open', () => {
        const { input, menu } = createFixture();
        const documentAdd = jest.spyOn(document, 'addEventListener');
        const documentRemove = jest.spyOn(document, 'removeEventListener');
        const windowAdd = jest.spyOn(window, 'addEventListener');
        const windowRemove = jest.spyOn(window, 'removeEventListener');

        onLoad(input);

        expect(documentAdd).not.toHaveBeenCalledWith('scroll', expect.any(Function), true);
        expect(windowAdd).not.toHaveBeenCalledWith('resize', expect.any(Function));

        input.setAttribute('aria-expanded', 'true');
        menu.hidden = false;
        onUpdate(input);

        expect(documentAdd).toHaveBeenCalledWith('scroll', expect.any(Function), true);
        expect(windowAdd).toHaveBeenCalledWith('resize', expect.any(Function));

        input.setAttribute('aria-expanded', 'false');
        menu.hidden = true;
        onUpdate(input);

        expect(documentRemove).toHaveBeenCalledWith('scroll', expect.any(Function), true);
        expect(windowRemove).toHaveBeenCalledWith('resize', expect.any(Function));
    });

    test('places menu above when there is more viewport space above the field', () => {
        const { input, menu } = createFixture({
            bottom: 728,
            height: 48,
            left: 40,
            right: 280,
            top: 680,
            width: 240,
            x: 40,
            y: 680,
        });
        input.setAttribute('aria-expanded', 'true');
        menu.hidden = false;

        onLoad(input);

        expect(menu.classList.contains('nt-combobox-menu-above')).toBe(true);
        expect(menu.style.top).toBe('356px');
    });

    test('hides manual popover menu and clears viewport placement', () => {
        const { input, menu } = createFixture();
        const hidePopover = jest.fn();
        menu.hidePopover = hidePopover;
        menu.matches = jest.fn(selector => selector === ':popover-open');
        input.setAttribute('aria-expanded', 'true');
        menu.hidden = false;
        onLoad(input);

        input.setAttribute('aria-expanded', 'false');
        menu.hidden = true;
        onUpdate(input);

        expect(hidePopover).toHaveBeenCalledTimes(1);
        expect(menu.classList.contains('nt-combobox-menu-layer')).toBe(false);
        expect(menu.style.left).toBe('');
        expect(menu.style.top).toBe('');
        expect(menu.style.width).toBe('');
        expect(menu.style.maxHeight).toBe('');
    });

    test('scrolls active option when invoked with input element reference', () => {
        const { input, second } = createFixture();

        scrollActiveOptionIntoView(input, second.id);

        expect(scrollIntoViewMock).toHaveBeenCalledWith({ block: 'nearest' });
    });

    test('does nothing when active option is missing', () => {
        const { input } = createFixture();

        scrollActiveOptionIntoView(input, 'missing-option');

        expect(scrollIntoViewMock).not.toHaveBeenCalled();
    });
});
