function getFileInput(container) {
    if (!(container instanceof HTMLElement)) {
        return null;
    }

    const input = container.querySelector('input[type="file"]');
    return input instanceof HTMLInputElement ? input : null;
}

export function restoreFileNames(container, files) {
    const input = getFileInput(container);

    if (!input || typeof DataTransfer === 'undefined' || !Array.isArray(files) || files.length === 0) {
        return;
    }

    const dataTransfer = new DataTransfer();
    for (const file of files) {
        dataTransfer.items.add(new File([], file.name, {
            type: file.type ?? '',
            lastModified: file.lastModified ?? Date.now()
        }));
    }

    input.files = dataTransfer.files;
}

export function removeSelectedFile(container, fileIndex) {
    const input = getFileInput(container);
    const files = input?.files;

    if (!input || !files || typeof DataTransfer === 'undefined' || !Number.isInteger(fileIndex)) {
        return;
    }

    const dataTransfer = new DataTransfer();
    let removed = false;
    for (let i = 0; i < files.length; i += 1) {
        if (!removed && i === fileIndex) {
            removed = true;
            continue;
        }
        dataTransfer.items.add(files[i]);
    }

    if (!removed) {
        return;
    }

    input.files = dataTransfer.files;
}
