type Maybe<T> = T | null | undefined;

interface SchedulerElement extends HTMLElement {
    __ntSchedulerState?: SchedulerState;
}

interface SchedulerState {
    activeDrag: SchedulerDrag | null;
    activeSelection: SchedulerSelection | null;
    dotNetRef: DotNetSchedulerReference;
    onClick: (event: MouseEvent) => void;
    onKeyDown: (event: KeyboardEvent) => void;
    onPointerCancel: (event: PointerEvent) => void;
    onPointerDown: (event: PointerEvent) => void;
    onPointerMove: (event: PointerEvent) => void;
    onPointerUp: (event: PointerEvent) => void;
    suppressClickUntil: number;
}

interface SchedulerSelection {
    anchorMinutes: number;
    date: string;
    endMinutes: number;
    hasMoved: boolean;
    indicator: HTMLElement | null;
    pointerId: number;
    startMinutes: number;
    startX: number;
    startY: number;
    target: HTMLElement;
}

interface SchedulerDrag {
    affectedEventStyles: Map<HTMLElement, string | null>;
    currentDrop: SchedulerDrop | null;
    currentResize: SchedulerResize | null;
    currentTarget: HTMLElement | null;
    dropIndicator: HTMLElement | null;
    durationMinutes: number;
    eventElement: HTMLElement;
    eventId: string;
    hasMoved: boolean;
    isTimedEvent: boolean;
    offsetX: number;
    offsetY: number;
    pointerId: number;
    preview: HTMLElement | null;
    resizeEdge: SchedulerResizeEdge | null;
    startMinute: number;
    startX: number;
    startY: number;
}

interface SchedulerDrop {
    date: string | null;
    minutes: number | null;
    target: HTMLElement;
}

interface SchedulerResize {
    endMinutes: number;
    target: HTMLElement;
    startMinutes: number;
}

interface DotNetSchedulerReference {
    invokeMethodAsync(methodName: 'NotifyEventDroppedAsync', eventId: string | null, date: string | null, minutes: number | null): Promise<void>;
    invokeMethodAsync(methodName: 'NotifyEventResizedAsync', eventId: string | null, startMinutes: number | null, endMinutes: number | null): Promise<void>;
    invokeMethodAsync(methodName: 'NotifySlotSelectedAsync', date: string | null, startMinutes: number | null, endMinutes: number | null): Promise<void>;
}

type SchedulerResizeEdge = 'start' | 'end';

const schedulerSelector = '[data-nt-scheduler="true"]';
const eventSelector = '[data-nt-scheduler-event-id]';
const dropDateSelector = '[data-nt-scheduler-drop-date]';
const dragThresholdPixels = 4;
const minutesPerDay = 1440;

function getScheduler(element: Maybe<Element>): SchedulerElement | null {
    if (!element) {
        return null;
    }

    if (element instanceof HTMLElement && element.matches(schedulerSelector)) {
        return element as SchedulerElement;
    }

    return element.closest?.(schedulerSelector) as SchedulerElement | null;
}

function getSchedulers(scope: Maybe<Element | Document>): SchedulerElement[] {
    const root = scope ?? document;
    const schedulers: SchedulerElement[] = [];
    if (root instanceof HTMLElement && root.matches(schedulerSelector)) {
        schedulers.push(root as SchedulerElement);
    }

    schedulers.push(...Array.from(root.querySelectorAll<SchedulerElement>(schedulerSelector)));
    return schedulers;
}

function toInt(value: Maybe<string>, fallback: number): number {
    if (value == null || value.trim().length === 0) {
        return fallback;
    }

    const parsed = Number.parseInt(value, 10);
    return Number.isFinite(parsed) ? parsed : fallback;
}

function clamp(value: number, min: number, max: number): number {
    return Math.min(Math.max(value, min), max);
}

function isDraggingEnabled(scheduler: SchedulerElement): boolean {
    return scheduler.dataset.ntSchedulerDragEnabled !== 'false';
}

function isRangeSelectionEnabled(scheduler: SchedulerElement): boolean {
    return scheduler.dataset.ntSchedulerRangeSelectEnabled === 'true';
}

function getCssNumber(element: HTMLElement, propertyName: string): number | null {
    const value = getComputedStyle(element).getPropertyValue(propertyName).trim();
    if (value.length === 0) {
        return null;
    }

    const parsed = Number.parseFloat(value);
    return Number.isFinite(parsed) ? parsed : null;
}

function getTimedEventDuration(eventElement: HTMLElement, scheduler: SchedulerElement): number {
    const startMinute = getCssNumber(eventElement, '--event-start-minute');
    const endMinute = getCssNumber(eventElement, '--event-end-minute');
    if (startMinute !== null && endMinute !== null && endMinute > startMinute) {
        return endMinute - startMinute;
    }

    const rowHeight = Math.max(getCssNumber(scheduler, '--nt-scheduler-row-height') ?? 56, 1);
    const duration = (eventElement.getBoundingClientRect().height / rowHeight) * 60;
    return clamp(Math.round(duration), 1, minutesPerDay);
}

function getSnapMinutes(scheduler: SchedulerElement): number {
    return clamp(toInt(scheduler.dataset.ntSchedulerSnapMinutes, 15), 1, minutesPerDay);
}

function getSnappedMinutes(target: HTMLElement, clientY: number, scheduler: SchedulerElement, allowEndOfDay = false): number | null {
    if (target.dataset.ntSchedulerDropTime !== 'true') {
        return null;
    }

    const rect = target.getBoundingClientRect();
    if (rect.height <= 0) {
        return null;
    }

    const snapMinutes = getSnapMinutes(scheduler);
    const rawMinutes = ((clientY - rect.top) / rect.height) * minutesPerDay;
    return clamp(Math.floor(rawMinutes / snapMinutes) * snapMinutes, 0, allowEndOfDay ? minutesPerDay : minutesPerDay - snapMinutes);
}

function getDropMinutes(target: HTMLElement, clientY: number, scheduler: SchedulerElement): number | null {
    return getSnappedMinutes(target, clientY, scheduler);
}

function getDropInfo(scheduler: SchedulerElement, drag: SchedulerDrag, event: PointerEvent): SchedulerDrop | null {
    const element = document.elementFromPoint(event.clientX, event.clientY);
    const target = element instanceof Element ? element.closest<HTMLElement>(dropDateSelector) : null;
    if (!target || !scheduler.contains(target)) {
        return null;
    }

    const minutes = getDropMinutes(target, event.clientY - drag.offsetY, scheduler);
    return {
        date: target.dataset.ntSchedulerDropDate ?? null,
        minutes,
        target
    };
}

function copyComputedStyles(source: HTMLElement, target: HTMLElement): void {
    const computedStyle = getComputedStyle(source);
    for (let i = 0; i < computedStyle.length; i++) {
        const propertyName = computedStyle.item(i);
        target.style.setProperty(propertyName, computedStyle.getPropertyValue(propertyName), computedStyle.getPropertyPriority(propertyName));
    }

    const sourceChildren = Array.from(source.children);
    const targetChildren = Array.from(target.children);
    for (let i = 0; i < sourceChildren.length && i < targetChildren.length; i++) {
        const sourceChild = sourceChildren[i];
        const targetChild = targetChildren[i];
        if (sourceChild instanceof HTMLElement && targetChild instanceof HTMLElement) {
            copyComputedStyles(sourceChild, targetChild);
        }
    }
}

function disablePointerEvents(element: HTMLElement): void {
    element.style.pointerEvents = 'none';
    for (const child of Array.from(element.children)) {
        if (child instanceof HTMLElement) {
            disablePointerEvents(child);
        }
    }
}

function createPreview(drag: SchedulerDrag, event: PointerEvent): HTMLElement {
    const preview = drag.eventElement.cloneNode(true) as HTMLElement;
    const rect = drag.eventElement.getBoundingClientRect();
    copyComputedStyles(drag.eventElement, preview);
    preview.classList.add('drag-preview');
    preview.classList.remove('event-dragging');
    preview.removeAttribute('id');
    preview.setAttribute('aria-hidden', 'true');
    preview.style.position = 'fixed';
    preview.style.inset = 'auto';
    preview.style.top = '0';
    preview.style.right = 'auto';
    preview.style.bottom = 'auto';
    preview.style.left = '0';
    preview.style.margin = '0';
    preview.style.zIndex = '10000';
    preview.style.opacity = '.88';
    preview.style.inlineSize = `${rect.width}px`;
    preview.style.blockSize = `${rect.height}px`;
    disablePointerEvents(preview);
    document.body.append(preview);
    updatePointerPreview(drag, event);
    return preview;
}

function updatePointerPreview(drag: SchedulerDrag, event: PointerEvent): void {
    setPreviewPosition(drag.preview, event.clientX - drag.offsetX, event.clientY - drag.offsetY);
}

function setPreviewPosition(preview: HTMLElement | null, x: number, y: number): void {
    preview?.style.setProperty('--nt-scheduler-preview-x', `${x}px`);
    preview?.style.setProperty('--nt-scheduler-preview-y', `${y}px`);
    if (preview) {
        preview.style.transform = `translate3d(${x}px, ${y}px, 0)`;
    }
}

function setActiveTarget(drag: SchedulerDrag, target: HTMLElement | null): void {
    if (drag.currentTarget === target) {
        return;
    }

    drag.currentTarget?.classList.remove('drop-target-active');
    target?.classList.add('drop-target-active');
    drag.currentTarget = target;
}

function removeDropIndicator(drag: SchedulerDrag): void {
    drag.dropIndicator?.remove();
    drag.dropIndicator = null;
}

function updateDropIndicator(drag: SchedulerDrag, target: HTMLElement, startMinute: number, durationMinutes = drag.durationMinutes): HTMLElement {
    const indicator = drag.dropIndicator ?? document.createElement('div');
    if (!drag.dropIndicator) {
        indicator.className = 'drop-indicator';
        indicator.setAttribute('aria-hidden', 'true');
        indicator.style.pointerEvents = 'none';
        drag.dropIndicator = indicator;
    }

    if (indicator.parentElement !== target) {
        target.append(indicator);
    }

    const endMinute = clamp(startMinute + durationMinutes, startMinute + 1, minutesPerDay);
    indicator.style.setProperty('--drop-start-minute', `${startMinute}`);
    indicator.style.setProperty('--drop-duration-minutes', `${endMinute - startMinute}`);
    return indicator;
}

function restoreReactiveLayout(drag: SchedulerDrag): void {
    for (const [eventElement, style] of drag.affectedEventStyles) {
        if (style === null) {
            eventElement.removeAttribute('style');
        }
        else {
            eventElement.setAttribute('style', style);
        }
    }

    drag.affectedEventStyles.clear();
}

function rememberEventStyle(drag: SchedulerDrag, eventElement: HTMLElement): void {
    if (!drag.affectedEventStyles.has(eventElement)) {
        drag.affectedEventStyles.set(eventElement, eventElement.getAttribute('style'));
    }
}

interface TimedLayoutEvent {
    element: HTMLElement;
    endMinute: number;
    lane: number;
    laneCount: number;
    startMinute: number;
}

function eventsOverlap(first: TimedLayoutEvent, second: TimedLayoutEvent): boolean {
    return first.startMinute < second.endMinute && second.startMinute < first.endMinute;
}

function getLaneCount(event: TimedLayoutEvent, events: TimedLayoutEvent[]): number {
    const connectedEvents = new Set<TimedLayoutEvent>([event]);
    const pendingEvents: TimedLayoutEvent[] = [event];
    while (pendingEvents.length > 0) {
        const current = pendingEvents.shift()!;
        for (const candidate of events) {
            if (connectedEvents.has(candidate) || !eventsOverlap(current, candidate)) {
                continue;
            }

            connectedEvents.add(candidate);
            pendingEvents.push(candidate);
        }
    }

    return Math.max(...Array.from(connectedEvents, item => item.lane)) + 1;
}

function assignTimedLanes(events: TimedLayoutEvent[]): void {
    const lanes: TimedLayoutEvent[][] = [];
    const orderedEvents = [...events].sort((first, second) => first.startMinute - second.startMinute || (second.endMinute - second.startMinute) - (first.endMinute - first.startMinute));
    for (const event of orderedEvents) {
        let lane = lanes.findIndex(existingEvents => existingEvents.every(existing => !eventsOverlap(existing, event)));
        if (lane < 0) {
            lane = lanes.length;
            lanes.push([]);
        }

        event.lane = lane;
        lanes[lane].push(event);
    }

    for (const event of orderedEvents) {
        event.laneCount = getLaneCount(event, events);
    }
}

function applyEventLaneStyle(drag: SchedulerDrag, event: TimedLayoutEvent): void {
    if (event.element !== drag.preview) {
        rememberEventStyle(drag, event.element);
    }

    event.element.style.setProperty('--event-lane', `${event.lane + 1}`);
    event.element.style.setProperty('--event-lane-count', `${event.laneCount}`);
}

function applyTimedPreviewLayout(drag: SchedulerDrag, target: HTMLElement, startMinute: number, pointerX: number, pointerY: number): void {
    const preview = drag.preview;
    if (!preview) {
        return;
    }

    const endMinute = clamp(startMinute + drag.durationMinutes, startMinute + 1, minutesPerDay);
    const events: TimedLayoutEvent[] = Array.from(target.querySelectorAll<HTMLElement>('.event-timed'))
        .filter(candidate => candidate !== drag.eventElement && candidate.dataset.ntSchedulerEventId !== drag.eventId)
        .map(candidate => {
            const candidateStart = getCssNumber(candidate, '--event-start-minute') ?? 0;
            const candidateEnd = getCssNumber(candidate, '--event-end-minute') ?? candidateStart + 1;
            return { element: candidate, endMinute: candidateEnd, lane: 0, laneCount: 1, startMinute: candidateStart };
        });

    const previewEvent: TimedLayoutEvent = { element: preview, endMinute, lane: 0, laneCount: 1, startMinute };
    events.push(previewEvent);
    assignTimedLanes(events);

    for (const event of events) {
        applyEventLaneStyle(drag, event);
    }

    const targetRect = target.getBoundingClientRect();
    const laneWidth = targetRect.width / previewEvent.laneCount;
    const previewWidth = Math.max(laneWidth - 8, 1);
    const previewX = clamp(pointerX - drag.offsetX, targetRect.left + 4, targetRect.right - previewWidth - 4);
    const previewHeight = Math.max(((endMinute - startMinute) / minutesPerDay) * targetRect.height, 28);
    const previewY = clamp(pointerY - drag.offsetY, targetRect.top, targetRect.bottom - previewHeight);
    preview.style.inlineSize = `${previewWidth}px`;
    preview.style.blockSize = `${previewHeight}px`;
    preview.style.setProperty('--event-start-minute', `${startMinute}`);
    preview.style.setProperty('--event-end-minute', `${endMinute}`);
    setPreviewPosition(preview, previewX, previewY);
}

function getResizeEdge(eventElement: HTMLElement, source: Element): SchedulerResizeEdge | null {
    const handle = source.closest<HTMLElement>('[data-nt-scheduler-resize-edge]');
    const declaredEdge = handle && eventElement.contains(handle) ? handle.dataset.ntSchedulerResizeEdge : null;
    return declaredEdge === 'start' || declaredEdge === 'end' ? declaredEdge : null;
}

function updateSelection(selection: SchedulerSelection, scheduler: SchedulerElement, clientY: number): void {
    const currentMinutes = getSnappedMinutes(selection.target, clientY, scheduler, true);
    if (currentMinutes === null) {
        return;
    }

    const snapMinutes = getSnapMinutes(scheduler);
    selection.startMinutes = Math.min(selection.anchorMinutes, currentMinutes);
    selection.endMinutes = currentMinutes > selection.anchorMinutes
        ? currentMinutes
        : Math.min(selection.anchorMinutes + snapMinutes, minutesPerDay);
    if (selection.endMinutes <= selection.startMinutes) {
        selection.endMinutes = Math.min(selection.startMinutes + snapMinutes, minutesPerDay);
    }

    const indicator = selection.indicator ?? document.createElement('div');
    if (!selection.indicator) {
        indicator.className = 'time-range-selection';
        indicator.setAttribute('aria-hidden', 'true');
        selection.target.append(indicator);
        selection.indicator = indicator;
    }

    indicator.style.setProperty('--selection-start-minute', `${selection.startMinutes}`);
    indicator.style.setProperty('--selection-end-minute', `${selection.endMinutes}`);
}

function cleanupSelection(state: SchedulerState): SchedulerSelection | null {
    const selection = state.activeSelection;
    if (!selection) {
        return null;
    }

    selection.indicator?.remove();
    selection.target.classList.remove('range-selecting');
    state.activeSelection = null;
    return selection;
}

function getResizeInfo(scheduler: SchedulerElement, drag: SchedulerDrag, event: PointerEvent): SchedulerResize | null {
    if (!drag.resizeEdge) {
        return null;
    }

    const element = document.elementFromPoint(event.clientX, event.clientY);
    const target = element instanceof Element ? element.closest<HTMLElement>(dropDateSelector) : null;
    if (!target || !scheduler.contains(target) || target.dataset.ntSchedulerDropTime !== 'true') {
        return null;
    }

    const snappedMinutes = getSnappedMinutes(target, event.clientY, scheduler, drag.resizeEdge === 'end');
    if (snappedMinutes === null) {
        return null;
    }

    const minimumDuration = Math.max(getSnapMinutes(scheduler), 15);
    const originalEndMinute = drag.startMinute + drag.durationMinutes;
    if (drag.resizeEdge === 'start') {
        const startMinutes = clamp(snappedMinutes, 0, originalEndMinute - minimumDuration);
        return { endMinutes: originalEndMinute, startMinutes, target };
    }

    const endMinutes = clamp(snappedMinutes, drag.startMinute + minimumDuration, minutesPerDay);
    return { endMinutes, startMinutes: drag.startMinute, target };
}

function applyTimedResizeLayout(drag: SchedulerDrag, resize: SchedulerResize): void {
    rememberEventStyle(drag, drag.eventElement);
    drag.eventElement.style.setProperty('--event-start-minute', `${resize.startMinutes}`);
    drag.eventElement.style.setProperty('--event-end-minute', `${resize.endMinutes}`);
    setActiveTarget(drag, updateDropIndicator(drag, resize.target, resize.startMinutes, resize.endMinutes - resize.startMinutes));
}

function updateDragPreview(scheduler: SchedulerElement, drag: SchedulerDrag, event: PointerEvent): void {
    restoreReactiveLayout(drag);
    if (drag.resizeEdge) {
        const resize = getResizeInfo(scheduler, drag, event);
        drag.currentResize = resize;
        if (resize) {
            applyTimedResizeLayout(drag, resize);
        }
        else {
            removeDropIndicator(drag);
            setActiveTarget(drag, null);
        }

        return;
    }

    const drop = getDropInfo(scheduler, drag, event);
    drag.currentDrop = drop;

    if (drop?.target && drop.minutes !== null && drag.isTimedEvent) {
        setActiveTarget(drag, updateDropIndicator(drag, drop.target, drop.minutes));
        applyTimedPreviewLayout(drag, drop.target, drop.minutes, event.clientX, event.clientY);
        return;
    }

    removeDropIndicator(drag);
    setActiveTarget(drag, drop?.target ?? null);
    updatePointerPreview(drag, event);
}

function shouldStartDrag(drag: SchedulerDrag, event: PointerEvent): boolean {
    return Math.abs(event.clientX - drag.startX) >= dragThresholdPixels || Math.abs(event.clientY - drag.startY) >= dragThresholdPixels;
}

function cleanupDrag(state: SchedulerState): SchedulerDrag | null {
    const drag = state.activeDrag;
    if (!drag) {
        return null;
    }

    drag.currentTarget?.classList.remove('drop-target-active');
    removeDropIndicator(drag);
    restoreReactiveLayout(drag);
    drag.eventElement.classList.remove('event-dragging');
    drag.eventElement.classList.remove('event-resizing');
    drag.preview?.remove();
    state.activeDrag = null;
    return drag;
}

function attachScheduler(scheduler: SchedulerElement, dotNetRef: Maybe<DotNetSchedulerReference>): void {
    if (!dotNetRef) {
        return;
    }

    if (scheduler.__ntSchedulerState) {
        scheduler.__ntSchedulerState.dotNetRef = dotNetRef;
        return;
    }

    const state: SchedulerState = {
        activeDrag: null,
        activeSelection: null,
        dotNetRef,
        suppressClickUntil: 0,
        onClick: event => {
            if (state.suppressClickUntil <= performance.now()) {
                return;
            }

            event.preventDefault();
            event.stopImmediatePropagation();
        },
        onKeyDown: event => {
            if (event.key === 'Escape' && (state.activeDrag || state.activeSelection)) {
                event.preventDefault();
                cleanupDrag(state);
                cleanupSelection(state);
            }
        },
        onPointerCancel: event => {
            if (state.activeDrag?.pointerId === event.pointerId) {
                cleanupDrag(state);
            }

            if (state.activeSelection?.pointerId === event.pointerId) {
                cleanupSelection(state);
            }
        },
        onPointerDown: event => {
            if (!event.isPrimary || event.button !== 0 || !(event.target instanceof Element)) {
                return;
            }

            const eventElement = event.target.closest<HTMLElement>(eventSelector);
            const eventId = eventElement?.dataset.ntSchedulerEventId ?? null;
            if (eventElement && eventId && scheduler.contains(eventElement)) {
                if (!isDraggingEnabled(scheduler)) {
                    return;
                }

                const rect = eventElement.getBoundingClientRect();
                state.activeDrag = {
                    affectedEventStyles: new Map<HTMLElement, string | null>(),
                    currentDrop: null,
                    currentResize: null,
                    currentTarget: null,
                    dropIndicator: null,
                    durationMinutes: getTimedEventDuration(eventElement, scheduler),
                    eventElement,
                    eventId,
                    hasMoved: false,
                    isTimedEvent: eventElement.classList.contains('event-timed'),
                    offsetX: event.clientX - rect.left,
                    offsetY: event.clientY - rect.top,
                    pointerId: event.pointerId,
                    preview: null,
                    resizeEdge: getResizeEdge(eventElement, event.target),
                    startMinute: getCssNumber(eventElement, '--event-start-minute') ?? 0,
                    startX: event.clientX,
                    startY: event.clientY
                };
                eventElement.setPointerCapture?.(event.pointerId);
                return;
            }

            const target = event.target.closest<HTMLElement>('[data-nt-scheduler-drop-time="true"]');
            const date = target?.dataset.ntSchedulerDropDate;
            const anchorMinutes = target ? getSnappedMinutes(target, event.clientY, scheduler) : null;
            if (!isRangeSelectionEnabled(scheduler) || !target || !date || anchorMinutes === null || !scheduler.contains(target)) {
                return;
            }

            state.activeSelection = {
                anchorMinutes,
                date,
                endMinutes: Math.min(anchorMinutes + getSnapMinutes(scheduler), minutesPerDay),
                hasMoved: false,
                indicator: null,
                pointerId: event.pointerId,
                startMinutes: anchorMinutes,
                startX: event.clientX,
                startY: event.clientY,
                target
            };
        },
        onPointerMove: event => {
            const selection = state.activeSelection;
            if (selection?.pointerId === event.pointerId) {
                if (!selection.hasMoved) {
                    if (Math.abs(event.clientX - selection.startX) < dragThresholdPixels && Math.abs(event.clientY - selection.startY) < dragThresholdPixels) {
                        return;
                    }

                    selection.hasMoved = true;
                    selection.target.setPointerCapture?.(event.pointerId);
                    selection.target.classList.add('range-selecting');
                }

                event.preventDefault();
                updateSelection(selection, scheduler, event.clientY);
                return;
            }

            const drag = state.activeDrag;
            if (!drag || drag.pointerId !== event.pointerId) {
                return;
            }

            if (!drag.hasMoved) {
                if (!shouldStartDrag(drag, event)) {
                    return;
                }

                drag.hasMoved = true;
                drag.eventElement.classList.add(drag.resizeEdge ? 'event-resizing' : 'event-dragging');
                if (!drag.resizeEdge) {
                    drag.preview = createPreview(drag, event);
                }
            }

            event.preventDefault();
            updateDragPreview(scheduler, drag, event);
        },
        onPointerUp: event => {
            const selection = state.activeSelection;
            if (selection?.pointerId === event.pointerId) {
                if (!selection.hasMoved) {
                    cleanupSelection(state);
                    return;
                }

                event.preventDefault();
                const date = selection.date;
                const startMinutes = selection.startMinutes;
                const endMinutes = selection.endMinutes;
                cleanupSelection(state);
                state.suppressClickUntil = performance.now() + 400;
                void state.dotNetRef.invokeMethodAsync('NotifySlotSelectedAsync', date, startMinutes, endMinutes);
                return;
            }

            const drag = state.activeDrag;
            if (!drag || drag.pointerId !== event.pointerId) {
                return;
            }

            if (!drag.hasMoved) {
                cleanupDrag(state);
                return;
            }

            event.preventDefault();
            const drop = drag.currentDrop;
            const resize = drag.currentResize;
            const target = drop?.target ?? null;
            const date = drop?.date ?? null;
            const minutes = drop?.minutes ?? null;
            const eventId = drag.eventId;
            const resizeEdge = drag.resizeEdge;
            cleanupDrag(state);
            state.suppressClickUntil = performance.now() + 400;

            if (resizeEdge && resize) {
                void state.dotNetRef.invokeMethodAsync(
                    'NotifyEventResizedAsync',
                    eventId,
                    resizeEdge === 'start' ? resize.startMinutes : null,
                    resizeEdge === 'end' ? resize.endMinutes : null);
            }
            else if (target && date) {
                void state.dotNetRef.invokeMethodAsync('NotifyEventDroppedAsync', eventId, date, minutes);
            }
        }
    };

    scheduler.addEventListener('click', state.onClick, true);
    scheduler.addEventListener('keydown', state.onKeyDown);
    scheduler.addEventListener('pointercancel', state.onPointerCancel);
    scheduler.addEventListener('pointerdown', state.onPointerDown);
    scheduler.addEventListener('pointermove', state.onPointerMove);
    scheduler.addEventListener('pointerup', state.onPointerUp);
    scheduler.__ntSchedulerState = state;
}

function detachScheduler(scheduler: Maybe<SchedulerElement>): void {
    const state = scheduler?.__ntSchedulerState;
    if (!scheduler || !state) {
        return;
    }

    cleanupDrag(state);
    cleanupSelection(state);
    scheduler.removeEventListener('click', state.onClick, true);
    scheduler.removeEventListener('keydown', state.onKeyDown);
    scheduler.removeEventListener('pointercancel', state.onPointerCancel);
    scheduler.removeEventListener('pointerdown', state.onPointerDown);
    scheduler.removeEventListener('pointermove', state.onPointerMove);
    scheduler.removeEventListener('pointerup', state.onPointerUp);
    delete scheduler.__ntSchedulerState;
}

export function onLoad(element: Maybe<Element>, dotNetRef: Maybe<DotNetSchedulerReference>): void {
    const scheduler = getScheduler(element);
    if (scheduler) {
        attachScheduler(scheduler, dotNetRef);
    }
}

export function onUpdate(element: Maybe<Element>, dotNetRef: Maybe<DotNetSchedulerReference>): void {
    const scheduler = getScheduler(element);
    if (scheduler) {
        attachScheduler(scheduler, dotNetRef);
    }
}

export function onDispose(element: Maybe<Element>): void {
    const scheduler = getScheduler(element);
    if (scheduler) {
        detachScheduler(scheduler);
        return;
    }

    for (const candidate of getSchedulers(document)) {
        detachScheduler(candidate);
    }
}
