class TnTThemeToggleElement extends HTMLElement {
    static observedAttributes = [window.NTComponents && window.NTComponents.customAttribute ? window.NTComponents.customAttribute : 'tnt-theme-toggle'];

    constructor() {
        super();
        this.themeContrastSelected = this.themeContrastSelected.bind(this);
    }

    themeStorageKey = 'NTComponentsStoredThemeKey';
    contrastStorageKey = 'NTComponentsStoredContrastKey';
    prefersDark = 'DARK';
    prefersLight = 'LIGHT';
    prefersSystem = 'SYSTEM';
    contrastDefault = 'DEFAULT';
    contrastMedium = 'MEDIUM';
    contrastHigh = 'HIGH';

    get runtime() {
        return window.NTComponents?.NTThemeRuntime;
    }

    get themeCssMap() {
        const config = this.runtime?.getConfiguration(this) || {};
        return {
            LIGHT: {
                DEFAULT: config.lightDefaultCss || 'light.css',
                MEDIUM: config.lightMediumCss || 'light-mc.css',
                HIGH: config.lightHighCss || 'light-hc.css',
            },
            DARK: {
                DEFAULT: config.darkDefaultCss || 'dark.css',
                MEDIUM: config.darkMediumCss || 'dark-mc.css',
                HIGH: config.darkHighCss || 'dark-hc.css',
            }
        };
    }

    connectedCallback() {
        this.runtime?.registerControl(this);
    }

    disconnectedCallback() {
        this.runtime?.unregisterControl(this);
        this.querySelector('select.tnt-theme-select')?.removeEventListener('change', this.themeContrastSelected);
    }

    getConfiguration() {
        return this.runtime?.getConfiguration(this) || {};
    }

    getStoredTheme() {
        return this.runtime?.getStoredTheme(this.getConfiguration()) || null;
    }

    setStoredTheme(theme) {
        const config = this.getConfiguration();
        this.runtime?.safeSetStorage(config.themeStorageKey || this.themeStorageKey, this.validateTheme(theme));
    }

    getStoredContrast() {
        return this.runtime?.getStoredContrast(this.getConfiguration()) || null;
    }

    setStoredContrast(contrast) {
        const config = this.getConfiguration();
        this.runtime?.safeSetStorage(config.contrastStorageKey || this.contrastStorageKey, this.validateContrast(contrast));
    }

    validateTheme(theme) {
        return this.runtime?.normalizeTheme(theme, this.prefersSystem) || this.prefersSystem;
    }

    validateContrast(contrast) {
        return this.runtime?.normalizeContrast(contrast, this.contrastDefault) || this.contrastDefault;
    }

    cleanupInvalidStoredValues() {
        this.runtime?.cleanupInvalidStoredValues(this.getConfiguration());
    }

    systemPrefersDark() {
        return this.runtime?.systemPrefersDark() || false;
    }

    async cssFileExists(href) {
        try {
            const response = await fetch(href, { method: 'HEAD' });
            return response.ok;
        } catch {
            return false;
        }
    }

    getThemesRoot() {
        const themesRoot = this.getConfiguration().themesRoot || '/Themes';
        return themesRoot.endsWith('/') ? themesRoot : `${themesRoot}/`;
    }

    removeCriticalThemeStyles() {
        this.runtime?.removeCriticalThemeStyles();
    }

    async updateThemeAttributes() {
        if (!this.runtime) {
            return this.prefersLight;
        }

        const result = await this.runtime.apply({ element: this, waitForLoad: true });
        return result.theme;
    }

    updateIcon(currentTheme) {
        const iconElement = this.querySelector('span.tnt-theme-toggle-icon');
        if (iconElement) {
            iconElement.innerHTML = currentTheme === this.prefersDark ? 'light_mode' : 'dark_mode';
        }
    }

    async initSelect(currentTheme, options = {}) {
        const config = this.getConfiguration();
        const theme = this.getStoredTheme() || config.defaultTheme || this.prefersSystem;
        const contrast = this.getStoredContrast() || config.defaultContrast || this.contrastDefault;
        const combinedValue = `${this.validateTheme(theme)}-${this.validateContrast(contrast)}`;

        const comboSelect = this.querySelector('select.tnt-theme-select');
        if (!comboSelect) {
            return;
        }

        comboSelect.removeEventListener('change', this.themeContrastSelected);
        comboSelect.addEventListener('change', this.themeContrastSelected);

        const optionsList = Array.from(comboSelect.querySelectorAll('option'));
        const fallbackValues = [`${this.validateTheme(theme)}-DEFAULT`, 'LIGHT-DEFAULT'];
        const valueToSelect = [combinedValue, ...fallbackValues].find(value => optionsList.some(opt => opt.value === value)) || combinedValue;

        optionsList.forEach(opt => {
            const isSelected = opt.value === valueToSelect;
            opt.selected = isSelected;
            if (isSelected) {
                opt.setAttribute('selected', 'true');
            } else {
                opt.removeAttribute('selected');
            }
        });

        if (!options.skipApply && currentTheme) {
            this.updateIcon(currentTheme);
        }
    }

    getFallbackCss() {
        return this.runtime?.getFallbackCss() || '';
    }

    injectFallbackStyles() {
        this.runtime?.injectFallbackStyles();
    }

    async updateThemeLink(cssHref, exists = true) {
        if (!this.runtime) {
            return null;
        }

        if (!exists) {
            const current = document.querySelector('link[data-nt-theme],link[data-tnt-theme]');

            if (!current) {
                this.injectFallbackStyles();
                this.removeCriticalThemeStyles();
            }

            return current;
        }

        return await this.runtime.applyStylesheet(new URL(cssHref, window.location.href).href, { waitForLoad: true });
    }

    async themeContrastSelected(e) {
        if (!e?.target) {
            return;
        }

        const [rawTheme = this.prefersSystem, rawContrast = this.contrastDefault] = e.target.value.split('-');
        const result = await this.runtime?.apply({
            element: this,
            theme: this.validateTheme(rawTheme),
            contrast: this.validateContrast(rawContrast),
            waitForLoad: true
        });

        if (result) {
            this.updateIcon(result.theme);
            await this.initSelect(result.theme, { skipApply: true });
        }
    }

    attributeChangedCallback(name) {
        if (name !== (window.NTComponents && window.NTComponents.customAttribute ? window.NTComponents.customAttribute : 'tnt-theme-toggle')) {
            return;
        }

        this.cleanupInvalidStoredValues();
        this.updateThemeAttributes().then(currentTheme => {
            this.updateIcon(currentTheme);
            this.initSelect(currentTheme, { skipApply: true });
        });
    }
}

export function onLoad(element, dotNetElementRef) {
    if (!customElements.get('tnt-theme-toggle')) {
        customElements.define('tnt-theme-toggle', TnTThemeToggleElement);
    }
}

export function onUpdate(element, dotNetElementRef) {
    if (element?.updateThemeAttributes) {
        element.updateThemeAttributes().then(currentTheme => {
            element.updateIcon?.(currentTheme);
            element.initSelect?.(currentTheme, { skipApply: true });
        });
    }
}

export function onDispose(element, dotNetElementRef) {
    element?.runtime?.unregisterControl(element);
}
