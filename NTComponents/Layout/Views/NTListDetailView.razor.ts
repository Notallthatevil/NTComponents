interface NTListDetailRegistration {
    clickHandler: (event: MouseEvent) => void;
    submitHandler: (event: SubmitEvent) => void;
}

type NTListDetailViewElement = HTMLElement & {
    __ntListDetailRegistration?: NTListDetailRegistration;
};

const viewSelector = '.nt-list-detail-view';
const detailVisibleClass = 'nt-list-detail-view-detail-visible';
const selectedItemClass = 'nt-list-detail-view-item-selected';
const panelBackwardClass = 'nt-list-detail-view-panel-backward';
const triggerSelector = '[data-nt-list-detail-trigger]';
const panelSelector = '[data-nt-list-detail-panel]';
const backSelector = '[data-nt-list-detail-back]';

let globalSyncScheduled = false;

function getViews(element?: Element | null): NTListDetailViewElement[] {
    if (element instanceof HTMLElement) {
        const view = element.matches(viewSelector)
            ? element
            : element.closest<NTListDetailViewElement>(viewSelector);

        if (view) {
            return [view as NTListDetailViewElement];
        }

        return Array.from(element.querySelectorAll<NTListDetailViewElement>(viewSelector));
    }

    return Array.from(document.querySelectorAll<NTListDetailViewElement>(viewSelector));
}

function getTriggerValue(trigger: Element): string {
    return trigger.getAttribute('data-nt-list-detail-trigger') ?? '';
}

function getPanelValue(panel: Element): string {
    return panel.getAttribute('data-nt-list-detail-panel') ?? '';
}

function findPanel(view: Element, value: string): HTMLElement | null {
    if (!value) {
        return null;
    }

    for (const panel of view.querySelectorAll<HTMLElement>(panelSelector)) {
        if (getPanelValue(panel) === value) {
            return panel;
        }
    }

    return null;
}

function shouldLetBrowserHandleClick(event: MouseEvent): boolean {
    return event.defaultPrevented
        || event.button !== 0
        || event.altKey
        || event.ctrlKey
        || event.metaKey
        || event.shiftKey;
}

function setDetailVisible(view: HTMLElement, visible: boolean): void {
    view.dataset.detailVisible = visible ? 'true' : 'false';
    view.classList.toggle(detailVisibleClass, visible);
}

function setSelectedTrigger(view: HTMLElement, selectedTrigger: HTMLElement): void {
    view.querySelectorAll<HTMLElement>(triggerSelector).forEach(trigger => {
        const selected = trigger === selectedTrigger;
        trigger.classList.toggle(selectedItemClass, selected);
        trigger.dataset.ntListDetailSelected = selected ? 'true' : 'false';

        if (selected) {
            trigger.setAttribute('aria-current', 'true');
        }
        else if (trigger.getAttribute('aria-current') === 'true') {
            trigger.removeAttribute('aria-current');
        }
    });
}

function restartPanelAnimation(selectedPanel: HTMLElement): void {
    selectedPanel.style.animation = 'none';

    if (typeof requestAnimationFrame !== 'function') {
        selectedPanel.style.animation = '';
        return;
    }

    requestAnimationFrame(() => {
        selectedPanel.style.animation = '';
    });
}

function setSelectedPanel(view: HTMLElement, selectedPanel: HTMLElement): void {
    const previousValue = view.dataset.ntListDetailSelectedValue ?? '';
    const selectedValue = getPanelValue(selectedPanel);
    const panels = view.querySelectorAll<HTMLElement>(panelSelector);
    let previousIndex = -1;
    let selectedIndex = -1;
    let index = 0;

    panels.forEach(panel => {
        if (getPanelValue(panel) === previousValue) {
            previousIndex = index;
        }

        if (panel === selectedPanel) {
            selectedIndex = index;
        }

        panel.hidden = panel !== selectedPanel;
        index++;
    });

    selectedPanel.classList.toggle(panelBackwardClass, previousIndex >= 0 && selectedIndex >= 0 && selectedIndex < previousIndex);
    restartPanelAnimation(selectedPanel);
    view.dataset.ntListDetailSelectedValue = selectedValue;
}

function selectTrigger(view: HTMLElement, trigger: HTMLElement): boolean {
    const selectedValue = getTriggerValue(trigger);
    const selectedPanel = findPanel(view, selectedValue);

    if (!selectedPanel) {
        return false;
    }

    setSelectedTrigger(view, trigger);
    setSelectedPanel(view, selectedPanel);
    setDetailVisible(view, true);

    view.dispatchEvent(new CustomEvent('nt-list-detail-select', {
        bubbles: true,
        detail: {
            panel: selectedPanel,
            trigger,
            value: selectedValue
        }
    }));

    return true;
}

function closeDetail(view: HTMLElement): void {
    setDetailVisible(view, false);

    view.dispatchEvent(new CustomEvent('nt-list-detail-close', {
        bubbles: true
    }));
}

function syncInitialSelection(view: HTMLElement): void {
    const selectedTrigger = view.querySelector<HTMLElement>(`${triggerSelector}[data-nt-list-detail-selected='true'], ${triggerSelector}[aria-current='true']`);

    if (selectedTrigger) {
        const selectedPanel = findPanel(view, getTriggerValue(selectedTrigger));

        if (selectedPanel) {
            const selectedValue = getPanelValue(selectedPanel);

            if (view.dataset.ntListDetailSelectedValue === selectedValue
                && !selectedPanel.hidden
                && selectedTrigger.dataset.ntListDetailSelected === 'true'
                && selectedTrigger.getAttribute('aria-current') === 'true') {
                return;
            }

            setSelectedTrigger(view, selectedTrigger);
            view.querySelectorAll<HTMLElement>(panelSelector).forEach(panel => {
                panel.hidden = panel !== selectedPanel;
            });
            view.dataset.ntListDetailSelectedValue = selectedValue;

            return;
        }
    }

    let visiblePanel: HTMLElement | null = null;
    for (const panel of view.querySelectorAll<HTMLElement>(panelSelector)) {
        if (!panel.hidden) {
            visiblePanel = panel;
            break;
        }
    }

    if (!visiblePanel) {
        return;
    }

    const value = getPanelValue(visiblePanel);
    let matchingTrigger: HTMLElement | null = null;
    for (const trigger of view.querySelectorAll<HTMLElement>(triggerSelector)) {
        if (getTriggerValue(trigger) === value) {
            matchingTrigger = trigger;
            break;
        }
    }

    if (matchingTrigger) {
        setSelectedTrigger(view, matchingTrigger);
    }

    view.dataset.ntListDetailSelectedValue = value;
}

function enhanceView(view: NTListDetailViewElement): void {
    if (view.__ntListDetailRegistration) {
        syncInitialSelection(view);
        return;
    }

    const clickHandler = (event: MouseEvent) => {
        if (shouldLetBrowserHandleClick(event)) {
            return;
        }

        const target = event.target instanceof Element ? event.target : null;

        if (!target) {
            return;
        }

        const back = target.closest<HTMLElement>(backSelector);
        if (back && back.closest(viewSelector) === view) {
            event.preventDefault();
            closeDetail(view);
            return;
        }

        const trigger = target.closest<HTMLElement>(triggerSelector);
        if (!trigger || trigger.closest(viewSelector) !== view) {
            return;
        }

        if (selectTrigger(view, trigger)) {
            event.preventDefault();
        }
    };

    const submitHandler = (event: SubmitEvent) => {
        const target = event.target instanceof Element ? event.target : null;
        const back = target?.closest<HTMLElement>(backSelector);

        if (!back || back.closest(viewSelector) !== view) {
            return;
        }

        event.preventDefault();
        closeDetail(view);
    };

    view.addEventListener('click', clickHandler);
    view.addEventListener('submit', submitHandler);
    view.__ntListDetailRegistration = {
        clickHandler,
        submitHandler
    };

    syncInitialSelection(view);
}

function disposeView(view: NTListDetailViewElement): void {
    const registration = view.__ntListDetailRegistration;

    if (!registration) {
        return;
    }

    view.removeEventListener('click', registration.clickHandler);
    view.removeEventListener('submit', registration.submitHandler);
    delete view.__ntListDetailRegistration;
}

function sync(element?: Element | null): void {
    getViews(element).forEach(enhanceView);
}

function scheduleGlobalSync(): void {
    if (globalSyncScheduled) {
        return;
    }

    globalSyncScheduled = true;

    queueMicrotask(() => {
        globalSyncScheduled = false;
        sync();
    });
}

export function onLoad(element?: Element | null): void {
    sync(element);
}

export function onUpdate(element?: Element | null): void {
    if (element == null) {
        scheduleGlobalSync();
        return;
    }

    sync(element);
}

export function onDispose(element?: Element | null): void {
    getViews(element).forEach(disposeView);
}
