class TnTTooltip extends HTMLElement {
    constructor() {
        super();
        this.showTimeoutId = null;
        this.hideTimeoutId = null;
        this.isVisible = false;
        this.lastMouseX = 0;
        this.lastMouseY = 0;
        this.showFromAnchor = false;

        this.handleMouseEnter = () => this.onMouseEnter();
        this.handleMouseLeave = () => this.onMouseLeave();
        this.handleMouseMove = (e) => this.onMouseMove(e);
        this.handleFocusIn = (event) => this.onFocusIn(event);
        this.handleFocusOut = (event) => this.onFocusOut(event);
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

        // Set initial off-screen position to prevent flickering
        this.style.left = '-9999px';
        this.style.top = '-9999px';

        parentElement.addEventListener('mouseenter', this.handleMouseEnter);
        parentElement.addEventListener('mouseleave', this.handleMouseLeave);
        parentElement.addEventListener('mousemove', this.handleMouseMove);
        parentElement.addEventListener('focusin', this.handleFocusIn);
        parentElement.addEventListener('focusout', this.handleFocusOut);

        if (this.id) {
            const describedByTokens = new Set((parentElement.getAttribute('aria-describedby') ?? '').split(/\s+/).filter(Boolean));
            describedByTokens.add(this.id);
            parentElement.setAttribute('aria-describedby', Array.from(describedByTokens).join(' '));
        }
    }

    dispose() {
        const parentElement = this.parentElement;
        if (!parentElement) return;

        parentElement.removeEventListener('mouseenter', this.handleMouseEnter);
        parentElement.removeEventListener('mouseleave', this.handleMouseLeave);
        parentElement.removeEventListener('mousemove', this.handleMouseMove);
        parentElement.removeEventListener('focusin', this.handleFocusIn);
        parentElement.removeEventListener('focusout', this.handleFocusOut);

        if (this.id) {
            const describedByTokens = (parentElement.getAttribute('aria-describedby') ?? '')
                .split(/\s+/)
                .filter((token) => token && token !== this.id);

            if (describedByTokens.length > 0) {
                parentElement.setAttribute('aria-describedby', describedByTokens.join(' '));
            }
            else {
                parentElement.removeAttribute('aria-describedby');
            }
        }

        this.clearTimeouts();
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

    onMouseEnter() {
        this.showFromAnchor = false;
        this.clearTimeouts();

        const showDelay = parseInt(getComputedStyle(this).getPropertyValue('--tnt-tooltip-show-delay')) || 500;

        this.showTimeoutId = setTimeout(() => {
            this.show(false);
            this.showTimeoutId = null;
        }, showDelay);
    }

    onMouseLeave() {
        this.clearTimeouts();

        const hideDelay = parseInt(getComputedStyle(this).getPropertyValue('--tnt-tooltip-hide-delay')) || 200;

        this.hideTimeoutId = setTimeout(() => {
            this.hide();
            this.hideTimeoutId = null;
        }, hideDelay);
    }

    onMouseMove(e) {
        // Store the latest mouse position
        this.lastMouseX = e.clientX;
        this.lastMouseY = e.clientY;

        if (!this.isVisible) return;
        this.updatePosition(e.clientX, e.clientY);
    }

    onFocusIn(event) {
        const parentElement = this.parentElement;
        if (parentElement?.contains(event.relatedTarget)) {
            return;
        }

        this.showFromAnchor = true;
        this.clearTimeouts();

        const showDelay = parseInt(getComputedStyle(this).getPropertyValue('--tnt-tooltip-show-delay')) || 500;

        this.showTimeoutId = setTimeout(() => {
            this.show(true);
            this.showTimeoutId = null;
        }, showDelay);
    }

    onFocusOut(event) {
        const parentElement = this.parentElement;
        if (parentElement?.contains(event.relatedTarget)) {
            return;
        }

        this.onMouseLeave();
    }

    show(useAnchorPosition = this.showFromAnchor) {
        if (this.isVisible) return;

        this.isVisible = true;
        this.classList.add('tnt-tooltip-visible');
        this.setAttribute('aria-hidden', 'false');
        if (useAnchorPosition) {
            this.updatePositionFromAnchor();
        }
        else {
            // Position tooltip immediately when it shows using the last known mouse position
            this.updatePosition(this.lastMouseX, this.lastMouseY);
        }
    }

    hide() {
        if (!this.isVisible) return;

        this.isVisible = false;
        this.classList.remove('tnt-tooltip-visible');
        this.setAttribute('aria-hidden', 'true');
        // Move off-screen when hidden to prevent hover/click issues
        this.style.left = '-9999px';
        this.style.top = '-9999px';
    }

    updatePositionFromAnchor() {
        const parentElement = this.parentElement;
        if (!parentElement) {
            this.updatePosition(this.lastMouseX, this.lastMouseY);
            return;
        }

        const rect = parentElement.getBoundingClientRect();
        const anchorX = rect.left + (rect.width / 2);
        const anchorY = rect.top;
        this.updatePosition(anchorX, anchorY);
    }

    updatePosition(clientX, clientY) {
        const offset = 10;
        const tooltipRect = this.getBoundingClientRect();
        const tooltipWidth = tooltipRect.width;
        const tooltipHeight = tooltipRect.height;

        // Position tooltip above the cursor
        let left = clientX - (tooltipWidth / 2);
        let top = clientY - tooltipHeight - offset;

        // Constrain to viewport width and height
        const viewportWidth = window.innerWidth;
        const viewportHeight = window.innerHeight;

        // If tooltip would go above the top edge, move it below the cursor instead
        if (top < 0) {
            top = clientY + offset + 16;
        }

        // Ensure the tooltip doesn't go off the left edge
        if (left < 0) {
            left = 0;
        }

        // Ensure the tooltip doesn't go off the right edge
        if (left + tooltipWidth > viewportWidth) {
            left = viewportWidth - tooltipWidth;
        }

        // Ensure the tooltip doesn't go off the bottom edge
        if (top + tooltipHeight > viewportHeight) {
            top = viewportHeight - tooltipHeight;
        }

        this.style.left = `${left}px`;
        this.style.top = `${top}px`;

        // Calculate pointer position and rotation
        this.updatePointer(clientX, clientY, left, top, tooltipWidth, tooltipHeight);
    }

    updatePointer(cursorX, cursorY, tooltipLeft, tooltipTop, tooltipWidth, tooltipHeight) {
        // Calculate horizontal position of pointer relative to cursor
        // Constrain pointer to stay within tooltip bounds
        let pointerX = cursorX - tooltipLeft;

        // Clamp pointer position to stay within tooltip bounds (with small margin)
        const minPointerX = 8; // Minimum distance from left edge
        const maxPointerX = tooltipWidth - 8; // Maximum distance from left edge
        pointerX = Math.max(minPointerX, Math.min(maxPointerX, pointerX));

        const pointerPercentage = (pointerX / tooltipWidth) * 100;

        // Set pointer horizontal position
        this.style.setProperty('--pointer-x', `${pointerPercentage}%`);

        // Determine if pointer should be on top or bottom
        // Check if tooltip is positioned above or below the cursor
        const tooltipCenterY = tooltipTop + tooltipHeight / 2;
        const isTooltipAboveCursor = tooltipCenterY < cursorY;

        // Add/remove class based on pointer position
        if (isTooltipAboveCursor) {
            // Tooltip is below cursor, pointer should point up
            this.classList.remove('tnt-tooltip-pointer-top');
        }
        else {
            // Tooltip is above cursor, pointer should point down
            this.classList.add('tnt-tooltip-pointer-top');
        }
    }
}

export function onLoad(element, dotNetRef) {
    if (!customElements.get('tnt-tooltip')) {
        customElements.define('tnt-tooltip', TnTTooltip);
    }
}

export function onUpdate(element, dotNetRef) {
}

export function onDispose(element, dotNetRef) {
}
