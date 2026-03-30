import { jest } from '@jest/globals';
import { disposePopoverWindow, initializePopoverWindow, updatePopoverWindow } from '../TnTPopoverWindow.razor.js';

if (typeof global.PointerEvent === 'undefined') {
  global.PointerEvent = MouseEvent;
}

describe('TnTPopoverWindow JS module', () => {
  beforeEach(() => {
    document.body.innerHTML = '';
    jest.clearAllMocks();
    Object.defineProperty(window, 'innerWidth', { configurable: true, value: 1024 });
    Object.defineProperty(window, 'innerHeight', { configurable: true, value: 768 });
  });

  function createPopover() {
    const element = document.createElement('section');
    element.className = 'tnt-popover';
    Object.defineProperty(element, 'offsetWidth', { configurable: true, value: 240 });
    Object.defineProperty(element, 'offsetHeight', { configurable: true, value: 180 });

    const handle = document.createElement('div');
    handle.setAttribute('data-tnt-popover-drag-handle', 'true');
    handle.setPointerCapture = jest.fn();
    handle.releasePointerCapture = jest.fn();

    const actionButton = document.createElement('button');
    actionButton.type = 'button';
    actionButton.textContent = 'Action';
    handle.appendChild(actionButton);
    element.appendChild(handle);
    document.body.appendChild(element);

    return { actionButton, element, handle };
  }

  test('initializes the element position', () => {
    const { element } = createPopover();

    initializePopoverWindow(element, { invokeMethodAsync: jest.fn() }, { left: 48, top: 96, allowDragging: true, viewportPadding: 16 });

    expect(element.style.left).toBe('48px');
    expect(element.style.top).toBe('96px');
  });

  test('dragging updates position and notifies .NET on pointer up', async () => {
    const { element, handle } = createPopover();
    const invokeMethodAsync = jest.fn(() => Promise.resolve());

    initializePopoverWindow(element, { invokeMethodAsync }, { left: 32, top: 64, allowDragging: true, viewportPadding: 16 });

    handle.dispatchEvent(new PointerEvent('pointerdown', { bubbles: true, button: 0, clientX: 100, clientY: 100, pointerId: 1 }));
    window.dispatchEvent(new PointerEvent('pointermove', { bubbles: true, clientX: 180, clientY: 160, pointerId: 1 }));
    window.dispatchEvent(new PointerEvent('pointerup', { bubbles: true, clientX: 180, clientY: 160, pointerId: 1 }));

    await Promise.resolve();

    expect(element.style.left).toBe('112px');
    expect(element.style.top).toBe('124px');
    expect(invokeMethodAsync).toHaveBeenCalledWith('NotifyActivated');
    expect(invokeMethodAsync).toHaveBeenCalledWith('NotifyPositionChanged', 112, 124);
  });

  test('pointerdown on an action button does not start dragging', () => {
    const { actionButton, element } = createPopover();
    const invokeMethodAsync = jest.fn(() => Promise.resolve());

    initializePopoverWindow(element, { invokeMethodAsync }, { left: 12, top: 24, allowDragging: true, viewportPadding: 16 });

    actionButton.dispatchEvent(new PointerEvent('pointerdown', { bubbles: true, button: 0, clientX: 40, clientY: 40, pointerId: 2 }));
    window.dispatchEvent(new PointerEvent('pointermove', { bubbles: true, clientX: 140, clientY: 160, pointerId: 2 }));

    expect(element.style.left).toBe('12px');
    expect(element.style.top).toBe('24px');
    expect(invokeMethodAsync).not.toHaveBeenCalledWith('NotifyPositionChanged', expect.anything(), expect.anything());
  });

  test('update reapplies the tracked position', () => {
    const { element } = createPopover();

    initializePopoverWindow(element, { invokeMethodAsync: jest.fn() }, { left: 0, top: 0, allowDragging: true, viewportPadding: 16 });
    updatePopoverWindow(element, { left: 240, top: 144, allowDragging: true, viewportPadding: 16 });

    expect(element.style.left).toBe('240px');
    expect(element.style.top).toBe('144px');
  });

  test('dispose removes event listeners', () => {
    const { element, handle } = createPopover();
    const invokeMethodAsync = jest.fn(() => Promise.resolve());

    initializePopoverWindow(element, { invokeMethodAsync }, { left: 16, top: 16, allowDragging: true, viewportPadding: 16 });
    disposePopoverWindow(element);

    handle.dispatchEvent(new PointerEvent('pointerdown', { bubbles: true, button: 0, clientX: 20, clientY: 20, pointerId: 3 }));
    window.dispatchEvent(new PointerEvent('pointermove', { bubbles: true, clientX: 120, clientY: 120, pointerId: 3 }));

    expect(element.style.left).toBe('16px');
    expect(element.style.top).toBe('16px');
  });
});
