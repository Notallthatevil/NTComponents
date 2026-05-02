import { jest } from '@jest/globals';
import { onLoad, onUpdate } from '../../NTComponents/Menus/NTMenu.razor.js';

function rect(values) {
   return {
      bottom: values.bottom,
      height: values.height,
      left: values.left,
      right: values.right,
      top: values.top,
      width: values.width,
      x: values.left,
      y: values.top,
      toJSON: () => ({})
   };
}

function createMenu({ anchorRect, menuRect }) {
   onLoad(null);

   const anchor = document.createElement('button');
   anchor.id = 'menu-anchor';
   anchor.getBoundingClientRect = jest.fn(() => rect(anchorRect));

   const menu = document.createElement('nt-menu');
   menu.className = 'nt-menu nt-menu-placement-auto nt-menu-placement-bottom nt-menu-anchor-auto nt-menu-anchor-end';
   menu.dataset.anchorSelector = '#menu-anchor';
   menu.dataset.closeOnItemClick = 'true';
   menu.getBoundingClientRect = jest.fn(() => rect(menuRect));
   menu.matches = jest.fn((selector) => selector === ':popover-open' ? false : HTMLElement.prototype.matches.call(menu, selector));
   const surface = document.createElement('div');
   surface.className = 'nt-menu-surface';
   const content = document.createElement('div');
   content.className = 'nt-menu-content';
   surface.append(content);
   menu.append(surface);

   document.body.append(anchor, menu);
   onLoad(menu);

   return { anchor, content, menu, surface };
}

describe('NTMenu custom element', () => {
   beforeEach(() => {
      document.body.innerHTML = '';
      delete window.NTComponents;
      jest.clearAllMocks();
   });

   test('auto placement opens above when there is not enough room below', () => {
      const { content, menu } = createMenu({
         anchorRect: { bottom: 390, height: 40, left: 450, right: 490, top: 350, width: 40 },
         menuRect: { bottom: 0, height: 120, left: 0, right: 0, top: 0, width: 160 }
      });
      Object.defineProperty(window, 'innerWidth', { configurable: true, value: 500 });
      Object.defineProperty(window, 'innerHeight', { configurable: true, value: 400 });

      menu.updatePlacement();

      expect(menu.classList.contains('nt-menu-placement-top')).toBe(true);
      expect(menu.classList.contains('nt-menu-placement-bottom')).toBe(false);
      expect(menu.classList.contains('nt-menu-anchor-end')).toBe(true);
   });

   test('auto placement aligns start edge near the left viewport edge', () => {
      const { content, menu } = createMenu({
         anchorRect: { bottom: 90, height: 40, left: 10, right: 50, top: 50, width: 40 },
         menuRect: { bottom: 0, height: 120, left: 0, right: 0, top: 0, width: 160 }
      });
      Object.defineProperty(window, 'innerWidth', { configurable: true, value: 500 });
      Object.defineProperty(window, 'innerHeight', { configurable: true, value: 400 });

      menu.updatePlacement();

      expect(menu.classList.contains('nt-menu-anchor-start')).toBe(true);
      expect(menu.classList.contains('nt-menu-anchor-end')).toBe(false);
      expect(menu.classList.contains('nt-menu-placement-bottom')).toBe(true);
   });

   test('anchor activation updates placement before native popover opens', () => {
      const { anchor, menu } = createMenu({
         anchorRect: { bottom: 390, height: 40, left: 450, right: 490, top: 350, width: 40 },
         menuRect: { bottom: 0, height: 120, left: 0, right: 0, top: 0, width: 160 }
      });
      Object.defineProperty(window, 'innerWidth', { configurable: true, value: 500 });
      Object.defineProperty(window, 'innerHeight', { configurable: true, value: 400 });

      anchor.dispatchEvent(new Event('pointerdown'));

      expect(menu.classList.contains('nt-menu-placement-top')).toBe(true);
   });

   test('beforetoggle updates placement before native popover opens', () => {
      const { menu } = createMenu({
         anchorRect: { bottom: 390, height: 40, left: 450, right: 490, top: 350, width: 40 },
         menuRect: { bottom: 0, height: 120, left: 0, right: 0, top: 0, width: 160 }
      });
      Object.defineProperty(window, 'innerWidth', { configurable: true, value: 500 });
      Object.defineProperty(window, 'innerHeight', { configurable: true, value: 400 });
      const event = new Event('beforetoggle');
      Object.defineProperty(event, 'newState', { value: 'open' });

      menu.dispatchEvent(event);

      expect(menu.classList.contains('nt-menu-placement-top')).toBe(true);
   });

   test('submenu opens to the left when the right edge cannot fit it', () => {
      const { menu } = createMenu({
         anchorRect: { bottom: 190, height: 48, left: 430, right: 490, top: 142, width: 60 },
         menuRect: { bottom: 0, height: 160, left: 0, right: 0, top: 0, width: 140 }
      });
      menu.dataset.submenu = 'true';
      Object.defineProperty(window, 'innerWidth', { configurable: true, value: 500 });
      Object.defineProperty(window, 'innerHeight', { configurable: true, value: 400 });

      menu.updatePlacement();

      expect(menu.classList.contains('nt-menu-placement-side-left')).toBe(true);
      expect(menu.classList.contains('nt-menu-placement-side-right')).toBe(false);
      expect(menu.style.left).toBe('286px');
      expect(menu.style.top).toBe('142px');
   });

   test('menu item click closes the owning menu but not submenu triggers', () => {
      const { content, menu } = createMenu({
         anchorRect: { bottom: 90, height: 40, left: 10, right: 50, top: 50, width: 40 },
         menuRect: { bottom: 0, height: 120, left: 0, right: 0, top: 0, width: 160 }
      });
      const item = document.createElement('button');
      item.className = 'nt-menu-item';
      const submenuTrigger = document.createElement('button');
      submenuTrigger.className = 'nt-menu-item';
      submenuTrigger.setAttribute('aria-haspopup', 'menu');
      menu.hidePopover = jest.fn();
      content.append(item, submenuTrigger);

      submenuTrigger.click();
      expect(menu.hidePopover).not.toHaveBeenCalled();

      item.click();
      expect(menu.hidePopover).toHaveBeenCalledTimes(1);
   });

   test('registers menu items for ripple interactions', () => {
      const registerButtonInteraction = jest.fn();
      window.NTComponents = { registerButtonInteraction };
      const { content, menu } = createMenu({
         anchorRect: { bottom: 90, height: 40, left: 10, right: 50, top: 50, width: 40 },
         menuRect: { bottom: 0, height: 120, left: 0, right: 0, top: 0, width: 160 }
      });
      const item = document.createElement('button');
      item.className = 'nt-menu-item';
      const label = document.createElement('div');
      label.className = 'nt-menu-label';
      const divider = document.createElement('hr');
      divider.className = 'nt-menu-divider';
      content.append(item, label, divider);

      onUpdate(menu);
      onUpdate(menu);

      expect(registerButtonInteraction).toHaveBeenCalledWith(item);
      expect(registerButtonInteraction).toHaveBeenCalledTimes(1);
   });

   test('open toggle marks anchor pressed and focuses first item', async () => {
      const { anchor, content, menu } = createMenu({
         anchorRect: { bottom: 90, height: 40, left: 10, right: 50, top: 50, width: 40 },
         menuRect: { bottom: 0, height: 120, left: 0, right: 0, top: 0, width: 160 }
      });
      const item = document.createElement('button');
      item.className = 'nt-menu-item';
      content.append(item);
      menu.matches = jest.fn((selector) => selector === ':popover-open' ? true : HTMLElement.prototype.matches.call(menu, selector));
      const previousRequestAnimationFrame = window.requestAnimationFrame;
      window.requestAnimationFrame = (callback) => {
         callback();
         return 0;
      };

      menu.dispatchEvent(new Event('toggle'));
      await new Promise(resolve => setTimeout(resolve, 0));
      window.requestAnimationFrame = previousRequestAnimationFrame;

      expect(anchor.classList.contains('nt-menu-anchor-pressed')).toBe(true);
      expect(anchor.classList.contains('nt-button--pressed-shape')).toBe(true);
      expect(anchor.getAttribute('aria-expanded')).toBe('true');
      expect(document.activeElement).toBe(item);
   });

   test('arrow keys move focus between menu items', () => {
      const { content, menu } = createMenu({
         anchorRect: { bottom: 90, height: 40, left: 10, right: 50, top: 50, width: 40 },
         menuRect: { bottom: 0, height: 120, left: 0, right: 0, top: 0, width: 160 }
      });
      const first = document.createElement('button');
      first.className = 'nt-menu-item';
      const second = document.createElement('button');
      second.className = 'nt-menu-item';
      content.append(first, second);
      menu.matches = jest.fn((selector) => selector === ':popover-open' ? true : HTMLElement.prototype.matches.call(menu, selector));
      first.focus();

      menu.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowDown', bubbles: true }));

      expect(document.activeElement).toBe(second);
   });
});
