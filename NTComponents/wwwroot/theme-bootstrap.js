(() => {
    if (window.__ntThemeBootstrapped || window.__tntThemeBootstrapped) {
        return;
    }

    window.__ntThemeBootstrapped = true;
    window.__tntThemeBootstrapped = true;

    const runtime = window.NTComponents?.NTThemeRuntime;
    if (!runtime) {
        return;
    }

    const restoreOrApplyTheme = () => {
        runtime.restoreThemeState?.({ waitForLoad: false });
        if (!runtime.hasCurrentThemeState?.()) {
            runtime.apply({ waitForLoad: false });
        }
    };
    restoreOrApplyTheme();

    if (window.Blazor?.addEventListener) {
        window.Blazor.addEventListener('enhancedload', restoreOrApplyTheme);
    } else {
        document.addEventListener('DOMContentLoaded', () => window.Blazor?.addEventListener?.('enhancedload', restoreOrApplyTheme), { once: true });
    }
})();
