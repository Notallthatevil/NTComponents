type Maybe<T> = T | null | undefined;
export type SnackbarActionCallback = () => void | Promise<void>;
export type SnackbarInput = string | SnackbarOptions;

interface NTSnackbarHostElement extends HTMLElement {
    __ntSnackbarState?: NTSnackbarHostState;
}

interface NTSnackbarHostState {
    activeElement: HTMLElement | null;
    activeSnackbar: SnackbarRecord | null;
    closeTimeout: number | null;
    queue: SnackbarRecord[];
}

interface SnackbarRecord {
    actionCallback: SnackbarActionCallback | null;
    actionLabel: string | null;
    actionColor: string | null;
    backgroundColor: string | null;
    dotNetActionMethod: string | null;
    dotNetCloseMethod: string | null;
    dotNetReference: DotNetSnackbarReference | null;
    id: string;
    message: string;
    showClose: boolean;
    textColor: string | null;
    timeout: number;
}

export interface SnackbarOptions {
    actionCallback?: SnackbarActionCallback | null;
    actionLabel?: string | null;
    actionColor?: string | null;
    backgroundColor?: string | null;
    dotNetActionMethod?: string | null;
    dotNetCloseMethod?: string | null;
    dotNetReference?: DotNetSnackbarReference | null;
    host?: HTMLElement | string | null;
    id?: string | null;
    message: string;
    showClose?: boolean | null;
    textColor?: string | null;
    timeout?: number | null;
}

export interface NTSnackbarBridge {
    addSnackbar(messageOrOptions: SnackbarInput, options?: Partial<SnackbarOptions>): string;
    clearSnackbars(host?: HTMLElement | string | null): void;
    closeSnackbar(id?: string, host?: HTMLElement | string | null): boolean;
    closeSnackbarFromBlazor(id: string, host?: HTMLElement | string | null): boolean;
    queueSnackbar(messageOrOptions: SnackbarInput, options?: Partial<SnackbarOptions>): string;
}

export interface DotNetSnackbarReference {
    invokeMethodAsync<T>(methodName: string, ...args: unknown[]): Promise<T>;
}

declare global {
    interface Window {
        NTSnackbar?: NTSnackbarBridge;
    }
}

const snackbarHostSelector = '[data-nt-snackbar-host="true"]';
const closeDelayMilliseconds = 200;
const defaultActionColor = 'var(--tnt-color-inverse-primary)';
const defaultBackgroundColor = 'var(--tnt-color-inverse-surface)';
const defaultTextColor = 'var(--tnt-color-inverse-on-surface)';
const pendingSnackbars: SnackbarRecord[] = [];
const hostStates = new WeakMap<NTSnackbarHostElement, NTSnackbarHostState>();
let defaultHost: NTSnackbarHostElement | null = null;
let nextSnackbarId = 0;

function getHosts(root: Maybe<Element | Document>): NTSnackbarHostElement[] {
    const scope = root ?? document;
    if (scope instanceof HTMLElement && scope.localName === 'tnt-page-script') {
        const sibling = scope.previousElementSibling;
        return sibling instanceof HTMLElement && sibling.matches(snackbarHostSelector) ? [sibling as NTSnackbarHostElement] : [];
    }

    const hosts = scope instanceof HTMLElement && scope.matches(snackbarHostSelector) ? [scope as NTSnackbarHostElement] : [];
    hosts.push(...Array.from(scope.querySelectorAll<NTSnackbarHostElement>(snackbarHostSelector)));
    return hosts;
}

function getHost(host?: HTMLElement | string | null): NTSnackbarHostElement | null {
    if (host instanceof HTMLElement) {
        return host.matches(snackbarHostSelector) ? host as NTSnackbarHostElement : null;
    }

    if (typeof host === 'string' && host.length > 0) {
        const element = document.getElementById(host);
        return element instanceof HTMLElement && element.matches(snackbarHostSelector) ? element as NTSnackbarHostElement : null;
    }

    if (defaultHost?.isConnected) {
        return defaultHost;
    }

    defaultHost = document.querySelector<NTSnackbarHostElement>(snackbarHostSelector);
    return defaultHost;
}

function getOrCreateState(host: NTSnackbarHostElement): NTSnackbarHostState {
    const existingState = hostStates.get(host);
    if (existingState) {
        return existingState;
    }

    const state: NTSnackbarHostState = {
        activeElement: null,
        activeSnackbar: null,
        closeTimeout: null,
        queue: []
    };

    host.__ntSnackbarState = state;
    hostStates.set(host, state);
    return state;
}

function normalizeSnackbar(messageOrOptions: SnackbarInput, options?: Partial<SnackbarOptions>): SnackbarRecord {
    const source = typeof messageOrOptions === 'string'
        ? { ...options, message: messageOrOptions }
        : { ...messageOrOptions, ...options };

    const actionLabel = source.actionLabel?.trim() || null;
    const hasAction = actionLabel !== null;
    const timeout = typeof source.timeout === 'number' ? source.timeout : hasAction ? 0 : 6;

    return {
        actionCallback: typeof source.actionCallback === 'function' ? source.actionCallback : null,
        actionColor: source.actionColor ?? defaultActionColor,
        actionLabel,
        backgroundColor: source.backgroundColor ?? defaultBackgroundColor,
        dotNetActionMethod: source.dotNetActionMethod ?? null,
        dotNetCloseMethod: source.dotNetCloseMethod ?? null,
        dotNetReference: source.dotNetReference ?? null,
        id: source.id?.trim() || `nt-snackbar-js-${++nextSnackbarId}`,
        message: source.message,
        showClose: source.showClose ?? hasAction,
        textColor: source.textColor ?? defaultTextColor,
        timeout
    };
}

function setStyleVariables(element: HTMLElement, snackbar: SnackbarRecord): void {
    if (snackbar.backgroundColor) {
        element.style.setProperty('--nt-snackbar-background-color', snackbar.backgroundColor);
    }

    if (snackbar.textColor) {
        element.style.setProperty('--nt-snackbar-text-color', snackbar.textColor);
    }

    if (snackbar.actionColor) {
        element.style.setProperty('--nt-snackbar-action-color', snackbar.actionColor);
    }
}

function createCloseIcon(): HTMLElement {
    const icon = document.createElement('span');
    icon.className = 'tnt-icon material-symbols-outlined mi-medium';
    icon.textContent = 'close';
    return icon;
}

function invokeSnackbarAction(snackbar: SnackbarRecord): Promise<void> {
    if (snackbar.actionCallback) {
        return Promise.resolve(snackbar.actionCallback());
    }

    if (snackbar.dotNetReference && snackbar.dotNetActionMethod) {
        return snackbar.dotNetReference.invokeMethodAsync<void>(snackbar.dotNetActionMethod, snackbar.id);
    }

    return Promise.resolve();
}

function notifySnackbarClosed(snackbar: SnackbarRecord): void {
    if (!snackbar.dotNetReference || !snackbar.dotNetCloseMethod) {
        return;
    }

    void snackbar.dotNetReference.invokeMethodAsync<void>(snackbar.dotNetCloseMethod, snackbar.id);
}

function createSnackbarElement(host: NTSnackbarHostElement, state: NTSnackbarHostState, snackbar: SnackbarRecord): HTMLElement {
    const stackItem = document.createElement('div');
    stackItem.className = 'nt-snackbar-stack-item nt-snackbar-front nt-snackbar-stack-depth-0';
    stackItem.style.setProperty('--nt-snackbar-stack-depth', '0');

    const snackbarElement = document.createElement('div');
    snackbarElement.id = snackbar.id;
    snackbarElement.className = 'nt-snackbar nt-elevation-medium';
    snackbarElement.setAttribute('role', 'status');
    snackbarElement.setAttribute('aria-live', 'polite');
    snackbarElement.setAttribute('aria-atomic', 'true');
    setStyleVariables(snackbarElement, snackbar);

    const message = document.createElement('div');
    message.className = 'nt-snackbar-message';
    message.textContent = snackbar.message;
    snackbarElement.appendChild(message);

    if (snackbar.actionLabel || snackbar.showClose) {
        const actions = document.createElement('div');
        actions.className = 'nt-snackbar-actions';

        if (snackbar.actionLabel) {
            const action = document.createElement('button');
            action.className = 'nt-snackbar-action tnt-interactable';
            action.type = 'button';
            action.textContent = snackbar.actionLabel;
            action.addEventListener('click', () => {
                void invokeSnackbarAction(snackbar)
                    .then(() => closeActiveSnackbar(host, state))
                    .catch(() => { });
            });
            actions.appendChild(action);
        }

        if (snackbar.showClose) {
            const close = document.createElement('button');
            close.className = 'nt-snackbar-close tnt-interactable';
            close.type = 'button';
            close.setAttribute('aria-label', 'Dismiss notification');
            close.appendChild(createCloseIcon());
            close.addEventListener('click', () => closeActiveSnackbar(host, state));
            actions.appendChild(close);
        }

        snackbarElement.appendChild(actions);
    }

    stackItem.appendChild(snackbarElement);
    return stackItem;
}

function tryShowNext(host: NTSnackbarHostElement): void {
    const state = getOrCreateState(host);
    if (state.activeElement || state.queue.length === 0) {
        return;
    }

    const snackbar = state.queue.shift();
    if (!snackbar) {
        return;
    }

    const element = createSnackbarElement(host, state, snackbar);
    state.activeSnackbar = snackbar;
    state.activeElement = element;
    host.appendChild(element);

    if (snackbar.timeout > 0) {
        state.closeTimeout = window.setTimeout(() => closeActiveSnackbar(host, state), snackbar.timeout * 1000);
    }
}

function closeActiveSnackbar(host: NTSnackbarHostElement, state: NTSnackbarHostState, notifyDotNet = true): boolean {
    const activeElement = state.activeElement;
    const activeSnackbar = state.activeSnackbar;
    if (!activeElement) {
        return false;
    }

    if (state.closeTimeout !== null) {
        window.clearTimeout(state.closeTimeout);
        state.closeTimeout = null;
    }

    activeElement.querySelector('.nt-snackbar')?.classList.add('nt-closing');
    state.activeElement = null;
    state.activeSnackbar = null;

    window.setTimeout(() => {
        activeElement.remove();
        if (notifyDotNet && activeSnackbar) {
            notifySnackbarClosed(activeSnackbar);
        }
        tryShowNext(host);
    }, closeDelayMilliseconds);

    return true;
}

function flushPendingSnackbars(): void {
    const host = getHost();
    if (!host || pendingSnackbars.length === 0) {
        return;
    }

    const state = getOrCreateState(host);
    state.queue.push(...pendingSnackbars.splice(0));
    tryShowNext(host);
}

function initializeHost(host: NTSnackbarHostElement): void {
    getOrCreateState(host);
    defaultHost = host;
    flushPendingSnackbars();
}

function disposeHost(host: NTSnackbarHostElement): void {
    const state = hostStates.get(host);
    if (!state) {
        return;
    }

    if (state.closeTimeout !== null) {
        window.clearTimeout(state.closeTimeout);
    }

    state.queue.length = 0;
    state.activeElement?.remove();
    delete host.__ntSnackbarState;
    hostStates.delete(host);

    if (defaultHost === host) {
        defaultHost = null;
    }
}

export function queueSnackbar(messageOrOptions: SnackbarInput, options?: Partial<SnackbarOptions>): string {
    const host = typeof messageOrOptions === 'string' ? getHost(options?.host) : getHost(options?.host ?? messageOrOptions.host);
    const snackbar = normalizeSnackbar(messageOrOptions, options);

    if (!host) {
        pendingSnackbars.push(snackbar);
        return snackbar.id;
    }

    const state = getOrCreateState(host);
    state.queue.push(snackbar);
    tryShowNext(host);
    return snackbar.id;
}

export function addSnackbar(messageOrOptions: SnackbarInput, options?: Partial<SnackbarOptions>): string {
    return queueSnackbar(messageOrOptions, options);
}

function closeSnackbarCore(id?: string, host?: HTMLElement | string | null, notifyDotNet = true): boolean {
    const resolvedHost = getHost(host);
    if (!resolvedHost) {
        return false;
    }

    const state = getOrCreateState(resolvedHost);
    if (!id || state.activeSnackbar?.id === id) {
        return closeActiveSnackbar(resolvedHost, state, notifyDotNet);
    }

    const existingIndex = state.queue.findIndex(snackbar => snackbar.id === id);
    if (existingIndex < 0) {
        return false;
    }

    const removedSnackbars = state.queue.splice(existingIndex, 1);
    if (notifyDotNet && removedSnackbars[0]) {
        notifySnackbarClosed(removedSnackbars[0]);
    }
    return true;
}

export function closeSnackbar(id?: string, host?: HTMLElement | string | null): boolean {
    return closeSnackbarCore(id, host);
}

export function closeSnackbarFromBlazor(id: string, host?: HTMLElement | string | null): boolean {
    return closeSnackbarCore(id, host, false);
}

export function clearSnackbars(host?: HTMLElement | string | null): void {
    const resolvedHost = getHost(host);
    pendingSnackbars.length = 0;
    if (!resolvedHost) {
        return;
    }

    const state = getOrCreateState(resolvedHost);
    if (state.closeTimeout !== null) {
        window.clearTimeout(state.closeTimeout);
        state.closeTimeout = null;
    }

    state.queue.length = 0;
    state.activeElement?.remove();
    state.activeElement = null;
    state.activeSnackbar = null;
}

export function onDispose(element?: Maybe<Element>): void {
    for (const host of getHosts(element)) {
        disposeHost(host);
    }
}

export function onLoad(element?: Maybe<Element>): void {
    for (const host of getHosts(element)) {
        initializeHost(host);
    }
}

export function onUpdate(element?: Maybe<Element>): void {
    for (const host of getHosts(element)) {
        initializeHost(host);
    }
}

window.NTSnackbar = {
    addSnackbar,
    clearSnackbars,
    closeSnackbarFromBlazor,
    closeSnackbar,
    queueSnackbar
};
