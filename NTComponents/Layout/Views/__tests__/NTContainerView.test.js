import { jest } from '@jest/globals';
import { onDispose, onLoad, onUpdate } from '../NTContainerView.razor.js';

function setScrollY(value) {
  Object.defineProperty(window, 'scrollY', {
    configurable: true,
    value
  });
}

function setHeadingTop(heading, absoluteTop) {
  heading.getBoundingClientRect = () => ({
    bottom: absoluteTop - window.scrollY + 32,
    height: 32,
    left: 0,
    right: 240,
    top: absoluteTop - window.scrollY,
    width: 240,
    x: 0,
    y: absoluteTop - window.scrollY,
    toJSON() { return this; }
  });
}

function setContainerMarkup(content, { wrapInScrollContainer = false } = {}) {
  const containerMarkup = `
    <div class="nt-container-view" id="docs" data-nt-container-view data-nt-container-view-quick-nav-enabled="true">
      <nav class="nt-container-view-quick-nav" aria-label="On this page" data-nt-container-view-quick-nav hidden>
        <div class="nt-container-view-quick-nav-title">On this page</div>
        <ol class="nt-container-view-quick-nav-list" data-nt-container-view-quick-nav-list></ol>
      </nav>
      ${content}
    </div>`;

  document.body.innerHTML = wrapInScrollContainer
    ? `<div class="scroll-host">${containerMarkup}</div>`
    : containerMarkup;

  return document.querySelector('[data-nt-container-view]');
}

describe('NTContainerView quick navigation runtime', () => {
  beforeEach(() => {
    setScrollY(0);
    window.matchMedia = jest.fn().mockReturnValue({ matches: false });
    window.getComputedStyle = jest.fn().mockReturnValue({ overflowY: 'visible' });
    window.requestAnimationFrame = jest.fn(callback => {
      callback(0);
      return 1;
    });
    window.scrollTo = jest.fn();
  });

  afterEach(() => {
    onDispose();
    jest.restoreAllMocks();
    document.body.innerHTML = '';
  });

  test('discovers headings and renders quick nav links in document order', () => {
    const view = setContainerMarkup(`
      <h1 id="intro">Intro</h1>
      <section>
        <h2>Details <span>Now</span></h2>
        <h3>Details Now</h3>
      </section>`);

    onLoad(view);

    const nav = view.querySelector('[data-nt-container-view-quick-nav]');
    const links = Array.from(view.querySelectorAll('.nt-container-view-quick-nav-list a'));

    expect(nav.hidden).toBe(false);
    expect(view.classList.contains('nt-container-view-with-quick-nav')).toBe(true);
    expect(links.map(link => link.textContent)).toEqual(['Intro', 'Details Now', 'Details Now']);
    expect(links.map(link => link.getAttribute('href'))).toEqual(['#intro', '#docs-details-now', '#docs-details-now-2']);
    expect(links[0].getAttribute('aria-current')).toBe('location');
  });

  test('does not include headings from nested container views', () => {
    const view = setContainerMarkup(`
      <h1 id="intro">Intro</h1>
      <div class="nt-container-view" data-nt-container-view data-nt-container-view-quick-nav-enabled="true">
        <h2 id="nested">Nested</h2>
      </div>
      <h2 id="details">Details</h2>`);

    onLoad(view);

    const links = Array.from(view.querySelectorAll(':scope > .nt-container-view-quick-nav .nt-container-view-quick-nav-list a'));

    expect(links.map(link => link.textContent)).toEqual(['Intro', 'Details']);
    expect(links.map(link => link.getAttribute('href'))).toEqual(['#intro', '#details']);
  });

  test('hides quick nav when the container has no headings', () => {
    const view = setContainerMarkup('<p>No headings here.</p>');

    onLoad(view);

    const nav = view.querySelector('[data-nt-container-view-quick-nav]');
    const links = view.querySelectorAll('.nt-container-view-quick-nav-list a');

    expect(nav.hidden).toBe(true);
    expect(view.classList.contains('nt-container-view-with-quick-nav')).toBe(false);
    expect(links).toHaveLength(0);
  });

  test('refreshes links when content changes after initial load', () => {
    const view = setContainerMarkup('<h1>Intro</h1>');

    onLoad(view);
    view.insertAdjacentHTML('beforeend', '<h2>Next Section</h2>');
    onUpdate(view);

    const links = Array.from(view.querySelectorAll('.nt-container-view-quick-nav-list a'));

    expect(links.map(link => link.textContent)).toEqual(['Intro', 'Next Section']);
    expect(links.map(link => link.getAttribute('href'))).toEqual(['#docs-intro', '#docs-next-section']);
  });

  test('updates the active item as the page scrolls', () => {
    const view = setContainerMarkup(`
      <h1 id="intro">Intro</h1>
      <h2 id="availability">Availability</h2>
      <h2 id="resources">Resources</h2>`);
    const headings = Array.from(view.querySelectorAll('h1, h2'));

    headings.forEach((heading, index) => setHeadingTop(heading, index * 400));

    onLoad(view);

    const links = Array.from(view.querySelectorAll('.nt-container-view-quick-nav-list a'));
    const items = Array.from(view.querySelectorAll('.nt-container-view-quick-nav-list li'));
    Object.defineProperty(items[1], 'offsetTop', { configurable: true, value: 48 });
    Object.defineProperty(items[1], 'offsetHeight', { configurable: true, value: 44 });

    setScrollY(420);
    window.dispatchEvent(new Event('scroll'));

    expect(links[0].hasAttribute('aria-current')).toBe(false);
    expect(links[1].getAttribute('aria-current')).toBe('location');
    expect(links[1].classList.contains('nt-container-view-quick-nav-link-active')).toBe(true);
    const list = view.querySelector('[data-nt-container-view-quick-nav-list]');
    expect(list.style.getPropertyValue('--nt-container-view-quick-nav-active-top')).toBe('48px');
    expect(list.style.getPropertyValue('--nt-container-view-quick-nav-active-height')).toBe('44px');
    expect(list.style.getPropertyValue('--nt-container-view-quick-nav-active-opacity')).toBe('1');
  });

  test('clicking a nav link smooth scrolls to the heading and marks it active', () => {
    const view = setContainerMarkup(`
      <h1 id="intro">Intro</h1>
      <h2 id="availability">Availability</h2>`);
    const headings = Array.from(view.querySelectorAll('h1, h2'));
    const pushState = jest.spyOn(history, 'pushState');

    headings.forEach((heading, index) => setHeadingTop(heading, index * 360));

    onLoad(view);

    const links = Array.from(view.querySelectorAll('.nt-container-view-quick-nav-list a'));
    links[1].click();

    expect(window.scrollTo).toHaveBeenCalledWith({ top: 360, behavior: 'smooth' });
    expect(pushState).toHaveBeenCalledWith(null, '', '#availability');
    expect(links[1].getAttribute('aria-current')).toBe('location');
    expect(links[1].classList.contains('nt-container-view-quick-nav-link-active')).toBe(true);
  });

  test('uses the nearest scroll container for scroll tracking and link navigation', () => {
    const view = setContainerMarkup(`
      <h1 id="intro">Intro</h1>
      <h2 id="availability">Availability</h2>`, { wrapInScrollContainer: true });
    const scrollHost = document.querySelector('.scroll-host');
    const headings = Array.from(view.querySelectorAll('h1, h2'));
    const pushState = jest.spyOn(history, 'pushState');

    Object.defineProperties(scrollHost, {
      clientHeight: { configurable: true, value: 320 },
      scrollHeight: { configurable: true, value: 900 },
      scrollTop: { configurable: true, writable: true, value: 0 }
    });
    scrollHost.getBoundingClientRect = () => ({
      bottom: 320,
      height: 320,
      left: 0,
      right: 300,
      top: 80,
      width: 300,
      x: 0,
      y: 80,
      toJSON() { return this; }
    });
    scrollHost.scrollTo = jest.fn(({ top }) => {
      scrollHost.scrollTop = top;
    });
    window.getComputedStyle = jest.fn(element => ({
      overflowY: element === scrollHost ? 'auto' : 'visible'
    }));
    headings[0].getBoundingClientRect = () => ({
      bottom: 80 - scrollHost.scrollTop + 32,
      height: 32,
      left: 0,
      right: 240,
      top: 80 - scrollHost.scrollTop,
      width: 240,
      x: 0,
      y: 80 - scrollHost.scrollTop,
      toJSON() { return this; }
    });
    headings[1].getBoundingClientRect = () => ({
      bottom: 80 + 360 - scrollHost.scrollTop + 32,
      height: 32,
      left: 0,
      right: 240,
      top: 80 + 360 - scrollHost.scrollTop,
      width: 240,
      x: 0,
      y: 80 + 360 - scrollHost.scrollTop,
      toJSON() { return this; }
    });

    onLoad(view);

    const links = Array.from(view.querySelectorAll('.nt-container-view-quick-nav-list a'));
    scrollHost.scrollTop = 370;
    scrollHost.dispatchEvent(new Event('scroll'));

    expect(links[1].getAttribute('aria-current')).toBe('location');

    links[1].click();

    expect(scrollHost.scrollTo).toHaveBeenCalledWith({ top: 360, behavior: 'smooth' });
    expect(window.scrollTo).not.toHaveBeenCalled();
    expect(pushState).toHaveBeenCalledWith(null, '', '#availability');
  });
});
