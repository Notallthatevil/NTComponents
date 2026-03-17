const searchSelectState = new WeakMap();

function createState(element) {
    if (searchSelectState.has(element)) {
        return searchSelectState.get(element);
    }

    const state = {
        dotNetRef: null,
        frameId: null,
        lastDropdown: null,
        mouseDownInside: false,
        onClick: null,
        onInputFocus: null,
        onFocusIn: null,
        onMouseDown: null,
        schedulePositionUpdate: null,
    };

    state.schedulePositionUpdate = event => schedulePositionUpdate(element, event);
    state.onMouseDown = event => {
        state.mouseDownInside = element.contains(event.target);
    };
    state.onClick = event => {
        const isClickInside = element.contains(event.target);
        if (!state.mouseDownInside && !isClickInside) {
            closeDropdownFromOutsideInteraction(state);
        }

        state.mouseDownInside = false;
    };
    state.onFocusIn = event => {
        if (element.contains(event.target)) {
            return;
        }

        closeDropdownFromOutsideInteraction(state);
    };
    state.onInputFocus = () => {
        const input = element.querySelector('.tnt-input-select-search-input');
        const hiddenInput = element.querySelector('input[type="hidden"]');
        if (!input || !hiddenInput) {
            return;
        }

        if (hiddenInput.value.trim().length === 0 || input.value.trim().length === 0) {
            return;
        }

        requestAnimationFrame(() => {
            if (document.activeElement === input) {
                input.select();
            }
        });
    };

    window.addEventListener('resize', state.schedulePositionUpdate, { passive: true });
    window.addEventListener('scroll', state.schedulePositionUpdate, true);
    window.addEventListener('mousedown', state.onMouseDown);
    window.addEventListener('click', state.onClick);
    document.addEventListener('focusin', state.onFocusIn);
    element.addEventListener('focusin', state.onInputFocus);

    searchSelectState.set(element, state);
    return state;
}

function closeDropdownFromOutsideInteraction(state) {
    if (state.dotNetRef?.invokeMethodAsync) {
        state.dotNetRef.invokeMethodAsync('CloseDropdownFromJs');
    }
}

function schedulePositionUpdate(element, event = null) {
    const state = searchSelectState.get(element);
    if (!state || !element.isConnected) {
        return;
    }

    const dropdown = element.querySelector('.tnt-input-select-search-content');
    if (event?.type === 'scroll' && dropdown && event.target === dropdown) {
        return;
    }

    const hasDropdown = dropdown !== null;
    if (!hasDropdown && !state.lastDropdown) {
        return;
    }

    if (state.frameId !== null) {
        cancelAnimationFrame(state.frameId);
    }

    state.frameId = requestAnimationFrame(() => {
        state.frameId = null;
        updateDropdownPosition(element, state);
    });
}

function updateDropdownPosition(element, state) {
    const dropdown = element.querySelector('.tnt-input-select-search-content');
    const input = element.querySelector('.tnt-input-select-search-input');

    if (!dropdown || !input) {
        if (state.lastDropdown) {
            state.lastDropdown.classList.remove('tnt-input-select-search-fixed');
            state.lastDropdown.style.removeProperty('visibility');
            state.lastDropdown.style.removeProperty('left');
            state.lastDropdown.style.removeProperty('top');
            state.lastDropdown.style.removeProperty('min-width');
            state.lastDropdown.style.removeProperty('max-width');
            state.lastDropdown.style.removeProperty('max-height');
        }

        state.lastDropdown = null;
        return;
    }

    state.lastDropdown = dropdown;
    dropdown.classList.add('tnt-input-select-search-fixed');
    dropdown.style.visibility = 'hidden';

    const viewportWidth = Math.max(document.documentElement?.clientWidth ?? 0, window.innerWidth ?? 0);
    const viewportHeight = Math.max(document.documentElement?.clientHeight ?? 0, window.innerHeight ?? 0);
    const edgePadding = 8;
    const offset = 4;

    const inputRect = input.getBoundingClientRect();
    const minWidth = Math.max(0, Math.round(inputRect.width));
    const maxWidth = Math.max(0, viewportWidth - edgePadding * 2);

    dropdown.style.minWidth = `${minWidth}px`;
    dropdown.style.maxWidth = `${maxWidth}px`;
    dropdown.style.maxHeight = `${Math.max(120, viewportHeight - edgePadding * 2)}px`;
    dropdown.style.left = `${Math.max(edgePadding, Math.round(inputRect.left))}px`;
    dropdown.style.top = `${Math.max(edgePadding, Math.round(inputRect.bottom + offset))}px`;

    const initialRect = dropdown.getBoundingClientRect();
    const spaceBelow = Math.max(0, viewportHeight - inputRect.bottom - edgePadding);
    const spaceAbove = Math.max(0, inputRect.top - edgePadding);
    const preferredHeight = Math.min(initialRect.height || 0, 280);
    const openBelow = spaceBelow >= preferredHeight || spaceBelow >= spaceAbove;
    const availableHeight = Math.max(120, openBelow ? spaceBelow : spaceAbove);
    const maxHeight = Math.min(availableHeight, Math.max(120, viewportHeight - edgePadding * 2));

    dropdown.style.maxHeight = `${Math.floor(maxHeight)}px`;

    const constrainedRect = dropdown.getBoundingClientRect();
    const dropdownHeight = Math.min(constrainedRect.height, maxHeight);
    const dropdownWidth = constrainedRect.width;

    let top = openBelow
        ? inputRect.bottom + offset
        : inputRect.top - dropdownHeight - offset;
    top = Math.max(edgePadding, Math.min(top, viewportHeight - dropdownHeight - edgePadding));

    let left = inputRect.left;
    if (left + dropdownWidth > viewportWidth - edgePadding) {
        left = viewportWidth - dropdownWidth - edgePadding;
    }

    left = Math.max(edgePadding, left);

    dropdown.style.left = `${Math.round(left)}px`;
    dropdown.style.top = `${Math.round(top)}px`;
    dropdown.style.visibility = 'visible';
    syncFocusedItemIntoView(dropdown);
}

function syncFocusedItemIntoView(dropdown) {
    const focusedItem = dropdown.querySelector('.tnt-input-select-search-list-item.tnt-focused');
    if (!focusedItem) {
        return;
    }

    const itemTop = focusedItem.offsetTop;
    const itemBottom = itemTop + focusedItem.offsetHeight;
    const viewTop = dropdown.scrollTop;
    const viewBottom = viewTop + dropdown.clientHeight;

    if (itemTop < viewTop) {
        dropdown.scrollTop = itemTop;
        return;
    }

    if (itemBottom > viewBottom) {
        dropdown.scrollTop = itemBottom - dropdown.clientHeight;
    }
}

function syncInputValidity(element) {
    const input = element.querySelector('.tnt-input-select-search-input');
    const hiddenInput = element.querySelector('input[type="hidden"]');
    if (!input || !hiddenInput) {
        return;
    }

    const requireSelection = input.dataset.requireSelection === 'true';
    const hasSelection = hiddenInput.value.trim().length > 0;
    const hasTypedText = input.value.trim().length > 0;

    if (requireSelection && !hasSelection && hasTypedText) {
        input.setCustomValidity('Please select an option from the list.');
        return;
    }

    input.setCustomValidity('');
}

export function onLoad(element, dotNetRef) {
    if (!element) {
        return;
    }

    const state = createState(element);
    state.dotNetRef = dotNetRef;
    schedulePositionUpdate(element);
    syncInputValidity(element);
}

export function onUpdate(element, dotNetRef) {
    if (!element) {
        return;
    }

    if (!searchSelectState.has(element)) {
        createState(element);
    }

    const state = searchSelectState.get(element);
    if (state) {
        state.dotNetRef = dotNetRef;
    }

    schedulePositionUpdate(element);
    syncInputValidity(element);
}

export function onDispose(element) {
    const state = searchSelectState.get(element);
    if (!state) {
        return;
    }

    if (state.frameId !== null) {
        cancelAnimationFrame(state.frameId);
    }

    window.removeEventListener('resize', state.schedulePositionUpdate);
    window.removeEventListener('scroll', state.schedulePositionUpdate, true);
    window.removeEventListener('mousedown', state.onMouseDown);
    window.removeEventListener('click', state.onClick);
    document.removeEventListener('focusin', state.onFocusIn);
    element.removeEventListener('focusin', state.onInputFocus);

    if (state.lastDropdown) {
        state.lastDropdown.classList.remove('tnt-input-select-search-fixed');
        state.lastDropdown.style.removeProperty('visibility');
        state.lastDropdown.style.removeProperty('left');
        state.lastDropdown.style.removeProperty('top');
        state.lastDropdown.style.removeProperty('min-width');
        state.lastDropdown.style.removeProperty('max-width');
        state.lastDropdown.style.removeProperty('max-height');
    }

    searchSelectState.delete(element);
}
