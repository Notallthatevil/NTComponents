import type {
    RichTextEditorToolContext,
    RichTextEditorToolEditorState,
    RichTextEditorToolHost,
    RichTextEditorToolPlugin
} from '../Editors/Core/NTRichTextEditorToolRegistry.js';

export function setRichTextEditorToolRegistryChangedCallback(callback: (() => void) | null): void;
export function registerRichTextEditorTool<TState>(tool: RichTextEditorToolPlugin<TState>): RichTextEditorToolPlugin<TState>;
export function getRichTextEditorTool(command: string): RichTextEditorToolPlugin<unknown> | null;
export function getRichTextEditorToolState<TState>(editorState: RichTextEditorToolEditorState, tool: RichTextEditorToolPlugin<TState>): TState;
export function createRichTextEditorToolContext<TState>(
    element: HTMLElement,
    editorState: RichTextEditorToolEditorState,
    host: RichTextEditorToolHost,
    tool: RichTextEditorToolPlugin<TState>
): RichTextEditorToolContext<TState>;
