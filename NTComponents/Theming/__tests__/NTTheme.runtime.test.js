/**
 * @jest-environment jsdom
 */
import { jest } from '@jest/globals';

const runtimeUrl = new URL('../../wwwroot/NTTheme.runtime.js', import.meta.url);

const loadRuntime = async () => {
    delete window.NTComponents?.NTThemeRuntime;
    window.NTComponents = {};
    jest.resetModules();
    await import(runtimeUrl.href);
    return window.NTComponents.NTThemeRuntime;
};

describe('NTTheme runtime', () => {
    beforeEach(() => {
        document.head.innerHTML = '';
        document.body.innerHTML = '';
        jest.clearAllMocks();
    });

    test('applyStylesheet creates a permanent stylesheet link when none exists', async () => {
        const runtime = await loadRuntime();
        const updatePromise = runtime.applyStylesheet(new URL('/Themes/light.css', window.location.href).href, { waitForLoad: true });
        const link = document.head.querySelector('link[data-nt-theme]');

        expect(link).not.toBeNull();
        expect(link.rel).toBe('stylesheet');
        expect(link.hasAttribute('data-permanent')).toBe(true);

        link.dispatchEvent(new Event('load'));
        await updatePromise;

        expect(link.getAttribute('data-nt-theme-loaded')).toBe('true');
        expect(link.getAttribute('data-tnt-theme-loaded')).toBe('true');
    });

    test('applyStylesheet removes first-paint default links after active theme loads', async () => {
        const runtime = await loadRuntime();
        const lightDefault = document.createElement('link');
        lightDefault.id = 'nt-theme-default-light';
        lightDefault.rel = 'stylesheet';
        lightDefault.href = '/Themes/light.css';
        lightDefault.setAttribute('data-nt-theme-default', 'true');
        lightDefault.setAttribute('data-permanent', '');
        const darkDefault = document.createElement('link');
        darkDefault.id = 'nt-theme-default-dark';
        darkDefault.rel = 'stylesheet';
        darkDefault.href = '/Themes/dark.css';
        darkDefault.setAttribute('data-nt-theme-default', 'true');
        darkDefault.setAttribute('data-permanent', '');
        document.head.append(lightDefault, darkDefault);

        const updatePromise = runtime.applyStylesheet(new URL('/Themes/dark.css', window.location.href).href, { waitForLoad: true });
        const link = document.head.querySelector('link[data-nt-theme]');

        expect(document.head.querySelectorAll('link[data-nt-theme-default]')).toHaveLength(1);
        expect(link).toBe(darkDefault);

        link.dispatchEvent(new Event('load'));
        await updatePromise;

        expect(link.isConnected).toBe(true);
        expect(lightDefault.isConnected).toBe(true);
        expect(lightDefault.hasAttribute('href')).toBe(false);
        expect(lightDefault.hasAttribute('rel')).toBe(false);
        expect(document.head.querySelectorAll('link[data-nt-theme-default]')).toHaveLength(0);
    });

    test('loaded theme leaves stable fallback elements inert for enhanced navigation', async () => {
        const runtime = await loadRuntime();
        const critical = document.createElement('style');
        critical.id = 'nt-theme-critical';
        critical.setAttribute('data-nt-theme-critical', 'true');
        critical.setAttribute('data-tnt-theme-critical', 'true');
        critical.setAttribute('data-permanent', '');
        critical.textContent = 'html, body { background: Canvas; }';
        const lightDefault = document.createElement('link');
        lightDefault.id = 'nt-theme-default-light';
        lightDefault.rel = 'stylesheet';
        lightDefault.href = '/Themes/light.css';
        lightDefault.setAttribute('data-nt-theme-default', 'true');
        lightDefault.setAttribute('data-permanent', '');
        document.head.append(critical, lightDefault);

        const updatePromise = runtime.applyStylesheet(new URL('/Themes/dark.css', window.location.href).href, { waitForLoad: true });
        const active = document.head.querySelector('link[data-nt-theme]');
        active.dispatchEvent(new Event('load'));
        await updatePromise;

        expect(critical.isConnected).toBe(true);
        expect(critical.textContent).toBe('');
        expect(critical.hasAttribute('data-nt-theme-critical')).toBe(false);
        expect(critical.hasAttribute('data-tnt-theme-critical')).toBe(false);
        expect(lightDefault.isConnected).toBe(true);
        expect(lightDefault.hasAttribute('href')).toBe(false);
        expect(lightDefault.hasAttribute('data-nt-theme-default')).toBe(false);
        expect(document.head.querySelectorAll('link[data-nt-theme]')).toHaveLength(1);
    });

    test('applyStylesheet preloads next theme before replacing current theme', async () => {
        const runtime = await loadRuntime();
        const current = document.createElement('link');
        current.rel = 'stylesheet';
        current.href = '/Themes/light.css';
        current.setAttribute('data-nt-theme', 'true');
        current.setAttribute('data-tnt-theme', 'true');
        document.head.appendChild(current);

        const updatePromise = runtime.applyStylesheet(new URL('/Themes/dark.css', window.location.href).href, { waitForLoad: true });
        const pending = document.head.querySelector('link[data-nt-theme-pending]');

        expect(pending).not.toBeNull();
        expect(pending.rel).toBe('preload');
        expect(pending.as).toBe('style');
        expect(pending.hasAttribute('data-permanent')).toBe(true);
        expect(document.head.querySelector('link[data-nt-theme]')).toBe(current);

        pending.dispatchEvent(new Event('load'));
        const promoted = await updatePromise;

        expect(promoted).toBe(pending);
        expect(current.isConnected).toBe(false);
        expect(promoted.rel).toBe('stylesheet');
        expect(promoted.hasAttribute('as')).toBe(false);
        expect(promoted.hasAttribute('data-nt-theme-pending')).toBe(false);
        expect(promoted.getAttribute('data-nt-theme-loaded')).toBe('true');
    });

    test('applyStylesheet leaves current theme active when pending preload errors', async () => {
        const runtime = await loadRuntime();
        const current = document.createElement('link');
        current.rel = 'stylesheet';
        current.href = '/Themes/light.css';
        current.setAttribute('data-nt-theme', 'true');
        document.head.appendChild(current);

        const updatePromise = runtime.applyStylesheet(new URL('/Themes/missing.css', window.location.href).href, { waitForLoad: true });
        const pending = document.head.querySelector('link[data-nt-theme-pending]');

        pending.dispatchEvent(new Event('error'));
        const result = await updatePromise;

        expect(result).toBe(current);
        expect(current.isConnected).toBe(true);
        expect(document.head.querySelector('link[data-nt-theme-pending]')).toBeNull();
    });

    test('repeated theme changes reuse stable active and pending slots without duplicates', async () => {
        const runtime = await loadRuntime();
        const lightHref = new URL('/Themes/light.css', window.location.href).href;
        const darkHref = new URL('/Themes/dark.css', window.location.href).href;

        const initialPromise = runtime.applyStylesheet(lightHref, { waitForLoad: true });
        document.head.querySelector('link[data-nt-theme]').dispatchEvent(new Event('load'));
        await initialPromise;

        const darkPromise = runtime.applyStylesheet(darkHref, { waitForLoad: true });
        document.head.querySelector('link[data-nt-theme-pending]').dispatchEvent(new Event('load'));
        await darkPromise;

        const lightPromise = runtime.applyStylesheet(lightHref, { waitForLoad: true });
        document.head.querySelector('link[data-nt-theme-pending]').dispatchEvent(new Event('load'));
        await lightPromise;

        expect(document.head.querySelectorAll('#nt-theme-active-slot')).toHaveLength(1);
        expect(document.head.querySelectorAll('#nt-theme-pending-slot')).toHaveLength(1);
        expect(document.head.querySelectorAll('link[data-nt-theme]')).toHaveLength(1);
        expect(document.head.querySelectorAll('link[data-nt-theme-pending]')).toHaveLength(0);
        expect(document.head.querySelector('link[data-nt-theme]').href).toBe(lightHref);
    });

    test('apply writes permanent theme state for enhanced navigation restoration', async () => {
        const runtime = await loadRuntime();
        const applyPromise = runtime.apply({ theme: 'DARK', contrast: 'HIGH', waitForLoad: true });
        const link = document.head.querySelector('link[data-nt-theme]');

        link.dispatchEvent(new Event('load'));
        await applyPromise;

        const stateElement = document.getElementById('nt-theme-state');
        const state = JSON.parse(stateElement.textContent);

        expect(stateElement.type).toBe('application/json');
        expect(stateElement.hasAttribute('data-permanent')).toBe(true);
        expect(state.themePreference).toBe('DARK');
        expect(state.theme).toBe('DARK');
        expect(state.contrast).toBe('HIGH');
        expect(state.href).toBe(new URL('/Themes/dark-hc.css', window.location.href).href);
    });

    test('restoreThemeState recreates stylesheet from permanent state', async () => {
        const runtime = await loadRuntime();
        const stateElement = document.createElement('script');
        stateElement.type = 'application/json';
        stateElement.id = 'nt-theme-state';
        stateElement.setAttribute('data-permanent', '');
        stateElement.textContent = JSON.stringify({ href: new URL('/Themes/dark.css', window.location.href).href });
        document.head.appendChild(stateElement);

        const result = await runtime.restoreThemeState({ waitForLoad: false });
        const link = document.head.querySelector('link[data-nt-theme]');

        expect(result).toBe(link);
        expect(link.href).toBe(new URL('/Themes/dark.css', window.location.href).href);
        expect(link.hasAttribute('data-permanent')).toBe(true);
    });

    test('restoreThemeState ignores external state hrefs', async () => {
        const runtime = await loadRuntime();
        const stateElement = document.createElement('script');
        stateElement.type = 'application/json';
        stateElement.id = 'nt-theme-state';
        stateElement.textContent = JSON.stringify({ href: 'https://example.com/theme.css' });
        document.head.appendChild(stateElement);

        expect(runtime.restoreThemeState({ waitForLoad: false })).toBeNull();
        expect(document.head.querySelector('link[data-nt-theme]')).toBeNull();
    });

    test('hasCurrentThemeState requires one active stylesheet matching permanent state', async () => {
        const runtime = await loadRuntime();
        const href = new URL('/Themes/dark.css', window.location.href).href;
        runtime.writeThemeState({ themePreference: 'DARK', theme: 'DARK', contrast: 'DEFAULT', href });
        const applyPromise = runtime.applyStylesheet(href, { waitForLoad: true });
        const active = document.head.querySelector('link[data-nt-theme]');
        active.dispatchEvent(new Event('load'));
        await applyPromise;

        expect(runtime.hasCurrentThemeState()).toBe(true);

        const duplicate = document.createElement('link');
        duplicate.setAttribute('data-nt-theme', 'true');
        duplicate.href = href;
        document.head.appendChild(duplicate);

        expect(runtime.hasCurrentThemeState()).toBe(false);
    });
});
