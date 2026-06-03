import { enhanceAll, onDispose, onLoad, onUpdate } from '../NTAnimation.razor.js';

class MockIntersectionObserver {
    static instances = [];

    constructor(callback, options) {
        this.callback = callback;
        this.options = options;
        this.elements = new Set();
        MockIntersectionObserver.instances.push(this);
    }

    observe(element) {
        this.elements.add(element);
    }

    unobserve(element) {
        this.elements.delete(element);
    }

    disconnect() {
        this.elements.clear();
    }

    trigger(isIntersecting = true) {
        this.callback(Array.from(this.elements).map(element => ({
            isIntersecting,
            target: element
        })));
    }
}

describe('NTAnimation runtime', () => {
    beforeEach(() => {
        document.body.innerHTML = '';
        MockIntersectionObserver.instances.length = 0;
        global.IntersectionObserver = MockIntersectionObserver;
        Object.defineProperty(window, 'innerHeight', { configurable: true, value: 100 });
        Object.defineProperty(window, 'innerWidth', { configurable: true, value: 100 });
    });

    test('enhanceAll observes animation wrappers', () => {
        const element = document.createElement('div');
        element.dataset.ntAnimation = 'true';
        element.getBoundingClientRect = () => ({ bottom: 200, left: 0, right: 50, top: 150 });
        document.body.appendChild(element);

        enhanceAll(document);

        expect(element.classList.contains('nt-animation-enhanced')).toBe(true);
        expect(MockIntersectionObserver.instances).toHaveLength(1);
        expect(MockIntersectionObserver.instances[0].elements.has(element)).toBe(true);
    });

    test('intersecting element becomes visible and unobserves by default', () => {
        const element = document.createElement('div');
        element.dataset.ntAnimation = 'true';
        element.getBoundingClientRect = () => ({ bottom: 200, left: 0, right: 50, top: 150 });
        document.body.appendChild(element);

        enhanceAll(document);
        MockIntersectionObserver.instances[0].trigger(true);

        expect(element.classList.contains('nt-animation-visible')).toBe(true);
        expect(MockIntersectionObserver.instances[0].elements.has(element)).toBe(false);
    });

    test('animate out removes visible state after leaving viewport', () => {
        const element = document.createElement('div');
        element.dataset.ntAnimation = 'true';
        element.dataset.ntAnimationAnimateOut = 'true';
        element.getBoundingClientRect = () => ({ bottom: 200, left: 0, right: 50, top: 150 });
        document.body.appendChild(element);

        enhanceAll(document);
        const observer = MockIntersectionObserver.instances[0];
        observer.trigger(true);
        observer.trigger(false);

        expect(element.classList.contains('nt-animation-visible')).toBe(false);
        expect(element.classList.contains('nt-animation-exiting')).toBe(true);
    });

    test('uses configured threshold and root margin', () => {
        const element = document.createElement('div');
        element.dataset.ntAnimation = 'true';
        element.dataset.ntAnimationThreshold = '0.75';
        element.dataset.ntAnimationRootMargin = '0px 0px -10% 0px';
        element.getBoundingClientRect = () => ({ bottom: 200, left: 0, right: 50, top: 150 });
        document.body.appendChild(element);

        onLoad(element);

        expect(MockIntersectionObserver.instances[0].options.threshold).toBe(0.75);
        expect(MockIntersectionObserver.instances[0].options.rootMargin).toBe('0px 0px -10% 0px');
    });

    test('onUpdate resynchronizes an existing element', () => {
        const element = document.createElement('div');
        element.dataset.ntAnimation = 'true';
        element.getBoundingClientRect = () => ({ bottom: 200, left: 0, right: 50, top: 150 });
        document.body.appendChild(element);

        onUpdate(element);
        onUpdate(element);

        expect(MockIntersectionObserver.instances).toHaveLength(2);
        expect(MockIntersectionObserver.instances[0].elements.size).toBe(0);
        expect(MockIntersectionObserver.instances[1].elements.has(element)).toBe(true);
    });

    test('onDispose disconnects observer', () => {
        const element = document.createElement('div');
        element.dataset.ntAnimation = 'true';
        element.getBoundingClientRect = () => ({ bottom: 200, left: 0, right: 50, top: 150 });
        document.body.appendChild(element);

        onUpdate(element);
        const observer = MockIntersectionObserver.instances[0];

        onDispose(element);

        expect(observer.elements.size).toBe(0);
        expect(element.__ntAnimationState).toBeUndefined();
    });
});
