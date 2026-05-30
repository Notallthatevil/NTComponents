import { jest } from '@jest/globals';
import { onLoad } from '../../NTComponents/Menus/NTContextMenu.razor.js';

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

function pointerEvent(type, values) {
   const event = new Event(type, { bubbles: true, cancelable: true });
   Object.defineProperty(event, 'clientX', { value: values.clientX });
   Object.defineProperty(event, 'clientY', { value: values.clientY });
   Object.defineProperty(event, 'isPrimary', { value: values.isPrimary ?? true });
   Object.defineProperty(event, 'pointerType', { value: values.pointerType ?? 'touch' });
   return event;
}

function createContextMenu({ disabled = false, longPressDelay = 500 } = {}) {
   onLoad(null);

   const contextMenu = document.createElement('nt-context-menu');
   contextMenu.dataset.disabled = disabled ? 'true' : 'false';
   contextMenu.dataset.longPressDelay = String(longPressDelay);
   contextMenu.dataset.menuId = 'menu';

   const target = document.createElement('span');
   target.className = 'nt-context-menu-target';
   target.tabIndex = 0;
   target.getBoundingClientRect = jest.fn(() => rect({
      bottom: 130,
      height: 30,
      left: 40,
      right: 200,
      top: 100,
      width: 160
   }));

   const menu = document.createElement('nt-menu');
   menu.id = 'menu';
   menu.openAt = jest.fn();

   contextMenu.append(target, menu);
   document.body.append(contextMenu);
   onLoad(contextMenu);

   return { contextMenu, menu, target };
}

function createNestedContextMenu() {
   onLoad(null);

   const parent = document.createElement('nt-context-menu');
   parent.dataset.disabled = 'false';
   parent.dataset.longPressDelay = '250';
   parent.dataset.menuId = 'parent-menu';
   const parentTarget = document.createElement('span');
   parentTarget.className = 'nt-context-menu-target';
   const parentMenu = document.createElement('nt-menu');
   parentMenu.id = 'parent-menu';
   parentMenu.openAt = jest.fn();

   const child = document.createElement('nt-context-menu');
   child.dataset.disabled = 'false';
   child.dataset.longPressDelay = '250';
   child.dataset.menuId = 'child-menu';
   const childTarget = document.createElement('span');
   childTarget.className = 'nt-context-menu-target';
   childTarget.tabIndex = 0;
   childTarget.getBoundingClientRect = jest.fn(() => rect({
      bottom: 84,
      height: 24,
      left: 20,
      right: 120,
      top: 60,
      width: 100
   }));
   const childMenu = document.createElement('nt-menu');
   childMenu.id = 'child-menu';
   childMenu.openAt = jest.fn();

   child.append(childTarget, childMenu);
   parentTarget.append(child);
   parent.append(parentTarget, parentMenu);
   document.body.append(parent);
   onLoad(parent);
   onLoad(child);

   return { childMenu, childTarget, parentMenu, parentTarget };
}

describe('NTContextMenu custom element', () => {
   beforeEach(() => {
      document.body.innerHTML = '';
      jest.clearAllMocks();
      jest.useRealTimers();
   });

   test('right-click prevents the native menu and opens at pointer coordinates', () => {
      const { menu, target } = createContextMenu();
      const event = new MouseEvent('contextmenu', {
         bubbles: true,
         cancelable: true,
         clientX: 84,
         clientY: 126
      });

      target.dispatchEvent(event);

      expect(event.defaultPrevented).toBe(true);
      expect(menu.openAt).toHaveBeenCalledWith(84, 126, target);
   });

   test('keyboard context menu key opens at the target lower-left corner', () => {
      const { menu, target } = createContextMenu();

      target.dispatchEvent(new KeyboardEvent('keydown', {
         bubbles: true,
         cancelable: true,
         key: 'ContextMenu'
      }));

      expect(menu.openAt).toHaveBeenCalledWith(40, 130, target);
   });

   test('shift f10 opens at the focused target lower-left corner', () => {
      const { menu, target } = createContextMenu();

      target.dispatchEvent(new KeyboardEvent('keydown', {
         bubbles: true,
         cancelable: true,
         key: 'F10',
         shiftKey: true
      }));

      expect(menu.openAt).toHaveBeenCalledWith(40, 130, target);
   });

   test('touch long-press opens after the configured delay', () => {
      jest.useFakeTimers();
      const { menu, target } = createContextMenu({ longPressDelay: 250 });

      target.dispatchEvent(pointerEvent('pointerdown', { clientX: 64, clientY: 92 }));
      jest.advanceTimersByTime(249);
      expect(menu.openAt).not.toHaveBeenCalled();

      jest.advanceTimersByTime(1);

      expect(menu.openAt).toHaveBeenCalledWith(64, 92, target);
   });

   test('touch long-press suppresses follow-up native context menu and click', () => {
      jest.useFakeTimers();
      const { target } = createContextMenu({ longPressDelay: 250 });

      target.dispatchEvent(pointerEvent('pointerdown', { clientX: 64, clientY: 92 }));
      jest.advanceTimersByTime(250);

      const contextMenuEvent = new MouseEvent('contextmenu', { bubbles: true, cancelable: true });
      const clickEvent = new MouseEvent('click', { bubbles: true, cancelable: true });
      target.dispatchEvent(contextMenuEvent);
      target.dispatchEvent(clickEvent);

      expect(contextMenuEvent.defaultPrevented).toBe(true);
      expect(clickEvent.defaultPrevented).toBe(true);
   });

   test('touch long-press cancels on movement beyond tolerance', () => {
      jest.useFakeTimers();
      const { menu, target } = createContextMenu({ longPressDelay: 250 });

      target.dispatchEvent(pointerEvent('pointerdown', { clientX: 64, clientY: 92 }));
      document.dispatchEvent(pointerEvent('pointermove', { clientX: 73, clientY: 92 }));
      jest.advanceTimersByTime(250);

      expect(menu.openAt).not.toHaveBeenCalled();
   });

   test('touch long-press cancels on pointer up', () => {
      jest.useFakeTimers();
      const { menu, target } = createContextMenu({ longPressDelay: 250 });

      target.dispatchEvent(pointerEvent('pointerdown', { clientX: 64, clientY: 92 }));
      document.dispatchEvent(pointerEvent('pointerup', { clientX: 64, clientY: 92 }));
      jest.advanceTimersByTime(250);

      expect(menu.openAt).not.toHaveBeenCalled();
   });

   test('disabled context menu does not prevent native context menu or open', () => {
      const { menu, target } = createContextMenu({ disabled: true });
      const event = new MouseEvent('contextmenu', {
         bubbles: true,
         cancelable: true,
         clientX: 84,
         clientY: 126
      });

      target.dispatchEvent(event);

      expect(event.defaultPrevented).toBe(false);
      expect(menu.openAt).not.toHaveBeenCalled();
   });

   test('nested child context menu does not also open the parent on right-click', () => {
      const { childMenu, childTarget, parentMenu } = createNestedContextMenu();
      const event = new MouseEvent('contextmenu', {
         bubbles: true,
         cancelable: true,
         clientX: 42,
         clientY: 76
      });

      childTarget.dispatchEvent(event);

      expect(event.defaultPrevented).toBe(true);
      expect(childMenu.openAt).toHaveBeenCalledWith(42, 76, childTarget);
      expect(parentMenu.openAt).not.toHaveBeenCalled();
   });

   test('nested child context menu does not also open the parent from keyboard', () => {
      const { childMenu, childTarget, parentMenu } = createNestedContextMenu();

      childTarget.dispatchEvent(new KeyboardEvent('keydown', {
         bubbles: true,
         cancelable: true,
         key: 'ContextMenu'
      }));

      expect(childMenu.openAt).toHaveBeenCalledWith(20, 84, childTarget);
      expect(parentMenu.openAt).not.toHaveBeenCalled();
   });

   test('nested child long-press does not start the parent long-press menu', () => {
      jest.useFakeTimers();
      const { childMenu, childTarget, parentMenu } = createNestedContextMenu();

      childTarget.dispatchEvent(pointerEvent('pointerdown', { clientX: 42, clientY: 76 }));
      jest.advanceTimersByTime(250);

      expect(childMenu.openAt).toHaveBeenCalledWith(42, 76, childTarget);
      expect(parentMenu.openAt).not.toHaveBeenCalled();
   });
});
