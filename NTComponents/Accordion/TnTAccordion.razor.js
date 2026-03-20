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

    clearPendingAriaHidden(content) {
        if (content?.accordionAriaHiddenHandler) {
            content.removeEventListener('animationend', content.accordionAriaHiddenHandler);
            content.accordionAriaHiddenHandler = undefined;
        }
    }

    scheduleAriaHidden(content) {
        if (!content) {
            return;
        }

        this.clearPendingAriaHidden(content);
        const onAnimationEnd = () => {
            if (content.classList.contains('tnt-collapsed') && !content.classList.contains('tnt-expanded')) {
                content.setAttribute('aria-hidden', 'true');
            }

            this.clearPendingAriaHidden(content);
        };

        content.accordionAriaHiddenHandler = onAnimationEnd;
        content.addEventListener('animationend', onAnimationEnd, { once: true });
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
                this.updateChild(content);
            }
        });
    }

    closeChildren(exclude) {
        this.getAccordionChildren().forEach((child) => {
            if (child !== exclude) {
                const content = this.getChildContent(child);
                if (content?.classList.contains('tnt-expanded')) {
                    this.setExpandedState(child, false);
                    this.updateChild(content);
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

    updateChild(content) {
        if (!content) {
            return;
        }

        if (content.resizeObserver) {
            content.resizeObserver.disconnect();
            content.resizeObserver = undefined;
        }

        if (content.mutationObserver) {
            content.mutationObserver.disconnect();
            content.mutationObserver = undefined;
        }

        if (content.classList.contains('tnt-expanded')) {
            this.clearPendingAriaHidden(content);
            content.style.setProperty('--content-height', content.scrollHeight + 'px');

            content.resizeObserver = new ResizeObserver(() => {
                content.style.setProperty('--content-height', content.scrollHeight + 'px');
            });

            content.mutationObserver = new MutationObserver((mutationList) => {
                for (const mutation of mutationList) {
                    if (mutation.type === 'childList') {
                        content.style.setProperty('--content-height', content.scrollHeight + 'px');
                    }
                }
            });

            content.resizeObserver.observe(document.body);
            content.mutationObserver.observe(content, { childList: true, subtree: true });
        }
        else {
            content.style.height = null;
        }
    }

    setExpandedState(child, expanded) {
        const content = this.getChildContent(child);
        if (!content) {
            return;
        }

        if (!expanded) {
            content.style.setProperty('--content-height', `${content.scrollHeight}px`);
        }

        content.classList.toggle('tnt-expanded', expanded);
        content.classList.toggle('tnt-collapsed', !expanded);
        const header = this.getChildHeader(child);
        header?.setAttribute('aria-expanded', expanded ? 'true' : 'false');
        if (expanded) {
            content.setAttribute('aria-hidden', 'false');
        }
        else {
            content.setAttribute('aria-hidden', 'false');
            this.scheduleAriaHidden(content);
        }
    }

    syncChildAccessibility(child, expanded) {
        const content = this.getChildContent(child);
        const header = this.getChildHeader(child);
        if (!content || !header) {
            return;
        }

        content.setAttribute('aria-hidden', expanded ? 'false' : 'true');
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
