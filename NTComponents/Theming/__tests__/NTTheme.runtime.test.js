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
        lightDefault.rel = 'stylesheet';
        lightDefault.href = '/Themes/light.css';
        lightDefault.setAttribute('data-nt-theme-default', 'true');
        const darkDefault = document.createElement('link');
        darkDefault.rel = 'stylesheet';
        darkDefault.href = '/Themes/dark.css';
        darkDefault.setAttribute('data-nt-theme-default', 'true');
        document.head.append(lightDefault, darkDefault);

        const updatePromise = runtime.applyStylesheet(new URL('/Themes/dark.css', window.location.href).href, { waitForLoad: true });
        const link = document.head.querySelector('link[data-nt-theme]');

        expect(document.head.querySelectorAll('link[data-nt-theme-default]')).toHaveLength(2);

        link.dispatchEvent(new Event('load'));
        await updatePromise;

        expect(link.isConnected).toBe(true);
        expect(document.head.querySelectorAll('link[data-nt-theme-default]')).toHaveLength(0);
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
});
