type Maybe<T> = T | null | undefined;

interface DotNetComboboxRef {
    invokeMethodAsync(methodName: 'NotifyComboboxSelectionChanged', selectedValues: string[]): Promise<unknown> | void;
    invokeMethodAsync(methodName: 'NotifyComboboxTouched'): Promise<unknown> | void;
}

interface ComboboxOptionState {
    element: HTMLButtonElement;
    filterText: string;
    label: string;
    listItem: HTMLElement | null;
    lowerLabel: string;
    lowerValue: string;
    trailing: HTMLElement | null;
    value: string;
}

interface ComboboxState {
    activeIndex: number;
    dotNetRef: DotNetComboboxRef | null;
    hiddenContainer: HTMLElement | null;
    filterQuery: string;
    input: HTMLInputElement;
    isOpen: boolean;
    menu: HTMLElement | null;
    onInputBeforeInput: (event: InputEvent) => void;
    onDocumentMouseDown: (event: MouseEvent) => void;
    onDocumentFocusIn: (event: FocusEvent) => void;
    onInputClick: (event: MouseEvent) => void;
    onInputInput: (event: Event) => void;
    onInputKeyDown: (event: KeyboardEvent) => void;
    onMenuClick: (event: MouseEvent) => void;
    onMenuMouseDown: (event: MouseEvent) => void;
    options: ComboboxOptionState[];
    root: HTMLElement;
    selectionNotificationInFlight: boolean;
    selectionNotificationPending: boolean;
    selectedValues: Set<string>;
}

const stateByInput = new Map<HTMLInputElement, ComboboxState>();
const activeOptionClass = 'nt-combobox-option-active';
const menuViewportMargin = 8;
const menuViewportOffset = 4;
const menuPreferredMaxHeight = 320;

function isDotNetObjectReference(value: unknown): value is DotNetComboboxRef {
    return typeof (value as DotNetComboboxRef | null)?.invokeMethodAsync === 'function';
}

function queryRoot(input: HTMLInputElement): HTMLElement {
    return input.closest<HTMLElement>('.nt-combobox') ?? input;
}

function queryOptions(root: HTMLElement): HTMLButtonElement[] {
    return Array.from(root.querySelectorAll<HTMLButtonElement>('[data-nt-combobox-option="true"]'));
}

function getOptionState(option: HTMLButtonElement): ComboboxOptionState {
    const label = getOptionLabel(option);
    const value = getOptionValue(option);
    const lowerLabel = label.toLocaleLowerCase();
    const lowerValue = value.toLocaleLowerCase();

    return {
        element: option,
        filterText: `${lowerValue} ${lowerLabel}`,
        label,
        listItem: option.closest<HTMLElement>('.nt-combobox-list-item'),
        lowerLabel,
        lowerValue,
        trailing: option.querySelector<HTMLElement>('.nt-combobox-option-trailing'),
        value,
    };
}

function isSearchable(input: HTMLInputElement): boolean {
    return input.dataset.ntComboboxSearchable !== 'false';
}

function isComponentReadOnly(input: HTMLInputElement): boolean {
    return input.dataset.ntComboboxReadonly === 'true';
}

function getOptionValue(option: HTMLButtonElement): string {
    return option.dataset.ntComboboxValue ?? '';
}

function getOptionLabel(option: HTMLButtonElement): string {
    return option.dataset.ntComboboxLabel ?? option.textContent?.trim() ?? '';
}

function getSelectedValuesFromDom(options: ComboboxOptionState[]): Set<string> {
    const selectedValues = new Set<string>();
    for (const option of options) {
        if (option.element.getAttribute('aria-selected') === 'true') {
            selectedValues.add(option.value);
        }
    }

    return selectedValues;
}

function getOptionOrder(option: ComboboxOptionState): number {
    const order = Number.parseInt(option.element.style.order || option.listItem?.style.order || '', 10);
    return Number.isFinite(order) ? order : 0;
}

function getVisibleEnabledOptions(state: ComboboxState): ComboboxOptionState[] {
    return state.options
        .filter(option => !option.element.hidden && !option.element.disabled)
        .sort((left, right) => getOptionOrder(left) - getOptionOrder(right));
}

function getSelectedOptions(state: ComboboxState): ComboboxOptionState[] {
    return state.options.filter(option => state.selectedValues.has(option.value));
}

function updateMenuPlacement(state: ComboboxState): void {
    if (!state.menu || state.menu.hidden) {
        return;
    }

    const rootRect = state.root.getBoundingClientRect();
    const spaceBelow = Math.max(0, window.innerHeight - rootRect.bottom - menuViewportMargin - menuViewportOffset);
    const spaceAbove = Math.max(0, rootRect.top - menuViewportMargin - menuViewportOffset);
    const openAbove = spaceBelow < menuPreferredMaxHeight && spaceAbove > spaceBelow;
    const availableSpace = openAbove ? spaceAbove : spaceBelow;

    state.menu.classList.toggle('nt-combobox-menu-above', openAbove);
    state.menu.style.maxHeight = `${Math.min(menuPreferredMaxHeight, availableSpace)}px`;
}

function scrollOptionIntoView(option: ComboboxOptionState): void {
    option.element.scrollIntoView({ block: 'nearest' });
}

function getDisplayText(state: ComboboxState): string {
    const separator = state.input.dataset.ntComboboxSeparator ?? ', ';
    return getSelectedOptions(state)
        .map(option => option.label)
        .join(separator);
}

function updateHiddenInputs(state: ComboboxState): void {
    const name = state.input.dataset.ntComboboxName;
    if (!state.hiddenContainer || !name) {
        return;
    }

    const fragment = document.createDocumentFragment();
    for (const value of state.selectedValues) {
        const hiddenInput = document.createElement('input');
        hiddenInput.type = 'hidden';
        hiddenInput.name = name;
        hiddenInput.value = value;
        fragment.appendChild(hiddenInput);
    }

    state.hiddenContainer.replaceChildren(fragment);
}

function updateSelectedOptionDom(state: ComboboxState): void {
    for (const option of state.options) {
        const selected = state.selectedValues.has(option.value);
        option.element.classList.toggle('nt-combobox-option-selected', selected);
        option.element.setAttribute('aria-selected', selected ? 'true' : 'false');
        if (option.trailing) {
            option.trailing.hidden = !selected;
        }
    }
}

function updateDisplayValue(state: ComboboxState): void {
    state.input.value = getDisplayText(state);
}

function invokeDotNetSafely(callback: () => Promise<unknown> | void): Promise<void> {
    try {
        return Promise.resolve(callback()).then(
            () => { },
            () => { }
        );
    }
    catch {
        return Promise.resolve();
    }
}

function updateActiveOption(state: ComboboxState, nextIndex: number): void {
    const visibleOptions = getVisibleEnabledOptions(state);
    if (visibleOptions.length === 0) {
        for (const option of state.options) {
            option.element.classList.remove(activeOptionClass);
        }

        state.activeIndex = -1;
        state.input.removeAttribute('aria-activedescendant');
        return;
    }

    for (const option of state.options) {
        option.element.classList.remove(activeOptionClass);
    }

    state.activeIndex = ((nextIndex % visibleOptions.length) + visibleOptions.length) % visibleOptions.length;
    const activeOption = visibleOptions[state.activeIndex];
    activeOption.element.classList.add(activeOptionClass);
    state.input.setAttribute('aria-activedescendant', activeOption.element.id);
    scrollOptionIntoView(activeOption);
}

function rankOption(option: ComboboxOptionState, query: string): number {
    if (!query) {
        return 0;
    }

    if (option.lowerValue === query || option.lowerLabel === query) {
        return 0;
    }

    if (option.lowerValue.startsWith(query) || option.lowerLabel.startsWith(query)) {
        return 1;
    }

    return option.filterText.includes(query) ? 2 : -1;
}

function collectRankedOptions(options: ComboboxOptionState[], query: string): ComboboxOptionState[] {
    const exactMatches: ComboboxOptionState[] = [];
    const startsWithMatches: ComboboxOptionState[] = [];
    const containsMatches: ComboboxOptionState[] = [];
    for (const option of options) {
        const rank = rankOption(option, query);
        if (rank === 0) {
            exactMatches.push(option);
            continue;
        }

        if (rank === 1) {
            startsWithMatches.push(option);
            continue;
        }

        if (rank === 2) {
            containsMatches.push(option);
        }
    }

    return exactMatches.concat(startsWithMatches, containsMatches);
}

function filterOptions(state: ComboboxState, query: string): void {
    const normalizedQuery = query.trim().toLocaleLowerCase();
    const visibleRankedOptions = collectRankedOptions(state.options, normalizedQuery);
    const visibleOptions = new Set(visibleRankedOptions);

    let order = 0;
    for (const option of visibleRankedOptions) {
        option.element.style.order = order.toString();
        if (option.listItem) {
            option.listItem.style.order = order.toString();
        }
        order++;
    }

    for (const option of state.options) {
        const visible = visibleOptions.has(option);
        option.element.hidden = !visible;
        option.listItem?.toggleAttribute('hidden', !visible);
        if (!visible) {
            option.element.style.removeProperty('order');
            option.listItem?.style.removeProperty('order');
        }
    }

    const empty = state.root.querySelector<HTMLElement>('.nt-combobox-empty');
    if (empty) {
        empty.hidden = visibleRankedOptions.length > 0;
    }

    if (state.isOpen) {
        updateMenuPlacement(state);
    }

    updateActiveOption(state, 0);
}

function clearFilter(state: ComboboxState): void {
    state.filterQuery = '';
    filterOptions(state, '');
}

function isPrintableKey(event: KeyboardEvent): boolean {
    return event.key.length === 1 && !event.altKey && !event.ctrlKey && !event.metaKey;
}

function appendTypeaheadCharacter(state: ComboboxState, character: string): void {
    if (!isSearchable(state.input)) {
        return;
    }

    state.filterQuery += character;
    openCombobox(state);
    filterOptions(state, state.filterQuery);
    updateDisplayValue(state);
}

function setOpen(state: ComboboxState, isOpen: boolean, notifyTouched = false): void {
    if (state.isOpen === isOpen) {
        return;
    }

    state.isOpen = isOpen;
    state.root.classList.toggle('nt-combobox-open', isOpen);
    state.input.setAttribute('aria-expanded', isOpen ? 'true' : 'false');
    if (state.menu) {
        state.menu.hidden = !isOpen;
        state.menu.setAttribute('aria-hidden', isOpen ? 'false' : 'true');
        if (!isOpen) {
            state.menu.classList.remove('nt-combobox-menu-above');
            state.menu.style.removeProperty('max-height');
        }
    }

    if (isOpen) {
        updateMenuPlacement(state);
        filterOptions(state, isSearchable(state.input) ? state.filterQuery : '');
        return;
    }

    state.input.removeAttribute('aria-activedescendant');
    clearFilter(state);
    updateDisplayValue(state);

    if (notifyTouched && state.dotNetRef) {
        void invokeDotNetSafely(() => state.dotNetRef?.invokeMethodAsync('NotifyComboboxTouched'));
    }
}

function openCombobox(state: ComboboxState, resetFilter = false): void {
    if (state.input.disabled || isComponentReadOnly(state.input)) {
        return;
    }

    if (resetFilter) {
        state.filterQuery = '';
    }

    setOpen(state, true);
}

function notifySelectionChanged(state: ComboboxState): void {
    const selectedValues = Array.from(state.selectedValues);
    updateSelectedOptionDom(state);
    updateHiddenInputs(state);
    updateDisplayValue(state);

    if (!state.dotNetRef) {
        return;
    }

    if (state.selectionNotificationInFlight) {
        state.selectionNotificationPending = true;
        return;
    }

    state.selectionNotificationInFlight = true;
    void invokeDotNetSafely(() => state.dotNetRef?.invokeMethodAsync('NotifyComboboxSelectionChanged', selectedValues)).finally(() => {
        state.selectionNotificationInFlight = false;
        if (state.selectionNotificationPending) {
            state.selectionNotificationPending = false;
            notifySelectionChanged(state);
        }
    });
}

function toggleOption(state: ComboboxState, option: ComboboxOptionState): void {
    if (option.element.disabled || state.input.disabled || isComponentReadOnly(state.input)) {
        return;
    }

    if (state.selectedValues.has(option.value)) {
        state.selectedValues.delete(option.value);
    }
    else {
        state.selectedValues.add(option.value);
    }

    clearFilter(state);
    notifySelectionChanged(state);
}

function updateElements(state: ComboboxState): void {
    state.root = queryRoot(state.input);
    state.menu = state.root.querySelector<HTMLElement>('[data-nt-combobox-menu="true"]');
    state.hiddenContainer = state.root.querySelector<HTMLElement>('[data-nt-combobox-hidden-container="true"]');
    state.options = queryOptions(state.root).map(getOptionState);
    state.selectedValues = getSelectedValuesFromDom(state.options);
    const shouldBeOpen = state.root.classList.contains('nt-combobox-open') || state.menu?.hidden === false;
    state.isOpen = !shouldBeOpen;
    updateSelectedOptionDom(state);
    updateHiddenInputs(state);
    updateDisplayValue(state);
    setOpen(state, shouldBeOpen);
}

function createState(input: HTMLInputElement, dotNetRef: Maybe<unknown>): ComboboxState {
    const state: ComboboxState = {
        activeIndex: -1,
        dotNetRef: isDotNetObjectReference(dotNetRef) ? dotNetRef : null,
        filterQuery: '',
        hiddenContainer: null,
        input,
        isOpen: false,
        menu: null,
        onInputBeforeInput: () => { },
        onDocumentMouseDown: () => { },
        onDocumentFocusIn: () => { },
        onInputClick: () => { },
        onInputInput: () => { },
        onInputKeyDown: () => { },
        onMenuClick: () => { },
        onMenuMouseDown: () => { },
        options: [],
        root: queryRoot(input),
        selectionNotificationInFlight: false,
        selectionNotificationPending: false,
        selectedValues: new Set(),
    };

    state.onInputClick = event => {
        event.preventDefault();
        openCombobox(state, true);
    };

    state.onInputBeforeInput = event => {
        event.preventDefault();
    };

    state.onInputInput = () => {
        updateDisplayValue(state);
    };

    state.onInputKeyDown = event => {
        if (state.input.disabled || isComponentReadOnly(state.input)) {
            return;
        }

        const visibleOptions = getVisibleEnabledOptions(state);
        switch (event.key) {
            case 'ArrowDown':
                event.preventDefault();
                openCombobox(state);
                updateActiveOption(state, state.activeIndex + 1);
                break;
            case 'ArrowUp':
                event.preventDefault();
                openCombobox(state);
                updateActiveOption(state, state.activeIndex - 1);
                break;
            case 'Home':
                if (state.isOpen) {
                    event.preventDefault();
                    updateActiveOption(state, 0);
                }
                break;
            case 'End':
                if (state.isOpen) {
                    event.preventDefault();
                    updateActiveOption(state, visibleOptions.length - 1);
                }
                break;
            case 'Enter':
            case ' ':
            case 'Spacebar':
                if (!state.isOpen) {
                    event.preventDefault();
                    openCombobox(state, true);
                    return;
                }

                if (state.activeIndex >= 0 && state.activeIndex < visibleOptions.length) {
                    event.preventDefault();
                    toggleOption(state, visibleOptions[state.activeIndex]);
                }
                break;
            case 'Escape':
                if (state.isOpen) {
                    event.preventDefault();
                    clearFilter(state);
                    setOpen(state, false, true);
                }
                break;
            case 'Backspace':
                if (state.isOpen && state.filterQuery.length > 0) {
                    event.preventDefault();
                    state.filterQuery = state.filterQuery.slice(0, -1);
                    filterOptions(state, state.filterQuery);
                    updateDisplayValue(state);
                }
                break;
            case 'Tab':
                setOpen(state, false, true);
                break;
            default:
                if (isPrintableKey(event)) {
                    event.preventDefault();
                    appendTypeaheadCharacter(state, event.key);
                }
                break;
        }
    };

    state.onMenuMouseDown = event => {
        event.preventDefault();
    };

    state.onMenuClick = event => {
        const target = event.target instanceof Element ? event.target : null;
        const optionElement = target?.closest<HTMLButtonElement>('[data-nt-combobox-option="true"]');
        const option = state.options.find(item => item.element === optionElement);
        if (!option) {
            return;
        }

        event.preventDefault();
        toggleOption(state, option);
    };

    state.onDocumentMouseDown = event => {
        const target = event.target instanceof Node ? event.target : null;
        if (target && state.root.contains(target)) {
            return;
        }

        setOpen(state, false, true);
    };

    state.onDocumentFocusIn = event => {
        const target = event.target instanceof Node ? event.target : null;
        if (target && state.root.contains(target)) {
            return;
        }

        setOpen(state, false, true);
    };

    input.addEventListener('click', state.onInputClick);
    input.addEventListener('beforeinput', state.onInputBeforeInput);
    input.addEventListener('input', state.onInputInput);
    input.addEventListener('keydown', state.onInputKeyDown);
    document.addEventListener('mousedown', state.onDocumentMouseDown);
    document.addEventListener('focusin', state.onDocumentFocusIn);
    updateElements(state);
    state.menu?.addEventListener('mousedown', state.onMenuMouseDown);
    state.menu?.addEventListener('click', state.onMenuClick);
    return state;
}

function cleanupState(state: Maybe<ComboboxState>): void {
    if (!state) {
        return;
    }

    state.input.removeEventListener('click', state.onInputClick);
    state.input.removeEventListener('beforeinput', state.onInputBeforeInput);
    state.input.removeEventListener('input', state.onInputInput);
    state.input.removeEventListener('keydown', state.onInputKeyDown);
    state.menu?.removeEventListener('mousedown', state.onMenuMouseDown);
    state.menu?.removeEventListener('click', state.onMenuClick);
    document.removeEventListener('mousedown', state.onDocumentMouseDown);
    document.removeEventListener('focusin', state.onDocumentFocusIn);
}

function synchronizeInput(input: Maybe<HTMLInputElement>, dotNetRef: Maybe<unknown>): void {
    if (!input) {
        return;
    }

    const existing = stateByInput.get(input);
    if (existing) {
        existing.dotNetRef = isDotNetObjectReference(dotNetRef) ? dotNetRef : existing.dotNetRef;
        const previousMenu = existing.menu;
        updateElements(existing);
        if (previousMenu !== existing.menu) {
            previousMenu?.removeEventListener('mousedown', existing.onMenuMouseDown);
            previousMenu?.removeEventListener('click', existing.onMenuClick);
            existing.menu?.addEventListener('mousedown', existing.onMenuMouseDown);
            existing.menu?.addEventListener('click', existing.onMenuClick);
        }
        return;
    }

    stateByInput.set(input, createState(input, dotNetRef));
}

function cleanupDisconnectedStates(): void {
    for (const [input, state] of stateByInput) {
        if (!input.isConnected) {
            cleanupState(state);
            stateByInput.delete(input);
        }
    }
}

export function onLoad(input: Maybe<HTMLInputElement>, dotNetRef: Maybe<unknown>): void {
    cleanupDisconnectedStates();
    synchronizeInput(input, dotNetRef);
}

export function onUpdate(input: Maybe<HTMLInputElement>, dotNetRef: Maybe<unknown>): void {
    cleanupDisconnectedStates();
    synchronizeInput(input, dotNetRef);
}

export function onDispose(input: Maybe<HTMLInputElement>): void {
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
    cleanupDisconnectedStates();
}
