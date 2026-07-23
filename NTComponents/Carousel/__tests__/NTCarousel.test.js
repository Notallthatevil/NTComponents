import { NTCarouselElement } from '../NTCarousel.razor.js';
import { jest } from '@jest/globals';

class ResizeObserverMock {
  observe = jest.fn();
  disconnect = jest.fn();
}

const mediaQuery = {
  matches: false,
  addEventListener: jest.fn(),
  removeEventListener: jest.fn()
};

global.ResizeObserver = ResizeObserverMock;
window.matchMedia = jest.fn(() => mediaQuery);

describe('NTCarousel', () => {
  beforeAll(() => {
    if (!customElements.get('nt-carousel')) {
      customElements.define('nt-carousel', NTCarouselElement);
    }
  });

  beforeEach(() => {
    document.body.innerHTML = '';
    mediaQuery.matches = false;
    jest.clearAllMocks();
  });

  afterEach(() => {
    jest.useRealTimers();
    jest.restoreAllMocks();
  });

  function createCarousel({ count = 6, height = 240, layout = 'multi-browse', rtl = false, snapping = true, width = 390, aspectRatios = [] } = {}) {
    const carousel = document.createElement('nt-carousel');
    carousel.setAttribute('aria-label', 'Test carousel');
    carousel.dataset.layout = layout;
    carousel.dataset.allowDragging = 'true';
    carousel.dataset.snap = snapping ? 'true' : 'false';
    if (rtl) carousel.style.direction = 'rtl';

    const viewport = document.createElement('div');
    viewport.dataset.carouselViewport = '';
    Object.defineProperty(viewport, 'clientWidth', { configurable: true, value: width });
    Object.defineProperty(viewport, 'clientHeight', { configurable: true, value: height });
    viewport.setPointerCapture = jest.fn();
    viewport.releasePointerCapture = jest.fn();
    viewport.hasPointerCapture = jest.fn(() => true);

    const track = document.createElement('div');
    track.dataset.carouselTrack = '';
    for (let index = 0; index < count; index++) {
      const item = document.createElement('nt-carousel-item');
      item.dataset.carouselItem = '';
      item.dataset.index = String(index);
      item.dataset.clickable = index === 0 ? 'true' : 'false';
      item.dataset.disabled = 'false';
      if (aspectRatios[index] != null) item.dataset.aspectRatio = String(aspectRatios[index]);
      item.tabIndex = index === 0 ? 0 : -1;
      const media = document.createElement('div');
      media.className = 'nt-carousel-item-media';
      const content = document.createElement('div');
      content.className = 'nt-carousel-item-content';
      content.textContent = `Item ${index + 1}`;
      item.append(media, content);
      track.appendChild(item);
    }
    viewport.appendChild(track);
    carousel.appendChild(viewport);
    document.body.appendChild(carousel);

    const dotNetRef = { invokeMethodAsync: jest.fn(() => Promise.resolve()) };
    carousel.update(dotNetRef);
    return { carousel, dotNetRef, items: [...track.querySelectorAll('[data-carousel-item]')], track, viewport };
  }

  const maskWidth = item => Number.parseFloat(item.style.getPropertyValue('--nt-carousel-mask-width'));
  const maskOffset = item => Number.parseFloat(item.style.getPropertyValue('--nt-carousel-mask-offset')) || 0;
  const screenMaskStart = (item, viewport) => Number.parseFloat(item.style.transform.match(/translate3d\(([-\d.]+)/)[1]) - viewport.scrollLeft + maskOffset(item);

  function installAnimationFrameClock() {
    let now = 0;
    let nextId = 1;
    const frames = new Map();
    jest.spyOn(performance, 'now').mockImplementation(() => now);
    jest.spyOn(window, 'requestAnimationFrame').mockImplementation(callback => {
      const id = nextId++;
      frames.set(id, callback);
      return id;
    });
    jest.spyOn(window, 'cancelAnimationFrame').mockImplementation(id => frames.delete(id));

    return {
      runAt(time) {
        now = time;
        const callbacks = [...frames.values()];
        frames.clear();
        callbacks.forEach(callback => callback(time));
      }
    };
  }

  test('creates Material large, medium, and small keylines', () => {
    const { carousel, items } = createCarousel();

    const widths = items.slice(0, 3).map(maskWidth);
    expect(carousel.dataset.enhanced).toBe('true');
    expect(widths[0]).toBeGreaterThan(widths[1]);
    expect(widths[1]).toBeGreaterThan(widths[2]);
    expect(widths[2]).toBeGreaterThanOrEqual(40);
    expect(widths[2]).toBeLessThanOrEqual(56);
    expect(items[2].dataset.visualSize).toBe('small');
  });

  test('interpolates masks continuously while native scroll changes', () => {
    const { carousel, items, viewport } = createCarousel();
    const initialFirstWidth = maskWidth(items[0]);
    const initialSecondWidth = maskWidth(items[1]);

    viewport.scrollLeft = carousel.containedLayout.step / 2;
    carousel.renderItems();

    expect(maskWidth(items[0])).toBeLessThan(initialFirstWidth);
    expect(maskWidth(items[1])).toBeGreaterThan(initialSecondWidth);
  });

  test('multi-browse advances through discrete trailing keyline states and reaches the final item', () => {
    const { carousel, items, viewport } = createCarousel();
    const step = carousel.containedLayout.step;
    const [large, medium, small] = items.slice(0, 3).map(maskWidth);

    expect(carousel.snapPositions).toEqual([0, step, step * 2, step * 3, (step * 3) + small + 8, (step * 3) + small + medium + 16]);
    expect(carousel.maxScroll).toBeLessThan(step * 5);

    viewport.scrollLeft = carousel.snapPositions.at(-2);
    carousel.renderItems();
    expect(items.slice(-3).map(item => item.dataset.visualSize)).toEqual(['small', 'large', 'medium']);

    viewport.scrollLeft = carousel.maxScroll;
    carousel.renderItems();
    const finalWidths = items.slice(-3).map(maskWidth);
    const finalStart = screenMaskStart(items.at(-1), viewport);
    expect(finalWidths[0]).toBeLessThan(finalWidths[1]);
    expect(finalWidths[1]).toBeLessThan(finalWidths[2]);
    expect(finalStart + finalWidths[2]).toBeCloseTo(viewport.clientWidth - 16);
    expect(finalWidths[2]).toBeCloseTo(large);
  });

  test('multi-browse items pin to the leading edge while squashing, then slide offscreen', () => {
    const { carousel, items, viewport } = createCarousel();
    const small = carousel.containedLayout.smallSize;

    expect(items[3].style.visibility).toBe('hidden');
    expect(maskWidth(items[3])).toBeCloseTo(small);

    viewport.scrollLeft = carousel.containedLayout.step * 0.5;
    carousel.renderItems();
    const halfwayWidth = maskWidth(items[0]);

    expect(screenMaskStart(items[0], viewport)).toBeCloseTo(0);
    expect(halfwayWidth).toBeGreaterThan(small);

    viewport.scrollLeft = carousel.containedLayout.step * 0.75;
    carousel.renderItems();
    const leadingStart = screenMaskStart(items[0], viewport);
    const leadingEnd = leadingStart + maskWidth(items[0]);
    const leadingGap = screenMaskStart(items[1], viewport) - leadingEnd;

    expect(leadingStart).toBeCloseTo(0);
    expect(maskWidth(items[0])).toBeGreaterThan(small);
    expect(maskWidth(items[0])).toBeLessThan(halfwayWidth);

    viewport.scrollLeft = carousel.containedLayout.step * 0.25;
    carousel.renderItems();
    const trailingGap = screenMaskStart(items[3], viewport) - (screenMaskStart(items[2], viewport) + maskWidth(items[2]));
    expect(leadingGap).toBeCloseTo(trailingGap);

    viewport.scrollLeft = carousel.containedLayout.step * 0.95;
    carousel.renderItems();
    expect(maskWidth(items[0])).toBeCloseTo(small);
    expect(screenMaskStart(items[0], viewport)).toBeLessThan(0);
  });

  test('expanded multi-browse shifts one trailing keyline at a time around multiple focal items', () => {
    const { carousel, items, viewport } = createCarousel({ width: 1022 });

    expect(items.map(item => item.dataset.visualSize)).toEqual(['large', 'large', 'large', 'large', 'medium', 'small']);
    viewport.scrollLeft = carousel.snapPositions.at(-2);
    carousel.renderItems();
    expect(items.map(item => item.dataset.visualSize)).toEqual(['small', 'large', 'large', 'large', 'large', 'medium']);
    viewport.scrollLeft = carousel.snapPositions.at(-1);
    carousel.renderItems();
    expect(items.map(item => item.dataset.visualSize)).toEqual(['small', 'medium', 'large', 'large', 'large', 'large']);
  });

  test('programmatic snapping animates unless reduced motion is requested and touch input cancels it', () => {
    const frames = [];
    const requestFrame = jest.spyOn(window, 'requestAnimationFrame').mockImplementation(callback => {
      frames.push(callback);
      return frames.length;
    });
    const { carousel, viewport } = createCarousel();
    const framesBeforeSnap = frames.length;

    carousel.goToIndex(1, false);
    expect(viewport.scrollLeft).toBe(0);
    expect(frames).toHaveLength(framesBeforeSnap + 1);
    viewport.dispatchEvent(new PointerEvent('pointerdown', { pointerId: 12, pointerType: 'touch' }));
    expect(carousel.animationFrame).toBeNull();
    requestFrame.mockRestore();

    mediaQuery.matches = true;
    const reduced = createCarousel();
    reduced.carousel.goToIndex(1, false);
    expect(reduced.viewport.scrollLeft).toBe(reduced.carousel.snapPositions[1]);
  });

  test('center hero shifts its first state to avoid an empty leading keyline', () => {
    const { items } = createCarousel({ layout: 'center-aligned-hero', width: 1022 });
    const starts = items.slice(0, 3).map(item => Number.parseFloat(item.style.transform.match(/translate3d\(([-\d.]+)/)[1]) + maskOffset(item));

    expect(starts[0]).toBe(16);
    expect(starts[1]).toBeGreaterThan(starts[0] + maskWidth(items[0]));
    expect(starts[2]).toBeGreaterThan(starts[1]);
  });

  test('hero mirrors its final state so the last large item reaches the trailing edge', () => {
    const { carousel, items, viewport } = createCarousel({ layout: 'hero' });
    viewport.scrollLeft = carousel.snapPositions.at(-1);
    carousel.renderItems();

    const previousStart = screenMaskStart(items.at(-2), viewport);
    const lastStart = screenMaskStart(items.at(-1), viewport);
    expect(previousStart).toBeCloseTo(16);
    expect(lastStart + maskWidth(items.at(-1))).toBeCloseTo(viewport.clientWidth - 16);
  });

  test('applies parallax only to the media layer through a CSS variable', () => {
    const { items } = createCarousel();
    const media = items[0].querySelector('.nt-carousel-item-media');
    const content = items[0].querySelector('.nt-carousel-item-content');

    expect(items[0].style.getPropertyValue('--nt-carousel-parallax-x')).not.toBe('0px');
    expect(media.style.transform).toBe('');
    expect(content.style.transform).toBe('');
  });

  test('keeps contained media at the large keyline width while its mask morphs', () => {
    const { carousel, items, viewport } = createCarousel();
    const initialMaskWidth = maskWidth(items[0]);
    const itemWidth = items[0].style.inlineSize;
    const contentWidth = items[0].style.getPropertyValue('--nt-carousel-item-width');
    const mediaWidth = items[0].style.getPropertyValue('--nt-carousel-media-width');

    viewport.scrollLeft = carousel.containedLayout.step / 2;
    carousel.renderItems();

    expect(maskWidth(items[0])).toBeLessThan(initialMaskWidth);
    expect(items[0].style.inlineSize).toBe(itemWidth);
    expect(items[0].style.getPropertyValue('--nt-carousel-item-width')).toBe(contentWidth);
    expect(items[0].style.getPropertyValue('--nt-carousel-media-width')).toBe(mediaWidth);
    expect(Number.parseFloat(mediaWidth)).toBeGreaterThan(initialMaskWidth);
  });

  test('reduced motion uses equal edge-reaching sizes and removes parallax', () => {
    mediaQuery.matches = true;
    const { carousel, items } = createCarousel();

    expect(carousel.dataset.reducedMotion).toBe('true');
    expect(maskWidth(items[0])).toBeCloseTo(maskWidth(items[1]));
    expect(items[0].style.getPropertyValue('--nt-carousel-parallax-x')).toBe('0px');
    expect(Number.parseFloat(items[0].style.transform.match(/translate3d\(([-\d.]+)/)[1])).toBe(0);
  });

  test('mouse drag follows the pointer and suppresses the accidental click', () => {
    const { carousel, viewport } = createCarousel({ snapping: false });
    const down = new PointerEvent('pointerdown', { button: 0, clientX: 180, pointerId: 7, pointerType: 'mouse' });
    const move = new PointerEvent('pointermove', { clientX: 80, pointerId: 7, pointerType: 'mouse' });
    const up = new PointerEvent('pointerup', { clientX: 80, pointerId: 7, pointerType: 'mouse' });

    viewport.dispatchEvent(down);
    viewport.dispatchEvent(move);
    expect(viewport.scrollLeft).toBeGreaterThan(0);
    viewport.dispatchEvent(up);

    const click = new MouseEvent('click', { bubbles: true, cancelable: true });
    carousel.querySelector('[data-carousel-item]').dispatchEvent(click);
    expect(click.defaultPrevented).toBe(true);
  });

  test('active dragging transfers to the replacement element during interactive hydration', () => {
    const original = createCarousel({ count: 14, snapping: false });
    original.viewport.dispatchEvent(new PointerEvent('pointerdown', { button: 0, clientX: 180, pointerId: 17, pointerType: 'mouse' }));
    original.viewport.dispatchEvent(new PointerEvent('pointermove', { clientX: 80, pointerId: 17, pointerType: 'mouse' }));
    expect(original.viewport.scrollLeft).toBe(100);

    original.carousel.remove();
    const replacement = createCarousel({ count: 14, snapping: false });
    replacement.carousel.restoreTransferredInteraction();
    expect(replacement.viewport.scrollLeft).toBe(100);
    expect(replacement.carousel.dragState?.pointerId).toBe(17);

    replacement.viewport.dispatchEvent(new PointerEvent('pointermove', { clientX: 30, pointerId: 17, pointerType: 'mouse' }));
    expect(replacement.viewport.scrollLeft).toBe(150);
    replacement.viewport.dispatchEvent(new PointerEvent('pointerup', { clientX: 30, pointerId: 17, pointerType: 'mouse' }));
    expect(replacement.carousel.dragState).toBeNull();
  });

  test('hero fling advances at most one dominant item', () => {
    mediaQuery.matches = true;
    const { carousel, viewport } = createCarousel({ layout: 'hero' });
    viewport.dispatchEvent(new PointerEvent('pointerdown', { button: 0, clientX: 360, pointerId: 8, pointerType: 'mouse' }));
    viewport.dispatchEvent(new PointerEvent('pointermove', { clientX: 20, pointerId: 8, pointerType: 'mouse' }));
    viewport.dispatchEvent(new PointerEvent('pointerup', { clientX: 20, pointerId: 8, pointerType: 'mouse' }));

    expect(carousel.activeIndex).toBe(1);
  });

  test('keyboard arrows move the roving tab stop and notify the settled index', async () => {
    mediaQuery.matches = true;
    const { dotNetRef, items } = createCarousel();
    items[0].focus();

    items[0].dispatchEvent(new KeyboardEvent('keydown', { bubbles: true, key: 'ArrowRight' }));
    await Promise.resolve();

    expect(items[0].tabIndex).toBe(-1);
    expect(items[1].tabIndex).toBe(0);
    expect(document.activeElement).toBe(items[1]);
    expect(dotNetRef.invokeMethodAsync).toHaveBeenCalledWith('NotifyIndexChangedAsync', 1);
  });

  test('multi-aspect uncontained layout honors item ratios without morphing', () => {
    const { items } = createCarousel({ layout: 'uncontained-multi-aspect-ratio', aspectRatios: [9 / 16, 1, 16 / 9] });

    const widths = items.slice(0, 3).map(item => Number.parseFloat(item.style.inlineSize));
    expect(widths[0]).toBeLessThan(widths[1]);
    expect(widths[1]).toBeLessThan(widths[2]);
    expect(items[0].dataset.visualSize).toBe('large');
  });

  test('full-screen layout scrolls vertically with edge-to-edge items', () => {
    const { carousel, items, track } = createCarousel({ height: 500, layout: 'full-screen', width: 320 });

    expect(items[0].style.inlineSize).toBe('320px');
    expect(items[0].style.blockSize).toBe('500px');
    expect(items[1].style.transform).toContain('516px');
    expect(Number.parseFloat(track.style.blockSize)).toBeGreaterThan(500);
    expect(carousel.snapPositions[1]).toBe(516);
  });

  test('RTL starts at the physical end while preserving logical index zero', () => {
    const { carousel, viewport } = createCarousel({ rtl: true });

    expect(viewport.scrollLeft).toBe(carousel.maxScroll);
    expect(carousel.activeIndex).toBe(0);
  });

  test('focus pauses autoplay and cleanup releases observers and listeners', () => {
    const { carousel, items } = createCarousel();
    carousel.dataset.autoPlayInterval = '1';
    const control = document.createElement('button');
    control.dataset.autoplayControl = '';
    carousel.prepend(control);
    carousel.update();

    items[0].focus();
    expect(control.textContent).toBe('Start rotation');

    const observer = carousel.resizeObserver;
    carousel.removeElementListeners();
    expect(observer.disconnect).toHaveBeenCalled();
  });

  test('autoplay smoothly returns to item one, notifies settlement, and schedules the next interval', () => {
    jest.useFakeTimers();
    const clock = installAnimationFrameClock();
    const { carousel, dotNetRef, items, viewport } = createCarousel();
    clock.runAt(0);
    carousel.dataset.autoPlayInterval = '1';
    carousel.update();
    carousel.activeIndex = items.length - 1;
    carousel.lastNotifiedIndex = items.length - 1;
    viewport.scrollLeft = carousel.maxScroll;
    carousel.scheduleAutoPlay();
    const finalPosition = carousel.maxScroll;

    jest.advanceTimersByTime(1000);

    expect(carousel.activeIndex).toBe(0);
    expect(viewport.scrollLeft).toBe(finalPosition);
    expect(dotNetRef.invokeMethodAsync).not.toHaveBeenCalled();

    clock.runAt(100);
    expect(viewport.scrollLeft).toBeGreaterThan(0);
    expect(viewport.scrollLeft).toBeLessThan(finalPosition);

    clock.runAt(600);
    expect(viewport.scrollLeft).toBe(0);
    expect(items[0].tabIndex).toBe(0);
    expect(items.slice(1).every(item => item.tabIndex === -1)).toBe(true);
    expect(dotNetRef.invokeMethodAsync).toHaveBeenCalledWith('NotifyIndexChangedAsync', 0);

    jest.advanceTimersByTime(1000);
    expect(carousel.activeIndex).toBe(1);
    clock.runAt(700);
    expect(viewport.scrollLeft).toBeGreaterThan(0);
    expect(viewport.scrollLeft).toBeLessThan(carousel.snapPositions[1]);
    clock.runAt(1200);
    expect(viewport.scrollLeft).toBe(carousel.snapPositions[1]);
    expect(dotNetRef.invokeMethodAsync).toHaveBeenCalledWith('NotifyIndexChangedAsync', 1);
  });

  test('autoplay skips duplicate multi-browse snaps and keeps rotating after settlement', () => {
    jest.useFakeTimers();
    const clock = installAnimationFrameClock();
    const { carousel, dotNetRef, items, viewport } = createCarousel({ width: 1022 });
    clock.runAt(0);
    carousel.dataset.autoPlayInterval = '1';
    carousel.update();
    jest.advanceTimersByTime(1000);

    expect(carousel.snapPositions.slice(0, 4)).toEqual([0, 0, 0, 0]);
    expect(carousel.activeIndex).toBe(4);
    expect(viewport.scrollLeft).toBe(0);

    clock.runAt(100);
    expect(viewport.scrollLeft).toBeGreaterThan(0);
    expect(viewport.scrollLeft).toBeLessThan(carousel.snapPositions[4]);

    clock.runAt(600);
    expect(viewport.scrollLeft).toBe(carousel.snapPositions[4]);
    expect(items[4].tabIndex).toBe(0);
    expect(dotNetRef.invokeMethodAsync).toHaveBeenCalledWith('NotifyIndexChangedAsync', 4);

    jest.advanceTimersByTime(1000);
    expect(carousel.activeIndex).toBe(5);
    clock.runAt(700);
    expect(viewport.scrollLeft).toBeGreaterThan(carousel.snapPositions[4]);
    expect(viewport.scrollLeft).toBeLessThan(carousel.snapPositions[5]);
    clock.runAt(1200);
    expect(viewport.scrollLeft).toBe(carousel.snapPositions[5]);
    expect(dotNetRef.invokeMethodAsync).toHaveBeenCalledWith('NotifyIndexChangedAsync', 5);
  });

  test('explicitly resumed reduced-motion autoplay wraps immediately and continues rotating', () => {
    jest.useFakeTimers();
    const clock = installAnimationFrameClock();
    mediaQuery.matches = true;
    const { carousel, dotNetRef, items, viewport } = createCarousel();
    const control = document.createElement('button');
    control.dataset.autoplayControl = '';
    carousel.prepend(control);
    carousel.dataset.autoPlayInterval = '1';
    carousel.update();
    clock.runAt(0);

    expect(control.textContent).toBe('Start rotation');
    carousel.activeIndex = items.length - 1;
    carousel.lastNotifiedIndex = items.length - 1;
    viewport.scrollLeft = carousel.maxScroll;
    control.click();
    jest.advanceTimersByTime(1000);

    expect(viewport.scrollLeft).toBe(0);
    expect(carousel.activeIndex).toBe(0);
    expect(items[0].tabIndex).toBe(0);
    expect(control.textContent).toBe('Pause rotation');
    expect(dotNetRef.invokeMethodAsync).toHaveBeenCalledWith('NotifyIndexChangedAsync', 0);

    jest.advanceTimersByTime(1000);
    expect(viewport.scrollLeft).toBe(carousel.snapPositions[1]);
    expect(carousel.activeIndex).toBe(1);
    expect(dotNetRef.invokeMethodAsync).toHaveBeenCalledWith('NotifyIndexChangedAsync', 1);
  });
});
