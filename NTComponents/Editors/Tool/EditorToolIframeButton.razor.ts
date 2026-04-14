import {
    registerRichTextEditorTool
} from '../../wwwroot/NTComponents.lib.module.js';
import type {
    InlineToolCloseOptions,
    Maybe,
    RichTextEditorToolContext
} from '../Core/NTRichTextEditorToolRegistry.js';

interface IframeToolState {
    iframeEditorInputs: HTMLInputElement[];
    iframeApplyButton: HTMLButtonElement | null;
    iframeCancelButton: HTMLButtonElement | null;
    iframeTarget: HTMLIFrameElement | null;
    onIframeApply?: (event: MouseEvent) => void;
    onIframeCancel?: (event: MouseEvent) => void;
    onIframeEditorKeyDown?: (event: KeyboardEvent) => void;
}

const defaultIframeTitle = 'Embedded content';
const defaultIframeWidth = '100%';
const defaultIframeHeight = '315';

function qs<T extends Element>(root: Maybe<ParentNode>, selector: string): T | null {
    return (root?.querySelector?.(selector) as T | null) ?? null;
}

function isHtmlInputElement(value: unknown): value is HTMLInputElement {
    return value instanceof HTMLInputElement;
}

function setDisabled(control: Maybe<HTMLButtonElement | HTMLInputElement>, disabled: boolean): void {
    if (control) {
        control.disabled = disabled;
    }
}

function extractUrlScheme(value: string): string {
    const match = value.match(/^([a-z][a-z0-9+.-]*):/i);
    return match?.[1]?.toLowerCase() ?? '';
}

function normalizeSafeIframeUrl(value: unknown): string {
    const trimmed = `${value ?? ''}`.trim();
    if (!trimmed) {
        return '';
    }

    if (trimmed.startsWith('/')
        || trimmed.startsWith('./')
        || trimmed.startsWith('../')
        || trimmed.startsWith('?')) {
        return trimmed;
    }

    const scheme = extractUrlScheme(trimmed);
    if (!scheme) {
        return trimmed;
    }

    return ['http', 'https'].includes(scheme) ? trimmed : '';
}

function escapeHtml(value: string): string {
    return value
        .replaceAll('&', '&amp;')
        .replaceAll('<', '&lt;')
        .replaceAll('>', '&gt;')
        .replaceAll('"', '&quot;')
        .replaceAll('\'', '&#39;');
}

function getIframeUrlInput(element: Maybe<ParentNode>): HTMLInputElement | null {
    return qs<HTMLInputElement>(element, '[data-role="iframe-url"]');
}

function getIframeTitleInput(element: Maybe<ParentNode>): HTMLInputElement | null {
    return qs<HTMLInputElement>(element, '[data-role="iframe-title"]');
}

function getIframeWidthInput(element: Maybe<ParentNode>): HTMLInputElement | null {
    return qs<HTMLInputElement>(element, '[data-role="iframe-width"]');
}

function getIframeHeightInput(element: Maybe<ParentNode>): HTMLInputElement | null {
    return qs<HTMLInputElement>(element, '[data-role="iframe-height"]');
}

function getIframeApplyButton(element: Maybe<ParentNode>): HTMLButtonElement | null {
    return qs<HTMLButtonElement>(element, '[data-role="iframe-apply"]');
}

function getIframeCancelButton(element: Maybe<ParentNode>): HTMLButtonElement | null {
    return qs<HTMLButtonElement>(element, '[data-role="iframe-cancel"]');
}

function normalizeIframeField(value: unknown, fallbackValue: string): string {
    const trimmed = `${value ?? ''}`.trim();
    return trimmed.length > 0 ? trimmed : fallbackValue;
}

function buildIframeHtml(url: string, title: string, width: string, height: string): string {
    const normalizedUrl = normalizeSafeIframeUrl(url);
    if (!normalizedUrl) {
        return '';
    }

    return `<iframe src="${escapeHtml(normalizedUrl)}" title="${escapeHtml(title)}" width="${escapeHtml(width)}" height="${escapeHtml(height)}" loading="lazy"></iframe>`;
}

function initializeIframeToolStateHandlers(context: RichTextEditorToolContext<IframeToolState>): void {
    const { element, host, toolState } = context;
    if (toolState.onIframeApply) {
        return;
    }

    toolState.onIframeApply = (event: MouseEvent) => {
        event.preventDefault();
        applyIframeEditor(context);
    };

    toolState.onIframeCancel = (event: MouseEvent) => {
        event.preventDefault();
        closeIframeEditor(context, { focusSurface: true });
        host.updateToolbarState(element);
    };

    toolState.onIframeEditorKeyDown = (event: KeyboardEvent) => {
        if (event.key === 'Escape') {
            event.preventDefault();
            closeIframeEditor(context, { focusSurface: true });
            host.updateToolbarState(element);
            return;
        }

        if (event.key === 'Enter') {
            event.preventDefault();
            applyIframeEditor(context);
        }
    };
}

function bindIframeToolControls(context: RichTextEditorToolContext<IframeToolState>): void {
    const { element, toolState } = context;
    initializeIframeToolStateHandlers(context);
    unbindIframeToolControls(context);
    const iframeApplyHandler = toolState.onIframeApply;
    const iframeCancelHandler = toolState.onIframeCancel;
    const iframeEditorKeyDownHandler = toolState.onIframeEditorKeyDown;

    toolState.iframeApplyButton = getIframeApplyButton(element);
    toolState.iframeCancelButton = getIframeCancelButton(element);
    toolState.iframeEditorInputs = [
        getIframeUrlInput(element),
        getIframeTitleInput(element),
        getIframeWidthInput(element),
        getIframeHeightInput(element)
    ].filter(isHtmlInputElement);

    if (iframeApplyHandler) {
        toolState.iframeApplyButton?.addEventListener('click', iframeApplyHandler);
    }

    if (iframeCancelHandler) {
        toolState.iframeCancelButton?.addEventListener('click', iframeCancelHandler);
    }

    if (iframeEditorKeyDownHandler) {
        for (const input of toolState.iframeEditorInputs) {
            input.addEventListener('keydown', iframeEditorKeyDownHandler);
        }
    }
}

function unbindIframeToolControls(context: RichTextEditorToolContext<IframeToolState>): void {
    const { toolState } = context;
    if (toolState.onIframeApply) {
        toolState.iframeApplyButton?.removeEventListener('click', toolState.onIframeApply);
    }

    if (toolState.onIframeCancel) {
        toolState.iframeCancelButton?.removeEventListener('click', toolState.onIframeCancel);
    }

    if (toolState.onIframeEditorKeyDown) {
        for (const input of toolState.iframeEditorInputs ?? []) {
            input.removeEventListener('keydown', toolState.onIframeEditorKeyDown);
        }
    }
}

function closeIframeEditor(context: RichTextEditorToolContext<IframeToolState>, { focusSurface = false, preserveSelection = false }: InlineToolCloseOptions = {}): void {
    const { element, editorState, host, toolState } = context;
    host.setToolPanelOpen(element, 'iframe', false);
    toolState.iframeTarget = null;
    if (!preserveSelection) {
        editorState.selectionRange = null;
    }

    if (focusSurface) {
        host.getSurface(element)?.focus();
    }
}

function openIframeEditor(
    context: RichTextEditorToolContext<IframeToolState>,
    { focusInput = true, selectInputText = true }: { focusInput?: boolean; selectInputText?: boolean } = {}
): boolean {
    const { element, editorState, host, toolState } = context;
    const surface = host.getSurface(element);
    if (!surface) {
        return false;
    }

    host.closeOtherTools(element, editorState, 'iframe');

    const selectionElement = host.getSelectionElement(surface);
    const existingIframeCandidate = selectionElement?.closest?.('iframe') ?? null;
    const existingIframe = existingIframeCandidate instanceof HTMLIFrameElement ? existingIframeCandidate : null;

    host.saveSelectionRange(surface, editorState);
    toolState.iframeTarget = existingIframe;

    const iframeUrlInput = getIframeUrlInput(element);
    if (iframeUrlInput) {
        iframeUrlInput.value = existingIframe?.getAttribute('src') ?? '';
    }

    const iframeTitleInput = getIframeTitleInput(element);
    if (iframeTitleInput) {
        iframeTitleInput.value = existingIframe?.getAttribute('title') ?? defaultIframeTitle;
    }

    const iframeWidthInput = getIframeWidthInput(element);
    if (iframeWidthInput) {
        iframeWidthInput.value = existingIframe?.getAttribute('width') ?? defaultIframeWidth;
    }

    const iframeHeightInput = getIframeHeightInput(element);
    if (iframeHeightInput) {
        iframeHeightInput.value = existingIframe?.getAttribute('height') ?? defaultIframeHeight;
    }

    host.setToolPanelOpen(element, 'iframe', true);

    if (focusInput) {
        iframeUrlInput?.focus();
        if (selectInputText) {
            iframeUrlInput?.select();
        }
    }

    return false;
}

function applyIframeEditor(context: RichTextEditorToolContext<IframeToolState>): boolean {
    const { element, editorState, host, toolState } = context;
    const surface = host.getSurface(element);
    if (!surface) {
        return false;
    }

    const iframeUrl = normalizeSafeIframeUrl(getIframeUrlInput(element)?.value ?? '');
    if (!iframeUrl) {
        getIframeUrlInput(element)?.focus();
        return false;
    }

    const iframeTitle = normalizeIframeField(getIframeTitleInput(element)?.value, defaultIframeTitle);
    const iframeWidth = normalizeIframeField(getIframeWidthInput(element)?.value, defaultIframeWidth);
    const iframeHeight = normalizeIframeField(getIframeHeightInput(element)?.value, defaultIframeHeight);
    const iframeTarget = toolState.iframeTarget;

    if (iframeTarget instanceof HTMLIFrameElement && surface.contains(iframeTarget)) {
        iframeTarget.setAttribute('src', iframeUrl);
        iframeTarget.setAttribute('title', iframeTitle);
        iframeTarget.setAttribute('width', iframeWidth);
        iframeTarget.setAttribute('height', iframeHeight);
        iframeTarget.setAttribute('loading', 'lazy');
    } else {
        host.restoreSelectionRange(surface, editorState);
        const iframeHtml = buildIframeHtml(iframeUrl, iframeTitle, iframeWidth, iframeHeight);
        if (!iframeHtml) {
            getIframeUrlInput(element)?.focus();
            return false;
        }

        document.execCommand('insertHTML', false, iframeHtml);
    }

    closeIframeEditor(context, { focusSurface: true });
    return true;
}

registerRichTextEditorTool<IframeToolState>({
    command: 'iframe',
    createState: () => ({
        iframeEditorInputs: [],
        iframeApplyButton: null,
        iframeCancelButton: null,
        iframeTarget: null,
        onIframeApply: undefined,
        onIframeCancel: undefined,
        onIframeEditorKeyDown: undefined
    }),
    bind: bindIframeToolControls,
    unbind: unbindIframeToolControls,
    setDisabled: ({ toolState }, disabled) => {
        toolState.iframeEditorInputs.forEach((input) => setDisabled(input, disabled));
        setDisabled(toolState.iframeApplyButton, disabled);
        setDisabled(toolState.iframeCancelButton, disabled);
    },
    execute: (context) => openIframeEditor(context),
    close: closeIframeEditor,
    syncState: (context) => {
        const { element, host } = context;
        const surface = host.getSurface(element);
        const selectionElement = host.getSelectionElement(surface);
        const existingIframe = selectionElement?.closest?.('iframe') instanceof HTMLIFrameElement;
        const focusedToolCommand = host.getFocusedToolCommand(element);

        host.setToolbarButtonPressed(element, 'iframe', existingIframe);

        if (focusedToolCommand === 'iframe') {
            return;
        }

        if (focusedToolCommand || !existingIframe) {
            closeIframeEditor(context, { preserveSelection: Boolean(focusedToolCommand) });
            return;
        }

        openIframeEditor(context, { focusInput: false, selectInputText: false });
    }
});
