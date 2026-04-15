import {
    registerRichTextEditorTool
} from '../../wwwroot/NTComponents.lib.module.js';
import type {
    InlineToolCloseOptions,
    Maybe,
    RichTextEditorToolContext
} from '../Core/NTRichTextEditorToolRegistry.js';

interface ImageDetails {
    src: string;
    alt: string;
    width: string;
    height: string;
}

interface ImageToolState {
    imageEditorInputs: HTMLInputElement[];
    imageFileInput: HTMLInputElement | null;
    imageApplyButton: HTMLButtonElement | null;
    imageCancelButton: HTMLButtonElement | null;
    imageTarget: HTMLImageElement | null;
    onImageFileChange?: (event: Event) => void;
    onImageApply?: (event: MouseEvent) => void;
    onImageCancel?: (event: MouseEvent) => void;
    onImageEditorKeyDown?: (event: KeyboardEvent) => void;
}

function qs<T extends Element>(root: Maybe<ParentNode>, selector: string): T | null {
    return (root?.querySelector?.(selector) as T | null) ?? null;
}

function isHtmlInputElement(value: unknown): value is HTMLInputElement {
    return value instanceof HTMLInputElement;
}

function getImageEditor(element: Maybe<ParentNode>): HTMLElement | null {
    return qs<HTMLElement>(element, '[data-tool-command="image"]');
}

function getImageUrlInput(element: Maybe<ParentNode>): HTMLInputElement | null {
    return qs<HTMLInputElement>(element, '[data-role="image-url"]');
}

function getImageFileInput(element: Maybe<ParentNode>): HTMLInputElement | null {
    return qs<HTMLInputElement>(element, '[data-role="image-file"]');
}

function getImageAltInput(element: Maybe<ParentNode>): HTMLInputElement | null {
    return qs<HTMLInputElement>(element, '[data-role="image-alt"]');
}

function getImageWidthInput(element: Maybe<ParentNode>): HTMLInputElement | null {
    return qs<HTMLInputElement>(element, '[data-role="image-width"]');
}

function getImageHeightInput(element: Maybe<ParentNode>): HTMLInputElement | null {
    return qs<HTMLInputElement>(element, '[data-role="image-height"]');
}

function getImageApplyButton(element: Maybe<ParentNode>): HTMLButtonElement | null {
    return qs<HTMLButtonElement>(element, '[data-role="image-apply"]');
}

function getImageCancelButton(element: Maybe<ParentNode>): HTMLButtonElement | null {
    return qs<HTMLButtonElement>(element, '[data-role="image-cancel"]');
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

function normalizeSafeImageUrl(value: unknown): string {
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

    return ['http', 'https', 'data', 'blob'].includes(scheme) ? trimmed : '';
}

function escapeHtml(value: string): string {
    return value
        .replaceAll('&', '&amp;')
        .replaceAll('<', '&lt;')
        .replaceAll('>', '&gt;')
        .replaceAll('"', '&quot;')
        .replaceAll('\'', '&#39;');
}

function normalizeImageDimension(value: unknown): string {
    const trimmed = `${value ?? ''}`.trim();
    return /^\d+$/.test(trimmed) ? trimmed : '';
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

function buildImageHtml({ src, alt = '', width = '', height = '' }: ImageDetails): string {
    const normalizedSrc = normalizeSafeImageUrl(src);
    if (!normalizedSrc) {
        return '';
    }

    const normalizedWidth = normalizeImageDimension(width);
    const normalizedHeight = normalizeImageDimension(height);
    const widthAttribute = normalizedWidth ? ` width="${escapeHtml(normalizedWidth)}"` : '';
    const heightAttribute = normalizedHeight ? ` height="${escapeHtml(normalizedHeight)}"` : '';
    return `<img src="${escapeHtml(normalizedSrc)}" alt="${escapeHtml(alt)}"${widthAttribute}${heightAttribute} />`;
}

function initializeImageToolStateHandlers(context: RichTextEditorToolContext<ImageToolState>): void {
    const { element, editorState, host, toolState } = context;
    if (toolState.onImageApply) {
        return;
    }

    toolState.onImageFileChange = (event: Event) => {
        const target = event.target instanceof HTMLInputElement ? event.target : null;
        const file = target?.files?.[0] ?? null;
        handleImageFileSelection(element, file);
    };

    toolState.onImageApply = (event: MouseEvent) => {
        event.preventDefault();
        applyImageEditor(context);
    };

    toolState.onImageCancel = (event: MouseEvent) => {
        event.preventDefault();
        closeImageEditor(context, { focusSurface: true });
        host.updateToolbarState(element);
    };

    toolState.onImageEditorKeyDown = (event: KeyboardEvent) => {
        if (event.key === 'Escape') {
            event.preventDefault();
            closeImageEditor(context, { focusSurface: true });
            host.updateToolbarState(element);
            return;
        }

        const target = event.target instanceof HTMLInputElement ? event.target : null;
        if (event.key === 'Enter' && target?.type !== 'file') {
            event.preventDefault();
            applyImageEditor(context);
        }
    };
}

function bindImageToolControls(context: RichTextEditorToolContext<ImageToolState>): void {
    const { element, toolState } = context;
    initializeImageToolStateHandlers(context);
    unbindImageToolControls(context);
    const imageFileChangeHandler = toolState.onImageFileChange;
    const imageApplyHandler = toolState.onImageApply;
    const imageCancelHandler = toolState.onImageCancel;
    const imageEditorKeyDownHandler = toolState.onImageEditorKeyDown;

    toolState.imageFileInput = getImageFileInput(element);
    toolState.imageApplyButton = getImageApplyButton(element);
    toolState.imageCancelButton = getImageCancelButton(element);
    toolState.imageEditorInputs = [
        getImageUrlInput(element),
        getImageAltInput(element),
        getImageWidthInput(element),
        getImageHeightInput(element)
    ].filter(isHtmlInputElement);

    if (imageFileChangeHandler) {
        toolState.imageFileInput?.addEventListener('change', imageFileChangeHandler);
    }

    if (imageApplyHandler) {
        toolState.imageApplyButton?.addEventListener('click', imageApplyHandler);
    }

    if (imageCancelHandler) {
        toolState.imageCancelButton?.addEventListener('click', imageCancelHandler);
    }

    if (imageEditorKeyDownHandler) {
        for (const input of toolState.imageEditorInputs) {
            input.addEventListener('keydown', imageEditorKeyDownHandler);
        }
    }
}

function unbindImageToolControls(context: RichTextEditorToolContext<ImageToolState>): void {
    const { toolState } = context;
    if (toolState.onImageFileChange) {
        toolState.imageFileInput?.removeEventListener('change', toolState.onImageFileChange);
    }

    if (toolState.onImageApply) {
        toolState.imageApplyButton?.removeEventListener('click', toolState.onImageApply);
    }

    if (toolState.onImageCancel) {
        toolState.imageCancelButton?.removeEventListener('click', toolState.onImageCancel);
    }

    if (toolState.onImageEditorKeyDown) {
        for (const input of toolState.imageEditorInputs ?? []) {
            input.removeEventListener('keydown', toolState.onImageEditorKeyDown);
        }
    }
}

function closeImageEditor(context: RichTextEditorToolContext<ImageToolState>, { focusSurface = false, preserveSelection = false }: InlineToolCloseOptions = {}): void {
    const { element, editorState, host, toolState } = context;
    host.setToolPanelOpen(element, 'image', false);
    toolState.imageTarget = null;
    if (!preserveSelection) {
        editorState.selectionRange = null;
    }

    if (toolState.imageFileInput) {
        toolState.imageFileInput.value = '';
    }

    if (focusSurface) {
        host.getSurface(element)?.focus();
    }
}

function openImageEditor(
    context: RichTextEditorToolContext<ImageToolState>,
    { focusInput = true, selectInputText = true }: { focusInput?: boolean; selectInputText?: boolean } = {}
): boolean {
    const { element, editorState, host, toolState } = context;
    const surface = host.getSurface(element);
    const imageEditor = getImageEditor(element);
    if (!surface || !imageEditor) {
        return false;
    }

    host.closeOtherTools(element, editorState, 'image');

    const selectionElement = host.getSelectionElement(surface);
    const existingImageCandidate = selectionElement?.closest?.('img') ?? null;
    const existingImage = existingImageCandidate instanceof HTMLImageElement ? existingImageCandidate : null;
    const imageDetails = getImageDetails(existingImage);

    host.saveSelectionRange(surface, editorState);
    toolState.imageTarget = existingImage;

    const imageUrlInput = getImageUrlInput(element);
    if (imageUrlInput) {
        imageUrlInput.value = imageDetails.src;
    }

    const imageAltInput = getImageAltInput(element);
    if (imageAltInput) {
        imageAltInput.value = imageDetails.alt;
    }

    const imageWidthInput = getImageWidthInput(element);
    if (imageWidthInput) {
        imageWidthInput.value = imageDetails.width;
    }

    const imageHeightInput = getImageHeightInput(element);
    if (imageHeightInput) {
        imageHeightInput.value = imageDetails.height;
    }

    if (toolState.imageFileInput) {
        toolState.imageFileInput.value = '';
    }

    host.setToolPanelOpen(element, 'image', true);

    if (focusInput) {
        imageUrlInput?.focus();
        if (selectInputText) {
            imageUrlInput?.select();
        }
    }

    return false;
}

function applyImageEditor(context: RichTextEditorToolContext<ImageToolState>): boolean {
    const { element, editorState, host, toolState } = context;
    const surface = host.getSurface(element);
    if (!surface) {
        return false;
    }

    const imageUrl = normalizeSafeImageUrl(getImageUrlInput(element)?.value ?? '');
    if (!imageUrl) {
        getImageUrlInput(element)?.focus();
        return false;
    }

    const imageAlt = getImageAltInput(element)?.value?.trim?.() ?? '';
    const imageWidth = normalizeImageDimension(getImageWidthInput(element)?.value ?? '');
    const imageHeight = normalizeImageDimension(getImageHeightInput(element)?.value ?? '');
    const imageTarget = toolState.imageTarget;

    if (imageTarget instanceof HTMLImageElement && surface.contains(imageTarget)) {
        imageTarget.setAttribute('src', imageUrl);
        imageTarget.setAttribute('alt', imageAlt);

        if (imageWidth) {
            imageTarget.setAttribute('width', imageWidth);
        } else {
            imageTarget.removeAttribute('width');
        }

        if (imageHeight) {
            imageTarget.setAttribute('height', imageHeight);
        } else {
            imageTarget.removeAttribute('height');
        }
    } else {
        host.restoreSelectionRange(surface, editorState);
        const imageHtml = buildImageHtml({
            src: imageUrl,
            alt: imageAlt,
            width: imageWidth,
            height: imageHeight
        } satisfies ImageDetails);
        if (!imageHtml) {
            getImageUrlInput(element)?.focus();
            return false;
        }

        document.execCommand('insertHTML', false, imageHtml);
    }

    closeImageEditor(context, { focusSurface: true });
    return true;
}

function handleImageFileSelection(element: HTMLElement, file: File | null): void {
    if (!(file instanceof File)) {
        return;
    }

    const reader = new FileReader();
    reader.onload = () => {
        const imageUrlInput = getImageUrlInput(element);
        if (typeof reader.result === 'string' && imageUrlInput) {
            imageUrlInput.value = reader.result;
            const imageAltInput = getImageAltInput(element);
            if (imageAltInput && !imageAltInput.value.trim()) {
                imageAltInput.value = file.name.replace(/\.[^.]+$/, '');
            }
        }
    };

    reader.readAsDataURL(file);
}

registerRichTextEditorTool<ImageToolState>({
    command: 'image',
    createState: () => ({
        imageEditorInputs: [],
        imageFileInput: null,
        imageApplyButton: null,
        imageCancelButton: null,
        imageTarget: null,
        onImageFileChange: undefined,
        onImageApply: undefined,
        onImageCancel: undefined,
        onImageEditorKeyDown: undefined
    }),
    bind: bindImageToolControls,
    unbind: unbindImageToolControls,
    setDisabled: ({ toolState }, disabled) => {
        toolState.imageEditorInputs.forEach((input) => setDisabled(input, disabled));
        setDisabled(toolState.imageFileInput, disabled);
        setDisabled(toolState.imageApplyButton, disabled);
        setDisabled(toolState.imageCancelButton, disabled);
    },
    execute: (context) => openImageEditor(context),
    close: closeImageEditor,
    syncState: (context) => {
        const { element, host } = context;
        const surface = host.getSurface(element);
        const selectionElement = host.getSelectionElement(surface);
        const existingImage = selectionElement?.closest?.('img') instanceof HTMLImageElement;
        const focusedToolCommand = host.getFocusedToolCommand(element);

        host.setToolbarButtonPressed(element, 'image', existingImage);

        if (focusedToolCommand === 'image') {
            return;
        }

        if (focusedToolCommand || !existingImage) {
            closeImageEditor(context, { preserveSelection: Boolean(focusedToolCommand) });
            return;
        }

        openImageEditor(context, { focusInput: false, selectInputText: false });
    }
});
