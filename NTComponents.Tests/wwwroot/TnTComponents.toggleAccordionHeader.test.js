import { jest } from '@jest/globals';
import '../../NTComponents/wwwroot/NTComponents.lib.module.js';
import { TnTAccordion } from '../../NTComponents/Accordion/TnTAccordion.razor.js';

describe('NTComponents.toggleAccordionHeader', () => {
   beforeEach(() => {
      document.body.innerHTML = '';
      jest.clearAllMocks();
   });

   const defineAccordionIfNeeded = () => {
      if (!customElements.get('tnt-accordion')) {
         customElements.define('tnt-accordion', TnTAccordion);
      }
   };

   test('toggles expanded class on accordion content', () => {
      const accordion = document.createElement('tnt-accordion');
      const itemContainer = document.createElement('div');
      itemContainer.className = 'tnt-accordion-child';
      itemContainer.setAttribute('data-accordion-child', 'true');
      itemContainer.setAttribute('data-accordion-child-id', '1');
      const heading = document.createElement('h3');
      const header = document.createElement('button');
      header.setAttribute('data-accordion-header', 'true');
      header.setAttribute('data-accordion-child-id', '1');
      const content = document.createElement('div');
      content.setAttribute('data-accordion-content', 'true');
      content.className = 'tnt-collapsed';

      heading.appendChild(header);
      // Build the structure: accordion > itemContainer > [heading, content]
      itemContainer.appendChild(heading);
      itemContainer.appendChild(content);
      accordion.appendChild(itemContainer);
      document.body.appendChild(accordion);

      // Mock accordion methods
      accordion.limitToOneExpanded = jest.fn(() => false);
      accordion.closeChildren = jest.fn();
      accordion.updateChild = jest.fn();
      accordion.resetChildren = jest.fn();

      const event = new Event('click', { bubbles: true });
      Object.defineProperty(event, 'target', { value: header, configurable: true });

      NTComponents.toggleAccordionHeader(event);

      // After the toggle, content should be expanded (class removed from collapsed, added to expanded)
      expect(content.classList.contains('tnt-expanded')).toBe(true);
      expect(header.getAttribute('aria-expanded')).toBe('true');
      expect(content.getAttribute('aria-hidden')).toBe('false');
      expect(accordion.updateChild).toHaveBeenCalledWith(content);
   });

   test('falls back to DOM inspection when accordion methods are unavailable', () => {
      const accordion = document.createElement('tnt-accordion');
      accordion.classList.add('tnt-limit-one-expanded');

      const firstItem = document.createElement('div');
      firstItem.className = 'tnt-accordion-child';
      firstItem.setAttribute('data-accordion-child', 'true');
      const firstHeading = document.createElement('h3');
      const firstHeader = document.createElement('button');
      firstHeader.setAttribute('data-accordion-header', 'true');
      const firstContent = document.createElement('div');
      firstContent.setAttribute('data-accordion-content', 'true');
      firstContent.className = 'tnt-expanded';
      firstHeading.appendChild(firstHeader);
      firstItem.appendChild(firstHeading);
      firstItem.appendChild(firstContent);

      const secondItem = document.createElement('div');
      secondItem.className = 'tnt-accordion-child';
      secondItem.setAttribute('data-accordion-child', 'true');
      const secondHeading = document.createElement('h3');
      const secondHeader = document.createElement('button');
      secondHeader.setAttribute('data-accordion-header', 'true');
      const secondContent = document.createElement('div');
      secondContent.setAttribute('data-accordion-content', 'true');
      secondContent.className = 'tnt-collapsed';
      secondHeading.appendChild(secondHeader);
      secondItem.appendChild(secondHeading);
      secondItem.appendChild(secondContent);

      accordion.appendChild(firstItem);
      accordion.appendChild(secondItem);
      document.body.appendChild(accordion);

      const event = new Event('click', { bubbles: true });
      Object.defineProperty(event, 'target', { value: secondHeader, configurable: true });

      expect(() => NTComponents.toggleAccordionHeader(event)).not.toThrow();
      expect(firstContent.classList.contains('tnt-collapsed')).toBe(true);
      expect(secondContent.classList.contains('tnt-expanded')).toBe(true);
   });

   test('supports upgraded accordion instances with legacy child markup', () => {
      defineAccordionIfNeeded();
      const accordion = document.createElement('tnt-accordion');
      accordion.classList.add('tnt-limit-one-expanded');

      const firstItem = document.createElement('div');
      firstItem.className = 'tnt-accordion-child';
      const firstHeader = document.createElement('h3');
      const firstContent = document.createElement('div');
      firstContent.className = 'tnt-expanded';
      firstItem.appendChild(firstHeader);
      firstItem.appendChild(firstContent);

      const secondItem = document.createElement('div');
      secondItem.className = 'tnt-accordion-child';
      const secondHeader = document.createElement('h3');
      const secondContent = document.createElement('div');
      secondContent.className = 'tnt-collapsed';
      secondItem.appendChild(secondHeader);
      secondItem.appendChild(secondContent);

      accordion.appendChild(firstItem);
      accordion.appendChild(secondItem);
      document.body.appendChild(accordion);
      accordion.update();

      const event = new Event('click', { bubbles: true });
      Object.defineProperty(event, 'target', { value: secondHeader, configurable: true });

      expect(() => NTComponents.toggleAccordionHeader(event)).not.toThrow();
      expect(firstContent.classList.contains('tnt-collapsed')).toBe(true);
      expect(secondContent.classList.contains('tnt-expanded')).toBe(true);
   });

   test('resets nested non-upgraded accordions when collapsing a parent', () => {
      const accordion = document.createElement('tnt-accordion');
      const itemContainer = document.createElement('div');
      itemContainer.className = 'tnt-accordion-child';
      itemContainer.setAttribute('data-accordion-child', 'true');
      const heading = document.createElement('h3');
      const header = document.createElement('button');
      header.setAttribute('data-accordion-header', 'true');
      const content = document.createElement('div');
      content.setAttribute('data-accordion-content', 'true');
      content.className = 'tnt-expanded';
      Object.defineProperty(content, 'scrollHeight', { value: 240, configurable: true });

      const nestedAccordion = document.createElement('tnt-accordion');
      const nestedItem = document.createElement('div');
      nestedItem.className = 'tnt-accordion-child';
      const nestedHeader = document.createElement('h3');
      const nestedContent = document.createElement('div');
      nestedContent.className = 'tnt-expanded';
      nestedItem.appendChild(nestedHeader);
      nestedItem.appendChild(nestedContent);
      nestedAccordion.appendChild(nestedItem);
      content.appendChild(nestedAccordion);

      heading.appendChild(header);
      itemContainer.appendChild(heading);
      itemContainer.appendChild(content);
      accordion.appendChild(itemContainer);
      document.body.appendChild(accordion);

      const event = new Event('click', { bubbles: true });
      Object.defineProperty(event, 'target', { value: header, configurable: true });

      NTComponents.toggleAccordionHeader(event);

      expect(content.classList.contains('tnt-collapsed')).toBe(true);
      expect(content.getAttribute('aria-hidden')).toBe('false');
      expect(content.style.getPropertyValue('--content-height')).toBe('240px');
      content.dispatchEvent(new Event('animationend'));
      expect(content.getAttribute('aria-hidden')).toBe('true');
      expect(nestedContent.classList.contains('tnt-expanded')).toBe(false);
      expect(nestedContent.classList.contains('tnt-collapsed')).toBe(false);
   });
});
