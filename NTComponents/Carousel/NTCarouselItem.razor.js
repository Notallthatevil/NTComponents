const carouselsByIdentifier = new Map();

export class NTCarouselItem extends HTMLElement {
    static observedAttributes = [NTComponents.customAttribute];
    constructor() {
        super();
        this.naturalWidth = null;
        this.backgroundImageWidth = '100%';
        this.contentContainer = null;
        this.carouselContainer = null;
        this._lastBg = null;
    }

    disconnectedCallback() {
        const identifier = this.getAttribute(NTComponents.customAttribute);
        if (carouselsByIdentifier.get(identifier)) {
            carouselsByIdentifier.delete(identifier);
        }
    }

    attributeChangedCallback(name, oldValue, newValue) {
        if (name === NTComponents.customAttribute && oldValue != newValue) {
            if (carouselsByIdentifier.get(oldValue)) carouselsByIdentifier.delete(oldValue);
            carouselsByIdentifier.set(newValue, this);
            this.onUpdate();
        }
    }

    calculateNaturalWidth() {
        if (!this.contentContainer) return;
        // Hero path
        if (this.classList.contains('tnt-carousel-hero')) {
            // Maintain original semantics: initial style width set to 80%, backgroundImageWidth forced to 100%
            this.style.width = '80%';
            this.backgroundImageWidth = '100%';
            this.naturalWidth = '100%';
            this.updateItemWidth();
            return;
        }
        const bgImage = this.contentContainer.style.backgroundImage;
        if (bgImage && bgImage !== 'none') {
            if (bgImage === this._lastBg && this.naturalWidth) { // already processed
                this.updateItemWidth();
                return;
            }
            this._lastBg = bgImage;
            const match = bgImage.match(/url\(["']?(.*?)["']?\)/);
            if (match && match[1]) {
                const img = new window.Image();
                img.onload = () => {
                    this.naturalWidth = img.naturalWidth;
                    this.backgroundImageWidth = `${this.naturalWidth}px`;
                    this.style.setProperty('--tnt-carousel-item-bg-width', this.backgroundImageWidth);
                    this.style.width = this.backgroundImageWidth;
                    this.updateItemWidth();
                };
                img.src = match[1];
                return;
            }
        }
        // Fallback path (no background)
        this.style.width = '80%';
        this.updateItemWidth();
    }

    updateItemWidth() {
        if (!this.carouselContainer || !this.contentContainer) {
            // Attempt lazy resolution once
            this.onUpdate();
            if (!this.carouselContainer || !this.contentContainer) return;
        }
        const parentRect = this.carouselContainer.getBoundingClientRect();
        const itemRect = this.getBoundingClientRect();
        let newWidth = itemRect.width;
        if (itemRect.width > parentRect.width) {
            this.style.width = `${parentRect.width}px`;
            this.contentContainer.style.width = '100%';
            return;
        }
        if (itemRect.right > parentRect.right) {
            if (itemRect.left > parentRect.right) newWidth = 0; else newWidth = parentRect.right - itemRect.left;
            this.contentContainer.style.width = `${newWidth}px`;
            this.style.justifyContent = null;
            return;
        }
        if (itemRect.left < parentRect.left) {
            if (itemRect.right < parentRect.left) newWidth = 0; else newWidth = itemRect.right - parentRect.left;
            this.contentContainer.style.width = `${newWidth}px`;
            this.style.justifyContent = 'end';
            this.style.width = this.backgroundImageWidth; // preserve original logic
            return;
        }
        this.contentContainer.style.width = `${newWidth}px`;
        this.style.width = `${newWidth}px`;
        this.style.justifyContent = null;
    }

    onUpdate() {
        // Cache queries only when missing
        if (!this.contentContainer) this.contentContainer = this.querySelector(':scope > .tnt-carousel-item-content');
        if (!this.carouselContainer) this.carouselContainer = this.closest('.tnt-carousel-viewport');
        if (!this.contentContainer || !this.carouselContainer) return;
        this.calculateNaturalWidth();
    }
}
export function onLoad(element, dotNetRef) {
    if (!customElements.get('tnt-carousel-item')) {
        customElements.define('tnt-carousel-item', NTCarouselItem);
    }
}
export function onUpdate(element, dotNetRef) { if (element && element instanceof NTCarouselItem) element.onUpdate(); }
export function onDispose(element, dotNetRef) { }