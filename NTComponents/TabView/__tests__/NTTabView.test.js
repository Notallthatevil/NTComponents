/**
 * @jest-environment jsdom
 */
import { jest } from '@jest/globals';
import { __testHooks, onDispose, onLoad, onUpdate } from '../NTTabView.razor.js';

const originalLocation = window.location.href;

class ResizeObserverMock {
  constructor(callback) {
    this.callback = callback;
    ResizeObserverMock.instances.push(this);
  }

  observe = jest.fn();
  disconnect = jest.fn();

  static instances = [];
}

describe('NTTabView page-script module', () => {
  beforeEach(() => {
    document.body.innerHTML = '';
    window.history.replaceState(null, '', originalLocation);
    global.ResizeObserver = ResizeObserverMock;
    ResizeObserverMock.instances = [];
    global.NTComponents = { registerButtonInteraction: jest.fn() };
    global.matchMedia = jest.fn().mockReturnValue({ matches: false });
    global.requestAnimationFrame = callback => {
      callback();
      return 1;
    };
    global.cancelAnimationFrame = jest.fn();
  });

  function createTabView() {
    document.body.innerHTML = `
      <nt-tab-view class="nt-tab-view" data-nt-tab-view="true" data-nt-tab-view-name="details" data-nt-tab-query-parameter="details" data-nt-tab-update-query="true">
        <div class="nt-tab-view-header">
          <div class="nt-tab-view-tablist" role="tablist">
            <button type="button" class="nt-tab-view-tab nt-tab-view-tab-selected" id="tab-overview" role="tab" aria-selected="true" aria-controls="panel-overview" tabindex="0" data-nt-tab-button="true" data-nt-tab-value="overview">
              <span class="nt-tab-view-content nt-tab-view-indicator-target">Overview</span>
            </button>
            <button type="button" class="nt-tab-view-tab" id="tab-specs" role="tab" aria-selected="false" aria-controls="panel-specs" tabindex="-1" data-nt-tab-button="true" data-nt-tab-value="specs">
              <span class="nt-tab-view-content nt-tab-view-indicator-target">Specs</span>
            </button>
            <span class="nt-tab-view-active-indicator" data-nt-tab-indicator></span>
          </div>
        </div>
        <div class="nt-tab-view-panels">
          <section class="nt-tab-view-panel nt-tab-view-panel-selected" id="panel-overview" role="tabpanel" data-nt-tab-panel="true" data-nt-tab-value="overview">Overview content</section>
          <section class="nt-tab-view-panel" id="panel-specs" role="tabpanel" data-nt-tab-panel="true" data-nt-tab-value="specs" hidden>Specs content</section>
        </div>
      </nt-tab-view>
      <tnt-page-script></tnt-page-script>`;

    return document.querySelector('nt-tab-view');
  }

  test('onLoad resolves a page-script element to the preceding tab view', () => {
    const tabView = createTabView();
    const pageScript = document.querySelector('tnt-page-script');

    onLoad(pageScript);

    expect(tabView.__ntTabViewState).toBeDefined();
    expect(global.NTComponents.registerButtonInteraction).toHaveBeenCalledTimes(2);
  });

  test('query parameter selects matching tab on load', () => {
    const tabView = createTabView();
    window.history.replaceState(null, '', `${originalLocation.split('?')[0]}?details=specs`);

    onLoad(tabView);

    expect(tabView.querySelector('[data-nt-tab-value="overview"]').getAttribute('aria-selected')).toBe('false');
    expect(tabView.querySelector('[data-nt-tab-value="specs"]').getAttribute('aria-selected')).toBe('true');
    expect(tabView.querySelector('#panel-overview').hidden).toBe(true);
    expect(tabView.querySelector('#panel-specs').hidden).toBe(false);
  });

  test('click selects tab and writes named query parameter', () => {
    const tabView = createTabView();
    onLoad(tabView);

    tabView.querySelector('[data-nt-tab-value="specs"]').click();

    expect(new URL(window.location.href).searchParams.get('details')).toBe('specs');
    expect(tabView.querySelector('#panel-specs').hidden).toBe(false);
  });

  test('keyboard arrows move focus without wrapping and enter selects focused tab', () => {
    const tabView = createTabView();
    onLoad(tabView);
    const overview = tabView.querySelector('[data-nt-tab-value="overview"]');
    const specs = tabView.querySelector('[data-nt-tab-value="specs"]');

    overview.focus();
    overview.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowRight', bubbles: true, cancelable: true }));
    expect(document.activeElement).toBe(specs);
    expect(specs.getAttribute('aria-selected')).toBe('false');
    expect(overview.tabIndex).toBe(-1);
    expect(specs.tabIndex).toBe(0);

    specs.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true, cancelable: true }));
    expect(specs.getAttribute('aria-selected')).toBe('true');

    specs.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowRight', bubbles: true, cancelable: true }));
    expect(document.activeElement).toBe(specs);
  });

  test('home and end keep a single roving tab stop', () => {
    const tabView = createTabView();
    onLoad(tabView);
    const overview = tabView.querySelector('[data-nt-tab-value="overview"]');
    const specs = tabView.querySelector('[data-nt-tab-value="specs"]');

    specs.focus();
    specs.dispatchEvent(new KeyboardEvent('keydown', { key: 'Home', bubbles: true, cancelable: true }));
    expect(document.activeElement).toBe(overview);
    expect(overview.tabIndex).toBe(0);
    expect(specs.tabIndex).toBe(-1);

    overview.dispatchEvent(new KeyboardEvent('keydown', { key: 'End', bubbles: true, cancelable: true }));
    expect(document.activeElement).toBe(specs);
    expect(overview.tabIndex).toBe(-1);
    expect(specs.tabIndex).toBe(0);
  });

  test('reduced motion uses auto scroll behavior', () => {
    const tabView = createTabView();
    global.matchMedia = jest.fn().mockReturnValue({ matches: true });
    const specs = tabView.querySelector('[data-nt-tab-value="specs"]');
    specs.scrollIntoView = jest.fn();

    onLoad(tabView);
    specs.click();

    expect(specs.scrollIntoView).toHaveBeenCalledWith({ behavior: 'auto', block: 'nearest', inline: 'nearest' });
  });

  test('updates preserve current selection instead of reapplying query value', () => {
    const tabView = createTabView();
    window.history.replaceState(null, '', `${originalLocation.split('?')[0]}?details=specs`);
    onLoad(tabView);
    const overview = tabView.querySelector('[data-nt-tab-value="overview"]');
    const specs = tabView.querySelector('[data-nt-tab-value="specs"]');

    overview.setAttribute('aria-selected', 'true');
    overview.tabIndex = 0;
    specs.setAttribute('aria-selected', 'false');
    specs.tabIndex = -1;

    onUpdate(tabView);

    expect(overview.getAttribute('aria-selected')).toBe('true');
    expect(specs.getAttribute('aria-selected')).toBe('false');
  });

  test('updates rebind tablist listeners when the tablist element is replaced', () => {
    const tabView = createTabView();
    onLoad(tabView);
    const originalTabList = tabView.querySelector('.nt-tab-view-tablist');
    const replacementTabList = originalTabList.cloneNode(true);
    originalTabList.replaceWith(replacementTabList);
    const specs = replacementTabList.querySelector('[data-nt-tab-value="specs"]');

    onUpdate(tabView);
    specs.click();

    expect(specs.getAttribute('aria-selected')).toBe('true');
    expect(tabView.__ntTabViewState.tabList).toBe(replacementTabList);
  });

  test('indicator uses active target geometry relative to the scroll container', () => {
    const tabView = createTabView();
    const tabList = tabView.querySelector('.nt-tab-view-tablist');
    const target = tabView.querySelector('[data-nt-tab-value="overview"] .nt-tab-view-indicator-target');

    Object.defineProperty(tabList, 'scrollLeft', { value: 12, configurable: true });
    tabList.getBoundingClientRect = () => ({ left: 10, width: 300 });
    target.getBoundingClientRect = () => ({ left: 42, width: 56 });

    __testHooks.updateIndicator(tabView);

    expect(tabView.style.getPropertyValue('--nt-tab-view-active-indicator-x')).toBe('44px');
    expect(tabView.style.getPropertyValue('--nt-tab-view-active-indicator-width')).toBe('56px');
    expect(tabView.querySelector('[data-nt-tab-indicator]').style.transform).toBe('');
    expect(tabView.querySelector('[data-nt-tab-indicator]').style.inlineSize).toBe('');
  });

  test('secondary indicator uses the full selected tab width', () => {
    const tabView = createTabView();
    tabView.classList.add('nt-tab-view-secondary');
    const tabList = tabView.querySelector('.nt-tab-view-tablist');
    const tab = tabView.querySelector('[data-nt-tab-value="overview"]');
    const target = tab.querySelector('.nt-tab-view-indicator-target');

    Object.defineProperty(tabList, 'scrollLeft', { value: 4, configurable: true });
    tabList.getBoundingClientRect = () => ({ left: 10, width: 300 });
    tab.getBoundingClientRect = () => ({ left: 30, width: 120 });
    target.getBoundingClientRect = () => ({ left: 60, width: 48 });

    __testHooks.updateIndicator(tabView);

    expect(tabView.style.getPropertyValue('--nt-tab-view-active-indicator-x')).toBe('24px');
    expect(tabView.style.getPropertyValue('--nt-tab-view-active-indicator-width')).toBe('120px');
    expect(tabView.querySelector('[data-nt-tab-indicator]').style.transform).toBe('');
    expect(tabView.querySelector('[data-nt-tab-indicator]').style.inlineSize).toBe('');
  });

  test('onUpdate keeps existing state and onDispose removes listeners and observers', () => {
    const tabView = createTabView();
    onLoad(tabView);
    const state = tabView.__ntTabViewState;

    onUpdate(tabView);
    onDispose(tabView);

    expect(state.resizeObserver.disconnect).toHaveBeenCalled();
    expect(tabView.__ntTabViewState).toBeUndefined();
  });

  test('dispose can resolve the tab view after page script element is detached', () => {
    const tabView = createTabView();
    const pageScript = document.querySelector('tnt-page-script');
    onLoad(pageScript);
    const state = tabView.__ntTabViewState;

    pageScript.remove();
    onDispose(pageScript);

    expect(state.resizeObserver.disconnect).toHaveBeenCalled();
    expect(tabView.__ntTabViewState).toBeUndefined();
  });
});
