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
            if ((this.activeIndex === -1 || this.activeIndex === index) && !element.classList.contains('tnt-disabled')) {
                this.activeIndex = index;
                if (!element.classList.contains('tnt-active')) {
                    element.classList.add('tnt-active');
                }
            }
            else {
                element.classList.remove('tnt-active');
            }
        });

        let self = this;

        this.querySelectorAll(':scope > .tnt-tab-view-header > .tnt-tab-view-header-buttons > .tnt-tab-view-button').forEach((button, index) => {
            function setActiveTab(e) {
                if (self) {
                    const headerButtons = self.querySelectorAll(":scope > .tnt-tab-view-header > .tnt-tab-view-header-buttons > .tnt-tab-view-button");
                    headerButtons.forEach(btn => {
                        if (btn.classList.contains('tnt-active')) {
                            btn.classList.remove('tnt-active');
                        }
                    });

                    e.target.classList.add('tnt-active');

                    if (index >= 0 && self.tabViews.length > index) {
                        if (self.tabViews[self.activeIndex].classList.contains('tnt-active')) {
                            self.tabViews[self.activeIndex].classList.remove('tnt-active');
                        }

                        if (!self.tabViews[index].classList.contains('tnt-active')) {
                            self.tabViews[index].classList.add('tnt-active');
                        }

                        self.activeIndex = index;
                        self.updateActiveIndicator();
                    }
                }
            }

            button.addEventListener('click', setActiveTab);
        });

        this.updateActiveIndicator();
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
