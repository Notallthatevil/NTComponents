import { jest } from '@jest/globals';
import { init, updateRenderState } from '../../NTComponents/Virtualization/NTVirtualize.razor.js';

describe('NTVirtualize.init', () => {
    let intersectionObservers;
    let mutationObservers;
    let rangeHeight;
    let innerHeightDescriptor;

    beforeEach(() => {
        document.body.innerHTML = '';
        intersectionObservers = [];
        mutationObservers = [];
        rangeHeight = 400;
        innerHeightDescriptor = Object.getOwnPropertyDescriptor(window, 'innerHeight');

        global.IntersectionObserver = class {
            constructor(callback, options) {
                this.callback = callback;
                this.options = options;
                this.observe = jest.fn();
                this.unobserve = jest.fn();
                this.disconnect = jest.fn();
                intersectionObservers.push(this);
            }
        };

        global.MutationObserver = class {
            constructor(callback) {
                this.callback = callback;
                this.observe = jest.fn();
                this.disconnect = jest.fn();
                mutationObservers.push(this);
            }
        };

        jest.spyOn(document, 'createRange').mockImplementation(() => ({
            setStartAfter: jest.fn(),
            setEndBefore: jest.fn(),
            getBoundingClientRect: () => ({ height: rangeHeight }),
        }));

        Object.defineProperty(window, 'innerHeight', {
            configurable: true,
            writable: true,
            value: 500,
        });

        document.documentElement.scrollTop = 0;
        document.body.scrollTop = 0;
    });

    afterEach(() => {
        document.createRange.mockRestore();

        if (innerHeightDescriptor) {
            Object.defineProperty(window, 'innerHeight', innerHeightDescriptor);
        }

        delete global.IntersectionObserver;
        delete global.MutationObserver;
    });

    test('uses the viewport when an ancestor is overflow hidden but not scrollable', () => {
        const { topSpacer, bottomSpacer, dotNetRef } = createVirtualizedElements('hidden');

        init(dotNetRef, topSpacer, bottomSpacer, 20, 1, 100);

        expect(intersectionObservers).toHaveLength(1);
        expect(intersectionObservers[0].options.root).toBeNull();
    });

    test('computes visible range relative to the list position when the body scrolls', () => {
        const { topSpacer, bottomSpacer, dotNetRef } = createVirtualizedElements();

        topSpacer.getBoundingClientRect = jest.fn(() => ({ top: -200 }));
        document.documentElement.scrollTop = 1200;
        document.body.scrollTop = 1200;

        init(dotNetRef, topSpacer, bottomSpacer, 20, 1, 100);
        updateRenderState(dotNetRef, 100, 0, 0);

        intersectionObservers[0].callback([
            { target: bottomSpacer, isIntersecting: true },
        ]);

        expect(dotNetRef.invokeMethodAsync).toHaveBeenCalledWith('LoadItems', 180, 1280, 9, 27);
    });

    function createVirtualizedElements(ancestorOverflowY = 'visible') {
        const scrollAncestor = document.createElement('div');
        scrollAncestor.style.overflowY = ancestorOverflowY;

        const container = document.createElement('div');
        const topSpacer = document.createElement('div');
        const content = document.createElement('div');
        const bottomSpacer = document.createElement('div');

        scrollAncestor.appendChild(container);
        container.appendChild(topSpacer);
        container.appendChild(content);
        container.appendChild(bottomSpacer);
        document.body.appendChild(scrollAncestor);

        topSpacer.getBoundingClientRect = jest.fn(() => ({ top: 0 }));
        bottomSpacer.getBoundingClientRect = jest.fn(() => ({ top: 400 }));
        scrollAncestor.getBoundingClientRect = jest.fn(() => ({ top: 0 }));

        const dotNetRef = {
            _callDispatcher: {},
            _id: 'virtualize-test',
            invokeMethodAsync: jest.fn(),
            dispose: jest.fn(),
        };

        return { topSpacer, bottomSpacer, dotNetRef };
    }
});
