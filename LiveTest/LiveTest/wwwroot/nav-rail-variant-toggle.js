document.addEventListener('change', event => {
    if (!(event.target instanceof HTMLInputElement) || !event.target.matches('[data-livetest-nav-rail-variant-toggle]')) {
        return;
    }

    event.target.closest('.nt-navigation-rail')?.classList.toggle('nt-navigation-rail-square', event.target.checked);
});
