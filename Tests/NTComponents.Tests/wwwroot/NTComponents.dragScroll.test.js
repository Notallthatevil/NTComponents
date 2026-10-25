import { jest } from '@jest/globals';
// Importing the module attaches the drag-to-scroll listeners.
import '../../../NTComponents/wwwroot/NTComponents.lib.module.js';

// jsdom has no PointerEvent, so pointer events are MouseEvents that carry the pointer fields the handler reads.
function pointer(type, target, { clientX = 100, clientY = 50, pointerType = 'mouse', pointerId = 1, button = 0 } = {}) {
    const event = new MouseEvent(type, { bubbles: true, cancelable: true, clientX, clientY, button });
    Object.defineProperties(event, { pointerType: { value: pointerType }, pointerId: { value: pointerId } });
    target.dispatchEvent(event);
    return event;
}

function click(target) {
    const event = new MouseEvent('click', { bubbles: true, cancelable: true });
    target.dispatchEvent(event);
    return event;
}

function dragStart(target) {
    const event = new Event('dragstart', { bubbles: true, cancelable: true });
    target.dispatchEvent(event);
    return event;
}

// A container whose content is 1000px wide, starting scrolled 100px in. Its 300x200 client area sits inside an optional
// border, so the scrollbars lie just past the client area's right and bottom edges.
function renderScroller({ scrollWidth = 1000, border = 0 } = {}) {
    document.body.innerHTML = `
        <div data-nt-drag-scroll>
            <button type="button">Track</button>
            <a href="#row">Row link</a>
            <span id="cell">Northwind</span>
            <input aria-label="Quantity" />
            <textarea aria-label="Notes"></textarea>
            <select aria-label="Carrier"><option>Contoso</option></select>
            <span id="editable" contenteditable="true">Edit me</span>
            <span id="read-only" contenteditable="false">Read only</span>
            <span id="handle" draggable="true">Handle</span>
        </div>
        <a id="outside" href="#outside">Outside link</a>`;
    const scroller = document.querySelector('[data-nt-drag-scroll]');
    Object.defineProperties(scroller, {
        scrollWidth: { value: scrollWidth },
        clientWidth: { value: 300 },
        clientHeight: { value: 200 },
        clientLeft: { value: border },
        clientTop: { value: border }
    });
    const size = 300 + border * 2;
    scroller.getBoundingClientRect = () => ({ left: 0, top: 0, right: size, bottom: 200 + border * 2, width: size, height: 200 + border * 2 });
    scroller.scrollLeft = 100;
    const button = document.querySelector('button');
    const buttonClicks = jest.fn();
    button.addEventListener('click', buttonClicks);
    return { scroller, button, buttonClicks, link: document.querySelector('a'), outside: document.getElementById('outside') };
}

function drag(target, deltaX, options = {}) {
    pointer('pointerdown', target, { clientX: 200, ...options });
    pointer('pointermove', target, { clientX: 200 + deltaX, ...options });
    pointer('pointerup', target, { clientX: 200 + deltaX, ...options });
}

describe('drag to scroll', () => {
    beforeEach(() => {
        // The handler clears its swallow-the-release-click flag on a timer, so tests control that timer.
        jest.useFakeTimers();
    });

    afterEach(() => {
        // The listeners keep module state across tests: end any press and let the release-click flag expire.
        pointer('pointercancel', document);
        jest.runOnlyPendingTimers();
        jest.useRealTimers();
        document.body.innerHTML = '';
    });

    test('dragging sideways scrolls the container by the distance dragged', () => {
        const { scroller } = renderScroller();

        drag(scroller, -60);

        expect(scroller.scrollLeft).toBe(160);
    });

    test('marks the container while a drag is in progress', () => {
        const { scroller } = renderScroller();
        pointer('pointerdown', scroller, { clientX: 200 });
        pointer('pointermove', scroller, { clientX: 150 });

        expect(scroller.hasAttribute('data-nt-dragging')).toBe(true);

        pointer('pointerup', scroller, { clientX: 150 });
        expect(scroller.hasAttribute('data-nt-dragging')).toBe(false);
    });

    test('starting a drag clears any text selection', () => {
        const { scroller } = renderScroller();
        window.getSelection().selectAllChildren(document.getElementById('cell'));

        pointer('pointerdown', scroller, { clientX: 200 });
        pointer('pointermove', scroller, { clientX: 150 });

        expect(window.getSelection().rangeCount).toBe(0);
    });

    test('the click that ends a drag does not reach the button under the pointer', () => {
        const { button, buttonClicks } = renderScroller();

        drag(button, -60);
        const release = click(button);

        expect(buttonClicks).not.toHaveBeenCalled();
        expect(release.defaultPrevented).toBe(true);
    });

    test('only the click that ends the drag is swallowed', () => {
        const { button, buttonClicks } = renderScroller();
        drag(button, -60);
        click(button);

        click(button);

        expect(buttonClicks).toHaveBeenCalledTimes(1);
    });

    test('a drag released where no click follows stops swallowing clicks once the release is over', () => {
        const { button, buttonClicks } = renderScroller();
        drag(button, -60);

        jest.runOnlyPendingTimers();
        click(button);

        expect(buttonClicks).toHaveBeenCalledTimes(1);
    });

    test('a press that moves just under the threshold stays a click', () => {
        const { scroller, button, buttonClicks } = renderScroller();

        drag(button, -4);
        click(button);

        expect(scroller.scrollLeft).toBe(100);
        expect(buttonClicks).toHaveBeenCalledTimes(1);
    });

    test('a press that moves exactly the threshold starts a drag', () => {
        const { scroller } = renderScroller();

        drag(scroller, -5);

        expect(scroller.scrollLeft).toBe(105);
    });

    test('a drag that ends in pointercancel does not swallow the next click', () => {
        const { button, buttonClicks } = renderScroller();
        pointer('pointerdown', button, { clientX: 200 });
        pointer('pointermove', button, { clientX: 140 });
        pointer('pointercancel', button, { clientX: 140 });

        click(button);

        expect(buttonClicks).toHaveBeenCalledTimes(1);
    });

    test('moves from another pointer do not scroll', () => {
        const { scroller } = renderScroller();
        pointer('pointerdown', scroller, { clientX: 200 });

        pointer('pointermove', scroller, { clientX: 140, pointerId: 2 });

        expect(scroller.scrollLeft).toBe(100);
    });

    test.each(['touch', 'pen'])('%s input is left to native panning', pointerType => {
        const { scroller } = renderScroller();

        drag(scroller, -60, { pointerType });

        expect(scroller.scrollLeft).toBe(100);
    });

    test('non-primary mouse buttons do not drag', () => {
        const { scroller } = renderScroller();

        drag(scroller, -60, { button: 2 });

        expect(scroller.scrollLeft).toBe(100);
    });

    test.each(['input', 'textarea', 'select', '#editable'])('presses in %s keep text editing and selection', selector => {
        const { scroller } = renderScroller();

        drag(document.querySelector(selector), -60);

        expect(scroller.scrollLeft).toBe(100);
    });

    test('presses on content marked contenteditable="false" still drag', () => {
        const { scroller } = renderScroller();

        drag(document.getElementById('read-only'), -60);

        expect(scroller.scrollLeft).toBe(160);
    });

    test('presses on the horizontal scrollbar keep the native scrollbar drag', () => {
        const { scroller } = renderScroller();

        // The client area is 200px tall, so y = 200 is the scrollbar's first row.
        drag(scroller, -60, { clientY: 200 });

        expect(scroller.scrollLeft).toBe(100);
    });

    test('presses on the vertical scrollbar keep the native scrollbar drag', () => {
        const { scroller } = renderScroller();

        // The client area is 300px wide, so x = 300 is the scrollbar's first column.
        pointer('pointerdown', scroller, { clientX: 300 });
        pointer('pointermove', scroller, { clientX: 240 });

        expect(scroller.scrollLeft).toBe(100);
    });

    test('presses on the border do not drag', () => {
        const { scroller } = renderScroller({ border: 10 });

        drag(scroller, -60, { clientY: 5 });

        expect(scroller.scrollLeft).toBe(100);
    });

    test('presses just above the scrollbar of a bordered container still drag', () => {
        const { scroller } = renderScroller({ border: 10 });

        // With a 10px top border, the 200px client area ends at y = 210.
        drag(scroller, -60, { clientY: 205 });

        expect(scroller.scrollLeft).toBe(160);
    });

    test('containers that do not overflow are not hijacked', () => {
        const { scroller } = renderScroller({ scrollWidth: 300 });

        drag(scroller, -60);

        expect(scroller.scrollLeft).toBe(100);
    });

    test('hovering a container that overflows marks it for the grab cursor', () => {
        const { scroller, button } = renderScroller();

        pointer('pointerover', button);

        expect(scroller.hasAttribute('data-nt-can-drag')).toBe(true);
    });

    test('hovering a container whose content fits clears the grab cursor', () => {
        const { scroller, button } = renderScroller({ scrollWidth: 300 });
        scroller.setAttribute('data-nt-can-drag', '');

        pointer('pointerover', button);

        expect(scroller.hasAttribute('data-nt-can-drag')).toBe(false);
    });

    test('native link dragging is prevented during a press so it cannot cancel the scroll', () => {
        const { link } = renderScroller();
        pointer('pointerdown', link, { clientX: 200 });

        expect(dragStart(link).defaultPrevented).toBe(true);
    });

    test('explicitly draggable content keeps native dragging during a press', () => {
        renderScroller();
        const handle = document.getElementById('handle');
        pointer('pointerdown', handle, { clientX: 200 });

        expect(dragStart(handle).defaultPrevented).toBe(false);
    });

    test('native dragging outside a press is left alone', () => {
        const { outside } = renderScroller();

        expect(dragStart(outside).defaultPrevented).toBe(false);
    });

    test('a press whose release was missed does not block native dragging after the next press elsewhere', () => {
        const { scroller, outside } = renderScroller();
        pointer('pointerdown', scroller, { clientX: 200 });

        pointer('pointerdown', outside, { clientX: 400 });

        expect(dragStart(outside).defaultPrevented).toBe(false);
    });
});
