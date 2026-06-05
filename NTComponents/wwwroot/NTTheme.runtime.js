(() => {
    const rootWindow = window;
    const ntComponents = rootWindow.NTComponents = rootWindow.NTComponents || {};

    if (ntComponents.NTThemeRuntime) {
        return;
    }

    const validThemes = new Set(['DARK', 'LIGHT', 'SYSTEM']);
    const validContrasts = new Set(['DEFAULT', 'MEDIUM', 'HIGH']);
    const defaultConfig = {
        themeStorageKey: 'NTComponentsStoredThemeKey',
        contrastStorageKey: 'NTComponentsStoredContrastKey',
        defaultTheme: 'SYSTEM',
        defaultContrast: 'DEFAULT',
        themesRoot: '/Themes',
        lightDefaultCss: 'light.css',
        lightMediumCss: 'light-mc.css',
        lightHighCss: 'light-hc.css',
        darkDefaultCss: 'dark.css',
        darkMediumCss: 'dark-mc.css',
        darkHighCss: 'dark-hc.css',
    };
    const themeStateElementId = 'nt-theme-state';

    const fallbackCss = ':root{--tnt-color-primary:rgb(84 90 146);--tnt-color-surface-tint:rgb(84 90 146);--tnt-color-on-primary:rgb(255 255 255);--tnt-color-primary-container:rgb(224 224 255);--tnt-color-on-primary-container:rgb(60 66 121);--tnt-color-secondary:rgb(92 93 114);--tnt-color-on-secondary:rgb(255 255 255);--tnt-color-secondary-container:rgb(225 224 249);--tnt-color-on-secondary-container:rgb(68 69 89);--tnt-color-tertiary:rgb(120 83 107);--tnt-color-on-tertiary:rgb(255 255 255);--tnt-color-tertiary-container:rgb(255 215 239);--tnt-color-on-tertiary-container:rgb(94 60 83);--tnt-color-error:rgb(186 26 26);--tnt-color-on-error:rgb(255 255 255);--tnt-color-error-container:rgb(255 218 214);--tnt-color-on-error-container:rgb(147 0 10);--tnt-color-background:rgb(251 248 255);--tnt-color-on-background:rgb(27 27 33);--tnt-color-surface:rgb(251 248 255);--tnt-color-on-surface:rgb(27 27 33);--tnt-color-surface-variant:rgb(227 225 236);--tnt-color-on-surface-variant:rgb(70 70 79);--tnt-color-outline:rgb(119 118 128);--tnt-color-outline-variant:rgb(199 197 208);--tnt-color-shadow:rgb(0 0 0);--tnt-color-scrim:rgb(0 0 0);--tnt-color-inverse-surface:rgb(48 48 54);--tnt-color-inverse-on-surface:rgb(242 239 247);--tnt-color-inverse-primary:rgb(189 194 255);--tnt-color-primary-fixed:rgb(224 224 255);--tnt-color-on-primary-fixed:rgb(15 21 75);--tnt-color-primary-fixed-dim:rgb(189 194 255);--tnt-color-on-primary-fixed-variant:rgb(60 66 121);--tnt-color-secondary-fixed:rgb(225 224 249);--tnt-color-on-secondary-fixed:rgb(24 26 44);--tnt-color-secondary-fixed-dim:rgb(196 196 221);--tnt-color-on-secondary-fixed-variant:rgb(68 69 89);--tnt-color-tertiary-fixed:rgb(255 215 239);--tnt-color-on-tertiary-fixed:rgb(46 17 38);--tnt-color-tertiary-fixed-dim:rgb(231 185 213);--tnt-color-on-tertiary-fixed-variant:rgb(94 60 83);--tnt-color-surface-dim:rgb(219 217 224);--tnt-color-surface-bright:rgb(251 248 255);--tnt-color-surface-container-lowest:rgb(255 255 255);--tnt-color-surface-container-low:rgb(245 242 250);--tnt-color-surface-container:rgb(239 237 244);--tnt-color-surface-container-high:rgb(234 231 239);--tnt-color-surface-container-highest:rgb(228 225 233);--tnt-color-info:rgb(67 94 145);--tnt-color-on-info:rgb(255 255 255);--tnt-color-info-container:rgb(215 226 255);--tnt-color-on-info-container:rgb(42 70 119);--tnt-color-success:rgb(49 106 66);--tnt-color-on-success:rgb(255 255 255);--tnt-color-success-container:rgb(179 241 190);--tnt-color-on-success-container:rgb(22 81 44);--tnt-color-warning:rgb(111 93 13);--tnt-color-on-warning:rgb(255 255 255);--tnt-color-warning-container:rgb(251 225 134);--tnt-color-on-warning-container:rgb(85 69 0);--tnt-color-assert:rgb(124 78 126);--tnt-color-on-assert:rgb(255 255 255);--tnt-color-assert-container:rgb(255 214 252);--tnt-color-on-assert-container:rgb(98 55 101);}';

    let listening = false;
    let mediaQueryList = null;
    let activeElementConfig = {};
    const controls = new Set();

    const upper = value => typeof value === 'string' ? value.trim().toUpperCase() : '';
    const safeGetStorage = key => {
        try {
            return rootWindow.localStorage?.getItem(key) ?? null;
        } catch {
            return null;
        }
    };
    const safeSetStorage = (key, value) => {
        try {
            rootWindow.localStorage?.setItem(key, value);
        } catch {
            // Theme still applies even when persistence is unavailable.
        }
    };
    const safeRemoveStorage = key => {
        try {
            rootWindow.localStorage?.removeItem(key);
        } catch {
            // Storage may be unavailable under browser privacy settings.
        }
    };

    const readJsonConfig = () => {
        const configElement = document.getElementById('nt-theme-config');
        if (!configElement?.textContent) {
            return {};
        }

        try {
            return JSON.parse(configElement.textContent);
        } catch {
            return {};
        }
    };
    const getThemeStateElement = () => document.getElementById(themeStateElementId);
    const ensureThemeStateElement = () => {
        let stateElement = getThemeStateElement();

        if (!stateElement) {
            stateElement = document.createElement('script');
            stateElement.type = 'application/json';
            stateElement.id = themeStateElementId;
            stateElement.setAttribute('data-permanent', '');
            document.head.appendChild(stateElement);
        }

        return stateElement;
    };
    const readThemeState = () => {
        const stateElement = getThemeStateElement();
        if (!stateElement?.textContent) {
            return {};
        }

        try {
            const state = JSON.parse(stateElement.textContent);
            return state && typeof state === 'object' ? state : {};
        } catch {
            return {};
        }
    };
    const writeThemeState = result => {
        const stateElement = ensureThemeStateElement();
        stateElement.textContent = JSON.stringify({
            themePreference: result.themePreference,
            theme: result.theme,
            contrast: result.contrast,
            href: result.href,
        });
        return stateElement;
    };

    const readAttribute = (element, ntName, legacyName) => element?.getAttribute(ntName) || element?.getAttribute(legacyName) || undefined;
    const getElementConfig = element => {
        if (!element) {
            return {};
        }

        return {
            themesRoot: readAttribute(element, 'nt-themes-root', 'tnt-themes-root'),
            lightDefaultCss: readAttribute(element, 'nt-light-default', 'tnt-light-default'),
            lightMediumCss: readAttribute(element, 'nt-light-medium', 'tnt-light-medium'),
            lightHighCss: readAttribute(element, 'nt-light-high', 'tnt-light-high'),
            darkDefaultCss: readAttribute(element, 'nt-dark-default', 'tnt-dark-default'),
            darkMediumCss: readAttribute(element, 'nt-dark-medium', 'tnt-dark-medium'),
            darkHighCss: readAttribute(element, 'nt-dark-high', 'tnt-dark-high'),
            defaultTheme: readAttribute(element, 'nt-default-theme', 'tnt-default-theme'),
            defaultContrast: readAttribute(element, 'nt-default-contrast', 'tnt-default-contrast'),
        };
    };

    const cleanConfig = config => Object.fromEntries(Object.entries(config).filter(([, value]) => value !== undefined && value !== null && value !== ''));
    const getConfiguration = element => {
        const elementConfig = cleanConfig(getElementConfig(element));
        if (element && Object.keys(elementConfig).length > 0) {
            activeElementConfig = elementConfig;
        }

        const configured = {
            ...defaultConfig,
            ...cleanConfig(rootWindow.NTComponentsThemeConfig || {}),
            ...cleanConfig(readJsonConfig()),
            ...(element ? elementConfig : activeElementConfig),
        };

        configured.defaultTheme = validThemes.has(upper(configured.defaultTheme)) ? upper(configured.defaultTheme) : defaultConfig.defaultTheme;
        configured.defaultContrast = validContrasts.has(upper(configured.defaultContrast)) ? upper(configured.defaultContrast) : defaultConfig.defaultContrast;
        return configured;
    };

    const normalizeTheme = (theme, fallback = 'SYSTEM') => validThemes.has(upper(theme)) ? upper(theme) : fallback;
    const normalizeContrast = (contrast, fallback = 'DEFAULT') => validContrasts.has(upper(contrast)) ? upper(contrast) : fallback;
    const systemPrefersDark = () => rootWindow.matchMedia?.('(prefers-color-scheme: dark)').matches === true;
    const resolveActualTheme = theme => theme === 'SYSTEM' ? (systemPrefersDark() ? 'DARK' : 'LIGHT') : theme;
    const getStoredTheme = config => {
        const stored = upper(safeGetStorage(config.themeStorageKey));
        return validThemes.has(stored) ? stored : null;
    };
    const getStoredContrast = config => {
        const stored = upper(safeGetStorage(config.contrastStorageKey));
        return validContrasts.has(stored) ? stored : null;
    };

    const cleanupInvalidStoredValues = config => {
        const storedTheme = safeGetStorage(config.themeStorageKey);
        const storedContrast = safeGetStorage(config.contrastStorageKey);

        if (storedTheme && !validThemes.has(upper(storedTheme))) {
            safeRemoveStorage(config.themeStorageKey);
        }

        if (storedContrast && !validContrasts.has(upper(storedContrast))) {
            safeRemoveStorage(config.contrastStorageKey);
        }
    };

    const hasScheme = value => /^[a-z][a-z0-9+.-]*:/i.test(value);
    const safeFileName = (value, fallback) => {
        const fileName = typeof value === 'string' ? value.trim() : '';
        return fileName && !hasScheme(fileName) && !fileName.startsWith('//') && !fileName.startsWith('/') ? fileName : fallback;
    };
    const safeThemesRoot = root => {
        const value = typeof root === 'string' && root.trim() ? root.trim() : defaultConfig.themesRoot;

        if (value.startsWith('//')) {
            return defaultConfig.themesRoot;
        }

        try {
            const url = new URL(value.endsWith('/') ? value : `${value}/`, rootWindow.location.href);
            return url.origin === rootWindow.location.origin ? url.href : new URL(defaultConfig.themesRoot, rootWindow.location.href).href;
        } catch {
            return new URL(defaultConfig.themesRoot, rootWindow.location.href).href;
        }
    };

    const getCssFile = (config, theme, contrast) => {
        const cssMap = {
            LIGHT: {
                DEFAULT: safeFileName(config.lightDefaultCss, defaultConfig.lightDefaultCss),
                MEDIUM: safeFileName(config.lightMediumCss, defaultConfig.lightMediumCss),
                HIGH: safeFileName(config.lightHighCss, defaultConfig.lightHighCss),
            },
            DARK: {
                DEFAULT: safeFileName(config.darkDefaultCss, defaultConfig.darkDefaultCss),
                MEDIUM: safeFileName(config.darkMediumCss, defaultConfig.darkMediumCss),
                HIGH: safeFileName(config.darkHighCss, defaultConfig.darkHighCss),
            },
        };

        return cssMap[theme]?.[contrast] || cssMap[theme]?.DEFAULT || defaultConfig.lightDefaultCss;
    };

    const resolveThemeHref = (config, theme, contrast) => {
        const rootUrl = safeThemesRoot(config.themesRoot);
        const cssUrl = new URL(getCssFile(config, theme, contrast), rootUrl);
        return cssUrl.origin === rootWindow.location.origin ? cssUrl.href : new URL(defaultConfig.lightDefaultCss, safeThemesRoot(defaultConfig.themesRoot)).href;
    };

    const removeCriticalThemeStyles = () => document.querySelector('style[data-tnt-theme-critical],style[data-nt-theme-critical]')?.remove();
    const removeFallbackStyles = () => document.querySelector('style[data-tnt-theme],style[data-nt-theme-fallback]')?.remove();
    const removeDefaultThemeStylesheets = () => document.querySelectorAll('link[data-nt-theme-default],link[data-tnt-theme-default]').forEach(link => link.remove());
    const injectFallbackStyles = () => {
        let style = document.head.querySelector('style[data-nt-theme-fallback]');
        if (!style) {
            style = document.createElement('style');
            style.setAttribute('data-nt-theme-fallback', 'true');
            style.setAttribute('data-tnt-theme', 'true');
            style.setAttribute('data-permanent', '');
            document.head.appendChild(style);
        }

        style.textContent = fallbackCss;
    };

    const waitForLink = link => {
        if (!link) {
            return Promise.resolve('error');
        }

        if (link.getAttribute('data-nt-theme-loaded') === 'true' || link.getAttribute('data-tnt-theme-loaded') === 'true' || link.sheet) {
            link.setAttribute('data-nt-theme-loaded', 'true');
            link.setAttribute('data-tnt-theme-loaded', 'true');
            return Promise.resolve('true');
        }

        return new Promise(resolve => {
            const complete = status => {
                link.setAttribute('data-nt-theme-loaded', status);
                link.setAttribute('data-tnt-theme-loaded', status);
                resolve(status);
            };

            link.addEventListener('load', () => complete('true'), { once: true });
            link.addEventListener('error', () => complete('error'), { once: true });
        });
    };

    const markThemeLink = link => {
        link.setAttribute('data-nt-theme', 'true');
        link.setAttribute('data-tnt-theme', 'true');
        link.setAttribute('data-nt-theme-loaded', 'false');
        link.setAttribute('data-tnt-theme-loaded', 'false');
        link.setAttribute('data-permanent', '');
    };

    const findCurrentThemeLink = () => document.head.querySelector('link[data-nt-theme],link[data-tnt-theme]');
    const applyStylesheet = async (href, options = {}) => {
        const waitForLoad = options.waitForLoad !== false;
        const current = findCurrentThemeLink();
        const currentHref = current ? new URL(current.href, rootWindow.location.href).href : null;
        const dispatchThemeFailed = detail => document.dispatchEvent(new CustomEvent('nt-theme-failed', { detail }));
        const finishLinkLoad = async link => {
            const status = await waitForLink(link);

            if (status === 'true') {
                removeCriticalThemeStyles();
                removeFallbackStyles();
                removeDefaultThemeStylesheets();
            } else {
                dispatchThemeFailed({ href: link.href, reason: 'stylesheet-error' });
                injectFallbackStyles();
            }

            return status;
        };

        if (current && currentHref === href) {
            if (waitForLoad) {
                await finishLinkLoad(current);
            } else {
                finishLinkLoad(current);
            }

            return current;
        }

        if (!current) {
            const link = document.createElement('link');
            link.rel = 'stylesheet';
            markThemeLink(link);
            link.href = href;
            document.head.appendChild(link);

            if (waitForLoad) {
                await finishLinkLoad(link);
            } else {
                finishLinkLoad(link);
            }

            return link;
        }

        const pending = document.createElement('link');
        pending.rel = 'preload';
        pending.as = 'style';
        pending.setAttribute('data-nt-theme-pending', 'true');
        pending.setAttribute('data-nt-theme-loaded', 'false');
        pending.setAttribute('data-tnt-theme-loaded', 'false');
        pending.setAttribute('data-permanent', '');
        pending.href = href;
        current.after(pending);

        const promotePending = status => {
            if (status === 'true') {
                current.remove();
                pending.rel = 'stylesheet';
                pending.removeAttribute('as');
                pending.removeAttribute('data-nt-theme-pending');
                markThemeLink(pending);
                pending.setAttribute('data-nt-theme-loaded', 'true');
                pending.setAttribute('data-tnt-theme-loaded', 'true');
                removeCriticalThemeStyles();
                removeFallbackStyles();
                removeDefaultThemeStylesheets();
                return pending;
            }

            pending.remove();
            dispatchThemeFailed({ href: pending.href, reason: 'stylesheet-error' });
            return current;
        };

        if (!waitForLoad) {
            waitForLink(pending).then(promotePending);
            return pending;
        }

        return promotePending(await waitForLink(pending));
    };
    const normalizeStateHref = href => {
        if (typeof href !== 'string' || !href.trim()) {
            return null;
        }

        try {
            const url = new URL(href, rootWindow.location.href);
            return url.origin === rootWindow.location.origin ? url.href : null;
        } catch {
            return null;
        }
    };
    const restoreThemeState = (options = {}) => {
        const href = normalizeStateHref(readThemeState().href);
        if (!href) {
            return null;
        }

        const current = findCurrentThemeLink();
        const currentHref = current ? new URL(current.href, rootWindow.location.href).href : null;

        if (currentHref === href) {
            return current;
        }

        return applyStylesheet(href, { waitForLoad: options.waitForLoad });
    };

    const syncControls = result => {
        controls.forEach(control => {
            control.updateIcon?.(result.theme);
            control.initSelect?.(result.theme, { skipApply: true });
        });
    };
    const dispatchThemeChanged = result => {
        document.dispatchEvent(new CustomEvent('nt-theme-changed', { detail: result }));
        document.dispatchEvent(new CustomEvent('tnt-theme-changed', { detail: result }));
    };
    const resolveThemeState = config => {
        cleanupInvalidStoredValues(config);
        const themePreference = normalizeTheme(getStoredTheme(config) || config.defaultTheme, defaultConfig.defaultTheme);
        const contrast = normalizeContrast(getStoredContrast(config) || config.defaultContrast, defaultConfig.defaultContrast);
        const theme = resolveActualTheme(themePreference);
        return { themePreference, theme, contrast, href: resolveThemeHref(config, theme, contrast) };
    };

    const apply = async (options = {}) => {
        const config = getConfiguration(options.element);

        if (options.theme) {
            safeSetStorage(config.themeStorageKey, normalizeTheme(options.theme, config.defaultTheme));
        }

        if (options.contrast) {
            safeSetStorage(config.contrastStorageKey, normalizeContrast(options.contrast, config.defaultContrast));
        }

        const result = resolveThemeState(config);
        writeThemeState(result);
        await applyStylesheet(result.href, { waitForLoad: options.waitForLoad });
        syncControls(result);
        dispatchThemeChanged(result);
        ensureListeners();
        return result;
    };

    function ensureListeners() {
        if (listening) {
            return;
        }

        listening = true;

        if (rootWindow.matchMedia) {
            mediaQueryList = rootWindow.matchMedia('(prefers-color-scheme: dark)');
            const handleMediaChange = () => {
                const config = getConfiguration();
                const storedTheme = normalizeTheme(getStoredTheme(config) || config.defaultTheme, config.defaultTheme);
                if (storedTheme === 'SYSTEM') {
                    apply({ waitForLoad: true });
                }
            };
            mediaQueryList.addEventListener?.('change', handleMediaChange);
            mediaQueryList.addListener?.(handleMediaChange);
        }

        rootWindow.addEventListener?.('storage', event => {
            const config = getConfiguration();
            if (event.key === config.themeStorageKey || event.key === config.contrastStorageKey) {
                apply({ waitForLoad: true });
            }
        });
    }

    ntComponents.NTThemeRuntime = {
        apply,
        applyStylesheet,
        cleanupInvalidStoredValues,
        controls,
        defaultConfig,
        fallbackCss,
        getConfiguration,
        getCssFile,
        getFallbackCss: () => fallbackCss,
        getStoredContrast,
        getStoredTheme,
        readThemeState,
        injectFallbackStyles,
        normalizeContrast,
        normalizeTheme,
        registerControl: element => controls.add(element),
        removeCriticalThemeStyles,
        removeDefaultThemeStylesheets,
        restoreThemeState,
        resolveThemeHref,
        safeSetStorage,
        systemPrefersDark,
        unregisterControl: element => controls.delete(element),
        writeThemeState,
    };
})();
