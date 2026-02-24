/**
 * @jest-environment jsdom
 */
import { jest } from '@jest/globals';
import { onLoad, onUpdate, onDispose } from '../TnTTypeahead.razor.js';

describe('TnTTypeahead overlay behavior', () => {
  const baseInputRect = {
    width: 220,
    height: 40,
    top: 120,
    left: 100,
    right: 320,
    bottom: 160,
    x: 100,
    y: 120,
  };

  const baseDropdownRect = {
    width: 260,
    height: 180,
    top: 0,
    left: 0,
    right: 260,
    bottom: 180,
    x: 0,
    y: 0,
  };

  function createRect(baseRect, overrides = {}) {
    const rect = {
      ...baseRect,
      ...overrides,
      toJSON: () => { },
    };

    if (overrides.left !== undefined && overrides.x === undefined) {
      rect.x = rect.left;
    }
    if (overrides.top !== undefined && overrides.y === undefined) {
      rect.y = rect.top;
    }
    if ((overrides.left !== undefined || overrides.width !== undefined) && overrides.right === undefined) {
      rect.right = rect.left + rect.width;
    }
    if ((overrides.top !== undefined || overrides.height !== undefined) && overrides.bottom === undefined) {
      rect.bottom = rect.top + rect.height;
    }

    return rect;
  }

  function createTypeahead({ includeInput = true, includeDropdown = true } = {}) {
    const root = document.createElement('div');
    root.className = 'tnt-typeahead';

    const box = document.createElement('span');
    box.className = 'tnt-typeahead-box';

    let input = null;
    if (includeInput) {
      input = document.createElement('input');
      box.appendChild(input);
    }

    let dropdown = null;
    if (includeDropdown) {
      dropdown = document.createElement('div');
      dropdown.className = 'tnt-typeahead-content';
    }

    root.appendChild(box);
    if (dropdown) {
      root.appendChild(dropdown);
    }
    document.body.appendChild(root);

    return { root, input, dropdown };
  }

  function mockRects(input, dropdown, { inputRect = {}, dropdownRect = {} } = {}) {
    if (input) {
      jest.spyOn(input, 'getBoundingClientRect').mockReturnValue(createRect(baseInputRect, inputRect));
    }
    if (dropdown) {
      const rect = createRect(baseDropdownRect, dropdownRect);
      jest.spyOn(dropdown, 'getBoundingClientRect').mockReturnValue(rect);
    }
  }

  function px(value) {
    return Number.parseFloat(value);
  }

  beforeEach(() => {
    document.body.innerHTML = '';
    jest.clearAllMocks();

    Object.defineProperty(window, 'innerWidth', { value: 1024, configurable: true });
    Object.defineProperty(window, 'innerHeight', { value: 768, configurable: true });

    jest.spyOn(global, 'requestAnimationFrame').mockImplementation(callback => {
      callback();
      return 1;
    });
    jest.spyOn(global, 'cancelAnimationFrame').mockImplementation(() => { });
  });

  afterEach(() => {
    jest.restoreAllMocks();
  });

  test('shows dropdown and places it below input when there is space', () => {
    const { root, input, dropdown } = createTypeahead();
    mockRects(input, dropdown);

    onLoad(root, null);

    const left = px(dropdown.style.left);
    const top = px(dropdown.style.top);

    expect(dropdown.style.visibility).toBe('visible');
    expect(dropdown.classList.contains('tnt-typeahead-fixed')).toBe(true);
    expect(top).toBeGreaterThanOrEqual(baseInputRect.bottom);
    expect(left).toBeGreaterThanOrEqual(0);
    expect(left + baseDropdownRect.width).toBeLessThanOrEqual(window.innerWidth);
  });

  test('positions dropdown above input when constrained near viewport bottom', () => {
    const { root, input, dropdown } = createTypeahead();
    mockRects(input, dropdown, {
      inputRect: {
        top: 700,
        bottom: 740,
      },
      dropdownRect: {
        height: 200,
      },
    });

    onUpdate(root, null);

    const top = px(dropdown.style.top);
    expect(top + 200).toBeLessThanOrEqual(700);
    expect(dropdown.style.visibility).toBe('visible');
  });

  test('keeps dropdown within horizontal viewport near the right edge', () => {
    const { root, input, dropdown } = createTypeahead();
    Object.defineProperty(window, 'innerWidth', { value: 360, configurable: true });
    mockRects(input, dropdown, {
      inputRect: {
        left: 300,
        width: 40,
      },
      dropdownRect: {
        width: 220,
      },
    });

    onLoad(root, null);

    const left = px(dropdown.style.left);
    expect(left).toBeGreaterThanOrEqual(0);
    expect(left + 220).toBeLessThanOrEqual(window.innerWidth);
  });

  test('safe fallback keeps dropdown on-screen in very short viewports', () => {
    const { root, input, dropdown } = createTypeahead();
    Object.defineProperty(window, 'innerHeight', { value: 140, configurable: true });
    mockRects(input, dropdown, {
      inputRect: {
        top: 56,
        bottom: 96,
      },
      dropdownRect: {
        height: 220,
      },
    });

    onUpdate(root, null);

    const top = px(dropdown.style.top);
    expect(Number.isFinite(top)).toBe(true);
    expect(top).toBeGreaterThanOrEqual(0);
    expect(dropdown.style.visibility).toBe('visible');
  });

  test('onUpdate handles removed dropdown safely and clears detached styling', () => {
    const { root, input, dropdown } = createTypeahead();
    mockRects(input, dropdown);

    onLoad(root, null);
    dropdown.remove();

    expect(() => onUpdate(root, null)).not.toThrow();
    expect(dropdown.classList.contains('tnt-typeahead-fixed')).toBe(false);
    expect(dropdown.style.left).toBe('');
    expect(dropdown.style.top).toBe('');
  });

  test('onUpdate handles missing input safely and clears overlay styling', () => {
    const { root, input, dropdown } = createTypeahead();
    mockRects(input, dropdown);

    onLoad(root, null);
    input.remove();

    expect(() => onUpdate(root, null)).not.toThrow();
    expect(dropdown.classList.contains('tnt-typeahead-fixed')).toBe(false);
    expect(dropdown.style.left).toBe('');
    expect(dropdown.style.top).toBe('');
  });

  test('dispose removes overlay state and later global events do not re-apply positioning', () => {
    const { root, input, dropdown } = createTypeahead();
    mockRects(input, dropdown);

    onLoad(root, null);
    expect(dropdown.classList.contains('tnt-typeahead-fixed')).toBe(true);

    onDispose(root, null);

    expect(dropdown.classList.contains('tnt-typeahead-fixed')).toBe(false);
    const leftAfterDispose = dropdown.style.left;
    const topAfterDispose = dropdown.style.top;

    window.dispatchEvent(new Event('resize'));
    window.dispatchEvent(new Event('scroll'));

    expect(dropdown.style.left).toBe(leftAfterDispose);
    expect(dropdown.style.top).toBe(topAfterDispose);
  });

  test('lifecycle calls are safe before initialization, after disposal, and with null', () => {
    const { root, input, dropdown } = createTypeahead();
    mockRects(input, dropdown);

    expect(() => onUpdate(root, null)).not.toThrow();
    expect(() => onDispose(root, null)).not.toThrow();

    onLoad(root, null);
    onDispose(root, null);

    expect(() => onUpdate(root, null)).not.toThrow();
    expect(() => onDispose(root, null)).not.toThrow();

    expect(() => onLoad(null, null)).not.toThrow();
    expect(() => onUpdate(null, null)).not.toThrow();
    expect(() => onDispose(null, null)).not.toThrow();
  });

  test('outside click closes dropdown through dotnet callback', () => {
    const { root, input, dropdown } = createTypeahead();
    mockRects(input, dropdown);
    const dotNetRef = { invokeMethodAsync: jest.fn() };
    const outside = document.createElement('button');
    document.body.appendChild(outside);

    onLoad(root, dotNetRef);

    outside.dispatchEvent(new MouseEvent('mousedown', { bubbles: true }));
    outside.dispatchEvent(new MouseEvent('click', { bubbles: true }));

    expect(dotNetRef.invokeMethodAsync).toHaveBeenCalledWith('CloseDropdownFromJs');
  });

  test('dragging from inside to outside does not close dropdown', () => {
    const { root, input, dropdown } = createTypeahead();
    mockRects(input, dropdown);
    const dotNetRef = { invokeMethodAsync: jest.fn() };
    const outside = document.createElement('button');
    document.body.appendChild(outside);

    onLoad(root, dotNetRef);

    input.dispatchEvent(new MouseEvent('mousedown', { bubbles: true }));
    outside.dispatchEvent(new MouseEvent('click', { bubbles: true }));

    expect(dotNetRef.invokeMethodAsync).not.toHaveBeenCalled();
  });

  test('moving focus outside closes dropdown (tab-out behavior)', () => {
    const { root, input, dropdown } = createTypeahead();
    mockRects(input, dropdown);
    const dotNetRef = { invokeMethodAsync: jest.fn() };
    const outside = document.createElement('button');
    document.body.appendChild(outside);

    onLoad(root, dotNetRef);

    outside.dispatchEvent(new FocusEvent('focusin', { bubbles: true }));

    expect(dotNetRef.invokeMethodAsync).toHaveBeenCalledWith('CloseDropdownFromJs');
  });

  test('focus inside does not close dropdown', () => {
    const { root, input, dropdown } = createTypeahead();
    mockRects(input, dropdown);
    const dotNetRef = { invokeMethodAsync: jest.fn() };

    onLoad(root, dotNetRef);

    input.dispatchEvent(new FocusEvent('focusin', { bubbles: true }));

    expect(dotNetRef.invokeMethodAsync).not.toHaveBeenCalled();
  });

  test('missing dropdown from the start is handled safely across lifecycle', () => {
    const { root, input } = createTypeahead({ includeDropdown: false });
    mockRects(input, null);

    expect(() => onLoad(root, null)).not.toThrow();
    expect(() => onUpdate(root, null)).not.toThrow();
    expect(() => onDispose(root, null)).not.toThrow();
  });

  test('arrow-focused item outside viewport scrolls into view', () => {
    const { root, input, dropdown } = createTypeahead();
    mockRects(input, dropdown);
    const list = document.createElement('ul');
    const focusedItem = document.createElement('li');
    focusedItem.className = 'tnt-typeahead-list-item tnt-focused';
    list.appendChild(focusedItem);
    dropdown.appendChild(list);

    Object.defineProperty(dropdown, 'clientHeight', { configurable: true, value: 100 });
    dropdown.scrollTop = 0;
    Object.defineProperty(focusedItem, 'offsetTop', { configurable: true, value: 160 });
    Object.defineProperty(focusedItem, 'offsetHeight', { configurable: true, value: 30 });

    onUpdate(root, null);

    expect(dropdown.scrollTop).toBe(90);
  });

  test('scroll event from dropdown does not trigger overlay reposition raf', () => {
    const { root, input, dropdown } = createTypeahead();
    mockRects(input, dropdown);

    onLoad(root, null);
    const rafCallsBefore = global.requestAnimationFrame.mock.calls.length;

    dropdown.dispatchEvent(new Event('scroll'));

    expect(global.requestAnimationFrame.mock.calls.length).toBe(rafCallsBefore);
  });
});
