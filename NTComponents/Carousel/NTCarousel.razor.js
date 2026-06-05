const carouselsByIdentifier = new Map();

const gapSize = 8;
export class NTCarousel extends HTMLElement {
    static observedAttributes = [NTComponents.customAttribute, 'tnt-auto-play-interval', 'tnt-allow-dragging'];
    constructor() {
        super();
        this.carouselViewPort = null;
        this.carouselItems = null;
        this.fullSize = null;
        this._scrollListener = null;
        this._scrollRafId = null;
        // Drag state
        this._isDragging = false;
        this._dragStartX = 0;
        this._dragScrollLeft = 0;
        this._dragMoved = false;
        this._onPointerDown = null;
        this._onPointerMove = null;
        this._onPointerUp = null;
        this._onPointerLeave = null;
        this._onClickCapture = null;
        this._rafId = null; // for throttling child width recalculations during drag
        // Resize observer
        this._resizeObserver = null;
        // Auto play
        this._autoPlayIntervalMs = null;
        this._autoPlayTimerId = null;
        this._currentAutoIndex = -1; // start before first
        // Nav buttons
        this._prevButton = null;
        this._nextButton = null;
        this._onPrevClick = null;
        this._onNextClick = null;
        // Dragging enable flag
        this._allowDragging = true; // default true for backward compatibility
        // Internal caches
        this._lastChildCount = 0;
    }

    disconnectedCallback() {
        let identifier = this.getAttribute(NTComponents.customAttribute);
        if (carouselsByIdentifier.get(identifier)) {
            carouselsByIdentifier.delete(identifier);
        }
        if (this._scrollListener && this.carouselViewPort) {
            this.carouselViewPort.removeEventListener('scroll', this._scrollListener);
            this._scrollListener = null;
        }
        if (this._scrollRafId) {
            cancelAnimationFrame(this._scrollRafId);
            this._scrollRafId = null;
        }
        if (this._rafId) {
            cancelAnimationFrame(this._rafId);
            this._rafId = null;
        }
        if (this._resizeObserver) {
            this._resizeObserver.disconnect();
            this._resizeObserver = null;
        }
        this._detachNavButtonEvents();
        this._stopAutoPlay();
        this._detachDragEvents();
    }

    attributeChangedCallback(name, oldValue, newValue) {
        if (name === NTComponents.customAttribute && oldValue != newValue) {
            if (carouselsByIdentifier.get(oldValue)) {
                carouselsByIdentifier.delete(oldValue);
            }
            carouselsByIdentifier.set(newValue, this);
            this.onUpdate();
        } else if (name === 'tnt-auto-play-interval' && oldValue !== newValue) {
            this._configureAutoPlayFromAttribute();
        } else if (name === 'tnt-allow-dragging' && oldValue !== newValue) {
            this._updateDraggingFromAttribute();
        }
    }

    _isCenteredMode() { return this.classList.contains('tnt-carousel-centered'); }

    _updateDraggingFromAttribute() {
        const attr = this.getAttribute('tnt-allow-dragging');
        if (attr == null) {
            this._allowDragging = false; // opt-in only
        } else {
            const val = attr.trim().toLowerCase();
            this._allowDragging = !(val === 'false' || val === '0' || val === 'no' || val === 'off');
        }
        if (this._allowDragging) {
            this._attachDragEvents();
        } else {
            this._detachDragEvents();
            this._isDragging = false;
            this.carouselViewPort?.classList.remove('tnt-carousel-dragging');
        }
    }

    _configureAutoPlayFromAttribute() {
        const attr = this.getAttribute('tnt-auto-play-interval');
        if (attr == null || attr === '') {
            this._autoPlayIntervalMs = null;
            this._stopAutoPlay();
            return;
        }
        const seconds = parseFloat(attr);
        if (!isNaN(seconds) && seconds > 0) {
            this._autoPlayIntervalMs = seconds * 1000;
            this._startAutoPlay();
        } else {
            this._autoPlayIntervalMs = null;
            this._stopAutoPlay();
        }
    }

    _startAutoPlay() {
        this._stopAutoPlay();
        if (!this._autoPlayIntervalMs || !this.carouselItems || this.carouselItems.length === 0) return;
        if (this._currentAutoIndex < 0 || this._currentAutoIndex >= this.carouselItems.length) {
            this._currentAutoIndex = -1;
        }
        this._autoPlayTimerId = setInterval(() => {
            if (this._isDragging) return;
            this._advanceAutoPlay();
        }, this._autoPlayIntervalMs);
    }

    _stopAutoPlay() { if (this._autoPlayTimerId) { clearInterval(this._autoPlayTimerId); this._autoPlayTimerId = null; } }

    _advanceAutoPlay() {
        if (!this.carouselItems || this.carouselItems.length === 0) return;
        this._currentAutoIndex = (this._currentAutoIndex + 1) % this.carouselItems.length;
        const target = this.carouselItems[this._currentAutoIndex];
        if (target) this._scrollToItem(target);
    }

    _scrollToItem(item) {
        if (!this.carouselViewPort || !item) return;
        const left = this._isCenteredMode()
            ? item.offsetLeft - (this.carouselViewPort.clientWidth - item.offsetWidth) / 2
            : item.offsetLeft;
        this.carouselViewPort.scrollTo({ left, behavior: 'smooth' });
    }

    _recalculateChildWidths() {
        if (!this.carouselItems) return;
        // Convert NodeList to array only once per call
        for (const item of this.carouselItems) {
            if (typeof item.updateItemWidth === 'function') item.updateItemWidth();
        }
    }

    _detachDragEvents() {
        if (!this.carouselViewPort) return;
        if (this._onPointerDown) this.carouselViewPort.removeEventListener('pointerdown', this._onPointerDown);
        if (this._onPointerMove) this.carouselViewPort.removeEventListener('pointermove', this._onPointerMove);
        if (this._onPointerUp) this.carouselViewPort.removeEventListener('pointerup', this._onPointerUp);
        if (this._onPointerLeave) this.carouselViewPort.removeEventListener('pointerleave', this._onPointerLeave);
        if (this._onClickCapture) this.carouselViewPort.removeEventListener('click', this._onClickCapture, true);
        this._onPointerDown = this._onPointerMove = this._onPointerUp = this._onPointerLeave = this._onClickCapture = null;
    }

    _scheduleRecalc() {
        if (this._rafId) return;
        this._rafId = requestAnimationFrame(() => {
            this._rafId = null;
            this._recalculateChildWidths();
        });
    }

    _attachDragEvents() {
        if (!this.carouselViewPort || !this._allowDragging) {
            this._detachDragEvents();
            return;
        }
        // If already attached, skip
        if (this._onPointerDown) return;
        this._detachDragEvents();

        const moveThreshold = 8;
        this._onPointerDown = (e) => {
            if (e.button !== 0 && e.pointerType === 'mouse') return;
            this._isDragging = true;
            this._dragMoved = false;
            this._dragStartX = e.clientX;
            this._dragScrollLeft = this.carouselViewPort.scrollLeft;
            this.carouselViewPort.classList.add('tnt-carousel-dragging');
            this._stopAutoPlay();
            try { this.carouselViewPort.setPointerCapture(e.pointerId); } catch { }
        };
        this._onPointerMove = (e) => {
            if (!this._isDragging) return;
            const dx = e.clientX - this._dragStartX;
            if (Math.abs(dx) > moveThreshold) this._dragMoved = true;
            e.preventDefault();
            const newLeft = this._dragScrollLeft - dx;
            if (this.carouselViewPort.scrollLeft !== newLeft) {
                this.carouselViewPort.scrollLeft = newLeft;
                this._scheduleRecalc();
                this.carouselViewPort.dispatchEvent(new Event('scroll'));
            }
        };
        const finishDrag = () => {
            if (!this._isDragging) return;
            this._isDragging = false;
            this.carouselViewPort.classList.remove('tnt-carousel-dragging');
            this._scheduleRecalc();
            this._syncAutoIndexToViewport();
            this._startAutoPlay();
        };
        this._onPointerUp = () => { finishDrag(); };
        this._onPointerLeave = () => { finishDrag(); };
        this._onClickCapture = (e) => { if (this._dragMoved) { e.stopPropagation(); e.preventDefault(); } };

        this.carouselViewPort.addEventListener('pointerdown', this._onPointerDown, { passive: true });
        this.carouselViewPort.addEventListener('pointermove', this._onPointerMove, { passive: false });
        this.carouselViewPort.addEventListener('pointerup', this._onPointerUp, { passive: true });
        this.carouselViewPort.addEventListener('pointerleave', this._onPointerLeave, { passive: true });
        this.carouselViewPort.addEventListener('click', this._onClickCapture, true);
    }

    _syncAutoIndexToViewport() {
        if (!this.carouselItems || this.carouselItems.length === 0) return;
        const viewportLeft = this.carouselViewPort.scrollLeft;
        const atEnd = Math.ceil(viewportLeft + this.carouselViewPort.clientWidth) >= this.carouselViewPort.scrollWidth - 1;
        if (atEnd) {
            this._currentAutoIndex = Math.max(0, this.carouselItems.length - 2);
            return;
        }
        let chosenIndex = 0;
        if (this._isCenteredMode()) {
            const viewportCenter = viewportLeft + this.carouselViewPort.clientWidth / 2;
            let closestDist = Number.POSITIVE_INFINITY;
            this.carouselItems.forEach((item, idx) => {
                const itemCenter = item.offsetLeft + item.offsetWidth / 2;
                const dist = Math.abs(itemCenter - viewportCenter);
                if (dist < closestDist) { closestDist = dist; chosenIndex = idx; }
            });
        } else {
            let closestDistance = Number.POSITIVE_INFINITY;
            this.carouselItems.forEach((item, idx) => {
                const dist = Math.abs(item.offsetLeft - viewportLeft);
                if (dist < closestDistance) { closestDistance = dist; chosenIndex = idx; }
            });
        }
        this._currentAutoIndex = chosenIndex - 1;
    }

    _attachResizeObserver() {
        if (!this.carouselViewPort) return;
        if (this._resizeObserver) this._resizeObserver.disconnect();
        this._resizeObserver = new ResizeObserver(() => { this._scheduleRecalc(); });
        this._resizeObserver.observe(this.carouselViewPort);
    }

    _getVisibleIndex(nextButton) {
        if (!this.carouselItems || this.carouselItems.length === 0) return -1;
        const viewportLeft = this.carouselViewPort.scrollLeft;
        const atEnd = Math.ceil(viewportLeft + this.carouselViewPort.clientWidth) >= this.carouselViewPort.scrollWidth - 1;
        if (atEnd && nextButton) return this.carouselItems.length - 1;
        if (this._isCenteredMode()) {
            if (this.carouselViewPort.scrollLeft === 0 && !nextButton) return this.carouselItems.length - 1;
            const viewportCenter = viewportLeft + this.carouselViewPort.clientWidth / 2;
            let closestIndex = 0, closestDistance = Number.POSITIVE_INFINITY;
            this.carouselItems.forEach((item, idx) => {
                const itemCenter = item.offsetLeft + item.offsetWidth / 2;
                const dist = Math.abs(itemCenter - viewportCenter);
                if (dist < closestDistance) { closestDistance = dist; closestIndex = idx; }
            });
            return closestIndex;
        }
        let closestIndex = 0, closestDistance = Number.POSITIVE_INFINITY;
        this.carouselItems.forEach((item, idx) => {
            const dist = Math.abs(item.offsetLeft - viewportLeft);
            if (dist < closestDistance) { closestDistance = dist; closestIndex = idx; }
        });
        return closestIndex;
    }

    _goToIndex(index) {
        if (!this.carouselItems || this.carouselItems.length === 0) return;
        const count = this.carouselItems.length;
        if (index < 0) index = (index + count) % count;
        if (index >= count) index = index % count;
        const target = this.carouselItems[index];
        if (!target) return;
        this._scrollToItem(target);
        this._currentAutoIndex = index;
    }

    _detachNavButtonEvents() {
        if (this._prevButton && this._onPrevClick) this._prevButton.removeEventListener('click', this._onPrevClick);
        if (this._nextButton && this._onNextClick) this._nextButton.removeEventListener('click', this._onNextClick);
        this._prevButton = this._nextButton = null;
        this._onPrevClick = this._onNextClick = null;
    }

    _attachNavButtonEvents() {
        this._detachNavButtonEvents();
        this._prevButton = this.querySelector(':scope > .tnt-carousel-prev-button');
        this._nextButton = this.querySelector(':scope > .tnt-carousel-next-button');
        if (!this._prevButton && !this._nextButton) return;
        this._onPrevClick = (e) => {
            e.stopPropagation();
            if (!this.carouselItems || this.carouselItems.length === 0) return;
            this._stopAutoPlay();
            const visible = this._getVisibleIndex();
            const target = (visible - 1 + this.carouselItems.length) % this.carouselItems.length;
            this._goToIndex(target);
            this._startAutoPlay();
        };
        this._onNextClick = (e) => {
            e.stopPropagation();
            if (!this.carouselItems || this.carouselItems.length === 0) return;
            this._stopAutoPlay();
            const visible = this._getVisibleIndex(true);
            const target = (visible + 1) % this.carouselItems.length;
            this._goToIndex(target);
            this._startAutoPlay();
        };
        if (this._prevButton) this._prevButton.addEventListener('click', this._onPrevClick);
        if (this._nextButton) this._nextButton.addEventListener('click', this._onNextClick);
    }

    onUpdate() {
        const viewPort = this.querySelector(':scope > .tnt-carousel-viewport');
        if (!viewPort) return; // nothing to do yet
        const childCountChanged = viewPort.children.length !== this._lastChildCount;
        const viewportChanged = viewPort !== this.carouselViewPort;
        this.carouselViewPort = viewPort;
        if (viewportChanged || childCountChanged) {
            this.carouselItems = this.carouselViewPort.querySelectorAll(':scope > tnt-carousel-item');
            this._lastChildCount = this.carouselViewPort.children.length;
            this.carouselViewPort.style.setProperty('--tnt-carousel-item-gap', `${gapSize}px`);
            this._recalculateChildWidths();
        }
        if (viewportChanged && this._scrollListener) {
            viewPort.removeEventListener('scroll', this._scrollListener);
            this._scrollListener = null;
        }
        if (!this._scrollListener) {
            this._scrollListener = () => {
                if (this._scrollRafId) return;
                this._scrollRafId = requestAnimationFrame(() => {
                    this._scrollRafId = null;
                    this._recalculateChildWidths();
                });
            };
            this.carouselViewPort.addEventListener('scroll', this._scrollListener, { passive: true });
        }
        this._updateDraggingFromAttribute();
        this._attachResizeObserver();
        this._attachNavButtonEvents();
        this._configureAutoPlayFromAttribute();
    }
}

export function onLoad(element, dotNetRef) {
    if (!customElements.get('tnt-carousel')) {
        customElements.define('tnt-carousel', NTCarousel);
    }
}

export function onUpdate(element, dotNetRef) { if (element && element instanceof NTCarousel) element.onUpdate(); }
export function onDispose(element, dotNetRef) { }