const schedulerState = new WeakMap();
const DRAG_THRESHOLD_PX = 6;
const GHOST_CURSOR_GAP_PX = 14;
const PREVIEW_INSET_PX = 4;

function canDrag(root) {
    return root?.dataset?.ntSchedulerCanDrag === 'true';
}

function getDropTarget(target) {
    const slotElement = target?.closest?.('[data-nt-scheduler-slot-start]') ?? null;
    if (!slotElement) {
        return null;
    }

    return {
        slotElement,
        slotStart: slotElement.getAttribute('data-nt-scheduler-slot-start')
    };
}

function getEventElement(target) {
    return target?.closest?.('[data-nt-scheduler-event-id]') ?? null;
}

function getEventId(target) {
    return getEventElement(target)?.getAttribute('data-nt-scheduler-event-id') ?? null;
}

function getPointerDistance(startX, startY, clientX, clientY) {
    return Math.hypot(clientX - startX, clientY - startY);
}

function getDropTargetFromPoint(clientX, clientY) {
    const elementFromPoint = document.elementsFromPoint?.(clientX, clientY)
        ?? (document.elementFromPoint?.(clientX, clientY) ? [document.elementFromPoint(clientX, clientY)] : []);

    for (const element of elementFromPoint) {
        const dropTarget = getDropTarget(element);
        if (dropTarget?.slotStart) {
            return dropTarget;
        }
    }

    return null;
}

function createVisualClone(sourceElement, className) {
    const clone = sourceElement.cloneNode(true);
    clone.classList.add(className);
    clone.removeAttribute('data-nt-scheduler-event-id');
    clone.removeAttribute('draggable');
    clone.removeAttribute('id');
    clone.style.position = 'fixed';
    clone.style.left = '0';
    clone.style.top = '0';
    clone.style.margin = '0';
    clone.style.pointerEvents = 'none';
    document.body.appendChild(clone);
    return clone;
}

function setVisualRect(element, { height, left, top, width }) {
    element.style.left = `${left}px`;
    element.style.top = `${top}px`;
    element.style.width = `${Math.max(width, 40)}px`;
    element.style.height = `${Math.max(height, 28)}px`;
}

function getPreviewRect(slotElement, sourceRect) {
    const monthCell = slotElement.closest('.nt-scheduler-month-view__cell');
    if (monthCell) {
        const eventsHost = monthCell.querySelector('.nt-scheduler-month-view__events');
        const hostRect = (eventsHost ?? monthCell).getBoundingClientRect();
        return {
            height: sourceRect.height,
            left: hostRect.left,
            top: hostRect.top,
            width: Math.min(sourceRect.width, hostRect.width)
        };
    }

    const dayColumn = slotElement.closest('.nt-scheduler-week-view__day-column, .nt-scheduler-day-view__day-column');
    if (dayColumn) {
        const slotRect = slotElement.getBoundingClientRect();
        const columnRect = dayColumn.getBoundingClientRect();
        return {
            height: sourceRect.height,
            left: columnRect.left + PREVIEW_INSET_PX,
            top: slotRect.top + PREVIEW_INSET_PX,
            width: Math.min(sourceRect.width, Math.max(40, columnRect.width - (PREVIEW_INSET_PX * 2)))
        };
    }

    const slotRect = slotElement.getBoundingClientRect();
    return {
        height: sourceRect.height,
        left: slotRect.left,
        top: slotRect.top,
        width: Math.min(sourceRect.width, slotRect.width)
    };
}

function showDropPreview(activeDrag, dropTarget) {
    if (!activeDrag.previewElement || !dropTarget?.slotElement) {
        return;
    }

    const previewRect = getPreviewRect(dropTarget.slotElement, activeDrag.sourceRect);
    setVisualRect(activeDrag.previewElement, previewRect);
    activeDrag.previewElement.hidden = false;
}

function hideDropPreview(activeDrag) {
    if (activeDrag?.previewElement) {
        activeDrag.previewElement.hidden = true;
    }
}

function clearPendingDrag(state) {
    state.pendingDrag = null;
}

function clearActiveDrag(root, state) {
    state.activeDrag?.ghostElement?.remove();
    state.activeDrag?.previewElement?.remove();
    state.activeDrag = null;
    root.classList.remove('nt-scheduler--dragging');
}

function suppressNextClick(state) {
    state.suppressClickUntil = Date.now() + 250;
}

function onPointerDownFactory(root) {
    return (event) => {
        if (!canDrag(root) || !event.isPrimary || event.button !== 0) {
            return;
        }

        const eventElement = getEventElement(event.target);
        const eventId = getEventId(event.target);
        if (!eventElement || !eventId) {
            return;
        }

        const state = schedulerState.get(root);
        if (!state) {
            return;
        }

        const sourceRect = eventElement.getBoundingClientRect();
        state.pendingDrag = {
            eventElement,
            eventId,
            pointerOffsetX: event.clientX - sourceRect.left,
            pointerOffsetY: event.clientY - sourceRect.top,
            pointerId: event.pointerId,
            sourceRect,
            startX: event.clientX,
            startY: event.clientY
        };
    };
}

function onPointerMoveFactory(root) {
    return (event) => {
        const state = schedulerState.get(root);
        if (!state) {
            return;
        }

        if (!state.activeDrag && state.pendingDrag?.pointerId === event.pointerId) {
            const distance = getPointerDistance(
                state.pendingDrag.startX,
                state.pendingDrag.startY,
                event.clientX,
                event.clientY
            );

            if (distance < DRAG_THRESHOLD_PX) {
                return;
            }

            state.activeDrag = {
                eventId: state.pendingDrag.eventId,
                ghostElement: createVisualClone(state.pendingDrag.eventElement, 'nt-scheduler__drag-ghost'),
                pointerOffsetX: state.pendingDrag.pointerOffsetX,
                pointerOffsetY: state.pendingDrag.pointerOffsetY,
                pointerId: state.pendingDrag.pointerId,
                previewElement: createVisualClone(state.pendingDrag.eventElement, 'nt-scheduler__drop-preview'),
                slotStart: null,
                sourceRect: state.pendingDrag.sourceRect
            };

            state.activeDrag.previewElement.hidden = true;
            clearPendingDrag(state);
            root.classList.add('nt-scheduler--dragging');
        }

        if (!state.activeDrag || state.activeDrag.pointerId !== event.pointerId) {
            return;
        }

        if (event.cancelable) {
            event.preventDefault();
        }

        setVisualRect(state.activeDrag.ghostElement, {
            height: state.activeDrag.sourceRect.height,
            left: event.clientX - state.activeDrag.pointerOffsetX,
            top: event.clientY + GHOST_CURSOR_GAP_PX,
            width: state.activeDrag.sourceRect.width
        });

        const dropTarget = getDropTargetFromPoint(event.clientX, event.clientY);
        state.activeDrag.slotStart = dropTarget?.slotStart ?? null;

        if (dropTarget) {
            showDropPreview(state.activeDrag, dropTarget);
        }
        else {
            hideDropPreview(state.activeDrag);
        }
    };
}

function onPointerUpFactory(root) {
    return async (event) => {
        const state = schedulerState.get(root);
        if (!state) {
            return;
        }

        if (state.pendingDrag?.pointerId === event.pointerId) {
            clearPendingDrag(state);
            return;
        }

        if (!state.activeDrag || state.activeDrag.pointerId !== event.pointerId) {
            return;
        }

        if (event.cancelable) {
            event.preventDefault();
        }

        const { eventId } = state.activeDrag;
        const slotStart = state.activeDrag.slotStart ?? getDropTargetFromPoint(event.clientX, event.clientY)?.slotStart ?? null;
        clearActiveDrag(root, state);
        suppressNextClick(state);

        if (!eventId || !slotStart) {
            return;
        }

        try {
            await state.dotNetRef?.invokeMethodAsync('HandleJsDropAsync', eventId, slotStart);
        }
        catch {
            root.classList.remove('nt-scheduler--dragging');
        }
    };
}

function onPointerCancelFactory(root) {
    return (event) => {
        const state = schedulerState.get(root);
        if (!state) {
            return;
        }

        if (state.pendingDrag?.pointerId === event.pointerId) {
            clearPendingDrag(state);
        }

        if (state.activeDrag?.pointerId === event.pointerId) {
            clearActiveDrag(root, state);
        }
    };
}

function onClickCaptureFactory(root) {
    return (event) => {
        const state = schedulerState.get(root);
        if (!state || Date.now() > state.suppressClickUntil) {
            return;
        }

        const eventId = getEventId(event.target);
        if (!eventId) {
            return;
        }

        state.suppressClickUntil = 0;
        event.preventDefault();
        event.stopPropagation();
    };
}

export function onLoad(root, dotNetRef) {
    if (!root || schedulerState.has(root)) {
        return;
    }

    const state = {
        activeDrag: null,
        dotNetRef,
        pendingDrag: null,
        suppressClickUntil: 0
    };

    state.onPointerDown = onPointerDownFactory(root);
    state.onPointerMove = onPointerMoveFactory(root);
    state.onPointerUp = onPointerUpFactory(root);
    state.onPointerCancel = onPointerCancelFactory(root);
    state.onClickCapture = onClickCaptureFactory(root);

    root.addEventListener('pointerdown', state.onPointerDown);
    root.addEventListener('click', state.onClickCapture, true);
    window.addEventListener('pointermove', state.onPointerMove, { passive: false });
    window.addEventListener('pointerup', state.onPointerUp, { passive: false });
    window.addEventListener('pointercancel', state.onPointerCancel);

    schedulerState.set(root, state);
}

export function onUpdate() { }

export function onDispose(root) {
    if (!root) {
        return;
    }

    const state = schedulerState.get(root);
    if (!state) {
        return;
    }

    root.removeEventListener('pointerdown', state.onPointerDown);
    root.removeEventListener('click', state.onClickCapture, true);
    window.removeEventListener('pointermove', state.onPointerMove, { passive: false });
    window.removeEventListener('pointerup', state.onPointerUp, { passive: false });
    window.removeEventListener('pointercancel', state.onPointerCancel);
    root.classList.remove('nt-scheduler--dragging');

    schedulerState.delete(root);
}
