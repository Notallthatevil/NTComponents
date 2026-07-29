class NTTooltip extends HTMLElement {
    constructor() {
        super();
        this.showTimeoutId = null;
        this.hideTimeoutId = null;
        this.isVisible = false;
        this.lastPointerX = null;
        this.lastPointerY = null;
        this.anchorElement = null;
        this.describedByElements = new Set();
        this.describedById = null;

        this.handleMouseEnter = (e) => this.onMouseEnter(e);
        this.handleMouseLeave = (e) => this.onMouseLeave(e);
        this.handleMouseMove = (e) => this.onMouseMove(e);
        this.handleFocusIn = (e) => this.onFocusIn(e);
        this.handleFocusOut = (e) => this.onFocusOut(e);
        this.handleTooltipMouseEnter = () => this.queueShow();
        this.handleTooltipMouseLeave = (e) => this.queueHide(e);
        this.handleTooltipFocusIn = () => this.queueShow();
        this.handleTooltipFocusOut = (e) => this.queueHide(e);
        this.handleKeyDown = (e) => this.onKeyDown(e);
        this.handleViewportChange = () => this.onViewportChange();
    }

    connectedCallback() {
        this.initialize();
    }

    disconnectedCallback() {
        this.dispose();
    }

    initialize() {
        const parentElement = this.parentElement;
        if (!parentElement) return;
        if (this.anchorElement === parentElement) return;
        if (this.anchorElement) {
            this.dispose();
        }

        this.anchorElement = parentElement;
        this.style.left = '-9999px';
        this.style.top = '-9999px';
        this.setAttribute('aria-hidden', 'true');
        this.ensureId();
        this.syncDescribedBy();

        parentElement.addEventListener('mouseenter', this.handleMouseEnter);
        parentElement.addEventListener('mouseleave', this.handleMouseLeave);
        parentElement.addEventListener('mousemove', this.handleMouseMove);
        parentElement.addEventListener('focusin', this.handleFocusIn);
        parentElement.addEventListener('focusout', this.handleFocusOut);
        parentElement.addEventListener('keydown', this.handleKeyDown);
        this.addEventListener('mouseenter', this.handleTooltipMouseEnter);
        this.addEventListener('mouseleave', this.handleTooltipMouseLeave);
        this.addEventListener('focusin', this.handleTooltipFocusIn);
        this.addEventListener('focusout', this.handleTooltipFocusOut);
        this.addEventListener('keydown', this.handleKeyDown);
    }

    dispose() {
        const parentElement = this.anchorElement;

        if (parentElement) {
            parentElement.removeEventListener('mouseenter', this.handleMouseEnter);
            parentElement.removeEventListener('mouseleave', this.handleMouseLeave);
            parentElement.removeEventListener('mousemove', this.handleMouseMove);
            parentElement.removeEventListener('focusin', this.handleFocusIn);
            parentElement.removeEventListener('focusout', this.handleFocusOut);
            parentElement.removeEventListener('keydown', this.handleKeyDown);
        }

        this.removeEventListener('mouseenter', this.handleTooltipMouseEnter);
        this.removeEventListener('mouseleave', this.handleTooltipMouseLeave);
        this.removeEventListener('focusin', this.handleTooltipFocusIn);
        this.removeEventListener('focusout', this.handleTooltipFocusOut);
        this.removeEventListener('keydown', this.handleKeyDown);
        this.detachVisibleListeners();
        this.clearTimeouts();
        this.removeDescribedBy();
        this.anchorElement = null;
    }

    ensureId() {
        if (this.id) return;

        const randomId = globalThis.crypto?.randomUUID?.() ?? Math.random().toString(36).slice(2);
        this.id = `nt-tooltip-${randomId}`;
    }

    syncDescribedBy(targetElement = this.anchorElement) {
        if (!(targetElement instanceof Element)) return;
        this.ensureId();

        if (this.describedById && this.describedById !== this.id) {
            this.removeDescribedBy();
        }

        this.addDescribedBy(targetElement, this.id);
    }

    addDescribedBy(element, id) {
        const currentValue = element.getAttribute('aria-describedby') ?? '';
        if (this.describedById === id && this.describedByElements.has(element) && this.hasToken(currentValue, id)) return;

        if (!this.hasToken(currentValue, id)) {
            element.setAttribute('aria-describedby', currentValue ? `${currentValue} ${id}` : id);
        }

        this.describedByElements.add(element);
        this.describedById = id;
    }

    removeDescribedBy(elementToRemove = null) {
        const id = this.describedById;
        if (!id) return;

        const elements = elementToRemove ? [elementToRemove] : Array.from(this.describedByElements);
        for (const element of elements) {
            const values = (element.getAttribute('aria-describedby') ?? '').split(/\s+/).filter((value) => value && value !== id);
            if (values.length > 0) {
                element.setAttribute('aria-describedby', values.join(' '));
            } else {
                element.removeAttribute('aria-describedby');
            }
            this.describedByElements.delete(element);
        }

        if (this.describedByElements.size === 0) {
            this.describedById = null;
        }
    }

    hasToken(value, token) {
        return ` ${value} `.includes(` ${token} `);
    }

    clearTimeouts() {
        if (this.showTimeoutId !== null) {
            clearTimeout(this.showTimeoutId);
            this.showTimeoutId = null;
        }
        if (this.hideTimeoutId !== null) {
            clearTimeout(this.hideTimeoutId);
            this.hideTimeoutId = null;
        }
    }

    onMouseEnter(e) {
        this.updatePointer(e);
        this.syncDescribedBy(this.anchorElement);
        this.queueShow();
    }

    onMouseLeave(e) {
        this.queueHide(e);
    }

    onMouseMove(e) {
        this.updatePointer(e);
        if (this.isVisible) {
            this.updatePosition();
        }
    }

    onFocusIn(e) {
        this.clearPointer();
        this.syncDescribedBy(e?.target);
        this.queueShow();
    }

    onFocusOut(e) {
        this.queueHide(e);
    }

    onKeyDown(e) {
        if (e.key !== 'Escape' || !this.isVisible) return;

        this.hide();
    }

    onViewportChange() {
        if (this.isVisible) {
            this.updatePosition();
        }
    }

    readDuration(propertyName, fallback) {
        const value = parseInt(getComputedStyle(this).getPropertyValue(propertyName));
        return Number.isFinite(value) && value >= 0 ? value : fallback;
    }

    updatePointer(e) {
        if (!e) return;

        this.lastPointerX = e.clientX;
        this.lastPointerY = e.clientY;
    }

    clearPointer() {
        this.lastPointerX = null;
        this.lastPointerY = null;
    }

    queueShow() {
        this.clearTimeouts();

        const showDelay = this.readDuration('--nt-tooltip-show-delay', 500);

        this.showTimeoutId = setTimeout(() => {
            this.show();
            this.showTimeoutId = null;
        }, showDelay);
    }

    queueHide(e) {
        if (this.shouldKeepOpen(e?.relatedTarget)) return;

        this.clearTimeouts();

        const hideDelay = this.readDuration('--nt-tooltip-hide-delay', 200);

        this.hideTimeoutId = setTimeout(() => {
            this.hide();
            this.hideTimeoutId = null;
        }, hideDelay);
    }

    shouldKeepOpen(nextTarget) {
        return nextTarget instanceof Node && this.anchorElement?.contains(nextTarget);
    }

    show() {
        if (this.isVisible) return;

        this.updatePosition();
        this.isVisible = true;
        this.setAttribute('aria-hidden', 'false');
        this.classList.add('nt-tooltip-visible');
        this.attachVisibleListeners();
    }

    hide() {
        if (!this.isVisible) return;

        this.isVisible = false;
        this.setAttribute('aria-hidden', 'true');
        this.classList.remove('nt-tooltip-visible');
        this.style.left = '-9999px';
        this.style.top = '-9999px';
        this.detachVisibleListeners();
    }

    attachVisibleListeners() {
        document.addEventListener('keydown', this.handleKeyDown);
        window.addEventListener('resize', this.handleViewportChange);
        window.addEventListener('scroll', this.handleViewportChange, true);
    }

    detachVisibleListeners() {
        document.removeEventListener('keydown', this.handleKeyDown);
        window.removeEventListener('resize', this.handleViewportChange);
        window.removeEventListener('scroll', this.handleViewportChange, true);
    }

    updatePosition() {
        const parentElement = this.anchorElement ?? this.parentElement;
        const anchorRect = parentElement?.getBoundingClientRect();
        const hasAnchorRect = anchorRect && (anchorRect.width > 0 || anchorRect.height > 0 || anchorRect.top !== 0 || anchorRect.left !== 0);
        const hasPointer = Number.isFinite(this.lastPointerX) && Number.isFinite(this.lastPointerY);
        const anchorX = hasPointer ? this.lastPointerX : hasAnchorRect ? anchorRect.left + anchorRect.width / 2 : window.innerWidth / 2;
        const anchorTop = hasPointer ? this.lastPointerY : hasAnchorRect ? anchorRect.top : window.innerHeight / 2;
        const anchorBottom = hasPointer ? this.lastPointerY : hasAnchorRect ? anchorRect.bottom : window.innerHeight / 2;
        const computedStyle = getComputedStyle(this);
        const offset = parseFloat(computedStyle.getPropertyValue('--nt-tooltip-position-offset')) || 8;
        const viewportMargin = parseFloat(computedStyle.getPropertyValue('--nt-tooltip-viewport-margin')) || 8;
        const tooltipRect = this.getBoundingClientRect();
        const tooltipWidth = tooltipRect.width;
        const tooltipHeight = tooltipRect.height;
        const viewportWidth = window.innerWidth;
        const viewportHeight = window.innerHeight;

        let left = anchorX - (tooltipWidth / 2);
        let top = anchorTop - tooltipHeight - offset;

        if (top < viewportMargin) {
            top = anchorBottom + offset;
        }

        if (left < viewportMargin) {
            left = viewportMargin;
        }

        if (left + tooltipWidth > viewportWidth - viewportMargin) {
            left = viewportWidth - tooltipWidth - viewportMargin;
        }

        if (top + tooltipHeight > viewportHeight - viewportMargin) {
            top = viewportHeight - tooltipHeight - viewportMargin;
        }

        this.style.left = `${Math.max(viewportMargin, left)}px`;
        this.style.top = `${Math.max(viewportMargin, top)}px`;
    }
}

Object.defineProperty(NTTooltip.prototype, '__ntComponentsTooltip', { value: true });

export function onLoad(element, dotNetRef) {
    const existingConstructor = customElements.get('nt-tooltip');
    if (!existingConstructor) {
        customElements.define('nt-tooltip', NTTooltip);
    } else if (!existingConstructor.prototype.__ntComponentsTooltip) {
        throw new Error('The nt-tooltip custom element is already registered by a different implementation.');
    }
}

export function onUpdate(element, dotNetRef) {
    element?.syncDescribedBy?.();
    element?.onViewportChange?.();
}

export function onDispose(element, dotNetRef) {
    element?.dispose?.();
}
