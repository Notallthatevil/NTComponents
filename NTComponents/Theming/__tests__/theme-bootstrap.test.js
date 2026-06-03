/**
 * @jest-environment jsdom
 */
import { jest } from '@jest/globals';

const bootstrapUrl = new URL('../../wwwroot/theme-bootstrap.js', import.meta.url);

const importBootstrap = () => {
    jest.resetModules();
    return import(bootstrapUrl.href);
};

describe('theme bootstrap', () => {
    beforeEach(() => {
        window.__ntThemeBootstrapped = false;
        window.__tntThemeBootstrapped = false;
        window.NTComponents = {
            NTThemeRuntime: {
                apply: jest.fn(),
                restoreThemeState: jest.fn()
            }
        };
        delete window.Blazor;
        jest.clearAllMocks();
    });

    test('applies theme immediately and registers enhancedload with Blazor', async () => {
        window.Blazor = { addEventListener: jest.fn() };

        await importBootstrap();

        expect(window.NTComponents.NTThemeRuntime.restoreThemeState).toHaveBeenCalledWith({ waitForLoad: false });
        expect(window.NTComponents.NTThemeRuntime.apply).toHaveBeenCalledWith({ waitForLoad: false });
        expect(window.Blazor.addEventListener).toHaveBeenCalledWith('enhancedload', expect.any(Function));

        const enhancedLoadHandler = window.Blazor.addEventListener.mock.calls[0][1];
        enhancedLoadHandler();

        expect(window.NTComponents.NTThemeRuntime.restoreThemeState).toHaveBeenCalledTimes(2);
        expect(window.NTComponents.NTThemeRuntime.apply).toHaveBeenCalledTimes(2);
    });

    test('does not bootstrap twice', async () => {
        window.Blazor = { addEventListener: jest.fn() };

        await importBootstrap();
        await importBootstrap();

        expect(window.NTComponents.NTThemeRuntime.apply).toHaveBeenCalledTimes(1);
        expect(window.Blazor.addEventListener).toHaveBeenCalledTimes(1);
    });

    test('defers enhancedload registration until Blazor is available', async () => {
        await importBootstrap();
        window.Blazor = { addEventListener: jest.fn() };

        document.dispatchEvent(new Event('DOMContentLoaded'));

        expect(window.Blazor.addEventListener).toHaveBeenCalledWith('enhancedload', expect.any(Function));
    });

    test('exits when runtime is missing', async () => {
        window.NTComponents = {};
        window.Blazor = { addEventListener: jest.fn() };

        await importBootstrap();

        expect(window.Blazor.addEventListener).not.toHaveBeenCalled();
    });
});
