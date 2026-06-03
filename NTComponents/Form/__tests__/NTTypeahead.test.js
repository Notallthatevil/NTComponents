/**
 * @jest-environment jsdom
 */
import { jest } from '@jest/globals';
import { onDispose, onLoad, scrollActiveOptionIntoView } from '../NTTypeahead.razor.js';

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
        jest.clearAllMocks();
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

    function createFixture() {
        const root = document.createElement('div');
        root.className = 'nt-input nt-typeahead';

        const input = document.createElement('input');
        input.dataset.ntTypeaheadInput = 'true';
        input.setAttribute('role', 'combobox');
        root.appendChild(input);

        const menu = document.createElement('div');
        menu.className = 'nt-combobox-menu';

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

        return { input, root, second };
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
