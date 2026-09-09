import { jest } from '@jest/globals';
import { init, onDispose, updateRenderState } from '../../../NTComponents/Virtualization/NTVirtualize.razor.js';

let dotNetRefId = 0;

describe('NTVirtualize.init', () => {
    let intersectionObservers;
    let mutationObservers;
    let rangeHeight;
    let innerHeightDescriptor;
    let virtualizedElements;

    beforeEach(() => {
        document.body.innerHTML = '';
        virtualizedElements = [];
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
        for (const { topSpacer, dotNetRef } of virtualizedElements) {
            onDispose(topSpacer, dotNetRef);
        }
        document.createRange.mockRestore();

        if (innerHeightDescriptor) {
            Object.defineProperty(window, 'innerHeight', innerHeightDescriptor);
        }

        delete global.IntersectionObserver;
        delete global.MutationObserver;
    });


    test('disabling measurement clears cached heights and stops observing rows', () => {
        const unobserve = jest.fn();
        global.ResizeObserver = class {
            observe() {}
            unobserve = unobserve;
            disconnect() {}
        };
        const { topSpacer, bottomSpacer, dotNetRef, scrollAncestor } = createVirtualizedElements('auto');
        scrollAncestor.style.maxHeight = '100px';
        const row = topSpacer.nextElementSibling;
        row.setAttribute('data-nt-virtualize-index', '0');
        row.getBoundingClientRect = () => ({ height: 220 });
        try {
            init(dotNetRef, topSpacer, bottomSpacer, 20, 0, 100);
            updateRenderState(dotNetRef, 100, 1, 0, true);
            updateRenderState(dotNetRef, 100, 1, 0, false);
            scrollAncestor.scrollTop = 250;
            intersectionObservers[0].callback([{ target: bottomSpacer, isIntersecting: true }]);
            expect(dotNetRef.invokeMethodAsync).toHaveBeenLastCalledWith('LoadItems', 240, 1660, 12, 5);
            expect(unobserve).toHaveBeenCalledWith(row);
        }
        finally {
            onDispose(topSpacer, dotNetRef);
            delete global.ResizeObserver;
        }
    });

    test('measures a summary and detail as one parent item when scrolling and collapsing', () => {
        const { topSpacer, bottomSpacer, dotNetRef, scrollAncestor } = createVirtualizedElements('auto');
        scrollAncestor.style.maxHeight = '100px';
        Object.defineProperty(scrollAncestor, 'clientHeight', { configurable: true, value: 100 });
        const summary = topSpacer.nextElementSibling;
        summary.setAttribute('data-nt-virtualize-index', '0');
        summary.getBoundingClientRect = () => ({ height: 20 });
        const detail = document.createElement('div');
        detail.setAttribute('data-nt-virtualize-index', '0');
        detail.getBoundingClientRect = () => ({ height: 200 });
        bottomSpacer.before(detail);
        init(dotNetRef, topSpacer, bottomSpacer, 20, 0, 100);
        updateRenderState(dotNetRef, 100, 1, 0, true);

        // The viewport is still within the first expanded parent at 150px.
        scrollAncestor.scrollTop = 150;
        intersectionObservers[0].callback([{ target: bottomSpacer, isIntersecting: true }]);
        expect(dotNetRef.invokeMethodAsync).toHaveBeenLastCalledWith('LoadItems', 0, 1900, 0, 5);

        scrollAncestor.scrollTop = 250;
        intersectionObservers[0].callback([{ target: bottomSpacer, isIntersecting: true }]);
        expect(dotNetRef.invokeMethodAsync).toHaveBeenLastCalledWith('LoadItems', 240, 1840, 2, 6);

        detail.remove();
        updateRenderState(dotNetRef, 100, 1, 0, true, 1);
        expect(scrollAncestor.scrollTop).toBe(50);
        expect(dotNetRef.invokeMethodAsync).toHaveBeenLastCalledWith('LoadItems', 40, 1840, 2, 6);
        onDispose(topSpacer, dotNetRef);
    });

    test('resize observation updates parent height and is released on disposal', () => {
        let resized;
        const disconnect = jest.fn();
        global.ResizeObserver = class {
            constructor(callback) { resized = callback; }
            observe() {}
            unobserve() {}
            disconnect = disconnect;
        };
        const { topSpacer, bottomSpacer, dotNetRef, scrollAncestor } = createVirtualizedElements('auto');
        scrollAncestor.style.maxHeight = '100px';
        const summary = topSpacer.nextElementSibling;
        summary.setAttribute('data-nt-virtualize-index', '0');
        let height = 20;
        summary.getBoundingClientRect = () => ({ height });
        try {
            init(dotNetRef, topSpacer, bottomSpacer, 20, 0, 100);
            updateRenderState(dotNetRef, 100, 1, 0, true);
            height = 300;
            resized();
            scrollAncestor.scrollTop = 250;
            intersectionObservers[0].callback([{ target: bottomSpacer, isIntersecting: true }]);
            expect(dotNetRef.invokeMethodAsync).toHaveBeenLastCalledWith('LoadItems', 0, 1900, 0, 5);
        }
        finally {
            onDispose(topSpacer, dotNetRef);
            delete global.ResizeObserver;
        }
        expect(disconnect).toHaveBeenCalledTimes(1);
    });

    test('invalidating offscreen measurements preserves the anchored item and its inset', () => {
        const { topSpacer, bottomSpacer, dotNetRef, scrollAncestor } = createVirtualizedElements('auto');
        scrollAncestor.style.maxHeight = '100px';
        const row = topSpacer.nextElementSibling;
        row.setAttribute('data-nt-virtualize-index', '0');
        row.getBoundingClientRect = () => ({ height: 220 });
        init(dotNetRef, topSpacer, bottomSpacer, 20, 0, 100);
        updateRenderState(dotNetRef, 100, 1, 0, true);

        scrollAncestor.scrollTop = 405; // Item 10, five pixels into the row.
        row.setAttribute('data-nt-virtualize-index', '10');
        row.getBoundingClientRect = () => ({ height: 20 });
        topSpacer.setAttribute('data-nt-virtualize-start', '10');
        bottomSpacer.setAttribute('data-nt-virtualize-end', '11');
        updateRenderState(dotNetRef, 100, 1, 0, true, 1);

        expect(scrollAncestor.scrollTop).toBe(205);
        expect(topSpacer.style.height).toBe('200px');
        expect(dotNetRef.invokeMethodAsync).toHaveBeenLastCalledWith('LoadItems', 200, 1680, 10, 6);
        onDispose(topSpacer, dotNetRef);
    });

    test('resizing a row above the viewport compensates scroll without moving the visible item', () => {
        const { topSpacer, bottomSpacer, dotNetRef, scrollAncestor } = createVirtualizedElements('auto');
        scrollAncestor.style.maxHeight = '100px';
        const row = topSpacer.nextElementSibling;
        row.setAttribute('data-nt-virtualize-index', '0');
        let height = 20;
        row.getBoundingClientRect = () => ({ height });
        init(dotNetRef, topSpacer, bottomSpacer, 20, 0, 100);
        updateRenderState(dotNetRef, 100, 1, 0, true);
        scrollAncestor.scrollTop = 205;

        height = 220;
        updateRenderState(dotNetRef, 100, 1, 0, true);
        expect(scrollAncestor.scrollTop).toBe(405);
        expect(dotNetRef.invokeMethodAsync).toHaveBeenLastCalledWith('LoadItems', 400, 1680, 10, 6);
        onDispose(topSpacer, dotNetRef);
    });

    test('collapsing the item containing the viewport keeps the anchor within the shorter row', () => {
        const { topSpacer, bottomSpacer, dotNetRef, scrollAncestor } = createVirtualizedElements('auto');
        scrollAncestor.style.maxHeight = '100px';
        const row = topSpacer.nextElementSibling;
        row.setAttribute('data-nt-virtualize-index', '0');
        let height = 220;
        row.getBoundingClientRect = () => ({ height });
        try {
            init(dotNetRef, topSpacer, bottomSpacer, 20, 0, 100);
            updateRenderState(dotNetRef, 100, 1, 0, true);
            scrollAncestor.scrollTop = 150;

            height = 20;
            updateRenderState(dotNetRef, 100, 1, 0, true, 1);

            expect(scrollAncestor.scrollTop).toBe(19);
            expect(dotNetRef.invokeMethodAsync).toHaveBeenLastCalledWith('LoadItems', 0, 1880, 0, 6);
        }
        finally {
            onDispose(topSpacer, dotNetRef);
        }
    });

    test('re-enabling measurement uses current heights and resumes resize observation', () => {
        let resized;
        const observe = jest.fn();
        global.ResizeObserver = class {
            constructor(callback) { resized = callback; }
            observe = observe;
            unobserve() {}
            disconnect() {}
        };
        const { topSpacer, bottomSpacer, dotNetRef, scrollAncestor } = createVirtualizedElements('auto');
        scrollAncestor.style.maxHeight = '100px';
        const row = topSpacer.nextElementSibling;
        row.setAttribute('data-nt-virtualize-index', '0');
        let height = 220;
        row.getBoundingClientRect = () => ({ height });
        try {
            init(dotNetRef, topSpacer, bottomSpacer, 20, 0, 100);
            updateRenderState(dotNetRef, 100, 1, 0, true);
            updateRenderState(dotNetRef, 100, 1, 0, false);
            height = 60;
            updateRenderState(dotNetRef, 100, 1, 0, true);
            scrollAncestor.scrollTop = 85;
            intersectionObservers[0].callback([{ target: bottomSpacer, isIntersecting: true }]);

            expect(dotNetRef.invokeMethodAsync).toHaveBeenLastCalledWith('LoadItems', 80, 1840, 2, 6);
            expect(observe).toHaveBeenCalledTimes(2);
            expect(observe).toHaveBeenLastCalledWith(row);

            height = 100;
            resized();
            expect(scrollAncestor.scrollTop).toBe(125);
            expect(dotNetRef.invokeMethodAsync).toHaveBeenLastCalledWith('LoadItems', 120, 1840, 2, 6);
        }
        finally {
            onDispose(topSpacer, dotNetRef);
            delete global.ResizeObserver;
        }
    });

    test('replacing rendered rows releases old observations and measures the new rows', () => {
        let resized;
        const observe = jest.fn();
        const unobserve = jest.fn();
        global.ResizeObserver = class {
            constructor(callback) { resized = callback; }
            observe = observe;
            unobserve = unobserve;
            disconnect() {}
        };
        const { topSpacer, bottomSpacer, dotNetRef, scrollAncestor } = createVirtualizedElements('auto');
        scrollAncestor.style.maxHeight = '100px';
        const oldRow = topSpacer.nextElementSibling;
        oldRow.setAttribute('data-nt-virtualize-index', '0');
        oldRow.getBoundingClientRect = () => ({ height: 220 });
        try {
            init(dotNetRef, topSpacer, bottomSpacer, 20, 0, 100);
            updateRenderState(dotNetRef, 100, 1, 0, true);
            const replacement = document.createElement('div');
            replacement.setAttribute('data-nt-virtualize-index', '0');
            let height = 60;
            replacement.getBoundingClientRect = () => ({ height });
            oldRow.replaceWith(replacement);
            updateRenderState(dotNetRef, 100, 1, 0, true);

            expect(unobserve).toHaveBeenCalledWith(oldRow);
            expect(observe).toHaveBeenLastCalledWith(replacement);
            scrollAncestor.scrollTop = 85;
            intersectionObservers[0].callback([{ target: bottomSpacer, isIntersecting: true }]);
            expect(dotNetRef.invokeMethodAsync).toHaveBeenLastCalledWith('LoadItems', 80, 1840, 2, 6);

            height = 100;
            resized();
            expect(scrollAncestor.scrollTop).toBe(125);
            expect(dotNetRef.invokeMethodAsync).toHaveBeenLastCalledWith('LoadItems', 120, 1840, 2, 6);
        }
        finally {
            onDispose(topSpacer, dotNetRef);
            delete global.ResizeObserver;
        }
    });

    test('shrinking then growing the item count does not resurrect removed row heights', () => {
        const { topSpacer, bottomSpacer, dotNetRef, scrollAncestor } = createVirtualizedElements('auto');
        scrollAncestor.style.maxHeight = '100px';
        const row = topSpacer.nextElementSibling;
        row.setAttribute('data-nt-virtualize-index', '90');
        row.getBoundingClientRect = () => ({ height: 220 });
        try {
            init(dotNetRef, topSpacer, bottomSpacer, 20, 0, 100);
            updateRenderState(dotNetRef, 100, 1, 0, true);
            expect(dotNetRef.invokeMethodAsync).toHaveBeenLastCalledWith('LoadItems', 0, 2100, 0, 5);

            row.remove();
            updateRenderState(dotNetRef, 10, 0, 0, true);
            updateRenderState(dotNetRef, 100, 0, 0, true);

            expect(dotNetRef.invokeMethodAsync).toHaveBeenLastCalledWith('LoadItems', 0, 1900, 0, 5);
        }
        finally {
            onDispose(topSpacer, dotNetRef);
        }
    });

    test.each([[100, 0, 10], [6, 0, 6], [6, 1, 8]])('measured short rows fill the viewport within max %i and overscan %i', (maxItemCount, overscanCount, expectedCount) => {
        const { topSpacer, bottomSpacer, dotNetRef, scrollAncestor } = createVirtualizedElements('auto');
        scrollAncestor.style.maxHeight = '100px';
        for (let index = 0; index < 10; index++) {
            const row = index === 0 ? topSpacer.nextElementSibling : document.createElement('div');
            row.setAttribute('data-nt-virtualize-index', String(index));
            row.getBoundingClientRect = () => ({ height: 10 });
            if (index > 0) {
                bottomSpacer.before(row);
            }
        }
        try {
            init(dotNetRef, topSpacer, bottomSpacer, 20, overscanCount, maxItemCount);
            updateRenderState(dotNetRef, 100, 10, 0, true);

            const [, , , startIndex, count] = dotNetRef.invokeMethodAsync.mock.calls.at(-1);
            expect(startIndex).toBe(0);
            expect(count).toBe(expectedCount);
            expect(count).toBeLessThanOrEqual(maxItemCount + 2 * overscanCount);
        }
        finally {
            onDispose(topSpacer, dotNetRef);
        }
    });

    test('100000 variable-height items retain coverage and bounded work across forward and reverse windows', () => {
        jest.useFakeTimers();
        const observedRows = new Set();
        const disconnect = jest.fn(() => observedRows.clear());
        global.ResizeObserver = class {
            observe(row) { observedRows.add(row); }
            unobserve(row) { observedRows.delete(row); }
            disconnect = disconnect;
        };
        const { topSpacer, bottomSpacer, dotNetRef, scrollAncestor } = createVirtualizedElements('auto');
        const itemCount = 100000;
        const viewportHeight = 240;
        const maxItemCount = 40;
        const overscanCount = 4;
        const windowSize = maxItemCount + 2 * overscanCount;
        scrollAncestor.style.maxHeight = `${viewportHeight}px`;
        scrollAncestor.style.overflowAnchor = 'auto';
        topSpacer.nextElementSibling.remove();

        // A dense prefix-sum oracle is deliberately independent of the sparse height map
        // and binary search used in production. Only rendered rows become known heights.
        const heights = new Float64Array(itemCount).fill(20);
        const offsets = new Float64Array(itemCount + 1);
        const rebuildOffsets = () => {
            for (let index = 0; index < itemCount; index++) {
                offsets[index + 1] = offsets[index] + heights[index];
            }
        };
        rebuildOffsets();
        const forwardTargets = Array.from({ length: 80 }, (_, index) => Math.floor(index * (itemCount - 10) / 79));
        const targets = [...forwardTargets, ...forwardTargets.toReversed()];
        let version = 0;
        try {
            init(dotNetRef, topSpacer, bottomSpacer, 20, overscanCount, maxItemCount);
            updateRenderState(dotNetRef, itemCount, 0, 0, true);
            dotNetRef.invokeMethodAsync.mockClear();

            targets.forEach((target, step) => {
                scrollAncestor.scrollTop = offsets[target] + 3;
                const start = Math.max(0, Math.min(target - overscanCount, itemCount - windowSize));
                const end = start + windowSize;
                while (topSpacer.nextElementSibling !== bottomSpacer) {
                    topSpacer.nextElementSibling.remove();
                }
                topSpacer.setAttribute('data-nt-virtualize-start', String(start));
                bottomSpacer.setAttribute('data-nt-virtualize-end', String(end));
                if (step % 32 === 0) {
                    version++;
                    heights.fill(20);
                }
                for (let index = start; index < end; index++) {
                    const summaryHeight = 12 + index % 7 * 4;
                    const detailHeight = (index + step) % 3 === 0 ? 60 + index % 5 * 30 : 0;
                    for (const height of [summaryHeight, detailHeight].filter(value => value > 0)) {
                        const row = document.createElement('div');
                        row.setAttribute('data-nt-virtualize-index', String(index));
                        row.getBoundingClientRect = () => ({ height });
                        bottomSpacer.before(row);
                    }
                    heights[index] = summaryHeight + detailHeight;
                }
                rebuildOffsets();
                const callsBefore = dotNetRef.invokeMethodAsync.mock.calls.length;
                updateRenderState(dotNetRef, itemCount, windowSize, 0, true, version);
                intersectionObservers[0].callback([{ target: bottomSpacer, isIntersecting: true }]);

                expect(scrollAncestor.scrollTop).toBe(offsets[target] + 3);
                const [method, before, after, requestedStart, count] = dotNetRef.invokeMethodAsync.mock.calls.at(-1);
                const requestedEnd = requestedStart + count;
                expect(method).toBe('LoadItems');
                expect(requestedStart).toBeGreaterThanOrEqual(0);
                expect(requestedEnd).toBeLessThanOrEqual(itemCount);
                expect(count).toBeLessThanOrEqual(windowSize);
                expect(before).toBe(offsets[requestedStart]);
                expect(after).toBe(offsets[itemCount] - offsets[requestedEnd]);
                expect(before).toBeLessThanOrEqual(scrollAncestor.scrollTop);
                expect(offsets[requestedEnd]).toBeGreaterThanOrEqual(Math.min(offsets[itemCount], scrollAncestor.scrollTop + viewportHeight));
                expect(dotNetRef.invokeMethodAsync.mock.calls.length - callsBefore).toBeLessThanOrEqual(1);
                expect(observedRows).toEqual(new Set(topSpacer.parentElement.querySelectorAll('[data-nt-virtualize-index]')));
            });

            expect(dotNetRef.invokeMethodAsync.mock.calls.length).toBeLessThanOrEqual(targets.length);
            onDispose(topSpacer, dotNetRef);
            const callsAtDisposal = dotNetRef.invokeMethodAsync.mock.calls.length;
            scrollAncestor.dispatchEvent(new Event('scroll'));
            jest.advanceTimersByTime(1000);
            expect(dotNetRef.invokeMethodAsync).toHaveBeenCalledTimes(callsAtDisposal);
            expect(jest.getTimerCount()).toBe(0);
            expect(observedRows.size).toBe(0);
            expect(disconnect).toHaveBeenCalledTimes(1);
            expect(scrollAncestor.style.overflowAnchor).toBe('auto');
        }
        finally {
            onDispose(topSpacer, dotNetRef);
            delete global.ResizeObserver;
            jest.useRealTimers();
        }
    });

    test('does nothing when the dotnet reference is null', () => {
        const { topSpacer, bottomSpacer } = createVirtualizedElements();
        const addEventListener = jest.spyOn(EventTarget.prototype, 'addEventListener');
        const setInterval = jest.spyOn(window, 'setInterval');
        const setTimeout = jest.spyOn(window, 'setTimeout');

        try {
            expect(() => init(null, topSpacer, bottomSpacer, 20, 1, 100)).not.toThrow();
            expect(intersectionObservers).toHaveLength(0);
            expect(mutationObservers).toHaveLength(0);
            expect(addEventListener).not.toHaveBeenCalled();
            expect(setInterval).not.toHaveBeenCalled();
            expect(setTimeout).not.toHaveBeenCalled();
        }
        finally {
            addEventListener.mockRestore();
            setInterval.mockRestore();
            setTimeout.mockRestore();
        }
    });

    test('does nothing when the spacer elements are disconnected', () => {
        const { topSpacer, bottomSpacer, dotNetRef, scrollAncestor } = createVirtualizedElements();
        scrollAncestor.remove();
        const addEventListener = jest.spyOn(EventTarget.prototype, 'addEventListener');
        const setInterval = jest.spyOn(window, 'setInterval');

        try {
            init(dotNetRef, topSpacer, bottomSpacer, 20, 1, 100);

            expect(intersectionObservers).toHaveLength(0);
            expect(mutationObservers).toHaveLength(0);
            expect(addEventListener).not.toHaveBeenCalled();
            expect(setInterval).not.toHaveBeenCalled();
            expect(dotNetRef.invokeMethodAsync).not.toHaveBeenCalled();
        }
        finally {
            addEventListener.mockRestore();
            setInterval.mockRestore();
        }
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

        topSpacer.getBoundingClientRect = jest.fn(() => ({ top: -scrollAncestor.scrollTop }));
        bottomSpacer.getBoundingClientRect = jest.fn(() => ({ top: 400 }));
        scrollAncestor.getBoundingClientRect = jest.fn(() => ({ top: 0 }));

        const dotNetRef = {
            _callDispatcher: {},
            _id: `virtualize-test-${++dotNetRefId}`,
            invokeMethodAsync: jest.fn(),
            dispose: jest.fn(),
        };

        const elements = { topSpacer, bottomSpacer, dotNetRef, scrollAncestor };
        virtualizedElements.push(elements);
        return elements;
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
