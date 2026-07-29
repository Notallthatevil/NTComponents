const tooltipsByElement = new WeakMap();

class TnTTooltip extends HTMLElement {
    constructor() {
        super();
        this.showTimeoutId = null;
        this.hideTimeoutId = null;
        this.isVisible = false;
        this.lastMouseX = 0;
        this.lastMouseY = 0;

        this.handleMouseEnter = () => this.onMouseEnter();
        this.handleMouseLeave = () => this.onMouseLeave();
        this.handleMouseMove = (e) => this.onMouseMove(e);
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
    }

    dispose() {
        const parentElement = this.parentElement;
        if (!parentElement) return;

        parentElement.removeEventListener('mouseenter', this.handleMouseEnter);
        parentElement.removeEventListener('mouseleave', this.handleMouseLeave);
        parentElement.removeEventListener('mousemove', this.handleMouseMove);

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
        this.clearTimeouts();

        const showDelay = parseInt(getComputedStyle(this).getPropertyValue('--tnt-tooltip-show-delay')) || 500;

        this.showTimeoutId = setTimeout(() => {
            this.show();
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

    show() {
        if (this.isVisible) return;

        this.isVisible = true;
        this.classList.add('tnt-tooltip-visible');
        // Position tooltip immediately when it shows using the last known mouse position
        this.updatePosition(this.lastMouseX, this.lastMouseY);
    }

    hide() {
        if (!this.isVisible) return;

        this.isVisible = false;
        this.classList.remove('tnt-tooltip-visible');
        // Move off-screen when hidden to prevent hover/click issues
        this.style.left = '-9999px';
        this.style.top = '-9999px';
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
        } else {
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
