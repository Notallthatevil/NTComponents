const tabViewsByIdentifier = new Map();

export class TnTTabView extends HTMLElement {
    static observedAttributes = [NTComponents.customAttribute];
    constructor() {
        super();
        this.activeIndex = -1;
        this.tabViews = [];
        this.resizeObserver = null;
    }

    disconnectedCallback() {
        let identifier = this.getAttribute(NTComponents.customAttribute);
        if (tabViewsByIdentifier.get(identifier)) {
            tabViewsByIdentifier.delete(identifier);
        }
    }

    attributeChangedCallback(name, oldValue, newValue) {
        if (name === NTComponents.customAttribute && oldValue != newValue) {
            if (tabViewsByIdentifier.get(oldValue)) {
                tabViewsByIdentifier.delete(oldValue);
            }
            tabViewsByIdentifier.set(newValue, this);
            this.update().then(() => {
                if (this.resizeObserver) {
                    this.resizeObserver.disconnect();
                }
                this.resizeObserver = new ResizeObserver((_) => {
                    this.updateActiveIndicator();
                });
                this.resizeObserver.observe(this);
            });
        }
    }

    async update() {
        this.tabViews = [];
        this.querySelectorAll('.tnt-tab-child').forEach((element, index) => {
            this.tabViews.push(element);
        });

        const headerButtons = this.getHeaderButtons();
        const firstEnabledIndex = this.findNextEnabledIndex(-1, 1);
        if (firstEnabledIndex === -1) {
            this.activeIndex = -1;
        } else if (this.activeIndex === -1 || this.isTabDisabled(this.activeIndex, headerButtons)) {
            this.activeIndex = firstEnabledIndex;
        }

        headerButtons.forEach((button, index) => {
            if (button._tntTabClickHandler) {
                button.removeEventListener('click', button._tntTabClickHandler);
            }
            if (button._tntTabKeydownHandler) {
                button.removeEventListener('keydown', button._tntTabKeydownHandler);
            }

            button._tntTabClickHandler = () => this.activateTab(index, true);
            button._tntTabKeydownHandler = (event) => this.handleHeaderKeyDown(event, index);

            button.addEventListener('click', button._tntTabClickHandler);
            button.addEventListener('keydown', button._tntTabKeydownHandler);
        });

        this.activateTab(this.activeIndex, false);
        this.updateActiveIndicator();
    }

    getHeaderButtons() {
        return Array.from(this.querySelectorAll(':scope > .tnt-tab-view-header > .tnt-tab-view-header-buttons > .tnt-tab-view-button'));
    }

    isTabDisabled(index, headerButtons = null) {
        const buttons = headerButtons ?? this.getHeaderButtons();
        const button = buttons[index];
        if (button?.disabled || button?.getAttribute('aria-disabled') === 'true') {
            return true;
        }

        return this.tabViews[index]?.classList.contains('tnt-disabled') ?? false;
    }

    findNextEnabledIndex(startIndex, direction) {
        const headerButtons = this.getHeaderButtons();
        const tabCount = Math.max(headerButtons.length, this.tabViews.length);
        if (tabCount === 0) {
            return -1;
        }

        let index = startIndex;
        for (let count = 0; count < tabCount; count++) {
            index = (index + direction + tabCount) % tabCount;
            if (!this.isTabDisabled(index, headerButtons)) {
                return index;
            }
        }

        return -1;
    }

    activateTab(index, focusButton) {
        const headerButtons = this.getHeaderButtons();
        if (index < 0 || index >= this.tabViews.length || this.isTabDisabled(index, headerButtons)) {
            return;
        }

        headerButtons.forEach((button, buttonIndex) => {
            const selected = buttonIndex === index;
            button.classList.toggle('tnt-active', selected);
            button.setAttribute('aria-selected', selected ? 'true' : 'false');
            button.setAttribute('tabindex', selected ? '0' : '-1');
        });

        this.tabViews.forEach((panel, panelIndex) => {
            const selected = panelIndex === index;
            panel.classList.toggle('tnt-active', selected);
            panel.hidden = !selected;
        });

        this.activeIndex = index;
        if (focusButton) {
            headerButtons[index]?.focus();
        }
        this.updateActiveIndicator();
    }

    handleHeaderKeyDown(event, index) {
        let nextIndex = -1;

        if (event.key === 'ArrowRight') {
            nextIndex = this.findNextEnabledIndex(index, 1);
        } else if (event.key === 'ArrowLeft') {
            nextIndex = this.findNextEnabledIndex(index, -1);
        } else if (event.key === 'Home') {
            nextIndex = this.findNextEnabledIndex(-1, 1);
        } else if (event.key === 'End') {
            nextIndex = this.findNextEnabledIndex(0, -1);
        } else if (event.key === 'Enter' || event.key === ' ') {
            event.preventDefault();
            this.activateTab(index, true);
            return;
        } else {
            return;
        }

        event.preventDefault();
        if (nextIndex !== -1) {
            this.activateTab(nextIndex, true);
        }
    }

    getActiveHeader() {
        let headerButtons = this.querySelectorAll(':scope > .tnt-tab-view-header > .tnt-tab-view-header-buttons > .tnt-tab-view-button');
        if (headerButtons && this.activeIndex >= 0 && headerButtons.length > this.activeIndex) {
            return headerButtons[this.activeIndex];
        }

        return null;
    }

    async updateActiveIndicator() {
        const activeHeader = this.getActiveHeader();
        let activeIndicator = this.querySelector(":scope > .tnt-tab-view-header > .tnt-tab-view-active-indicator");
        if (!activeHeader || !activeIndicator) {
            if (activeIndicator) {
                activeIndicator.style.display = 'none';
            }
            return;
        }
        activeIndicator.style.display = 'block';
        const boundingRect = activeHeader.getBoundingClientRect();
        const parentScrollLeft = activeHeader.parentElement.scrollLeft;
        const diff = boundingRect.left + parentScrollLeft - activeHeader.offsetLeft;
        if (!this.classList.contains('tnt-tab-view-secondary')) {
            const headerElementWidth = activeHeader.clientWidth / 2;
            activeIndicator.style.left = `${(boundingRect.left + headerElementWidth) - (activeIndicator.clientWidth / 2) - diff}px`;
        }
        else {
            activeIndicator.style.left = `${boundingRect.left - diff}px`;
            activeIndicator.style.width = `${activeHeader.clientWidth}px`;
        }
    }
}

export function onLoad(element, dotNetRef) {
    if (!customElements.get('tnt-tab-view')) {
        customElements.define('tnt-tab-view', TnTTabView);
    }

    if (dotNetRef) {
        element.update();
    }
}

export function onUpdate(element, dotNetRef) {
    if (dotNetRef) {
        element.update();
    }
    NTComponents.setupRipple();
}

export function onDispose(element, dotNetRef) {
}
