import { jest } from '@jest/globals';
import { disposePopoverWindow, initializePopoverWindow, updatePopoverWindow, waitForCloseAnimation } from '../TnTPopoverWindow.razor.js';

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

  describe('enter animation', () => {
    test('invokes NotifyEnterAnimationCompleted when tnt-popover-enter ends on the element', async () => {
      // Arrange
      const { element } = createPopover();
      const invokeMethodAsync = jest.fn(() => Promise.resolve());
      initializePopoverWindow(element, { invokeMethodAsync }, { left: 0, top: 0 });

      // Act
      element.dispatchEvent(new Event('animationend', { bubbles: true, animationName: 'tnt-popover-enter' }));
      Object.defineProperty(Event.prototype, 'animationName', { configurable: true, get() { return this._animationName ?? ''; } });
      const evt = new Event('animationend', { bubbles: true });
      evt._animationName = 'tnt-popover-enter';
      Object.defineProperty(evt, 'target', { configurable: true, get: () => element });
      element.dispatchEvent(evt);
      await Promise.resolve();

      // Assert
      expect(invokeMethodAsync).toHaveBeenCalledWith('NotifyEnterAnimationCompleted');
    });

    test('ignores animationend from child elements bubbling to the popover', async () => {
      // Arrange
      const { element } = createPopover();
      const invokeMethodAsync = jest.fn(() => Promise.resolve());
      initializePopoverWindow(element, { invokeMethodAsync }, { left: 0, top: 0 });

      const child = document.createElement('div');
      element.appendChild(child);

      // Act – fire animationend from child (bubbles to element but target !== element)
      const evt = new Event('animationend', { bubbles: true });
      Object.defineProperty(evt, 'animationName', { configurable: true, value: 'tnt-popover-enter' });
      child.dispatchEvent(evt);
      await Promise.resolve();

      // Assert
      expect(invokeMethodAsync).not.toHaveBeenCalledWith('NotifyEnterAnimationCompleted');
    });
  });

  describe('waitForCloseAnimation', () => {
    beforeEach(() => {
      jest.useFakeTimers();
    });

    afterEach(() => {
      jest.useRealTimers();
    });

    test('resolves when tnt-popover-exit animationend fires on the element', async () => {
      // Arrange
      const { element } = createPopover();
      const promise = waitForCloseAnimation(element);

      // Act
      const evt = new Event('animationend', { bubbles: false });
      Object.defineProperty(evt, 'animationName', { configurable: true, value: 'tnt-popover-exit' });
      Object.defineProperty(evt, 'target', { configurable: true, get: () => element });
      element.dispatchEvent(evt);

      // Assert
      await expect(promise).resolves.toBeUndefined();
    });

    test('ignores animationend from a child element bubbling up', async () => {
      // Arrange
      const { element } = createPopover();
      const child = document.createElement('div');
      element.appendChild(child);

      const promise = waitForCloseAnimation(element);

      // Act – fire from child; event.target is child, not element
      const evt = new Event('animationend', { bubbles: true });
      Object.defineProperty(evt, 'animationName', { configurable: true, value: 'tnt-popover-exit' });
      child.dispatchEvent(evt);

      // The promise should NOT have resolved yet
      let resolved = false;
      promise.then(() => { resolved = true; });
      await Promise.resolve();
      expect(resolved).toBe(false);

      // Advance past the fallback timeout so the test can finish
      jest.advanceTimersByTime(300);
      await promise;
    });

    test('ignores animationend with a different animation name', async () => {
      // Arrange
      const { element } = createPopover();
      const promise = waitForCloseAnimation(element);

      // Act – fire with a different animation name
      const evt = new Event('animationend', { bubbles: false });
      Object.defineProperty(evt, 'animationName', { configurable: true, value: 'some-other-animation' });
      Object.defineProperty(evt, 'target', { configurable: true, get: () => element });
      element.dispatchEvent(evt);

      let resolved = false;
      promise.then(() => { resolved = true; });
      await Promise.resolve();
      expect(resolved).toBe(false);

      jest.advanceTimersByTime(300);
      await promise;
    });

    test('resolves after the fallback timeout when no animationend fires', async () => {
      // Arrange
      const { element } = createPopover();
      const promise = waitForCloseAnimation(element);

      // Act – advance past the 300ms fallback
      jest.advanceTimersByTime(300);

      // Assert
      await expect(promise).resolves.toBeUndefined();
    });

    test('resolves immediately for a null element', async () => {
      await expect(waitForCloseAnimation(null)).resolves.toBeUndefined();
    });
  });
});


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
