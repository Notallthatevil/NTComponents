// Test the TnTMarkdownEditor JavaScript module exports
// Note: This module uses dynamic CDN imports that cannot be easily tested in Jest
// This test verifies the module structure and basic functionality without the CDN dependencies

import { readFileSync, existsSync } from 'fs';
import { join, dirname } from 'path';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

describe('TnTMarkdownEditor JavaScript Module Structure', () => {
  test('module file exists and can be accessed', () => {
    // This test verifies the file exists and has the expected structure
    // The actual CDN imports (EasyMDE, highlight.js) prevent full module testing in Jest
    
    // We can at least verify the structure by checking the file exists
    const moduleFile = join(__dirname, '..', 'TnTMarkdownEditor.razor.js');
    
    expect(existsSync(moduleFile)).toBe(true);
    
    const fileContent = readFileSync(moduleFile, 'utf8');
    
    // Verify expected exports exist in the file
    expect(fileContent).toContain('export function onLoad');
    expect(fileContent).toContain('export function onUpdate');
    expect(fileContent).toContain('export function onDispose');
    
    // Verify key functionality is present
    expect(fileContent).toContain('customElements.define');
    expect(fileContent).toContain('tnt-markdown-editor');
    expect(fileContent).toContain('EasyMDE');
    expect(fileContent).toContain('attributeChangedCallback');
    expect(fileContent).toContain('editor-value');
    expect(fileContent).toContain('editor-revision');
    expect(fileContent).toContain('setEditorValue');
    
    // Verify custom toolbar items are defined
    expect(fileContent).toContain('left-text');
    expect(fileContent).toContain('center-text');
    expect(fileContent).toContain('right-text');
    
    // Verify text transformations are present
    expect(fileContent).toContain('tnt-left');
    expect(fileContent).toContain('tnt-center');
    expect(fileContent).toContain('tnt-right');
  });

  test('module contains expected CSS loading logic', () => {
    const moduleFile = join(__dirname, '..', 'TnTMarkdownEditor.razor.js');
    const fileContent = readFileSync(moduleFile, 'utf8');
    
    // Verify CSS loading logic
    expect(fileContent).toContain('highlight.js/latest/styles/github.min.css');
    expect(fileContent).toContain('easymde/dist/easymde.min.css');
    expect(fileContent).toContain('document.createElement(\'link\')');
    expect(fileContent).toContain('link.rel = \'stylesheet\'');
  });

  test('module contains expected EasyMDE configuration', () => {
    const moduleFile = join(__dirname, '..', 'TnTMarkdownEditor.razor.js');
    const fileContent = readFileSync(moduleFile, 'utf8');
    
    // Verify EasyMDE configuration options
    expect(fileContent).toContain('sideBySideFullscreen: false');
    expect(fileContent).toContain('previewRender:');
    expect(fileContent).toContain('toolbar:');
    
    // Verify custom alignment functionality
    expect(fileContent).toContain('<tnt-left>');
    expect(fileContent).toContain('<tnt-center>');
    expect(fileContent).toContain('<tnt-right>');
    
    // Verify change handler setup
    expect(fileContent).toContain('codemirror.on("change"');
    expect(fileContent).toContain('UpdateValue');
    expect(fileContent).toContain('isSynchronizing');
    expect(fileContent).toContain('localRevision');
    expect(fileContent).toContain('shouldSkipServerSync');
  });

  test('module handles dotNetRef mapping', () => {
    const moduleFile = join(__dirname, '..', 'TnTMarkdownEditor.razor.js');
    const fileContent = readFileSync(moduleFile, 'utf8');
    
    // Verify dotNetRef mapping functionality
    expect(fileContent).toContain('elementDotNetRefMap');
    expect(fileContent).toContain('markdownEditorsMap');
    expect(fileContent).toContain('NTComponents.customAttribute');
    expect(fileContent).toContain('hasFocus');
  });
});

// Note: Comprehensive integration testing is done in the C# test suite
// The JavaScript module's CDN imports make it challenging to test in isolation
// without complex mocking infrastructure. The C# tests cover the component
// behavior comprehensively from the Blazor perspective.
