import { NTCarouselItem } from '../NTCarouselItem.razor.js';

// Mock Image so onload triggers immediately with a width
class ImageMock {
  constructor(){
    setTimeout(() => { 
      if(this.onload){ 
        this.naturalWidth = 320; 
        this.onload(); 
      } 
    }, 0);
  }
  set src(v){ this._src = v; }
  get src(){ return this._src; }
}

global.Image = ImageMock;

describe('NTCarouselItem', () => {
  beforeAll(() => {
    if(!customElements.get('tnt-carousel-item')) {
      customElements.define('tnt-carousel-item', NTCarouselItem);
    }
  });

  beforeEach(() => {
    document.body.innerHTML = '';
  });

  function buildDom({ hero = false, bgUrl = null, itemWidth = 300, viewportWidth = 500 } = {}) {
    const viewport = document.createElement('div');
    viewport.className = 'tnt-carousel-viewport';
    Object.defineProperty(viewport, 'getBoundingClientRect', { 
      value: () => ({ left: 0, right: viewportWidth, width: viewportWidth }), 
      configurable: true 
    });

    const item = document.createElement('tnt-carousel-item');
    item.setAttribute('tntid', `item-${Date.now()}-${Math.random()}`);
    if(hero) item.classList.add('tnt-carousel-hero');

    const content = document.createElement('div');
    content.className = 'tnt-carousel-item-content';
    if(bgUrl){ content.style.backgroundImage = `url('${bgUrl}')`; }
    
    Object.defineProperty(item, 'getBoundingClientRect', { 
      value: () => ({ left: 0, right: itemWidth, width: itemWidth }), 
      configurable: true 
    });

    item.appendChild(content);
    viewport.appendChild(item);
    document.body.appendChild(viewport);
    return { item, content, viewport };
  }

  test('upgrades to custom element', () => {
    const el = document.createElement('tnt-carousel-item');
    expect(el instanceof NTCarouselItem).toBe(true);
  });

  test('hero item sets backgroundImageWidth 100% and computes width via update', () => {
    const { item } = buildDom({ hero: true });
    item.onUpdate();
    expect(item.backgroundImageWidth).toBe('100%');
    // Hero items still call updateItemWidth which uses getBoundingClientRect result
    expect(item.style.width).toBe('300px'); // Final width after updateItemWidth
  });

  test('non-hero with no background uses computed width from getBoundingClientRect', () => {
    const { item } = buildDom();
    item.onUpdate();
    // The actual implementation calls updateItemWidth which uses getBoundingClientRect
    expect(item.style.width).toBe('300px'); // Uses the mocked width
  });

  test('background image triggers natural width calculation', async () => {
    const { item } = buildDom({ bgUrl: 'test.png' });
    item.onUpdate();
    // Wait for mock image to load
    await new Promise(r => setTimeout(r, 10));
    expect(item.naturalWidth).toBe(320);
    expect(item.style.getPropertyValue('--tnt-carousel-item-bg-width')).toBe('320px');
    // After image loads, width should be set to natural width, but updateItemWidth may override
    expect(item.style.width).toBe('300px'); // Final width after updateItemWidth
  });

  test('background image with quotes is parsed correctly', async () => {
    const { item, content } = buildDom();
    content.style.backgroundImage = `url("test-quoted.png")`;
    item.onUpdate();
    await new Promise(r => setTimeout(r, 10));
    expect(item.naturalWidth).toBe(320);
  });

  test('background image with single quotes is parsed correctly', async () => {
    const { item, content } = buildDom();
    content.style.backgroundImage = `url('test-single-quoted.png')`;
    item.onUpdate();
    await new Promise(r => setTimeout(r, 10));
    expect(item.naturalWidth).toBe(320);
  });

  test('updateItemWidth handles item wider than viewport', () => {
    const { item, content } = buildDom({ itemWidth: 600, viewportWidth: 500 });
    item.onUpdate();
    
    // Item should be constrained to viewport width
    expect(item.style.width).toBe('500px');
    expect(content.style.width).toBe('100%');
  });

  test('updateItemWidth handles item extending beyond viewport right edge', () => {
    const { item, content, viewport } = buildDom({ itemWidth: 300, viewportWidth: 500 });
    
    // Mock item positioned near right edge
    Object.defineProperty(item, 'getBoundingClientRect', { 
      value: () => ({ left: 350, right: 650, width: 300 }), 
      configurable: true 
    });
    
    item.onUpdate();
    
    // Content should be clipped to fit within viewport
    expect(content.style.width).toBe('150px'); // 500 - 350 = 150
    expect(item.style.justifyContent).toBe(''); // Empty string, not null
  });

  test('updateItemWidth handles item extending beyond viewport left edge', () => {
    const { item, content } = buildDom({ itemWidth: 300, viewportWidth: 500 });
    
    // Mock item positioned before left edge
    Object.defineProperty(item, 'getBoundingClientRect', { 
      value: () => ({ left: -100, right: 200, width: 300 }), 
      configurable: true 
    });
    
    item.onUpdate();
    
    // Content should be clipped and justified to end
    expect(content.style.width).toBe('200px'); // right edge = 200
    expect(item.style.justifyContent).toBe('end');
  });

  test('updateItemWidth handles item completely outside viewport (right)', () => {
    const { item, content } = buildDom({ itemWidth: 300, viewportWidth: 500 });
    
    // Mock item completely to the right of viewport
    Object.defineProperty(item, 'getBoundingClientRect', { 
      value: () => ({ left: 600, right: 900, width: 300 }), 
      configurable: true 
    });
    
    item.onUpdate();
    
    expect(content.style.width).toBe('0px');
  });

  test('updateItemWidth handles item completely outside viewport (left)', () => {
    const { item, content } = buildDom({ itemWidth: 300, viewportWidth: 500 });
    
    // Mock item completely to the left of viewport
    Object.defineProperty(item, 'getBoundingClientRect', { 
      value: () => ({ left: -400, right: -100, width: 300 }), 
      configurable: true 
    });
    
    item.onUpdate();
    
    expect(content.style.width).toBe('0px');
  });

  test('caches background image processing to avoid recomputation', async () => {
    const { item } = buildDom({ bgUrl: 'cached-test.png' });
    
    // First call
    item.onUpdate();
    await new Promise(r => setTimeout(r, 10));
    const firstNaturalWidth = item.naturalWidth;
    
    // Second call with same background should use cache
    item.onUpdate();
    await new Promise(r => setTimeout(r, 10));
    expect(item.naturalWidth).toBe(firstNaturalWidth);
  });

  test('handles missing content container gracefully', () => {
    const viewport = document.createElement('div');
    viewport.className = 'tnt-carousel-viewport';
    const item = document.createElement('tnt-carousel-item');
    // No content div added
    viewport.appendChild(item);
    document.body.appendChild(viewport);
    
    expect(() => item.onUpdate()).not.toThrow();
  });

  test('handles missing carousel container gracefully', () => {
    const item = document.createElement('tnt-carousel-item');
    const content = document.createElement('div');
    content.className = 'tnt-carousel-item-content';
    item.appendChild(content);
    document.body.appendChild(item); // Not in a carousel viewport
    
    expect(() => item.onUpdate()).not.toThrow();
  });

  test('disconnectedCallback removes from identifier map', () => {
    const { item } = buildDom();
    const identifier = item.getAttribute('tntid');
    
    // Simulate being registered
    item.attributeChangedCallback('tntid', null, identifier);
    
    // Disconnect should clean up
    item.disconnectedCallback();
    // We can't directly test the private map, but ensure no errors occur
    expect(() => item.disconnectedCallback()).not.toThrow();
  });

  test('attributeChangedCallback updates identifier mapping', () => {
    const { item } = buildDom();
    const oldId = 'old-id';
    const newId = 'new-id';
    
    // Should handle mapping changes without errors
    expect(() => {
      item.attributeChangedCallback('tntid', oldId, newId);
    }).not.toThrow();
  });

  test('handles malformed background image URLs gracefully', () => {
    const { item, content } = buildDom();
    content.style.backgroundImage = 'url(malformed-url';
    
    expect(() => item.onUpdate()).not.toThrow();
    // With malformed URL, it falls back to updateItemWidth behavior
    expect(item.style.width).toBe('300px'); // Uses getBoundingClientRect width
  });

  test('handles background image load error gracefully', async () => {
    // Mock Image that fails to load
    const OriginalImage = global.Image;
    global.Image = class {
      constructor() {
        setTimeout(() => {
          if (this.onerror) this.onerror();
        }, 0);
      }
      set src(v) { this._src = v; }
      get src() { return this._src; }
    };

    const { item } = buildDom({ bgUrl: 'broken-image.png' });
    item.onUpdate();
    
    await new Promise(r => setTimeout(r, 10));
    
    // Should handle error gracefully and not crash
    expect(() => item.updateItemWidth()).not.toThrow();
    
    // Restore original Image
    global.Image = OriginalImage;
  });

  test('lazy resolution works when containers not initially available', () => {
    const item = document.createElement('tnt-carousel-item');
    item.setAttribute('tntid', 'test-item');
    
    // Call updateItemWidth without containers
    expect(() => item.updateItemWidth()).not.toThrow();
    
    // Add containers after
    const content = document.createElement('div');
    content.className = 'tnt-carousel-item-content';
    item.appendChild(content);
    
    const viewport = document.createElement('div');
    viewport.className = 'tnt-carousel-viewport';
    viewport.appendChild(item);
    document.body.appendChild(viewport);
    
    // Mock dimensions
    Object.defineProperty(item, 'getBoundingClientRect', { 
      value: () => ({ left: 0, right: 300, width: 300 }), 
      configurable: true 
    });
    Object.defineProperty(viewport, 'getBoundingClientRect', { 
      value: () => ({ left: 0, right: 500, width: 500 }), 
      configurable: true 
    });
    
    // Should work now
    expect(() => item.updateItemWidth()).not.toThrow();
    expect(item.contentContainer).toBe(content);
    expect(item.carouselContainer).toBe(viewport);
  });
});
