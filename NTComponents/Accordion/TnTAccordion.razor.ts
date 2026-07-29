const accordionByIdentifier = new Map();

export class TnTAccordion extends HTMLElement {
    static observedAttributes = [NTComponents.customAttribute];
    constructor() {
        super();
        this.accordionChildren = [];
        this.identifier = '';
        this.dotNetRef = null;
    }

    disconnectedCallback() {
        let identifier = this.getAttribute(NTComponents.customAttribute);
        if (accordionByIdentifier.get(identifier)) {
            accordionByIdentifier.delete(identifier);
        }
    }

    attributeChangedCallback(name, oldValue, newValue) {
        if (name === NTComponents.customAttribute && oldValue != newValue) {
            if (accordionByIdentifier.get(oldValue)) {
                accordionByIdentifier.delete(oldValue);
            }
            accordionByIdentifier.set(newValue, this);
            this.identifier = newValue;

            
            this.update();
        }
    }

    limitToOneExpanded() { return this.classList.contains('tnt-limit-one-expanded'); }

    getAccordionChildren() {
        return this.querySelectorAll(':scope > .tnt-accordion-child');
    }

    getChildContent(child) {
        return child?.querySelector(':scope > [data-accordion-content="true"]')
            ?? child?.querySelector(':scope > div:last-child')
            ?? child?.lastElementChild
            ?? null;
    }

    getChildHeader(child) {
        return child?.querySelector(':scope > h3 > [data-accordion-header="true"]')
            ?? child?.querySelector(':scope > h3')
            ?? child?.firstElementChild
            ?? null;
    }

    resetAccordionElement(accordion) {
        if (typeof accordion?.resetChildren === 'function' && accordion !== this) {
            accordion.resetChildren();
            return;
        }

        accordion?.querySelectorAll?.(':scope > [data-accordion-child="true"], :scope > .tnt-accordion-child')
            ?.forEach((child) => {
                const content = accordion?.getChildContent?.(child)
                    ?? child?.querySelector?.(':scope > [data-accordion-content="true"]')
                    ?? child?.querySelector?.(':scope > div:last-child')
                    ?? child?.lastElementChild;
                const header = accordion?.getChildHeader?.(child)
                    ?? child?.querySelector?.(':scope > h3 > [data-accordion-header="true"]')
                    ?? child?.querySelector?.(':scope > h3')
                    ?? child?.firstElementChild;

                if (!content) {
                    return;
                }

                content.classList.remove('tnt-expanded');
                content.classList.remove('tnt-collapsed');
                content.setAttribute('aria-hidden', 'true');
                header?.setAttribute('aria-expanded', 'false');

                child?.querySelectorAll?.('tnt-accordion')?.forEach((nestedAccordion) => {
                    this.resetAccordionElement(nestedAccordion);
                });
            });
    }

    update() {
        this.accordionChildren = this.getAccordionChildren();
        this.accordionChildren.forEach((child) => {
            const content = this.getChildContent(child);
            if (content) {
                this.syncChildAccessibility(child, content.classList.contains('tnt-expanded'));
            }
        });
    }

    closeChildren(exclude) {
        this.getAccordionChildren().forEach((child) => {
            if (child !== exclude) {
                const content = this.getChildContent(child);
                if (content?.classList.contains('tnt-expanded')) {
                    this.setExpandedState(child, false);
                }
            }
        });
    }

    resetChildren() {
        this.getAccordionChildren().forEach((child) => {
            const content = this.getChildContent(child);
            if (!content) {
                return;
            }

            content.classList.remove('tnt-expanded');
            content.classList.remove('tnt-collapsed');
            this.syncChildAccessibility(child, false);

            const nestedAccordion = child.querySelectorAll('tnt-accordion');
            if (nestedAccordion) {
                nestedAccordion.forEach((accordion) => {
                    this.resetAccordionElement(accordion);
                });
            }
        });
    }

    updateChild() { }

    setExpandedState(child, expanded) {
        const content = this.getChildContent(child);
        if (!content) {
            return;
        }

        content.classList.toggle('tnt-expanded', expanded);
        content.classList.toggle('tnt-collapsed', !expanded);
        this.syncChildAccessibility(child, expanded);
    }

    syncChildAccessibility(child, expanded) {
        const content = this.getChildContent(child);
        const header = this.getChildHeader(child);
        if (!content || !header) {
            return;
        }

        content.setAttribute('aria-hidden', expanded ? 'false' : 'true');
        content.toggleAttribute('inert', !expanded);
        header.setAttribute('aria-expanded', expanded ? 'true' : 'false');
    }
}

export function onLoad(element, dotNetRef) {
    if (!customElements.get('tnt-accordion')) {
        customElements.define('tnt-accordion', TnTAccordion);
    }
    if (element) {
        element.dotNetRef = dotNetRef;
    }
}

export function onUpdate(element, dotNetRef) {
    if (element && element.update) {
        element.update();
        element.dotNetRef = dotNetRef;
    }
}

export function onDispose(element, dotNetRef) { }
