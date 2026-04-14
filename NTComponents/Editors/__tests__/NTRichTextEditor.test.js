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

function createDotNetRef() {
  return {
    invokeMethodAsync: jest.fn(() => Promise.resolve())
  };
}

function getNodeIndex(node) {
  return Array.from(node.parentNode.childNodes).indexOf(node);
}

function collapseBeforeNode(node) {
  const range = document.createRange();
  range.setStart(node.parentNode, getNodeIndex(node));
  range.collapse(true);
  window.getSelection().removeAllRanges();
  window.getSelection().addRange(range);
  return range;
}

function selectText(node, startOffset, endOffset) {
  const range = document.createRange();
  range.setStart(node, startOffset);
  range.setEnd(node, endOffset);
  window.getSelection().removeAllRanges();
  window.getSelection().addRange(range);
  return range;
}

function click(element) {
  element.dispatchEvent(new MouseEvent('click', { bubbles: true, cancelable: true }));
}

function keydown(element, key, options = {}) {
  const event = new KeyboardEvent('keydown', {
    bubbles: true,
    cancelable: true,
    key,
    ...options
  });
  element.dispatchEvent(event);
}

function blur(element) {
  element.dispatchEvent(new FocusEvent('blur', { bubbles: false, cancelable: false }));
}

function setSurfaceContent(surface, html) {
  surface.innerHTML = html;
}

function createEditorFixture({
  bindOnInput = false,
  editable = true,
  value = '',
  maxLength = 200
} = {}) {
  document.body.innerHTML = `
    <div class="tnt-input-container">
      <nt-rich-text-editor data-bind-on-input="${bindOnInput}" data-editable="${editable}">
        <div class="tnt-rich-text-editor-shell">
          <input type="hidden" class="tnt-rich-text-editor-hidden-input" value="${value}" />
          <div class="tnt-rich-text-editor-value" hidden>${value}</div>
          <div class="tnt-rich-text-editor-toolbar" role="toolbar">
            <button type="button" class="tnt-rich-text-editor-toolbar-button" data-command="bold" aria-keyshortcuts="Control+B" aria-pressed="false"></button>
            <button type="button" class="tnt-rich-text-editor-toolbar-button" data-command="unorderedList" aria-keyshortcuts="Control+Alt+7" aria-pressed="false"></button>
            <button type="button" class="tnt-rich-text-editor-toolbar-button" data-command="image" aria-keyshortcuts="Control+Alt+M" aria-pressed="false"></button>
            <button type="button" class="tnt-rich-text-editor-toolbar-button" data-command="table" aria-keyshortcuts="Control+Alt+T" aria-pressed="false"></button>
            <button type="button" class="tnt-rich-text-editor-toolbar-button" data-command="textColor" aria-keyshortcuts="Control+Alt+X" aria-pressed="false"></button>
            <button type="button" class="tnt-rich-text-editor-toolbar-button" data-command="link" aria-keyshortcuts="Control+K" aria-pressed="false"></button>
            <button type="button" class="tnt-rich-text-editor-toolbar-button" data-command="iframe" aria-keyshortcuts="Control+Alt+F" aria-pressed="false"></button>
          </div>

          <div data-tool-command="image" data-role="image-editor" hidden aria-hidden="true">
            <input data-role="image-url" type="url" />
            <input data-role="image-file" type="file" />
            <input data-role="image-alt" type="text" />
            <input data-role="image-width" type="number" />
            <input data-role="image-height" type="number" />
            <button type="button" data-role="image-apply"></button>
            <button type="button" data-role="image-cancel"></button>
          </div>

          <div data-tool-command="table" data-role="table-editor" hidden aria-hidden="true">
            <input data-role="table-columns" type="number" />
            <input data-role="table-rows" type="number" />
            <input data-role="table-border-color" type="color" />
            <button type="button" data-role="table-apply"></button>
            <button type="button" data-role="table-cancel"></button>
          </div>

          <div data-tool-command="textColor" data-role="text-color-editor" hidden aria-hidden="true">
            <input data-role="text-color-value" type="color" />
            <button type="button" data-role="text-color-apply"></button>
            <button type="button" data-role="text-color-cancel"></button>
          </div>

          <div data-tool-command="link" data-role="link-editor" hidden aria-hidden="true">
            <input data-role="link-url" type="url" />
            <input data-role="link-text" type="text" />
            <button type="button" data-role="link-apply"></button>
            <button type="button" data-role="link-cancel"></button>
          </div>

          <div data-tool-command="iframe" data-role="iframe-editor" hidden aria-hidden="true">
            <input data-role="iframe-url" type="url" />
            <input data-role="iframe-title" type="text" />
            <input data-role="iframe-width" type="text" />
            <input data-role="iframe-height" type="text" />
            <button type="button" data-role="iframe-apply"></button>
            <button type="button" data-role="iframe-cancel"></button>
          </div>

          <div class="tnt-rich-text-editor-surface" contenteditable="true" data-placeholder="Write" data-maxlength="${maxLength}" tabindex="0"></div>
        </div>
      </nt-rich-text-editor>
      <span class="tnt-input-length">0/${maxLength}</span>
    </div>
  `;

  return {
    container: document.querySelector('.tnt-input-container'),
    element: document.querySelector('nt-rich-text-editor'),
    hiddenInput: document.querySelector('.tnt-rich-text-editor-hidden-input'),
    sourceValue: document.querySelector('.tnt-rich-text-editor-value'),
    surface: document.querySelector('.tnt-rich-text-editor-surface'),
    length: document.querySelector('.tnt-input-length')
  };
}

describe('NTRichTextEditor runtime behavior', () => {
  let editorModule;
  let libraryModule;
  let selectionState;
  let originalExecCommand;
  let originalFileReader;
  let originalRequestAnimationFrame;
  let originalCancelAnimationFrame;

  beforeAll(async () => {
    libraryModule = await importModule('NTComponents/wwwroot/NTComponents.lib.module.js');
    await importModule('NTComponents/Editors/Tool/EditorToolImageButton.razor.js');
    await importModule('NTComponents/Editors/Tool/EditorToolTableButton.razor.js');
    await importModule('NTComponents/Editors/Tool/EditorToolTextColorButton.razor.js');
    await importModule('NTComponents/Editors/Tool/EditorToolLinkButton.razor.js');
    await importModule('NTComponents/Editors/Tool/EditorToolIframeButton.razor.js');
    editorModule = await importModule('NTComponents/Editors/NTRichTextEditor.razor.js');
  });

  beforeEach(() => {
    document.body.innerHTML = '';

    selectionState = { range: null };
    Object.defineProperty(window, 'getSelection', {
      configurable: true,
      value: () => ({
        get rangeCount() {
          return selectionState.range ? 1 : 0;
        },
        get anchorNode() {
          return selectionState.range?.startContainer ?? null;
        },
        getRangeAt(index) {
          if (index !== 0 || !selectionState.range) {
            throw new Error('No selection range available.');
          }

          return selectionState.range;
        },
        removeAllRanges() {
          selectionState.range = null;
        },
        addRange(range) {
          selectionState.range = range;
        },
        toString() {
          return selectionState.range?.toString() ?? '';
        }
      })
    });

    originalExecCommand = document.execCommand;
    document.execCommand = jest.fn((command, _showUi, value) => {
      if (command !== 'insertHTML') {
        return true;
      }

      const selection = window.getSelection();
      if (!selection.rangeCount) {
        return true;
      }

      const range = selection.getRangeAt(0);
      const template = document.createElement('template');
      template.innerHTML = value;
      const fragment = template.content.cloneNode(true);
      const insertedNodes = Array.from(fragment.childNodes);
      range.deleteContents();
      range.insertNode(fragment);

      if (insertedNodes.length > 0) {
        const nextRange = document.createRange();
        nextRange.setStartAfter(insertedNodes[insertedNodes.length - 1]);
        nextRange.collapse(true);
        selection.removeAllRanges();
        selection.addRange(nextRange);
      }

      return true;
    });

    originalFileReader = global.FileReader;
    global.FileReader = class MockFileReader {
      constructor() {
        this.result = null;
        this.onload = null;
      }

      readAsDataURL(file) {
        this.result = `data:${file.type};base64,ZmFrZQ==`;
        this.onload?.({ target: this });
      }
    };

    originalRequestAnimationFrame = window.requestAnimationFrame;
    originalCancelAnimationFrame = window.cancelAnimationFrame;
    window.requestAnimationFrame = (callback) => setTimeout(() => callback(0), 0);
    window.cancelAnimationFrame = (handle) => clearTimeout(handle);
  });

  afterEach(() => {
    jest.useRealTimers();
    document.execCommand = originalExecCommand;
    global.FileReader = originalFileReader;
    window.requestAnimationFrame = originalRequestAnimationFrame;
    window.cancelAnimationFrame = originalCancelAnimationFrame;
    document.body.innerHTML = '';
  });

  test('onLoad renders markdown, updates supporting length, and syncs bind-on-input changes', async () => {
    const fixture = createEditorFixture({ bindOnInput: true, value: 'Hello' });
    const dotNetRef = createDotNetRef();

    editorModule.onLoad(fixture.element, dotNetRef);

    expect(fixture.surface.innerHTML).toContain('Hello');
    expect(fixture.length.textContent).toBe('5/200');

    setSurfaceContent(fixture.surface, '<p>World</p>');
    fixture.surface.dispatchEvent(new Event('input', { bubbles: true }));

    expect(fixture.hiddenInput.value).toBe('World');
    expect(fixture.sourceValue.textContent).toBe('World');
    expect(dotNetRef.invokeMethodAsync).toHaveBeenCalledWith(
      'UpdateValueFromJs',
      'World',
      '<p>World</p>'
    );
  });

  test('keyboard shortcuts execute commands and blur commits the editor value', () => {
    jest.useFakeTimers();

    const fixture = createEditorFixture();
    const dotNetRef = createDotNetRef();
    editorModule.onLoad(fixture.element, dotNetRef);

    keydown(fixture.surface, 'b', { ctrlKey: true });
    expect(document.execCommand).toHaveBeenCalledWith('bold', false);

    setSurfaceContent(fixture.surface, '<p>Committed</p>');
    const outsideButton = document.createElement('button');
    document.body.appendChild(outsideButton);
    outsideButton.focus();
    blur(fixture.surface);
    jest.runAllTimers();

    expect(dotNetRef.invokeMethodAsync).toHaveBeenCalledWith(
      'CommitValueFromJs',
      'Committed',
      '<p>Committed</p>'
    );
  });

  test('disabled editors disable toolbar and tool controls and block toolbar execution', () => {
    const fixture = createEditorFixture({ editable: false });
    editorModule.onLoad(fixture.element, createDotNetRef());

    const boldButton = fixture.element.querySelector('[data-command="bold"]');
    const imageUrlInput = fixture.element.querySelector('[data-role="image-url"]');
    const imageApplyButton = fixture.element.querySelector('[data-role="image-apply"]');

    expect(boldButton.disabled).toBe(true);
    expect(imageUrlInput.disabled).toBe(true);
    expect(imageApplyButton.disabled).toBe(true);

    click(boldButton);

    expect(document.execCommand).not.toHaveBeenCalled();
  });

  test('image tool edits an existing image and populates uploaded file details', () => {
    const fixture = createEditorFixture();
    editorModule.onLoad(fixture.element, createDotNetRef());

    setSurfaceContent(fixture.surface, '<p><img src="https://old.example/image.png" alt="Old alt" width="320" height="180" /></p>');
    const image = fixture.surface.querySelector('img');
    collapseBeforeNode(image);

    click(fixture.element.querySelector('[data-command="image"]'));

    const imagePanel = fixture.element.querySelector('[data-tool-command="image"]');
    const urlInput = fixture.element.querySelector('[data-role="image-url"]');
    const fileInput = fixture.element.querySelector('[data-role="image-file"]');
    const altInput = fixture.element.querySelector('[data-role="image-alt"]');
    const widthInput = fixture.element.querySelector('[data-role="image-width"]');
    const heightInput = fixture.element.querySelector('[data-role="image-height"]');

    expect(imagePanel.hidden).toBe(false);
    expect(urlInput.value).toBe('https://old.example/image.png');
    expect(altInput.value).toBe('Old alt');
    expect(widthInput.value).toBe('320');
    expect(heightInput.value).toBe('180');

    Object.defineProperty(fileInput, 'files', {
      configurable: true,
      value: [new File(['image'], 'updated-image.png', { type: 'image/png' })]
    });
    fileInput.dispatchEvent(new Event('change', { bubbles: true }));

    expect(urlInput.value).toBe('data:image/png;base64,ZmFrZQ==');
    expect(altInput.value).toBe('Old alt');

    urlInput.value = 'https://new.example/image.png';
    altInput.value = 'Updated alt';
    widthInput.value = '640';
    heightInput.value = '360';
    click(fixture.element.querySelector('[data-role="image-apply"]'));

    expect(image.getAttribute('src')).toBe('https://new.example/image.png');
    expect(image.getAttribute('alt')).toBe('Updated alt');
    expect(image.getAttribute('width')).toBe('640');
    expect(image.getAttribute('height')).toBe('360');
    expect(imagePanel.hidden).toBe(false);
  });

  test('table tool inserts clamped tables with border styling', () => {
    const fixture = createEditorFixture();
    editorModule.onLoad(fixture.element, createDotNetRef());

    fixture.surface.textContent = '';
    const range = document.createRange();
    range.selectNodeContents(fixture.surface);
    range.collapse(false);
    window.getSelection().removeAllRanges();
    window.getSelection().addRange(range);

    click(fixture.element.querySelector('[data-command="table"]'));

    fixture.element.querySelector('[data-role="table-columns"]').value = '20';
    fixture.element.querySelector('[data-role="table-rows"]').value = '0';
    fixture.element.querySelector('[data-role="table-border-color"]').value = '#112233';
    click(fixture.element.querySelector('[data-role="table-apply"]'));

    const table = fixture.surface.querySelector('table');
    expect(table).not.toBeNull();
    expect(table.querySelectorAll('thead th')).toHaveLength(8);
    expect(table.querySelectorAll('tbody tr')).toHaveLength(1);
    expect(table.getAttribute('data-border-color')).toBe('#112233');
    expect(table.style.getPropertyValue('--nt-rich-text-table-border-color')).toBe('#112233');
    expect(fixture.surface.lastElementChild.tagName).toBe('P');
  });

  test('text color tool wraps selected text and normalizes adjacent colored spans', () => {
    const fixture = createEditorFixture();
    editorModule.onLoad(fixture.element, createDotNetRef());

    setSurfaceContent(fixture.surface, '<p>Alpha Beta</p>');
    const textNode = fixture.surface.querySelector('p').firstChild;
    selectText(textNode, 0, 5);

    click(fixture.element.querySelector('[data-command="textColor"]'));
    fixture.element.querySelector('[data-role="text-color-value"]').value = '#ff0000';
    click(fixture.element.querySelector('[data-role="text-color-apply"]'));

    const coloredSpan = fixture.surface.querySelector('span');
    expect(coloredSpan).not.toBeNull();
    expect(coloredSpan.textContent).toBe('Alpha');
    expect(coloredSpan.style.color).toBe('rgb(255, 0, 0)');

    setSurfaceContent(fixture.surface, '<p><span style="color:#ff0000">Alpha</span><span style="color:#ff0000">Beta</span></p>');
    const secondSpanTextNode = fixture.surface.querySelectorAll('span')[1].firstChild;
    selectText(secondSpanTextNode, 0, 4);

    click(fixture.element.querySelector('[data-command="textColor"]'));
    click(fixture.element.querySelector('[data-role="text-color-apply"]'));

    const mergedSpans = fixture.surface.querySelectorAll('span');
    expect(mergedSpans).toHaveLength(1);
    expect(mergedSpans[0].textContent).toBe('AlphaBeta');
  });

  test('link tool inserts links from the current selection and rejects unsafe URLs', () => {
    const fixture = createEditorFixture();
    editorModule.onLoad(fixture.element, createDotNetRef());

    setSurfaceContent(fixture.surface, '<p>Docs</p>');
    const textNode = fixture.surface.querySelector('p').firstChild;
    selectText(textNode, 0, 4);

    click(fixture.element.querySelector('[data-command="link"]'));

    const linkUrlInput = fixture.element.querySelector('[data-role="link-url"]');
    const linkTextInput = fixture.element.querySelector('[data-role="link-text"]');
    expect(linkTextInput.value).toBe('Docs');

    linkUrlInput.value = 'javascript:alert(1)';
    click(fixture.element.querySelector('[data-role="link-apply"]'));
    expect(fixture.surface.querySelector('a')).toBeNull();
    expect(document.activeElement).toBe(linkUrlInput);

    linkUrlInput.value = 'https://example.com/docs';
    linkTextInput.value = '';
    click(fixture.element.querySelector('[data-role="link-apply"]'));

    const anchor = fixture.surface.querySelector('a');
    expect(anchor).not.toBeNull();
    expect(anchor.getAttribute('href')).toBe('https://example.com/docs');
    expect(anchor.textContent).toBe('Docs');
  });

  test('iframe tool inserts lazy-loaded embeds with default metadata', () => {
    const fixture = createEditorFixture();
    editorModule.onLoad(fixture.element, createDotNetRef());

    const range = document.createRange();
    range.selectNodeContents(fixture.surface);
    range.collapse(false);
    window.getSelection().removeAllRanges();
    window.getSelection().addRange(range);

    click(fixture.element.querySelector('[data-command="iframe"]'));

    fixture.element.querySelector('[data-role="iframe-url"]').value = 'https://example.com/embed';
    click(fixture.element.querySelector('[data-role="iframe-apply"]'));

    const iframe = fixture.surface.querySelector('iframe');
    expect(iframe).not.toBeNull();
    expect(iframe.getAttribute('src')).toBe('https://example.com/embed');
    expect(iframe.getAttribute('title')).toBe('Embedded content');
    expect(iframe.getAttribute('width')).toBe('100%');
    expect(iframe.getAttribute('height')).toBe('315');
    expect(iframe.getAttribute('loading')).toBe('lazy');
  });

  test('onUpdate binds newly added tool controls on the first sync', () => {
    const fixture = createEditorFixture();
    const imageButton = fixture.element.querySelector('[data-command="image"]');
    const imagePanel = fixture.element.querySelector('[data-tool-command="image"]');
    imageButton.remove();
    imagePanel.remove();

    editorModule.onLoad(fixture.element, createDotNetRef());

    fixture.element.querySelector('.tnt-rich-text-editor-toolbar').appendChild(imageButton);
    fixture.element.querySelector('.tnt-rich-text-editor-shell').insertBefore(imagePanel, fixture.surface);
    editorModule.onUpdate(fixture.element, createDotNetRef());

    const range = document.createRange();
    range.selectNodeContents(fixture.surface);
    range.collapse(false);
    window.getSelection().removeAllRanges();
    window.getSelection().addRange(range);

    click(imageButton);
    expect(imagePanel.hidden).toBe(false);

    fixture.element.querySelector('[data-role="image-url"]').value = 'https://example.com/runtime-image.png';
    click(fixture.element.querySelector('[data-role="image-apply"]'));

    const image = fixture.surface.querySelector('img');
    expect(image).not.toBeNull();
    expect(image.getAttribute('src')).toBe('https://example.com/runtime-image.png');
  });

  test('onDispose detaches editor listeners so later events do not resync state', () => {
    const fixture = createEditorFixture({ bindOnInput: true, value: 'Before' });
    const dotNetRef = createDotNetRef();
    editorModule.onLoad(fixture.element, dotNetRef);
    dotNetRef.invokeMethodAsync.mockClear();

    editorModule.onDispose(fixture.element);

    setSurfaceContent(fixture.surface, '<p>After</p>');
    fixture.surface.dispatchEvent(new Event('input', { bubbles: true }));
    click(fixture.element.querySelector('[data-command="bold"]'));

    expect(dotNetRef.invokeMethodAsync).not.toHaveBeenCalled();
    expect(document.execCommand).not.toHaveBeenCalled();
  });

  test('test hooks cover utility and normalization helpers', async () => {
    const hooks = editorModule.__testHooks;
    const root = document.createElement('div');
    root.innerHTML = '<div class="foo"><button type="button"></button><span style="color: rgb(1, 2, 3);">text</span><table><tbody><tr><td>A</td></tr></tbody></table></div>';
    const button = root.querySelector('button');
    const span = root.querySelector('span');
    const row = root.querySelector('tr');
    const cell = root.querySelector('td');

    expect(hooks.toArray({ 0: 'x', 1: 'y', length: 2 })).toEqual(['x', 'y']);
    expect(hooks.qs(root, '.foo')).toBe(root.firstElementChild);
    expect(hooks.isElement(root.firstElementChild)).toBe(true);
    expect(hooks.isHtmlButtonElement(button)).toBe(true);
    expect(hooks.isHtmlElement(root.firstElementChild)).toBe(true);
    expect(hooks.isHtmlInputElement(document.createElement('input'))).toBe(true);
    expect(hooks.isHtmlSpanElement(span)).toBe(true);
    expect(hooks.isTableCellElement(cell)).toBe(true);
    expect(hooks.isTableRowElement(row)).toBe(true);
    expect(hooks.getTableCells(row)).toHaveLength(1);

    const blurState = { blurTimeoutId: setTimeout(() => {}, 50) };
    hooks.clearBlurTimeout(blurState);
    expect(blurState.blurTimeoutId).toBeNull();
    hooks.clearBlurTimeout(null);

    const pendingState = { pendingSyncFrameId: setTimeout(() => {}, 50) };
    hooks.clearPendingSync(pendingState);
    expect(pendingState.pendingSyncFrameId).toBeNull();
    hooks.clearPendingSync(null);

    const caretSurface = document.createElement('div');
    caretSurface.innerHTML = '<p>Alpha</p><p>Beta</p>';
    document.body.appendChild(caretSurface);
    hooks.placeCaretAtEnd(caretSurface);
    expect(window.getSelection().anchorNode).not.toBeNull();

    expect(hooks.extractUrlScheme('https://example.com')).toBe('https');
    expect(hooks.normalizeSafeUrl('', 'link')).toBe('');
    expect(hooks.normalizeSafeUrl('#anchor', 'link')).toBe('#anchor');
    expect(hooks.normalizeSafeUrl('/relative', 'link')).toBe('/relative');
    expect(hooks.normalizeSafeUrl('mailto:test@example.com', 'link')).toBe('mailto:test@example.com');
    expect(hooks.normalizeSafeUrl('tel:555', 'link')).toBe('tel:555');
    expect(hooks.normalizeSafeUrl('blob:https://example.com/x', 'image')).toBe('blob:https://example.com/x');
    expect(hooks.normalizeSafeUrl('data:image/png;base64,abc', 'image')).toBe('data:image/png;base64,abc');
    expect(hooks.normalizeSafeUrl('javascript:alert(1)', 'link')).toBe('');
    expect(hooks.normalizeSafeUrl('ftp://example.com/file', 'iframe')).toBe('');
    expect(hooks.normalizeSafeUrl('example.com/path', 'iframe')).toBe('example.com/path');
    expect(hooks.canUseMarkdownUrl('https://example.com/a-b')).toBe(true);
    expect(hooks.canUseMarkdownUrl('https://example.com/a b')).toBe(false);

    expect(hooks.getChildNodeIndex(cell)).toBe(0);
    expect(hooks.getChildNodeIndex(document.createTextNode('orphan'))).toBe(-1);

    const boundarySurface = document.createElement('div');
    boundarySurface.innerHTML = '<p><img src="https://example.com/a.png" alt="A" />Text</p>';
    document.body.appendChild(boundarySurface);
    const paragraph = boundarySurface.querySelector('p');
    const image = boundarySurface.querySelector('img');
    const textNode = paragraph.lastChild;
    expect(hooks.getBoundaryAdjacentElement(boundarySurface, textNode, 0, 'before')).toBe(image);
    expect(hooks.getBoundaryAdjacentElement(boundarySurface, textNode, textNode.textContent.length, 'after')).toBeNull();

    collapseBeforeNode(image);
    expect(hooks.getSelectionElement(boundarySurface)).toBe(image);
    expect(hooks.getCurrentBlockElement(boundarySurface)).toBe(paragraph);
    paragraph.setAttribute('align', 'center');
    expect(hooks.getCurrentAlignment(boundarySurface, paragraph)).toBe('center');
    expect(hooks.hasAncestorTag(boundarySurface, 'p')).toBe(true);
    expect(hooks.getElementTextColor(span)).toBe('rgb(1, 2, 3)');
    expect(hooks.getElementTextColor({})).toBe('');
    expect(hooks.normalizeHexColor('#abc')).toBe('#aabbcc');
    expect(hooks.normalizeHexColor('#aabbcc')).toBe('#aabbcc');
    expect(hooks.normalizeHexColor('red')).toBe('');
    expect(hooks.convertRgbChannelToHex('16')).toBe('10');
    expect(hooks.convertRgbChannelToHex('bad')).toBe('');
    expect(hooks.normalizeTextColorValue('rgb(255, 0, 128)')).toBe('#ff0080');
    expect(hooks.normalizeTextColorValue('rgba(255, 0, 128, 0.5)')).toBe('#ff0080');
    expect(hooks.normalizeTextColorValue('invalid')).toBe('');
    expect(hooks.normalizeNewLines('a\r\nb\rc')).toBe('a\nb\nc');
    expect(hooks.countLeadingSpaces('   x')).toBe(3);
    expect(hooks.stripIndent('hello', 10)).toBe('');
    expect(hooks.escapeHtml(`<&>"'`)).toBe('&lt;&amp;&gt;&quot;&#39;');

    const tokens = [];
    expect(hooks.createInlineToken(tokens, '<strong>x</strong>')).toBe('__NT_INLINE_TOKEN_0__');
    expect(tokens).toHaveLength(1);
    expect(hooks.normalizeImageDimension('320')).toBe('320');
    expect(hooks.normalizeImageDimension('50%')).toBe('');
    expect(hooks.normalizeTableBorderColor('#112233')).toBe('#112233');
    expect(hooks.normalizeTableBorderColor('#123')).toBe('');
    expect(hooks.buildTableStyleAttribute('#112233')).toContain('data-border-color="#112233"');
    expect(hooks.buildTableStyleAttribute('bad')).toBe('');
    expect(hooks.buildImageHtml({ src: 'https://example.com/a.png', alt: 'A', width: '100', height: '200' })).toContain('width="100"');
    expect(hooks.buildImageHtml({ src: 'javascript:bad', alt: 'A', width: '', height: '' })).toBe('');
    expect(hooks.buildImageMarkdown({ src: 'https://example.com/a.png', alt: 'A', width: '', height: '' })).toBe('![A](https://example.com/a.png)');
    expect(hooks.buildImageMarkdown({ src: 'https://example.com/a b.png', alt: 'A', width: '100', height: '' })).toContain('<img src="https://example.com/a b.png"');
    expect(hooks.renderMarkdownImage('', 'Alt', 'https://example.com/a.png')).toContain('<img');
    expect(hooks.renderHtmlImage('', 'https://example.com/a.png', 'Alt', '50', '60')).toContain('height="60"');
    expect(hooks.getImageDetails({})).toEqual({ src: '', alt: '', width: '', height: '' });
    expect(hooks.renderHtmlAnchor('', 'javascript:bad', 'Text')).toBe('Text');
    expect(hooks.renderLink('', 'Text', 'https://example.com')).toContain('<a href="https://example.com">');
    expect(hooks.renderTextColor('', '#123456', 'Text')).toContain('style="color:#123456;"');

    const rejectedRef = { invokeMethodAsync: jest.fn(() => Promise.reject(new Error('disconnect'))) };
    hooks.invokeDotNetVoid(rejectedRef, 'UpdateMarkupValueFromJs', '<p>x</p>');
    hooks.invokeDotNetVoid(rejectedRef, 'UpdateValueFromJs', 'md', '<p>md</p>');
    await Promise.resolve();
  });

  test('test hooks render markdown blocks and serialize complex editor content', () => {
    const hooks = editorModule.__testHooks;

    const markdown = [
      '# Heading',
      '',
      'Plain **bold** *italic* ~~strike~~ <u>underline</u> [link](https://example.com)',
      '',
      '> Quote',
      '>',
      '> More quote',
      '',
      '- Item 1',
      '  continued',
      '  - Nested item',
      '1. Numbered',
      '',
      '```ts',
      'const value = 1;',
      '```',
      '',
      '| Name | Role |',
      '| :--- | ---: |',
      '| Avery | Host |',
      '',
      '<table data-border-color="#112233"><thead><tr><th>Col</th></tr></thead><tbody><tr><td>Cell</td></tr></tbody></table>',
      '',
      '<div align="justify">',
      'Aligned text',
      '</div>',
      '',
      '<iframe src="https://example.com/embed" title="Embed" width="100%" height="315"></iframe>'
    ].join('\n');

    const rendered = hooks.markdownToHtml(markdown);
    expect(rendered).toContain('<h1>Heading</h1>');
    expect(rendered).toContain('<strong>bold</strong>');
    expect(rendered).toContain('<em>italic</em>');
    expect(rendered).toContain('<s>strike</s>');
    expect(rendered).toContain('<u>underline</u>');
    expect(rendered).toContain('<a href="https://example.com">link</a>');
    expect(rendered).toContain('<blockquote>');
    expect(rendered).toContain('<ul>');
    expect(rendered).toContain('<ol>');
    expect(rendered).toContain('<pre data-language="ts"><code data-language="ts">const value = 1;</code></pre>');
    expect(rendered).toContain('<table>');
    expect(rendered).toContain('data-border-color="#112233"');
    expect(rendered).toContain('text-align:justify;');
    expect(rendered).toContain('loading="lazy"');

    const surface = document.createElement('div');
    surface.innerHTML = [
      '<div align="center"><p><strong>Bold</strong> <em>Italic</em> <u>Underline</u> <s>Strike</s> <span style="color:#123456">Color</span><br><img src="https://example.com/image.png" alt="Alt" width="50" height="60" /><a href="https://example.com/docs">Docs</a></p></div>',
      '<pre data-language="ts"><code data-language="ts">const value = 1;\n</code></pre>',
      '<blockquote><p>Quote</p><p>More</p></blockquote>',
      '<ul><li>Item 1<ul><li>Nested</li></ul></li><li>Item 2</li></ul>',
      '<table data-border-color="#112233"><thead><tr><th style="text-align:center;">Name</th><th>Role</th></tr></thead><tbody><tr><td>Avery</td><td>Host</td></tr></tbody></table>',
      '<iframe src="https://example.com/embed" title="Embed" width="100%" height="315" loading="lazy"></iframe>'
    ].join('');

    const serialized = hooks.surfaceToMarkdown(surface);
    expect(serialized).toContain('<div align="center">');
    expect(serialized).toContain('**Bold**');
    expect(serialized).toContain('*Italic*');
    expect(serialized).toContain('<u>Underline</u>');
    expect(serialized).toContain('~~Strike~~');
    expect(serialized).toContain('<span style="color:rgb\\(18, 52, 86\\);">Color</span>');
    expect(serialized).toContain('<img src="https://example.com/image.png" alt="Alt" width="50" height="60" />');
    expect(serialized).toContain('[Docs](https://example.com/docs)');
    expect(serialized).toContain('```ts');
    expect(serialized).toContain('> Quote');
    expect(serialized).toContain('- Item 1');
    expect(serialized).toContain('<table data-border-color="#112233"');
    expect(serialized).toContain('<iframe src="https://example.com/embed" title="Embed" width="100%" height="315" loading="lazy"></iframe>');

    expect(hooks.escapeMarkdownText('[a](b)*')).toBe('\\[a\\]\\(b\\)\\*');
    expect(hooks.escapeMarkdownAttribute("line\nbreak")).toBe('line break');
    expect(hooks.normalizeAlignment('start')).toBe('left');
    expect(hooks.normalizeAlignment('end')).toBe('right');
    expect(hooks.normalizeAlignment('weird')).toBe('');
    expect(hooks.getElementAlignment(surface.firstElementChild)).toBe('center');
    expect(hooks.hasRenderableBlockChildren(surface)).toBe(true);
    expect(hooks.wrapAlignedMarkdown('justify', 'Hello')).toContain('<div align="justify">');
    expect(hooks.indentMarkdownBlock('A\n\nB', 2)).toBe('  A\n\n  B');
    expect(hooks.serializeListItemContent(2, '- ', 'Line 1\nLine 2')).toBe('  - Line 1\n    Line 2');
    expect(hooks.formatTableSeparator('center')).toBe(':---:');
    expect(hooks.formatTableSeparator('right')).toBe('---:');
    expect(hooks.formatTableSeparator('left')).toBe(':---');
    expect(hooks.formatTableSeparator('')).toBe('---');
  });

  test('test hooks cover runtime synchronization and toolbar state helpers', () => {
    const hooks = editorModule.__testHooks;
    const fixture = createEditorFixture({ bindOnInput: false, value: 'Start', maxLength: 25 });
    const dotNetRef = createDotNetRef();
    editorModule.onLoad(fixture.element, dotNetRef);

    hooks.updateSourceValue(fixture.element, 'Updated');
    expect(fixture.hiddenInput.value).toBe('Updated');
    expect(fixture.sourceValue.textContent).toBe('Updated');

    hooks.updateEmptyState(fixture.surface);
    expect(fixture.surface.dataset.empty).toBe('false');
    fixture.surface.innerHTML = '';
    hooks.updateEmptyState(fixture.surface);
    expect(fixture.surface.dataset.empty).toBe('true');

    hooks.updateLength(fixture.element, '1234');
    expect(fixture.length.textContent).toBe('4/25');
    fixture.surface.removeAttribute('data-maxlength');
    hooks.updateLength(fixture.element, '12345');
    expect(fixture.length.textContent).toBe('4/25');
    fixture.surface.setAttribute('data-maxlength', '25');

    hooks.applyMarkdownToSurface(fixture.element, '**Bold**');
    expect(fixture.surface.innerHTML).toContain('<strong>Bold</strong>');

    setSurfaceContent(fixture.surface, '<p>Sync</p>');
    hooks.syncValueFromSurface(fixture.element, true);
    expect(fixture.hiddenInput.value).toBe('Sync');
    expect(dotNetRef.invokeMethodAsync).toHaveBeenCalledWith('UpdateValueFromJs', 'Sync', '<p>Sync</p>');

    setSurfaceContent(fixture.surface, '<p>Commit</p>');
    hooks.commitValue(fixture.element);
    expect(dotNetRef.invokeMethodAsync).toHaveBeenCalledWith('CommitValueFromJs', 'Commit', '<p>Commit</p>');

    setSurfaceContent(fixture.surface, '<blockquote>Quote</blockquote>');
    collapseBeforeNode(fixture.surface.querySelector('blockquote'));
    fixture.element.querySelector('.tnt-rich-text-editor-toolbar').innerHTML = `
      <button type="button" class="tnt-rich-text-editor-toolbar-button" data-command="paragraph" data-value="" aria-pressed="false"></button>
      <button type="button" class="tnt-rich-text-editor-toolbar-button" data-command="heading" data-value="1" aria-pressed="false"></button>
      <button type="button" class="tnt-rich-text-editor-toolbar-button" data-command="alignLeft" aria-pressed="false"></button>
      <button type="button" class="tnt-rich-text-editor-toolbar-button" data-command="alignCenter" aria-pressed="false"></button>
      <button type="button" class="tnt-rich-text-editor-toolbar-button" data-command="alignRight" aria-pressed="false"></button>
      <button type="button" class="tnt-rich-text-editor-toolbar-button" data-command="alignJustify" aria-pressed="false"></button>
      <button type="button" class="tnt-rich-text-editor-toolbar-button" data-command="unorderedList" aria-pressed="false"></button>
      <button type="button" class="tnt-rich-text-editor-toolbar-button" data-command="orderedList" aria-pressed="false"></button>
      <button type="button" class="tnt-rich-text-editor-toolbar-button" data-command="blockquote" aria-pressed="false"></button>
      <button type="button" class="tnt-rich-text-editor-toolbar-button" data-command="codeBlock" aria-pressed="false"></button>
      <button type="button" class="tnt-rich-text-editor-toolbar-button" data-command="bold" aria-pressed="false"></button>
      <button type="button" class="tnt-rich-text-editor-toolbar-button" data-command="italic" aria-pressed="false"></button>
      <button type="button" class="tnt-rich-text-editor-toolbar-button" data-command="underline" aria-pressed="false"></button>
      <button type="button" class="tnt-rich-text-editor-toolbar-button" data-command="strikeThrough" aria-pressed="false"></button>
    `;
    editorModule.onUpdate(fixture.element, dotNetRef);
    hooks.updateToolbarState(fixture.element);
    expect(fixture.element.querySelector('[data-command="blockquote"]').getAttribute('aria-pressed')).toBe('true');

    setSurfaceContent(fixture.surface, '<ul><li><strong><em><u><s>Alpha</s></u></em></strong></li></ul>');
    selectText(fixture.surface.querySelector('s').firstChild, 0, 5);
    hooks.updateToolbarState(fixture.element);
    expect(fixture.element.querySelector('[data-command="unorderedList"]').getAttribute('aria-pressed')).toBe('true');
    expect(fixture.element.querySelector('[data-command="bold"]').getAttribute('aria-pressed')).toBe('true');
    expect(fixture.element.querySelector('[data-command="italic"]').getAttribute('aria-pressed')).toBe('true');
    expect(fixture.element.querySelector('[data-command="underline"]').getAttribute('aria-pressed')).toBe('true');
    expect(fixture.element.querySelector('[data-command="strikeThrough"]').getAttribute('aria-pressed')).toBe('true');

    expect(hooks.eventMatchesShortcut(new KeyboardEvent('keydown', { key: 'B', ctrlKey: true }), 'Control+B')).toBe(true);
    expect(hooks.eventMatchesShortcut(new KeyboardEvent('keydown', { key: 'B' }), '')).toBe(false);
    expect(hooks.eventMatchesShortcut(new KeyboardEvent('keydown', { key: 'B', shiftKey: true }), 'Control+B')).toBe(false);
  });

  test('selection hooks clear invalid ranges and fall back to a caret inside the surface', () => {
    const hooks = editorModule.__testHooks;
    const surface = document.createElement('div');
    surface.innerHTML = '<p>Alpha</p><p>Beta</p>';
    document.body.appendChild(surface);
    const state = { selectionRange: 'sentinel' };

    window.getSelection().removeAllRanges();
    hooks.saveSelectionRange(surface, state);
    expect(state.selectionRange).toBeNull();

    const outside = document.createElement('div');
    outside.textContent = 'Outside';
    document.body.appendChild(outside);
    selectText(outside.firstChild, 0, 3);
    hooks.saveSelectionRange(surface, state);
    expect(state.selectionRange).toBeNull();

    const insideText = surface.querySelector('p').firstChild;
    selectText(insideText, 0, 5);
    hooks.saveSelectionRange(surface, state);
    expect(state.selectionRange).not.toBeNull();

    const detachedHost = document.createElement('div');
    detachedHost.innerHTML = '<p>Detached</p>';
    const detachedRange = document.createRange();
    detachedRange.setStart(detachedHost.querySelector('p').firstChild, 0);
    detachedRange.setEnd(detachedHost.querySelector('p').firstChild, 3);
    state.selectionRange = detachedRange;
    hooks.restoreSelectionRange(surface, state);
    expect(state.selectionRange).toBeNull();
    expect(window.getSelection().rangeCount).toBe(1);
    expect(surface.contains(window.getSelection().anchorNode)).toBe(true);

    hooks.restoreSelectionRange(surface, null);
    expect(window.getSelection().rangeCount).toBe(1);
  });

  test('image tool updates existing images, clears dimensions, autofills alt text, and rejects unsafe urls', () => {
    const fixture = createEditorFixture();
    editorModule.onLoad(fixture.element, createDotNetRef());

    setSurfaceContent(fixture.surface, '<p><img src="https://example.com/old.png" alt="" width="40" height="50" /></p>');
    const image = fixture.surface.querySelector('img');
    collapseBeforeNode(image);
    click(fixture.element.querySelector('[data-command="image"]'));

    const fileInput = fixture.element.querySelector('[data-role="image-file"]');
    Object.defineProperty(fileInput, 'files', {
      configurable: true,
      value: [new File(['x'], 'photo.png', { type: 'image/png' })]
    });
    fileInput.dispatchEvent(new Event('change', { bubbles: true }));

    expect(fixture.element.querySelector('[data-role="image-url"]').value).toContain('data:image/png;base64');
    expect(fixture.element.querySelector('[data-role="image-alt"]').value).toBe('photo');

    fixture.element.querySelector('[data-role="image-url"]').value = 'https://example.com/new.png';
    fixture.element.querySelector('[data-role="image-alt"]').value = 'Updated';
    fixture.element.querySelector('[data-role="image-width"]').value = '';
    fixture.element.querySelector('[data-role="image-height"]').value = '';
    click(fixture.element.querySelector('[data-role="image-apply"]'));

    const updatedImage = fixture.surface.querySelector('img');
    expect(updatedImage.getAttribute('src')).toBe('https://example.com/new.png');
    expect(updatedImage.getAttribute('alt')).toBe('Updated');
    expect(updatedImage.hasAttribute('width')).toBe(false);
    expect(updatedImage.hasAttribute('height')).toBe(false);

    collapseBeforeNode(updatedImage);
    click(fixture.element.querySelector('[data-command="image"]'));
    const imageUrlInput = fixture.element.querySelector('[data-role="image-url"]');
    imageUrlInput.value = 'javascript:alert(1)';
    click(fixture.element.querySelector('[data-role="image-apply"]'));

    expect(document.activeElement).toBe(imageUrlInput);
    expect(fixture.surface.querySelector('img').getAttribute('src')).toBe('https://example.com/new.png');
  });

  test('link tool updates existing anchors and falls back to selected text or url when inserting', () => {
    const fixture = createEditorFixture();
    editorModule.onLoad(fixture.element, createDotNetRef());

    setSurfaceContent(fixture.surface, '<p><a href="https://example.com/old">Old</a></p>');
    const anchor = fixture.surface.querySelector('a');
    selectText(anchor.firstChild, 0, 3);
    click(fixture.element.querySelector('[data-command="link"]'));
    fixture.element.querySelector('[data-role="link-url"]').value = 'https://example.com/new';
    fixture.element.querySelector('[data-role="link-text"]').value = 'New';
    click(fixture.element.querySelector('[data-role="link-apply"]'));

    expect(anchor.getAttribute('href')).toBe('https://example.com/new');
    expect(anchor.textContent).toBe('New');

    setSurfaceContent(fixture.surface, '<p>Alpha Beta</p>');
    selectText(fixture.surface.querySelector('p').firstChild, 0, 5);
    click(fixture.element.querySelector('[data-command="link"]'));
    fixture.element.querySelector('[data-role="link-url"]').value = 'https://example.com/selected';
    fixture.element.querySelector('[data-role="link-text"]').value = '';
    click(fixture.element.querySelector('[data-role="link-apply"]'));
    expect(fixture.surface.querySelector('a').textContent).toBe('Alpha');

    setSurfaceContent(fixture.surface, '<p>Tail</p>');
    const collapsedRange = document.createRange();
    collapsedRange.setStart(fixture.surface.querySelector('p').firstChild, 4);
    collapsedRange.collapse(true);
    window.getSelection().removeAllRanges();
    window.getSelection().addRange(collapsedRange);

    click(fixture.element.querySelector('[data-command="link"]'));
    fixture.element.querySelector('[data-role="link-url"]').value = 'https://example.com/url';
    fixture.element.querySelector('[data-role="link-text"]').value = '';
    click(fixture.element.querySelector('[data-role="link-apply"]'));
    const links = fixture.surface.querySelectorAll('a');
    expect(links[links.length - 1].textContent).toBe('https://example.com/url');
  });

  test('table and iframe tools update existing nodes in place with preserved content and defaults', () => {
    const fixture = createEditorFixture();
    editorModule.onLoad(fixture.element, createDotNetRef());

    setSurfaceContent(
      fixture.surface,
      '<table data-border-color="#112233"><thead><tr><th>H1</th><th>H2</th></tr></thead><tbody><tr><td>A1</td><td>A2</td></tr></tbody></table>'
    );
    const table = fixture.surface.querySelector('table');
    selectText(table.querySelector('td').firstChild, 0, 1);
    click(fixture.element.querySelector('[data-command="table"]'));
    fixture.element.querySelector('[data-role="table-columns"]').value = '3';
    fixture.element.querySelector('[data-role="table-rows"]').value = '2';
    fixture.element.querySelector('[data-role="table-border-color"]').value = '#123456';
    click(fixture.element.querySelector('[data-role="table-apply"]'));

    const updatedTable = fixture.surface.querySelector('table');
    const headers = Array.from(updatedTable.querySelectorAll('th')).map((cell) => cell.textContent.trim());
    const rows = Array.from(updatedTable.querySelectorAll('tbody tr')).map((row) =>
      Array.from(row.querySelectorAll('td')).map((cell) => cell.textContent.trim())
    );
    expect(headers).toEqual(['H1', 'H2', 'Header 3']);
    expect(rows).toEqual([
      ['A1', 'A2', 'Cell 1-3'],
      ['Cell 2-1', 'Cell 2-2', 'Cell 2-3']
    ]);
    expect(updatedTable.getAttribute('data-border-color')).toBe('#123456');

    setSurfaceContent(fixture.surface, '<p><iframe src="https://example.com/old" title="Old" width="640" height="480"></iframe></p>');
    const iframe = fixture.surface.querySelector('iframe');
    collapseBeforeNode(iframe);
    click(fixture.element.querySelector('[data-command="iframe"]'));
    fixture.element.querySelector('[data-role="iframe-url"]').value = 'https://example.com/new';
    fixture.element.querySelector('[data-role="iframe-title"]').value = '';
    fixture.element.querySelector('[data-role="iframe-width"]').value = '';
    fixture.element.querySelector('[data-role="iframe-height"]').value = '';
    click(fixture.element.querySelector('[data-role="iframe-apply"]'));

    expect(iframe.getAttribute('src')).toBe('https://example.com/new');
    expect(iframe.getAttribute('title')).toBe('Embedded content');
    expect(iframe.getAttribute('width')).toBe('100%');
    expect(iframe.getAttribute('height')).toBe('315');
    expect(iframe.getAttribute('loading')).toBe('lazy');
  });

  test('text color tool normalizes nested same-color spans and blocks collapsed-range applies', () => {
    const fixture = createEditorFixture();
    editorModule.onLoad(fixture.element, createDotNetRef());

    setSurfaceContent(fixture.surface, '<p><span style="color:#123456"><span style="color:#123456">Blue</span></span></p>');
    selectText(fixture.surface.querySelector('span span').firstChild, 0, 4);
    click(fixture.element.querySelector('[data-command="textColor"]'));
    fixture.element.querySelector('[data-role="text-color-value"]').value = '#123456';
    click(fixture.element.querySelector('[data-role="text-color-apply"]'));

    const coloredSpans = fixture.surface.querySelectorAll('span[style*="color"]');
    expect(coloredSpans).toHaveLength(1);
    expect(coloredSpans[0].textContent).toBe('Blue');
    expect(coloredSpans[0].style.color).toBe('rgb(18, 52, 86)');

    setSurfaceContent(fixture.surface, '<p>Plain</p>');
    const plainText = fixture.surface.querySelector('p').firstChild;
    const collapsedRange = document.createRange();
    collapsedRange.setStart(plainText, 5);
    collapsedRange.collapse(true);
    window.getSelection().removeAllRanges();
    window.getSelection().addRange(collapsedRange);
    click(fixture.element.querySelector('[data-command="textColor"]'));
    const textColorInput = fixture.element.querySelector('[data-role="text-color-value"]');
    textColorInput.value = '#654321';
    click(fixture.element.querySelector('[data-role="text-color-apply"]'));

    expect(document.activeElement).toBe(textColorInput);
    expect(fixture.surface.innerHTML).toBe('<p>Plain</p>');
  });

  test('command routing handles toggles, multiple shortcut expressions, and tab list indentation', () => {
    const hooks = editorModule.__testHooks;
    const fixture = createEditorFixture({ bindOnInput: true, value: 'Start' });
    const dotNetRef = createDotNetRef();
    editorModule.onLoad(fixture.element, dotNetRef);

    setSurfaceContent(fixture.surface, '<blockquote><p>Quote</p></blockquote>');
    selectText(fixture.surface.querySelector('p').firstChild, 0, 5);
    document.execCommand.mockClear();
    hooks.executeEditorCommand(fixture.element, 'blockquote');
    expect(document.execCommand).toHaveBeenCalledWith('outdent', false);

    setSurfaceContent(fixture.surface, '<pre><code>const x = 1;</code></pre>');
    selectText(fixture.surface.querySelector('code').firstChild, 0, 5);
    hooks.executeEditorCommand(fixture.element, 'codeBlock');
    expect(document.execCommand).toHaveBeenCalledWith('formatBlock', false, 'P');

    hooks.executeEditorCommand(fixture.element, 'heading', '2');
    expect(document.execCommand).toHaveBeenCalledWith('formatBlock', false, 'H2');
    hooks.executeEditorCommand(fixture.element, 'alignCenter');
    expect(document.execCommand).toHaveBeenCalledWith('justifyCenter', false);
    hooks.executeEditorCommand(fixture.element, 'orderedList');
    expect(document.execCommand).toHaveBeenCalledWith('insertOrderedList', false);

    fixture.element.querySelector('.tnt-rich-text-editor-toolbar').innerHTML = `
      <button type="button" class="tnt-rich-text-editor-toolbar-button" data-command="bold" aria-keyshortcuts="Control+B" disabled></button>
      <button type="button" class="tnt-rich-text-editor-toolbar-button" data-command="italic" aria-keyshortcuts="Control+Alt+9 Control+Shift+9"></button>
      <button type="button" class="tnt-rich-text-editor-toolbar-button" data-command="underline"></button>
    `;
    editorModule.onUpdate(fixture.element, dotNetRef);
    fixture.element.querySelector('[data-command="bold"]').disabled = true;

    document.execCommand.mockClear();
    const handledShortcut = hooks.tryHandleShortcut(
      fixture.element,
      new KeyboardEvent('keydown', { key: '9', ctrlKey: true, altKey: true, bubbles: true, cancelable: true })
    );
    expect(handledShortcut).toBe(true);
    expect(document.execCommand).toHaveBeenCalledWith('italic', false);

    document.execCommand.mockClear();
    const ignoredShortcut = hooks.tryHandleShortcut(
      fixture.element,
      new KeyboardEvent('keydown', { key: 'B', ctrlKey: true, bubbles: true, cancelable: true })
    );
    expect(ignoredShortcut).toBe(false);
    expect(document.execCommand).not.toHaveBeenCalled();

    setSurfaceContent(fixture.surface, '<ul><li>Item</li></ul>');
    selectText(fixture.surface.querySelector('li').firstChild, 0, 4);
    document.execCommand.mockClear();
    hooks.handleEditorKeyDown(
      fixture.element,
      new KeyboardEvent('keydown', { key: 'Tab', bubbles: true, cancelable: true })
    );
    expect(document.execCommand).toHaveBeenCalledWith('indent', false);
    expect(dotNetRef.invokeMethodAsync).toHaveBeenCalledWith('UpdateValueFromJs', expect.any(String), '<ul><li>Item</li></ul>');

    document.execCommand.mockClear();
    hooks.handleEditorKeyDown(
      fixture.element,
      new KeyboardEvent('keydown', { key: 'Tab', shiftKey: true, bubbles: true, cancelable: true })
    );
    expect(document.execCommand).toHaveBeenCalledWith('outdent', false);
  });

  test('image and table tools support cancel, escape, enter, and invalid-input paths', () => {
    const fixture = createEditorFixture();
    editorModule.onLoad(fixture.element, createDotNetRef());

    setSurfaceContent(fixture.surface, '<p>Image host</p>');
    const imageText = fixture.surface.querySelector('p').firstChild;
    const imageRange = document.createRange();
    imageRange.setStart(imageText, imageText.textContent.length);
    imageRange.collapse(true);
    window.getSelection().removeAllRanges();
    window.getSelection().addRange(imageRange);

    click(fixture.element.querySelector('[data-command="image"]'));
    const imagePanel = fixture.element.querySelector('[data-tool-command="image"]');
    const imageUrlInput = fixture.element.querySelector('[data-role="image-url"]');
    const imageAltInput = fixture.element.querySelector('[data-role="image-alt"]');
    expect(imagePanel.hidden).toBe(false);

    click(fixture.element.querySelector('[data-role="image-cancel"]'));
    expect(imagePanel.hidden).toBe(true);
    expect(document.activeElement).toBe(fixture.surface);

    click(fixture.element.querySelector('[data-command="image"]'));
    keydown(imageUrlInput, 'Escape');
    expect(imagePanel.hidden).toBe(true);

    click(fixture.element.querySelector('[data-command="image"]'));
    imageUrlInput.value = 'https://example.com/enter.png';
    imageAltInput.value = 'Enter image';
    keydown(imageAltInput, 'Enter');
    expect(fixture.surface.querySelector('img').getAttribute('src')).toBe('https://example.com/enter.png');

    setSurfaceContent(fixture.surface, '<p>Table host</p>');
    const tableText = fixture.surface.querySelector('p').firstChild;
    const tableRange = document.createRange();
    tableRange.setStart(tableText, tableText.textContent.length);
    tableRange.collapse(true);
    window.getSelection().removeAllRanges();
    window.getSelection().addRange(tableRange);

    click(fixture.element.querySelector('[data-command="table"]'));
    const tablePanel = fixture.element.querySelector('[data-tool-command="table"]');
    const columnsInput = fixture.element.querySelector('[data-role="table-columns"]');
    const rowsInput = fixture.element.querySelector('[data-role="table-rows"]');
    expect(tablePanel.hidden).toBe(false);

    click(fixture.element.querySelector('[data-role="table-cancel"]'));
    expect(tablePanel.hidden).toBe(true);
    expect(document.activeElement).toBe(fixture.surface);

    click(fixture.element.querySelector('[data-command="table"]'));
    keydown(columnsInput, 'Escape');
    expect(tablePanel.hidden).toBe(true);

    click(fixture.element.querySelector('[data-command="table"]'));
    columnsInput.value = '';
    rowsInput.value = '2';
    click(fixture.element.querySelector('[data-role="table-apply"]'));
    expect(document.activeElement).toBe(columnsInput);

    columnsInput.value = '2';
    rowsInput.value = '2';
    keydown(rowsInput, 'Enter');
    expect(fixture.surface.querySelectorAll('table')).toHaveLength(1);
  });

  test('link, iframe, and text-color tools support escape, cancel, enter, and invalid apply branches', () => {
    const fixture = createEditorFixture();
    editorModule.onLoad(fixture.element, createDotNetRef());

    setSurfaceContent(fixture.surface, '<p>Alpha Beta</p>');
    selectText(fixture.surface.querySelector('p').firstChild, 0, 5);

    click(fixture.element.querySelector('[data-command="link"]'));
    const linkPanel = fixture.element.querySelector('[data-tool-command="link"]');
    const linkUrlInput = fixture.element.querySelector('[data-role="link-url"]');
    const linkTextInput = fixture.element.querySelector('[data-role="link-text"]');
    keydown(linkUrlInput, 'Escape');
    expect(linkPanel.hidden).toBe(true);

    click(fixture.element.querySelector('[data-command="link"]'));
    linkUrlInput.value = '';
    click(fixture.element.querySelector('[data-role="link-apply"]'));
    expect(document.activeElement).toBe(linkUrlInput);

    linkUrlInput.value = 'https://example.com/enter-link';
    linkTextInput.value = 'Entered';
    keydown(linkTextInput, 'Enter');
    expect(fixture.surface.querySelector('a').textContent).toBe('Entered');

    setSurfaceContent(fixture.surface, '<p>Iframe host</p>');
    const iframeText = fixture.surface.querySelector('p').firstChild;
    const iframeRange = document.createRange();
    iframeRange.setStart(iframeText, iframeText.textContent.length);
    iframeRange.collapse(true);
    window.getSelection().removeAllRanges();
    window.getSelection().addRange(iframeRange);

    click(fixture.element.querySelector('[data-command="iframe"]'));
    const iframePanel = fixture.element.querySelector('[data-tool-command="iframe"]');
    const iframeUrlInput = fixture.element.querySelector('[data-role="iframe-url"]');
    const iframeHeightInput = fixture.element.querySelector('[data-role="iframe-height"]');
    click(fixture.element.querySelector('[data-role="iframe-cancel"]'));
    expect(iframePanel.hidden).toBe(true);
    expect(document.activeElement).toBe(fixture.surface);

    click(fixture.element.querySelector('[data-command="iframe"]'));
    keydown(iframeUrlInput, 'Escape');
    expect(iframePanel.hidden).toBe(true);

    click(fixture.element.querySelector('[data-command="iframe"]'));
    iframeUrlInput.value = '';
    click(fixture.element.querySelector('[data-role="iframe-apply"]'));
    expect(document.activeElement).toBe(iframeUrlInput);

    iframeUrlInput.value = 'https://example.com/embed-enter';
    iframeHeightInput.value = '320';
    keydown(iframeHeightInput, 'Enter');
    expect(fixture.surface.querySelector('iframe').getAttribute('src')).toBe('https://example.com/embed-enter');

    setSurfaceContent(fixture.surface, '<p>Color me</p>');
    selectText(fixture.surface.querySelector('p').firstChild, 0, 5);
    click(fixture.element.querySelector('[data-command="textColor"]'));
    const textColorPanel = fixture.element.querySelector('[data-tool-command="textColor"]');
    const textColorInput = fixture.element.querySelector('[data-role="text-color-value"]');
    click(fixture.element.querySelector('[data-role="text-color-cancel"]'));
    expect(textColorPanel.hidden).toBe(true);
    expect(document.activeElement).toBe(fixture.surface);

    click(fixture.element.querySelector('[data-command="textColor"]'));
    keydown(textColorInput, 'Escape');
    expect(textColorPanel.hidden).toBe(true);

    click(fixture.element.querySelector('[data-command="textColor"]'));
    textColorInput.value = '#ff0000';
    keydown(textColorInput, 'Enter');
    expect(fixture.surface.querySelector('span[style*="color"]')).not.toBeNull();
  });
});
