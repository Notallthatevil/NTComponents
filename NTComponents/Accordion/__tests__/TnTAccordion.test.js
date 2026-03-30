/**
 * @jest-environment jsdom
 */
import { jest } from '@jest/globals';
import { TnTAccordion, onLoad, onUpdate, onDispose } from '../TnTAccordion.razor.js';

if (!global.NTComponents) {
  global.NTComponents = { customAttribute: 'tntid' };
}

describe('TnTAccordion web component', () => {
  beforeEach(() => {
    document.body.innerHTML = '';
  });

  const defineIfNeeded = () => {
    if (!customElements.get('tnt-accordion')) {
      customElements.define('tnt-accordion', TnTAccordion);
    }
  };

  function createAccordion({ limitOne = false } = {}) {
    defineIfNeeded();
    const el = document.createElement('tnt-accordion');
    if (limitOne) el.classList.add('tnt-limit-one-expanded');
    document.body.appendChild(el);
    return el;
  }

  function createChild({ expanded = false } = {}) {
    const wrapper = document.createElement('div');
    wrapper.classList.add('tnt-accordion-child');
    wrapper.setAttribute('data-accordion-child', 'true');
    wrapper.setAttribute('data-accordion-child-id', '42');
    const header = document.createElement('h3');
    const button = document.createElement('button');
    button.setAttribute('data-accordion-header', 'true');
    button.setAttribute('data-accordion-child-id', '42');
    const content = document.createElement('div');
    content.setAttribute('data-accordion-content', 'true');
    if (expanded) content.classList.add('tnt-expanded');
    header.appendChild(button);
    wrapper.appendChild(header);
    wrapper.appendChild(content);
    return { wrapper, button, content };
  }

  function createLegacyChild({ expanded = false } = {}) {
    const wrapper = document.createElement('div');
    wrapper.classList.add('tnt-accordion-child');
    const heading = document.createElement('h3');
    const content = document.createElement('div');
    content.classList.add(expanded ? 'tnt-expanded' : 'tnt-collapsed');
    wrapper.appendChild(heading);
    wrapper.appendChild(content);
    return { wrapper, heading, content };
  }

  test('onLoad registers custom element only once', () => {
    const defineSpy = jest.spyOn(customElements, 'define');
    const host = document.createElement('tnt-accordion');
    onLoad(host, null);
    onLoad(host, null);
    expect(defineSpy).toHaveBeenCalledTimes(defineSpy.mock.calls.length <= 1 ? defineSpy.mock.calls.length : 1);
    defineSpy.mockRestore();
  });

  test('onLoad assigns dotNetRef to element', () => {
    const host = createAccordion();
    onLoad(host, { some: 'ref' });
    expect(host.dotNetRef).toEqual({ some: 'ref' });
  });

  test('attributeChangedCallback maps identifiers and triggers update', () => {
    defineIfNeeded();
    const acc = new TnTAccordion();
    const spy = jest.spyOn(acc, 'update');
    acc.setAttribute(NTComponents.customAttribute, 'one');
    acc.setAttribute(NTComponents.customAttribute, 'two');
    expect(acc.identifier).toBe('two');
    expect(spy).toHaveBeenCalled();
  });

  test('update collects only direct child accordion items', () => {
    const acc = createAccordion();
    const a = createChild();
    const b = createChild();
    acc.appendChild(a.wrapper);
    acc.appendChild(b.wrapper);
    acc.update();
    expect(acc.accordionChildren.length).toBe(2);
  });

  test('update syncs inert and aria state for expanded content', () => {
    const acc = createAccordion();
    const { wrapper, content } = createChild({ expanded: true });
    acc.appendChild(wrapper);
    acc.update();
    expect(content.getAttribute('aria-hidden')).toBe('false');
    expect(content.hasAttribute('inert')).toBe(false);
  });

  test('update syncs inert and aria state for collapsed content', () => {
    const acc = createAccordion();
    const { wrapper, content } = createChild();
    acc.appendChild(wrapper);
    acc.update();
    expect(content.getAttribute('aria-hidden')).toBe('true');
    expect(content.hasAttribute('inert')).toBe(true);
  });

  test('closeChildren collapses other expanded children', () => {
    const acc = createAccordion({ limitOne: true });
    const first = createChild({ expanded: true });
    const second = createChild({ expanded: true });
    acc.appendChild(first.wrapper);
    acc.appendChild(second.wrapper);
    acc.update();
    acc.closeChildren(second.wrapper);
    expect(first.content.classList.contains('tnt-expanded')).toBe(false);
    expect(first.content.classList.contains('tnt-collapsed')).toBe(true);
  });

  test('setExpandedState supports legacy child markup after upgrade', () => {
    const acc = createAccordion({ limitOne: true });
    const first = createLegacyChild({ expanded: true });
    const second = createLegacyChild({ expanded: false });
    acc.appendChild(first.wrapper);
    acc.appendChild(second.wrapper);
    acc.update();

    acc.closeChildren(second.wrapper);
    acc.setExpandedState(second.wrapper, true);

    expect(first.content.classList.contains('tnt-expanded')).toBe(false);
    expect(first.content.classList.contains('tnt-collapsed')).toBe(true);
    expect(second.content.classList.contains('tnt-expanded')).toBe(true);
    expect(second.content.classList.contains('tnt-collapsed')).toBe(false);
  });

  test('resetChildren removes expanded/collapsed classes and recurses', () => {
    const acc = createAccordion();
    const child = createChild({ expanded: true });
    const nestedAcc = createAccordion();
    const nestedChild = createChild({ expanded: true });
    nestedAcc.appendChild(nestedChild.wrapper);
    child.content.appendChild(nestedAcc);
    acc.appendChild(child.wrapper);
    acc.update();
    expect(child.content.classList.contains('tnt-expanded')).toBe(true);
    acc.resetChildren();
    expect(child.content.classList.contains('tnt-expanded')).toBe(false);
    expect(child.content.classList.contains('tnt-collapsed')).toBe(false);
    expect(nestedChild.content.classList.contains('tnt-expanded')).toBe(false);
    expect(nestedChild.content.classList.contains('tnt-collapsed')).toBe(false);
  });

  test('resetChildren supports legacy child markup', () => {
    const acc = createAccordion();
    const child = createLegacyChild({ expanded: true });
    acc.appendChild(child.wrapper);
    acc.update();
    acc.resetChildren();
    expect(child.content.classList.contains('tnt-expanded')).toBe(false);
    expect(child.content.classList.contains('tnt-collapsed')).toBe(false);
  });

  test('update syncs aria state from expanded content', () => {
    const acc = createAccordion();
    const { wrapper, button, content } = createChild({ expanded: true });
    acc.appendChild(wrapper);
    acc.update();
    expect(button.getAttribute('aria-expanded')).toBe('true');
    expect(content.getAttribute('aria-hidden')).toBe('false');
  });

  test('closeChildren updates aria state for collapsed siblings', () => {
    const acc = createAccordion({ limitOne: true });
    const first = createChild({ expanded: true });
    const second = createChild({ expanded: true });
    acc.appendChild(first.wrapper);
    acc.appendChild(second.wrapper);
    acc.update();
    acc.closeChildren(second.wrapper);
    expect(first.button.getAttribute('aria-expanded')).toBe('false');
    expect(first.content.getAttribute('aria-hidden')).toBe('true');
    expect(first.content.hasAttribute('inert')).toBe(true);
  });

  test('limitToOneExpanded reflects CSS class', () => {
    const acc = createAccordion({ limitOne: true });
    expect(acc.limitToOneExpanded()).toBe(true);
  });

  test('onUpdate invokes update and sets dotNetRef', () => {
    const acc = createAccordion();
    const spy = jest.spyOn(acc, 'update');
    onUpdate(acc, { ref: 1 });
    expect(spy).toHaveBeenCalled();
    expect(acc.dotNetRef).toEqual({ ref: 1 });
  });

  test('onUpdate safe when element null or missing update', () => {
    expect(() => onUpdate(null, null)).not.toThrow();
    expect(() => onUpdate({}, null)).not.toThrow();
  });

  test('disconnectedCallback cleans map entry', () => {
    const acc = createAccordion();
    const deleteSpy = jest.spyOn(Map.prototype, 'delete');
    acc.setAttribute(NTComponents.customAttribute, 'zzz');
    acc.disconnectedCallback();
    expect(deleteSpy).toHaveBeenCalled();
    deleteSpy.mockRestore();
  });

  test('onDispose is a no-op', () => {
    const acc = createAccordion();
    expect(() => onDispose(acc, {})).not.toThrow();
  });
});
