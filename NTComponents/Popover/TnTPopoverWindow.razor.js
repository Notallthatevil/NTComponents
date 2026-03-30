const states = new WeakMap();
const animationOptions = {
    duration: 220,
    easing: 'cubic-bezier(0.2, 0, 0, 1)',
    fill: 'both'
};

function clamp(value, min, max) {
    return Math.min(Math.max(value, min), max);
}

function applyPosition(element, left, top) {
    if (!element) {
        return;
    }

    element.style.left = `${left}px`;
    element.style.top = `${top}px`;
}

function getHandle(element) {
    return element?.querySelector('[data-tnt-popover-drag-handle="true"]') ?? null;
}

function getLauncherStrip() {
    return document.querySelector('[data-tnt-popover-launchers="true"]');
}

function getLauncher(popoverId) {
    if (!popoverId || typeof CSS === 'undefined' || typeof CSS.escape !== 'function') {
        return null;
    }

    return document.querySelector(`[data-tnt-popover-launcher-id="${CSS.escape(popoverId)}"]`);
}

function getFallbackLauncherRect() {
    const strip = getLauncherStrip();
    if (!strip) {
        return null;
    }

    const stripRect = strip.getBoundingClientRect();
    const width = Math.min(192, Math.max(144, stripRect.width * 0.2));
    const height = 44;

    return {
        height,
        left: stripRect.left,
        top: stripRect.bottom - height,
        width
    };
}

function getTargetRect(popoverId) {
    const launcher = getLauncher(popoverId);
    return launcher?.getBoundingClientRect() ?? getFallbackLauncherRect();
}

function getDelta(sourceRect, targetRect) {
    const sourceCenterX = sourceRect.left + (sourceRect.width / 2);
    const sourceCenterY = sourceRect.top + (sourceRect.height / 2);
    const targetCenterX = targetRect.left + (targetRect.width / 2);
    const targetCenterY = targetRect.top + (targetRect.height / 2);

    return {
        scaleX: clamp(targetRect.width / sourceRect.width, 0.18, 1),
        scaleY: clamp(targetRect.height / sourceRect.height, 0.18, 1),
        x: targetCenterX - sourceCenterX,
        y: targetCenterY - sourceCenterY
    };
}

async function runAnimation(element, keyframes) {
    if (typeof element?.animate !== 'function') {
        return;
    }

    const animation = element.animate(keyframes, animationOptions);

    try {
        await animation.finished;
    } catch {
    }
}

function notifyActivated(state) {
    return state.dotNetObjectRef?.invokeMethodAsync('NotifyActivated') ?? Promise.resolve();
}

function cleanupDrag(state) {
    if (!state.isDragging) {
        return;
    }

    state.isDragging = false;
    state.element.classList.remove('tnt-popover--dragging');
    window.removeEventListener('pointermove', state.onPointerMove);
    window.removeEventListener('pointerup', state.onPointerUp);
    window.removeEventListener('pointercancel', state.onPointerUp);
}

export function initializePopoverWindow(element, dotNetObjectRef, options) {
    if (!element) {
        return;
    }

    disposePopoverWindow(element);

    const state = {
        dotNetObjectRef,
        element,
        isDragging: false,
        left: options?.left ?? 0,
        options: options ?? {},
        pointerId: null,
        startClientX: 0,
        startClientY: 0,
        startLeft: options?.left ?? 0,
        startTop: options?.top ?? 0,
        top: options?.top ?? 0
    };

    state.onFocusIn = () => {
        notifyActivated(state);
    };

    state.onPointerMove = event => {
        if (!state.isDragging) {
            return;
        }

        const viewportPadding = state.options.viewportPadding ?? 16;
        const width = state.element.offsetWidth;
        const height = state.element.offsetHeight;
        const maxLeft = Math.max(viewportPadding, window.innerWidth - width - viewportPadding);
        const maxTop = Math.max(viewportPadding, window.innerHeight - height - viewportPadding);
        const nextLeft = clamp(state.startLeft + (event.clientX - state.startClientX), viewportPadding, maxLeft);
        const nextTop = clamp(state.startTop + (event.clientY - state.startClientY), viewportPadding, maxTop);

        state.left = nextLeft;
        state.top = nextTop;
        applyPosition(state.element, nextLeft, nextTop);
    };

    state.onPointerUp = async () => {
        cleanupDrag(state);
        await state.dotNetObjectRef?.invokeMethodAsync('NotifyPositionChanged', state.left, state.top);
    };

    state.onPointerDown = async event => {
        if ((event.button ?? 0) !== 0 || !state.options.allowDragging) {
            return;
        }

        if (event.target?.closest('button, a, input, textarea, select, [role="button"]')) {
            return;
        }

        state.isDragging = true;
        state.pointerId = event.pointerId ?? null;
        state.startClientX = event.clientX;
        state.startClientY = event.clientY;
        state.startLeft = state.left;
        state.startTop = state.top;
        state.element.classList.add('tnt-popover--dragging');

        if (typeof state.handle?.setPointerCapture === 'function' && state.pointerId !== null) {
            state.handle.setPointerCapture(state.pointerId);
        }

        window.addEventListener('pointermove', state.onPointerMove);
        window.addEventListener('pointerup', state.onPointerUp);
        window.addEventListener('pointercancel', state.onPointerUp);

        await notifyActivated(state);
    };

    state.handle = getHandle(element);
    state.handle?.addEventListener('pointerdown', state.onPointerDown);
    state.element.addEventListener('focusin', state.onFocusIn);

    states.set(element, state);
    applyPosition(element, state.left, state.top);
}

export function updatePopoverWindow(element, options) {
    const state = states.get(element);
    if (!state) {
        return;
    }

    state.options = options ?? {};
    state.left = options?.left ?? state.left;
    state.top = options?.top ?? state.top;
    applyPosition(element, state.left, state.top);
}

export async function animatePopoverToLauncher(element, popoverId) {
    if (!element) {
        return;
    }

    const state = states.get(element);
    if (state) {
        cleanupDrag(state);
    }

    const sourceRect = element.getBoundingClientRect();
    const targetRect = getTargetRect(popoverId);
    if (!targetRect || sourceRect.width === 0 || sourceRect.height === 0) {
        return;
    }

    const delta = getDelta(sourceRect, targetRect);
    element.style.pointerEvents = 'none';

    await runAnimation(element, [
        {
            opacity: 1,
            transform: 'translate(0px, 0px) scale(1)',
            transformOrigin: 'center center'
        },
        {
            opacity: 0.24,
            transform: `translate(${delta.x}px, ${delta.y}px) scale(${delta.scaleX}, ${delta.scaleY})`,
            transformOrigin: 'center center'
        }
    ]);
}

export async function animatePopoverFromLauncher(element, popoverId) {
    if (!element) {
        return;
    }

    const sourceRect = getTargetRect(popoverId);
    const targetRect = element.getBoundingClientRect();
    if (!sourceRect || targetRect.width === 0 || targetRect.height === 0) {
        return;
    }

    const delta = getDelta(targetRect, sourceRect);

    await runAnimation(element, [
        {
            opacity: 0.24,
            transform: `translate(${delta.x}px, ${delta.y}px) scale(${delta.scaleX}, ${delta.scaleY})`,
            transformOrigin: 'center center'
        },
        {
            opacity: 1,
            transform: 'translate(0px, 0px) scale(1)',
            transformOrigin: 'center center'
        }
    ]);
}

export function disposePopoverWindow(element) {
    const state = states.get(element);
    if (!state) {
        return;
    }

    cleanupDrag(state);
    state.handle?.removeEventListener('pointerdown', state.onPointerDown);
    state.element.removeEventListener('focusin', state.onFocusIn);

    if (typeof state.handle?.releasePointerCapture === 'function' && state.pointerId !== null) {
        state.handle.releasePointerCapture(state.pointerId);
    }

    states.delete(element);
}
