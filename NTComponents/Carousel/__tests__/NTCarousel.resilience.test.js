import { NTCarouselElement } from '../NTCarousel.razor.js';
import { jest } from '@jest/globals';

class ResizeObserverMock {
  observe = jest.fn();
  disconnect = jest.fn();
}

const reducedMotionListeners = new Set();
const mediaQuery = {
  matches: false,
  addEventListener: jest.fn((eventName, listener) => {
    if (eventName === 'change') reducedMotionListeners.add(listener);
  }),
  removeEventListener: jest.fn((eventName, listener) => {
    if (eventName === 'change') reducedMotionListeners.delete(listener);
  })
};

global.ResizeObserver = ResizeObserverMock;
window.matchMedia = jest.fn(() => mediaQuery);

describe('NTCarousel autoplay resilience', () => {
  beforeAll(() => {
    if (!customElements.get('nt-carousel')) {
      customElements.define('nt-carousel', NTCarouselElement);
    }
  });

  beforeEach(() => {
    jest.useFakeTimers();
    document.body.innerHTML = '';
    Object.defineProperty(document, 'hidden', { configurable: true, value: false });
    mediaQuery.matches = false;
    reducedMotionListeners.clear();
    jest.clearAllMocks();
  });

  afterEach(() => {
    for (const carousel of document.querySelectorAll('nt-carousel')) {
      carousel.removeElementListeners();
    }
    document.body.innerHTML = '';
    jest.restoreAllMocks();
    jest.useRealTimers();
  });

  function installAnimationFrameQueue() {
    let nextId = 1;
    const frames = new Map();
    const request = jest.spyOn(window, 'requestAnimationFrame').mockImplementation(callback => {
      const id = nextId++;
      frames.set(id, callback);
      return id;
    });
    const cancel = jest.spyOn(window, 'cancelAnimationFrame').mockImplementation(id => frames.delete(id));
    const flush = now => {
      const callbacks = [...frames.values()];
      frames.clear();
      for (const callback of callbacks) callback(now);
    };
    return { cancel, flush, frames, request };
  }

  function createCarousel({ count = 6, intervalSeconds = 1, snapping = true, width = 390 } = {}) {
    const carousel = document.createElement('nt-carousel');
    carousel.setAttribute('aria-label', 'Autoplay resilience carousel');
    carousel.dataset.layout = 'multi-browse';
    carousel.dataset.allowDragging = 'true';
    carousel.dataset.snap = snapping ? 'true' : 'false';
    carousel.dataset.autoPlayInterval = String(intervalSeconds);

    const control = document.createElement('button');
    control.dataset.autoplayControl = '';
    const viewport = document.createElement('div');
    viewport.dataset.carouselViewport = '';
    Object.defineProperty(viewport, 'clientWidth', { configurable: true, value: width });
    Object.defineProperty(viewport, 'clientHeight', { configurable: true, value: 240 });
    viewport.setPointerCapture = jest.fn();
    viewport.releasePointerCapture = jest.fn();
    viewport.hasPointerCapture = jest.fn(() => true);

    const track = document.createElement('div');
    track.dataset.carouselTrack = '';
    for (let index = 0; index < count; index++) {
      const item = document.createElement('nt-carousel-item');
      item.dataset.carouselItem = '';
      item.dataset.clickable = 'false';
      item.dataset.disabled = 'false';
      item.tabIndex = index === 0 ? 0 : -1;
      const content = document.createElement('div');
      content.className = 'nt-carousel-item-content';
      content.textContent = `Item ${index + 1}`;
      item.appendChild(content);
      track.appendChild(item);
    }

    viewport.appendChild(track);
    carousel.append(control, viewport);
    document.body.appendChild(carousel);
    const dotNetRef = { invokeMethodAsync: jest.fn(() => Promise.resolve()) };
    carousel.update(dotNetRef);
    return { carousel, control, dotNetRef, items: [...track.children], viewport };
  }

  function dispatchReducedMotionChange(matches) {
    mediaQuery.matches = matches;
    for (const listener of [...reducedMotionListeners]) listener({ matches, media: mediaQuery });
  }

  test('nested autoplay controls toggle only their owning carousel', () => {
    const outer = createCarousel();
    const inner = createCarousel();
    outer.items[0].appendChild(inner.carousel);

    inner.control.click();

    expect(inner.control.textContent).toBe('Start rotation');
    expect(outer.control.textContent).toBe('Pause rotation');
  });

  test('hover pauses the pending interval and pointer leave resumes with one fresh interval', () => {
    installAnimationFrameQueue();
    const { carousel } = createCarousel();
    const animateToIndex = jest.spyOn(carousel, 'animateToIndex').mockImplementation(() => {});

    carousel.dispatchEvent(new PointerEvent('pointerenter'));
    jest.advanceTimersByTime(1500);
    expect(animateToIndex).not.toHaveBeenCalled();
    expect(carousel.autoPlayTimer).toBeNull();

    carousel.dispatchEvent(new PointerEvent('pointerleave'));
    carousel.dispatchEvent(new PointerEvent('pointerleave'));
    expect(jest.getTimerCount()).toBe(1);
    jest.advanceTimersByTime(999);
    expect(animateToIndex).not.toHaveBeenCalled();
    jest.advanceTimersByTime(1);
    expect(animateToIndex).toHaveBeenCalledTimes(1);
  });

  test('focus pause survives hover and visibility recovery until the user explicitly restarts rotation', async () => {
    installAnimationFrameQueue();
    const { carousel, control, items } = createCarousel();
    const animateToIndex = jest.spyOn(carousel, 'animateToIndex').mockImplementation(() => {});

    items[0].focus();
    carousel.dispatchEvent(new PointerEvent('pointerleave'));
    Object.defineProperty(document, 'hidden', { configurable: true, value: true });
    document.dispatchEvent(new Event('visibilitychange'));
    Object.defineProperty(document, 'hidden', { configurable: true, value: false });
    document.dispatchEvent(new Event('visibilitychange'));
    jest.advanceTimersByTime(2000);

    expect(animateToIndex).not.toHaveBeenCalled();
    expect(control.textContent).toBe('Start rotation');

    control.click();
    await Promise.resolve();
    expect(control.textContent).toBe('Pause rotation');
    jest.advanceTimersByTime(1000);
    expect(animateToIndex).toHaveBeenCalledTimes(1);
  });

  test('mouse drag suppresses autoplay until pointer release settles, then schedules the next interval', () => {
    const animationFrames = installAnimationFrameQueue();
    const { carousel, viewport } = createCarousel();
    const animateToIndex = jest.spyOn(carousel, 'animateToIndex');

    viewport.dispatchEvent(new PointerEvent('pointerdown', { button: 0, clientX: 180, pointerId: 7, pointerType: 'mouse' }));
    viewport.dispatchEvent(new PointerEvent('pointermove', { clientX: 80, pointerId: 7, pointerType: 'mouse' }));
    jest.advanceTimersByTime(1500);
    expect(animateToIndex).not.toHaveBeenCalled();

    viewport.dispatchEvent(new PointerEvent('pointerup', { clientX: 80, pointerId: 7, pointerType: 'mouse' }));
    expect(animateToIndex).toHaveBeenCalledTimes(1);
    animationFrames.flush(performance.now() + 1000);
    expect(carousel.autoPlayTimer).not.toBeNull();
  });

  test('wheel cancels an active autoplay animation and scroll settlement restores rotation', () => {
    const animationFrames = installAnimationFrameQueue();
    const { carousel, viewport } = createCarousel();

    jest.advanceTimersByTime(1000);
    expect(carousel.animationFrame).not.toBeNull();
    const activeFrame = carousel.animationFrame;

    viewport.dispatchEvent(new WheelEvent('wheel'));
    expect(carousel.animationFrame).toBeNull();
    expect(animationFrames.cancel).toHaveBeenCalledWith(activeFrame);
    expect(carousel.autoPlayTimer).not.toBeNull();
    expect(jest.getTimerCount()).toBe(1);

    viewport.dispatchEvent(new Event('scrollend'));
    expect(carousel.autoPlayTimer).not.toBeNull();
    expect(jest.getTimerCount()).toBe(1);
  });

  test('a canceled animation frame cannot mutate scroll state if its callback arrives late', () => {
    const animationFrames = installAnimationFrameQueue();
    const { carousel, viewport } = createCarousel();

    jest.advanceTimersByTime(1000);
    const staleCallback = animationFrames.frames.get(carousel.animationFrame);
    viewport.dispatchEvent(new WheelEvent('wheel'));
    const scrollAfterCancellation = viewport.scrollLeft;

    staleCallback(performance.now() + 1000);
    expect(viewport.scrollLeft).toBe(scrollAfterCancellation);
    expect(carousel.animationFrame).toBeNull();
  });

  test('a boundary wheel gesture recovers autoplay even when no scroll event follows', () => {
    installAnimationFrameQueue();
    const { carousel, viewport } = createCarousel();
    const animateToIndex = jest.spyOn(carousel, 'animateToIndex').mockImplementation(() => {});

    viewport.dispatchEvent(new WheelEvent('wheel'));
    jest.advanceTimersByTime(2000);

    expect(animateToIndex).toHaveBeenCalledTimes(1);
  });

  test('touch contact and native scrolling defer autoplay until scrolling settles', () => {
    installAnimationFrameQueue();
    const { carousel, viewport } = createCarousel();
    const animateToIndex = jest.spyOn(carousel, 'animateToIndex').mockImplementation(() => {});

    viewport.dispatchEvent(new PointerEvent('pointerdown', { pointerId: 12, pointerType: 'touch' }));
    jest.advanceTimersByTime(1000);
    expect(animateToIndex).not.toHaveBeenCalled();

    viewport.dispatchEvent(new PointerEvent('pointerup', { pointerId: 12, pointerType: 'touch' }));
    jest.advanceTimersByTime(500);
    viewport.dispatchEvent(new Event('scroll'));
    jest.advanceTimersByTime(1000);
    expect(animateToIndex).not.toHaveBeenCalled();

    viewport.dispatchEvent(new Event('scrollend'));
    jest.advanceTimersByTime(999);
    expect(animateToIndex).not.toHaveBeenCalled();

    jest.advanceTimersByTime(1);
    expect(animateToIndex).toHaveBeenCalledTimes(1);
  });

  test('visibility changes cancel pending work and resume with exactly one fresh interval', () => {
    installAnimationFrameQueue();
    const { carousel } = createCarousel();
    const animateToIndex = jest.spyOn(carousel, 'animateToIndex').mockImplementation(() => {});

    Object.defineProperty(document, 'hidden', { configurable: true, value: true });
    document.dispatchEvent(new Event('visibilitychange'));
    jest.advanceTimersByTime(3000);
    expect(animateToIndex).not.toHaveBeenCalled();

    Object.defineProperty(document, 'hidden', { configurable: true, value: false });
    document.dispatchEvent(new Event('visibilitychange'));
    document.dispatchEvent(new Event('visibilitychange'));
    expect(jest.getTimerCount()).toBe(1);
    jest.advanceTimersByTime(1000);
    expect(animateToIndex).toHaveBeenCalledTimes(1);
  });

  test('hiding the document cancels an autoplay animation already in flight', () => {
    const animationFrames = installAnimationFrameQueue();
    const { carousel } = createCarousel();

    jest.advanceTimersByTime(1000);
    const activeFrame = carousel.animationFrame;
    expect(activeFrame).not.toBeNull();

    Object.defineProperty(document, 'hidden', { configurable: true, value: true });
    document.dispatchEvent(new Event('visibilitychange'));

    expect(carousel.animationFrame).toBeNull();
    expect(animationFrames.cancel).toHaveBeenCalledWith(activeFrame);
  });

  test('reduced motion permanently pauses autoplay until the visible control restarts it', () => {
    installAnimationFrameQueue();
    const { carousel, control } = createCarousel();
    const animateToIndex = jest.spyOn(carousel, 'animateToIndex').mockImplementation(() => {});

    dispatchReducedMotionChange(true);
    dispatchReducedMotionChange(false);
    jest.advanceTimersByTime(2000);
    expect(animateToIndex).not.toHaveBeenCalled();
    expect(control.textContent).toBe('Start rotation');

    control.click();
    jest.advanceTimersByTime(1000);
    expect(animateToIndex).toHaveBeenCalledTimes(1);
  });

  test('enabling reduced motion cancels an autoplay animation already in flight', () => {
    const animationFrames = installAnimationFrameQueue();
    const { carousel } = createCarousel();

    jest.advanceTimersByTime(1000);
    const activeFrame = carousel.animationFrame;
    expect(activeFrame).not.toBeNull();

    dispatchReducedMotionChange(true);

    expect(carousel.animationFrame).toBeNull();
    expect(animationFrames.cancel).toHaveBeenCalledWith(activeFrame);
  });

  test('repeated updates replace the pending interval instead of accumulating timers', () => {
    installAnimationFrameQueue();
    const { carousel } = createCarousel();
    const animateToIndex = jest.spyOn(carousel, 'animateToIndex').mockImplementation(() => {});

    carousel.update();
    carousel.update();
    carousel.update();
    expect(jest.getTimerCount()).toBe(1);

    jest.advanceTimersByTime(1000);
    expect(animateToIndex).toHaveBeenCalledTimes(1);
  });

  test('disconnect cancels timers and every queued animation frame without late movement', () => {
    const animationFrames = installAnimationFrameQueue();
    const { carousel, viewport } = createCarousel();
    const initialScroll = viewport.scrollLeft;
    viewport.dispatchEvent(new Event('scroll'));
    carousel.scheduleRender();

    carousel.remove();
    expect(carousel.autoPlayTimer).toBeNull();
    expect(carousel.scrollEndTimer).toBeNull();
    expect(carousel.scrollFrame).toBeNull();
    expect(carousel.restoreInteractionFrame).toBeNull();
    expect(carousel.resizeObserver).toBeNull();
    expect(carousel.mutationObserver).toBeNull();

    jest.advanceTimersByTime(3000);
    animationFrames.flush(performance.now() + 3000);
    expect(viewport.scrollLeft).toBe(initialScroll);
  });

  test('disconnect cancels an autoplay animation already in flight', () => {
    const animationFrames = installAnimationFrameQueue();
    const { carousel, viewport } = createCarousel();

    jest.advanceTimersByTime(1000);
    const activeFrame = carousel.animationFrame;
    const scrollBeforeDisconnect = viewport.scrollLeft;
    expect(activeFrame).not.toBeNull();

    carousel.remove();
    expect(carousel.animationFrame).toBeNull();
    expect(animationFrames.cancel).toHaveBeenCalledWith(activeFrame);
    animationFrames.flush(performance.now() + 1000);
    expect(viewport.scrollLeft).toBe(scrollBeforeDisconnect);
  });

  test('hydration replacement preserves a drag pause and resumes autoplay only after release settlement', () => {
    const animationFrames = installAnimationFrameQueue();
    const original = createCarousel({ count: 14 });
    const originalAnimate = jest.spyOn(original.carousel, 'animateToIndex').mockImplementation(() => {});
    original.viewport.dispatchEvent(new PointerEvent('pointerdown', { button: 0, clientX: 180, pointerId: 17, pointerType: 'mouse' }));
    original.viewport.dispatchEvent(new PointerEvent('pointermove', { clientX: 80, pointerId: 17, pointerType: 'mouse' }));
    original.carousel.remove();

    const replacement = createCarousel({ count: 14 });
    replacement.carousel.restoreTransferredInteraction();
    const replacementAnimate = jest.spyOn(replacement.carousel, 'animateToIndex');
    jest.advanceTimersByTime(1500);
    expect(originalAnimate).not.toHaveBeenCalled();
    expect(replacementAnimate).not.toHaveBeenCalled();

    replacement.viewport.dispatchEvent(new PointerEvent('pointerup', { clientX: 80, pointerId: 17, pointerType: 'mouse' }));
    expect(replacementAnimate).toHaveBeenCalledTimes(1);
    animationFrames.flush(performance.now() + 1000);
    expect(replacement.carousel.autoPlayTimer).not.toBeNull();
  });
});
