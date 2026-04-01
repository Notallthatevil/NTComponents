// All theme logic encapsulated in the custom element class
class TnTThemeToggleElement extends HTMLElement {
    static observedAttributes = [window.NTComponents && window.NTComponents.customAttribute ? window.NTComponents.customAttribute : 'tnt-theme-toggle'];

    constructor() {
        super();
        // Bind the combined select handler
        this.themeContrastSelected = this.themeContrastSelected.bind(this);
    }

    // Storage keys and constants
    themeStorageKey = 'NTComponentsStoredThemeKey';
    contrastStorageKey = 'NTComponentsStoredContrastKey';
    prefersDark = 'DARK';
    prefersLight = 'LIGHT';
    prefersSystem = 'SYSTEM';
    contrastDefault = 'DEFAULT';
    contrastMedium = 'MEDIUM';
    contrastHigh = 'HIGH';

    themeCssMap = {
        LIGHT: {
            DEFAULT: 'light.css',
            MEDIUM: 'light-mc.css',
            HIGH: 'light-hc.css',
        },
        DARK: {
            DEFAULT: 'dark.css',
            MEDIUM: 'dark-mc.css',
            HIGH: 'dark-hc.css',
        }
    };
    getStoredTheme() {
        const stored = localStorage.getItem(this.themeStorageKey);
        // Validate the stored theme value
        const validThemes = [this.prefersDark, this.prefersLight, this.prefersSystem];
        return validThemes.includes(stored) ? stored : null;
    }

    setStoredTheme(theme) {
        localStorage.setItem(this.themeStorageKey, theme);
    }

    getStoredContrast() {
        const stored = localStorage.getItem(this.contrastStorageKey);
        // Validate the stored contrast value
        const validContrasts = [this.contrastDefault, this.contrastMedium, this.contrastHigh];
        return validContrasts.includes(stored) ? stored : null;
    }

    setStoredContrast(contrast) {
        localStorage.setItem(this.contrastStorageKey, contrast);
    }

    // Validate and sanitize theme values
    validateTheme(theme) {
        const validThemes = [this.prefersDark, this.prefersLight, this.prefersSystem];
        return validThemes.includes(theme) ? theme : this.prefersSystem;
    } validateContrast(contrast) {
        const validContrasts = [this.contrastDefault, this.contrastMedium, this.contrastHigh];
        return validContrasts.includes(contrast) ? contrast : this.contrastDefault;
    }

    // Clean up invalid stored values
    cleanupInvalidStoredValues() {
        const storedTheme = localStorage.getItem(this.themeStorageKey);
        const storedContrast = localStorage.getItem(this.contrastStorageKey);

        const validThemes = [this.prefersDark, this.prefersLight, this.prefersSystem];
        const validContrasts = [this.contrastDefault, this.contrastMedium, this.contrastHigh];

        // Remove or fix invalid theme
        if (storedTheme && !validThemes.includes(storedTheme)) {
            console.warn(`Invalid theme stored: ${storedTheme}. Removing from localStorage.`);
            localStorage.removeItem(this.themeStorageKey);
        }

        // Remove or fix invalid contrast
        if (storedContrast && !validContrasts.includes(storedContrast)) {
            console.warn(`Invalid contrast stored: ${storedContrast}. Removing from localStorage.`);
            localStorage.removeItem(this.contrastStorageKey);
        }
    }

    systemPrefersDark() {
        return window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
    }

    async cssFileExists(href) {
        try {
            const response = await fetch(href, { method: 'HEAD' });
            return response.ok;
        } catch {
            return false;
        }
    }
    async updateThemeAttributes() {
        const theme = this.getStoredTheme();
        const contrast = this.getStoredContrast();

        const actualTheme = (!theme || theme === this.prefersSystem)
            ? (this.systemPrefersDark() ? this.prefersDark : this.prefersLight)
            : theme;
        const actualContrast = contrast || this.contrastDefault;

        const cssFile = this.themeCssMap[actualTheme]?.[actualContrast] || this.themeCssMap[actualTheme]?.[this.contrastDefault];
        const themesRoot = this.getThemesRoot();
        const cssHref = `${themesRoot}${cssFile}`;
        const exists = await this.cssFileExists(cssHref);

        await this.updateThemeLink(cssHref, exists);

        // Dispatch theme changed event
        document.dispatchEvent(new CustomEvent('tnt-theme-changed', {
            detail: { theme: actualTheme, contrast: actualContrast }
        }));

        return actualTheme;
    }

    getThemesRoot() {
        let themesRoot = "/Themes";
        const themeToggleEl = document.querySelector('tnt-theme-toggle');
        if (themeToggleEl?.getAttribute('tnt-themes-root')) {
            themesRoot = themeToggleEl.getAttribute('tnt-themes-root');
        }
        return themesRoot.endsWith("/") ? themesRoot : `${themesRoot}/`;
    }

    getThemeLinkAbsoluteHref(cssHref) {
        return new URL(cssHref, window.location.href).href;
    }

    removeCriticalThemeStyles() {
        document.querySelector('style[data-tnt-theme-critical]')?.remove();
    }

    waitForThemeLinkLoad(themeLink) {
        if (!themeLink) {
            return Promise.resolve();
        }

        if (themeLink.getAttribute('data-tnt-theme-loaded') === 'true' || themeLink.sheet) {
            themeLink.setAttribute('data-tnt-theme-loaded', 'true');
            return Promise.resolve();
        }

        return new Promise(resolve => {
            const complete = (status) => {
                themeLink.setAttribute('data-tnt-theme-loaded', status);
                resolve();
            };

            themeLink.addEventListener('load', () => complete('true'), { once: true });
            themeLink.addEventListener('error', () => complete('error'), { once: true });
        });
    }

    async updateThemeLink(cssHref, exists) {
        const themeLink = document.querySelector('link[data-tnt-theme]');

        if (exists) {
            let activeThemeLink = themeLink;
            const newHref = this.getThemeLinkAbsoluteHref(cssHref);

            if (themeLink) {
                // Only update href if different
                if (themeLink.href !== newHref) {
                    themeLink.setAttribute('data-tnt-theme-loaded', 'false');
                    themeLink.href = cssHref;
                }
            } else {
                // Create the link if not present
                const link = document.createElement('link');
                Object.assign(link, {
                    rel: 'stylesheet',
                    href: cssHref
                });
                link.setAttribute('data-tnt-theme', 'true');
                link.setAttribute('data-tnt-theme-loaded', 'false');
                document.head.appendChild(link);
                activeThemeLink = link;
            }

            await this.waitForThemeLinkLoad(activeThemeLink);
            this.removeCriticalThemeStyles();
            document.querySelector('style[data-tnt-theme]')?.remove();
        } else {
            // Remove the link if present and inject fallback styles
            themeLink?.remove();
            this.injectFallbackStyles();
            this.removeCriticalThemeStyles();
        }
    }

    injectFallbackStyles() {
        const style = document.createElement('style');
        style.setAttribute('data-tnt-theme', 'true');
        style.textContent = this.getFallbackCss();
        document.head.appendChild(style);
    }

    getFallbackCss() { return `:root{--tnt-color-primary:rgb(84 90 146);--tnt-color-surface-tint:rgb(84 90 146);--tnt-color-on-primary:rgb(255 255 255);--tnt-color-primary-container:rgb(224 224 255);--tnt-color-on-primary-container:rgb(60 66 121);--tnt-color-secondary:rgb(92 93 114);--tnt-color-on-secondary:rgb(255 255 255);--tnt-color-secondary-container:rgb(225 224 249);--tnt-color-on-secondary-container:rgb(68 69 89);--tnt-color-tertiary:rgb(120 83 107);--tnt-color-on-tertiary:rgb(255 255 255);--tnt-color-tertiary-container:rgb(255 215 239);--tnt-color-on-tertiary-container:rgb(94 60 83);--tnt-color-error:rgb(186 26 26);--tnt-color-on-error:rgb(255 255 255);--tnt-color-error-container:rgb(255 218 214);--tnt-color-on-error-container:rgb(147 0 10);--tnt-color-background:rgb(251 248 255);--tnt-color-on-background:rgb(27 27 33);--tnt-color-surface:rgb(251 248 255);--tnt-color-on-surface:rgb(27 27 33);--tnt-color-surface-variant:rgb(227 225 236);--tnt-color-on-surface-variant:rgb(70 70 79);--tnt-color-outline:rgb(119 118 128);--tnt-color-outline-variant:rgb(199 197 208);--tnt-color-shadow:rgb(0 0 0);--tnt-color-scrim:rgb(0 0 0);--tnt-color-inverse-surface:rgb(48 48 54);--tnt-color-inverse-on-surface:rgb(242 239 247);--tnt-color-inverse-primary:rgb(189 194 255);--tnt-color-primary-fixed:rgb(224 224 255);--tnt-color-on-primary-fixed:rgb(15 21 75);--tnt-color-primary-fixed-dim:rgb(189 194 255);--tnt-color-on-primary-fixed-variant:rgb(60 66 121);--tnt-color-secondary-fixed:rgb(225 224 249);--tnt-color-on-secondary-fixed:rgb(24 26 44);--tnt-color-secondary-fixed-dim:rgb(196 196 221);--tnt-color-on-secondary-fixed-variant:rgb(68 69 89);--tnt-color-tertiary-fixed:rgb(255 215 239);--tnt-color-on-tertiary-fixed:rgb(46 17 38);--tnt-color-tertiary-fixed-dim:rgb(231 185 213);--tnt-color-on-tertiary-fixed-variant:rgb(94 60 83);--tnt-color-surface-dim:rgb(219 217 224);--tnt-color-surface-bright:rgb(251 248 255);--tnt-color-surface-container-lowest:rgb(255 255 255);--tnt-color-surface-container-low:rgb(245 242 250);--tnt-color-surface-container:rgb(239 237 244);--tnt-color-surface-container-high:rgb(234 231 239);--tnt-color-surface-container-highest:rgb(228 225 233);--tnt-color-info:rgb(67 94 145);--tnt-color-on-info:rgb(255 255 255);--tnt-color-info-container:rgb(215 226 255);--tnt-color-on-info-container:rgb(42 70 119);--tnt-color-success:rgb(49 106 66);--tnt-color-on-success:rgb(255 255 255);--tnt-color-success-container:rgb(179 241 190);--tnt-color-on-success-container:rgb(22 81 44);--tnt-color-warning:rgb(111 93 13);--tnt-color-on-warning:rgb(255 255 255);--tnt-color-warning-container:rgb(251 225 134);--tnt-color-on-warning-container:rgb(85 69 0);--tnt-color-assert:rgb(124 78 126);--tnt-color-on-assert:rgb(255 255 255);--tnt-color-assert-container:rgb(255 214 252);--tnt-color-on-assert-container:rgb(98 55 101);}`; }

    // Handles the new combined theme-contrast select
    async themeContrastSelected(e) {
        if (!e?.target) return;

        const value = e.target.value;
        // Value format: THEME-CONTRAST (e.g., DARK-HIGH, SYSTEM-DEFAULT)
        const [rawTheme = this.prefersSystem, rawContrast = this.contrastDefault] = value.split('-');

        // Validate and sanitize the values
        const theme = this.validateTheme(rawTheme);
        const contrast = this.validateContrast(rawContrast);

        // Store validated values
        this.setStoredTheme(theme);
        this.setStoredContrast(contrast);

        const currentTheme = await this.updateThemeAttributes();
        const parentElement = e.target.parentElement;

        if (parentElement) {
            parentElement.updateIcon?.(currentTheme);
            parentElement.initSelect?.(currentTheme);
        }
    }    // Called when the attribute changes (for Blazor interop)
    attributeChangedCallback(name, oldValue, newValue) {
        if (name !== (window.NTComponents && window.NTComponents.customAttribute ? window.NTComponents.customAttribute : 'tnt-theme-toggle')) {
            return;
        }
        // Clean up any invalid stored values first
        this.cleanupInvalidStoredValues();

        // updateThemeAttributes returns a promise
        this.updateThemeAttributes().then(currentTheme => {
            this.updateIcon(currentTheme);
            this.initSelect(currentTheme);
        });
    } updateIcon(currentTheme) {
        const iconElement = this.querySelector('span.tnt-theme-toggle-icon');
        if (iconElement) {
            iconElement.innerHTML = currentTheme === this.prefersDark ? 'light_mode' : 'dark_mode';
        }
    } async initSelect(currentTheme) {
        // For the new combined select
        const theme = this.getStoredTheme() || this.prefersSystem;
        const contrast = this.getStoredContrast() || this.contrastDefault;
        const combinedValue = `${theme}-${contrast}`;

        const comboSelect = this.querySelector('select.tnt-theme-select');
        if (!comboSelect) return;

        comboSelect.removeEventListener('change', this.themeContrastSelected);
        comboSelect.addEventListener('change', this.themeContrastSelected);

        const options = comboSelect.querySelectorAll('option');
        const fallbackValues = [`${theme}-DEFAULT`, 'LIGHT-DEFAULT'];

        // Try to match the full theme-contrast value, then fallbacks
        const valueToSelect = [combinedValue, ...fallbackValues]
            .find(value => Array.from(options).some(opt => opt.value === value)) || combinedValue;

        options.forEach(opt => {
            const isSelected = opt.value === valueToSelect;
            opt.selected = isSelected;
            if (isSelected) {
                opt.setAttribute('selected', true);
            } else {
                opt.removeAttribute('selected');
            }
        });

        // Ensure theme is applied on init
        await this.updateThemeAttributes();
    }
}


// Blazor interop entry points
export function onLoad(element, dotNetElementRef) {
    // Register the custom element only once
    if (!customElements.get('tnt-theme-toggle')) {
        customElements.define('tnt-theme-toggle', TnTThemeToggleElement);
    }
}

export function onUpdate(element, dotNetElementRef) {
    if (element?.updateThemeAttributes) {
        element.updateThemeAttributes().then(currentTheme => {
            element.updateIcon?.(currentTheme);
            element.initSelect?.(currentTheme);
        });
    }
}

export function onDispose(element, dotNetElementRef) {
    // No-op for now
}
