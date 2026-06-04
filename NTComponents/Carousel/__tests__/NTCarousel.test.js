import { NTCarousel } from '../NTCarousel.razor.js';

// Polyfills / mocks
class ResizeObserverMock {
  constructor(cb){ this.cb = cb; }
  observe(){ /* noop */ }
  disconnect(){ /* noop */ }
}

global.ResizeObserver = ResizeObserverMock;

describe('NTCarousel custom element', () => {
  beforeAll(() => {
    if(!customElements.get('tnt-carousel')) {
      customElements.define('tnt-carousel', NTCarousel);
    }
  });

  beforeEach(() => {
    document.body.innerHTML = '';
  });

  function createCarousel(count = 3, { hero = false, centered = false, snapping = true } = {}) {
    const carousel = document.createElement('tnt-carousel');
    carousel.setAttribute('tntid', `carousel-${Date.now()}-${Math.random()}`);
    
    if (hero) carousel.classList.add('tnt-carousel-hero');
    if (centered) carousel.classList.add('tnt-carousel-centered');
    if (snapping) carousel.classList.add('tnt-carousel-snapping');

    const viewport = document.createElement('div');
    viewport.className = 'tnt-carousel-viewport';
    Object.defineProperty(viewport, 'clientWidth', { value: 500, configurable: true });
    Object.defineProperty(viewport, 'scrollWidth', { value: 500 * count, configurable: true });
    Object.defineProperty(viewport, 'scrollLeft', { value: 0, writable: true, configurable: true });

    viewport.setPointerCapture = () => {}; // Mock function
    viewport.scrollTo = ({ left }) => { viewport.scrollLeft = left; }; // Mock function

    for (let i = 0; i < count; i++) {
      const item = document.createElement('tnt-carousel-item');
      Object.defineProperty(item, 'offsetLeft', { value: i * 500, configurable: true });
      Object.defineProperty(item, 'offsetWidth', { value: 500, configurable: true });
      viewport.appendChild(item);
    }

    const prevButton = document.createElement('button');
    prevButton.className = 'tnt-carousel-prev-button';
    const nextButton = document.createElement('button');
    nextButton.className = 'tnt-carousel-next-button';

    carousel.appendChild(prevButton);
    carousel.appendChild(nextButton);
    carousel.appendChild(viewport);

    document.body.appendChild(carousel);
    return { carousel, viewport, prevButton, nextButton };
  }

  test('registers and upgrades element', () => {
    const el = document.createElement('tnt-carousel');
    expect(el instanceof NTCarousel).toBe(true);
  });

  test('onUpdate wires viewport, items and sets gap css var', () => {
    const { carousel, viewport } = createCarousel();
    carousel.onUpdate();
    expect(carousel.carouselViewPort).toBe(viewport);
    expect(carousel.carouselItems.length).toBe(3);
    expect(viewport.style.getPropertyValue('--tnt-carousel-item-gap')).toBe('8px');
  });

  test('navigation buttons go next/prev', () => {
    const { carousel, prevButton, nextButton } = createCarousel();
    carousel.onUpdate();

    nextButton.click();
    expect(carousel._currentAutoIndex).toBe(1);
    
    prevButton.click();
    expect(carousel._currentAutoIndex).toBe(0);
  });

  test('auto play advances items', async () => {
    const { carousel } = createCarousel();
    carousel.setAttribute('tnt-auto-play-interval', '0.01');
    carousel.onUpdate();

    await new Promise(resolve => {
      setTimeout(() => {
        expect(carousel._currentAutoIndex).toBeGreaterThanOrEqual(0);
        carousel.disconnectedCallback();
        resolve();
      }, 50);
    });
  });

  test('dragging disabled when attribute false', () => {
    const { carousel } = createCarousel();
    carousel.setAttribute('tnt-allow-dragging', 'false');
    carousel.onUpdate();
    
    const ev = new PointerEvent('pointerdown', { clientX: 0, button: 0, pointerType: 'mouse' });
    carousel.carouselViewPort.dispatchEvent(ev);
    expect(carousel._isDragging).toBe(false);
  });

  test('dragging enabled when attribute true', () => {
    const { carousel } = createCarousel();
    carousel.setAttribute('tnt-allow-dragging', 'true');
    carousel.onUpdate();
    
    const ev = new PointerEvent('pointerdown', { clientX: 100, button: 0, pointerType: 'mouse' });
    carousel.carouselViewPort.dispatchEvent(ev);
    expect(carousel._isDragging).toBe(true);
  });

  test('dragging sets dragging flag and updates scroll', () => {
    const { carousel } = createCarousel();
    carousel.setAttribute('tnt-allow-dragging', 'true');
    carousel.onUpdate();

    const down = new PointerEvent('pointerdown', { clientX: 100, button: 0, pointerType: 'mouse' });
    carousel.carouselViewPort.dispatchEvent(down);
    expect(carousel._isDragging).toBe(true);

    const move = new PointerEvent('pointermove', { clientX: 50, pointerType: 'mouse' });
    carousel.carouselViewPort.dispatchEvent(move);
    expect(carousel.carouselViewPort.scrollLeft).toBeGreaterThan(0);

    const up = new PointerEvent('pointerup', { clientX: 50, pointerType: 'mouse' });
    carousel.carouselViewPort.dispatchEvent(up);
    expect(carousel._isDragging).toBe(false);
  });

  test('dragging stops on pointer leave', () => {
    const { carousel } = createCarousel();
    carousel.setAttribute('tnt-allow-dragging', 'true');
    carousel.onUpdate();

    const down = new PointerEvent('pointerdown', { clientX: 100, button: 0, pointerType: 'mouse' });
    carousel.carouselViewPort.dispatchEvent(down);
    expect(carousel._isDragging).toBe(true);

    const leave = new PointerEvent('pointerleave', { clientX: 200, pointerType: 'mouse' });
    carousel.carouselViewPort.dispatchEvent(leave);
    expect(carousel._isDragging).toBe(false);
  });

  test('auto play stops during dragging', () => {
    const { carousel } = createCarousel();
    carousel.setAttribute('tnt-auto-play-interval', '1');
    carousel.setAttribute('tnt-allow-dragging', 'true');
    carousel.onUpdate();
    
    const initialTimerId = carousel._autoPlayTimerId;
    expect(initialTimerId).toBeTruthy();

    const down = new PointerEvent('pointerdown', { clientX: 100, button: 0, pointerType: 'mouse' });
    carousel.carouselViewPort.dispatchEvent(down);
    
    expect(carousel._autoPlayTimerId).toBeNull();
  });

  test('centered mode scrolls to center items', () => {
    const { carousel, viewport } = createCarousel(3, { centered: true });
    carousel.onUpdate();
    
    const item = carousel.carouselItems[1]; // middle item
    // Create a spy to track scrollTo calls
    const scrollToCalls = [];
    viewport.scrollTo = (options) => {
      viewport.scrollLeft = options.left;
      scrollToCalls.push(options);
    };
    
    carousel._scrollToItem(item);
    
    const expectedLeft = item.offsetLeft - (carousel.carouselViewPort.clientWidth - item.offsetWidth) / 2;
    expect(scrollToCalls.length).toBe(1);
    expect(scrollToCalls[0]).toEqual({ left: expectedLeft, behavior: 'smooth' });
  });

  test('non-centered mode scrolls to item left edge', () => {
    const { carousel, viewport } = createCarousel(3, { centered: false });
    carousel.onUpdate();
    
    const item = carousel.carouselItems[1];
    // Create a spy to track scrollTo calls
    const scrollToCalls = [];
    viewport.scrollTo = (options) => {
      viewport.scrollLeft = options.left;
      scrollToCalls.push(options);
    };
    
    carousel._scrollToItem(item);
    
    expect(scrollToCalls.length).toBe(1);
    expect(scrollToCalls[0]).toEqual({ left: item.offsetLeft, behavior: 'smooth' });
  });

  test('handles empty carousel gracefully', () => {
    const { carousel } = createCarousel(0);
    carousel.onUpdate();
    
    expect(carousel.carouselItems.length).toBe(0);
    expect(() => carousel._advanceAutoPlay()).not.toThrow();
    expect(() => carousel._goToIndex(0)).not.toThrow();
  });

  test('wraps around on navigation', () => {
    const { carousel, nextButton } = createCarousel(3);
    carousel.onUpdate();
    
    // _getVisibleIndex returns 0 when scrollLeft is 0, so next goes to index 1
    expect(carousel._currentAutoIndex).toBe(-1); // default start
    
    nextButton.click();
    expect(carousel._currentAutoIndex).toBe(1); // _getVisibleIndex(true) returns 0, +1 = 1
    
    nextButton.click();
    expect(carousel._currentAutoIndex).toBe(2); // _getVisibleIndex(true) returns 1, +1 = 2
    
    // Now from index 2, next should wrap to 0
    nextButton.click();
    expect(carousel._currentAutoIndex).toBe(0); // wrapped around: (2 + 1) % 3 = 0
  });

  test('disconnectedCallback cleans up resources', () => {
    const { carousel } = createCarousel();
    carousel.setAttribute('tnt-auto-play-interval', '1');
    carousel.onUpdate();
    
    const initialTimerId = carousel._autoPlayTimerId;
    expect(initialTimerId).toBeTruthy();
    
    carousel.disconnectedCallback();
    
    expect(carousel._autoPlayTimerId).toBeNull();
    expect(carousel._scrollListener).toBeNull();
  });

  test('attribute changes trigger updates', () => {
    const { carousel } = createCarousel();
    carousel.onUpdate();
    
    // Test auto-play interval change (need to set attribute on element, not call directly)
    carousel.setAttribute('tnt-auto-play-interval', '2');
    carousel.attributeChangedCallback('tnt-auto-play-interval', null, '2');
    expect(carousel._autoPlayIntervalMs).toBe(2000);
    
    // Test dragging change
    carousel.attributeChangedCallback('tnt-allow-dragging', null, 'false');
    expect(carousel._allowDragging).toBe(false);
  });

  test('invalid auto-play intervals are ignored', () => {
    const { carousel } = createCarousel();
    carousel.onUpdate();
    
    carousel.attributeChangedCallback('tnt-auto-play-interval', null, 'invalid');
    expect(carousel._autoPlayIntervalMs).toBeNull();
    
    carousel.attributeChangedCallback('tnt-auto-play-interval', null, '-1');
    expect(carousel._autoPlayIntervalMs).toBeNull();
  });

  test('drag movement threshold prevents accidental drags', () => {
    const { carousel } = createCarousel();
    carousel.setAttribute('tnt-allow-dragging', 'true');
    carousel.onUpdate();

    const down = new PointerEvent('pointerdown', { clientX: 100, button: 0, pointerType: 'mouse' });
    carousel.carouselViewPort.dispatchEvent(down);
    
    // Small movement (below threshold)
    const smallMove = new PointerEvent('pointermove', { clientX: 105, pointerType: 'mouse' });
    carousel.carouselViewPort.dispatchEvent(smallMove);
    expect(carousel._dragMoved).toBe(false);
    
    // Large movement (above threshold)
    const largeMove = new PointerEvent('pointermove', { clientX: 120, pointerType: 'mouse' });
    carousel.carouselViewPort.dispatchEvent(largeMove);
    expect(carousel._dragMoved).toBe(true);
  });
});
