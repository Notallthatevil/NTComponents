/**
 * @jest-environment jsdom
 */
import { jest } from '@jest/globals';
import { onLoad, onUpdate, onDispose } from '../NTTooltip.razor.js';

describe('NTTooltip custom HTML element', () => {
  beforeEach(() => {
    document.body.innerHTML = '';
    jest.useFakeTimers();
  });

  afterEach(() => {
    jest.clearAllTimers();
    jest.useRealTimers();
    jest.restoreAllMocks();
  });

  const defineTooltip = () => {
    if (!customElements.get('nt-tooltip')) {
      onLoad(null, null);
    }
  };

  function createTooltipSetup() {
    defineTooltip();

    const parent = document.createElement('div');
    parent.style.position = 'relative';
    document.body.appendChild(parent);

    const tooltip = document.createElement('nt-tooltip');
    tooltip.style.setProperty('--nt-tooltip-show-delay', '500');
    tooltip.style.setProperty('--nt-tooltip-hide-delay', '200');
    parent.appendChild(tooltip);

    return { parent, tooltip };
  }

  function createMockComputedStyle(showDelay = '500', hideDelay = '200') {
    return {
      getPropertyValue: (prop) => {
        if (prop === '--nt-tooltip-show-delay') return showDelay;
        if (prop === '--nt-tooltip-hide-delay') return hideDelay;
        if (prop === '--nt-tooltip-position-offset') return '8';
        if (prop === '--nt-tooltip-viewport-margin') return '8';
        return '';
      }
    };
  }

  describe('Initialization', () => {
    test('connectedCallback attaches pointer and focus listeners to parent', () => {
      defineTooltip();
      const parent = document.createElement('div');
      document.body.appendChild(parent);
      const addEventListenerSpy = jest.spyOn(parent, 'addEventListener');
      const tooltip = document.createElement('nt-tooltip');

      parent.appendChild(tooltip);

      expect(addEventListenerSpy).toHaveBeenCalledWith('mouseenter', expect.any(Function));
      expect(addEventListenerSpy).toHaveBeenCalledWith('mouseleave', expect.any(Function));
      expect(addEventListenerSpy).toHaveBeenCalledWith('mousemove', expect.any(Function));
      expect(addEventListenerSpy).toHaveBeenCalledWith('focusin', expect.any(Function));
      expect(addEventListenerSpy).toHaveBeenCalledWith('focusout', expect.any(Function));
      expect(addEventListenerSpy).toHaveBeenCalledWith('keydown', expect.any(Function));
    });

    test('initialize sets hidden off-screen state and describes parent', () => {
      const { parent, tooltip } = createTooltipSetup();

      tooltip.initialize();

      expect(tooltip.style.left).toBe('-9999px');
      expect(tooltip.style.top).toBe('-9999px');
      expect(tooltip.getAttribute('aria-hidden')).toBe('true');
      expect(tooltip.id).toBeTruthy();
      expect(parent.getAttribute('aria-describedby')).toContain(tooltip.id);
    });

    test('initialize without parent element returns early', () => {
      defineTooltip();
      const tooltip = document.createElement('nt-tooltip');

      expect(() => tooltip.initialize()).not.toThrow();
    });
  });

  describe('Cleanup', () => {
    test('disconnectedCallback removes parent listeners and describedby', () => {
      const { parent, tooltip } = createTooltipSetup();
      const removeEventListenerSpy = jest.spyOn(parent, 'removeEventListener');

      tooltip.disconnectedCallback();

      expect(removeEventListenerSpy).toHaveBeenCalledWith('mouseenter', expect.any(Function));
      expect(removeEventListenerSpy).toHaveBeenCalledWith('mouseleave', expect.any(Function));
      expect(removeEventListenerSpy).toHaveBeenCalledWith('mousemove', expect.any(Function));
      expect(removeEventListenerSpy).toHaveBeenCalledWith('focusin', expect.any(Function));
      expect(removeEventListenerSpy).toHaveBeenCalledWith('focusout', expect.any(Function));
      expect(removeEventListenerSpy).toHaveBeenCalledWith('keydown', expect.any(Function));
      expect(parent.hasAttribute('aria-describedby')).toBe(false);
    });

    test('onUpdate reconciles changed tooltip id in parent describedby', () => {
      const { parent, tooltip } = createTooltipSetup();
      const originalId = tooltip.id;

      tooltip.id = 'changed-tooltip-id';
      onUpdate(tooltip, null);

      const describedBy = parent.getAttribute('aria-describedby');
      expect(describedBy).toBe('changed-tooltip-id');
      expect(describedBy).not.toContain(originalId);
    });

    test('focus describes the actual focused trigger descendant', () => {
      const { parent, tooltip } = createTooltipSetup();
      const button = document.createElement('button');
      parent.insertBefore(button, tooltip);

      tooltip.onFocusIn({ target: button });

      expect(button.getAttribute('aria-describedby')).toContain(tooltip.id);
    });

    test('disconnectedCallback clears pending timeouts', () => {
      const { tooltip } = createTooltipSetup();
      tooltip.showTimeoutId = 123;
      tooltip.hideTimeoutId = 456;
      const clearTimeoutSpy = jest.spyOn(global, 'clearTimeout');

      tooltip.disconnectedCallback();

      expect(clearTimeoutSpy).toHaveBeenCalledWith(123);
      expect(clearTimeoutSpy).toHaveBeenCalledWith(456);
    });
  });

  describe('Events', () => {
    test('hover and focus start show and hide delay timers from nt variables', () => {
      const { tooltip } = createTooltipSetup();
      jest.spyOn(global, 'getComputedStyle').mockReturnValue(createMockComputedStyle('1000', '300'));
      const setTimeoutSpy = jest.spyOn(global, 'setTimeout');

      tooltip.onMouseEnter();
      tooltip.onMouseLeave();
      tooltip.onFocusIn();
      tooltip.onFocusOut();

      expect(setTimeoutSpy).toHaveBeenCalledWith(expect.any(Function), 1000);
      expect(setTimeoutSpy).toHaveBeenCalledWith(expect.any(Function), 300);
    });

    test('zero show and hide delays are honored', () => {
      const { tooltip } = createTooltipSetup();
      jest.spyOn(global, 'getComputedStyle').mockReturnValue(createMockComputedStyle('0', '0'));
      const setTimeoutSpy = jest.spyOn(global, 'setTimeout');

      tooltip.onMouseEnter();
      tooltip.onMouseLeave();

      expect(setTimeoutSpy).toHaveBeenCalledWith(expect.any(Function), 0);
    });

    test('mouse move stores pointer coordinates and repositions visible tooltip', () => {
      const { tooltip } = createTooltipSetup();
      tooltip.isVisible = true;
      const updatePositionSpy = jest.spyOn(tooltip, 'updatePosition');

      tooltip.onMouseMove(new MouseEvent('mousemove', { clientX: 100, clientY: 200 }));

      expect(tooltip.lastPointerX).toBe(100);
      expect(tooltip.lastPointerY).toBe(200);
      expect(updatePositionSpy).toHaveBeenCalledWith();
    });

    test('focus clears pointer coordinates for keyboard positioning', () => {
      const { tooltip } = createTooltipSetup();
      tooltip.lastPointerX = 100;
      tooltip.lastPointerY = 200;
      jest.spyOn(global, 'getComputedStyle').mockReturnValue(createMockComputedStyle());

      tooltip.onFocusIn({ target: tooltip.parentElement });

      expect(tooltip.lastPointerX).toBeNull();
      expect(tooltip.lastPointerY).toBeNull();
    });

  });

  describe('Show and hide', () => {
    test('show toggles visible class, aria-hidden, and positions once', () => {
      const { tooltip } = createTooltipSetup();
      const updatePositionSpy = jest.spyOn(tooltip, 'updatePosition');
      const documentKeyDownSpy = jest.spyOn(document, 'addEventListener');
      const windowResizeSpy = jest.spyOn(window, 'addEventListener');

      tooltip.show();

      expect(tooltip.isVisible).toBe(true);
      expect(tooltip.getAttribute('aria-hidden')).toBe('false');
      expect(tooltip.classList.contains('nt-tooltip-visible')).toBe(true);
      expect(updatePositionSpy).toHaveBeenCalledWith();
      expect(documentKeyDownSpy).toHaveBeenCalledWith('keydown', expect.any(Function));
      expect(windowResizeSpy).toHaveBeenCalledWith('resize', expect.any(Function));
    });

    test('hide removes visible state and moves off-screen', () => {
      const { tooltip } = createTooltipSetup();
      tooltip.isVisible = true;
      tooltip.classList.add('nt-tooltip-visible');
      const documentKeyDownSpy = jest.spyOn(document, 'removeEventListener');
      const windowResizeSpy = jest.spyOn(window, 'removeEventListener');

      tooltip.hide();

      expect(tooltip.isVisible).toBe(false);
      expect(tooltip.getAttribute('aria-hidden')).toBe('true');
      expect(tooltip.classList.contains('nt-tooltip-visible')).toBe(false);
      expect(tooltip.style.left).toBe('-9999px');
      expect(tooltip.style.top).toBe('-9999px');
      expect(documentKeyDownSpy).toHaveBeenCalledWith('keydown', expect.any(Function));
      expect(windowResizeSpy).toHaveBeenCalledWith('resize', expect.any(Function));
    });

    test('Escape hides visible tooltip', () => {
      const { tooltip } = createTooltipSetup();
      tooltip.isVisible = true;
      tooltip.classList.add('nt-tooltip-visible');

      tooltip.onKeyDown(new KeyboardEvent('keydown', { key: 'Escape' }));

      expect(tooltip.isVisible).toBe(false);
    });
  });

  describe('Position updates', () => {
    test('updatePosition anchors to parent when cursor coordinates are absent', () => {
      const { parent, tooltip } = createTooltipSetup();
      jest.spyOn(parent, 'getBoundingClientRect').mockReturnValue({
        width: 80,
        height: 32,
        top: 200,
        left: 160,
        right: 240,
        bottom: 232
      });
      jest.spyOn(tooltip, 'getBoundingClientRect').mockReturnValue({
        width: 100,
        height: 40,
        top: 0,
        left: 50,
        right: 150,
        bottom: 40
      });
      jest.spyOn(global, 'getComputedStyle').mockReturnValue(createMockComputedStyle());
      Object.defineProperty(window, 'innerWidth', { value: 1024, writable: true });
      Object.defineProperty(window, 'innerHeight', { value: 768, writable: true });

      tooltip.updatePosition();

      expect(parseInt(tooltip.style.left)).toBeGreaterThan(0);
      expect(parseInt(tooltip.style.top)).toBeGreaterThan(0);
    });

    test('updatePosition constrains to viewport margin', () => {
      const { tooltip } = createTooltipSetup();
      jest.spyOn(tooltip, 'getBoundingClientRect').mockReturnValue({
        width: 100,
        height: 40,
        top: 0,
        left: 0,
        right: 100,
        bottom: 40
      });
      jest.spyOn(global, 'getComputedStyle').mockReturnValue(createMockComputedStyle());
      Object.defineProperty(window, 'innerWidth', { value: 120, writable: true });
      Object.defineProperty(window, 'innerHeight', { value: 80, writable: true });

      tooltip.updatePosition();

      expect(parseInt(tooltip.style.left)).toBeGreaterThanOrEqual(8);
      expect(parseInt(tooltip.style.top)).toBeGreaterThanOrEqual(8);
    });

    test('updatePosition follows stored pointer and clamps inside viewport', () => {
      const { tooltip } = createTooltipSetup();
      tooltip.lastPointerX = 115;
      tooltip.lastPointerY = 10;
      jest.spyOn(tooltip, 'getBoundingClientRect').mockReturnValue({
        width: 100,
        height: 40,
        top: 0,
        left: 0,
        right: 100,
        bottom: 40
      });
      jest.spyOn(global, 'getComputedStyle').mockReturnValue(createMockComputedStyle());
      Object.defineProperty(window, 'innerWidth', { value: 120, writable: true });
      Object.defineProperty(window, 'innerHeight', { value: 80, writable: true });

      tooltip.updatePosition();

      expect(parseInt(tooltip.style.left)).toBe(12);
      expect(parseInt(tooltip.style.top)).toBe(18);
    });

    test('visible tooltip repositions on viewport changes', () => {
      const { tooltip } = createTooltipSetup();
      tooltip.isVisible = true;
      const updatePositionSpy = jest.spyOn(tooltip, 'updatePosition');

      tooltip.onViewportChange();

      expect(updatePositionSpy).toHaveBeenCalledWith();
    });
  });

  describe('Module exports', () => {
    test('onLoad defines nt-tooltip custom element if not already defined', () => {
      onLoad(document.createElement('div'), null);

      expect(customElements.get('nt-tooltip')).toBeDefined();
    });

    test('onUpdate repositions visible tooltip after Blazor rerender', () => {
      const { tooltip } = createTooltipSetup();
      tooltip.isVisible = true;
      const updatePositionSpy = jest.spyOn(tooltip, 'updatePosition');

      onUpdate(tooltip, null);

      expect(updatePositionSpy).toHaveBeenCalledWith();
    });

    test('onDispose calls custom element dispose when available', () => {
      const { tooltip } = createTooltipSetup();
      const disposeSpy = jest.spyOn(tooltip, 'dispose');

      onDispose(tooltip, null);

      expect(disposeSpy).toHaveBeenCalled();
    });
  });
});
