export class TnTAnimation extends HTMLElement {
    static observedAttributes = [NTComponents.customAttribute];
    constructor() {
        super();
        this.intersectionObserver = null;
    }


    disconnectedCallback() {
        this.intersectionObserver.unobserve(this);
        this.intersectionObserver.disconnect();
    }

    attributeChangedCallback(name, oldValue, newValue) {
        if (name === NTComponents.customAttribute) {
            if (!this.intersectionObserver) {
                var threshold = parseFloat(this.getAttribute('tnt-threshold')) || 0.5;
                const options = {
                    root: this.parentElement,
                    rootMargin: '0px',
                    threshold: threshold
                }
                this.intersectionObserver = new IntersectionObserver(this.intersected, options);
                this.intersectionObserver.observe(this);
            }
        }
    }

    intersected(entries) {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.classList.add('tnt-animation-intersected');
                entry.target.intersectionObserver.unobserve(entry.target);
                console.log(`Ratio: ${entry.intersectionRatio}`)
            }
        });
    }
}


export function onLoad(element, dotnNetRef) {
    if (!customElements.get('tnt-animation')) {
        customElements.define('tnt-animation', TnTAnimation);
    }
}

export function onUpdate(element, dotnNetRef) {
}

export function onDispose(element, dotnNetRef) {
}
