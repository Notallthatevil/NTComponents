type Maybe<T> = T | null | undefined;

interface DotNetSplitButtonRef {
    invokeMethodAsync(methodName: 'NotifySplitButtonExpandedChanged', expanded: boolean): Promise<unknown> | void;
}

interface NTComponentsGlobals {
    registerButtonInteraction?: (element: Maybe<Element>) => void;
}

interface NTMenuElement extends HTMLElement {
    updatePlacement?: () => void;
}

declare global {
    interface Window {
        NTComponents?: NTComponentsGlobals;
    }

    interface HTMLElementTagNameMap {
        'nt-split-button': NTSplitButton;
    }
}

function isDotNetObjectReference(value: unknown): value is DotNetSplitButtonRef {
    return typeof (value as DotNetSplitButtonRef | null)?.invokeMethodAsync === 'function';
}

function isSplitButtonElement(value: unknown): value is NTSplitButton {
    return value instanceof NTSplitButton;
}

export class NTSplitButton extends HTMLElement {
    private button: HTMLButtonElement | null = null;
    private dotNetRef: DotNetSplitButtonRef | null = null;
    private expanded: boolean | null = null;
    private interactionsRegistered = false;
    private items: Element[] = [];
    private leadingButton: HTMLButtonElement | null = null;
    private panel: NTMenuElement | null = null;
    private suppressNextToggleNotification = false;

    private readonly onLeadingClick = (): void => {
        if (this.panel?.matches(':popover-open')) {
            this.panel.hidePopover?.();
        }
    };

    private readonly onToggle = (): void => {
        this.syncOpenState();
    };

    public connectedCallback(): void {
        this.update();
    }

    public disconnectedCallback(): void {
        this.removeElementListeners();
    }

    public removeElementListeners(): void {
        this.panel?.removeEventListener('toggle', this.onToggle);
        this.leadingButton?.removeEventListener('click', this.onLeadingClick);
        this.button = null;
        this.items = [];
        this.leadingButton = null;
        this.panel = null;
    }

    public setExpanded(expanded: Maybe<boolean>): void {
        this.updateElements();

        if (!this.panel || expanded == null) {
            return;
        }

        if (expanded) {
            this.panel.updatePlacement?.();
        }
        const isOpen = this.panel.matches(':popover-open');
        if (expanded === isOpen) {
            this.updateOpenState(isOpen, false);
            return;
        }

        this.suppressNextToggleNotification = true;
        if (expanded) {
            this.panel.showPopover?.();
        } else {
            this.panel.hidePopover?.();
        }

        this.updateOpenState(expanded, false);
    }

    public update(dotNetRef: Maybe<DotNetSplitButtonRef> = null): void {
        this.setDotNetRef(dotNetRef);
        this.updateElements();
        this.setExpanded(this.classList.contains('nt-split-button-expanded'));
    }

    private registerButtonInteractions(): void {
        const registerButtonInteraction = window.NTComponents?.registerButtonInteraction;
        if (!registerButtonInteraction) {
            this.interactionsRegistered = false;
            return;
        }

        registerButtonInteraction(this.leadingButton);
        registerButtonInteraction(this.button);

        for (const item of this.items) {
            registerButtonInteraction(item);
        }

        this.interactionsRegistered = true;
    }

    private setDotNetRef(dotNetRef: Maybe<unknown>): void {
        if (isDotNetObjectReference(dotNetRef)) {
            this.dotNetRef = dotNetRef;
        }
    }

    private syncOpenState(): void {
        this.updateOpenState(this.panel?.matches(':popover-open') ?? false, !this.suppressNextToggleNotification);
        this.suppressNextToggleNotification = false;
    }

    private updateElements(): void {
        const panel = this.querySelector<NTMenuElement>(':scope .nt-split-button-menu-panel');
        const leadingButton = this.querySelector<HTMLButtonElement>(':scope .nt-split-button-leading');
        const button = this.querySelector<HTMLButtonElement>(':scope .nt-split-button-trailing');
        const items = Array.from(this.querySelectorAll<Element>(':scope .nt-menu-item'));
        const itemsChanged = items.length !== this.items.length || items.some((item, index) => item !== this.items[index]);

        if (panel === this.panel && leadingButton === this.leadingButton && button === this.button && !itemsChanged) {
            if (!this.interactionsRegistered) {
                this.registerButtonInteractions();
            }
            return;
        }

        this.removeElementListeners();
        this.panel = panel;
        this.leadingButton = leadingButton;
        this.button = button;
        this.items = items;

        this.panel?.addEventListener('toggle', this.onToggle);
        this.leadingButton?.addEventListener('click', this.onLeadingClick);
        this.registerButtonInteractions();
    }

    private updateOpenState(isOpen: boolean, notifyDotNet: boolean): void {
        if (!this.panel || !this.button) {
            return;
        }

        this.classList.toggle('nt-split-button-expanded', isOpen);
        this.button.setAttribute('aria-expanded', isOpen ? 'true' : 'false');

        if (this.expanded === isOpen) {
            return;
        }

        this.expanded = isOpen;

        if (notifyDotNet && isDotNetObjectReference(this.dotNetRef)) {
            Promise.resolve(this.dotNetRef.invokeMethodAsync('NotifySplitButtonExpandedChanged', isOpen)).catch(() => {
                // Ignore late/disconnected interop failures from native popover notifications.
            });
        }
    }
}

export function onLoad(element: Maybe<HTMLElement>, dotNetRef: Maybe<DotNetSplitButtonRef>): void {
    if (!customElements.get('nt-split-button')) {
        customElements.define('nt-split-button', NTSplitButton);
    }

    if (isSplitButtonElement(element)) {
        element.update(dotNetRef);
    }
}

export function onUpdate(element: Maybe<HTMLElement>, dotNetRef: Maybe<DotNetSplitButtonRef>): void {
    if (isSplitButtonElement(element)) {
        element.update(dotNetRef);
    }
}

export function onDispose(element: Maybe<HTMLElement>): void {
    if (isSplitButtonElement(element)) {
        element.removeElementListeners();
    }
}

export function setExpanded(element: Maybe<HTMLElement>, expanded: Maybe<boolean>): void {
    if (isSplitButtonElement(element)) {
        element.setExpanded(expanded);
    }
}
