import { jest } from '@jest/globals';
import { init, onDispose, updateRenderState } from '../../../NTComponents/Virtualization/NTVirtualize.razor.js';

let dotNetRefId = 0;

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
        history.replaceState(null, '', '/');
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
            _id: `virtualize-test-${++dotNetRefId}`,
            invokeMethodAsync: jest.fn(),
            dispose: jest.fn(),
        };

        return { topSpacer, bottomSpacer, dotNetRef, scrollAncestor };
    }

    test('uses max-height of scroll container when content is shorter than max-height', () => {
        // When a scroll container uses max-height, clientHeight only reflects actual content height
        // on initial load (content is shorter than the max-height). This test verifies that the
        // max-height is used instead so enough items are requested to cause overflow.
        const { topSpacer, bottomSpacer, dotNetRef, scrollAncestor } = createVirtualizedElements('auto');
        scrollAncestor.style.maxHeight = '400px';
        Object.defineProperty(scrollAncestor, 'clientHeight', { configurable: true, value: 100 });

        init(dotNetRef, topSpacer, bottomSpacer, 20, 1, 100);
        updateRenderState(dotNetRef, 100, 0, 0);

        intersectionObservers[0].callback([
            { target: bottomSpacer, isIntersecting: true },
        ]);

        // containerSize should be 400 (max-height), not 100 (clientHeight)
        // visibleItemCapacity = ceil(400/20) + 2*1 = 20 + 2 = 22
        // itemsBefore = max(0, floor(0/20) - 1) = 0
        // itemsAfter = max(0, 100 - 22 - 0) = 78
        // bottomSpacerSize = 78 * 20 = 1560
        expect(dotNetRef.invokeMethodAsync).toHaveBeenCalledWith('LoadItems', 0, 1560, 0, 22);
    });

    test('recalculates the bottom spacer when total count becomes available', () => {
        const { topSpacer, bottomSpacer, dotNetRef, scrollAncestor } = createVirtualizedElements('auto');
        scrollAncestor.style.maxHeight = '864px';
        Object.defineProperty(scrollAncestor, 'clientHeight', { configurable: true, value: 672 });

        init(dotNetRef, topSpacer, bottomSpacer, 48, 3, 100);

        intersectionObservers[0].callback([
            { target: bottomSpacer, isIntersecting: true },
        ]);

        expect(dotNetRef.invokeMethodAsync).toHaveBeenCalledWith('LoadItems', 0, 0, 0, 24);
        dotNetRef.invokeMethodAsync.mockClear();

        updateRenderState(dotNetRef, 10000, 24, 0);

        expect(dotNetRef.invokeMethodAsync).toHaveBeenCalledWith('LoadItems', 0, 478848, 0, 24);
    });

    test('recalculates visible range when the scroll container moves through an already intersecting spacer', () => {
        const originalRequestAnimationFrame = window.requestAnimationFrame;
        const originalCancelAnimationFrame = window.cancelAnimationFrame;
        window.requestAnimationFrame = callback => {
            callback(0);
            return 1;
        };
        window.cancelAnimationFrame = jest.fn();

        try {
            const { topSpacer, bottomSpacer, dotNetRef, scrollAncestor } = createVirtualizedElements('auto');
            scrollAncestor.style.maxHeight = '400px';
            Object.defineProperty(scrollAncestor, 'clientHeight', { configurable: true, value: 400 });

            init(dotNetRef, topSpacer, bottomSpacer, 20, 1, 100);
            updateRenderState(dotNetRef, 1000, 0, 0);
            dotNetRef.invokeMethodAsync.mockClear();

            scrollAncestor.scrollTop = 240;
            scrollAncestor.dispatchEvent(new Event('scroll'));

            expect(dotNetRef.invokeMethodAsync).toHaveBeenCalledWith('LoadItems', 220, 19340, 11, 22);
        }
        finally {
            window.requestAnimationFrame = originalRequestAnimationFrame;
            window.cancelAnimationFrame = originalCancelAnimationFrame;
        }
    });

    test('persists the scroll position in the current history entry without replacing existing state', () => {
        const originalRequestAnimationFrame = window.requestAnimationFrame;
        const originalCancelAnimationFrame = window.cancelAnimationFrame;
        window.requestAnimationFrame = callback => {
            callback(0);
            return 1;
        };
        window.cancelAnimationFrame = jest.fn();

        try {
            history.replaceState({ navigationIndex: 3 }, '', '/virtualized-grid');
            const { topSpacer, bottomSpacer, dotNetRef, scrollAncestor } = createVirtualizedElements('auto');
            Object.defineProperty(scrollAncestor, 'clientHeight', { configurable: true, value: 400 });
            Object.defineProperty(scrollAncestor, 'scrollHeight', { configurable: true, value: 2000 });

            init(dotNetRef, topSpacer, bottomSpacer, 20, 1, 100, 'jobs-scroll');
            updateRenderState(dotNetRef, 1000, 0, 0);

            scrollAncestor.scrollTop = 480;
            scrollAncestor.dispatchEvent(new Event('scroll'));

            expect(history.state).toEqual({
                navigationIndex: 3,
                __ntVirtualizeScrollPositions: {
                    'jobs-scroll': 480,
                },
            });
        }
        finally {
            window.requestAnimationFrame = originalRequestAnimationFrame;
            window.cancelAnimationFrame = originalCancelAnimationFrame;
        }
    });

    test('restores a saved scroll position when its history entry is revisited', () => {
        history.replaceState({
            __ntVirtualizeScrollPositions: {
                'jobs-scroll': 480,
            },
        }, '', '/virtualized-grid');
        const { topSpacer, bottomSpacer, dotNetRef, scrollAncestor } = createVirtualizedElements('auto');
        Object.defineProperty(scrollAncestor, 'clientHeight', { configurable: true, value: 400 });
        Object.defineProperty(scrollAncestor, 'scrollHeight', { configurable: true, value: 2000 });

        init(dotNetRef, topSpacer, bottomSpacer, 20, 1, 100, 'jobs-scroll');
        updateRenderState(dotNetRef, 0, 0, 0);

        expect(scrollAncestor.scrollTop).toBe(0);

        updateRenderState(dotNetRef, 1000, 0, 0);

        expect(scrollAncestor.scrollTop).toBe(480);
        expect(dotNetRef.invokeMethodAsync).toHaveBeenLastCalledWith('LoadItems', 460, 19100, 23, 22);
    });

    test('disposes observers and restores scroll overflow anchoring without disposing dotnet reference', () => {
        const { topSpacer, bottomSpacer, dotNetRef, scrollAncestor } = createVirtualizedElements('auto');
        scrollAncestor.style.overflowAnchor = 'auto';

        init(dotNetRef, topSpacer, bottomSpacer, 20, 1, 100);

        expect(scrollAncestor.style.overflowAnchor).toBe('none');

        onDispose(topSpacer, dotNetRef);

        expect(intersectionObservers[0].disconnect).toHaveBeenCalled();
        expect(mutationObservers[0].disconnect).toHaveBeenCalled();
        expect(mutationObservers[1].disconnect).toHaveBeenCalled();
        expect(scrollAncestor.style.overflowAnchor).toBe('auto');
        expect(dotNetRef.dispose).not.toHaveBeenCalled();
    });
});
