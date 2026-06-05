type Maybe<T> = T | null | undefined;

interface NTComponentsGlobals {
    registerButtonInteraction?: (element: Maybe<Element>) => void;
}

interface TabViewState {
    mutationObserver?: MutationObserver;
    onKeyDown?: (event: KeyboardEvent) => void;
    onScroll?: () => void;
    onTabListClick?: (event: MouseEvent) => void;
    resizeObserver?: ResizeObserver;
    tabList?: HTMLElement | null;
    updateFrame?: number;
}

interface NTTabViewElement extends HTMLElement {
    __ntTabViewState?: TabViewState;
}

const scriptTargets = new WeakMap<Element, NTTabViewElement[]>();

declare global {
    interface Window {
        NTComponents?: NTComponentsGlobals;
    }

    interface HTMLElementTagNameMap {
        'nt-tab-view': NTTabViewElement;
    }
}

function isTabViewElement(element: Maybe<Element>): element is NTTabViewElement {
    return element instanceof HTMLElement && element.matches('nt-tab-view.nt-tab-view');
}

function getTargetTabViews(scope: Maybe<Element>): NTTabViewElement[] {
    if (isTabViewElement(scope)) {
        return [scope];
    }

    if (scope) {
        const mappedTargets = scriptTargets.get(scope);
        if (mappedTargets) {
            return mappedTargets;
        }
    }

    const previousElement = scope?.previousElementSibling;
    if (isTabViewElement(previousElement)) {
        const targets = [previousElement];
        if (scope) {
            scriptTargets.set(scope, targets);
        }
        return targets;
    }

    const root = scope instanceof HTMLElement ? scope : document;
    const targets = Array.from(root.querySelectorAll<NTTabViewElement>('nt-tab-view.nt-tab-view'));
    if (scope) {
        scriptTargets.set(scope, targets);
    }
    return targets;
}

function getTabList(tabView: NTTabViewElement): HTMLElement | null {
    return tabView.querySelector<HTMLElement>(':scope > .nt-tab-view-header > .nt-tab-view-tablist');
}

function getIndicator(tabView: NTTabViewElement): HTMLElement | null {
    return tabView.querySelector<HTMLElement>(':scope > .nt-tab-view-header > .nt-tab-view-tablist > .nt-tab-view-active-indicator');
}

function getTabs(tabView: NTTabViewElement): HTMLButtonElement[] {
    return Array.from(tabView.querySelectorAll<HTMLButtonElement>(':scope > .nt-tab-view-header > .nt-tab-view-tablist > .nt-tab-view-tab[data-nt-tab-button="true"]'));
}

function getPanels(tabView: NTTabViewElement): HTMLElement[] {
    return Array.from(tabView.querySelectorAll<HTMLElement>(':scope > .nt-tab-view-panels > .nt-tab-view-panel[data-nt-tab-panel="true"]'));
}

function getEnabledTabs(tabView: NTTabViewElement): HTMLButtonElement[] {
    return getTabs(tabView).filter(tab => !tab.disabled && tab.getAttribute('aria-disabled') !== 'true');
}

function getSelectedTab(tabView: NTTabViewElement): HTMLButtonElement | null {
    return getTabs(tabView).find(tab => tab.getAttribute('aria-selected') === 'true' && !tab.disabled) ?? getEnabledTabs(tabView)[0] ?? null;
}

function getQueryParameterName(tabView: NTTabViewElement): string | null {
    const parameterName = tabView.dataset.ntTabQueryParameter?.trim();
    return parameterName ? parameterName : null;
}

function valuesEqual(left: Maybe<string>, right: Maybe<string>): boolean {
    return (left ?? '').localeCompare(right ?? '', undefined, { sensitivity: 'accent' }) === 0;
}

function findTabByValue(tabView: NTTabViewElement, value: Maybe<string>): HTMLButtonElement | null {
    if (!value) {
        return null;
    }

    return getEnabledTabs(tabView).find(tab => valuesEqual(tab.dataset.ntTabValue, value)) ?? null;
}

function findPanelForTab(tabView: NTTabViewElement, tab: HTMLButtonElement): HTMLElement | null {
    const panelId = tab.getAttribute('aria-controls');
    if (panelId) {
        const panel = document.getElementById(panelId);
        if (panel instanceof HTMLElement && tabView.contains(panel)) {
            return panel;
        }
    }

    const tabValue = tab.dataset.ntTabValue;
    return getPanels(tabView).find(panel => valuesEqual(panel.dataset.ntTabValue, tabValue)) ?? null;
}

function updateQueryString(tabView: NTTabViewElement, tab: HTMLButtonElement): void {
    if (tabView.dataset.ntTabUpdateQuery !== 'true') {
        return;
    }

    const parameterName = getQueryParameterName(tabView);
    const tabValue = tab.dataset.ntTabValue;
    if (!parameterName || !tabValue || typeof window.history?.replaceState !== 'function') {
        return;
    }

    const url = new URL(window.location.href);
    url.searchParams.set(parameterName, tabValue);
    window.history.replaceState(window.history.state, '', url);
}

function scrollTabIntoView(tab: HTMLButtonElement): void {
    if (typeof tab.scrollIntoView === 'function') {
        const reducedMotion = window.matchMedia?.('(prefers-reduced-motion: reduce)').matches;
        tab.scrollIntoView({ behavior: reducedMotion ? 'auto' : 'smooth', block: 'nearest', inline: 'nearest' });
    }
}

function selectTab(tabView: NTTabViewElement, selectedTab: HTMLButtonElement, updateUrl: boolean): void {
    if (selectedTab.disabled || selectedTab.getAttribute('aria-disabled') === 'true') {
        return;
    }

    getTabs(tabView).forEach(tab => {
        const selected = tab === selectedTab;
        tab.classList.toggle('nt-tab-view-tab-selected', selected);
        tab.setAttribute('aria-selected', selected ? 'true' : 'false');
        tab.tabIndex = selected ? 0 : -1;
    });

    const selectedPanel = getSelectedPanel(tabView, selectedTab);
    getPanels(tabView).forEach(panel => {
        const selected = panel === selectedPanel;
        panel.classList.toggle('nt-tab-view-panel-selected', selected);
        panel.toggleAttribute('hidden', !selected);
    });

    if (updateUrl) {
        updateQueryString(tabView, selectedTab);
    }

    scrollTabIntoView(selectedTab);
    scheduleIndicatorUpdate(tabView);
}

function setFocusedTab(tabView: NTTabViewElement, focusedTab: HTMLButtonElement): void {
    getTabs(tabView).forEach(tab => {
        tab.tabIndex = tab === focusedTab ? 0 : -1;
    });
}

function selectInitialTab(tabView: NTTabViewElement, useQueryString: boolean): void {
    const parameterName = getQueryParameterName(tabView);
    const queryValue = useQueryString && parameterName ? new URLSearchParams(window.location.search).get(parameterName) : null;
    const queryTab = findTabByValue(tabView, queryValue);
    const selectedTab = queryTab ?? getSelectedTab(tabView);

    if (selectedTab) {
        selectTab(tabView, selectedTab, false);
    } else {
        scheduleIndicatorUpdate(tabView);
    }
}

function moveFocus(tabView: NTTabViewElement, currentTab: HTMLButtonElement, direction: -1 | 1): void {
    const tabs = getEnabledTabs(tabView);
    const currentIndex = tabs.indexOf(currentTab);
    const nextIndex = currentIndex + direction;
    const nextTab = tabs[nextIndex];

    if (!nextTab) {
        return;
    }

    setFocusedTab(tabView, nextTab);
    nextTab.focus();
    scrollTabIntoView(nextTab);
    scheduleIndicatorUpdate(tabView);
}

function focusEdgeTab(tabView: NTTabViewElement, edge: 'first' | 'last'): void {
    const tabs = getEnabledTabs(tabView);
    const tab = edge === 'first' ? tabs[0] : tabs.at(-1);
    if (tab) {
        setFocusedTab(tabView, tab);
        tab.focus();
        scrollTabIntoView(tab);
    }
    scheduleIndicatorUpdate(tabView);
}

function handleKeyDown(tabView: NTTabViewElement, event: KeyboardEvent): void {
    const currentTab = event.target instanceof HTMLButtonElement && event.target.matches('.nt-tab-view-tab')
        ? event.target
        : null;

    if (!currentTab) {
        return;
    }

    if (event.key === 'ArrowRight') {
        event.preventDefault();
        moveFocus(tabView, currentTab, 1);
    } else if (event.key === 'ArrowLeft') {
        event.preventDefault();
        moveFocus(tabView, currentTab, -1);
    } else if (event.key === 'Home') {
        event.preventDefault();
        focusEdgeTab(tabView, 'first');
    } else if (event.key === 'End') {
        event.preventDefault();
        focusEdgeTab(tabView, 'last');
    } else if (event.key === ' ' || event.key === 'Enter') {
        event.preventDefault();
        selectTab(tabView, currentTab, true);
    }
}

function getIndicatorTarget(tabView: NTTabViewElement, tab: HTMLButtonElement): HTMLElement {
    return tabView.classList.contains('nt-tab-view-secondary')
        ? tab
        : tab.querySelector<HTMLElement>(':scope > .nt-tab-view-indicator-target') ?? tab;
}

function getSelectedPanel(tabView: NTTabViewElement, selectedTab: HTMLButtonElement): HTMLElement | null {
    return findPanelForTab(tabView, selectedTab);
}

function updateIndicator(tabView: NTTabViewElement): void {
    const tabList = getTabList(tabView);
    const indicator = getIndicator(tabView);
    const selectedTab = getSelectedTab(tabView);

    if (!tabList || !indicator || !selectedTab) {
        if (indicator) {
            indicator.style.opacity = '0';
        }
        return;
    }

    const target = getIndicatorTarget(tabView, selectedTab);
    const tabListRect = tabList.getBoundingClientRect();
    const targetRect = target.getBoundingClientRect();
    const targetWidth = Math.max(24, Math.round(targetRect.width));
    const indicatorX = Math.round(targetRect.left - tabListRect.left + tabList.scrollLeft);

    tabView.style.setProperty('--nt-tab-view-active-indicator-x', `${indicatorX}px`);
    tabView.style.setProperty('--nt-tab-view-active-indicator-width', `${targetWidth}px`);
    indicator.style.opacity = '1';
}

function scheduleIndicatorUpdate(tabView: NTTabViewElement): void {
    const state = tabView.__ntTabViewState;
    if (!state) {
        updateIndicator(tabView);
        return;
    }

    if (state.updateFrame !== undefined) {
        return;
    }

    state.updateFrame = window.requestAnimationFrame
        ? window.requestAnimationFrame(() => {
            delete state.updateFrame;
            updateIndicator(tabView);
        })
        : window.setTimeout(() => {
            delete state.updateFrame;
            updateIndicator(tabView);
        }, 0);
}

function registerButtonInteractions(tabView: NTTabViewElement): void {
    const registerButtonInteraction = window.NTComponents?.registerButtonInteraction;
    if (!registerButtonInteraction) {
        return;
    }

    getTabs(tabView).forEach(tab => registerButtonInteraction(tab));
}

function registerTabs(tabView: NTTabViewElement, _state: TabViewState): void {
    registerButtonInteractions(tabView);
}

function syncTabList(tabView: NTTabViewElement, state: TabViewState): void {
    const tabList = getTabList(tabView);
    if (state.tabList === tabList) {
        return;
    }

    if (state.onTabListClick) {
        state.tabList?.removeEventListener('click', state.onTabListClick);
        tabList?.addEventListener('click', state.onTabListClick);
    }
    if (state.onScroll) {
        state.tabList?.removeEventListener('scroll', state.onScroll);
        tabList?.addEventListener('scroll', state.onScroll, { passive: true });
    }

    state.resizeObserver?.disconnect();
    if (typeof ResizeObserver === 'function') {
        state.resizeObserver = new ResizeObserver(() => scheduleIndicatorUpdate(tabView));
        state.resizeObserver.observe(tabView);
        if (tabList) {
            state.resizeObserver.observe(tabList);
        }
    }

    state.mutationObserver?.disconnect();
    if (typeof MutationObserver === 'function') {
        state.mutationObserver = new MutationObserver(() => {
            registerTabs(tabView, state);
            selectInitialTab(tabView, false);
        });
        if (tabList) {
            state.mutationObserver.observe(tabList, { childList: true });
        }
        const panels = tabView.querySelector<HTMLElement>(':scope > .nt-tab-view-panels');
        if (panels) {
            state.mutationObserver.observe(panels, { childList: true });
        }
    }

    state.tabList = tabList;
    scheduleIndicatorUpdate(tabView);
}

function updateTabView(tabView: NTTabViewElement): void {
    const existingState = tabView.__ntTabViewState;
    if (existingState) {
        syncTabList(tabView, existingState);
        registerTabs(tabView, existingState);
        selectInitialTab(tabView, false);
        return;
    }

    const state: TabViewState = {};
    state.onKeyDown = event => handleKeyDown(tabView, event);
    tabView.addEventListener('keydown', state.onKeyDown);

    const tabList = getTabList(tabView);
    state.tabList = tabList;
    state.onTabListClick = (event: MouseEvent) => {
        const tab = event.target instanceof Element
            ? event.target.closest<HTMLButtonElement>('.nt-tab-view-tab[data-nt-tab-button="true"]')
            : null;

        if (!tab || !tabView.contains(tab)) {
            return;
        }

        event.preventDefault();
        selectTab(tabView, tab, true);
        tab.focus();
    };
    tabList?.addEventListener('click', state.onTabListClick);

    state.onScroll = () => scheduleIndicatorUpdate(tabView);
    tabList?.addEventListener('scroll', state.onScroll, { passive: true });

    if (typeof ResizeObserver === 'function') {
        state.resizeObserver = new ResizeObserver(() => scheduleIndicatorUpdate(tabView));
        state.resizeObserver.observe(tabView);
        if (tabList) {
            state.resizeObserver.observe(tabList);
        }
    } else {
        window.addEventListener('resize', state.onScroll);
    }

    if (typeof MutationObserver === 'function') {
        state.mutationObserver = new MutationObserver(() => {
            registerTabs(tabView, state);
            selectInitialTab(tabView, false);
        });
        if (tabList) {
            state.mutationObserver.observe(tabList, { childList: true });
        }
        const panels = tabView.querySelector<HTMLElement>(':scope > .nt-tab-view-panels');
        if (panels) {
            state.mutationObserver.observe(panels, { childList: true });
        }
    }

    tabView.__ntTabViewState = state;
    registerTabs(tabView, state);
    selectInitialTab(tabView, true);
}

function disposeTabView(tabView: Maybe<NTTabViewElement>): void {
    const state = tabView?.__ntTabViewState;
    if (!tabView || !state) {
        return;
    }

    if (state.onKeyDown) {
        tabView.removeEventListener('keydown', state.onKeyDown);
    }
    if (state.onTabListClick) {
        state.tabList?.removeEventListener('click', state.onTabListClick);
    }
    if (state.onScroll) {
        state.tabList?.removeEventListener('scroll', state.onScroll);
        window.removeEventListener('resize', state.onScroll);
    }
    if (state.updateFrame !== undefined) {
        if (window.cancelAnimationFrame) {
            window.cancelAnimationFrame(state.updateFrame);
        } else {
            window.clearTimeout(state.updateFrame);
        }
    }

    state.resizeObserver?.disconnect();
    state.mutationObserver?.disconnect();
    delete tabView.__ntTabViewState;
}

export function onLoad(scope?: Maybe<Element>): void {
    getTargetTabViews(scope).forEach(updateTabView);
}

export function onUpdate(scope?: Maybe<Element>): void {
    getTargetTabViews(scope).forEach(updateTabView);
}

export function onDispose(scope?: Maybe<Element>): void {
    getTargetTabViews(scope).forEach(disposeTabView);
}

export const __testHooks = {
    findTabByValue,
    getTargetTabViews,
    selectTab,
    updateIndicator
};
