import { join } from 'path';
import { pathToFileURL } from 'url';
import { jest } from '@jest/globals';

const repoRoot = process.cwd();

function moduleUrl(relativePath) {
  return pathToFileURL(join(repoRoot, relativePath)).href;
}

async function importModule(relativePath) {
  return import(moduleUrl(relativePath));
}

describe('NTRichTextEditor tool registry', () => {
  let editorModule;

  beforeAll(async () => {
    editorModule = await importModule('NTComponents/Editors/NTRichTextEditor.razor.js');
  });

  test('registers tools, notifies on change, and reuses per-editor tool state', () => {
    const changed = [];
    editorModule.setRichTextEditorToolRegistryChangedCallback(() => changed.push('changed'));

    const command = `registry-${Date.now()}-${Math.random().toString(16).slice(2)}`;
    const tool = {
      command,
      createState: jest.fn(() => ({ created: true })),
      bind: jest.fn(),
      unbind: jest.fn()
    };

    const registeredTool = editorModule.registerRichTextEditorTool(tool);
    expect(registeredTool).toBe(tool);
    expect(changed).toEqual(['changed']);
    expect(editorModule.getRichTextEditorTool(command)).toBe(tool);
    expect(editorModule.getRichTextEditorTool('missing-command')).toBeNull();

    const editorState = {
      selectionRange: null,
      toolStates: new Map()
    };

    const firstState = editorModule.getRichTextEditorToolState(editorState, tool);
    const secondState = editorModule.getRichTextEditorToolState(editorState, tool);
    expect(firstState).toBe(secondState);
    expect(tool.createState).toHaveBeenCalledTimes(1);

    const element = document.createElement('nt-rich-text-editor');
    const host = { getSurface: jest.fn(() => null) };
    const context = editorModule.createRichTextEditorToolContext(element, editorState, host, tool);
    expect(context).toEqual({
      element,
      editorState,
      host,
      toolState: firstState
    });
  });
});
