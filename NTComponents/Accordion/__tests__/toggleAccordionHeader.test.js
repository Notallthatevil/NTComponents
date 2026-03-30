/**
 * @jest-environment jsdom
 */
import '../../wwwroot/NTComponents.lib.module.js';

describe('toggleAccordionHeader', () => {
    let toggleAccordionHeader;

    beforeAll(() => {
        toggleAccordionHeader = window.NTComponents.toggleAccordionHeader;
    });

    beforeEach(() => {
        document.body.innerHTML = '';
    });

    /**
     * Builds a minimal but realistic accordion DOM tree.
     * Returns references to the key elements so tests can inspect state.
     */
    function createDOM({ expanded = false } = {}) {
        const accordion = document.createElement('tnt-accordion');

        const child = document.createElement('div');
        child.setAttribute('data-accordion-child', 'true');
        child.setAttribute('data-accordion-child-id', '1');

        const h3 = document.createElement('h3');
        const headerButton = document.createElement('button');
        headerButton.setAttribute('data-accordion-header', 'true');
        headerButton.setAttribute('data-accordion-child-id', '1');
        h3.appendChild(headerButton);

        const content = document.createElement('div');
        content.setAttribute('data-accordion-content', 'true');
        content.classList.add(expanded ? 'tnt-expanded' : 'tnt-collapsed');

        child.appendChild(h3);
        child.appendChild(content);
        accordion.appendChild(child);
        document.body.appendChild(accordion);

        return { accordion, child, headerButton, content };
    }

    /** Simulates the native event shape that toggleAccordionHeader receives via onclick="...". */
    function fakeEvent(target, currentTarget) {
        return { target, currentTarget };
    }

    // ---------------------------------------------------------------------------
    // Guard: nested interactive elements must NOT fire the accordion toggle
    // ---------------------------------------------------------------------------

    test('WithNestedButton_DoesNotToggle', () => {
        // Arrange
        const { headerButton, content } = createDOM();
        const nested = document.createElement('button');
        headerButton.appendChild(nested);

        // Act
        toggleAccordionHeader(fakeEvent(nested, headerButton));

        // Assert — content classes must be unchanged
        expect(content.classList.contains('tnt-expanded')).toBe(false);
        expect(content.classList.contains('tnt-collapsed')).toBe(true);
    });

    test('WithNestedAnchor_DoesNotToggle', () => {
        // Arrange
        const { headerButton, content } = createDOM();
        const nested = document.createElement('a');
        headerButton.appendChild(nested);

        // Act
        toggleAccordionHeader(fakeEvent(nested, headerButton));

        // Assert
        expect(content.classList.contains('tnt-expanded')).toBe(false);
    });

    test('WithNestedInput_DoesNotToggle', () => {
        // Arrange
        const { headerButton, content } = createDOM();
        const nested = document.createElement('input');
        headerButton.appendChild(nested);

        // Act
        toggleAccordionHeader(fakeEvent(nested, headerButton));

        // Assert
        expect(content.classList.contains('tnt-expanded')).toBe(false);
    });

    test('WithNestedSelect_DoesNotToggle', () => {
        // Arrange
        const { headerButton, content } = createDOM();
        const nested = document.createElement('select');
        headerButton.appendChild(nested);

        // Act
        toggleAccordionHeader(fakeEvent(nested, headerButton));

        // Assert
        expect(content.classList.contains('tnt-expanded')).toBe(false);
    });

    test('WithNestedTextarea_DoesNotToggle', () => {
        // Arrange
        const { headerButton, content } = createDOM();
        const nested = document.createElement('textarea');
        headerButton.appendChild(nested);

        // Act
        toggleAccordionHeader(fakeEvent(nested, headerButton));

        // Assert
        expect(content.classList.contains('tnt-expanded')).toBe(false);
    });

    test('WithDeeplyNestedButton_DoesNotToggle', () => {
        // Arrange — a span inside a nested button (e.g., button's label text element)
        const { headerButton, content } = createDOM();
        const nested = document.createElement('button');
        const deepSpan = document.createElement('span');
        nested.appendChild(deepSpan);
        headerButton.appendChild(nested);

        // Act — target is the span, but its nearest interactive ancestor is the inner button
        toggleAccordionHeader(fakeEvent(deepSpan, headerButton));

        // Assert
        expect(content.classList.contains('tnt-expanded')).toBe(false);
    });

    // ---------------------------------------------------------------------------
    // Normal behavior: header button or non-interactive children SHOULD toggle
    // ---------------------------------------------------------------------------

    test('WithHeaderAsTarget_ExpandsCollapsedPanel', () => {
        // Arrange
        const { headerButton, content } = createDOM();

        // Act
        toggleAccordionHeader(fakeEvent(headerButton, headerButton));

        // Assert
        expect(content.classList.contains('tnt-expanded')).toBe(true);
        expect(content.classList.contains('tnt-collapsed')).toBe(false);
    });

    test('WithHeaderAsTarget_CollapsesExpandedPanel', () => {
        // Arrange
        const { headerButton, content } = createDOM({ expanded: true });

        // Act
        toggleAccordionHeader(fakeEvent(headerButton, headerButton));

        // Assert
        expect(content.classList.contains('tnt-expanded')).toBe(false);
        expect(content.classList.contains('tnt-collapsed')).toBe(true);
    });

    test('WithNonInteractiveLabelSpan_Toggles', () => {
        // Arrange — a plain <span> inside the header button (e.g., the label text wrapper)
        const { headerButton, content } = createDOM();
        const label = document.createElement('span');
        headerButton.appendChild(label);

        // Act — closest('button') from label walks up to headerButton itself
        toggleAccordionHeader(fakeEvent(label, headerButton));

        // Assert
        expect(content.classList.contains('tnt-expanded')).toBe(true);
    });

    test('WithHeaderAsTarget_UpdatesAriaExpandedToTrue', () => {
        // Arrange
        const { headerButton } = createDOM();

        // Act
        toggleAccordionHeader(fakeEvent(headerButton, headerButton));

        // Assert
        expect(headerButton.getAttribute('aria-expanded')).toBe('true');
    });

    test('WithHeaderAsTarget_UpdatesAriaExpandedToFalse', () => {
        // Arrange
        const { headerButton } = createDOM({ expanded: true });

        // Act
        toggleAccordionHeader(fakeEvent(headerButton, headerButton));

        // Assert
        expect(headerButton.getAttribute('aria-expanded')).toBe('false');
    });
});
