import {
    registerRichTextEditorTool
} from '../../wwwroot/NTComponents.lib.module.js';
import type {
    InlineToolCloseOptions,
    Maybe,
    RichTextEditorToolContext
} from '../Core/NTRichTextEditorToolRegistry.js';

interface ExistingTableContent {
    headers: string[];
    rows: string[][];
}

interface TableToolState {
    tableEditorInputs: HTMLInputElement[];
    tableApplyButton: HTMLButtonElement | null;
    tableCancelButton: HTMLButtonElement | null;
    tableTarget: HTMLTableElement | null;
    onTableApply?: (event: MouseEvent) => void;
    onTableCancel?: (event: MouseEvent) => void;
    onTableEditorKeyDown?: (event: KeyboardEvent) => void;
}

const defaultTableBorderColor = '#94a3b8';

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

function toArray<T>(values: Maybe<Iterable<T> | ArrayLike<T>>): T[] {
    return values ? Array.from(values) : [];
}

function escapeHtml(value: string): string {
    return value
        .replaceAll('&', '&amp;')
        .replaceAll('<', '&lt;')
        .replaceAll('>', '&gt;')
        .replaceAll('"', '&quot;')
        .replaceAll('\'', '&#39;');
}

function isTableCellElement(value: unknown): value is HTMLTableCellElement {
    return value instanceof HTMLTableCellElement;
}

function getTableRows(tableElement: HTMLTableElement): HTMLTableRowElement[] {
    return toArray(tableElement.rows);
}

function getTableCells(row: Maybe<HTMLTableRowElement>): HTMLTableCellElement[] {
    return toArray(row?.children).filter(isTableCellElement);
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

function getTableBorderColor(tableElement: Maybe<HTMLTableElement>): string {
    return tableElement?.dataset.borderColor
        ?? tableElement?.style.getPropertyValue('--nt-rich-text-table-border-color').trim()
        ?? '';
}

function getTableEditor(element: Maybe<ParentNode>): HTMLElement | null {
    return qs<HTMLElement>(element, '[data-tool-command="table"]');
}

function getTableColumnsInput(element: Maybe<ParentNode>): HTMLInputElement | null {
    return qs<HTMLInputElement>(element, '[data-role="table-columns"]');
}

function getTableRowsInput(element: Maybe<ParentNode>): HTMLInputElement | null {
    return qs<HTMLInputElement>(element, '[data-role="table-rows"]');
}

function getTableBorderColorInput(element: Maybe<ParentNode>): HTMLInputElement | null {
    return qs<HTMLInputElement>(element, '[data-role="table-border-color"]');
}

function getTableApplyButton(element: Maybe<ParentNode>): HTMLButtonElement | null {
    return qs<HTMLButtonElement>(element, '[data-role="table-apply"]');
}

function getTableCancelButton(element: Maybe<ParentNode>): HTMLButtonElement | null {
    return qs<HTMLButtonElement>(element, '[data-role="table-cancel"]');
}

function clampTableColumns(value: number): number {
    return Math.min(Math.max(value, 1), 8);
}

function clampTableRows(value: number): number {
    return Math.min(Math.max(value, 1), 12);
}

function getTableEditorDetails(tableElement: Maybe<HTMLTableElement>): { columns: number; rows: number; borderColor: string } {
    if (!(tableElement instanceof HTMLTableElement)) {
        return {
            columns: 3,
            rows: 2,
            borderColor: defaultTableBorderColor
        };
    }

    const rows = getTableRows(tableElement);
    const headerCells = getTableCells(rows[0]);
    const bodyRows = rows.slice(1)
        .map((row) => getTableCells(row))
        .filter((cells) => cells.length > 0);

    return {
        columns: clampTableColumns(headerCells.length || 3),
        rows: clampTableRows(bodyRows.length || 2),
        borderColor: getTableBorderColor(tableElement) || defaultTableBorderColor
    };
}

function getExistingTableContent(tableElement: Maybe<HTMLTableElement>): ExistingTableContent {
    if (!(tableElement instanceof HTMLTableElement)) {
        return {
            headers: [],
            rows: []
        };
    }

    const rows = getTableRows(tableElement);
    const headerCells = getTableCells(rows[0]);
    const bodyRows = rows.slice(1)
        .map((row) => getTableCells(row))
        .filter((cells) => cells.length > 0);

    return {
        headers: headerCells.map((cell) => cell.innerHTML.trim()),
        rows: bodyRows.map((cells) => cells.map((cell) => cell.innerHTML.trim()))
    };
}

function getTableCellMarkup(content: string | undefined, fallbackText: string, tagName: 'th' | 'td'): string {
    const normalizedContent = `${content ?? ''}`.trim();
    if (normalizedContent.length > 0) {
        return `<${tagName}>${normalizedContent}</${tagName}>`;
    }

    return `<${tagName}>${escapeHtml(fallbackText)}</${tagName}>`;
}

function buildTableHtml(details: { columns: number; rows: number; borderColor: string; existingContent?: ExistingTableContent | null }): string {
    const { columns, rows, borderColor, existingContent = null } = details;
    const normalizedBorderColor = normalizeTableBorderColor(borderColor) || defaultTableBorderColor;
    const headerMarkup = Array.from({ length: columns }, (_, columnIndex) => getTableCellMarkup(
        existingContent?.headers?.[columnIndex],
        `Header ${columnIndex + 1}`,
        'th'
    )).join('');

    const bodyMarkup = Array.from({ length: rows }, (_, rowIndex) => `<tr>${Array.from({ length: columns }, (_, columnIndex) => getTableCellMarkup(
        existingContent?.rows?.[rowIndex]?.[columnIndex],
        `Cell ${rowIndex + 1}-${columnIndex + 1}`,
        'td'
    )).join('')}</tr>`).join('');

    return `<table${buildTableStyleAttribute(normalizedBorderColor)}><thead><tr>${headerMarkup}</tr></thead><tbody>${bodyMarkup}</tbody></table>`;
}

function initializeTableToolStateHandlers(context: RichTextEditorToolContext<TableToolState>): void {
    const { element, host, toolState } = context;
    if (toolState.onTableApply) {
        return;
    }

    toolState.onTableApply = (event: MouseEvent) => {
        event.preventDefault();
        applyTableEditor(context);
    };

    toolState.onTableCancel = (event: MouseEvent) => {
        event.preventDefault();
        closeTableEditor(context, { focusSurface: true });
        host.updateToolbarState(element);
    };

    toolState.onTableEditorKeyDown = (event: KeyboardEvent) => {
        if (event.key === 'Escape') {
            event.preventDefault();
            closeTableEditor(context, { focusSurface: true });
            host.updateToolbarState(element);
            return;
        }

        if (event.key === 'Enter') {
            event.preventDefault();
            applyTableEditor(context);
        }
    };
}

function bindTableToolControls(context: RichTextEditorToolContext<TableToolState>): void {
    const { element, toolState } = context;
    initializeTableToolStateHandlers(context);
    unbindTableToolControls(context);
    const tableApplyHandler = toolState.onTableApply;
    const tableCancelHandler = toolState.onTableCancel;
    const tableEditorKeyDownHandler = toolState.onTableEditorKeyDown;

    toolState.tableApplyButton = getTableApplyButton(element);
    toolState.tableCancelButton = getTableCancelButton(element);
    toolState.tableEditorInputs = [
        getTableColumnsInput(element),
        getTableRowsInput(element),
        getTableBorderColorInput(element)
    ].filter(isHtmlInputElement);

    if (tableApplyHandler) {
        toolState.tableApplyButton?.addEventListener('click', tableApplyHandler);
    }

    if (tableCancelHandler) {
        toolState.tableCancelButton?.addEventListener('click', tableCancelHandler);
    }

    if (tableEditorKeyDownHandler) {
        for (const input of toolState.tableEditorInputs) {
            input.addEventListener('keydown', tableEditorKeyDownHandler);
        }
    }
}

function unbindTableToolControls(context: RichTextEditorToolContext<TableToolState>): void {
    const { toolState } = context;
    if (toolState.onTableApply) {
        toolState.tableApplyButton?.removeEventListener('click', toolState.onTableApply);
    }

    if (toolState.onTableCancel) {
        toolState.tableCancelButton?.removeEventListener('click', toolState.onTableCancel);
    }

    if (toolState.onTableEditorKeyDown) {
        for (const input of toolState.tableEditorInputs ?? []) {
            input.removeEventListener('keydown', toolState.onTableEditorKeyDown);
        }
    }
}

function closeTableEditor(context: RichTextEditorToolContext<TableToolState>, { focusSurface = false, preserveSelection = false }: InlineToolCloseOptions = {}): void {
    const { element, editorState, host, toolState } = context;
    host.setToolPanelOpen(element, 'table', false);
    toolState.tableTarget = null;
    if (!preserveSelection) {
        editorState.selectionRange = null;
    }

    if (focusSurface) {
        host.getSurface(element)?.focus();
    }
}

function openTableEditor(
    context: RichTextEditorToolContext<TableToolState>,
    { focusInput = true, selectInputText = true }: { focusInput?: boolean; selectInputText?: boolean } = {}
): boolean {
    const { element, editorState, host, toolState } = context;
    const surface = host.getSurface(element);
    const tableEditor = getTableEditor(element);
    if (!surface || !tableEditor) {
        return false;
    }

    host.closeOtherTools(element, editorState, 'table');

    const selectionElement = host.getSelectionElement(surface);
    const existingTableCandidate = selectionElement?.closest?.('table') ?? null;
    const existingTable = existingTableCandidate instanceof HTMLTableElement ? existingTableCandidate : null;
    const tableDetails = getTableEditorDetails(existingTable);

    host.saveSelectionRange(surface, editorState);
    toolState.tableTarget = existingTable;

    const columnsInput = getTableColumnsInput(element);
    if (columnsInput) {
        columnsInput.value = `${tableDetails.columns}`;
    }

    const rowsInput = getTableRowsInput(element);
    if (rowsInput) {
        rowsInput.value = `${tableDetails.rows}`;
    }

    const borderColorInput = getTableBorderColorInput(element);
    if (borderColorInput) {
        borderColorInput.value = tableDetails.borderColor;
    }

    host.setToolPanelOpen(element, 'table', true);

    if (focusInput) {
        columnsInput?.focus();
        if (selectInputText) {
            columnsInput?.select();
        }
    }

    return false;
}

function applyTableEditor(context: RichTextEditorToolContext<TableToolState>): boolean {
    const { element, editorState, host, toolState } = context;
    const surface = host.getSurface(element);
    if (!surface) {
        return false;
    }

    const requestedColumns = Number.parseInt(getTableColumnsInput(element)?.value ?? '', 10);
    if (Number.isNaN(requestedColumns)) {
        getTableColumnsInput(element)?.focus();
        return false;
    }

    const requestedRows = Number.parseInt(getTableRowsInput(element)?.value ?? '', 10);
    if (Number.isNaN(requestedRows)) {
        getTableRowsInput(element)?.focus();
        return false;
    }

    const columns = clampTableColumns(requestedColumns);
    const rows = clampTableRows(requestedRows);
    const borderColor = normalizeTableBorderColor(getTableBorderColorInput(element)?.value ?? '') || defaultTableBorderColor;
    const tableTarget = toolState.tableTarget;

    if (tableTarget instanceof HTMLTableElement && surface.contains(tableTarget)) {
        const updatedTableHtml = buildTableHtml({
            columns,
            rows,
            borderColor,
            existingContent: getExistingTableContent(tableTarget)
        });

        const template = document.createElement('template');
        template.innerHTML = updatedTableHtml;
        const replacementTable = template.content.querySelector('table');
        if (!(replacementTable instanceof HTMLTableElement)) {
            return false;
        }

        tableTarget.replaceWith(replacementTable);
        toolState.tableTarget = replacementTable;
    } else {
        host.restoreSelectionRange(surface, editorState);
        document.execCommand('insertHTML', false, `${buildTableHtml({ columns, rows, borderColor })}<p><br></p>`);
    }

    closeTableEditor(context, { focusSurface: true });
    return true;
}

registerRichTextEditorTool<TableToolState>({
    command: 'table',
    createState: () => ({
        tableEditorInputs: [],
        tableApplyButton: null,
        tableCancelButton: null,
        tableTarget: null,
        onTableApply: undefined,
        onTableCancel: undefined,
        onTableEditorKeyDown: undefined
    }),
    bind: bindTableToolControls,
    unbind: unbindTableToolControls,
    setDisabled: ({ toolState }, disabled) => {
        toolState.tableEditorInputs.forEach((input) => setDisabled(input, disabled));
        setDisabled(toolState.tableApplyButton, disabled);
        setDisabled(toolState.tableCancelButton, disabled);
    },
    execute: (context) => openTableEditor(context),
    close: closeTableEditor,
    syncState: (context) => {
        const { element, host } = context;
        const surface = host.getSurface(element);
        const selectionElement = host.getSelectionElement(surface);
        const existingTable = selectionElement?.closest?.('table') instanceof HTMLTableElement;
        const focusedToolCommand = host.getFocusedToolCommand(element);

        host.setToolbarButtonPressed(element, 'table', existingTable);

        if (focusedToolCommand === 'table') {
            return;
        }

        if (focusedToolCommand || !existingTable) {
            closeTableEditor(context, { preserveSelection: Boolean(focusedToolCommand) });
            return;
        }

        openTableEditor(context, { focusInput: false, selectInputText: false });
    }
});
