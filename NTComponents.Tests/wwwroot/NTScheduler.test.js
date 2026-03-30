/**
 * @jest-environment jsdom
 */
import { jest } from '@jest/globals';
import { onDispose, onLoad } from '../../NTComponents/Scheduler/NTScheduler.razor.js';

function createPointerEvent(type, { button = 0, clientX = 0, clientY = 0, pointerId = 1 } = {}) {
    const event = new MouseEvent(type, {
        bubbles: true,
        button,
        cancelable: true,
        clientX,
        clientY
    });

    Object.defineProperty(event, 'pointerId', {
        configurable: true,
        value: pointerId
    });

    Object.defineProperty(event, 'isPrimary', {
        configurable: true,
        value: true
    });

    return event;
}

describe('NTScheduler drag interop', () => {
    beforeEach(() => {
        document.body.innerHTML = '';
        Object.defineProperty(document, 'elementsFromPoint', {
            configurable: true,
            value: jest.fn(() => [])
        });
    });

    test('pointer drag invokes dotnet drop callback for slot target', async () => {
        const root = document.createElement('div');
        root.dataset.ntSchedulerCanDrag = 'true';

        const eventButton = document.createElement('button');
        eventButton.setAttribute('data-nt-scheduler-event-id', '42');
        eventButton.getBoundingClientRect = jest.fn(() => ({
            bottom: 138,
            height: 38,
            left: 20,
            right: 180,
            top: 100,
            width: 160
        }));

        const dayColumn = document.createElement('div');
        dayColumn.className = 'nt-scheduler-week-view__day-column';
        dayColumn.getBoundingClientRect = jest.fn(() => ({
            bottom: 600,
            height: 500,
            left: 200,
            right: 360,
            top: 100,
            width: 160
        }));

        const slotButton = document.createElement('button');
        slotButton.setAttribute('data-nt-scheduler-slot-start', '2026-03-21T10:00:00.0000000-06:00');
        slotButton.getBoundingClientRect = jest.fn(() => ({
            bottom: 240,
            height: 32,
            left: 200,
            right: 360,
            top: 208,
            width: 160
        }));

        root.appendChild(eventButton);
        root.appendChild(dayColumn);
        dayColumn.appendChild(slotButton);
        document.body.appendChild(root);

        document.elementsFromPoint.mockReturnValue([slotButton]);

        const dotNetRef = {
            invokeMethodAsync: jest.fn().mockResolvedValue(undefined)
        };

        onLoad(root, dotNetRef);

        eventButton.dispatchEvent(createPointerEvent('pointerdown', { clientX: 30, clientY: 110 }));
        window.dispatchEvent(createPointerEvent('pointermove', { clientX: 224, clientY: 212 }));
        const dragGhost = document.querySelector('.nt-scheduler__drag-ghost');
        const dropPreview = document.querySelector('.nt-scheduler__drop-preview');

        expect(dragGhost).not.toBeNull();
        expect(dropPreview).not.toBeNull();
        expect(dropPreview.hidden).toBe(false);

        window.dispatchEvent(createPointerEvent('pointerup', { clientX: 224, clientY: 212 }));
        await Promise.resolve();

        expect(dotNetRef.invokeMethodAsync).toHaveBeenCalledWith(
            'HandleJsDropAsync',
            '42',
            '2026-03-21T10:00:00.0000000-06:00'
        );
        expect(root.classList.contains('nt-scheduler--dragging')).toBe(false);

        onDispose(root);
    });

    test('pointer click without movement does not invoke drop callback', async () => {
        const root = document.createElement('div');
        root.dataset.ntSchedulerCanDrag = 'true';

        const eventButton = document.createElement('button');
        eventButton.setAttribute('data-nt-scheduler-event-id', '42');
        eventButton.getBoundingClientRect = jest.fn(() => ({
            bottom: 138,
            height: 38,
            left: 20,
            right: 180,
            top: 100,
            width: 160
        }));

        root.appendChild(eventButton);
        document.body.appendChild(root);

        const dotNetRef = {
            invokeMethodAsync: jest.fn().mockResolvedValue(undefined)
        };

        onLoad(root, dotNetRef);

        eventButton.dispatchEvent(createPointerEvent('pointerdown', { clientX: 10, clientY: 10 }));
        window.dispatchEvent(createPointerEvent('pointerup', { clientX: 10, clientY: 10 }));
        await Promise.resolve();

        expect(dotNetRef.invokeMethodAsync).not.toHaveBeenCalled();
        expect(root.classList.contains('nt-scheduler--dragging')).toBe(false);
        expect(document.querySelector('.nt-scheduler__drag-ghost')).toBeNull();

        onDispose(root);
    });
});
