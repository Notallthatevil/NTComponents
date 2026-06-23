type Maybe<T> = T | null | undefined;

interface DotNetAutocompleteRef {
    invokeMethodAsync(methodName: 'NotifyAutocompleteValueChanged', value: string, closeMenu: boolean): Promise<unknown> | void;
    invokeMethodAsync(methodName: 'NotifyAutocompleteTouched'): Promise<unknown> | void;
}

interface AutocompleteOptionState {
    element: HTMLButtonElement;
    filterText: string;
    isCustom: boolean;
    label: string;
    labelElement: HTMLElement | null;
    listItem: HTMLElement | null;
    lowerLabel: string;
    lowerValue: string;
    originalIndex: number;
    trailing: HTMLElement | null;
    value: string;
}

interface AutocompleteIconDefinition {
    cssClass: string;
    icon: string;
    style?: string;
    title: string;
}

interface AutocompleteOptionDefinition {
    cssClass: string;
    customFormat?: string;
    disabled: boolean;
    id: string;
    isCustom: boolean;
    label: string;
    leadingIcon?: AutocompleteIconDefinition;
    selected: boolean;
    supportingText?: string;
    value: string;
}

interface AutocompleteState {
    activeIndex: number;
    documentListenersAttached: boolean;
    dotNetRef: DotNetAutocompleteRef | null;
    input: HTMLInputElement;
    isOpen: boolean;
    list: HTMLElement | null;
    menu: HTMLElement | null;
    onDocumentFocusIn: (event: FocusEvent) => void;
    onDocumentMouseDown: (event: MouseEvent) => void;
    onDocumentScroll: (event: Event) => void;
    onInputClick: (event: MouseEvent) => void;
    onInputInput: (event: Event) => void;
    onInputKeyDown: (event: KeyboardEvent) => void;
    onMenuClick: (event: MouseEvent) => void;
    onMenuMouseDown: (event: MouseEvent) => void;
    onWindowResize: (event: UIEvent) => void;
    options: AutocompleteOptionState[];
    optionsSource: HTMLScriptElement | null;
    root: HTMLElement;
    valueNotificationInFlight: boolean;
    valueNotificationPendingCloseMenu: boolean;
    valueNotificationPending: boolean;
}

const stateByInput = new Map<HTMLInputElement, AutocompleteState>();
const activeOptionClass = 'nt-combobox-option-active';
const menuLayerClass = 'nt-combobox-menu-layer';
const menuViewportMargin = 8;
const menuViewportOffset = 4;
const menuPreferredMaxHeight = 320;

function isDotNetObjectReference(value: unknown): value is DotNetAutocompleteRef {
    return typeof (value as DotNetAutocompleteRef | null)?.invokeMethodAsync === 'function';
}

function queryRoot(input: HTMLInputElement): HTMLElement {
    return input.closest<HTMLElement>('.nt-autocomplete') ?? input;
}

function queryOptions(root: HTMLElement): HTMLButtonElement[] {
    return Array.from(root.querySelectorAll<HTMLButtonElement>('.nt-combobox-list [data-nt-autocomplete-option="true"]'));
}

function getOptionState(option: HTMLButtonElement, index: number): AutocompleteOptionState {
    option.dataset.ntAutocompleteIndex = index.toString();
    const label = option.dataset.ntAutocompleteLabel ?? option.textContent?.trim() ?? '';
    const value = option.dataset.ntAutocompleteValue ?? label;
    const lowerLabel = label.toLocaleLowerCase();
    const lowerValue = value.toLocaleLowerCase();
    const labelElement = option.querySelector<HTMLElement>('.nt-combobox-option-label');
    const isCustom = option.dataset.ntAutocompleteCustomOption === 'true';
    return {
        element: option,
        filterText: `${lowerValue} ${lowerLabel}`,
        isCustom,
        label,
        labelElement,
        listItem: option.closest<HTMLElement>('.nt-combobox-list-item'),
        lowerLabel,
        lowerValue,
        originalIndex: Number.parseInt(option.dataset.ntAutocompleteIndex, 10) || index,
        trailing: option.querySelector<HTMLElement>('.nt-combobox-option-trailing'),
        value,
    };
}

function getOptionOrder(option: AutocompleteOptionState): number {
    const order = Number.parseInt(option.element.style.order || option.listItem?.style.order || '', 10);
    return Number.isFinite(order) ? order : option.originalIndex;
}

function getVisibleEnabledOptions(state: AutocompleteState): AutocompleteOptionState[] {
    return state.options
        .filter(option => option.listItem?.isConnected && !option.element.hidden && !option.element.disabled)
        .sort((left, right) => getOptionOrder(left) - getOptionOrder(right) || left.originalIndex - right.originalIndex);
}

function updateMenuPlacement(state: AutocompleteState): void {
    if (!state.menu || state.menu.hidden) {
        return;
    }

    const rootRect = state.root.getBoundingClientRect();
    const viewportWidth = window.innerWidth || document.documentElement.clientWidth;
    const viewportHeight = window.innerHeight || document.documentElement.clientHeight;
    const maxViewportWidth = Math.max(0, viewportWidth - menuViewportMargin * 2);
    const width = Math.min(rootRect.width, maxViewportWidth);
    const left = Math.min(Math.max(rootRect.left, menuViewportMargin), Math.max(menuViewportMargin, viewportWidth - width - menuViewportMargin));
    const spaceBelow = Math.max(0, viewportHeight - rootRect.bottom - menuViewportMargin - menuViewportOffset);
    const spaceAbove = Math.max(0, rootRect.top - menuViewportMargin - menuViewportOffset);
    const openAbove = spaceBelow < menuPreferredMaxHeight && spaceAbove > spaceBelow;
    const availableSpace = openAbove ? spaceAbove : spaceBelow;
    const maxHeight = Math.min(menuPreferredMaxHeight, availableSpace);
    const height = Math.min(state.menu.scrollHeight || maxHeight, maxHeight);
    const top = openAbove ? rootRect.top - menuViewportOffset - height : rootRect.bottom + menuViewportOffset;

    state.menu.classList.toggle('nt-combobox-menu-above', openAbove);
    state.menu.classList.add(menuLayerClass);
    state.menu.style.left = `${left}px`;
    state.menu.style.maxHeight = `${maxHeight}px`;
    state.menu.style.top = `${Math.max(menuViewportMargin, top)}px`;
    state.menu.style.width = `${width}px`;
}

function showMenuSurface(state: AutocompleteState): void {
    if (!state.menu) {
        return;
    }

    if (typeof state.menu.showPopover === 'function' && !isMenuPopoverOpen(state.menu)) {
        try {
            state.menu.showPopover();
        }
        catch {
            // Already-open or unsupported popover transitions should not block the fallback positioned menu.
        }
    }
}

function hideMenuSurface(state: AutocompleteState): void {
    if (!state.menu) {
        return;
    }

    if (typeof state.menu.hidePopover === 'function' && isMenuPopoverOpen(state.menu)) {
        try {
            state.menu.hidePopover();
        }
        catch {
            // Best-effort cleanup only.
        }
    }

    state.menu.classList.remove(menuLayerClass, 'nt-combobox-menu-above');
    state.menu.style.removeProperty('left');
    state.menu.style.removeProperty('max-height');
    state.menu.style.removeProperty('top');
    state.menu.style.removeProperty('width');
}

function isMenuPopoverOpen(menu: HTMLElement): boolean {
    try {
        return menu.matches(':popover-open');
    }
    catch {
        return false;
    }
}

function scrollOptionIntoView(option: AutocompleteOptionState): void {
    option.element.scrollIntoView({ block: 'nearest' });
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

function updateSelectedOptionDom(state: AutocompleteState): void {
    for (const option of state.options) {
        const selected = !option.isCustom && option.value === state.input.value;
        option.element.classList.toggle('nt-combobox-option-selected', selected);
        option.element.setAttribute('aria-selected', selected ? 'true' : 'false');
        if (option.trailing) {
            option.trailing.hidden = !selected;
        }
    }
}

function clearActiveOption(state: AutocompleteState): void {
    state.activeIndex = -1;
    state.input.removeAttribute('aria-activedescendant');
    for (const option of state.options) {
        option.element.classList.remove(activeOptionClass);
    }
}

function updateActiveOption(state: AutocompleteState, nextIndex: number): void {
    if (!state.isOpen) {
        clearActiveOption(state);
        return;
    }

    const visibleOptions = getVisibleEnabledOptions(state);
    if (visibleOptions.length === 0) {
        clearActiveOption(state);
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

function rankOption(option: AutocompleteOptionState, query: string): number {
    if (option.isCustom) {
        return -1;
    }

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

function collectRankedOptions(options: AutocompleteOptionState[], query: string): AutocompleteOptionState[] {
    const exactMatches: AutocompleteOptionState[] = [];
    const startsWithMatches: AutocompleteOptionState[] = [];
    const containsMatches: AutocompleteOptionState[] = [];
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

function formatCustomOptionText(option: AutocompleteOptionState, value: string): string {
    const format = option.element.dataset.ntAutocompleteCustomFormat ?? 'Use "{0}"';
    return format.replace('{0}', value);
}

function filterOptions(state: AutocompleteState, updateActive = state.isOpen): void {
    if (state.options.length === 0) {
        if (updateActive) {
            clearActiveOption(state);
        }
        return;
    }

    const typedValue = state.input.value.trim();
    const query = typedValue.toLocaleLowerCase();
    const normalOptions = state.options.filter(option => !option.isCustom);
    const customOption = state.options.find(option => option.isCustom);
    const showCustomOption = Boolean(customOption && typedValue);
    const visibleRankedOptions = collectRankedOptions(normalOptions, query);
    const visibleOptions = new Set(visibleRankedOptions);
    if (customOption && showCustomOption) {
        visibleOptions.add(customOption);
        customOption.value = typedValue;
        customOption.label = typedValue;
        customOption.lowerValue = typedValue.toLocaleLowerCase();
        customOption.lowerLabel = customOption.lowerValue;
        customOption.filterText = customOption.lowerValue;
        customOption.element.dataset.ntAutocompleteValue = typedValue;
        customOption.element.dataset.ntAutocompleteLabel = typedValue;
        if (customOption.labelElement) {
            customOption.labelElement.textContent = formatCustomOptionText(customOption, typedValue);
        }
    }

    let order = 0;
    for (const option of visibleRankedOptions) {
        option.element.style.order = order.toString();
        if (option.listItem) {
            option.listItem.style.order = order.toString();
        }
        order++;
    }

    if (showCustomOption && customOption) {
        customOption.element.style.order = order.toString();
        if (customOption.listItem) {
            customOption.listItem.style.order = order.toString();
        }
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
        empty.hidden = visibleRankedOptions.length > 0 || showCustomOption;
    }

    if (state.isOpen) {
        updateMenuPlacement(state);
    }

    if (updateActive) {
        updateActiveOption(state, 0);
    }
    else {
        clearActiveOption(state);
    }
}

function attachDocumentListeners(state: AutocompleteState): void {
    if (state.documentListenersAttached) {
        return;
    }

    document.addEventListener('mousedown', state.onDocumentMouseDown);
    document.addEventListener('focusin', state.onDocumentFocusIn);
    document.addEventListener('scroll', state.onDocumentScroll, true);
    window.addEventListener('resize', state.onWindowResize);
    state.documentListenersAttached = true;
}

function detachDocumentListeners(state: AutocompleteState): void {
    if (!state.documentListenersAttached) {
        return;
    }

    document.removeEventListener('mousedown', state.onDocumentMouseDown);
    document.removeEventListener('focusin', state.onDocumentFocusIn);
    document.removeEventListener('scroll', state.onDocumentScroll, true);
    window.removeEventListener('resize', state.onWindowResize);
    state.documentListenersAttached = false;
}

function applyOpenState(state: AutocompleteState, isOpen: boolean): void {
    state.isOpen = isOpen;
    state.root.classList.toggle('nt-autocomplete-open', isOpen);
    state.input.setAttribute('aria-expanded', isOpen ? 'true' : 'false');
    if (state.menu) {
        state.menu.hidden = !isOpen;
        state.menu.setAttribute('aria-hidden', isOpen ? 'false' : 'true');
        if (!isOpen) {
            hideMenuSurface(state);
        }
    }

    if (isOpen) {
        showMenuSurface(state);
        updateMenuPlacement(state);
        attachDocumentListeners(state);
    }
    else {
        detachDocumentListeners(state);
        clearActiveOption(state);
        clearMenuOptions(state);
    }
}

function setOpen(state: AutocompleteState, isOpen: boolean, notifyTouched = false): void {
    if (state.isOpen === isOpen) {
        return;
    }

    applyOpenState(state, isOpen);
    if (isOpen) {
        filterOptions(state);
        return;
    }

    filterOptions(state, false);

    if (notifyTouched && state.dotNetRef) {
        void invokeDotNetSafely(() => state.dotNetRef?.invokeMethodAsync('NotifyAutocompleteTouched'));
    }
}

function openAutocomplete(state: AutocompleteState): boolean {
    if (state.input.disabled || state.input.readOnly) {
        return false;
    }

    if (state.isOpen) {
        return false;
    }

    renderMenuOptions(state);
    setOpen(state, true);
    return true;
}

function notifyValueChanged(state: AutocompleteState, closeMenu: boolean): void {
    updateSelectedOptionDom(state);
    if (!state.dotNetRef) {
        return;
    }

    if (state.valueNotificationInFlight) {
        state.valueNotificationPending = true;
        state.valueNotificationPendingCloseMenu ||= closeMenu;
        return;
    }

    const value = state.input.value;
    state.valueNotificationInFlight = true;
    void invokeDotNetSafely(() => state.dotNetRef?.invokeMethodAsync('NotifyAutocompleteValueChanged', value, closeMenu)).finally(() => {
        state.valueNotificationInFlight = false;
        if (state.valueNotificationPending) {
            const pendingCloseMenu = state.valueNotificationPendingCloseMenu;
            state.valueNotificationPending = false;
            state.valueNotificationPendingCloseMenu = false;
            notifyValueChanged(state, pendingCloseMenu);
        }
    });
}

function selectOption(state: AutocompleteState, option: AutocompleteOptionState): void {
    if (option.element.disabled || state.input.disabled || state.input.readOnly) {
        return;
    }

    state.input.value = option.value;
    updateSelectedOptionDom(state);
    setOpen(state, false);
    notifyValueChanged(state, true);
}

function updateElements(state: AutocompleteState): void {
    state.root = queryRoot(state.input);
    state.list = state.root.querySelector<HTMLElement>('.nt-combobox-list');
    state.menu = state.root.querySelector<HTMLElement>('[data-nt-autocomplete-menu="true"]');
    state.optionsSource = state.root.querySelector<HTMLScriptElement>('script[data-nt-autocomplete-options="true"]');
    const shouldBeOpen = state.root.classList.contains('nt-autocomplete-open') || state.menu?.hidden === false;
    if (shouldBeOpen) {
        renderMenuOptions(state);
    }
    else {
        clearMenuOptions(state);
    }

    applyOpenState(state, shouldBeOpen);
    updateSelectedOptionDom(state);
    filterOptions(state, shouldBeOpen);
}

function hasLiveMenuOptions(state: AutocompleteState): boolean {
    return Array.from(state.list?.children ?? []).some(child => child.classList.contains('nt-combobox-list-item'));
}

function parseOptionDefinitions(state: AutocompleteState): AutocompleteOptionDefinition[] {
    if (!state.optionsSource?.textContent) {
        return [];
    }

    try {
        const value = JSON.parse(state.optionsSource.textContent) as AutocompleteOptionDefinition[];
        return Array.isArray(value) ? value : [];
    }
    catch {
        return [];
    }
}

function createIconElement(icon: AutocompleteIconDefinition): HTMLElement {
    const wrapper = document.createElement('span');
    wrapper.className = 'nt-combobox-option-leading';
    wrapper.setAttribute('aria-hidden', 'true');

    const iconElement = document.createElement('span');
    iconElement.className = icon.cssClass;
    iconElement.title = icon.title;
    if (icon.style) {
        iconElement.setAttribute('style', icon.style);
    }
    iconElement.textContent = icon.icon;
    wrapper.appendChild(iconElement);
    return wrapper;
}

function createOptionElement(definition: AutocompleteOptionDefinition): HTMLElement {
    const item = document.createElement('li');
    item.className = 'nt-combobox-list-item';
    item.setAttribute('role', 'presentation');
    if (definition.isCustom) {
        item.hidden = true;
    }

    const option = document.createElement('button');
    option.className = definition.cssClass || 'nt-combobox-option';
    option.id = definition.id;
    option.type = 'button';
    option.setAttribute('role', 'option');
    option.setAttribute('aria-selected', definition.selected ? 'true' : 'false');
    option.dataset.ntAutocompleteOption = 'true';
    option.dataset.ntAutocompleteValue = definition.value;
    option.dataset.ntAutocompleteLabel = definition.label;
    if (definition.disabled) {
        option.disabled = true;
        option.setAttribute('aria-disabled', 'true');
    }
    if (definition.isCustom) {
        option.dataset.ntAutocompleteCustomOption = 'true';
        option.dataset.ntAutocompleteCustomFormat = definition.customFormat ?? 'Use "{0}"';
        option.hidden = true;
    }

    if (definition.leadingIcon) {
        option.appendChild(createIconElement(definition.leadingIcon));
    }

    const content = document.createElement('span');
    content.className = 'nt-combobox-option-content';
    const label = document.createElement('span');
    label.className = 'nt-combobox-option-label';
    label.textContent = definition.isCustom ? (definition.customFormat ?? 'Use "{0}"').replace('{0}', '') : definition.label;
    content.appendChild(label);
    if (definition.supportingText) {
        const supporting = document.createElement('span');
        supporting.className = 'nt-combobox-option-supporting';
        supporting.textContent = definition.supportingText;
        content.appendChild(supporting);
    }
    option.appendChild(content);

    if (!definition.isCustom) {
        const trailing = document.createElement('span');
        trailing.className = 'nt-combobox-option-trailing';
        trailing.setAttribute('aria-hidden', 'true');
        trailing.hidden = !definition.selected;
        const check = document.createElement('span');
        check.className = 'tnt-components tnt-icon material-symbols-outlined mi-medium';
        check.title = 'check';
        check.textContent = 'check';
        trailing.appendChild(check);
        option.appendChild(trailing);
    }

    item.appendChild(option);
    return item;
}

function renderMenuOptions(state: AutocompleteState): void {
    if (!state.list || hasLiveMenuOptions(state)) {
        state.options = queryOptions(state.root).map(getOptionState);
        return;
    }

    const fragment = document.createDocumentFragment();
    for (const definition of parseOptionDefinitions(state)) {
        fragment.appendChild(createOptionElement(definition));
    }
    state.list.appendChild(fragment);
    state.options = queryOptions(state.root).map(getOptionState);
}

function clearMenuOptions(state: AutocompleteState): void {
    for (const child of Array.from(state.list?.children ?? [])) {
        if (child.classList.contains('nt-combobox-list-item')) {
            child.remove();
        }
    }

    state.options = [];
}

function createState(input: HTMLInputElement, dotNetRef: Maybe<unknown>): AutocompleteState {
    const state: AutocompleteState = {
        activeIndex: -1,
        documentListenersAttached: false,
        dotNetRef: isDotNetObjectReference(dotNetRef) ? dotNetRef : null,
        input,
        isOpen: false,
        list: null,
        menu: null,
        onDocumentFocusIn: () => { },
        onDocumentMouseDown: () => { },
        onDocumentScroll: () => { },
        onInputClick: () => { },
        onInputInput: () => { },
        onInputKeyDown: () => { },
        onMenuClick: () => { },
        onMenuMouseDown: () => { },
        onWindowResize: () => { },
        options: [],
        optionsSource: null,
        root: queryRoot(input),
        valueNotificationInFlight: false,
        valueNotificationPendingCloseMenu: false,
        valueNotificationPending: false,
    };

    state.onInputClick = () => {
        openAutocomplete(state);
    };

    state.onInputInput = () => {
        if (!openAutocomplete(state)) {
            filterOptions(state);
        }
        updateSelectedOptionDom(state);
    };

    state.onInputKeyDown = event => {
        if (state.input.disabled || state.input.readOnly) {
            return;
        }

        const visibleOptions = getVisibleEnabledOptions(state);
        switch (event.key) {
            case 'ArrowDown':
                event.preventDefault();
                if (!openAutocomplete(state)) {
                    updateActiveOption(state, state.activeIndex + 1);
                }
                break;
            case 'ArrowUp':
                event.preventDefault();
                openAutocomplete(state);
                updateActiveOption(state, state.activeIndex - 1);
                break;
            case 'Enter':
                if (!state.isOpen) {
                    openAutocomplete(state);
                    return;
                }

                if (state.activeIndex >= 0 && state.activeIndex < visibleOptions.length) {
                    event.preventDefault();
                    selectOption(state, visibleOptions[state.activeIndex]);
                }
                break;
            case 'Escape':
                if (state.isOpen) {
                    event.preventDefault();
                    setOpen(state, false, true);
                }
                break;
            case 'Tab':
                setOpen(state, false, true);
                break;
        }
    };

    state.onMenuMouseDown = event => {
        event.preventDefault();
    };

    state.onMenuClick = event => {
        const target = event.target instanceof Element ? event.target : null;
        const optionElement = target?.closest<HTMLButtonElement>('[data-nt-autocomplete-option="true"]');
        const option = state.options.find(item => item.element === optionElement);
        if (!option) {
            return;
        }

        event.preventDefault();
        selectOption(state, option);
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

    state.onDocumentScroll = () => {
        updateMenuPlacement(state);
    };

    state.onWindowResize = () => {
        updateMenuPlacement(state);
    };

    input.addEventListener('click', state.onInputClick);
    input.addEventListener('input', state.onInputInput);
    input.addEventListener('keydown', state.onInputKeyDown);
    updateElements(state);
    state.menu?.addEventListener('mousedown', state.onMenuMouseDown);
    state.menu?.addEventListener('click', state.onMenuClick);
    return state;
}

function cleanupState(state: Maybe<AutocompleteState>): void {
    if (!state) {
        return;
    }

    state.input.removeEventListener('click', state.onInputClick);
    state.input.removeEventListener('input', state.onInputInput);
    state.input.removeEventListener('keydown', state.onInputKeyDown);
    state.menu?.removeEventListener('mousedown', state.onMenuMouseDown);
    state.menu?.removeEventListener('click', state.onMenuClick);
    hideMenuSurface(state);
    detachDocumentListeners(state);
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
    synchronizeAutocomplete(input, dotNetRef);
}

export function onUpdate(input: Maybe<HTMLInputElement>, dotNetRef: Maybe<unknown>): void {
    synchronizeAutocomplete(input, dotNetRef);
}

function synchronizeAutocomplete(input: Maybe<HTMLInputElement>, dotNetRef: Maybe<unknown>): void {
    cleanupDisconnectedStates();
    synchronizeInput(input, dotNetRef);
}

export function enhanceAll(root: ParentNode = document): void {
    cleanupDisconnectedStates();
    for (const input of Array.from(root.querySelectorAll<HTMLInputElement>('[data-nt-autocomplete-input="true"]'))) {
        synchronizeInput(input, null);
    }
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
