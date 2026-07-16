import { jest } from '@jest/globals';
import { onDispose, onLoad } from '../../NTComponents/Scheduler/NTScheduler.razor.js';

class TestPointerEvent extends MouseEvent {
   constructor(type, options = {}) {
      super(type, options);
      Object.defineProperty(this, 'pointerId', { value: options.pointerId ?? 1 });
      Object.defineProperty(this, 'pointerType', { value: options.pointerType ?? 'mouse' });
      Object.defineProperty(this, 'isPrimary', { value: options.isPrimary ?? true });
   }
}

function setRect(element, rect) {
   element.getBoundingClientRect = () => ({
      bottom: rect.top + rect.height,
      height: rect.height,
      left: rect.left,
      right: rect.left + rect.width,
      top: rect.top,
      width: rect.width,
      x: rect.left,
      y: rect.top,
      toJSON: () => rect
   });
}

describe('NTScheduler pointer drag', () => {
   beforeEach(() => {
      global.PointerEvent = TestPointerEvent;
      HTMLElement.prototype.setPointerCapture = jest.fn();
      document.body.innerHTML = `
         <section data-nt-scheduler="true" data-nt-scheduler-drag-enabled="true" data-nt-scheduler-snap-minutes="15">
            <div class="timed-day" data-nt-scheduler-drop-date="2024-06-12" data-nt-scheduler-drop-time="true">
               <button class="time-slot" data-nt-scheduler-drop-slot="0"></button>
               <button class="time-slot" data-nt-scheduler-drop-slot="60"></button>
               <button class="time-slot" data-nt-scheduler-drop-slot="720"></button>
               <article class="event event-timed event-draggable" style="--event-start-minute:0;--event-end-minute:60;--event-lane:1;--event-lane-count:1;" data-nt-scheduler-event-id="42"><span class="resize-handle resize-handle-start" data-nt-scheduler-resize-edge="start"></span><span class="event-title">Design review</span><span class="resize-handle resize-handle-end" data-nt-scheduler-resize-edge="end"></span></article>
               <article class="event event-timed" style="--event-start-minute:60;--event-end-minute:120;--event-lane:1;--event-lane-count:1;" data-nt-scheduler-event-id="99">Customer call</article>
            </div>
         </section>`;
   });

   afterEach(() => {
      onDispose(document.querySelector('[data-nt-scheduler="true"]'));
      jest.restoreAllMocks();
      document.body.innerHTML = '';
   });

   test('creates a preview and drops with snapped minutes using pointer events', () => {
      const scheduler = document.querySelector('[data-nt-scheduler="true"]');
      const eventElement = document.querySelector('[data-nt-scheduler-event-id="42"]');
      const existingEvent = document.querySelector('[data-nt-scheduler-event-id="99"]');
      const dropTarget = document.querySelector('[data-nt-scheduler-drop-date]');
      const activeSlot = document.querySelector('[data-nt-scheduler-drop-slot="60"]');
      const dotNetRef = { invokeMethodAsync: jest.fn().mockResolvedValue(undefined) };
      setRect(eventElement, { left: 20, top: 30, width: 140, height: 48 });
      setRect(dropTarget, { left: 0, top: 100, width: 200, height: 1440 });
      document.elementFromPoint = jest.fn(() => activeSlot);

      onLoad(scheduler, dotNetRef);
      eventElement.dispatchEvent(new PointerEvent('pointerdown', { bubbles: true, button: 0, clientX: 40, clientY: 50 }));
      eventElement.dispatchEvent(new PointerEvent('pointermove', { bubbles: true, button: 0, clientX: 80, clientY: 190 }));

      const preview = document.body.querySelector('.drag-preview');
      const dropIndicator = dropTarget.querySelector('.drop-indicator');
      expect(preview).not.toBeNull();
      expect(preview.parentElement).toBe(document.body);
      expect(preview.style.pointerEvents).toBe('none');
      expect(preview.querySelector('.event-title').style.pointerEvents).toBe('none');
      expect(preview.style.getPropertyValue('--event-start-minute')).toBe('60');
      expect(preview.style.getPropertyValue('--nt-scheduler-preview-x')).toBe('60px');
      expect(preview.style.getPropertyValue('--nt-scheduler-preview-y')).toBe('170px');
      expect(dropIndicator).not.toBeNull();
      expect(dropIndicator.classList.contains('drop-target-active')).toBe(true);
      expect(dropIndicator.style.getPropertyValue('--drop-start-minute')).toBe('60');
      expect(dropIndicator.style.getPropertyValue('--drop-duration-minutes')).toBe('60');
      expect(eventElement.classList.contains('event-dragging')).toBe(true);
      expect(activeSlot.classList.contains('drop-target-active')).toBe(false);
      expect(dropTarget.classList.contains('drop-target-active')).toBe(false);
      expect(existingEvent.style.getPropertyValue('--event-lane-count')).toBe('2');

      eventElement.dispatchEvent(new PointerEvent('pointerup', { bubbles: true, button: 0, clientX: 80, clientY: 190 }));

      expect(dotNetRef.invokeMethodAsync.mock.calls).toEqual([
         ['NotifyEventDroppedAsync', '42', '2024-06-12', 60]
      ]);
      expect(document.body.querySelector('.drag-preview')).toBeNull();
      expect(dropTarget.querySelector('.drop-indicator')).toBeNull();
      expect(eventElement.classList.contains('event-dragging')).toBe(false);
      expect(existingEvent.style.getPropertyValue('--event-lane-count')).toBe('1');
   });

   test('drops using the dragged event start instead of the pointer position', () => {
      const scheduler = document.querySelector('[data-nt-scheduler="true"]');
      const eventElement = document.querySelector('[data-nt-scheduler-event-id="42"]');
      const dropTarget = document.querySelector('[data-nt-scheduler-drop-date]');
      const noonSlot = document.querySelector('[data-nt-scheduler-drop-slot="720"]');
      const dotNetRef = { invokeMethodAsync: jest.fn().mockResolvedValue(undefined) };
      setRect(eventElement, { left: 20, top: 30, width: 140, height: 60 });
      setRect(dropTarget, { left: 0, top: 100, width: 200, height: 1440 });
      document.elementFromPoint = jest.fn(() => noonSlot);

      onLoad(scheduler, dotNetRef);
      eventElement.dispatchEvent(new PointerEvent('pointerdown', { bubbles: true, button: 0, clientX: 40, clientY: 85 }));
      eventElement.dispatchEvent(new PointerEvent('pointermove', { bubbles: true, button: 0, clientX: 80, clientY: 879 }));
      eventElement.dispatchEvent(new PointerEvent('pointerup', { bubbles: true, button: 0, clientX: 80, clientY: 879 }));

      expect(dotNetRef.invokeMethodAsync.mock.calls).toEqual([
         ['NotifyEventDroppedAsync', '42', '2024-06-12', 720]
      ]);
   });

   test('does not start pointer dragging when scheduler dragging is disabled', () => {
      const scheduler = document.querySelector('[data-nt-scheduler="true"]');
      const eventElement = document.querySelector('[data-nt-scheduler-event-id="42"]');
      scheduler.dataset.ntSchedulerDragEnabled = 'false';
      setRect(eventElement, { left: 20, top: 30, width: 140, height: 48 });
      onLoad(scheduler, { invokeMethodAsync: jest.fn().mockResolvedValue(undefined) });

      eventElement.dispatchEvent(new PointerEvent('pointerdown', { bubbles: true, button: 0, clientX: 40, clientY: 50 }));
      eventElement.dispatchEvent(new PointerEvent('pointermove', { bubbles: true, button: 0, clientX: 80, clientY: 190 }));

      expect(document.body.querySelector('.drag-preview')).toBeNull();
      expect(eventElement.classList.contains('event-dragging')).toBe(false);
   });

   test('resizes the bottom edge in day view with snapped minutes', () => {
      const scheduler = document.querySelector('[data-nt-scheduler="true"]');
      const eventElement = document.querySelector('[data-nt-scheduler-event-id="42"]');
      const resizeHandle = eventElement.querySelector('[data-nt-scheduler-resize-edge="end"]');
      const dropTarget = document.querySelector('[data-nt-scheduler-drop-date]');
      const dotNetRef = { invokeMethodAsync: jest.fn().mockResolvedValue(undefined) };
      scheduler.classList.add('nt-scheduler-view-day');
      setRect(eventElement, { left: 20, top: 100, width: 140, height: 60 });
      setRect(dropTarget, { left: 0, top: 100, width: 200, height: 1440 });
      document.elementFromPoint = jest.fn(() => resizeHandle);

      onLoad(scheduler, dotNetRef);
      resizeHandle.dispatchEvent(new PointerEvent('pointerdown', { bubbles: true, button: 0, clientX: 40, clientY: 158 }));
      resizeHandle.dispatchEvent(new PointerEvent('pointermove', { bubbles: true, button: 0, clientX: 40, clientY: 250 }));

      const dropIndicator = dropTarget.querySelector('.drop-indicator');
      expect(document.body.querySelector('.drag-preview')).toBeNull();
      expect(eventElement.classList.contains('event-resizing')).toBe(true);
      expect(eventElement.style.getPropertyValue('--event-end-minute')).toBe('150');
      expect(dropIndicator.style.getPropertyValue('--drop-start-minute')).toBe('0');
      expect(dropIndicator.style.getPropertyValue('--drop-duration-minutes')).toBe('150');

      resizeHandle.dispatchEvent(new PointerEvent('pointerup', { bubbles: true, button: 0, clientX: 40, clientY: 250 }));

      expect(dotNetRef.invokeMethodAsync.mock.calls).toEqual([
         ['NotifyEventResizedAsync', '42', null, 150]
      ]);
      expect(dropTarget.querySelector('.drop-indicator')).toBeNull();
      expect(eventElement.classList.contains('event-resizing')).toBe(false);
   });

   test('resizes from an explicit handle in week view', () => {
      const scheduler = document.querySelector('[data-nt-scheduler="true"]');
      const eventElement = document.querySelector('[data-nt-scheduler-event-id="42"]');
      const resizeHandle = eventElement.querySelector('[data-nt-scheduler-resize-edge="end"]');
      const dropTarget = document.querySelector('[data-nt-scheduler-drop-date]');
      const dotNetRef = { invokeMethodAsync: jest.fn().mockResolvedValue(undefined) };
      setRect(eventElement, { left: 20, top: 100, width: 140, height: 60 });
      setRect(dropTarget, { left: 0, top: 100, width: 200, height: 1440 });
      document.elementFromPoint = jest.fn(() => resizeHandle);

      onLoad(scheduler, dotNetRef);
      resizeHandle.dispatchEvent(new PointerEvent('pointerdown', { bubbles: true, button: 0, clientX: 40, clientY: 158 }));
      resizeHandle.dispatchEvent(new PointerEvent('pointermove', { bubbles: true, button: 0, clientX: 40, clientY: 250 }));
      resizeHandle.dispatchEvent(new PointerEvent('pointerup', { bubbles: true, button: 0, clientX: 40, clientY: 250 }));

      expect(dotNetRef.invokeMethodAsync.mock.calls).toEqual([
         ['NotifyEventResizedAsync', '42', null, 150]
      ]);
   });

   test('drags across an empty timed day to select an event range', () => {
      const scheduler = document.querySelector('[data-nt-scheduler="true"]');
      const dropTarget = document.querySelector('[data-nt-scheduler-drop-date]');
      const startSlot = document.querySelector('[data-nt-scheduler-drop-slot="720"]');
      const dotNetRef = { invokeMethodAsync: jest.fn().mockResolvedValue(undefined) };
      scheduler.dataset.ntSchedulerRangeSelectEnabled = 'true';
      setRect(dropTarget, { left: 0, top: 100, width: 200, height: 1440 });
      document.elementFromPoint = jest.fn(() => startSlot);

      onLoad(scheduler, dotNetRef);
      startSlot.dispatchEvent(new PointerEvent('pointerdown', { bubbles: true, button: 0, clientX: 40, clientY: 820 }));
      startSlot.dispatchEvent(new PointerEvent('pointermove', { bubbles: true, button: 0, clientX: 40, clientY: 910 }));

      const selection = dropTarget.querySelector('.time-range-selection');
      expect(dropTarget.setPointerCapture).toHaveBeenCalledWith(1);
      expect(selection).not.toBeNull();
      expect(selection.style.getPropertyValue('--selection-start-minute')).toBe('720');
      expect(selection.style.getPropertyValue('--selection-end-minute')).toBe('810');

      startSlot.dispatchEvent(new PointerEvent('pointerup', { bubbles: true, button: 0, clientX: 40, clientY: 910 }));

      expect(dotNetRef.invokeMethodAsync.mock.calls).toEqual([
         ['NotifySlotSelectedAsync', '2024-06-12', 720, 810]
      ]);
      expect(dropTarget.querySelector('.time-range-selection')).toBeNull();
   });

   test('leaves an empty slot click targeted at the slot until range dragging starts', () => {
      const scheduler = document.querySelector('[data-nt-scheduler="true"]');
      const dropTarget = document.querySelector('[data-nt-scheduler-drop-date]');
      const startSlot = document.querySelector('[data-nt-scheduler-drop-slot="720"]');
      const dotNetRef = { invokeMethodAsync: jest.fn().mockResolvedValue(undefined) };
      scheduler.dataset.ntSchedulerRangeSelectEnabled = 'true';
      setRect(dropTarget, { left: 0, top: 100, width: 200, height: 1440 });

      onLoad(scheduler, dotNetRef);
      startSlot.dispatchEvent(new PointerEvent('pointerdown', { bubbles: true, button: 0, clientX: 40, clientY: 820 }));

      expect(dropTarget.setPointerCapture).not.toHaveBeenCalled();

      startSlot.dispatchEvent(new PointerEvent('pointerup', { bubbles: true, button: 0, clientX: 40, clientY: 820 }));
      expect(dotNetRef.invokeMethodAsync).not.toHaveBeenCalled();
   });

   test('resizes the top edge in day view without going below fifteen minutes', () => {
      const scheduler = document.querySelector('[data-nt-scheduler="true"]');
      const eventElement = document.querySelector('[data-nt-scheduler-event-id="42"]');
      const resizeHandle = eventElement.querySelector('[data-nt-scheduler-resize-edge="start"]');
      const dropTarget = document.querySelector('[data-nt-scheduler-drop-date]');
      const dotNetRef = { invokeMethodAsync: jest.fn().mockResolvedValue(undefined) };
      scheduler.classList.add('nt-scheduler-view-day');
      eventElement.style.setProperty('--event-start-minute', '60');
      eventElement.style.setProperty('--event-end-minute', '120');
      setRect(eventElement, { left: 20, top: 160, width: 140, height: 60 });
      setRect(dropTarget, { left: 0, top: 100, width: 200, height: 1440 });
      document.elementFromPoint = jest.fn(() => resizeHandle);

      onLoad(scheduler, dotNetRef);
      resizeHandle.dispatchEvent(new PointerEvent('pointerdown', { bubbles: true, button: 0, clientX: 40, clientY: 162 }));
      resizeHandle.dispatchEvent(new PointerEvent('pointermove', { bubbles: true, button: 0, clientX: 40, clientY: 216 }));

      const dropIndicator = dropTarget.querySelector('.drop-indicator');
      expect(eventElement.style.getPropertyValue('--event-start-minute')).toBe('105');
      expect(dropIndicator.style.getPropertyValue('--drop-start-minute')).toBe('105');
      expect(dropIndicator.style.getPropertyValue('--drop-duration-minutes')).toBe('15');

      resizeHandle.dispatchEvent(new PointerEvent('pointerup', { bubbles: true, button: 0, clientX: 40, clientY: 216 }));

      expect(dotNetRef.invokeMethodAsync.mock.calls).toEqual([
         ['NotifyEventResizedAsync', '42', 105, null]
      ]);
   });
});
