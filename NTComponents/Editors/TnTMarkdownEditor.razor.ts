import * as EasyMDEImport from "https://unpkg.com/easymde/dist/easymde.min.js";
import * as HighlightImport from "https://cdn.jsdelivr.net/highlight.js/latest/highlight.min.js";

const highlightJsCss = 'https://cdn.jsdelivr.net/highlight.js/latest/styles/github.min.css';
const easyMDECss = 'https://unpkg.com/easymde/dist/easymde.min.css';

const markdownEditorsMap = new Map();
const elementDotNetRefMap = new Map();

function getEditorKey(element) {
    return element?.getAttribute(NTComponents.customAttribute);
}

function getEditorValue(element) {
    const valueElement = element?.querySelector('.editor-value');
    return valueElement?.textContent ?? '';
}

function getEditorRevision(element) {
    const revisionElement = element?.querySelector('.editor-revision');
    const revision = Number.parseInt(revisionElement?.textContent ?? '0', 10);
    return Number.isNaN(revision) ? 0 : revision;
}

function isEditorFocused(editorState) {
    const codeMirror = editorState?.mde?.codemirror;
    if (!codeMirror) {
        return false;
    }

    if (typeof codeMirror.hasFocus === 'function' && codeMirror.hasFocus()) {
        return true;
    }

    const wrapper = typeof codeMirror.getWrapperElement === 'function'
        ? codeMirror.getWrapperElement()
        : null;

    return !!wrapper?.contains(document.activeElement);
}

function shouldSkipServerSync(editorState, nextValue, nextRevision) {
    if (!editorState?.mde || !isEditorFocused(editorState)) {
        return false;
    }

    const currentValue = editorState.mde.value();
    const currentRevision = editorState.localRevision ?? 0;
    return nextRevision < currentRevision && nextValue !== currentValue;
}

function setEditorValue(editorState, value) {
    if (!editorState?.mde) {
        return;
    }

    const nextValue = value ?? '';
    if (editorState.mde.value() === nextValue) {
        return;
    }

    editorState.isSynchronizing = true;
    try {
        editorState.mde.value(nextValue);
    } finally {
        editorState.isSynchronizing = false;
    }
}

export function onLoad(element, dotNetElementRef) {
    const highlightLink = document.querySelector(`link[rel="stylesheet"][href*="${highlightJsCss}"]`);
    if (!highlightLink) {
        const link = document.createElement('link');
        link.rel = 'stylesheet';
        link.href = highlightJsCss;
        document.head.appendChild(link);
    }

    const easyMDELink = document.querySelector(`link[rel="stylesheet"][href*="${easyMDECss}"]`);
    if (!easyMDELink) {
        const link = document.createElement('link');
        link.rel = 'stylesheet';
        link.href = easyMDECss;
        document.head.appendChild(link);
    }

    if (!customElements.get('tnt-markdown-editor')) {
        customElements.define('tnt-markdown-editor', class extends HTMLElement {
            static observedAttributes = [NTComponents.customAttribute];

            // We use attributeChangedCallback instead of connectedCallback
            // because a page-script element might get reused between enhanced
            // navigations.
            attributeChangedCallback(name, oldValue, newValue) {
                if (name !== NTComponents.customAttribute) {
                    return;
                }

                if (elementDotNetRefMap.get(oldValue)) {
                    elementDotNetRefMap.set(newValue, elementDotNetRefMap.get(oldValue));
                    elementDotNetRefMap.delete(oldValue);
                }

                let easyMDE = null;
                let localRevision = getEditorRevision(this);

                const existingState = markdownEditorsMap.get(oldValue);
                if (existingState) {
                    easyMDE = existingState.mde;
                    localRevision = existingState.localRevision ?? localRevision;
                    markdownEditorsMap.delete(oldValue);
                }

                if (easyMDE === null) {
                    let child = this.querySelector('textarea');
                    if (!child) {
                        child = document.createElement('textarea');
                        this.appendChild(child);
                    }

                    let self = this;
                    easyMDE = new EasyMDE({
                        element: child,
                        initialValue: getEditorValue(this),
                        sideBySideFullscreen: false,
                        previewRender: (text) => {
                            const key = getEditorKey(self);
                            const editorState = key ? markdownEditorsMap.get(key) : null;
                            const editor = editorState?.mde;
                            if (editor && editor.markdown) {
                                text = editor.markdown(text);
                            }

                            text = text.replace(/<tnt-left>(.+)?<\/tnt-left>/g, '<div style="text-align:left">$1</div>');
                            text = text.replace(/<tnt-center>(.+)?<\/tnt-center>/g, '<div style="text-align:center">$1</div>');
                            text = text.replace(/<tnt-right>(.+)?<\/tnt-right>/g, '<div style="text-align:right">$1</div>');
                            text = text.replace(/<table>/, '<table style="width:100%">');
                            return text;
                        },
                        toolbar: [
                            "bold",
                            "italic",
                            "strikethrough",
                            "|",
                            {
                                name: "left-text",
                                action: (editor) => {
                                    let cm = editor.codemirror;
                                    var output = '';
                                    var selectedText = cm.getSelection();
                                    var text = selectedText || 'align-left';

                                    output = '<tnt-left>' + text + '</tnt-left>';
                                    cm.replaceSelection(output);
                                },
                                className: "fa fa-align-left",
                                text: "",
                                title: "Align Left"
                            },
                            {
                                name: "center-text",
                                action: (editor) => {
                                    let cm = editor.codemirror;
                                    var output = '';
                                    var selectedText = cm.getSelection();
                                    var text = selectedText || 'align-center';

                                    output = '<tnt-center>' + text + '</tnt-center>';
                                    cm.replaceSelection(output);
                                },
                                className: "fa fa-align-center",
                                text: "",
                                title: "Align Center"
                            },
                            {
                                name: "right-text",
                                action: (editor) => {
                                    let cm = editor.codemirror;
                                    var output = '';
                                    var selectedText = cm.getSelection();
                                    var text = selectedText || 'align-right';

                                    output = '<tnt-right>' + text + '</tnt-right>';
                                    cm.replaceSelection(output);
                                },
                                className: "fa fa-align-right",
                                text: "",
                                title: "Align Right"
                            },
                            "|",
                            "heading",
                            "heading-smaller",
                            "heading-bigger",
                            "heading-1",
                            "heading-2",
                            "heading-3",
                            "|",
                            "code",
                            "|",
                            "quote",
                            "|",
                            "unordered-list",
                            "ordered-list",
                            "|",
                            "clean-block",
                            "|",
                            "link",
                            "image",
                            "upload-image",
                            "|",
                            "table",
                            "|",
                            "horizontal-rule",
                            "|",
                            "preview",
                            "side-by-side",
                            "|",
                            "guide",
                            "|",
                            "undo",
                            "redo"
                        ]
                    });
                    if (easyMDE.codemirror && easyMDE.codemirror.on) {
                        easyMDE.codemirror.on("change", function () {
                            const key = getEditorKey(self);
                            const editorState = key ? markdownEditorsMap.get(key) : null;
                            if (editorState?.isSynchronizing) {
                                return;
                            }

                            var text = easyMDE.value();
                            editorState.localRevision = (editorState.localRevision ?? 0) + 1;
                            const revision = editorState.localRevision;
                            const dotNetRef = key ? elementDotNetRefMap.get(key) : null;
                            if (dotNetRef) {
                                dotNetRef.invokeMethodAsync("UpdateValue", text, easyMDE.options.previewRender(text), revision);
                            }
                        });
                        easyMDE.codemirror.on("blur", function () {
                            const key = getEditorKey(self);
                            const dotNetRef = key ? elementDotNetRefMap.get(key) : null;
                            if (dotNetRef) {
                                dotNetRef.invokeMethodAsync("HandleBlurAsync");
                            }
                        });
                    }
                }

                markdownEditorsMap.set(newValue, {
                    element: this,
                    localRevision,
                    mde: easyMDE,
                    isSynchronizing: false
                });
            }

            disconnectedCallback() {
                let attribute = this.getAttribute(NTComponents.customAttribute);
                if (markdownEditorsMap.get(attribute)) {
                    markdownEditorsMap.delete(attribute);
                }
            }
        });
    }
}

export function onUpdate(element, dotNetElementRef) {
    if (element && dotNetElementRef) {
        const key = getEditorKey(element);

        if (elementDotNetRefMap.get(key)) {
            elementDotNetRefMap.delete(key);
        }
        elementDotNetRefMap.set(key, dotNetElementRef);

        const editorState = markdownEditorsMap.get(key);
        if (editorState) {
            const nextValue = getEditorValue(element);
            const nextRevision = getEditorRevision(element);
            if (shouldSkipServerSync(editorState, nextValue, nextRevision)) {
                return;
            }

            editorState.localRevision = Math.max(editorState.localRevision ?? 0, nextRevision);
            setEditorValue(editorState, nextValue);
        }
    }
}

export function onDispose(element, dotNetElementRef) {
    if (element) {
        const key = getEditorKey(element);
        elementDotNetRefMap.delete(key);
    }
}
