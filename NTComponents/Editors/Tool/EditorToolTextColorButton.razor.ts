import {
    registerRichTextEditorTool
} from '../../wwwroot/NTComponents.lib.module.js';
import type {
    InlineToolCloseOptions,
    Maybe,
    RichTextEditorToolContext
} from '../Core/NTRichTextEditorToolRegistry.js';

interface TextColorToolState {
    textColorEditorInputs: HTMLInputElement[];
    textColorApplyButton: HTMLButtonElement | null;
    textColorCancelButton: HTMLButtonElement | null;
    textColorTarget: HTMLSpanElement | null;
    onTextColorApply?: (event: MouseEvent) => void;
    onTextColorCancel?: (event: MouseEvent) => void;
    onTextColorEditorKeyDown?: (event: KeyboardEvent) => void;
}

const defaultTextColor = '#1d4ed8';

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

function isHtmlSpanElement(value: unknown): value is HTMLSpanElement {
    return value instanceof HTMLSpanElement;
}

function getElementTextColor(element: unknown): string {
    if (!(element instanceof Element)) {
        return '';
    }

    const styleColor = element instanceof HTMLElement ? element.style.color.trim() : '';
    if (styleColor) {
        return styleColor;
    }

    const styleAttribute = element.getAttribute?.('style') ?? '';
    const match = styleAttribute.match(/color:\s*([^;]+)/i);
    return match?.[1]?.trim?.() ?? '';
}

function normalizeHexColor(value: unknown): string {
    const trimmed = `${value ?? ''}`.trim();
    if (/^#[\da-f]{6}$/i.test(trimmed)) {
        return trimmed.toLowerCase();
    }

    if (/^#[\da-f]{3}$/i.test(trimmed)) {
        return `#${trimmed[1]}${trimmed[1]}${trimmed[2]}${trimmed[2]}${trimmed[3]}${trimmed[3]}`.toLowerCase();
    }

    return '';
}

function convertRgbChannelToHex(value: string): string {
    const numericValue = Number.parseInt(value, 10);
    if (Number.isNaN(numericValue)) {
        return '';
    }

    return Math.min(Math.max(numericValue, 0), 255).toString(16).padStart(2, '0');
}

function normalizeTextColorValue(value: unknown): string {
    const hexColor = normalizeHexColor(value);
    if (hexColor) {
        return hexColor;
    }

    const rgbMatch = `${value ?? ''}`.trim().match(/^rgba?\(\s*(\d{1,3})\s*,\s*(\d{1,3})\s*,\s*(\d{1,3})(?:\s*,[\d.]+\s*)?\)$/i);
    if (!rgbMatch) {
        return '';
    }

    const red = convertRgbChannelToHex(rgbMatch[1]);
    const green = convertRgbChannelToHex(rgbMatch[2]);
    const blue = convertRgbChannelToHex(rgbMatch[3]);
    return red && green && blue ? `#${red}${green}${blue}` : '';
}

function getTextColorInput(element: Maybe<ParentNode>): HTMLInputElement | null {
    return qs<HTMLInputElement>(element, '[data-role="text-color-value"]');
}

function getTextColorApplyButton(element: Maybe<ParentNode>): HTMLButtonElement | null {
    return qs<HTMLButtonElement>(element, '[data-role="text-color-apply"]');
}

function getTextColorCancelButton(element: Maybe<ParentNode>): HTMLButtonElement | null {
    return qs<HTMLButtonElement>(element, '[data-role="text-color-cancel"]');
}

function unwrapElement(element: Element): void {
    element.replaceWith(...Array.from(element.childNodes));
}

function mergeAdjacentColorSiblings(span: HTMLSpanElement, color: string): HTMLSpanElement {
    let current = span;

    let previous = current.previousSibling;
    while (previous instanceof HTMLSpanElement && normalizeTextColorValue(getElementTextColor(previous)) === color) {
        const source = previous;
        previous = source.previousSibling;
        current.prepend(...Array.from(source.childNodes));
        source.remove();
    }

    let next = current.nextSibling;
    while (next instanceof HTMLSpanElement && normalizeTextColorValue(getElementTextColor(next)) === color) {
        const source = next;
        next = source.nextSibling;
        current.append(...Array.from(source.childNodes));
        source.remove();
    }

    current.normalize();
    return current;
}

function normalizeTextColorSpan(span: HTMLSpanElement): HTMLSpanElement {
    const color = normalizeTextColorValue(getElementTextColor(span));
    if (!color) {
        return span;
    }

    span.style.color = color;

    for (const descendant of Array.from(span.querySelectorAll('span[style*="color"]'))) {
        if (!(descendant instanceof HTMLSpanElement)) {
            continue;
        }

        if (normalizeTextColorValue(getElementTextColor(descendant)) === color) {
            unwrapElement(descendant);
        }
    }

    const parent = span.parentElement;
    if (parent instanceof HTMLSpanElement && normalizeTextColorValue(getElementTextColor(parent)) === color) {
        for (const childNode of Array.from(span.childNodes)) {
            parent.insertBefore(childNode, span);
        }

        span.remove();
        return mergeAdjacentColorSiblings(parent, color);
    }

    return mergeAdjacentColorSiblings(span, color);
}

function applyTextColorRange(range: Range, color: string): void {
    const selectedContent = range.extractContents();
    const wrapper = document.createElement('span');
    wrapper.style.color = color;
    wrapper.appendChild(selectedContent);
    range.insertNode(wrapper);
    const normalizedWrapper = normalizeTextColorSpan(wrapper);

    const selection = window.getSelection?.();
    if (selection) {
        const updatedRange = document.createRange();
        updatedRange.selectNodeContents(normalizedWrapper);
        selection.removeAllRanges();
        selection.addRange(updatedRange);
    }
}

function initializeTextColorToolStateHandlers(context: RichTextEditorToolContext<TextColorToolState>): void {
    const { element, host, toolState } = context;
    if (toolState.onTextColorApply) {
        return;
    }

    toolState.onTextColorApply = (event: MouseEvent) => {
        event.preventDefault();
        applyTextColorEditor(context);
    };

    toolState.onTextColorCancel = (event: MouseEvent) => {
        event.preventDefault();
        closeTextColorEditor(context, { focusSurface: true });
        host.updateToolbarState(element);
    };

    toolState.onTextColorEditorKeyDown = (event: KeyboardEvent) => {
        if (event.key === 'Escape') {
            event.preventDefault();
            closeTextColorEditor(context, { focusSurface: true });
            host.updateToolbarState(element);
            return;
        }

        if (event.key === 'Enter') {
            event.preventDefault();
            applyTextColorEditor(context);
        }
    };
}

function bindTextColorToolControls(context: RichTextEditorToolContext<TextColorToolState>): void {
    const { element, toolState } = context;
    initializeTextColorToolStateHandlers(context);
    unbindTextColorToolControls(context);
    const textColorApplyHandler = toolState.onTextColorApply;
    const textColorCancelHandler = toolState.onTextColorCancel;
    const textColorEditorKeyDownHandler = toolState.onTextColorEditorKeyDown;

    toolState.textColorApplyButton = getTextColorApplyButton(element);
    toolState.textColorCancelButton = getTextColorCancelButton(element);
    toolState.textColorEditorInputs = [getTextColorInput(element)].filter(isHtmlInputElement);

    if (textColorApplyHandler) {
        toolState.textColorApplyButton?.addEventListener('click', textColorApplyHandler);
    }

    if (textColorCancelHandler) {
        toolState.textColorCancelButton?.addEventListener('click', textColorCancelHandler);
    }

    if (textColorEditorKeyDownHandler) {
        for (const input of toolState.textColorEditorInputs) {
            input.addEventListener('keydown', textColorEditorKeyDownHandler);
        }
    }
}

function unbindTextColorToolControls(context: RichTextEditorToolContext<TextColorToolState>): void {
    const { toolState } = context;
    if (toolState.onTextColorApply) {
        toolState.textColorApplyButton?.removeEventListener('click', toolState.onTextColorApply);
    }

    if (toolState.onTextColorCancel) {
        toolState.textColorCancelButton?.removeEventListener('click', toolState.onTextColorCancel);
    }

    if (toolState.onTextColorEditorKeyDown) {
        for (const input of toolState.textColorEditorInputs ?? []) {
            input.removeEventListener('keydown', toolState.onTextColorEditorKeyDown);
        }
    }
}

function closeTextColorEditor(context: RichTextEditorToolContext<TextColorToolState>, { focusSurface = false, preserveSelection = false }: InlineToolCloseOptions = {}): void {
    const { element, editorState, host, toolState } = context;
    host.setToolPanelOpen(element, 'textColor', false);
    toolState.textColorTarget = null;
    if (!preserveSelection) {
        editorState.selectionRange = null;
    }

    if (focusSurface) {
        host.getSurface(element)?.focus();
    }
}

function openTextColorEditor(
    context: RichTextEditorToolContext<TextColorToolState>,
    { focusInput = true }: { focusInput?: boolean } = {}
): boolean {
    const { element, editorState, host, toolState } = context;
    const surface = host.getSurface(element);
    if (!surface) {
        return false;
    }

    host.closeOtherTools(element, editorState, 'textColor');

    const selectionElement = host.getSelectionElement(surface);
    const existingTextColorCandidate = selectionElement?.closest?.('span[style*="color"]') ?? null;
    const existingTextColorElement = isHtmlSpanElement(existingTextColorCandidate) ? existingTextColorCandidate : null;
    const textColor = normalizeTextColorValue(getElementTextColor(existingTextColorElement)) || defaultTextColor;

    host.saveSelectionRange(surface, editorState);
    toolState.textColorTarget = existingTextColorElement;

    const textColorInput = getTextColorInput(element);
    if (textColorInput) {
        textColorInput.value = textColor;
    }

    host.setToolPanelOpen(element, 'textColor', true);

    if (focusInput) {
        textColorInput?.focus();
    }

    return false;
}

function applyTextColorEditor(context: RichTextEditorToolContext<TextColorToolState>): boolean {
    const { element, editorState, host, toolState } = context;
    const surface = host.getSurface(element);
    if (!surface) {
        return false;
    }

    const colorValue = normalizeTextColorValue(getTextColorInput(element)?.value ?? '') || defaultTextColor;
    const textColorTarget = toolState.textColorTarget;

    if (textColorTarget instanceof HTMLSpanElement && surface.contains(textColorTarget)) {
        textColorTarget.style.color = colorValue;
        normalizeTextColorSpan(textColorTarget);
    } else {
        host.restoreSelectionRange(surface, editorState);
        const selection = window.getSelection?.();
        const range = selection?.rangeCount ? selection.getRangeAt(0) : null;
        if (!range || range.collapsed || !surface.contains(range.commonAncestorContainer)) {
            getTextColorInput(element)?.focus();
            return false;
        }

        applyTextColorRange(range, colorValue);
    }

    closeTextColorEditor(context, { focusSurface: true });
    return true;
}

registerRichTextEditorTool<TextColorToolState>({
    command: 'textColor',
    createState: () => ({
        textColorEditorInputs: [],
        textColorApplyButton: null,
        textColorCancelButton: null,
        textColorTarget: null,
        onTextColorApply: undefined,
        onTextColorCancel: undefined,
        onTextColorEditorKeyDown: undefined
    }),
    bind: bindTextColorToolControls,
    unbind: unbindTextColorToolControls,
    setDisabled: ({ toolState }, disabled) => {
        toolState.textColorEditorInputs.forEach((input) => setDisabled(input, disabled));
        setDisabled(toolState.textColorApplyButton, disabled);
        setDisabled(toolState.textColorCancelButton, disabled);
    },
    execute: (context) => openTextColorEditor(context),
    close: closeTextColorEditor,
    syncState: (context) => {
        const { element, host } = context;
        const surface = host.getSurface(element);
        const selectionElement = host.getSelectionElement(surface);
        const existingTextColorElement = isHtmlSpanElement(selectionElement?.closest?.('span[style*="color"]') ?? null);
        const focusedToolCommand = host.getFocusedToolCommand(element);

        host.setToolbarButtonPressed(element, 'textColor', existingTextColorElement);

        if (focusedToolCommand === 'textColor') {
            return;
        }

        if (focusedToolCommand || !existingTextColorElement) {
            closeTextColorEditor(context, { preserveSelection: Boolean(focusedToolCommand) });
            return;
        }

        openTextColorEditor(context, { focusInput: false });
    }
});
