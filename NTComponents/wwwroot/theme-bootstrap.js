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

    runtime.restoreThemeState?.({ waitForLoad: false });
    runtime.apply({ waitForLoad: false });

    const applyAfterEnhancedLoad = () => {
        runtime.restoreThemeState?.({ waitForLoad: false });
        runtime.apply({ waitForLoad: false });
    };
    if (window.Blazor?.addEventListener) {
        window.Blazor.addEventListener('enhancedload', applyAfterEnhancedLoad);
    } else {
        document.addEventListener('DOMContentLoaded', () => window.Blazor?.addEventListener?.('enhancedload', applyAfterEnhancedLoad), { once: true });
    }
})();
