import { readFileSync, existsSync } from 'fs';
import { join, dirname } from 'path';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

describe('NTRichTextEditor JavaScript Module Structure', () => {
  test('module file exists and exposes expected lifecycle exports', () => {
    const moduleFile = join(__dirname, '..', 'NTRichTextEditor.razor.js');

    expect(existsSync(moduleFile)).toBe(true);

    const fileContent = readFileSync(moduleFile, 'utf8');

    expect(fileContent).toContain('export function onLoad');
    expect(fileContent).toContain('export function onUpdate');
    expect(fileContent).toContain('export function onDispose');
    expect(fileContent).toContain('export function focusEditor');
  });

  test('module contains toolbar command handling and markdown sync', () => {
    const moduleFile = join(__dirname, '..', 'NTRichTextEditor.razor.js');
    const fileContent = readFileSync(moduleFile, 'utf8');

    expect(fileContent).toContain('document.execCommand');
    expect(fileContent).toContain("'undo'");
    expect(fileContent).toContain("'redo'");
    expect(fileContent).toContain("'strikeThrough'");
    expect(fileContent).toContain('insertUnorderedList');
    expect(fileContent).toContain('insertOrderedList');
    expect(fileContent).toContain('BLOCKQUOTE');
    expect(fileContent).toContain('PRE');
    expect(fileContent).toContain('justifyLeft');
    expect(fileContent).toContain('justifyCenter');
    expect(fileContent).toContain('justifyRight');
    expect(fileContent).toContain('justifyFull');
    expect(fileContent).toContain('insertHTML');
    expect(fileContent).toContain('createLink');
    expect(fileContent).toContain('Text color');
    expect(fileContent).toContain('Text to color');
    expect(fileContent).toContain('Link URL');
    expect(fileContent).toContain('Table columns');
    expect(fileContent).toContain('Table rows');
    expect(fileContent).toContain('Iframe URL');
    expect(fileContent).toContain('loading="lazy"');
    expect(fileContent).toContain('tryHandleShortcut');
    expect(fileContent).toContain('eventMatchesShortcut');
    expect(fileContent).toContain('paragraph');
    expect(fileContent).toContain('UpdateValueFromJs');
    expect(fileContent).toContain('CommitValueFromJs');
    expect(fileContent).toContain('surfaceToMarkdown');
    expect(fileContent).toContain('markdownToHtml');
  });

  test('module tracks editor state and supporting length text', () => {
    const moduleFile = join(__dirname, '..', 'NTRichTextEditor.razor.js');
    const fileContent = readFileSync(moduleFile, 'utf8');

    expect(fileContent).toContain('WeakMap');
    expect(fileContent).toContain('selectionchange');
    expect(fileContent).toContain('initializeAllEditors');
    expect(fileContent).toContain('aria-pressed');
    expect(fileContent).toContain("key: '7'");
    expect(fileContent).toContain("key: 'z'");
    expect(fileContent).toContain("key: 'y'");
    expect(fileContent).toContain("key: 's'");
    expect(fileContent).toContain("key: 'b'");
    expect(fileContent).toContain("key: 'k'");
    expect(fileContent).toContain("key: 'x'");
    expect(fileContent).toContain("key: 'l'");
    expect(fileContent).toContain("key: 'e'");
    expect(fileContent).toContain("key: 'r'");
    expect(fileContent).toContain("key: 'j'");
    expect(fileContent).toContain("key: 't'");
    expect(fileContent).toContain("key: 'f'");
    expect(fileContent).toContain('alignCenter');
    expect(fileContent).toContain('parseTableSeparator');
    expect(fileContent).toContain('renderTable');
    expect(fileContent).toContain('renderTextColor');
    expect(fileContent).toContain("case 's'");
    expect(fileContent).toContain("case 'span'");
    expect(fileContent).toContain("tagName === 'table'");
    expect(fileContent).toContain('parseIframeMarker');
    expect(fileContent).toContain("tagName === 'iframe'");
    expect(fileContent).toContain('blockquote');
    expect(fileContent).toContain('codeBlock');
    expect(fileContent).toContain("case 'a'");
    expect(fileContent).toContain('handleEditorKeyDown');
    expect(fileContent).toContain('tnt-rich-text-editor-surface');
    expect(fileContent).toContain('tnt-input-length');
    expect(fileContent).toContain('focusEditor');
  });
});
