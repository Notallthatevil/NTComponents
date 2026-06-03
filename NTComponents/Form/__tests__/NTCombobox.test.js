/**
 * @jest-environment jsdom
 */
import { jest } from '@jest/globals';
import { onDispose, onLoad, onUpdate } from '../NTCombobox.razor.js';

describe('NTCombobox browser behavior', () => {
    const defaultOptions = [
        ['design', 'Design', 'Visual work', false],
        ['engineering', 'Engineering', 'Implementation', false],
        ['qa', 'QA', 'Testing', true],
    ];
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

    function createFixture({ dotNetRef = null, options = defaultOptions, searchable = true, selected = [] } = {}) {
        const root = document.createElement('div');
        root.className = 'nt-input nt-combobox';

        const input = document.createElement('input');
        input.type = 'text';
        input.dataset.ntComboboxInput = 'true';
        input.dataset.ntComboboxListbox = 'tags-listbox';
        input.dataset.ntComboboxName = 'Tags';
        input.dataset.ntComboboxReadonly = 'false';
        input.dataset.ntComboboxSearchable = searchable ? 'true' : 'false';
        input.dataset.ntComboboxSeparator = ', ';
        input.setAttribute('role', 'combobox');
        input.setAttribute('aria-expanded', 'false');
        root.appendChild(input);

        const menu = document.createElement('div');
        menu.className = 'nt-combobox-menu';
        menu.dataset.ntComboboxMenu = 'true';
        menu.hidden = true;
        menu.setAttribute('aria-hidden', 'true');

        const list = document.createElement('ul');
        list.id = 'tags-listbox';
        list.setAttribute('role', 'listbox');
        for (const option of options) {
            list.appendChild(createOption(option[0], option[1], option[2], selected.includes(option[0]), option[3]));
        }
        menu.appendChild(list);

        const empty = document.createElement('div');
        empty.className = 'nt-combobox-empty';
        empty.hidden = true;
        menu.appendChild(empty);
        root.appendChild(menu);

        const hiddenContainer = document.createElement('span');
        hiddenContainer.dataset.ntComboboxHiddenContainer = 'true';
        root.appendChild(hiddenContainer);
        document.body.appendChild(root);

        const effectiveDotNetRef = dotNetRef ?? {
            invokeMethodAsync: jest.fn(() => Promise.resolve()),
        };

        onLoad(input, effectiveDotNetRef);
        return { dotNetRef: effectiveDotNetRef, empty, input, menu, root };
    }

    function getVisibleMenuValues(root) {
        return Array.from(root.querySelectorAll('[data-nt-combobox-option="true"]'))
            .filter(option => !option.hidden && !option.closest('.nt-combobox-list-item')?.hidden)
            .sort((left, right) => Number.parseInt(left.style.order || '0', 10) - Number.parseInt(right.style.order || '0', 10))
            .map(option => option.dataset.ntComboboxValue);
    }

    function createOption(value, label, supporting, selected = false, disabled = false) {
        const item = document.createElement('li');
        item.className = 'nt-combobox-list-item';
        const option = document.createElement('button');
        option.type = 'button';
        option.id = `option-${value}`;
        option.className = selected ? 'nt-combobox-option nt-combobox-option-selected' : 'nt-combobox-option';
        option.dataset.ntComboboxOption = 'true';
        option.dataset.ntComboboxValue = value;
        option.dataset.ntComboboxLabel = label;
        option.setAttribute('aria-selected', selected ? 'true' : 'false');
        option.disabled = disabled;

        const labelSpan = document.createElement('span');
        labelSpan.className = 'nt-combobox-option-label';
        labelSpan.textContent = label;
        option.appendChild(labelSpan);

        const supportingSpan = document.createElement('span');
        supportingSpan.className = 'nt-combobox-option-supporting';
        supportingSpan.textContent = supporting;
        option.appendChild(supportingSpan);

        const trailing = document.createElement('span');
        trailing.className = 'nt-combobox-option-trailing';
        trailing.hidden = !selected;
        option.appendChild(trailing);

        item.appendChild(option);
        return item;
    }

    afterEach(() => {
        onDispose(null);
        document.body.textContent = '';
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
    });

    test('opens from input click', () => {
        const { input, menu, root } = createFixture();

        input.dispatchEvent(new MouseEvent('click', { bubbles: true }));

        expect(root.classList.contains('nt-combobox-open')).toBe(true);
        expect(menu.hidden).toBe(false);
        expect(input.getAttribute('aria-expanded')).toBe('true');
    });

    test('typing filters options without changing the input value', () => {
        const { empty, input, root } = createFixture();

        input.dispatchEvent(new KeyboardEvent('keydown', { bubbles: true, key: 'e' }));
        input.dispatchEvent(new KeyboardEvent('keydown', { bubbles: true, key: 'n' }));
        input.dispatchEvent(new KeyboardEvent('keydown', { bubbles: true, key: 'g' }));

        const options = Array.from(root.querySelectorAll('[data-nt-combobox-option="true"]'));
        expect(input.value).toBe('');
        expect(options.map(option => option.hidden)).toEqual([true, false, true]);
        expect(empty.hidden).toBe(true);
    });

    test('filters like autocomplete with exact then starts-with then contains matches', () => {
        const options = [
            ['Austin', 'Austin', 'Texas', false],
            ['Boston', 'Boston', 'Massachusetts', false],
            ['North Austin', 'North Austin', 'Neighborhood', false],
        ];
        const { input, root } = createFixture({ options });

        for (const key of 'austin') {
            input.dispatchEvent(new KeyboardEvent('keydown', { bubbles: true, key }));
        }

        expect(getVisibleMenuValues(root)).toEqual(['Austin', 'North Austin']);
    });

    test('does not match supporting text while filtering', () => {
        const { empty, input, root } = createFixture();

        for (const key of 'visual') {
            input.dispatchEvent(new KeyboardEvent('keydown', { bubbles: true, key }));
        }

        expect(getVisibleMenuValues(root)).toEqual([]);
        expect(empty.hidden).toBe(false);
    });

    test('input events cannot persist arbitrary typed values', () => {
        const { input } = createFixture({ selected: ['design'] });

        input.value = 'arbitrary';
        input.dispatchEvent(new InputEvent('beforeinput', { bubbles: true, cancelable: true, data: 'x' }));
        input.dispatchEvent(new Event('input', { bubbles: true }));

        expect(input.value).toBe('Design');
    });

    test('toggles selected values and updates hidden form inputs', async () => {
        const { dotNetRef, input, root } = createFixture();
        input.dispatchEvent(new MouseEvent('click', { bubbles: true }));

        root.querySelector('[data-nt-combobox-value="design"]').dispatchEvent(new MouseEvent('click', { bubbles: true }));
        await Promise.resolve();

        expect(input.value).toBe('Design');
        expect(root.querySelector('[data-nt-combobox-value="design"]').getAttribute('aria-selected')).toBe('true');
        expect(root.querySelector('[data-nt-combobox-hidden-container="true"] input').value).toBe('design');
        expect(dotNetRef.invokeMethodAsync).toHaveBeenLastCalledWith('NotifyComboboxSelectionChanged', ['design']);

        root.querySelector('[data-nt-combobox-value="design"]').dispatchEvent(new MouseEvent('click', { bubbles: true }));
        await Promise.resolve();

        expect(input.value).toBe('');
        expect(root.querySelectorAll('[data-nt-combobox-hidden-container="true"] input')).toHaveLength(0);
        expect(dotNetRef.invokeMethodAsync).toHaveBeenLastCalledWith('NotifyComboboxSelectionChanged', []);
    });

    test('selection resets the internal filter to an empty string', async () => {
        const { input, root } = createFixture();

        for (const key of 'eng') {
            input.dispatchEvent(new KeyboardEvent('keydown', { bubbles: true, key }));
        }

        expect(getVisibleMenuValues(root)).toEqual(['engineering']);

        root.querySelector('[data-nt-combobox-value="engineering"]').dispatchEvent(new MouseEvent('click', { bubbles: true }));
        await Promise.resolve();

        expect(input.value).toBe('Engineering');
        expect(getVisibleMenuValues(root)).toEqual(['design', 'engineering', 'qa']);
    });

    test('disabled options do not toggle', () => {
        const { dotNetRef, input, root } = createFixture();
        input.dispatchEvent(new MouseEvent('click', { bubbles: true }));

        root.querySelector('[data-nt-combobox-value="qa"]').dispatchEvent(new MouseEvent('click', { bubbles: true }));

        expect(dotNetRef.invokeMethodAsync).not.toHaveBeenCalledWith('NotifyComboboxSelectionChanged', expect.anything());
        expect(root.querySelector('[data-nt-combobox-value="qa"]').getAttribute('aria-selected')).toBe('false');
    });

    test('space toggles the active option', async () => {
        const { dotNetRef, input, root } = createFixture();

        input.dispatchEvent(new MouseEvent('click', { bubbles: true }));
        input.dispatchEvent(new KeyboardEvent('keydown', { bubbles: true, key: ' ' }));
        await Promise.resolve();

        expect(root.querySelector('[data-nt-combobox-value="design"]').getAttribute('aria-selected')).toBe('true');
        expect(dotNetRef.invokeMethodAsync).toHaveBeenLastCalledWith('NotifyComboboxSelectionChanged', ['design']);
    });

    test('home and end move the active option while open', () => {
        const { input, root } = createFixture();

        input.dispatchEvent(new MouseEvent('click', { bubbles: true }));
        input.dispatchEvent(new KeyboardEvent('keydown', { bubbles: true, key: 'End' }));

        const engineering = root.querySelector('[data-nt-combobox-value="engineering"]');
        expect(engineering.classList.contains('nt-combobox-option-active')).toBe(true);
        expect(input.getAttribute('aria-activedescendant')).toBe(engineering.id);

        input.dispatchEvent(new KeyboardEvent('keydown', { bubbles: true, key: 'Home' }));

        const design = root.querySelector('[data-nt-combobox-value="design"]');
        expect(design.classList.contains('nt-combobox-option-active')).toBe(true);
        expect(input.getAttribute('aria-activedescendant')).toBe(design.id);
    });

    test('arrow navigation scrolls the active option into view', () => {
        const options = Array.from({ length: 12 }, (_, index) => [`value-${index + 1}`, `Value ${index + 1}`, 'Supporting text', false]);
        const { input } = createFixture({ options });

        input.dispatchEvent(new KeyboardEvent('keydown', { bubbles: true, key: 'ArrowDown' }));
        input.dispatchEvent(new KeyboardEvent('keydown', { bubbles: true, key: 'ArrowDown' }));

        expect(scrollIntoViewMock).toHaveBeenCalledWith({ block: 'nearest' });
    });

    test('opens above and constrains menu height when near the bottom of the viewport', () => {
        const { input, menu, root } = createFixture();
        Object.defineProperty(window, 'innerHeight', { configurable: true, value: 760 });
        root.getBoundingClientRect = () => ({
            bottom: 740,
            height: 40,
            left: 0,
            right: 300,
            top: 700,
            width: 300,
            x: 0,
            y: 700,
            toJSON: () => ({}),
        });

        input.dispatchEvent(new MouseEvent('click', { bubbles: true }));

        expect(menu.classList.contains('nt-combobox-menu-above')).toBe(true);
        expect(menu.style.maxHeight).toBe('320px');
    });

    test('backspace narrows the internal typeahead filter without editing selected display text', () => {
        const { input, root } = createFixture({ selected: ['design'] });

        input.dispatchEvent(new KeyboardEvent('keydown', { bubbles: true, key: 'e' }));
        input.dispatchEvent(new KeyboardEvent('keydown', { bubbles: true, key: 'n' }));
        input.dispatchEvent(new KeyboardEvent('keydown', { bubbles: true, key: 'g' }));
        input.dispatchEvent(new KeyboardEvent('keydown', { bubbles: true, key: 'Backspace' }));

        const options = Array.from(root.querySelectorAll('[data-nt-combobox-option="true"]'));
        expect(input.value).toBe('Design');
        expect(options.map(option => option.hidden)).toEqual([true, false, true]);
    });

    test('closes and notifies touched on outside click', async () => {
        const { dotNetRef, input, menu } = createFixture();
        input.dispatchEvent(new MouseEvent('click', { bubbles: true }));

        document.body.dispatchEvent(new MouseEvent('mousedown', { bubbles: true }));
        await Promise.resolve();

        expect(menu.hidden).toBe(true);
        expect(dotNetRef.invokeMethodAsync).toHaveBeenLastCalledWith('NotifyComboboxTouched');
    });

    test('onUpdate reconnects rerendered menu content', () => {
        const { input, menu, root } = createFixture();
        const replacementMenu = menu.cloneNode(true);
        menu.replaceWith(replacementMenu);

        onUpdate(input, null);
        input.dispatchEvent(new MouseEvent('click', { bubbles: true }));

        expect(replacementMenu.hidden).toBe(false);
        expect(root.classList.contains('nt-combobox-open')).toBe(true);
    });

    test('onUpdate refreshes cached option metadata after rerender', () => {
        const { input, menu, root } = createFixture();
        const replacementMenu = menu.cloneNode(true);
        const replacementOption = replacementMenu.querySelector('[data-nt-combobox-value="engineering"]');
        replacementOption.dataset.ntComboboxLabel = 'Build';
        replacementOption.querySelector('.nt-combobox-option-label').textContent = 'Build';
        replacementOption.querySelector('.nt-combobox-option-supporting').textContent = 'Delivery';
        menu.replaceWith(replacementMenu);

        onUpdate(input, null);
        for (const key of 'bui') {
            input.dispatchEvent(new KeyboardEvent('keydown', { bubbles: true, key }));
        }

        const options = Array.from(root.querySelectorAll('[data-nt-combobox-option="true"]'));
        expect(options.map(option => option.hidden)).toEqual([true, false, true]);
    });

    test('does not treat selected display text as the open filter', () => {
        const { empty, input, root } = createFixture({ selected: ['design', 'engineering'] });

        expect(input.value).toBe('Design, Engineering');
        input.dispatchEvent(new MouseEvent('click', { bubbles: true }));

        const options = Array.from(root.querySelectorAll('[data-nt-combobox-option="true"]'));
        expect(options.map(option => option.hidden)).toEqual([false, false, false]);
        expect(empty.hidden).toBe(true);
    });

    test('ignores synchronous interop failures during rapid selection', () => {
        const dotNetRef = {
            invokeMethodAsync: jest.fn(() => {
                throw new Error('transport busy');
            }),
        };
        const { input, root } = createFixture({ dotNetRef });

        input.dispatchEvent(new MouseEvent('click', { bubbles: true }));
        expect(() => {
            root.querySelector('[data-nt-combobox-value="design"]').dispatchEvent(new MouseEvent('click', { bubbles: true }));
            root.querySelector('[data-nt-combobox-value="engineering"]').dispatchEvent(new MouseEvent('click', { bubbles: true }));
        }).not.toThrow();

        expect(root.querySelector('[data-nt-combobox-value="design"]').getAttribute('aria-selected')).toBe('true');
        expect(root.querySelector('[data-nt-combobox-value="engineering"]').getAttribute('aria-selected')).toBe('true');
    });
});
