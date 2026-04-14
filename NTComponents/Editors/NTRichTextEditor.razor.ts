import {
    createRichTextEditorToolContext,
    getRichTextEditorTool,
    setRichTextEditorToolRegistryChangedCallback
} from '../wwwroot/NTComponents.lib.module.js';
import type {
    Maybe,
    RichTextEditorToolEditorState,
    RichTextEditorToolHost,
    RichTextEditorToolPlugin
} from './Core/NTRichTextEditorToolRegistry.js';

// Source of truth for the rich text editor module.
// Rebuild NTRichTextEditor.razor.js with: npm run build:rich-text-editor
type Alignment = '' | 'left' | 'center' | 'right' | 'justify';
type DotNetEditorMethod = 'UpdateValueFromJs' | 'CommitValueFromJs' | 'UpdateMarkupValueFromJs';

interface DotNetEditorRef {
    invokeMethodAsync(methodName: 'UpdateValueFromJs' | 'CommitValueFromJs', markdown: string, html: string): Promise<unknown> | void;
    invokeMethodAsync(methodName: 'UpdateMarkupValueFromJs', html: string): Promise<unknown> | void;
}

interface InlineToken {
    token: string;
    html: string;
}

interface ListMarker {
    indent: number;
    ordered: boolean;
    content: string;
}

interface IframeDetails {
    src: string;
    title: string;
    width: string;
    height: string;
}

interface ImageDetails {
    src: string;
    alt: string;
    width: string;
    height: string;
}

interface BlockRenderResult {
    html: string;
    nextIndex: number;
}

interface ExistingTableContent {
    headers: string[];
    rows: string[][];
}

interface TableEditorDetails {
    columns: number;
    rows: number;
    borderColor: string;
}

interface TableHtmlOptions {
    columns: number;
    rows: number;
    borderColor: string;
    existingContent?: ExistingTableContent | null;
}

interface EditorState extends RichTextEditorToolEditorState {
    dotNetRef: DotNetEditorRef | null;
    isDisposed: boolean;
    blurTimeoutId: number | null;
    pendingSyncFrameId: number | null;
    requiresInitialRender: boolean;
    toolbarButtons: HTMLButtonElement[];
    toolCommands: string[];
    lastMarkdown: string;
    lastHtml: string;
    onInput: () => void;
    onFocus: () => void;
    onFocusIn: () => void;
    onBlur: () => void;
    onKeyUp: () => void;
    onMouseUp: () => void;
    onKeyDown: (event: KeyboardEvent) => void;
    onToolbarMouseDown: (event: MouseEvent) => void;
}

type RegisteredTool = RichTextEditorToolPlugin<unknown>;
type RegisteredToolContext = ReturnType<typeof createRichTextEditorToolContext<unknown>>;

const editorState = new WeakMap<HTMLElement, EditorState>();
const blockNodeTags = new Set<string>(['DIV', 'P', 'H1', 'H2', 'H3', 'H4', 'H5', 'H6', 'UL', 'OL', 'BLOCKQUOTE', 'PRE', 'TABLE', 'IFRAME']);
const blockSelector = 'h1, h2, h3, h4, h5, h6, pre, blockquote, table, th, td, iframe, li, p, div';
const defaultTableBorderColor = '#94a3b8';
const defaultTextColor = '#1d4ed8';
const editorElements = new Set<HTMLElement>();
let selectionChangeRegistered = false;
let activeEditor: HTMLElement | null = null;

setRichTextEditorToolRegistryChangedCallback(() => {
    initializeAllEditors();
});

function toArray<T>(values: Maybe<Iterable<T> | ArrayLike<T>>): T[] {
    return values ? Array.from(values) : [];
}

function qs<T extends Element>(root: Maybe<ParentNode>, selector: string): T | null {
    return (root?.querySelector?.(selector) as T | null) ?? null;
}

function isElement(value: unknown): value is Element {
    return value instanceof Element;
}

function isHtmlButtonElement(value: unknown): value is HTMLButtonElement {
    return value instanceof HTMLButtonElement;
}

function isHtmlElement(value: unknown): value is HTMLElement {
    return value instanceof HTMLElement;
}

function isHtmlInputElement(value: unknown): value is HTMLInputElement {
    return value instanceof HTMLInputElement;
}

function isHtmlSpanElement(value: unknown): value is HTMLSpanElement {
    return value instanceof HTMLSpanElement;
}

function isTableCellElement(value: unknown): value is HTMLTableCellElement {
    return value instanceof HTMLTableCellElement;
}

function isTableRowElement(value: unknown): value is HTMLTableRowElement {
    return value instanceof HTMLTableRowElement;
}

function getTableCells(row: Maybe<HTMLTableRowElement>): HTMLTableCellElement[] {
    return toArray(row?.children).filter(isTableCellElement);
}

function clearBlurTimeout(state: Maybe<EditorState>): void {
    if (!state || state.blurTimeoutId === null) {
        return;
    }

    window.clearTimeout(state.blurTimeoutId);
    state.blurTimeoutId = null;
}

function clearPendingSync(state: Maybe<EditorState>): void {
    if (!state || state.pendingSyncFrameId === null) {
        return;
    }

    window.cancelAnimationFrame(state.pendingSyncFrameId);
    state.pendingSyncFrameId = null;
}

function placeCaretAtEnd(surface: HTMLElement): void {
    surface.focus();

    const selection = window.getSelection?.();
    if (!selection) {
        return;
    }

    const range = document.createRange();
    range.selectNodeContents(surface);
    range.collapse(false);
    selection.removeAllRanges();
    selection.addRange(range);
}

function getSurface(element: Maybe<ParentNode>): HTMLElement | null {
    return qs<HTMLElement>(element, '.tnt-rich-text-editor-surface');
}

function getSourceValueElement(element: Maybe<ParentNode>): HTMLDivElement | null {
    return qs<HTMLDivElement>(element, '.tnt-rich-text-editor-value');
}

function getHiddenInput(element: Maybe<ParentNode>): HTMLInputElement | null {
    return qs<HTMLInputElement>(element, '.tnt-rich-text-editor-hidden-input');
}

function getLengthElement(element: Maybe<Element>): HTMLElement | null {
    return qs<HTMLElement>(element?.closest?.('.tnt-input-container') ?? null, '.tnt-input-length');
}

function getToolbarButtons(element: Maybe<ParentNode>): HTMLButtonElement[] {
    return toArray(element?.querySelectorAll?.('.tnt-rich-text-editor-toolbar .tnt-rich-text-editor-toolbar-button')).filter(isHtmlButtonElement);
}

function isEditorEditable(element: Maybe<HTMLElement>): boolean {
    return element?.dataset.editable === 'true';
}

function setActiveEditor(element: Maybe<HTMLElement>): void {
    activeEditor = element ?? null;
}

function clearActiveEditor(element: Maybe<HTMLElement>): void {
    if (activeEditor === element) {
        activeEditor = null;
    }
}

function invokeDotNetVoid(
    dotNetRef: Maybe<DotNetEditorRef>,
    methodName: DotNetEditorMethod,
    ...args: string[]
): void {
    if (!dotNetRef) {
        return;
    }

    const invocation = methodName === 'UpdateMarkupValueFromJs'
        ? dotNetRef.invokeMethodAsync('UpdateMarkupValueFromJs', args[0] ?? '')
        : dotNetRef.invokeMethodAsync(methodName, args[0] ?? '', args[1] ?? '');

    Promise.resolve(invocation)
        .catch(() => {
            // Ignore late/disconnected interop failures from fire-and-forget editor sync paths.
        });
}

function extractUrlScheme(value: string): string {
    const match = value.match(/^([a-z][a-z0-9+.-]*):/i);
    return match?.[1]?.toLowerCase() ?? '';
}

function normalizeSafeUrl(value: unknown, kind: 'link' | 'iframe' | 'image'): string {
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

    if (kind === 'link' && ['http', 'https', 'mailto', 'tel'].includes(scheme)) {
        return trimmed;
    }

    if (kind === 'iframe' && ['http', 'https'].includes(scheme)) {
        return trimmed;
    }

    if (kind === 'image' && ['http', 'https', 'data', 'blob'].includes(scheme)) {
        return trimmed;
    }

    return '';
}

function canUseMarkdownUrl(url: string): boolean {
    return !/[)\s]/.test(url);
}

function unbindToolbarButtons(buttons: Maybe<HTMLButtonElement[]>, state: Maybe<EditorState>): void {
    if (!state) {
        return;
    }

    for (const button of buttons ?? []) {
        button.removeEventListener('mousedown', state.onToolbarMouseDown);
        button.removeEventListener('click', handleToolbarCommand);
    }
}

function bindToolbarButtons(element: Maybe<HTMLElement>, state: EditorState): void {
    unbindToolbarButtons(state?.toolbarButtons, state);

    const buttons = getToolbarButtons(element);
    for (const button of buttons) {
        button.addEventListener('mousedown', state.onToolbarMouseDown);
        button.addEventListener('click', handleToolbarCommand);
    }

    state.toolbarButtons = buttons;
}

function getChildNodeIndex(node: Node): number {
    const parentNode = node.parentNode;
    return parentNode ? toArray<Node>(parentNode.childNodes).indexOf(node) : -1;
}

function getBoundaryAdjacentElement(surface: HTMLElement, container: Node, offset: number, direction: 'before' | 'after'): Element | null {
    let currentNode: Node | null = container;
    let currentOffset = offset;

    while (currentNode) {
        if (currentNode.nodeType === Node.TEXT_NODE) {
            const textLength = currentNode.textContent?.length ?? 0;
            if (direction === 'before') {
                if (currentOffset !== 0) {
                    return null;
                }
            } else if (currentOffset !== textLength) {
                return null;
            }

            const textNodeIndex = getChildNodeIndex(currentNode);
            currentNode = currentNode.parentNode;
            if (!currentNode || textNodeIndex < 0) {
                return null;
            }

            currentOffset = direction === 'before' ? textNodeIndex : textNodeIndex + 1;
            continue;
        }

        if (currentNode.nodeType !== Node.ELEMENT_NODE) {
            return null;
        }

        const currentElement = currentNode as Element;
        const childNodes = toArray(currentElement.childNodes);

        if (direction === 'before') {
            if (currentOffset > 0) {
                const candidateNode = childNodes[currentOffset - 1];
                return candidateNode instanceof Element && surface.contains(candidateNode) ? candidateNode : null;
            }
        } else if (currentOffset < childNodes.length) {
            const candidateNode = childNodes[currentOffset];
            return candidateNode instanceof Element && surface.contains(candidateNode) ? candidateNode : null;
        }

        if (currentElement === surface) {
            return null;
        }

        const elementNodeIndex = getChildNodeIndex(currentElement);
        currentNode = currentElement.parentNode;
        if (!currentNode || elementNodeIndex < 0) {
            return null;
        }

        currentOffset = direction === 'before' ? elementNodeIndex : elementNodeIndex + 1;
    }

    return null;
}

function getSelectionElement(surface: Maybe<HTMLElement>): Element | null {
    if (!surface) {
        return null;
    }

    const selection = window.getSelection?.();
    const range = selection?.rangeCount ? selection.getRangeAt(0) : null;
    let node: Node | null = selection?.anchorNode ?? surface;

    if (range?.collapsed) {
        const adjacentElement = getBoundaryAdjacentElement(surface, range.startContainer, range.startOffset, 'before')
            ?? getBoundaryAdjacentElement(surface, range.startContainer, range.startOffset, 'after');
        if (adjacentElement) {
            return adjacentElement;
        }
    }

    if (node?.nodeType === Node.TEXT_NODE) {
        node = node.parentElement;
    }

    if (node instanceof Element && surface.contains(node)) {
        return node;
    }

    return null;
}

function getCurrentBlockElement(surface: Maybe<HTMLElement>): Element | null {
    const selectionElement = getSelectionElement(surface);
    if (selectionElement) {
        return selectionElement.closest(blockSelector) ?? selectionElement;
    }

    return null;
}

function getCurrentAlignment(surface: Maybe<HTMLElement>, blockElement: Maybe<Element>): Alignment {
    const selectionElement = getSelectionElement(surface) ?? blockElement;
    let node = selectionElement;

    while (node instanceof Element && surface?.contains(node)) {
        const alignment = getElementAlignment(node);
        if (alignment) {
            return alignment;
        }

        node = node.parentElement;
    }

    return getElementAlignment(blockElement) || 'left';
}

function hasAncestorTag(surface: Maybe<HTMLElement>, tagNames: string): boolean {
    const selectionElement = getSelectionElement(surface);
    return Boolean(selectionElement?.closest?.(tagNames));
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

    const rgbMatch = `${value ?? ''}`.trim().match(/^rgba?\(\s*(\d{1,3})\s*,\s*(\d{1,3})\s*,\s*(\d{1,3})(?:\s*,\s*[\d.]+\s*)?\)$/i);
    if (!rgbMatch) {
        return '';
    }

    const red = convertRgbChannelToHex(rgbMatch[1]);
    const green = convertRgbChannelToHex(rgbMatch[2]);
    const blue = convertRgbChannelToHex(rgbMatch[3]);
    return red && green && blue ? `#${red}${green}${blue}` : '';
}

function normalizeNewLines(value: unknown): string {
    return `${value ?? ''}`.replace(/\r\n/g, '\n').replace(/\r/g, '\n');
}

function countLeadingSpaces(value: string): number {
    let count = 0;
    while (count < value.length && value[count] === ' ') {
        count++;
    }

    return count;
}

function stripIndent(value: string, count: number): string {
    return count >= value.length ? '' : value.slice(count);
}

function escapeHtml(value: string): string {
    return value
        .replaceAll('&', '&amp;')
        .replaceAll('<', '&lt;')
        .replaceAll('>', '&gt;')
        .replaceAll('"', '&quot;')
        .replaceAll('\'', '&#39;');
}

function createInlineToken(tokens: InlineToken[], html: string): string {
    const token = `__NT_INLINE_TOKEN_${tokens.length}__`;
    tokens.push({ token, html });
    return token;
}

function normalizeImageDimension(value: unknown): string {
    const trimmed = `${value ?? ''}`.trim();
    return /^\d+$/.test(trimmed) ? trimmed : '';
}

function normalizeTableBorderColor(value: unknown): string {
    const trimmed = `${value ?? ''}`.trim();
    return /^#[\da-f]{6}$/i.test(trimmed) ? trimmed.toLowerCase() : '';
}

function buildTableStyleAttribute(borderColor: unknown): string {
    const normalizedBorderColor = normalizeTableBorderColor(borderColor);
    if (!normalizedBorderColor) {
        return '';
    }

    const encodedBorderColor = escapeHtml(normalizedBorderColor);
    return ` data-border-color="${encodedBorderColor}" style="--nt-rich-text-table-border-color:${encodedBorderColor};"`;
}

function buildImageHtml({ src, alt = '', width = '', height = '' }: ImageDetails): string {
    const normalizedSource = normalizeSafeUrl(src, 'image');
    if (!normalizedSource) {
        return '';
    }

    const normalizedWidth = normalizeImageDimension(width);
    const normalizedHeight = normalizeImageDimension(height);
    const widthAttribute = normalizedWidth ? ` width="${escapeHtml(normalizedWidth)}"` : '';
    const heightAttribute = normalizedHeight ? ` height="${escapeHtml(normalizedHeight)}"` : '';
    return `<img src="${escapeHtml(normalizedSource)}" alt="${escapeHtml(alt)}"${widthAttribute}${heightAttribute} />`;
}

function buildImageMarkdown({ src, alt = '', width = '', height = '' }: ImageDetails): string {
    const normalizedSource = normalizeSafeUrl(src, 'image');
    if (!normalizedSource) {
        return escapeMarkdownText(alt);
    }

    const normalizedWidth = normalizeImageDimension(width);
    const normalizedHeight = normalizeImageDimension(height);
    if (!normalizedWidth && !normalizedHeight && canUseMarkdownUrl(normalizedSource)) {
        return `![${escapeMarkdownAttribute(alt)}](${normalizedSource})`;
    }

    const widthAttribute = normalizedWidth ? ` width="${escapeMarkdownAttribute(normalizedWidth)}"` : '';
    const heightAttribute = normalizedHeight ? ` height="${escapeMarkdownAttribute(normalizedHeight)}"` : '';
    return `<img src="${normalizedSource}" alt="${escapeMarkdownAttribute(alt)}"${widthAttribute}${heightAttribute} />`;
}

function renderMarkdownImage(_match: string, alt: string, url: string): string {
    return buildImageHtml({ src: url, alt, width: '', height: '' });
}

function renderHtmlImage(_match: string, src: string, alt = '', width = '', height = ''): string {
    return buildImageHtml({ src, alt, width, height });
}

function getImageDetails(imageElement: unknown): ImageDetails {
    if (!(imageElement instanceof HTMLImageElement)) {
        return {
            src: '',
            alt: '',
            width: '',
            height: ''
        };
    }

    return {
        src: imageElement.getAttribute('src') ?? '',
        alt: imageElement.getAttribute('alt') ?? '',
        width: normalizeImageDimension(imageElement.getAttribute('width') ?? ''),
        height: normalizeImageDimension(imageElement.getAttribute('height') ?? '')
    };
}

function renderHtmlAnchor(_match: string, href: string, text: string): string {
    const normalizedHref = normalizeSafeUrl(href, 'link');
    const renderedText = renderInlineMarkdown(text);
    return normalizedHref ? `<a href="${escapeHtml(normalizedHref)}">${renderedText}</a>` : renderedText;
}

function renderLink(_match: string, text: string, url: string): string {
    return renderHtmlAnchor(_match, url, text);
}

function renderTextColor(_match: string, color: string, content: string): string {
    return `<span style="color:${escapeHtml(color.trim())};">${renderInlineMarkdown(content)}</span>`;
}

function renderInlineMarkdown(value: string): string {
    const tokens: InlineToken[] = [];
    let rendered = value.replace(/<img\b[^>]*>/gi, (match) => {
        const template = document.createElement('template');
        template.innerHTML = match;
        const image = template.content.querySelector('img');
        return image instanceof HTMLImageElement
            ? createInlineToken(tokens, buildImageHtml(getImageDetails(image)))
            : match;
    });
    rendered = rendered.replace(/<a\b[^>]*href="([^"]+)"[^>]*>([\s\S]+?)<\/a>/gi,
        (match, href, text) => createInlineToken(tokens, renderHtmlAnchor(match, href, text)));
    rendered = rendered.replace(/<img\s+src="([^"]+)"(?:\s+alt="([^"]*)")?(?:\s+width="([^"]+)")?(?:\s+height="([^"]+)")?\s*\/?>/gi,
        (match, src, alt, width, height) => createInlineToken(tokens, renderHtmlImage(match, src, alt ?? '', width ?? '', height ?? '')));
    rendered = rendered.replace(/!\[([^\]]*)\]\(([^)\s]+)\)/g, (match, alt, url) => createInlineToken(tokens, renderMarkdownImage(match, alt, url)));
    rendered = rendered.replace(/(?<!!)\[([^\]]+)\]\(([^)\s]+)\)/g, (match, text, url) => createInlineToken(tokens, renderLink(match, text, url)));
    rendered = rendered.replace(/<span\s+style="color:\s*([^";>]+)\s*;?">\s*([\s\S]+?)\s*<\/span>/gi, (match, color, content) => createInlineToken(tokens, renderTextColor(match, color, content)));
    rendered = rendered.replace(/<u>([\s\S]+?)<\/u>/gi, (_, content) => createInlineToken(tokens, `<u>${renderInlineMarkdown(content)}</u>`));

    rendered = escapeHtml(rendered);
    rendered = rendered.replace(/\*\*(.+?)\*\*/gs, '<strong>$1</strong>');
    rendered = rendered.replace(/(^|[^*])\*([^*\r\n]+?)\*(?!\*)/gs, (_, prefix, content) => `${prefix}<em>${content}</em>`);
    rendered = rendered.replace(/~~(.+?)~~/gs, '<s>$1</s>');

    for (const token of tokens) {
        rendered = rendered.replaceAll(token.token, token.html);
    }

    return rendered;
}

function isCodeFenceLine(line: string): boolean {
    return line.trimStart().startsWith('```');
}

function isBlockQuoteLine(line: string): boolean {
    return line.trimStart().startsWith('>');
}

function stripBlockQuoteMarker(line: string): string {
    const trimmed = line.trimStart();
    if (!trimmed.startsWith('>')) {
        return trimmed;
    }

    return trimmed.length > 1 && trimmed[1] === ' ' ? trimmed.slice(2) : trimmed.slice(1);
}

function parseListMarker(line: string): ListMarker | null {
    const indent = countLeadingSpaces(line);
    const trimmed = stripIndent(line, indent);

    if (trimmed.length >= 2 && ['-', '*', '+'].includes(trimmed[0]) && trimmed[1] === ' ') {
        return {
            indent,
            ordered: false,
            content: trimmed.slice(2)
        };
    }

    const orderedMatch = trimmed.match(/^\d+\.\s+(.*)$/s);
    if (!orderedMatch) {
        return null;
    }

    return {
        indent,
        ordered: true,
        content: orderedMatch[1]
    };
}

function parseAlignmentMarker(line: string): Alignment | null {
    const match = line.trim().match(/^<div\s+align="(left|center|right|justify)">$/i);
    if (!match) {
        return null;
    }

    return normalizeAlignment(match[1]);
}

function parseIframeMarker(line: string): IframeDetails | null {
    const trimmed = line.trim();
    if (!/^<iframe\b/i.test(trimmed)) {
        return null;
    }

    const template = document.createElement('template');
    template.innerHTML = trimmed;
    const iframe = template.content.querySelector('iframe');
    if (!(iframe instanceof HTMLIFrameElement) || template.content.childElementCount !== 1) {
        return null;
    }

    return {
        src: iframe.getAttribute('src') ?? '',
        title: iframe.getAttribute('title') ?? '',
        width: iframe.getAttribute('width') ?? '',
        height: iframe.getAttribute('height') ?? ''
    };
}

function splitTableRow(line: string): string[] {
    if (!line.includes('|')) {
        return [];
    }

    let normalized = line.trim();
    if (normalized.startsWith('|')) {
        normalized = normalized.slice(1);
    }

    if (normalized.endsWith('|')) {
        normalized = normalized.slice(0, -1);
    }

    return normalized.split('|').map((cell) => cell.trim());
}

function parseTableSeparator(line: string): Alignment[] | null {
    const cells = splitTableRow(line);
    if (cells.length === 0) {
        return null;
    }

    const alignments: Alignment[] = [];
    for (const cell of cells) {
        const trimmed = cell.trim();
        if (trimmed.length < 3) {
            return null;
        }

        const leftAligned = trimmed.startsWith(':');
        const rightAligned = trimmed.endsWith(':');
        const dashSection = trimmed.replace(/^:/, '').replace(/:$/, '');
        if (dashSection.length < 3 || !/^-+$/.test(dashSection)) {
            return null;
        }

        alignments.push(leftAligned && rightAligned ? 'center'
            : rightAligned ? 'right'
            : leftAligned ? 'left'
            : '');
    }

    return alignments;
}

function isTableStart(lines: string[], index: number): boolean {
    if (index + 1 >= lines.length) {
        return false;
    }

    const headerCells = splitTableRow(lines[index]);
    const separatorCells = parseTableSeparator(lines[index + 1]);
    return headerCells.length > 0 && separatorCells !== null && separatorCells.length === headerCells.length;
}

function isBlockBoundary(lines: string[], index: number): boolean {
    const trimmed = lines[index].trimStart();
    return parseAlignmentMarker(trimmed) !== null
        || parseIframeMarker(trimmed) !== null
        || isHtmlTableStart(lines, index)
        || isCodeFenceLine(trimmed)
        || isBlockQuoteLine(trimmed)
        || isTableStart(lines, index)
        || /^(#{1,6})\s+(.+)$/s.test(trimmed)
        || parseListMarker(lines[index]) !== null;
}

function hasListContinuation(lines: string[], blankLineIndex: number, currentIndent: number): boolean {
    for (let lookahead = blankLineIndex + 1; lookahead < lines.length; lookahead++) {
        if (!lines[lookahead].trim()) {
            continue;
        }

        const listMarker = parseListMarker(lines[lookahead]);
        if (listMarker) {
            return listMarker.indent >= currentIndent;
        }

        return countLeadingSpaces(lines[lookahead]) > currentIndent;
    }

    return false;
}

function renderParagraph(lines: string[], startIndex: number): BlockRenderResult {
    const paragraphLines = [];
    let index = startIndex;

    while (index < lines.length) {
        const line = lines[index];
        if (!line.trim()) {
            break;
        }

        if (paragraphLines.length > 0 && isBlockBoundary(lines, index)) {
            break;
        }

        paragraphLines.push(line.trimEnd());
        index++;
    }

    if (paragraphLines.length === 0) {
        return { html: '', nextIndex: index };
    }

    return {
        html: `<p>${renderInlineMarkdown(paragraphLines.join('\n')).replace(/\n/g, '<br />')}</p>`,
        nextIndex: index
    };
}

function renderCodeBlock(lines: string[], startIndex: number): BlockRenderResult {
    const firstLine = lines[startIndex].trim();
    const language = firstLine.slice(3).trim();
    const codeLines = [];
    let index = startIndex + 1;

    while (index < lines.length && !lines[index].trimStart().startsWith('```')) {
        codeLines.push(lines[index]);
        index++;
    }

    if (index < lines.length) {
        index++;
    }

    const encodedLanguage = escapeHtml(language);
    const encodedCode = escapeHtml(codeLines.join('\n'));
    const languageAttribute = language ? ` data-language="${encodedLanguage}"` : '';

    return {
        html: `<pre${languageAttribute}><code${languageAttribute}>${encodedCode}</code></pre>`,
        nextIndex: index
    };
}

function renderAlignmentBlock(lines: string[], startIndex: number): BlockRenderResult {
    const alignment = parseAlignmentMarker(lines[startIndex]);
    if (!alignment) {
        return { html: '', nextIndex: startIndex + 1 };
    }

    const innerLines = [];
    let index = startIndex + 1;

    while (index < lines.length && !/^<\/div>\s*$/i.test(lines[index].trim())) {
        innerLines.push(lines[index]);
        index++;
    }

    if (index < lines.length) {
        index++;
    }

    return {
        html: `<div class="tnt-rich-text-editor-alignment" style="text-align:${escapeHtml(alignment)};">${renderBlocksFromLines(innerLines).html}</div>`,
        nextIndex: index
    };
}

function renderIframeBlock(lines: string[], startIndex: number): BlockRenderResult {
    const iframe = parseIframeMarker(lines[startIndex]);
    if (!iframe) {
        return { html: '', nextIndex: startIndex + 1 };
    }

    const iframeSource = normalizeSafeUrl(iframe.src, 'iframe');
    if (!iframeSource) {
        return { html: '', nextIndex: startIndex + 1 };
    }

    return {
        html: `<iframe src="${escapeHtml(iframeSource)}" title="${escapeHtml(iframe.title)}" width="${escapeHtml(iframe.width)}" height="${escapeHtml(iframe.height)}" loading="lazy"></iframe>`,
        nextIndex: startIndex + 1
    };
}

function normalizeTableCells<T>(cells: T[], targetCount: number, filler?: T): T[] {
    const normalized = cells.slice(0, targetCount);
    while (normalized.length < targetCount) {
        normalized.push(filler ?? ('' as T));
    }

    return normalized;
}

function isHtmlTableStart(lines: string[], index: number): boolean {
    return /^\s*<table(?:\s|>)/i.test(lines[index] ?? '');
}

function getTableBorderColor(tableElement: Maybe<HTMLTableElement>): string {
    if (!(tableElement instanceof HTMLTableElement)) {
        return '';
    }

    const dataBorderColor = normalizeTableBorderColor(tableElement.getAttribute('data-border-color') ?? '');
    if (dataBorderColor) {
        return dataBorderColor;
    }

    const styleAttribute = tableElement.getAttribute('style') ?? '';
    const styleMatch = styleAttribute.match(/--nt-rich-text-table-border-color:\s*([^;]+)/i);
    return normalizeTableBorderColor(styleMatch?.[1] ?? '');
}

function renderTableCellFromElement(cell: HTMLTableCellElement, tagName: 'th' | 'td'): string {
    const alignment = getTableCellAlignment(cell);
    const alignmentStyle = alignment ? ` style="text-align:${escapeHtml(alignment)};"` : '';
    return `<${tagName}${alignmentStyle}>${renderInlineMarkdown(serializeTableCell(cell))}</${tagName}>`;
}

function renderHtmlTableElement(tableElement: HTMLTableElement): string {
    const rows = getTableRows(tableElement);
    if (rows.length === 0) {
        return '<table></table>';
    }

    const headerCells = getTableCells(rows[0]);
    if (headerCells.length === 0) {
        return '<table></table>';
    }

    const borderColor = getTableBorderColor(tableElement);
    const headerHtml = headerCells
        .map((cell) => renderTableCellFromElement(cell, 'th'))
        .join('');

    const bodyHtml = rows.slice(1)
        .map((row) => getTableCells(row))
        .filter((cells) => cells.length > 0)
        .map((cells) => `<tr>${normalizeTableCells(cells, headerCells.length).map((cell) => cell instanceof Element
            ? renderTableCellFromElement(cell, cell.tagName === 'TH' ? 'th' : 'td')
            : '<td></td>').join('')}</tr>`)
        .join('');

    return `<table${buildTableStyleAttribute(borderColor)}><thead><tr>${headerHtml}</tr></thead>${bodyHtml ? `<tbody>${bodyHtml}</tbody>` : ''}</table>`;
}

function renderHtmlTableBlock(lines: string[], startIndex: number): BlockRenderResult {
    const blockLines = [];
    let index = startIndex;

    while (index < lines.length) {
        blockLines.push(lines[index]);
        if (/<\/table>\s*$/i.test(lines[index].trim())) {
            index++;
            break;
        }

        index++;
    }

    const template = document.createElement('template');
    template.innerHTML = blockLines.join('\n').trim();

    const tableElement = template.content.querySelector('table');
    if (!(tableElement instanceof HTMLTableElement)) {
        return {
            html: '',
            nextIndex: index
        };
    }

    return {
        html: renderHtmlTableElement(tableElement),
        nextIndex: index
    };
}

function renderTable(lines: string[], startIndex: number): BlockRenderResult {
    const headerCells = splitTableRow(lines[startIndex]);
    const alignments = parseTableSeparator(lines[startIndex + 1]) ?? headerCells.map(() => '');
    let index = startIndex + 2;
    const bodyRows = [];

    while (index < lines.length) {
        if (!lines[index].trim()) {
            break;
        }

        if (isBlockBoundary(lines, index) && !lines[index].includes('|')) {
            break;
        }

        const rowCells = splitTableRow(lines[index]);
        if (rowCells.length === 0) {
            break;
        }

        bodyRows.push(normalizeTableCells(rowCells, headerCells.length));
        index++;
    }

    const headerHtml = headerCells.map((cell, cellIndex) => {
        const alignment = alignments[cellIndex] ? ` style="text-align:${escapeHtml(alignments[cellIndex])};"` : '';
        return `<th${alignment}>${renderInlineMarkdown(cell)}</th>`;
    }).join('');

    const bodyHtml = bodyRows.length === 0
        ? ''
        : `<tbody>${bodyRows.map((row) => `<tr>${row.map((cell, cellIndex) => {
            const alignment = alignments[cellIndex] ? ` style="text-align:${escapeHtml(alignments[cellIndex])};"` : '';
            return `<td${alignment}>${renderInlineMarkdown(cell)}</td>`;
        }).join('')}</tr>`).join('')}</tbody>`;

    return {
        html: `<table><thead><tr>${headerHtml}</tr></thead>${bodyHtml}</table>`,
        nextIndex: index
    };
}

function renderBlockQuote(lines: string[], startIndex: number): BlockRenderResult {
    const quoteLines = [];
    let index = startIndex;

    while (index < lines.length) {
        const line = lines[index];
        if (!line.trim()) {
            if (index + 1 < lines.length && isBlockQuoteLine(lines[index + 1])) {
                quoteLines.push('');
                index++;
                continue;
            }

            break;
        }

        if (!isBlockQuoteLine(line)) {
            break;
        }

        quoteLines.push(stripBlockQuoteMarker(line));
        index++;
    }

    return {
        html: `<blockquote>${renderBlocksFromLines(quoteLines).html}</blockquote>`,
        nextIndex: index
    };
}

function renderList(lines: string[], startIndex: number): BlockRenderResult {
    const marker = parseListMarker(lines[startIndex]);
    if (!marker) {
        return { html: '', nextIndex: startIndex };
    }

    const { indent, ordered } = marker;
    const tagName = ordered ? 'ol' : 'ul';
    const parts = [`<${tagName}>`];
    let index = startIndex;

    while (index < lines.length) {
        if (!lines[index].trim()) {
            index++;
            continue;
        }

        const lineMarker = parseListMarker(lines[index]);
        if (!lineMarker || lineMarker.indent !== indent || lineMarker.ordered !== ordered) {
            break;
        }

        index++;
        parts.push('<li>');

        const inlineLines = [];
        if (lineMarker.content.trim()) {
            inlineLines.push(lineMarker.content.trimEnd());
        }

        while (index < lines.length) {
            if (!lines[index].trim()) {
                if (hasListContinuation(lines, index, indent)) {
                    index++;
                    continue;
                }

                break;
            }

            const nestedMarker = parseListMarker(lines[index]);
            if (nestedMarker) {
                if (nestedMarker.indent === indent) {
                    break;
                }

                if (nestedMarker.indent > indent) {
                    if (inlineLines.length > 0) {
                        parts.push(renderInlineMarkdown(inlineLines.join('\n')).replace(/\n/g, '<br />'));
                        inlineLines.length = 0;
                    }

                    const nestedList = renderList(lines, index);
                    parts.push(nestedList.html);
                    index = nestedList.nextIndex;
                    continue;
                }
            }

            const continuationIndent = countLeadingSpaces(lines[index]);
            if (continuationIndent <= indent) {
                break;
            }

            const continuationText = stripIndent(lines[index], Math.min(lines[index].length, indent + 2)).trimEnd();
            if (continuationText) {
                inlineLines.push(continuationText);
            }

            index++;
        }

        if (inlineLines.length > 0) {
            parts.push(renderInlineMarkdown(inlineLines.join('\n')).replace(/\n/g, '<br />'));
        }

        parts.push('</li>');
    }

    parts.push(`</${tagName}>`);
    return {
        html: parts.join(''),
        nextIndex: index
    };
}

function renderBlocksFromLines(lines: string[], startIndex = 0): BlockRenderResult {
    const parts = [];
    let index = startIndex;

    while (index < lines.length) {
        if (!lines[index].trim()) {
            index++;
            continue;
        }

        if (parseAlignmentMarker(lines[index])) {
            const block = renderAlignmentBlock(lines, index);
            parts.push(block.html);
            index = block.nextIndex;
            continue;
        }

        if (parseIframeMarker(lines[index])) {
            const block = renderIframeBlock(lines, index);
            parts.push(block.html);
            index = block.nextIndex;
            continue;
        }

        if (isCodeFenceLine(lines[index])) {
            const block = renderCodeBlock(lines, index);
            parts.push(block.html);
            index = block.nextIndex;
            continue;
        }

        if (isBlockQuoteLine(lines[index])) {
            const block = renderBlockQuote(lines, index);
            parts.push(block.html);
            index = block.nextIndex;
            continue;
        }

        if (parseListMarker(lines[index])) {
            const block = renderList(lines, index);
            parts.push(block.html);
            index = block.nextIndex;
            continue;
        }

        if (isTableStart(lines, index)) {
            const block = renderTable(lines, index);
            parts.push(block.html);
            index = block.nextIndex;
            continue;
        }

        if (isHtmlTableStart(lines, index)) {
            const block = renderHtmlTableBlock(lines, index);
            parts.push(block.html);
            index = block.nextIndex;
            continue;
        }

        const headingMatch = lines[index].trim().match(/^(#{1,6})\s+(.+)$/s);
        if (headingMatch) {
            const level = headingMatch[1].length;
            parts.push(`<h${level}>${renderInlineMarkdown(headingMatch[2].trim())}</h${level}>`);
            index++;
            continue;
        }

        const block = renderParagraph(lines, index);
        parts.push(block.html);
        index = block.nextIndex;
    }

    return { html: parts.join(''), nextIndex: index };
}

function markdownToHtml(markdown: string): string {
    const normalized = normalizeNewLines(markdown);
    if (!normalized.trim()) {
        return '';
    }

    return renderBlocksFromLines(normalized.split('\n')).html;
}

function escapeMarkdownText(value: unknown): string {
    return `${value ?? ''}`.replace(/([\\*_\[\]\(\)])/g, '\\$1');
}

function escapeMarkdownAttribute(value: unknown): string {
    return escapeMarkdownText(value).replace(/\n/g, ' ');
}

function normalizeAlignment(value: unknown): Alignment {
    const normalized = `${value ?? ''}`.trim().toLowerCase();
    if (normalized === 'start') {
        return 'left';
    }

    if (normalized === 'end') {
        return 'right';
    }

    return normalized === 'left'
        || normalized === 'center'
        || normalized === 'right'
        || normalized === 'justify'
        ? normalized
        : '';
}

function getElementAlignment(element: unknown): Alignment {
    if (!(element instanceof Element)) {
        return '';
    }

    const textAlign = element instanceof HTMLElement ? element.style.textAlign : '';
    return normalizeAlignment(textAlign || element.getAttribute?.('align') || '');
}

function hasRenderableBlockChildren(element: Maybe<Element>): boolean {
    return toArray(element?.children)
        .filter((child) => child instanceof Element)
        .some((child) => blockNodeTags.has(child.tagName) || child.tagName === 'IMG');
}

function wrapAlignedMarkdown(alignment: Alignment, markdown: string): string {
    const normalizedAlignment = normalizeAlignment(alignment);
    if (!normalizedAlignment || normalizedAlignment === 'left' || !markdown.trim()) {
        return markdown;
    }

    return `<div align="${normalizedAlignment}">\n${markdown}\n</div>`;
}

function serializeInline(node: Maybe<Node>): string {
    if (!node) {
        return '';
    }

    if (node.nodeType === Node.TEXT_NODE) {
        return escapeMarkdownText(node.textContent ?? '');
    }

    if (!(node instanceof Element)) {
        return '';
    }

    const tagName = node.tagName.toLowerCase();
    const content = toArray(node.childNodes).map(serializeInline).join('');

    switch (tagName) {
        case 'strong':
        case 'b':
            return `**${content}**`;
        case 'em':
        case 'i':
            return `*${content}*`;
        case 'u':
            return `<u>${content}</u>`;
        case 's':
        case 'strike':
        case 'del':
            return `~~${content}~~`;
        case 'span': {
            const textColor = getElementTextColor(node);
            return textColor ? `<span style="color:${escapeMarkdownAttribute(textColor)};">${content}</span>` : content;
        }
        case 'br':
            return '\n';
        case 'img':
            return buildImageMarkdown(getImageDetails(node));
        case 'a': {
            const href = normalizeSafeUrl(node.getAttribute('href') ?? '', 'link');
            if (!href) {
                return content;
            }

            return canUseMarkdownUrl(href)
                ? `[${content}](${href})`
                : `<a href="${href}">${content}</a>`;
        }
        default:
            return content;
    }
}

function indentMarkdownBlock(markdown: string, indent: number): string {
    const indentation = ' '.repeat(indent);
    return markdown
        .split('\n')
        .map((line) => line.length > 0 ? `${indentation}${line}` : line)
        .join('\n');
}

function serializeCodeBlock(block: HTMLElement): string {
    const codeElement = block.querySelector('code');
    const language = codeElement?.dataset.language ?? block.dataset.language ?? '';
    const codeText = normalizeNewLines(codeElement?.textContent ?? block.textContent ?? '').replace(/\n+$/g, '');
    return `\`\`\`${language}\n${codeText}\n\`\`\``;
}

function serializeBlockQuote(block: HTMLElement): string {
    const innerMarkdown = serializeContainerBlocks(block);
    return innerMarkdown
        .split('\n')
        .map((line) => line.length > 0 ? `> ${line}` : '>')
        .join('\n');
}

function serializeListItemContent(indent: number, marker: string, content: string): string {
    const lines = content.split('\n');
    const firstLine = lines.shift() ?? '';
    const result = [`${' '.repeat(indent)}${marker}${firstLine}`.trimEnd()];
    const continuationIndent = ' '.repeat(indent + 2);

    for (const line of lines) {
        result.push(`${continuationIndent}${line}`.trimEnd());
    }

    return result.join('\n');
}

function serializeList(listElement: HTMLOListElement | HTMLUListElement, indent = 0): string {
    const ordered = listElement.tagName.toLowerCase() === 'ol';
    const items: string[] = [];
    const listItems = toArray(listElement.children).filter((child) => child instanceof HTMLLIElement);

    listItems.forEach((item, itemIndex) => {
        const marker = ordered ? `${itemIndex + 1}. ` : '- ';
        let inlineContent = '';
        const nestedBlocks: string[] = [];

        for (const child of toArray(item.childNodes)) {
            if (child instanceof Element && blockNodeTags.has(child.tagName)) {
                if (child instanceof HTMLUListElement || child instanceof HTMLOListElement) {
                    nestedBlocks.push(serializeList(child, indent + 2));
                    continue;
                }

                nestedBlocks.push(indentMarkdownBlock(serializeBlock(child), indent + 2));
                continue;
            }

            inlineContent += serializeInline(child);
        }

        const normalizedInline = inlineContent.replace(/\u00a0/g, ' ').replace(/\n+$/g, '').trimEnd();
        const lines: string[] = [];

        if (normalizedInline.length > 0 || nestedBlocks.length === 0) {
            lines.push(serializeListItemContent(indent, marker, normalizedInline));
        } else {
            lines.push(`${' '.repeat(indent)}${marker}`.trimEnd());
        }

        lines.push(...nestedBlocks);
        items.push(lines.join('\n'));
    });

    return items.join('\n');
}

function getTableRows(tableElement: HTMLTableElement): HTMLTableRowElement[] {
    const rows: HTMLTableRowElement[] = [];
    const headRows = toArray(tableElement.querySelectorAll(':scope > thead > tr')).filter(isTableRowElement);
    const bodyRows = toArray(tableElement.querySelectorAll(':scope > tbody > tr')).filter(isTableRowElement);

    if (headRows.length > 0) {
        rows.push(headRows[0]);
    } else {
        const firstRow = tableElement.querySelector(':scope > tr');
        if (firstRow instanceof HTMLTableRowElement) {
            rows.push(firstRow);
        }
    }

    rows.push(...bodyRows);
    return rows;
}

function serializeTableCell(cell: HTMLTableCellElement): string {
    return toArray(cell.childNodes)
        .map(serializeInline)
        .join('')
        .replace(/\u00a0/g, ' ')
        .replace(/\s*\n+\s*/g, ' ')
        .trim();
}

function getTableCellAlignment(cell: HTMLTableCellElement): Alignment {
    const alignment = getElementAlignment(cell);
    if (alignment) {
        return alignment;
    }

    return getElementAlignment(cell.closest('table'));
}

function formatTableSeparator(alignment: Alignment): string {
    switch (alignment) {
        case 'center':
            return ':---:';
        case 'right':
            return '---:';
        case 'left':
            return ':---';
        default:
            return '---';
    }
}

function serializeHtmlTableCell(cell: HTMLTableCellElement, tagName: 'th' | 'td'): string {
    const alignment = getTableCellAlignment(cell);
    const alignmentStyle = alignment ? ` style="text-align:${escapeHtml(alignment)};"` : '';
    return `<${tagName}${alignmentStyle}>${serializeTableCell(cell)}</${tagName}>`;
}

function serializeHtmlTable(tableElement: HTMLTableElement, borderColor?: string): string {
    const rows = getTableRows(tableElement);
    if (rows.length === 0) {
        return '';
    }

    const headerCells = getTableCells(rows[0]);
    if (headerCells.length === 0) {
        return '';
    }

    const bodyRows = rows.slice(1)
        .map((row) => getTableCells(row))
        .filter((cells) => cells.length > 0)
        .map((cells) => `<tr>${normalizeTableCells(cells, headerCells.length).map((cell) => cell instanceof Element
            ? serializeHtmlTableCell(cell, cell.tagName === 'TH' ? 'th' : 'td')
            : '<td></td>').join('')}</tr>`);

    return [
        `<table${buildTableStyleAttribute(borderColor ?? '')}>`,
        '<thead>',
        `<tr>${headerCells.map((cell) => serializeHtmlTableCell(cell, 'th')).join('')}</tr>`,
        '</thead>',
        ...(bodyRows.length > 0 ? ['<tbody>', ...bodyRows, '</tbody>'] : []),
        '</table>'
    ].join('\n');
}

function serializeTable(tableElement: HTMLTableElement): string {
    const rows = getTableRows(tableElement);
    if (rows.length === 0) {
        return '';
    }

    const headerCells = getTableCells(rows[0]);
    if (headerCells.length === 0) {
        return '';
    }

    const borderColor = getTableBorderColor(tableElement);
    return serializeHtmlTable(tableElement, borderColor);
}

function serializeBlock(block: Maybe<Element>): string {
    if (!(block instanceof Element)) {
        return '';
    }

    const tagName = block.tagName.toLowerCase();
    const alignment = getElementAlignment(block);
    let markdown = '';

    if (tagName === 'div' && hasRenderableBlockChildren(block)) {
        markdown = serializeContainerBlocks(block);
    } else if (/^h[1-6]$/.test(tagName)) {
        const text = toArray(block.childNodes).map(serializeInline).join('');
        const normalized = text.replace(/\u00a0/g, ' ').replace(/\n{3,}/g, '\n\n').replace(/\n+$/g, '').trimEnd();
        markdown = `${'#'.repeat(Number.parseInt(tagName[1], 10))} ${normalized}`.trimEnd();
    } else if (block instanceof HTMLUListElement || block instanceof HTMLOListElement) {
        markdown = serializeList(block);
    } else if (tagName === 'blockquote' && block instanceof HTMLElement) {
        markdown = serializeBlockQuote(block);
    } else if (tagName === 'pre' && block instanceof HTMLElement) {
        markdown = serializeCodeBlock(block);
    } else if (block instanceof HTMLTableElement) {
        markdown = serializeTable(block);
    } else if (tagName === 'img') {
        markdown = serializeInline(block);
    } else if (tagName === 'iframe') {
        const iframeSource = normalizeSafeUrl(block.getAttribute('src') ?? '', 'iframe');
        markdown = iframeSource
            ? `<iframe src="${iframeSource}" title="${escapeMarkdownAttribute(block.getAttribute('title') ?? '')}" width="${block.getAttribute('width') ?? '100%'}" height="${block.getAttribute('height') ?? '315'}" loading="lazy"></iframe>`
            : '';
    } else {
        const text = toArray(block.childNodes).map(serializeInline).join('');
        markdown = text.replace(/\u00a0/g, ' ').replace(/\n{3,}/g, '\n\n').replace(/\n+$/g, '').trimEnd();
    }

    return wrapAlignedMarkdown(alignment, markdown);
}

function serializeContainerBlocks(container: Element | DocumentFragment): string {
    const blocks: string[] = [];
    let inlineBuffer = '';

    for (const child of toArray(container.childNodes)) {
        if (child instanceof Element && (blockNodeTags.has(child.tagName) || child.tagName === 'IMG')) {
            if (inlineBuffer.trim().length > 0) {
                blocks.push(inlineBuffer.replace(/\n+$/g, ''));
                inlineBuffer = '';
            }

            blocks.push(serializeBlock(child));
            continue;
        }

        inlineBuffer += serializeInline(child);
    }

    if (inlineBuffer.trim().length > 0) {
        blocks.push(inlineBuffer.replace(/\n+$/g, ''));
    }

    return blocks
        .map((block) => block.replace(/\u00a0/g, ' ').trimEnd())
        .filter((block) => block.length > 0)
        .join('\n\n')
        .replace(/\n{3,}/g, '\n\n')
        .trim();
}

function surfaceToMarkdown(surface: HTMLElement): string {
    if (!surface) {
        return '';
    }

    return serializeContainerBlocks(surface);
}

function updateSourceValue(element: HTMLElement, markdown: string): void {
    const sourceValueElement = getSourceValueElement(element);
    if (sourceValueElement) {
        sourceValueElement.textContent = markdown;
    }

    const hiddenInput = getHiddenInput(element);
    if (hiddenInput) {
        hiddenInput.value = markdown;
    }
}

function updateEmptyState(surface: Maybe<HTMLElement>): void {
    if (!surface) {
        return;
    }

    const hasContent = Boolean(surface.textContent?.trim().length)
        || Boolean(surface.querySelector('strong, b, em, i, u, br, p, div, ul, ol, li, blockquote, pre, table, img, iframe'));
    surface.dataset.empty = hasContent ? 'false' : 'true';
}

function updateLength(element: HTMLElement, markdown: string): void {
    const lengthElement = getLengthElement(element);
    if (!lengthElement) {
        return;
    }

    const maxLength = getSurface(element)?.dataset.maxlength;
    if (!maxLength) {
        return;
    }

    lengthElement.textContent = `${markdown.length}/${maxLength}`;
}

function applyMarkdownToSurface(element: HTMLElement, markdown: string): void {
    const surface = getSurface(element);
    const state = editorState.get(element);
    if (!surface) {
        return;
    }

    const html = markdownToHtml(markdown);
    surface.innerHTML = html;
    updateEmptyState(surface);
    updateLength(element, markdown);
    updateSourceValue(element, markdown);
    if (state) {
        state.lastMarkdown = markdown;
        state.lastHtml = html;
    }
    updateToolbarState(element);
}

function scheduleSyncValueFromSurface(element: HTMLElement, notifyDotNet: boolean): void {
    const state = editorState.get(element);
    if (!state) {
        return;
    }

    clearPendingSync(state);
    state.pendingSyncFrameId = window.requestAnimationFrame(() => {
        state.pendingSyncFrameId = null;
        syncValueFromSurface(element, notifyDotNet);
    });
}

function syncValueFromSurface(element: HTMLElement, notifyDotNet: boolean): void {
    const state = editorState.get(element);
    const surface = getSurface(element);
    if (!state || state.isDisposed || !surface) {
        return;
    }

    const markdown = surfaceToMarkdown(surface);
    const html = surface.innerHTML;
    const didValueChange = markdown !== state.lastMarkdown || html !== state.lastHtml;
    state.lastMarkdown = markdown;
    state.lastHtml = html;
    updateEmptyState(surface);
    updateLength(element, markdown);
    updateSourceValue(element, markdown);
    updateToolbarState(element);

    if (notifyDotNet && didValueChange && state.dotNetRef) {
        invokeDotNetVoid(state.dotNetRef, 'UpdateValueFromJs', markdown, html);
    }
}

function commitValue(element: HTMLElement): void {
    const state = editorState.get(element);
    const surface = getSurface(element);
    if (!state || state.isDisposed || !surface) {
        return;
    }

    clearPendingSync(state);
    const markdown = surfaceToMarkdown(surface);
    const html = surface.innerHTML;
    state.lastMarkdown = markdown;
    state.lastHtml = html;
    updateEmptyState(surface);
    updateLength(element, markdown);
    updateSourceValue(element, markdown);
    updateToolbarState(element);

    invokeDotNetVoid(state.dotNetRef, 'CommitValueFromJs', markdown, html);
}

function updateToolbarState(element: HTMLElement): void {
    const state = editorState.get(element);
    if (!state) {
        return;
    }

    const surface = getSurface(element);
    const focusedToolCommand = getFocusedToolCommand(element);
    const selectionElement = getSelectionElement(surface);
    const hasLocalSelectionContext = Boolean(selectionElement);
    const toolbarButtons = state.toolbarButtons;

    if (!hasLocalSelectionContext && !focusedToolCommand) {
        for (const button of toolbarButtons) {
            const command = button.dataset.command ?? '';
            if (!command || getRichTextEditorTool(command)) {
                continue;
            }

            button.setAttribute('aria-pressed', 'false');
        }

        forEachRegisteredToolContext(element, state, (tool, context) => {
            tool.syncState?.(context);
        });

        return;
    }

    const blockElement = getCurrentBlockElement(surface);
    const activeBlockTag = blockElement?.tagName?.toLowerCase?.() ?? 'p';
    const activeAlignment = getCurrentAlignment(surface, blockElement);
    const isBold = hasAncestorTag(surface, 'strong, b');
    const isItalic = hasAncestorTag(surface, 'em, i');
    const isUnderline = hasAncestorTag(surface, 'u');
    const isStrikethrough = hasAncestorTag(surface, 's, strike, del');
    const isUnorderedList = hasAncestorTag(surface, 'ul');
    const isOrderedList = hasAncestorTag(surface, 'ol');
    const isBlockQuote = hasAncestorTag(surface, 'blockquote');
    const isCodeBlock = hasAncestorTag(surface, 'pre');

    for (const button of toolbarButtons) {
        const command = button.dataset.command ?? '';
        if (command && getRichTextEditorTool(command)) {
            continue;
        }

        let isSelected = false;

        if (command === 'paragraph') {
            isSelected = activeBlockTag === 'p' || activeBlockTag === 'div';
        } else if (command === 'heading') {
            isSelected = activeBlockTag === `h${button.dataset.value ?? '1'}`;
        } else if (command === 'alignLeft') {
            isSelected = activeAlignment === 'left';
        } else if (command === 'alignCenter') {
            isSelected = activeAlignment === 'center';
        } else if (command === 'alignRight') {
            isSelected = activeAlignment === 'right';
        } else if (command === 'alignJustify') {
            isSelected = activeAlignment === 'justify';
        } else if (command === 'unorderedList') {
            isSelected = isUnorderedList;
        } else if (command === 'orderedList') {
            isSelected = isOrderedList;
        } else if (command === 'blockquote') {
            isSelected = isBlockQuote;
        } else if (command === 'codeBlock') {
            isSelected = isCodeBlock;
        } else if (command === 'bold') {
            isSelected = isBold;
        } else if (command === 'italic') {
            isSelected = isItalic;
        } else if (command === 'underline') {
            isSelected = isUnderline;
        } else if (command === 'strikeThrough') {
            isSelected = isStrikethrough;
        }

        button.setAttribute('aria-pressed', isSelected ? 'true' : 'false');
    }

    forEachRegisteredToolContext(element, state, (tool, context) => {
        tool.syncState?.(context);
    });
}

function getFocusedToolCommand(element: HTMLElement): string | null {
    const activeElement = document.activeElement;
    if (!(activeElement instanceof Element) || !element.contains(activeElement)) {
        return null;
    }

    return activeElement.closest('[data-tool-command]')?.getAttribute('data-tool-command') ?? null;
}

function collectToolCommands(element: Maybe<ParentNode>, toolbarButtons: HTMLButtonElement[] = getToolbarButtons(element)): string[] {
    const commands = new Set<string>();
    for (const button of toolbarButtons) {
        const command = button.dataset.command?.trim();
        if (command) {
            commands.add(command);
        }
    }

    for (const panel of toArray(element?.querySelectorAll?.('[data-tool-command]')).filter(isElement)) {
        const command = panel.getAttribute('data-tool-command')?.trim();
        if (command) {
            commands.add(command);
        }
    }

    return Array.from(commands);
}

function forEachRegisteredToolContext(
    element: HTMLElement,
    state: EditorState,
    callback: (tool: RegisteredTool, context: RegisteredToolContext) => void
): void {
    for (const command of state.toolCommands) {
        const registeredTool = getRichTextEditorTool(command);
        if (!registeredTool) {
            continue;
        }

        callback(registeredTool, createRichTextEditorToolContext(element, state, toolHost, registeredTool));
    }
}

function closeOtherTools(element: HTMLElement, state: EditorState, exceptCommand: string | null = null): void {
    forEachRegisteredToolContext(element, state, (tool, context) => {
        if (!tool || tool.command === exceptCommand) {
            return;
        }

        tool.close?.(context, { preserveSelection: true });
    });
}

function saveSelectionRange(surface: HTMLElement, state: RichTextEditorToolEditorState): void {
    const selection = window.getSelection?.();
    if (!selection || selection.rangeCount === 0) {
        state.selectionRange = null;
        return;
    }

    const range = selection.getRangeAt(0);
    const commonAncestor = range.commonAncestorContainer;
    if (!surface.contains(commonAncestor)) {
        state.selectionRange = null;
        return;
    }

    state.selectionRange = range.cloneRange();
}

function restoreSelectionRange(surface: HTMLElement, state: Maybe<RichTextEditorToolEditorState>): void {
    if (!state?.selectionRange) {
        placeCaretAtEnd(surface);
        return;
    }

    const { selectionRange } = state;
    const startContainer = selectionRange.startContainer;
    const endContainer = selectionRange.endContainer;
    const isRangeAttached = startContainer.isConnected
        && endContainer.isConnected
        && surface.contains(startContainer)
        && surface.contains(endContainer);

    if (!isRangeAttached) {
        state.selectionRange = null;
        placeCaretAtEnd(surface);
        return;
    }

    surface.focus();

    const selection = window.getSelection?.();
    if (!selection) {
        return;
    }

    selection.removeAllRanges();
    selection.addRange(selectionRange);
}

const toolHost: RichTextEditorToolHost = {
    closeOtherTools,
    getFocusedToolCommand,
    getSelectionElement,
    getSurface,
    getToolPanel: (element: Maybe<ParentNode>, command: string) =>
        qs<HTMLElement>(element, `[data-tool-command="${command}"]`),
    getToolbarButton: (element: Maybe<ParentNode>, command: string, value?: string) => {
        const matchingButtons = getToolbarButtons(element).filter((button) => button.dataset.command === command);
        if (typeof value === 'undefined') {
            return matchingButtons[0] ?? null;
        }

        return matchingButtons.find((button) => button.dataset.value === value) ?? null;
    },
    restoreSelectionRange,
    saveSelectionRange,
    setToolPanelOpen: (element: HTMLElement, command: string, isOpen: boolean) => {
        const panel = qs<HTMLElement>(element, `[data-tool-command="${command}"]`);
        if (!panel) {
            return;
        }

        panel.hidden = !isOpen;
        panel.setAttribute('aria-hidden', isOpen ? 'false' : 'true');
    },
    setToolbarButtonPressed: (element: HTMLElement, command: string, isPressed: boolean, value?: string) => {
        const button = toolHost.getToolbarButton(element, command, value);
        button?.setAttribute('aria-pressed', isPressed ? 'true' : 'false');
    },
    syncValueFromSurface,
    updateToolbarState
};

function executeEditorCommand(element: HTMLElement, command: string | undefined, value?: string): boolean {
    const surface = getSurface(element);
    const state = editorState.get(element);
    if (!surface || !command) {
        return false;
    }

    if (!isEditorEditable(element)) {
        updateToolbarState(element);
        return false;
    }

    surface.focus();
    let didChange = true;
    const tool = state ? getRichTextEditorTool(command) : null;
    if (tool && state) {
        const toolContext = createRichTextEditorToolContext(element, state, toolHost, tool);
        didChange = tool.execute?.(toolContext, value) ?? false;
        if (!didChange) {
            updateToolbarState(element);
            return false;
        }

        syncValueFromSurface(element, true);
        return true;
    }

    switch (command) {
        case 'undo':
            document.execCommand('undo', false);
            break;
        case 'redo':
            document.execCommand('redo', false);
            break;
        case 'heading':
            document.execCommand('formatBlock', false, `H${value ?? '1'}`);
            break;
        case 'paragraph':
            document.execCommand('formatBlock', false, 'P');
            break;
        case 'alignLeft':
            document.execCommand('justifyLeft', false);
            break;
        case 'alignCenter':
            document.execCommand('justifyCenter', false);
            break;
        case 'alignRight':
            document.execCommand('justifyRight', false);
            break;
        case 'alignJustify':
            document.execCommand('justifyFull', false);
            break;
        case 'unorderedList':
            document.execCommand('insertUnorderedList', false);
            break;
        case 'orderedList':
            document.execCommand('insertOrderedList', false);
            break;
        case 'blockquote':
            if (hasAncestorTag(surface, 'blockquote')) {
                document.execCommand('outdent', false);
            } else {
                document.execCommand('formatBlock', false, 'BLOCKQUOTE');
            }
            break;
        case 'codeBlock':
            if (hasAncestorTag(surface, 'pre')) {
                document.execCommand('formatBlock', false, 'P');
            } else {
                document.execCommand('formatBlock', false, 'PRE');
            }
            break;
        default:
            document.execCommand(command, false);
            break;
    }

    if (!didChange) {
        updateToolbarState(element);
        return false;
    }

    syncValueFromSurface(element, true);
    updateToolbarState(element);
    return true;
}

function handleToolbarCommand(event: MouseEvent): void {
    const button = event.currentTarget instanceof HTMLButtonElement ? event.currentTarget : null;
    const element = button?.closest?.('nt-rich-text-editor');
    if (!(element instanceof HTMLElement)) {
        return;
    }

    const surface = getSurface(element);
    if (!surface || !button || button.disabled) {
        return;
    }

    event.preventDefault();
    executeEditorCommand(element, button.dataset.command, button.dataset.value);
}

function eventMatchesShortcut(event: KeyboardEvent, shortcutExpression: string): boolean {
    const normalizedShortcut = shortcutExpression.trim();
    if (!normalizedShortcut) {
        return false;
    }

    const shortcutParts = normalizedShortcut.split('+').map((part) => part.trim()).filter(Boolean);
    const keyPart = shortcutParts[shortcutParts.length - 1]?.toLowerCase() ?? '';
    if (!keyPart) {
        return false;
    }

    const requiresPrimaryModifier = shortcutParts.some((part) => part === 'Control' || part === 'Meta');
    const requiresAlt = shortcutParts.includes('Alt');
    const requiresShift = shortcutParts.includes('Shift');
    const hasPrimaryModifier = event.ctrlKey || event.metaKey;

    return hasPrimaryModifier === requiresPrimaryModifier
        && event.altKey === requiresAlt
        && event.shiftKey === requiresShift
        && event.key.toLowerCase() === keyPart;
}

function tryHandleShortcut(element: HTMLElement, event: KeyboardEvent): boolean {
    for (const button of getToolbarButtons(element)) {
        if (button.disabled) {
            continue;
        }

        const shortcutExpression = button.getAttribute('aria-keyshortcuts');
        if (!shortcutExpression) {
            continue;
        }

        const shortcuts = shortcutExpression.split(/\s+/).map((value) => value.trim()).filter(Boolean);
        if (!shortcuts.some((shortcut) => eventMatchesShortcut(event, shortcut))) {
            continue;
        }

        event.preventDefault();
        executeEditorCommand(element, button.dataset.command, button.dataset.value);
        return true;
    }

    return false;
}

function handleEditorKeyDown(element: HTMLElement, event: KeyboardEvent): void {
    const surface = getSurface(element);
    if (!surface) {
        return;
    }

    if (!isEditorEditable(element)) {
        return;
    }

    if (tryHandleShortcut(element, event)) {
        return;
    }

    if (event.key === 'Tab' && hasAncestorTag(surface, 'li')) {
        event.preventDefault();
        document.execCommand(event.shiftKey ? 'outdent' : 'indent', false);
        const shouldNotify = element.dataset.bindOnInput === 'true';
        syncValueFromSurface(element, shouldNotify);
        updateToolbarState(element);
    }
}

function bindRegisteredToolControls(element: HTMLElement, state: EditorState): void {
    forEachRegisteredToolContext(element, state, (tool, context) => {
        tool.bind(context);
    });
}

function unbindRegisteredToolControls(element: HTMLElement, state: EditorState): void {
    forEachRegisteredToolContext(element, state, (tool, context) => {
        tool.unbind(context);
    });
}

function setRegisteredToolDisabledState(element: HTMLElement, state: EditorState, disabled: boolean): void {
    forEachRegisteredToolContext(element, state, (tool, context) => {
        tool.setDisabled?.(context, disabled);
    });
}

function ensureState(element: Maybe<HTMLElement>, dotNetRef: DotNetEditorRef | null): EditorState | null {
    if (!element) {
        return null;
    }

    const existingState = editorState.get(element);
    if (existingState) {
        existingState.dotNetRef = dotNetRef ?? existingState.dotNetRef;
        return existingState;
    }

    const surface = getSurface(element);
    if (!surface) {
        return null;
    }

    const state: EditorState = {
        dotNetRef,
        isDisposed: false,
        blurTimeoutId: null,
        pendingSyncFrameId: null,
        requiresInitialRender: true,
        toolbarButtons: [],
        toolCommands: [],
        selectionRange: null,
        toolStates: new Map<string, unknown>(),
        lastMarkdown: normalizeNewLines(getSourceValueElement(element)?.textContent ?? ''),
        lastHtml: '',
        onInput: () => {
            const shouldNotify = element.dataset.bindOnInput === 'true';
            if (shouldNotify) {
                syncValueFromSurface(element, true);
                return;
            }

            scheduleSyncValueFromSurface(element, false);
        },
        onFocus: () => {
            clearBlurTimeout(state);
            setActiveEditor(element);
            updateToolbarState(element);
        },
        onFocusIn: () => {
            setActiveEditor(element);
            updateToolbarState(element);
        },
        onBlur: () => {
            clearBlurTimeout(state);
            state.blurTimeoutId = window.setTimeout(() => {
                state.blurTimeoutId = null;

                if (state.isDisposed || !element.isConnected) {
                    return;
                }

                if (element.contains(document.activeElement)) {
                    return;
                }

                clearActiveEditor(element);
                updateToolbarState(element);
                commitValue(element);
            }, 0);
        },
        onKeyUp: () => {
            updateToolbarState(element);
        },
        onMouseUp: () => {
            updateToolbarState(element);
        },
        onKeyDown: (event: KeyboardEvent) => {
            handleEditorKeyDown(element, event);
        },
        onToolbarMouseDown: (event: MouseEvent) => {
            event.preventDefault();
        }
    };

    surface.addEventListener('input', state.onInput);
    surface.addEventListener('focus', state.onFocus);
    surface.addEventListener('blur', state.onBlur);
    surface.addEventListener('keyup', state.onKeyUp);
    surface.addEventListener('mouseup', state.onMouseUp);
    surface.addEventListener('keydown', state.onKeyDown);
    element.addEventListener('focusin', state.onFocusIn);

    state.toolCommands = collectToolCommands(element, state.toolbarButtons);
    bindToolbarButtons(element, state);
    bindRegisteredToolControls(element, state);
    editorElements.add(element);
    editorState.set(element, state);
    return state;
}

function synchronizeElement(element: Maybe<HTMLElement>, dotNetRef: DotNetEditorRef | null): void {
    if (!element) {
        return;
    }

    const state = ensureState(element, dotNetRef);
    if (!state) {
        return;
    }

    state.isDisposed = false;
    state.dotNetRef = dotNetRef ?? state.dotNetRef;

    const markdown = normalizeNewLines(getSourceValueElement(element)?.textContent ?? '');
    if (state.requiresInitialRender || markdown !== state.lastMarkdown) {
        applyMarkdownToSurface(element, markdown);
        state.lastMarkdown = markdown;
        state.requiresInitialRender = false;
    } else {
        updateLength(element, markdown);
        updateEmptyState(getSurface(element));
        updateSourceValue(element, markdown);
    }

    const isEditable = element.dataset.editable === 'true';
    const surface = getSurface(element);
    if (surface) {
        surface.setAttribute('contenteditable', isEditable ? 'true' : 'false');
    }

    bindToolbarButtons(element, state);
    state.toolCommands = collectToolCommands(element, state.toolbarButtons);
    unbindRegisteredToolControls(element, state);
    bindRegisteredToolControls(element, state);

    for (const button of state.toolbarButtons) {
        button.disabled = !isEditable;
    }

    setRegisteredToolDisabledState(element, state, !isEditable);

    if (!isEditable) {
        closeOtherTools(element, state);
    }

    const currentHtml = surface?.innerHTML ?? '';
    if (currentHtml !== state.lastHtml) {
        state.lastHtml = currentHtml;
        invokeDotNetVoid(state.dotNetRef, 'UpdateMarkupValueFromJs', currentHtml);
    }
    updateToolbarState(element);
}

function initializeAllEditors(): void {
    for (const element of toArray(document.querySelectorAll('nt-rich-text-editor')).filter(isHtmlElement)) {
        synchronizeElement(element, null);
    }
}

function registerSelectionChangeHandler(): void {
    if (selectionChangeRegistered) {
        return;
    }

    selectionChangeRegistered = true;
    document.addEventListener('selectionchange', () => {
        const selectionAnchorNode = window.getSelection?.()?.anchorNode ?? null;
        const selectionEditor = selectionAnchorNode instanceof Element
            ? selectionAnchorNode.closest('nt-rich-text-editor')
            : selectionAnchorNode?.parentElement?.closest?.('nt-rich-text-editor') ?? null;
        const editorToUpdate = selectionEditor instanceof HTMLElement
            ? selectionEditor
            : activeEditor;

        if (editorToUpdate && editorToUpdate.isConnected) {
            updateToolbarState(editorToUpdate);
        }
    });
}

export function focusEditor(element: Maybe<HTMLElement>): void {
    getSurface(element)?.focus();
}

export function onLoad(element: Maybe<HTMLElement>, dotNetRef: DotNetEditorRef | null): void {
    registerSelectionChangeHandler();

    if (!element) {
        initializeAllEditors();
        return;
    }

    synchronizeElement(element, dotNetRef);
}

export function onUpdate(element: Maybe<HTMLElement>, dotNetRef: DotNetEditorRef | null): void {
    if (!element) {
        initializeAllEditors();
        return;
    }

    synchronizeElement(element, dotNetRef);
}

export function onDispose(element: Maybe<HTMLElement>): void {
    if (!element) {
        return;
    }

    const state = editorState.get(element);
    const surface = getSurface(element);
    if (state) {
        state.isDisposed = true;
        state.dotNetRef = null;
        clearBlurTimeout(state);
        clearPendingSync(state);
    }

    if (state && surface) {
        surface.removeEventListener('input', state.onInput);
        surface.removeEventListener('focus', state.onFocus);
        surface.removeEventListener('blur', state.onBlur);
        surface.removeEventListener('keyup', state.onKeyUp);
        surface.removeEventListener('mouseup', state.onMouseUp);
        surface.removeEventListener('keydown', state.onKeyDown);
    }

    if (state) {
        element.removeEventListener('focusin', state.onFocusIn);
    }

    unbindToolbarButtons(state?.toolbarButtons ?? getToolbarButtons(element), state);
    if (state) {
        unbindRegisteredToolControls(element, state);
    }

    clearActiveEditor(element);
    editorElements.delete(element);
    editorState.delete(element);
}

export const __testHooks = {
    blockNodeTags,
    blockSelector,
    defaultTableBorderColor,
    defaultTextColor,
    editorElements,
    editorState,
    toolHost,
    toArray,
    qs,
    isElement,
    isHtmlButtonElement,
    isHtmlElement,
    isHtmlInputElement,
    isHtmlSpanElement,
    isTableCellElement,
    isTableRowElement,
    getTableCells,
    clearBlurTimeout,
    clearPendingSync,
    placeCaretAtEnd,
    getSurface,
    getSourceValueElement,
    getHiddenInput,
    getLengthElement,
    getToolbarButtons,
    isEditorEditable,
    setActiveEditor,
    clearActiveEditor,
    invokeDotNetVoid,
    extractUrlScheme,
    normalizeSafeUrl,
    canUseMarkdownUrl,
    unbindToolbarButtons,
    bindToolbarButtons,
    getChildNodeIndex,
    getBoundaryAdjacentElement,
    getSelectionElement,
    getCurrentBlockElement,
    getCurrentAlignment,
    hasAncestorTag,
    getElementTextColor,
    normalizeHexColor,
    convertRgbChannelToHex,
    normalizeTextColorValue,
    normalizeNewLines,
    countLeadingSpaces,
    stripIndent,
    escapeHtml,
    createInlineToken,
    normalizeImageDimension,
    normalizeTableBorderColor,
    buildTableStyleAttribute,
    buildImageHtml,
    buildImageMarkdown,
    renderMarkdownImage,
    renderHtmlImage,
    getImageDetails,
    renderHtmlAnchor,
    renderLink,
    renderTextColor,
    renderInlineMarkdown,
    isCodeFenceLine,
    isBlockQuoteLine,
    stripBlockQuoteMarker,
    parseListMarker,
    parseAlignmentMarker,
    parseIframeMarker,
    splitTableRow,
    parseTableSeparator,
    isTableStart,
    isBlockBoundary,
    hasListContinuation,
    renderParagraph,
    renderCodeBlock,
    renderAlignmentBlock,
    renderIframeBlock,
    normalizeTableCells,
    isHtmlTableStart,
    getTableBorderColor,
    renderTableCellFromElement,
    renderHtmlTableElement,
    renderHtmlTableBlock,
    renderTable,
    renderBlockQuote,
    renderList,
    renderBlocksFromLines,
    markdownToHtml,
    escapeMarkdownText,
    escapeMarkdownAttribute,
    normalizeAlignment,
    getElementAlignment,
    hasRenderableBlockChildren,
    wrapAlignedMarkdown,
    serializeInline,
    indentMarkdownBlock,
    serializeCodeBlock,
    serializeBlockQuote,
    serializeListItemContent,
    serializeList,
    getTableRows,
    serializeTableCell,
    getTableCellAlignment,
    formatTableSeparator,
    serializeHtmlTableCell,
    serializeHtmlTable,
    serializeTable,
    serializeBlock,
    serializeContainerBlocks,
    surfaceToMarkdown,
    updateSourceValue,
    updateEmptyState,
    updateLength,
    applyMarkdownToSurface,
    scheduleSyncValueFromSurface,
    syncValueFromSurface,
    commitValue,
    updateToolbarState,
    getFocusedToolCommand,
    collectToolCommands,
    forEachRegisteredToolContext,
    closeOtherTools,
    saveSelectionRange,
    restoreSelectionRange,
    executeEditorCommand,
    handleToolbarCommand,
    eventMatchesShortcut,
    tryHandleShortcut,
    handleEditorKeyDown,
    bindRegisteredToolControls,
    unbindRegisteredToolControls,
    setRegisteredToolDisabledState,
    ensureState,
    synchronizeElement,
    initializeAllEditors,
    registerSelectionChangeHandler
};
