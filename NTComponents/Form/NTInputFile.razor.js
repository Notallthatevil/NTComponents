function getFileInput(container) {
    if (!(container instanceof HTMLElement)) {
        return null;
    }

    const input = container.querySelector('input[type="file"]');
    return input instanceof HTMLInputElement ? input : null;
}

export function removeSelectedFile(container, removedIndex) {
    const input = getFileInput(container);
    const files = input?.files;

    if (!input || !files || removedIndex < 0 || removedIndex >= files.length || typeof DataTransfer === 'undefined') {
        return;
    }

    const dataTransfer = new DataTransfer();
    for (let i = 0; i < files.length; i += 1) {
        if (i !== removedIndex) {
            dataTransfer.items.add(files[i]);
        }
    }

    input.files = dataTransfer.files;
}
