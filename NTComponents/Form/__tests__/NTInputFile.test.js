/**
 * @jest-environment jsdom
 */
import { removeSelectedFile } from '../NTInputFile.razor.js';

function createFileList(files) {
    return Object.assign([...files], {
        item: index => files[index] ?? null,
    });
}

function createFixture(fileNames) {
    const container = document.createElement('span');
    const input = document.createElement('input');
    input.type = 'file';

    let currentFiles = createFileList(fileNames.map(name => new File(['content'], name, { type: 'text/plain' })));
    let assignmentCount = 0;

    Object.defineProperty(input, 'files', {
        configurable: true,
        get: () => currentFiles,
        set: value => {
            assignmentCount += 1;
            currentFiles = value;
        },
    });

    container.appendChild(input);
    document.body.appendChild(container);

    return {
        container,
        getAssignmentCount: () => assignmentCount,
        getFileNames: () => Array.from(currentFiles, file => file.name),
    };
}

describe('NTInputFile removeSelectedFile', () => {
    let originalDataTransfer;
    let originalFile;

    beforeEach(() => {
        document.body.innerHTML = '';

        originalDataTransfer = global.DataTransfer;
        originalFile = global.File;

        if (global.File === undefined) {
            global.File = class FakeFile {
                constructor(parts, name, options = {}) {
                    this.name = name;
                    this.type = options.type || '';
                    this.lastModified = options.lastModified || Date.now();
                    this.parts = parts;
                }
            };
        }

        global.DataTransfer = class FakeDataTransfer {
            constructor() {
                const files = [];

                this.items = {
                    add: file => files.push(file),
                };

                Object.defineProperty(this, 'files', {
                    enumerable: true,
                    get: () => createFileList(files),
                });
            }
        };
    });

    afterEach(() => {
        document.body.innerHTML = '';

        if (originalDataTransfer === undefined) {
            delete global.DataTransfer;
        }
        else {
            global.DataTransfer = originalDataTransfer;
        }

        if (originalFile === undefined) {
            delete global.File;
        }
        else {
            global.File = originalFile;
        }
    });

    test('removes the selected file and preserves the remaining order', () => {
        const fixture = createFixture(['first.txt', 'second.txt', 'third.txt']);

        removeSelectedFile(fixture.container, 1);

        expect(fixture.getFileNames()).toEqual(['first.txt', 'third.txt']);
        expect(fixture.getAssignmentCount()).toBe(1);
    });

    test('can remove the last remaining file from the native file list', () => {
        const fixture = createFixture(['only.txt']);

        removeSelectedFile(fixture.container, 0);

        expect(fixture.getFileNames()).toEqual([]);
        expect(fixture.getAssignmentCount()).toBe(1);
    });

    test('does nothing when the removed index is outside the native file list bounds', () => {
        const fixture = createFixture(['first.txt', 'second.txt']);

        removeSelectedFile(fixture.container, 4);

        expect(fixture.getFileNames()).toEqual(['first.txt', 'second.txt']);
        expect(fixture.getAssignmentCount()).toBe(0);
    });

    test('does nothing when DataTransfer is unavailable', () => {
        const fixture = createFixture(['first.txt', 'second.txt']);
        delete global.DataTransfer;

        removeSelectedFile(fixture.container, 1);

        expect(fixture.getFileNames()).toEqual(['first.txt', 'second.txt']);
        expect(fixture.getAssignmentCount()).toBe(0);
    });

    test('removes the correct file when multiple selected files share the same name', () => {
        const container = document.createElement('span');
        const input = document.createElement('input');
        input.type = 'file';

        const first = new File(['a'], 'duplicate.txt', { type: 'text/plain' });
        const second = new File(['b'], 'duplicate.txt', { type: 'text/plain' });
        const third = new File(['c'], 'third.txt', { type: 'text/plain' });
        let currentFiles = createFileList([first, second, third]);

        Object.defineProperty(input, 'files', {
            configurable: true,
            get: () => currentFiles,
            set: value => {
                currentFiles = value;
            },
        });

        container.appendChild(input);
        document.body.appendChild(container);

        removeSelectedFile(container, 1);

        expect(Array.from(currentFiles, file => file.name)).toEqual(['duplicate.txt', 'third.txt']);
    });
});
