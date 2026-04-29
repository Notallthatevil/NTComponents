function isDotNetObjectReference(value) {
    return typeof value?.invokeMethodAsync === 'function';
}

export class NTSplitButton extends HTMLElement {
    constructor() {
        super();
        this.button = null;
        this.dotNetRef = null;
        this.leadingButton = null;
        this.panel = null;
        this.onLeadingClick = () => this.setExpanded(false);
        this.onPanelClick = (event) => this.handlePanelClick(event);
        this.onToggle = () => this.syncOpenState();
    }

    connectedCallback() {
        this.update();
    }

    disconnectedCallback() {
        this.removeElementListeners();
    }

    handlePanelClick(event) {
        if (this.panel?.dataset.closeOnItemClick !== 'true') {
            return;
        }

        const item = event.target?.closest?.('.nt-split-button-menu-item');
        if (!item || item.classList.contains('nt-split-button-menu-item-disabled')) {
            return;
        }

        this.panel.hidePopover?.();
    }

    removeElementListeners() {
        this.panel?.removeEventListener('toggle', this.onToggle);
        this.panel?.removeEventListener('click', this.onPanelClick);
        this.leadingButton?.removeEventListener('click', this.onLeadingClick);
        this.button = null;
        this.leadingButton = null;
        this.panel = null;
    }

    setDotNetRef(dotNetRef) {
        if (isDotNetObjectReference(dotNetRef)) {
            this.dotNetRef = dotNetRef;
        }
    }

    setExpanded(expanded) {
        this.updateElements();

        if (!this.panel || expanded == null) {
            return;
        }

        const isOpen = this.panel.matches(':popover-open');
        if (expanded && !isOpen) {
            this.panel.showPopover?.();
        } else if (!expanded && isOpen) {
            this.panel.hidePopover?.();
        }

        this.syncOpenState();
    }

    syncOpenState() {
        if (!this.panel || !this.button) {
            return;
        }

        const isOpen = this.panel.matches(':popover-open');
        this.classList.toggle('nt-split-button-expanded', isOpen);
        this.button.setAttribute('aria-expanded', isOpen ? 'true' : 'false');

        if (isDotNetObjectReference(this.dotNetRef)) {
            this.dotNetRef.invokeMethodAsync('NotifySplitButtonExpandedChanged', isOpen).catch(() => { });
        }
    }

    update(dotNetRef = null) {
        this.setDotNetRef(dotNetRef);
        this.updateElements();
        this.setExpanded(this.classList.contains('nt-split-button-expanded'));
    }

    updateElements() {
        const panel = this.querySelector(':scope .nt-split-button-menu-panel');
        const leadingButton = this.querySelector(':scope .nt-split-button-leading');
        const button = this.querySelector(':scope .nt-split-button-trailing');

        if (panel === this.panel && leadingButton === this.leadingButton && button === this.button) {
            this.registerButtonInteractions();
            return;
        }

        this.removeElementListeners();
        this.panel = panel;
        this.leadingButton = leadingButton;
        this.button = button;

        this.panel?.addEventListener('toggle', this.onToggle);
        this.panel?.addEventListener('click', this.onPanelClick);
        this.leadingButton?.addEventListener('click', this.onLeadingClick);
        this.registerButtonInteractions();
    }

    registerButtonInteractions() {
        window.NTComponents?.registerButtonInteraction?.(this.leadingButton);
        window.NTComponents?.registerButtonInteraction?.(this.button);
    }
}

export function onLoad(element, dotNetRef) {
    if (!customElements.get('nt-split-button')) {
        customElements.define('nt-split-button', NTSplitButton);
    }

    element?.update?.(dotNetRef);
}

export function onUpdate(element, dotNetRef) {
    element?.update?.(dotNetRef);
}

export function onDispose(element) {
    element?.removeElementListeners?.();
}

export function setExpanded(element, expanded) {
    element?.setExpanded?.(expanded);
}
