import {
    registerRichTextEditorTool
} from '../../wwwroot/NTComponents.lib.module.js';
import type {
    InlineToolCloseOptions,
    Maybe,
    RichTextEditorToolContext
} from '../Core/NTRichTextEditorToolRegistry.js';

interface LinkToolState {
    linkEditorInputs: HTMLInputElement[];
    linkApplyButton: HTMLButtonElement | null;
    linkCancelButton: HTMLButtonElement | null;
    linkTarget: HTMLAnchorElement | null;
    linkSelectedText: string;
    onLinkApply?: (event: MouseEvent) => void;
    onLinkCancel?: (event: MouseEvent) => void;
    onLinkEditorKeyDown?: (event: KeyboardEvent) => void;
}

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

function normalizeSafeLinkUrl(value: unknown): string {
    const trimmed = `${value ?? ''}`.trim();
    if (!trimmed) {
        return '';
    }

    if (trimmed.startsWith('#')
        || trimmed.startsWith('/')
        || trimmed.startsWith('./')
        || trimmed.startsWith('../')
        || trimmed.startsWith('?')) {
        return trimmed;
    }

    const scheme = extractUrlScheme(trimmed);
    if (!scheme) {
        return trimmed;
    }

    return ['http', 'https', 'mailto', 'tel'].includes(scheme) ? trimmed : '';
}

function getLinkUrlInput(element: Maybe<ParentNode>): HTMLInputElement | null {
    return qs<HTMLInputElement>(element, '[data-role="link-url"]');
}

function getLinkTextInput(element: Maybe<ParentNode>): HTMLInputElement | null {
    return qs<HTMLInputElement>(element, '[data-role="link-text"]');
}

function getLinkApplyButton(element: Maybe<ParentNode>): HTMLButtonElement | null {
    return qs<HTMLButtonElement>(element, '[data-role="link-apply"]');
}

function getLinkCancelButton(element: Maybe<ParentNode>): HTMLButtonElement | null {
    return qs<HTMLButtonElement>(element, '[data-role="link-cancel"]');
}

function initializeLinkToolStateHandlers(context: RichTextEditorToolContext<LinkToolState>): void {
    const { element, host, toolState } = context;
    if (toolState.onLinkApply) {
        return;
    }

    toolState.onLinkApply = (event: MouseEvent) => {
        event.preventDefault();
        applyLinkEditor(context);
    };

    toolState.onLinkCancel = (event: MouseEvent) => {
        event.preventDefault();
        closeLinkEditor(context, { focusSurface: true });
        host.updateToolbarState(element);
    };

    toolState.onLinkEditorKeyDown = (event: KeyboardEvent) => {
        if (event.key === 'Escape') {
            event.preventDefault();
            closeLinkEditor(context, { focusSurface: true });
            host.updateToolbarState(element);
            return;
        }

        if (event.key === 'Enter') {
            event.preventDefault();
            applyLinkEditor(context);
        }
    };
}

function bindLinkToolControls(context: RichTextEditorToolContext<LinkToolState>): void {
    const { element, toolState } = context;
    initializeLinkToolStateHandlers(context);
    unbindLinkToolControls(context);
    const linkApplyHandler = toolState.onLinkApply;
    const linkCancelHandler = toolState.onLinkCancel;
    const linkEditorKeyDownHandler = toolState.onLinkEditorKeyDown;

    toolState.linkApplyButton = getLinkApplyButton(element);
    toolState.linkCancelButton = getLinkCancelButton(element);
    toolState.linkEditorInputs = [
        getLinkUrlInput(element),
        getLinkTextInput(element)
    ].filter(isHtmlInputElement);

    if (linkApplyHandler) {
        toolState.linkApplyButton?.addEventListener('click', linkApplyHandler);
    }

    if (linkCancelHandler) {
        toolState.linkCancelButton?.addEventListener('click', linkCancelHandler);
    }

    if (linkEditorKeyDownHandler) {
        for (const input of toolState.linkEditorInputs) {
            input.addEventListener('keydown', linkEditorKeyDownHandler);
        }
    }
}

function unbindLinkToolControls(context: RichTextEditorToolContext<LinkToolState>): void {
    const { toolState } = context;
    if (toolState.onLinkApply) {
        toolState.linkApplyButton?.removeEventListener('click', toolState.onLinkApply);
    }

    if (toolState.onLinkCancel) {
        toolState.linkCancelButton?.removeEventListener('click', toolState.onLinkCancel);
    }

    if (toolState.onLinkEditorKeyDown) {
        for (const input of toolState.linkEditorInputs ?? []) {
            input.removeEventListener('keydown', toolState.onLinkEditorKeyDown);
        }
    }
}

function closeLinkEditor(context: RichTextEditorToolContext<LinkToolState>, { focusSurface = false, preserveSelection = false }: InlineToolCloseOptions = {}): void {
    const { element, editorState, host, toolState } = context;
    host.setToolPanelOpen(element, 'link', false);
    toolState.linkTarget = null;
    toolState.linkSelectedText = '';
    if (!preserveSelection) {
        editorState.selectionRange = null;
    }

    if (focusSurface) {
        host.getSurface(element)?.focus();
    }
}

function openLinkEditor(
    context: RichTextEditorToolContext<LinkToolState>,
    { focusInput = true, selectInputText = true }: { focusInput?: boolean; selectInputText?: boolean } = {}
): boolean {
    const { element, editorState, host, toolState } = context;
    const surface = host.getSurface(element);
    if (!surface) {
        return false;
    }

    host.closeOtherTools(element, editorState, 'link');

    const selectionElement = host.getSelectionElement(surface);
    const existingLinkCandidate = selectionElement?.closest?.('a') ?? null;
    const existingLink = existingLinkCandidate instanceof HTMLAnchorElement ? existingLinkCandidate : null;
    const selection = window.getSelection?.();
    const selectedText = selection?.toString?.().trim?.() ?? '';
    const linkText = existingLink?.textContent?.trim?.() || selectedText;
    const linkUrl = existingLink?.getAttribute('href') ?? '';

    host.saveSelectionRange(surface, editorState);
    toolState.linkTarget = existingLink;
    toolState.linkSelectedText = selectedText;

    const linkUrlInput = getLinkUrlInput(element);
    if (linkUrlInput) {
        linkUrlInput.value = linkUrl;
    }

    const linkTextInput = getLinkTextInput(element);
    if (linkTextInput) {
        linkTextInput.value = linkText;
    }

    host.setToolPanelOpen(element, 'link', true);

    if (focusInput) {
        linkUrlInput?.focus();
        if (selectInputText) {
            linkUrlInput?.select();
        }
    }

    return false;
}

function applyLinkEditor(context: RichTextEditorToolContext<LinkToolState>): boolean {
    const { element, editorState, host, toolState } = context;
    const surface = host.getSurface(element);
    if (!surface) {
        return false;
    }

    const linkUrl = normalizeSafeLinkUrl(getLinkUrlInput(element)?.value ?? '');
    if (!linkUrl) {
        getLinkUrlInput(element)?.focus();
        return false;
    }

    const linkTextInput = getLinkTextInput(element)?.value?.trim?.() ?? '';
    const linkText = linkTextInput || toolState.linkSelectedText || linkUrl;
    const linkTarget = toolState.linkTarget;

    if (linkTarget instanceof HTMLAnchorElement && surface.contains(linkTarget)) {
        linkTarget.setAttribute('href', linkUrl);
        linkTarget.textContent = linkText;
    } else {
        host.restoreSelectionRange(surface, editorState);
        const selection = window.getSelection?.();
        const range = selection?.rangeCount ? selection.getRangeAt(0) : null;
        if (!range || !surface.contains(range.commonAncestorContainer)) {
            getLinkUrlInput(element)?.focus();
            return false;
        }

        const anchor = document.createElement('a');
        anchor.setAttribute('href', linkUrl);
        anchor.textContent = linkText;

        range.deleteContents();
        range.insertNode(anchor);

        if (selection) {
            const updatedRange = document.createRange();
            updatedRange.selectNodeContents(anchor);
            selection.removeAllRanges();
            selection.addRange(updatedRange);
        }
    }

    closeLinkEditor(context, { focusSurface: true });
    return true;
}

registerRichTextEditorTool<LinkToolState>({
    command: 'link',
    createState: () => ({
        linkEditorInputs: [],
        linkApplyButton: null,
        linkCancelButton: null,
        linkTarget: null,
        linkSelectedText: '',
        onLinkApply: undefined,
        onLinkCancel: undefined,
        onLinkEditorKeyDown: undefined
    }),
    bind: bindLinkToolControls,
    unbind: unbindLinkToolControls,
    setDisabled: ({ toolState }, disabled) => {
        toolState.linkEditorInputs.forEach((input) => setDisabled(input, disabled));
        setDisabled(toolState.linkApplyButton, disabled);
        setDisabled(toolState.linkCancelButton, disabled);
    },
    execute: (context) => openLinkEditor(context),
    close: closeLinkEditor,
    syncState: (context) => {
        const { element, host } = context;
        const surface = host.getSurface(element);
        const selectionElement = host.getSelectionElement(surface);
        const existingLink = selectionElement?.closest?.('a') instanceof HTMLAnchorElement;
        const focusedToolCommand = host.getFocusedToolCommand(element);

        host.setToolbarButtonPressed(element, 'link', existingLink);

        if (focusedToolCommand === 'link') {
            return;
        }

        if (focusedToolCommand || !existingLink) {
            closeLinkEditor(context, { preserveSelection: Boolean(focusedToolCommand) });
            return;
        }

        openLinkEditor(context, { focusInput: false, selectInputText: false });
    }
});
