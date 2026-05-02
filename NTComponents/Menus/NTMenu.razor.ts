type Maybe<T> = T | null | undefined;

declare global {
    interface HTMLElementTagNameMap {
        'nt-menu': NTMenu;
    }
}

function isMenuElement(value: unknown): value is NTMenu {
    return value instanceof NTMenu;
}

export class NTMenu extends HTMLElement {
    private anchor: Element | null = null;

    private readonly onAnchorActivate = (): void => {
        this.updatePlacement();
    };

    private readonly onMenuClick = (event: MouseEvent): void => {
        this.handleMenuClick(event);
    };

    public connectedCallback(): void {
        this.update();
        this.addEventListener('click', this.onMenuClick);
    }

    public disconnectedCallback(): void {
        this.dispose();
    }

    public dispose(): void {
        this.removeEventListener('click', this.onMenuClick);
        this.removeAnchorListeners();
    }

    public update(): void {
        const anchor = this.findAnchor();
        if (anchor === this.anchor) {
            return;
        }

        this.removeAnchorListeners();
        this.anchor = anchor;
        this.anchor?.addEventListener('click', this.onAnchorActivate);
        this.anchor?.addEventListener('keydown', this.onAnchorActivate);
        this.anchor?.addEventListener('pointerdown', this.onAnchorActivate);
    }

    public updatePlacement(): void {
        if (!this.classList.contains('nt-menu-placement-auto')) {
            return;
        }

        const anchor = this.anchor ?? this.findAnchor();
        if (!anchor) {
            return;
        }

        const anchorRect = anchor.getBoundingClientRect();
        const menuSize = this.measureMenu();
        const viewportWidth = window.innerWidth || document.documentElement.clientWidth;
        const viewportHeight = window.innerHeight || document.documentElement.clientHeight;
        const edgeMargin = 8;
        const gap = 4;
        if (this.dataset.submenu === 'true') {
            this.updateSubMenuPlacement(anchorRect, menuSize, viewportWidth, viewportHeight, edgeMargin, gap);
            return;
        }

        const spaceBelow = viewportHeight - anchorRect.bottom - gap - edgeMargin;
        const spaceAbove = anchorRect.top - gap - edgeMargin;
        const openTop = spaceBelow < menuSize.height && spaceAbove > spaceBelow;
        const spaceStart = viewportWidth - anchorRect.left - edgeMargin;
        const spaceEnd = anchorRect.right - edgeMargin;
        const alignStart = spaceStart >= menuSize.width || spaceStart > spaceEnd;
        const left = alignStart ? anchorRect.left : anchorRect.right - menuSize.width;
        const top = openTop ? anchorRect.top - gap - menuSize.height : anchorRect.bottom + gap;

        this.classList.toggle('nt-menu-placement-top', openTop);
        this.classList.toggle('nt-menu-placement-bottom', !openTop);
        this.classList.toggle('nt-menu-anchor-start', alignStart);
        this.classList.toggle('nt-menu-anchor-end', !alignStart);
        this.classList.remove('nt-menu-placement-side-left', 'nt-menu-placement-side-right');
        this.setPosition(top, left, menuSize, viewportWidth, viewportHeight, edgeMargin);
    }

    private findAnchor(): Element | null {
        const selector = this.dataset.anchorSelector?.trim();
        if (!selector) {
            return null;
        }

        try {
            return document.querySelector(selector);
        } catch {
            return null;
        }
    }

    private measureMenu(): { height: number; width: number } {
        const wasOpen = this.matches(':popover-open');
        if (wasOpen) {
            const rect = this.getBoundingClientRect();
            return { height: rect.height, width: rect.width };
        }

        const previousDisplay = this.style.display;
        const previousInset = this.style.inset;
        const previousPointerEvents = this.style.pointerEvents;
        const previousPosition = this.style.position;
        const previousVisibility = this.style.visibility;

        this.style.display = 'block';
        this.style.inset = 'auto';
        this.style.pointerEvents = 'none';
        this.style.position = 'fixed';
        this.style.visibility = 'hidden';

        const rect = this.getBoundingClientRect();
        const width = rect.width || this.scrollWidth;
        const height = rect.height || this.scrollHeight;

        this.style.display = previousDisplay;
        this.style.inset = previousInset;
        this.style.pointerEvents = previousPointerEvents;
        this.style.position = previousPosition;
        this.style.visibility = previousVisibility;

        return { height, width };
    }

    private handleMenuClick(event: MouseEvent): void {
        if (this.dataset.closeOnItemClick !== 'true') {
            return;
        }

        const target = event.target instanceof Element ? event.target : null;
        const item = target?.closest('.nt-menu-item');
        if (!item || item.closest('nt-menu') !== this) {
            return;
        }

        if (item.classList.contains('nt-menu-item-disabled') || item.hasAttribute('disabled') || item.getAttribute('aria-disabled') === 'true') {
            return;
        }

        if (item.getAttribute('aria-haspopup') === 'menu' || item.hasAttribute('data-nt-menu-submenu-trigger')) {
            return;
        }

        this.hidePopover?.();
    }

    private removeAnchorListeners(): void {
        this.anchor?.removeEventListener('click', this.onAnchorActivate);
        this.anchor?.removeEventListener('keydown', this.onAnchorActivate);
        this.anchor?.removeEventListener('pointerdown', this.onAnchorActivate);
        this.anchor = null;
    }

    private setPosition(top: number, left: number, menuSize: { height: number; width: number }, viewportWidth: number, viewportHeight: number, edgeMargin: number): void {
        const maxLeft = Math.max(edgeMargin, viewportWidth - menuSize.width - edgeMargin);
        const maxTop = Math.max(edgeMargin, viewportHeight - menuSize.height - edgeMargin);
        const clampedLeft = Math.min(Math.max(left, edgeMargin), maxLeft);
        const clampedTop = Math.min(Math.max(top, edgeMargin), maxTop);

        this.classList.add('nt-menu-js-positioned');
        this.style.bottom = 'auto';
        this.style.left = `${clampedLeft}px`;
        this.style.right = 'auto';
        this.style.top = `${clampedTop}px`;
    }

    private updateSubMenuPlacement(anchorRect: DOMRect, menuSize: { height: number; width: number }, viewportWidth: number, viewportHeight: number, edgeMargin: number, gap: number): void {
        const spaceRight = viewportWidth - anchorRect.right - gap - edgeMargin;
        const spaceLeft = anchorRect.left - gap - edgeMargin;
        const openLeft = spaceRight < menuSize.width && spaceLeft > spaceRight;
        const left = openLeft ? anchorRect.left - gap - menuSize.width : anchorRect.right + gap;

        this.classList.remove('nt-menu-placement-top', 'nt-menu-placement-bottom');
        this.classList.toggle('nt-menu-placement-side-left', openLeft);
        this.classList.toggle('nt-menu-placement-side-right', !openLeft);
        this.classList.toggle('nt-menu-anchor-start', !openLeft);
        this.classList.toggle('nt-menu-anchor-end', openLeft);
        this.setPosition(anchorRect.top, left, menuSize, viewportWidth, viewportHeight, edgeMargin);
    }
}

export function onLoad(element: Maybe<HTMLElement>): void {
    if (!customElements.get('nt-menu')) {
        customElements.define('nt-menu', NTMenu);
    }

    if (isMenuElement(element)) {
        element.update();
    }
}

export function onUpdate(element: Maybe<HTMLElement>): void {
    if (isMenuElement(element)) {
        element.update();
    }
}

export function onDispose(element: Maybe<HTMLElement>): void {
    if (isMenuElement(element)) {
        element.dispose();
    }
}
