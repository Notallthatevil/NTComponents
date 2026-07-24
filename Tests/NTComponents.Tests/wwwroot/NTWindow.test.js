import { jest } from '@jest/globals';
import { onDispose, onLoad } from '../../../NTComponents/Window/NTWindow.razor.js';

describe('NTWindow module', () => {
   const initialInnerHeight = window.innerHeight;
   const initialInnerWidth = window.innerWidth;

   beforeEach(() => {
      jest.useFakeTimers();
      document.body.innerHTML = '';
   });

   afterEach(() => {
      expect(jest.getTimerCount()).toBe(0);
      jest.runOnlyPendingTimers();
      jest.useRealTimers();
      Object.defineProperty(window, 'innerHeight', { configurable: true, value: initialInnerHeight });
      Object.defineProperty(window, 'innerWidth', { configurable: true, value: initialInnerWidth });
      document.body.innerHTML = '';
   });

   function createWindow({ initialize = true } = {}) {
      document.body.innerHTML = `
         <section id="report-window"
                  class="nt-window"
                  role="dialog"
                  data-nt-window="true"
                  data-nt-window-dock-position="bottom-right"
                  data-nt-window-draggable="true"
                  data-nt-window-state="normal">
            <header class="nt-window-header" data-nt-window-drag-handle="true">
               <h2 class="nt-window-title">Report</h2>
               <div class="nt-window-controls">
                  <button data-nt-window-action="minimize" aria-label="Minimize Report" aria-expanded="true">
                     <span class="tnt-icon">minimize</span>
                  </button>
                  <button data-nt-window-action="fullscreen" aria-label="Show Report fullscreen" aria-pressed="false">
                     <span class="tnt-icon">fullscreen</span>
                  </button>
                  <button data-nt-window-action="close" aria-label="Close Report">
                     <span class="tnt-icon">close</span>
                  </button>
               </div>
            </header>
            <div class="nt-window-content-frame" aria-hidden="false">
               <div class="nt-window-content">Quarterly results</div>
            </div>
         </section>
         <tnt-page-script></tnt-page-script>`;

      const element = document.querySelector('[data-nt-window]');
      const pageScript = document.querySelector('tnt-page-script');
      if (initialize) {
         onLoad(pageScript);
      }
      return { element, pageScript };
   }

   test('static markup supports minimize, fullscreen, and close without Blazor', () => {
      const { element } = createWindow();

      element.querySelector('[data-nt-window-action="minimize"]').click();

      expect(element.dataset.ntWindowState).toBe('minimized');
      expect(element.classList.contains('nt-window-minimized')).toBe(true);
      expect(element.querySelector('.nt-window-content-frame').getAttribute('aria-hidden')).toBe('true');
      expect(element.querySelector('[data-nt-window-action="minimize"]').getAttribute('aria-label')).toBe('Restore Report');

      element.querySelector('[data-nt-window-action="fullscreen"]').click();

      expect(element.dataset.ntWindowState).toBe('fullscreen');
      expect(element.classList.contains('nt-window-fullscreen')).toBe(true);
      expect(element.classList.contains('nt-window-minimized')).toBe(false);
      expect(element.querySelector('[data-nt-window-action="fullscreen"]').getAttribute('aria-label')).toBe('Restore Report');

      element.querySelector('[data-nt-window-action="close"]').click();

      expect(element.classList.contains('nt-window-closing')).toBe(true);
      jest.advanceTimersByTime(200);
      expect(element.hidden).toBe(true);
   });

   test('browser bridge reopens a statically closed window', () => {
      const { element } = createWindow();
      element.hidden = true;

      expect(window.NTWindow.openWindow('report-window')).toBe(true);
      expect(element.hidden).toBe(false);
   });

   test('reopening a closing window cancels the pending close', () => {
      const { element } = createWindow();
      const closeHandler = jest.fn();
      element.addEventListener('ntwindowclose', closeHandler);

      expect(window.NTWindow.closeWindow(element)).toBe(true);
      expect(element.classList.contains('nt-window-closing')).toBe(true);

      expect(window.NTWindow.openWindow(element)).toBe(true);
      jest.advanceTimersByTime(200);

      expect(element.hidden).toBe(false);
      expect(element.classList.contains('nt-window-closing')).toBe(false);
      expect(closeHandler).not.toHaveBeenCalled();
   });

   test('closing an already closing window is idempotent', () => {
      const { element } = createWindow();
      const closeHandler = jest.fn();
      element.addEventListener('ntwindowclose', closeHandler);

      expect(window.NTWindow.closeWindow(element)).toBe(true);
      expect(window.NTWindow.closeWindow(element)).toBe(false);
      jest.advanceTimersByTime(200);

      expect(element.hidden).toBe(true);
      expect(closeHandler).toHaveBeenCalledTimes(1);
   });

   test('data attribute trigger reopens a window from static markup', () => {
      const { element } = createWindow();
      const trigger = document.createElement('button');
      trigger.dataset.ntWindowOpen = 'report-window';
      document.body.appendChild(trigger);
      element.hidden = true;

      trigger.click();

      expect(element.hidden).toBe(false);
   });

   test('dragging the header moves and clamps the floating window', () => {
      const { element } = createWindow();
      Object.defineProperty(window, 'innerWidth', { configurable: true, value: 800 });
      Object.defineProperty(window, 'innerHeight', { configurable: true, value: 600 });
      element.getBoundingClientRect = jest.fn(() => ({
         bottom: 320,
         height: 200,
         left: 100,
         right: 400,
         toJSON: () => ({}),
         top: 120,
         width: 300,
         x: 100,
         y: 120
      }));

      element.querySelector('[data-nt-window-drag-handle]').dispatchEvent(new MouseEvent('pointerdown', {
         bubbles: true,
         button: 0,
         clientX: 150,
         clientY: 150
      }));
      window.dispatchEvent(new MouseEvent('pointermove', {
         clientX: 900,
         clientY: 900
      }));

      expect(element.style.left).toBe('500px');
      expect(element.style.top).toBe('400px');
      expect(element.style.translate).toBe('none');
      expect(element.dataset.ntWindowDragging).toBe('true');

      window.dispatchEvent(new MouseEvent('pointerup'));
      expect(element.dataset.ntWindowDragging).toBeUndefined();
   });

   test('disposing a removed page-script marker cleans up an active drag', () => {
      const { element, pageScript } = createWindow();
      Object.defineProperty(window, 'innerWidth', { configurable: true, value: 800 });
      Object.defineProperty(window, 'innerHeight', { configurable: true, value: 600 });
      element.getBoundingClientRect = jest.fn(() => ({
         bottom: 320,
         height: 200,
         left: 100,
         right: 400,
         toJSON: () => ({}),
         top: 120,
         width: 300,
         x: 100,
         y: 120
      }));

      element.querySelector('[data-nt-window-drag-handle]').dispatchEvent(new MouseEvent('pointerdown', {
         bubbles: true,
         button: 0,
         clientX: 150,
         clientY: 150
      }));
      const leftAtDisposal = element.style.left;
      const topAtDisposal = element.style.top;

      pageScript.remove();
      onDispose(pageScript);
      const draggingAfterDisposal = element.dataset.ntWindowDragging;
      window.dispatchEvent(new MouseEvent('pointermove', {
         clientX: 700,
         clientY: 500
      }));
      const leftAfterMove = element.style.left;
      const topAfterMove = element.style.top;
      window.dispatchEvent(new MouseEvent('pointerup'));

      expect(draggingAfterDisposal).toBeUndefined();
      expect(leftAfterMove).toBe(leftAtDisposal);
      expect(topAfterMove).toBe(topAtDisposal);
   });

   test('grabbing a window raises it above every existing window', () => {
      const { element } = createWindow();
      const second = element.cloneNode(true);
      second.id = 'second-window';
      document.body.insertBefore(second, document.querySelector('tnt-page-script'));
      element.style.zIndex = '9000';
      onLoad(document.body);

      second.querySelector('[data-nt-window-drag-handle]').dispatchEvent(new MouseEvent('pointerdown', {
         bubbles: true,
         button: 0,
         clientX: 150,
         clientY: 150
      }));

      expect(Number(second.style.zIndex)).toBeGreaterThan(Number(element.style.zIndex));
      window.dispatchEvent(new MouseEvent('pointerup'));
   });

   test('the most recently grabbed window stays above many competing windows', () => {
      const { element, pageScript } = createWindow();
      const windows = [element];

      for (let index = 1; index < 48; index++) {
         const candidate = element.cloneNode(true);
         candidate.id = `stress-window-${index}`;
         candidate.style.zIndex = String(5000 + index * 17);
         document.body.insertBefore(candidate, pageScript);
         windows.push(candidate);
      }
      onLoad(document.body);

      const grabOrder = [...windows].reverse().concat(windows.filter((_, index) => index % 2 === 0));
      for (const candidate of grabOrder) {
         candidate.querySelector('[data-nt-window-drag-handle]').dispatchEvent(new MouseEvent('pointerdown', {
            bubbles: true,
            button: 0,
            clientX: 150,
            clientY: 150
         }));
         window.dispatchEvent(new MouseEvent('pointerup'));

         const competingZIndexes = windows
            .filter(windowElement => windowElement !== candidate)
            .map(windowElement => Number(window.getComputedStyle(windowElement).zIndex));
         expect(Number(window.getComputedStyle(candidate).zIndex)).toBeGreaterThan(Math.max(...competingZIndexes));
      }
   });

   test('minimizing docks the window and restoring returns it to its floating position', () => {
      const { element } = createWindow();
      element.style.inset = 'auto';
      element.style.left = '120px';
      element.style.top = '80px';
      element.style.translate = 'none';

      element.querySelector('[data-nt-window-action="minimize"]').click();

      expect(element.style.left).toBe('');
      expect(element.style.top).toBe('');
      expect(element.style.getPropertyValue('--nt-window-dock-offset')).toBe('0px');

      element.querySelector('[data-nt-window-action="minimize"]').click();

      expect(element.style.inset).toBe('auto');
      expect(element.style.left).toBe('120px');
      expect(element.style.top).toBe('80px');
      expect(element.style.translate).toBe('none');
   });

   test('minimized windows sharing a dock stack without overlapping', () => {
      const { element } = createWindow();
      const second = element.cloneNode(true);
      second.id = 'second-window';
      document.body.insertBefore(second, document.querySelector('tnt-page-script'));
      onLoad(document.body);

      element.querySelector('[data-nt-window-action="minimize"]').click();
      second.querySelector('[data-nt-window-action="minimize"]').click();

      expect(element.style.getPropertyValue('--nt-window-dock-offset')).toBe('0px');
      expect(second.style.getPropertyValue('--nt-window-dock-offset')).toBe('56px');
   });

   test('removing a minimized window compacts the remaining docked windows', () => {
      const { element, pageScript } = createWindow();
      const second = element.cloneNode(true);
      const secondPageScript = document.createElement('tnt-page-script');
      second.id = 'second-window';
      document.body.append(second, secondPageScript);
      onLoad(secondPageScript);

      element.querySelector('[data-nt-window-action="minimize"]').click();
      second.querySelector('[data-nt-window-action="minimize"]').click();
      expect(second.style.getPropertyValue('--nt-window-dock-offset')).toBe('56px');

      element.remove();
      pageScript.remove();
      onDispose(pageScript);

      expect(second.style.getPropertyValue('--nt-window-dock-offset')).toBe('0px');
   });

   test('progressive enhancement preserves custom state control labels', () => {
      const { element, pageScript } = createWindow({ initialize: false });
      const minimizeButton = element.querySelector('[data-nt-window-action="minimize"]');
      const fullscreenButton = element.querySelector('[data-nt-window-action="fullscreen"]');
      minimizeButton.setAttribute('aria-label', 'Collapse report workspace');
      fullscreenButton.setAttribute('aria-label', 'Expand report workspace');

      onLoad(pageScript);

      expect(minimizeButton.getAttribute('aria-label')).toBe('Collapse report workspace');
      expect(fullscreenButton.getAttribute('aria-label')).toBe('Expand report workspace');

      minimizeButton.click();
      fullscreenButton.click();

      expect(minimizeButton.getAttribute('aria-label')).toBe('Collapse report workspace');
      expect(fullscreenButton.getAttribute('aria-label')).toBe('Expand report workspace');
   });

   test('onDispose removes progressive enhancement handlers', () => {
      const { element, pageScript } = createWindow();

      onDispose(pageScript);
      element.querySelector('[data-nt-window-action="minimize"]').click();

      expect(element.dataset.ntWindowState).toBe('normal');
   });

});
