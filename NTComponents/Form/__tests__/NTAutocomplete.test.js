/**
 * @jest-environment jsdom
 */
import { jest } from '@jest/globals';
import { onDispose, onLoad, onUpdate } from '../NTAutocomplete.razor.js';

describe('NTAutocomplete browser behavior', () => {
    const defaultOptions = [
        ['Austin', 'Austin', 'Texas'],
        ['Boston', 'Boston', 'Massachusetts'],
        ['New York', 'New York', 'New York'],
        ['North Austin', 'North Austin', 'Neighborhood'],
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

    function createFixture({ allowCustomValue = true, value = '', dotNetRef = undefined, options = defaultOptions } = {}) {
        const root = document.createElement('div');
        root.className = 'nt-input nt-autocomplete';

        const input = document.createElement('input');
        input.type = 'text';
        input.value = value;
        input.dataset.ntAutocompleteInput = 'true';
        input.dataset.ntAutocompleteAllowCustomValue = allowCustomValue ? 'true' : 'false';
        input.setAttribute('role', 'combobox');
        input.setAttribute('aria-expanded', 'false');
        root.appendChild(input);

        const menu = document.createElement('div');
        menu.className = 'nt-combobox-menu';
        menu.dataset.ntAutocompleteMenu = 'true';
        menu.setAttribute('popover', 'manual');
        menu.hidden = true;
        menu.setAttribute('aria-hidden', 'true');

        const list = document.createElement('ul');
        list.className = 'nt-combobox-list';
        list.id = 'city-listbox';
        list.setAttribute('role', 'listbox');
        menu.appendChild(list);

        const empty = document.createElement('div');
        empty.className = 'nt-combobox-empty';
        empty.hidden = true;
        menu.appendChild(empty);
        root.appendChild(menu);

        const optionDefinitions = options.map((option, index) => createOptionDefinition(option[0], option[1], option[2], index, option[4] ?? false, false, option[3]));
        if (allowCustomValue) {
            optionDefinitions.push(createOptionDefinition('', '', '', options.length, false, true));
        }
        appendOptionDefinitions(menu, list, optionDefinitions);

        document.body.appendChild(root);

        const effectiveDotNetRef = dotNetRef === undefined ? {
            invokeMethodAsync: jest.fn(() => Promise.resolve()),
        } : dotNetRef;

        onLoad(input, effectiveDotNetRef);
        return { dotNetRef: effectiveDotNetRef, empty, input, menu, root };
    }

    function getVisibleMenuValues(root) {
        return Array.from(root.querySelectorAll('[data-nt-autocomplete-option="true"]'))
            .filter(option => !option.hidden && !option.closest('.nt-combobox-list-item')?.hidden)
            .sort((left, right) => Number.parseInt(left.style.order || '0', 10) - Number.parseInt(right.style.order || '0', 10))
            .map(option => option.dataset.ntAutocompleteValue);
    }

    function getVisibleMenuRows(root) {
        return Array.from(root.querySelectorAll('.nt-combobox-list > .nt-combobox-list-item'))
            .filter(item => !item.hidden)
            .sort((left, right) => Number.parseInt(left.style.order || '0', 10) - Number.parseInt(right.style.order || '0', 10))
            .map(item => {
                const groupLabel = item.querySelector('.nt-combobox-group-label');
                if (groupLabel) {
                    return `group:${groupLabel.textContent}`;
                }

                return `option:${item.querySelector('[data-nt-autocomplete-option="true"]')?.dataset.ntAutocompleteLabel}`;
            });
    }

    function appendOptionDefinitions(parent, before, definitions) {
        for (const definition of definitions) {
            const source = document.createElement('script');
            source.type = 'application/json';
            source.dataset.ntAutocompleteOptionDefinition = 'true';
            source.textContent = JSON.stringify(definition);
            parent.insertBefore(source, before);
        }
    }

    function createOptionDefinition(value, label, supporting, index, disabled = false, custom = false, group = undefined) {
        return {
            cssClass: 'nt-combobox-option',
            customFormat: custom ? 'Use "{0}"' : undefined,
            disabled,
            group,
            id: `option-${index}`,
            isCustom: custom,
            label,
            selected: false,
            supportingText: supporting || undefined,
            value,
        };
    }

    afterEach(() => {
        onDispose(null);
        document.body.textContent = '';
        jest.clearAllMocks();
        delete global.fetch;
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

    async function waitForAsyncWork() {
        await new Promise(resolve => setTimeout(resolve, 0));
        await Promise.resolve();
        await Promise.resolve();
    }

    test('opens from input click and creates menu content from static metadata', () => {
        const { dotNetRef, input, menu, root } = createFixture();

        expect(input.hasAttribute('list')).toBe(false);
        expect(root.querySelector('.nt-combobox-list [data-nt-autocomplete-option="true"]')).toBeNull();
        input.dispatchEvent(new MouseEvent('click', { bubbles: true }));

        expect(root.classList.contains('nt-autocomplete-open')).toBe(true);
        expect(menu.hidden).toBe(false);
        expect(menu.classList.contains('nt-combobox-menu-layer')).toBe(true);
        expect(input.getAttribute('aria-expanded')).toBe('true');
        expect(root.querySelector('.nt-combobox-list [data-nt-autocomplete-option="true"]')).not.toBeNull();
        expect(root.querySelector('[data-nt-autocomplete-value="Austin"]').classList.contains('nt-autocomplete-group-option')).toBe(false);
        expect(dotNetRef.invokeMethodAsync).not.toHaveBeenCalled();
    });

    test('marks the existing value selected when opening', () => {
        const { input, root } = createFixture({ allowCustomValue: false, value: 'Austin' });

        input.dispatchEvent(new MouseEvent('click', { bubbles: true }));

        const selectedOption = root.querySelector('[data-nt-autocomplete-value="Austin"]');
        expect(selectedOption.classList.contains('nt-combobox-option-selected')).toBe(true);
        expect(selectedOption.getAttribute('aria-selected')).toBe('true');
        expect(selectedOption.querySelector('.nt-combobox-option-trailing')).not.toBeNull();
        expect(root.querySelector('[data-nt-autocomplete-value="Boston"] .nt-combobox-option-trailing')).toBeNull();
    });

    test('preserves static metadata when closing and reopening', () => {
        const { input, root } = createFixture({ allowCustomValue: false });

        input.dispatchEvent(new MouseEvent('click', { bubbles: true }));
        expect(root.querySelectorAll('.nt-combobox-list > .nt-combobox-list-item')).toHaveLength(4);

        input.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
        expect(root.querySelector('script[data-nt-autocomplete-option-definition="true"]')).not.toBeNull();
        expect(root.querySelectorAll('.nt-combobox-list > .nt-combobox-list-item')).toHaveLength(0);

        input.dispatchEvent(new MouseEvent('click', { bubbles: true }));
        expect(getVisibleMenuValues(root)).toEqual(['Austin', 'Boston', 'New York', 'North Austin']);
    });

    test('dispose leaves input without a list attribute', () => {
        const { input } = createFixture();

        expect(input.hasAttribute('list')).toBe(false);
        onDispose(input);

        expect(input.hasAttribute('list')).toBe(false);
    });

    test('synchronizes native pattern when custom values are disallowed', () => {
        const options = [
            ['Austin', 'Austin', 'Texas'],
            ['A/B (North)', 'A/B (North)', 'Neighborhood'],
            ['Phoenix', 'Phoenix', 'Arizona', undefined, true],
        ];
        const { input } = createFixture({ allowCustomValue: false, options });

        expect(input.getAttribute('pattern')).toBe('(?:Austin|A\\/B \\(North\\))');
    });

    test('filters to exact and contains matches without matching supporting text', () => {
        const { dotNetRef, empty, input, root } = createFixture({ allowCustomValue: false });

        input.value = 'Austin';
        input.dispatchEvent(new Event('input', { bubbles: true }));

        expect(getVisibleMenuValues(root)).toEqual(['Austin', 'North Austin']);
        expect(root.querySelectorAll('.nt-combobox-list > .nt-combobox-list-item')).toHaveLength(4);
        expect(empty.hidden).toBe(true);
        expect(dotNetRef.invokeMethodAsync).not.toHaveBeenCalledWith('NotifyAutocompleteValueChanged', expect.anything(), false);
    });

    test('works without a .NET object reference', () => {
        const { input, menu, root } = createFixture({ allowCustomValue: false, dotNetRef: null });

        input.value = 'Boston';
        input.dispatchEvent(new Event('input', { bubbles: true }));

        expect(root.classList.contains('nt-autocomplete-open')).toBe(true);
        expect(menu.hidden).toBe(false);
        expect(getVisibleMenuValues(root)).toEqual(['Boston']);

        root.querySelector('[data-nt-autocomplete-value="Boston"]').dispatchEvent(new MouseEvent('click', { bubbles: true }));

        expect(input.value).toBe('Boston');
        expect(root.classList.contains('nt-autocomplete-open')).toBe(false);
        expect(menu.classList.contains('nt-combobox-menu-layer')).toBe(false);
    });

    test('filters multi-word queries to only matching values', () => {
        const { input, root } = createFixture({ allowCustomValue: false });

        input.value = 'North Austin';
        input.dispatchEvent(new Event('input', { bubbles: true }));

        expect(getVisibleMenuValues(root)).toEqual(['North Austin']);
        expect(root.querySelectorAll('.nt-combobox-list > .nt-combobox-list-item')).toHaveLength(4);
    });

    test('renders option groups and hides empty groups while filtering', () => {
        const options = [
            ['Austin', 'Austin', 'Texas', 'Texas'],
            ['Dallas', 'Dallas', 'Texas', 'Texas'],
            ['Boston', 'Boston', 'Massachusetts', 'Massachusetts'],
        ];
        const { input, root } = createFixture({ allowCustomValue: false, options });
        const visibleGroups = () => Array.from(root.querySelectorAll('.nt-combobox-group-label'))
            .filter(label => !label.closest('.nt-combobox-list-item')?.hidden)
            .map(label => label.textContent);

        input.dispatchEvent(new MouseEvent('click', { bubbles: true }));

        expect(visibleGroups()).toEqual(['Texas', 'Massachusetts']);
        expect(getVisibleMenuValues(root)).toEqual(['Austin', 'Dallas', 'Boston']);
        expect(root.querySelector('[data-nt-autocomplete-value="Austin"]').classList.contains('nt-autocomplete-group-option')).toBe(true);

        input.value = 'Boston';
        input.dispatchEvent(new Event('input', { bubbles: true }));

        expect(visibleGroups()).toEqual(['Massachusetts']);
        expect(getVisibleMenuValues(root)).toEqual(['Boston']);
    });

    test('keeps filtered options under their group headers when ranked', () => {
        const options = [
            ['Austin', 'Austin', 'Texas', 'Texas'],
            ['Dallas', 'Dallas', 'Texas', 'Texas'],
            ['San Antonio', 'San Antonio', 'Texas', 'Texas'],
            ['East Austin', 'East Austin', 'Neighborhood', 'Neighborhood'],
            ['North Austin', 'North Austin', 'Neighborhood', 'Neighborhood'],
            ['Los Angeles', 'Los Angeles', 'California', 'California'],
            ['San Diego', 'San Diego', 'California', 'California'],
            ['Washington', 'Washington', 'District of Columbia', 'District of Columbia'],
        ];
        const { input, root } = createFixture({ allowCustomValue: false, options });

        input.value = 'a';
        input.dispatchEvent(new Event('input', { bubbles: true }));

        expect(getVisibleMenuRows(root)).toEqual([
            'group:Texas',
            'option:Austin',
            'option:Dallas',
            'option:San Antonio',
            'group:Neighborhood',
            'option:East Austin',
            'option:North Austin',
            'group:California',
            'option:Los Angeles',
            'option:San Diego',
            'group:District of Columbia',
            'option:Washington',
        ]);
    });

    test('renders the checkmark only for the selected option', () => {
        const { input, root } = createFixture({ allowCustomValue: false });

        input.dispatchEvent(new MouseEvent('click', { bubbles: true }));
        expect(root.querySelectorAll('.nt-combobox-option-trailing')).toHaveLength(0);

        input.value = 'Austin';
        input.dispatchEvent(new Event('input', { bubbles: true }));

        expect(root.querySelector('[data-nt-autocomplete-value="Austin"] .nt-combobox-option-trailing')).not.toBeNull();
        expect(root.querySelector('[data-nt-autocomplete-value="North Austin"] .nt-combobox-option-trailing')).toBeNull();

        input.value = 'North Austin';
        input.dispatchEvent(new Event('input', { bubbles: true }));

        expect(root.querySelector('[data-nt-autocomplete-value="Austin"] .nt-combobox-option-trailing')).toBeNull();
        expect(root.querySelector('[data-nt-autocomplete-value="North Austin"] .nt-combobox-option-trailing')).not.toBeNull();
    });

    test('shows custom value option last by default', () => {
        const { input, root } = createFixture();

        input.value = 'Austin';
        input.dispatchEvent(new Event('input', { bubbles: true }));

        const customOption = root.querySelector('[data-nt-autocomplete-custom-option="true"]');

        expect(getVisibleMenuValues(root)).toEqual(['Austin', 'North Austin', 'Austin']);
        expect(customOption.classList.contains('nt-combobox-option-selected')).toBe(false);
        expect(customOption.getAttribute('aria-selected')).toBe('false');
        expect(customOption.querySelector('.nt-combobox-option-label').textContent).toBe('Use "Austin"');
    });

    test('shows only exact multi-word match and custom option for multi-word query', () => {
        const { input, root } = createFixture();

        input.value = 'North Austin';
        input.dispatchEvent(new Event('input', { bubbles: true }));

        expect(getVisibleMenuValues(root)).toEqual(['North Austin', 'North Austin']);
    });

    test('renders all matching options in the menu', () => {
        const options = Array.from({ length: 12 }, (_, index) => [`Austin ${index + 1}`, `Austin ${index + 1}`, 'Texas']);
        const { input, root } = createFixture({ allowCustomValue: false, options });

        input.value = 'Austin';
        input.dispatchEvent(new Event('input', { bubbles: true }));

        expect(getVisibleMenuValues(root)).toEqual([
            'Austin 1',
            'Austin 2',
            'Austin 3',
            'Austin 4',
            'Austin 5',
            'Austin 6',
            'Austin 7',
            'Austin 8',
            'Austin 9',
            'Austin 10',
            'Austin 11',
            'Austin 12',
        ]);
        expect(root.querySelectorAll('.nt-combobox-list > .nt-combobox-list-item')).toHaveLength(12);
    });

    test('keeps hidden options available for later filtering', () => {
        const options = Array.from({ length: 12 }, (_, index) => [`Austin ${index + 1}`, `Austin ${index + 1}`, 'Texas']);
        const { input, root } = createFixture({ allowCustomValue: false, options });

        input.value = 'Austin';
        input.dispatchEvent(new Event('input', { bubbles: true }));
        input.value = 'Austin 12';
        input.dispatchEvent(new Event('input', { bubbles: true }));

        expect(getVisibleMenuValues(root)).toEqual(['Austin 12']);
        expect(root.querySelectorAll('.nt-combobox-list > .nt-combobox-list-item')).toHaveLength(12);
    });

    test('refreshes option state when rerendered items shrink', () => {
        const { input, root } = createFixture({ allowCustomValue: false });
        for (const source of root.querySelectorAll('script[data-nt-autocomplete-option-definition="true"]')) {
            source.remove();
        }
        appendOptionDefinitions(root.querySelector('.nt-combobox-menu'), root.querySelector('.nt-combobox-list'), [
            createOptionDefinition('Boston', 'Boston', 'Massachusetts', 0),
        ]);
        onUpdate(input, null);

        input.value = 'Austin';
        input.dispatchEvent(new Event('input', { bubbles: true }));

        expect(getVisibleMenuValues(root)).toEqual([]);
        expect(root.querySelectorAll('.nt-combobox-list > .nt-combobox-list-item')).toHaveLength(1);
    });

    test('arrow navigation applies active class and close clears active state', () => {
        const { input, root } = createFixture({ allowCustomValue: false });

        input.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowDown', bubbles: true }));

        const activeOption = root.querySelector('.nt-combobox-option-active');
        expect(activeOption).not.toBeNull();
        expect(activeOption.dataset.ntAutocompleteValue).toBe('Austin');
        expect(input.getAttribute('aria-activedescendant')).toBe(activeOption.id);

        input.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));

        expect(root.querySelector('.nt-combobox-option-active')).toBeNull();
        expect(input.hasAttribute('aria-activedescendant')).toBe(false);
    });

    test('arrow navigation scrolls the active option into view', () => {
        const options = Array.from({ length: 12 }, (_, index) => [`Austin ${index + 1}`, `Austin ${index + 1}`, 'Texas']);
        const { input } = createFixture({ allowCustomValue: false, options });

        input.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowDown', bubbles: true }));
        input.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowDown', bubbles: true }));

        expect(scrollIntoViewMock).toHaveBeenCalledWith({ block: 'nearest' });
    });

    test('opens above and constrains menu height when near the bottom of the viewport', () => {
        const { input, menu, root } = createFixture({ allowCustomValue: false });
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
        expect(menu.style.top).toBe('376px');
        expect(menu.style.left).toBe('8px');
        expect(menu.style.width).toBe('300px');
    });

    test('uses native popover when opening and closing the menu', () => {
        const { input, menu } = createFixture({ allowCustomValue: false });
        menu.showPopover = jest.fn(() => {
            menu.matches = selector => selector === ':popover-open';
        });
        menu.hidePopover = jest.fn(() => {
            menu.matches = () => false;
        });

        input.dispatchEvent(new MouseEvent('click', { bubbles: true }));

        expect(menu.showPopover).toHaveBeenCalledTimes(1);

        input.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));

        expect(menu.hidePopover).toHaveBeenCalledTimes(1);
    });

    test('repositions the open menu when scrolling a parent', () => {
        const { input, menu, root } = createFixture({ allowCustomValue: false });
        Object.defineProperty(window, 'innerHeight', { configurable: true, value: 760 });
        root.getBoundingClientRect = () => ({
            bottom: 144,
            height: 40,
            left: 24,
            right: 324,
            top: 104,
            width: 300,
            x: 24,
            y: 104,
            toJSON: () => ({}),
        });

        input.dispatchEvent(new MouseEvent('click', { bubbles: true }));
        expect(menu.style.top).toBe('148px');

        root.getBoundingClientRect = () => ({
            bottom: 94,
            height: 40,
            left: 24,
            right: 324,
            top: 54,
            width: 300,
            x: 24,
            y: 54,
            toJSON: () => ({}),
        });
        document.dispatchEvent(new Event('scroll'));

        expect(menu.style.top).toBe('98px');
    });

    test('selects an option and notifies .NET with the input value', async () => {
        const { dotNetRef, input, root } = createFixture({ allowCustomValue: false });

        input.dispatchEvent(new MouseEvent('click', { bubbles: true }));
        root.querySelector('[data-nt-autocomplete-value="Boston"]').dispatchEvent(new MouseEvent('click', { bubbles: true }));
        await Promise.resolve();

        expect(input.value).toBe('Boston');
        expect(root.querySelector('.nt-combobox-list [data-nt-autocomplete-value="Boston"]')).toBeNull();
        expect(dotNetRef.invokeMethodAsync).toHaveBeenLastCalledWith('NotifyAutocompleteValueChanged', 'Boston', true);
    });

    test('onUpdate reconnects rerendered menu content', () => {
        const { input, menu, root } = createFixture({ allowCustomValue: false });
        const replacementMenu = menu.cloneNode(true);
        menu.replaceWith(replacementMenu);

        onUpdate(input, null);
        input.dispatchEvent(new MouseEvent('click', { bubbles: true }));

        expect(replacementMenu.hidden).toBe(false);
        expect(root.classList.contains('nt-autocomplete-open')).toBe(true);
    });

    test('typing filters only local options and does not call fetch', () => {
        global.fetch = jest.fn();
        const { input, root } = createFixture({ allowCustomValue: false });

        input.value = 'austin';
        input.dispatchEvent(new Event('input', { bubbles: true }));

        expect(global.fetch).not.toHaveBeenCalled();
        expect(getVisibleMenuValues(root)).toEqual(['Austin', 'North Austin']);
    });
});
