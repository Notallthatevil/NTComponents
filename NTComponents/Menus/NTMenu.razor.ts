type Maybe<T> = T | null | undefined;

interface NTComponentsGlobals {
    registerButtonInteraction?: (element: Maybe<Element>) => void;
}

declare global {
    interface Window {
        NTComponents?: NTComponentsGlobals;
    }

    interface HTMLElementTagNameMap {
        'nt-menu': NTMenu;
    }
}

function isMenuElement(value: unknown): value is NTMenu {
    return value instanceof NTMenu;
}

export class NTMenu extends HTMLElement {
    private anchor: Element | null = null;
    private interactionsRegistered = false;
    private items: HTMLElement[] = [];

    private readonly onAnchorActivate = (): void => {
        this.updatePlacement();
    };

    private readonly onBeforeToggle = (event: Event): void => {
        if ((event as Event & { newState?: string }).newState === 'open') {
            this.updatePlacement();
        }
    };

    private readonly onKeyDown = (event: KeyboardEvent): void => {
        this.handleKeyDown(event);
    };

    private readonly onMenuClick = (event: MouseEvent): void => {
        this.handleMenuClick(event);
    };

    private readonly onToggle = (): void => {
        this.syncOpenState();
    };

    public connectedCallback(): void {
        this.update();
        this.addEventListener('beforetoggle', this.onBeforeToggle);
        this.addEventListener('click', this.onMenuClick);
        this.addEventListener('keydown', this.onKeyDown);
        this.addEventListener('toggle', this.onToggle);
    }

    public disconnectedCallback(): void {
        this.dispose();
    }

    public dispose(): void {
        this.removeEventListener('beforetoggle', this.onBeforeToggle);
        this.removeEventListener('click', this.onMenuClick);
        this.removeEventListener('keydown', this.onKeyDown);
        this.removeEventListener('toggle', this.onToggle);
        this.removeAnchorListeners();
        this.interactionsRegistered = false;
        this.items = [];
    }

    public update(): void {
        const anchor = this.findAnchor();
        const items = this.getItems();
        const itemsChanged = items.length !== this.items.length || items.some((item, index) => item !== this.items[index]);

        if (anchor === this.anchor && !itemsChanged) {
            if (!this.interactionsRegistered) {
                this.registerButtonInteractions();
            }
            return;
        }

        if (anchor !== this.anchor) {
            this.removeAnchorListeners();
            this.anchor = anchor;
            this.anchor?.addEventListener('click', this.onAnchorActivate);
            this.anchor?.addEventListener('keydown', this.onAnchorActivate);
            this.anchor?.addEventListener('pointerdown', this.onAnchorActivate);
        }

        if (itemsChanged) {
            this.items = items;
            this.interactionsRegistered = false;
        }

        if (!this.interactionsRegistered) {
            this.registerButtonInteractions();
        }
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
        const previousTransform = this.style.transform;
        const previousVisibility = this.style.visibility;
        const previousClipPath = this.style.clipPath;

        this.style.clipPath = 'none';
        this.style.display = 'block';
        this.style.inset = 'auto';
        this.style.pointerEvents = 'none';
        this.style.position = 'fixed';
        this.style.transform = 'none';
        this.style.visibility = 'hidden';

        const rect = this.getBoundingClientRect();
        const width = rect.width || this.scrollWidth;
        const height = rect.height || this.scrollHeight;

        this.style.display = previousDisplay;
        this.style.inset = previousInset;
        this.style.pointerEvents = previousPointerEvents;
        this.style.position = previousPosition;
        this.style.transform = previousTransform;
        this.style.visibility = previousVisibility;
        this.style.clipPath = previousClipPath;

        return { height, width };
    }

    private focusItem(offset: number): void {
        const items = this.getItems();
        if (items.length === 0) {
            return;
        }

        const activeIndex = items.indexOf(document.activeElement as HTMLElement);
        const startIndex = activeIndex >= 0 ? activeIndex : (offset > 0 ? -1 : 0);
        const nextIndex = (startIndex + offset + items.length) % items.length;
        items[nextIndex]?.focus();
    }

    private focusFirstItem(): void {
        this.getItems()[0]?.focus();
    }

    private focusMatchingItem(key: string): void {
        const search = key.toLocaleLowerCase();
        const items = this.getItems();
        const activeIndex = items.indexOf(document.activeElement as HTMLElement);

        for (let offset = 1; offset <= items.length; offset++) {
            const item = items[(Math.max(activeIndex, 0) + offset) % items.length];
            if (item?.textContent?.trim().toLocaleLowerCase().startsWith(search)) {
                item.focus();
                return;
            }
        }
    }

    private getItems(): HTMLElement[] {
        return Array.from(this.querySelectorAll<HTMLElement>(':scope > .nt-menu-surface > .nt-menu-content > .nt-menu-item'));
    }

    private handleKeyDown(event: KeyboardEvent): void {
        if (!this.matches(':popover-open')) {
            return;
        }

        switch (event.key) {
            case 'ArrowDown':
                event.preventDefault();
                this.focusItem(1);
                break;
            case 'ArrowUp':
                event.preventDefault();
                this.focusItem(-1);
                break;
            case 'Escape':
                event.preventDefault();
                this.hidePopover?.();
                this.getAnchorElement()?.focus?.();
                break;
            case 'ArrowRight':
                this.openFocusedSubMenu(event);
                break;
            case 'ArrowLeft':
                this.closeFromSubMenu(event);
                break;
            default:
                if (event.key.length === 1 && !event.altKey && !event.ctrlKey && !event.metaKey) {
                    this.focusMatchingItem(event.key);
                }
                break;
        }
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

    private registerButtonInteractions(): void {
        const registerButtonInteraction = window.NTComponents?.registerButtonInteraction;
        if (!registerButtonInteraction) {
            this.interactionsRegistered = false;
            return;
        }

        for (const item of this.items) {
            registerButtonInteraction(item);
        }

        this.interactionsRegistered = true;
    }

    private openFocusedSubMenu(event: KeyboardEvent): void {
        const item = document.activeElement instanceof HTMLElement ? document.activeElement : null;
        if (!item || item.closest('nt-menu') !== this || item.getAttribute('aria-haspopup') !== 'menu') {
            return;
        }

        event.preventDefault();
        const submenuId = item.getAttribute('aria-controls');
        const submenu = submenuId ? document.getElementById(submenuId) as NTMenu | null : null;
        submenu?.showPopover?.();
    }

    private closeFromSubMenu(event: KeyboardEvent): void {
        if (this.dataset.submenu !== 'true') {
            return;
        }

        event.preventDefault();
        this.hidePopover?.();
        this.getAnchorElement()?.focus?.();
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

    private getAnchorElement(): HTMLElement | null {
        const anchor = this.anchor ?? this.findAnchor();
        return anchor instanceof HTMLElement ? anchor : null;
    }

    private setAnchorPressed(isOpen: boolean): void {
        const anchor = this.getAnchorElement();
        if (!anchor) {
            return;
        }

        anchor.classList.toggle('nt-menu-anchor-pressed', isOpen);
        anchor.classList.toggle('nt-button--pressed-shape', isOpen);
        anchor.setAttribute('aria-expanded', isOpen ? 'true' : 'false');
    }

    private syncOpenState(): void {
        const isOpen = this.matches(':popover-open');
        this.setAnchorPressed(isOpen);

        if (isOpen) {
            const focus = () => this.focusFirstItem();
            if (typeof window.requestAnimationFrame === 'function') {
                window.requestAnimationFrame(focus);
            } else {
                window.setTimeout(focus, 0);
            }
        }
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
