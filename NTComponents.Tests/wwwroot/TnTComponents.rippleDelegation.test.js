import { jest } from '@jest/globals';
import { afterWebStarted } from '../../NTComponents/wwwroot/NTComponents.lib.module.js';

describe('NTComponents delegated ripple', () => {
   beforeEach(() => {
      document.body.innerHTML = '';
      jest.clearAllMocks();
      jest.useFakeTimers();
   });

   afterEach(() => {
      jest.runOnlyPendingTimers();
      jest.useRealTimers();
   });

   test('creates and releases ripple nodes for nt-button ripple hosts', () => {
      const blazor = { addEventListener: jest.fn() };

      try {
         afterWebStarted(blazor);
      } catch (error) {
         if (!String(error?.message ?? error).includes('already been registered')) {
            throw error;
         }
      }

      const button = document.createElement('button');
      button.className = 'nt-button tnt-ripple';

      Object.defineProperty(button, 'offsetWidth', { configurable: true, value: 100 });
      Object.defineProperty(button, 'offsetHeight', { configurable: true, value: 40 });
      button.getBoundingClientRect = jest.fn(() => ({
         top: 10,
         left: 20,
         right: 120,
         bottom: 50,
         width: 100,
         height: 40
      }));

      const host = document.createElement('span');
      host.className = 'nt-button-ripple-host';
      button.appendChild(host);
      document.body.appendChild(button);

      const PointerCtor = window.PointerEvent ?? MouseEvent;
      const pointerDown = new PointerCtor(window.PointerEvent ? 'pointerdown' : 'mousedown', {
         bubbles: true,
         clientX: 60,
         clientY: 30,
         pointerId: 1,
         pointerType: 'mouse'
      });

      button.dispatchEvent(pointerDown);
      jest.advanceTimersByTime(1);

      const ripple = host.querySelector('.nt-button-ripple');
      expect(ripple).not.toBeNull();
      expect(ripple.style.getPropertyValue('--nt-button-ripple-origin-x')).toBe('40px');
      expect(ripple.style.getPropertyValue('--nt-button-ripple-origin-y')).toBe('20px');
      expect(ripple.style.getPropertyValue('--nt-button-ripple-width')).toBe('200px');
      expect(ripple.style.getPropertyValue('--nt-button-ripple-height')).toBe('80px');

      const pointerUp = new PointerCtor(window.PointerEvent ? 'pointerup' : 'mouseup', {
         bubbles: true,
         clientX: 60,
         clientY: 30,
         pointerId: 1,
         pointerType: 'mouse'
      });

      button.dispatchEvent(pointerUp);
      jest.advanceTimersByTime(600);

      expect(host.querySelector('.nt-button-ripple')).toBeNull();
   });

   test('creates a centered ripple for keyboard activation on registered hosts', () => {
      const blazor = { addEventListener: jest.fn() };

      try {
         afterWebStarted(blazor);
      } catch (error) {
         if (!String(error?.message ?? error).includes('already been registered')) {
            throw error;
         }
      }

      const button = document.createElement('button');
      button.className = 'nt-button tnt-ripple';

      Object.defineProperty(button, 'offsetWidth', { configurable: true, value: 120 });
      Object.defineProperty(button, 'offsetHeight', { configurable: true, value: 48 });

      const host = document.createElement('span');
      host.className = 'nt-button-ripple-host';
      button.appendChild(host);
      document.body.appendChild(button);

      global.NTComponents.registerRippleHost(host);

      button.dispatchEvent(new KeyboardEvent('keydown', { bubbles: true, key: 'Enter' }));
      jest.advanceTimersByTime(1);

      const ripple = host.querySelector('.nt-button-ripple');
      expect(ripple).not.toBeNull();
      expect(ripple.style.getPropertyValue('--nt-button-ripple-origin-x')).toBe('60px');
      expect(ripple.style.getPropertyValue('--nt-button-ripple-origin-y')).toBe('24px');
      expect(ripple.style.getPropertyValue('--nt-button-ripple-width')).toBe('240px');
      expect(ripple.style.getPropertyValue('--nt-button-ripple-height')).toBe('96px');

      button.dispatchEvent(new KeyboardEvent('keyup', { bubbles: true, key: 'Enter' }));
      jest.advanceTimersByTime(600);

      expect(host.querySelector('.nt-button-ripple')).toBeNull();
   });

   test('registerRippleHost keeps pressed shape until mouseleave', () => {
      const blazor = { addEventListener: jest.fn() };

      try {
         afterWebStarted(blazor);
      } catch (error) {
         if (!String(error?.message ?? error).includes('already been registered')) {
            throw error;
         }
      }

      const button = document.createElement('button');
      button.className = 'nt-button';

      Object.defineProperty(button, 'offsetWidth', { configurable: true, value: 100 });
      Object.defineProperty(button, 'offsetHeight', { configurable: true, value: 40 });
      button.getBoundingClientRect = jest.fn(() => ({
         top: 10,
         left: 20,
         right: 120,
         bottom: 50,
         width: 100,
         height: 40
      }));

      const host = document.createElement('span');
      host.className = 'nt-button-ripple-host';
      button.appendChild(host);
      document.body.appendChild(button);

      global.NTComponents.registerRippleHost(host);

      const PointerCtor = window.PointerEvent ?? MouseEvent;
      button.dispatchEvent(new PointerCtor(window.PointerEvent ? 'pointerdown' : 'mousedown', {
         bubbles: true,
         clientX: 60,
         clientY: 30,
         pointerId: 1,
         pointerType: 'mouse'
      }));

      jest.advanceTimersByTime(1);

      const ripple = host.querySelector('.nt-button-ripple');
      expect(ripple).not.toBeNull();
      expect(button.classList.contains('nt-button--pressed-shape')).toBe(true);
      expect(ripple.style.getPropertyValue('--nt-button-ripple-origin-x')).toBe('40px');
      expect(ripple.style.getPropertyValue('--nt-button-ripple-origin-y')).toBe('20px');
      expect(ripple.style.getPropertyValue('--nt-button-ripple-width')).toBe('200px');
      expect(ripple.style.getPropertyValue('--nt-button-ripple-height')).toBe('80px');

      button.dispatchEvent(new PointerCtor(window.PointerEvent ? 'pointerup' : 'mouseup', {
         bubbles: true,
         clientX: 60,
         clientY: 30,
         pointerId: 1,
         pointerType: 'mouse'
      }));

      expect(button.classList.contains('nt-button--pressed-shape')).toBe(true);

      button.dispatchEvent(new PointerCtor(window.PointerEvent ? 'pointerleave' : 'mouseleave', {
         bubbles: true,
         clientX: 60,
         clientY: 30,
         pointerId: 1,
         pointerType: 'mouse'
      }));

      jest.advanceTimersByTime(600);

      expect(button.classList.contains('nt-button--pressed-shape')).toBe(false);
      expect(host.querySelector('.nt-button-ripple')).toBeNull();
   });

   test('startRippleHost registers the previous host element through the shared module function', () => {
      const blazor = { addEventListener: jest.fn() };

      try {
         afterWebStarted(blazor);
      } catch (error) {
         if (!String(error?.message ?? error).includes('already been registered')) {
            throw error;
         }
      }

      const wrapper = document.createElement('div');
      const host = document.createElement('span');
      host.className = 'nt-button-ripple-host';
      const script = document.createElement('script');
      wrapper.appendChild(host);
      wrapper.appendChild(script);
      document.body.appendChild(wrapper);

      const registerSpy = jest.spyOn(global.NTComponents, 'registerRippleHost');

      global.NTComponents.startRippleHost(script);

      expect(registerSpy).toHaveBeenCalledWith(host);
   });

   test('registerButtonInteraction keeps pressed shape without a ripple host', () => {
      const blazor = { addEventListener: jest.fn() };

      try {
         afterWebStarted(blazor);
      } catch (error) {
         if (!String(error?.message ?? error).includes('already been registered')) {
            throw error;
         }
      }

      const button = document.createElement('button');
      button.className = 'nt-button';
      document.body.appendChild(button);

      global.NTComponents.registerButtonInteraction(button);

      const PointerCtor = window.PointerEvent ?? MouseEvent;
      button.dispatchEvent(new PointerCtor(window.PointerEvent ? 'pointerdown' : 'mousedown', {
         bubbles: true,
         clientX: 60,
         clientY: 30,
         pointerId: 1,
         pointerType: 'mouse'
      }));

      expect(button.classList.contains('nt-button--pressed-shape')).toBe(true);

      button.dispatchEvent(new PointerCtor(window.PointerEvent ? 'pointerup' : 'mouseup', {
         bubbles: true,
         clientX: 60,
         clientY: 30,
         pointerId: 1,
         pointerType: 'mouse'
      }));

      expect(button.classList.contains('nt-button--pressed-shape')).toBe(true);

      button.dispatchEvent(new PointerCtor(window.PointerEvent ? 'pointerleave' : 'mouseleave', {
         bubbles: true,
         clientX: 60,
         clientY: 30,
         pointerId: 1,
         pointerType: 'mouse'
      }));

      expect(button.classList.contains('nt-button--pressed-shape')).toBe(false);
   });
});
