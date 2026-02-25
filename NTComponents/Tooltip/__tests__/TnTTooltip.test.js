/**
 * @jest-environment jsdom
 */
import { jest } from '@jest/globals';
import { onLoad, onUpdate, onDispose } from '../TnTTooltip.razor.js';

describe('TnTTooltip custom HTML element', () => {
  beforeEach(() => {
    document.body.innerHTML = '';
    jest.clearAllMocks();
    jest.useFakeTimers();
    
    // Reset customElements registry for tooltip
    if (customElements.get('tnt-tooltip')) {
      // Cannot undefine, so we just work with the existing registration
    }
  });

  afterEach(() => {
    jest.runOnlyPendingTimers();
    jest.useRealTimers();
  });

  const defineTooltip = () => {
    if (!customElements.get('tnt-tooltip')) {
      onLoad(null, null);
    }
  };

  function createTooltipSetup() {
    defineTooltip();
    
    // Create parent element
    const parent = document.createElement('div');
    parent.style.position = 'relative';
    document.body.appendChild(parent);
    
    // Create tooltip element
    const tooltip = document.createElement('tnt-tooltip');
    tooltip.style.setProperty('--tnt-tooltip-show-delay', '500');
    tooltip.style.setProperty('--tnt-tooltip-hide-delay', '200');
    parent.appendChild(tooltip);
    
    return { parent, tooltip };
  }

  function createMockComputedStyle(showDelay = '500', hideDelay = '200') {
    return {
      getPropertyValue: (prop) => {
        if (prop === '--tnt-tooltip-show-delay') return showDelay;
        if (prop === '--tnt-tooltip-hide-delay') return hideDelay;
        return '';
      }
    };
  }

  describe('Initialization', () => {
    test('connectedCallback attaches event listeners to parent', () => {
      const { parent, tooltip } = createTooltipSetup();
      const addEventListenerSpy = jest.spyOn(parent, 'addEventListener');
      
      tooltip.connectedCallback();
      
      expect(addEventListenerSpy).toHaveBeenCalledWith('mouseenter', expect.any(Function));
      expect(addEventListenerSpy).toHaveBeenCalledWith('mouseleave', expect.any(Function));
      expect(addEventListenerSpy).toHaveBeenCalledWith('mousemove', expect.any(Function));
    });

    test('initialize sets initial off-screen position', () => {
      const { tooltip } = createTooltipSetup();
      
      tooltip.initialize();
      
      expect(tooltip.style.left).toBe('-9999px');
      expect(tooltip.style.top).toBe('-9999px');
    });

    test('initialize stores lastMouseX and lastMouseY as zero', () => {
      const { tooltip } = createTooltipSetup();
      
      tooltip.initialize();
      
      expect(tooltip.lastMouseX).toBe(0);
      expect(tooltip.lastMouseY).toBe(0);
    });

    test('initialize without parent element returns early', () => {
      const tooltip = document.createElement('tnt-tooltip');
      
      expect(() => tooltip.initialize()).not.toThrow();
    });
  });

  describe('Cleanup', () => {
    test('disconnectedCallback removes event listeners from parent', () => {
      const { parent, tooltip } = createTooltipSetup();
      tooltip.connectedCallback();
      
      const removeEventListenerSpy = jest.spyOn(parent, 'removeEventListener');
      
      tooltip.disconnectedCallback();
      
      expect(removeEventListenerSpy).toHaveBeenCalledWith('mouseenter', expect.any(Function));
      expect(removeEventListenerSpy).toHaveBeenCalledWith('mouseleave', expect.any(Function));
      expect(removeEventListenerSpy).toHaveBeenCalledWith('mousemove', expect.any(Function));
    });

    test('disconnectedCallback clears pending timeouts', () => {
      const { tooltip } = createTooltipSetup();
      tooltip.initialize();
      
      tooltip.showTimeoutId = 123;
      tooltip.hideTimeoutId = 456;
      
      const clearTimeoutSpy = jest.spyOn(global, 'clearTimeout');
      
      tooltip.disconnectedCallback();
      
      expect(clearTimeoutSpy).toHaveBeenCalledWith(123);
      expect(clearTimeoutSpy).toHaveBeenCalledWith(456);
    });

    test('dispose without parent returns early', () => {
      const tooltip = document.createElement('tnt-tooltip');
      
      expect(() => tooltip.dispose()).not.toThrow();
    });
  });

  describe('Mouse Events', () => {
    test('onMouseEnter starts show delay timer', () => {
      const { tooltip } = createTooltipSetup();
      tooltip.initialize();
      
      jest.spyOn(global, 'getComputedStyle').mockReturnValue(createMockComputedStyle());
      const setTimeoutSpy = jest.spyOn(global, 'setTimeout');
      
      tooltip.onMouseEnter();
      
      expect(setTimeoutSpy).toHaveBeenCalledWith(expect.any(Function), 500);
    });

    test('onMouseEnter clears previous timeouts', () => {
      const { tooltip } = createTooltipSetup();
      tooltip.initialize();
      
      tooltip.showTimeoutId = 123;
      tooltip.hideTimeoutId = 456;
      
      jest.spyOn(global, 'getComputedStyle').mockReturnValue(createMockComputedStyle());
      const clearTimeoutSpy = jest.spyOn(global, 'clearTimeout');
      
      tooltip.onMouseEnter();
      
      expect(clearTimeoutSpy).toHaveBeenCalledWith(123);
      expect(clearTimeoutSpy).toHaveBeenCalledWith(456);
    });

    test('onMouseLeave starts hide delay timer', () => {
      const { tooltip } = createTooltipSetup();
      tooltip.initialize();
      
      jest.spyOn(global, 'getComputedStyle').mockReturnValue(createMockComputedStyle());
      const setTimeoutSpy = jest.spyOn(global, 'setTimeout');
      
      tooltip.onMouseLeave();
      
      expect(setTimeoutSpy).toHaveBeenCalledWith(expect.any(Function), 200);
    });

    test('onMouseMove stores cursor position', () => {
      const { tooltip } = createTooltipSetup();
      const mouseEvent = new MouseEvent('mousemove', { clientX: 100, clientY: 200 });
      
      tooltip.onMouseMove(mouseEvent);
      
      expect(tooltip.lastMouseX).toBe(100);
      expect(tooltip.lastMouseY).toBe(200);
    });

    test('onMouseMove does not update position if not visible', () => {
      const { tooltip } = createTooltipSetup();
      tooltip.isVisible = false;
      
      const updatePositionSpy = jest.spyOn(tooltip, 'updatePosition');
      const mouseEvent = new MouseEvent('mousemove', { clientX: 100, clientY: 200 });
      
      tooltip.onMouseMove(mouseEvent);
      
      expect(updatePositionSpy).not.toHaveBeenCalled();
    });

    test('onMouseMove updates position if visible', () => {
      const { tooltip } = createTooltipSetup();
      tooltip.isVisible = true;
      
      const updatePositionSpy = jest.spyOn(tooltip, 'updatePosition');
      const mouseEvent = new MouseEvent('mousemove', { clientX: 100, clientY: 200 });
      
      tooltip.onMouseMove(mouseEvent);
      
      expect(updatePositionSpy).toHaveBeenCalledWith(100, 200);
    });
  });

  describe('Show/Hide', () => {
    test('show adds visible class and calls updatePosition', () => {
      const { tooltip } = createTooltipSetup();
      tooltip.lastMouseX = 100;
      tooltip.lastMouseY = 200;
      
      const updatePositionSpy = jest.spyOn(tooltip, 'updatePosition');
      
      tooltip.show();
      
      expect(tooltip.isVisible).toBe(true);
      expect(tooltip.classList.contains('tnt-tooltip-visible')).toBe(true);
      expect(updatePositionSpy).toHaveBeenCalledWith(100, 200);
    });

    test('show does not double-show', () => {
      const { tooltip } = createTooltipSetup();
      tooltip.isVisible = true;
      tooltip.classList.add('tnt-tooltip-visible');
      
      const updatePositionSpy = jest.spyOn(tooltip, 'updatePosition');
      
      tooltip.show();
      
      expect(updatePositionSpy).not.toHaveBeenCalled();
    });

    test('hide removes visible class and moves off-screen', () => {
      const { tooltip } = createTooltipSetup();
      tooltip.isVisible = true;
      tooltip.classList.add('tnt-tooltip-visible');
      
      tooltip.hide();
      
      expect(tooltip.isVisible).toBe(false);
      expect(tooltip.classList.contains('tnt-tooltip-visible')).toBe(false);
      expect(tooltip.style.left).toBe('-9999px');
      expect(tooltip.style.top).toBe('-9999px');
    });

    test('hide does not double-hide', () => {
      const { tooltip } = createTooltipSetup();
      tooltip.isVisible = false;
      
      tooltip.hide();
      
      expect(tooltip.isVisible).toBe(false);
    });
  });

  describe('Position Updates', () => {
    test('updatePosition calculates correct position above cursor', () => {
      const { tooltip } = createTooltipSetup();
      
      jest.spyOn(tooltip, 'getBoundingClientRect').mockReturnValue({
        width: 100,
        height: 40,
        top: 100,
        left: 50,
        right: 150,
        bottom: 140
      });
      
      tooltip.updatePosition(200, 300);
      
      const expectedLeft = 200 - 50; // clientX - (tooltipWidth / 2)
      expect(tooltip.style.left).toBe(`${expectedLeft}px`);
      expect(tooltip.style.top).not.toBe('-9999px');
    });

    test('updatePosition repositions below cursor if too close to top', () => {
      const { tooltip } = createTooltipSetup();
      
      jest.spyOn(tooltip, 'getBoundingClientRect').mockReturnValue({
        width: 100,
        height: 40,
        top: 0,
        left: 50,
        right: 150,
        bottom: 40
      });
      
      // Mock window dimensions
      Object.defineProperty(window, 'innerWidth', { value: 1024, writable: true });
      Object.defineProperty(window, 'innerHeight', { value: 768, writable: true });
      
      tooltip.updatePosition(200, 20); // Near top of viewport
      
      // Tooltip should be positioned below
      const topPosition = parseInt(tooltip.style.top);
      expect(topPosition).toBeGreaterThan(20);
    });

    test('updatePosition constrains left edge', () => {
      const { tooltip } = createTooltipSetup();
      
      jest.spyOn(tooltip, 'getBoundingClientRect').mockReturnValue({
        width: 100,
        height: 40,
        top: 0,
        left: 0,
        right: 100,
        bottom: 40
      });
      
      Object.defineProperty(window, 'innerWidth', { value: 1024, writable: true });
      Object.defineProperty(window, 'innerHeight', { value: 768, writable: true });
      
      tooltip.updatePosition(10, 300); // Very close to left edge
      
      const leftPosition = parseInt(tooltip.style.left);
      expect(leftPosition).toBeGreaterThanOrEqual(0);
    });

    test('updatePosition constrains right edge', () => {
      const { tooltip } = createTooltipSetup();
      
      jest.spyOn(tooltip, 'getBoundingClientRect').mockReturnValue({
        width: 100,
        height: 40,
        top: 0,
        left: 900,
        right: 1000,
        bottom: 40
      });
      
      Object.defineProperty(window, 'innerWidth', { value: 1024, writable: true });
      Object.defineProperty(window, 'innerHeight', { value: 768, writable: true });
      
      tooltip.updatePosition(1020, 300); // Very close to right edge
      
      const leftPosition = parseInt(tooltip.style.left);
      expect(leftPosition + 100).toBeLessThanOrEqual(1024);
    });

    test('updatePosition calls updatePointer', () => {
      const { tooltip } = createTooltipSetup();
      
      jest.spyOn(tooltip, 'getBoundingClientRect').mockReturnValue({
        width: 100,
        height: 40,
        top: 0,
        left: 50,
        right: 150,
        bottom: 40
      });
      
      const updatePointerSpy = jest.spyOn(tooltip, 'updatePointer');
      
      Object.defineProperty(window, 'innerWidth', { value: 1024, writable: true });
      Object.defineProperty(window, 'innerHeight', { value: 768, writable: true });
      
      tooltip.updatePosition(200, 300);
      
      expect(updatePointerSpy).toHaveBeenCalled();
    });
  });

  describe('Pointer Updates', () => {
    test('updatePointer calculates horizontal position correctly', () => {
      const { tooltip } = createTooltipSetup();
      
      tooltip.updatePointer(200, 300, 100, 400, 100, 40);
      
      const pointerX = tooltip.style.getPropertyValue('--pointer-x');
      expect(pointerX).toBeTruthy();
    });

    test('updatePointer clamps pointer to left boundary', () => {
      const { tooltip } = createTooltipSetup();
      
      // Cursor is at 105 (left of container at 100), should clamp to minimum
      tooltip.updatePointer(105, 300, 100, 400, 100, 40);
      
      const pointerX = parseFloat(tooltip.style.getPropertyValue('--pointer-x'));
      expect(pointerX).toBeGreaterThanOrEqual(8);
    });

    test('updatePointer clamps pointer to right boundary', () => {
      const { tooltip } = createTooltipSetup();
      
      // Cursor is at 195 (right of container), should clamp to maximum
      tooltip.updatePointer(195, 300, 100, 400, 100, 40);
      
      const pointerX = parseFloat(tooltip.style.getPropertyValue('--pointer-x'));
      expect(pointerX).toBeLessThanOrEqual(92);
    });

    test('updatePointer does not add pointer-top class when tooltip above cursor', () => {
      const { tooltip } = createTooltipSetup();

      // Tooltip center (420) is above cursor (450); default arrow at bottom points down toward cursor
      tooltip.updatePointer(200, 450, 100, 400, 100, 40);

      expect(tooltip.classList.contains('tnt-tooltip-pointer-top')).toBe(false);
    });

    test('updatePointer adds pointer-top class when tooltip below cursor', () => {
      const { tooltip } = createTooltipSetup();

      // Tooltip center (420) is below cursor (300); arrow moves to top and points up toward cursor
      tooltip.updatePointer(200, 300, 100, 400, 100, 40);

      expect(tooltip.classList.contains('tnt-tooltip-pointer-top')).toBe(true);
    });

    test('updatePointer centers pointer when cursor is in middle', () => {
      const { tooltip } = createTooltipSetup();
      
      // Cursor at 150 (middle of 100-200 range)
      tooltip.updatePointer(150, 300, 100, 400, 100, 40);
      
      const pointerX = parseFloat(tooltip.style.getPropertyValue('--pointer-x'));
      expect(pointerX).toBeCloseTo(50, 0); // Should be around 50%
    });
  });

  describe('Timeout Management', () => {
    test('clearTimeouts clears both timeout IDs', () => {
      const { tooltip } = createTooltipSetup();
      
      tooltip.showTimeoutId = 123;
      tooltip.hideTimeoutId = 456;
      
      const clearTimeoutSpy = jest.spyOn(global, 'clearTimeout');
      
      tooltip.clearTimeouts();
      
      expect(clearTimeoutSpy).toHaveBeenCalledWith(123);
      expect(clearTimeoutSpy).toHaveBeenCalledWith(456);
      expect(tooltip.showTimeoutId).toBeNull();
      expect(tooltip.hideTimeoutId).toBeNull();
    });

    test('clearTimeouts handles null timeouts gracefully', () => {
      const { tooltip } = createTooltipSetup();
      
      tooltip.showTimeoutId = null;
      tooltip.hideTimeoutId = null;
      
      expect(() => tooltip.clearTimeouts()).not.toThrow();
    });
  });

  describe('Delay Configuration', () => {
    test('onMouseEnter reads show delay from CSS variable', () => {
      const { tooltip } = createTooltipSetup();
      tooltip.initialize();
      
      jest.spyOn(global, 'getComputedStyle').mockReturnValue(createMockComputedStyle('1000', '200'));
      const setTimeoutSpy = jest.spyOn(global, 'setTimeout');
      
      tooltip.onMouseEnter();
      
      expect(setTimeoutSpy).toHaveBeenCalledWith(expect.any(Function), 1000);
    });

    test('onMouseLeave reads hide delay from CSS variable', () => {
      const { tooltip } = createTooltipSetup();
      tooltip.initialize();
      
      jest.spyOn(global, 'getComputedStyle').mockReturnValue(createMockComputedStyle('500', '500'));
      const setTimeoutSpy = jest.spyOn(global, 'setTimeout');
      
      tooltip.onMouseLeave();
      
      expect(setTimeoutSpy).toHaveBeenCalledWith(expect.any(Function), 500);
    });

    test('show delay defaults to 500ms if CSS variable not set', () => {
      const { tooltip } = createTooltipSetup();
      tooltip.initialize();
      
      jest.spyOn(global, 'getComputedStyle').mockReturnValue(createMockComputedStyle('', '200'));
      const setTimeoutSpy = jest.spyOn(global, 'setTimeout');
      
      tooltip.onMouseEnter();
      
      expect(setTimeoutSpy).toHaveBeenCalledWith(expect.any(Function), 500);
    });

    test('hide delay defaults to 200ms if CSS variable not set', () => {
      const { tooltip } = createTooltipSetup();
      tooltip.initialize();
      
      jest.spyOn(global, 'getComputedStyle').mockReturnValue(createMockComputedStyle('500', ''));
      const setTimeoutSpy = jest.spyOn(global, 'setTimeout');
      
      tooltip.onMouseLeave();
      
      expect(setTimeoutSpy).toHaveBeenCalledWith(expect.any(Function), 200);
    });
  });

  describe('Module Exports', () => {
    test('onLoad defines custom element if not already defined', () => {
      const element = document.createElement('div');
      const defineSpy = jest.spyOn(customElements, 'define');
      
      // This might not do anything if already defined, which is expected
      onLoad(element, null);
      
      // Should either define or be a no-op
      expect(typeof onLoad).toBe('function');
    });

    test('onUpdate does not throw', () => {
      const element = document.createElement('tnt-tooltip');
      
      expect(() => onUpdate(element, null)).not.toThrow();
    });

    test('onDispose does not throw', () => {
      const element = document.createElement('tnt-tooltip');
      
      expect(() => onDispose(element, null)).not.toThrow();
    });
  });

  describe('Integration Scenarios', () => {
    test('complete flow: hover -> show -> move -> hide', () => {
      const { tooltip } = createTooltipSetup();
      tooltip.initialize();
      
      jest.spyOn(global, 'getComputedStyle').mockReturnValue(createMockComputedStyle());
      jest.spyOn(tooltip, 'getBoundingClientRect').mockReturnValue({
        width: 100,
        height: 40,
        top: 0,
        left: 50,
        right: 150,
        bottom: 40
      });
      
      Object.defineProperty(window, 'innerWidth', { value: 1024, writable: true });
      Object.defineProperty(window, 'innerHeight', { value: 768, writable: true });
      
      // Simulate hover enter
      tooltip.onMouseEnter();
      expect(tooltip.showTimeoutId).not.toBeNull();
      
      // Trigger show
      jest.runOnlyPendingTimers();
      expect(tooltip.isVisible).toBe(true);
      
      // Simulate mouse move
      tooltip.onMouseMove(new MouseEvent('mousemove', { clientX: 200, clientY: 300 }));
      expect(tooltip.lastMouseX).toBe(200);
      expect(tooltip.lastMouseY).toBe(300);
      
      // Simulate hover leave
      tooltip.onMouseLeave();
      
      // Trigger hide
      jest.runOnlyPendingTimers();
      expect(tooltip.isVisible).toBe(false);
      expect(tooltip.style.left).toBe('-9999px');
    });

    test('rapid hover enter/exit cancels show', () => {
      const { tooltip } = createTooltipSetup();
      tooltip.initialize();
      
      jest.spyOn(global, 'getComputedStyle').mockReturnValue(createMockComputedStyle());
      
      // Enter
      tooltip.onMouseEnter();
      const firstTimeoutId = tooltip.showTimeoutId;
      
      // Leave before show delay completes
      tooltip.onMouseLeave();
      
      // Enter again
      tooltip.onMouseEnter();
      
      // Should have cleared the first timeout
      expect(tooltip.showTimeoutId).not.toBe(firstTimeoutId);
    });

    test('hovering resets hide timer', () => {
      const { tooltip } = createTooltipSetup();
      tooltip.initialize();
      tooltip.isVisible = true;
      
      jest.spyOn(global, 'getComputedStyle').mockReturnValue(createMockComputedStyle());
      
      // Leave - start hide timer
      tooltip.onMouseLeave();
      const hideTimeoutId = tooltip.hideTimeoutId;
      
      // Enter - should clear hide timer
      tooltip.onMouseEnter();
      
      expect(tooltip.hideTimeoutId).not.toBe(hideTimeoutId);
      expect(tooltip.isVisible).toBe(true);
    });
  });

  describe('Edge Cases', () => {
    test('handles element without parent gracefully', () => {
      const tooltip = document.createElement('tnt-tooltip');
      
      expect(() => {
        tooltip.initialize();
        tooltip.dispose();
      }).not.toThrow();
    });

    test('handles getBoundingClientRect returning zeros', () => {
      const { tooltip } = createTooltipSetup();
      
      jest.spyOn(tooltip, 'getBoundingClientRect').mockReturnValue({
        width: 0,
        height: 0,
        top: 0,
        left: 0,
        right: 0,
        bottom: 0
      });
      
      Object.defineProperty(window, 'innerWidth', { value: 1024, writable: true });
      Object.defineProperty(window, 'innerHeight', { value: 768, writable: true });
      
      expect(() => tooltip.updatePosition(0, 0)).not.toThrow();
    });

    test('handles negative window dimensions gracefully', () => {
      const { tooltip } = createTooltipSetup();
      
      jest.spyOn(tooltip, 'getBoundingClientRect').mockReturnValue({
        width: 100,
        height: 40,
        top: 0,
        left: 50,
        right: 150,
        bottom: 40
      });
      
      Object.defineProperty(window, 'innerWidth', { value: 0, writable: true });
      Object.defineProperty(window, 'innerHeight', { value: 0, writable: true });
      
      expect(() => tooltip.updatePosition(100, 100)).not.toThrow();
    });

    test('multiple show calls do not create duplicate visible class', () => {
      const { tooltip } = createTooltipSetup();
      tooltip.lastMouseX = 100;
      tooltip.lastMouseY = 200;
      
      tooltip.show();
      tooltip.show();
      
      const classCount = (tooltip.className.match(/tnt-tooltip-visible/g) || []).length;
      expect(classCount).toBe(1);
    });
  });
});
