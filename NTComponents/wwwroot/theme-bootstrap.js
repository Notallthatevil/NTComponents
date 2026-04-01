(() => {
    if (window.__tntThemeBootstrapped) {
        return;
    }

    window.__tntThemeBootstrapped = true;

    const validThemes = new Set(['DARK', 'LIGHT', 'SYSTEM']);
    const validContrasts = new Set(['DEFAULT', 'MEDIUM', 'HIGH']);
    const themeCssMap = {
        LIGHT: { DEFAULT: 'light.css', MEDIUM: 'light-mc.css', HIGH: 'light-hc.css' },
        DARK: { DEFAULT: 'dark.css', MEDIUM: 'dark-mc.css', HIGH: 'dark-hc.css' }
    };

    const readStoredValue = (key, validValues) => {
        try {
            const value = localStorage.getItem(key);
            return validValues.has(value) ? value : null;
        } catch {
            return null;
        }
    };

    const storedTheme = readStoredValue('NTComponentsStoredThemeKey', validThemes);
    const storedContrast = readStoredValue('NTComponentsStoredContrastKey', validContrasts) ?? 'DEFAULT';
    const resolvedTheme = !storedTheme || storedTheme === 'SYSTEM'
        ? (window.matchMedia?.('(prefers-color-scheme: dark)').matches ? 'DARK' : 'LIGHT')
        : storedTheme;
    const resolvedCssFile = themeCssMap[resolvedTheme]?.[storedContrast] ?? themeCssMap[resolvedTheme]?.DEFAULT;

    const resolvedColorScheme = resolvedTheme === 'DARK' ? 'dark' : 'light';
    let criticalStyle = document.head.querySelector('style[data-tnt-theme-critical]');
    if (!criticalStyle) {
        criticalStyle = document.createElement('style');
        criticalStyle.setAttribute('data-tnt-theme-critical', 'true');
        document.head.appendChild(criticalStyle);
    }

    criticalStyle.textContent = `html { color-scheme: ${resolvedColorScheme}; } html, body, #app { background-color: Canvas; color: CanvasText; }`;

    if (!resolvedCssFile) {
        return;
    }

    const resolvedHref = new URL(`/Themes/${resolvedCssFile}`, window.location.href).href;
    let themeLink = document.head.querySelector('link[data-tnt-theme]');

    if (!themeLink) {
        themeLink = document.createElement('link');
        themeLink.rel = 'stylesheet';
        themeLink.setAttribute('data-tnt-theme', 'true');
        document.head.appendChild(themeLink);
    }

    themeLink.setAttribute('data-tnt-theme-loaded', 'false');
    themeLink.addEventListener('load', () => themeLink.setAttribute('data-tnt-theme-loaded', 'true'), { once: true });
    themeLink.addEventListener('error', () => themeLink.setAttribute('data-tnt-theme-loaded', 'error'), { once: true });

    if (themeLink.href !== resolvedHref) {
        themeLink.href = resolvedHref;
    } else if (themeLink.sheet) {
        themeLink.setAttribute('data-tnt-theme-loaded', 'true');
    }
})();
