type Maybe<T> = T | null | undefined;

interface PointMenuElement extends HTMLElement {
    openAt?: (clientX: number, clientY: number, sourceElement?: Element | null) => void;
}

declare global {
    interface HTMLElementTagNameMap {
        'nt-context-menu': NTContextMenu;
    }
}

function isContextMenuElement(value: unknown): value is NTContextMenu {
    return value instanceof NTContextMenu;
}

export class NTContextMenu extends HTMLElement {
    private longPressStartX = 0;
    private longPressStartY = 0;
    private longPressTimer: number | null = null;
    private menu: PointMenuElement | null = null;
    private suppressNativeUntil = 0;
    private target: HTMLElement | null = null;

    private readonly onClick = (event: MouseEvent): void => {
        if (this.shouldSuppressNativeEvent()) {
            event.preventDefault();
            event.stopPropagation();
        }
    };

    private readonly onContextMenu = (event: MouseEvent): void => {
        this.handleContextMenu(event);
    };

    private readonly onKeyDown = (event: KeyboardEvent): void => {
        this.handleKeyDown(event);
    };

    private readonly onPointerCancel = (): void => {
        this.cancelLongPress();
    };

    private readonly onPointerDown = (event: PointerEvent): void => {
        this.handlePointerDown(event);
    };

    private readonly onPointerMove = (event: PointerEvent): void => {
        this.handlePointerMove(event);
    };

    private readonly onPointerUp = (): void => {
        this.cancelLongPress();
    };

    private readonly onScroll = (): void => {
        this.cancelLongPress();
    };

    public connectedCallback(): void {
        this.update();
    }

    public disconnectedCallback(): void {
        this.dispose();
    }

    public dispose(): void {
        this.cancelLongPress();
        this.removeTargetListeners();
        this.removeTransientListeners();
        this.menu = null;
    }

    public update(): void {
        const target = this.querySelector<HTMLElement>(':scope > .nt-context-menu-target');
        const menu = this.getMenu();

        if (target !== this.target) {
            this.removeTargetListeners();
            this.target = target;
            this.addTargetListeners();
        }

        this.menu = menu;
    }

    private addTargetListeners(): void {
        this.target?.addEventListener('click', this.onClick);
        this.target?.addEventListener('contextmenu', this.onContextMenu);
        this.target?.addEventListener('keydown', this.onKeyDown);
        this.target?.addEventListener('pointerdown', this.onPointerDown);
    }

    private cancelLongPress(): void {
        if (this.longPressTimer !== null) {
            window.clearTimeout(this.longPressTimer);
            this.longPressTimer = null;
        }

        this.removeTransientListeners();
    }

    private getLongPressDelay(): number {
        const delay = Number.parseInt(this.dataset.longPressDelay ?? '', 10);
        return Number.isFinite(delay) && delay >= 0 ? delay : 500;
    }

    private getMenu(): PointMenuElement | null {
        const menuId = this.dataset.menuId?.trim();
        if (menuId) {
            const menu = document.getElementById(menuId);
            if (menu instanceof HTMLElement) {
                return menu as PointMenuElement;
            }
        }

        return this.querySelector<PointMenuElement>(':scope > nt-menu');
    }

    private handleContextMenu(event: MouseEvent): void {
        if (this.shouldSuppressNativeEvent()) {
            event.preventDefault();
            event.stopPropagation();
            return;
        }

        if (this.isDisabled()) {
            return;
        }

        event.preventDefault();
        event.stopPropagation();
        this.openAt(event.clientX, event.clientY, event.target instanceof Element ? event.target : this.target);
    }

    private handleKeyDown(event: KeyboardEvent): void {
        if (this.isDisabled()) {
            return;
        }

        const opensContextMenu = event.key === 'ContextMenu' || (event.shiftKey && event.key === 'F10');
        if (!opensContextMenu) {
            return;
        }

        const source = event.target instanceof Element ? event.target : this.target;
        const rect = (source instanceof HTMLElement ? source : this.target)?.getBoundingClientRect();
        if (!rect) {
            return;
        }

        event.preventDefault();
        event.stopPropagation();
        this.openAt(rect.left, rect.bottom, source);
    }

    private handlePointerDown(event: PointerEvent): void {
        if (this.isDisabled() || !event.isPrimary || (event.pointerType !== 'touch' && event.pointerType !== 'pen')) {
            return;
        }

        this.cancelLongPress();
        event.stopPropagation();
        this.longPressStartX = event.clientX;
        this.longPressStartY = event.clientY;
        this.longPressTimer = window.setTimeout(() => {
            this.longPressTimer = null;
            this.suppressFollowUpNativeEvents();
            this.openAt(this.longPressStartX, this.longPressStartY, event.target instanceof Element ? event.target : this.target);
            this.removeTransientListeners();
        }, this.getLongPressDelay());

        document.addEventListener('pointermove', this.onPointerMove);
        document.addEventListener('pointerup', this.onPointerUp);
        document.addEventListener('pointercancel', this.onPointerCancel);
        window.addEventListener('scroll', this.onScroll, true);
    }

    private handlePointerMove(event: PointerEvent): void {
        const movement = Math.hypot(event.clientX - this.longPressStartX, event.clientY - this.longPressStartY);
        if (movement > 8) {
            this.cancelLongPress();
        }
    }

    private isDisabled(): boolean {
        return this.dataset.disabled === 'true' || this.hasAttribute('disabled') || this.classList.contains('tnt-disabled');
    }

    private openAt(clientX: number, clientY: number, sourceElement: Maybe<Element>): void {
        const menu = this.menu ?? this.getMenu();
        menu?.openAt?.(clientX, clientY, sourceElement ?? this.target);
    }

    private removeTargetListeners(): void {
        this.target?.removeEventListener('click', this.onClick);
        this.target?.removeEventListener('contextmenu', this.onContextMenu);
        this.target?.removeEventListener('keydown', this.onKeyDown);
        this.target?.removeEventListener('pointerdown', this.onPointerDown);
        this.target = null;
    }

    private removeTransientListeners(): void {
        document.removeEventListener('pointermove', this.onPointerMove);
        document.removeEventListener('pointerup', this.onPointerUp);
        document.removeEventListener('pointercancel', this.onPointerCancel);
        window.removeEventListener('scroll', this.onScroll, true);
    }

    private shouldSuppressNativeEvent(): boolean {
        return Date.now() < this.suppressNativeUntil;
    }

    private suppressFollowUpNativeEvents(): void {
        this.suppressNativeUntil = Date.now() + 750;
    }
}

export function onLoad(element: Maybe<HTMLElement>): void {
    if (!customElements.get('nt-context-menu')) {
        customElements.define('nt-context-menu', NTContextMenu);
    }

    if (isContextMenuElement(element)) {
        element.update();
    }
}

export function onUpdate(element: Maybe<HTMLElement>): void {
    if (isContextMenuElement(element)) {
        element.update();
    }
}

export function onDispose(element: Maybe<HTMLElement>): void {
    if (isContextMenuElement(element)) {
        element.dispose();
    }
}
