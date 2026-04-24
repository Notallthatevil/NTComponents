window.ntDocs = window.ntDocs || {};

window.ntDocs.scrollToFragment = (fragmentId, updateHistory) => {
    if (!fragmentId) {
        return;
    }

    const normalizedId = fragmentId.replace(/^#/, "");
    const target = document.getElementById(normalizedId);
    if (!target) {
        return;
    }

    target.scrollIntoView({
        behavior: "smooth",
        block: "start"
    });

    if (updateHistory) {
        const baseUrl = window.location.pathname + window.location.search;
        window.history.replaceState(null, "", `${baseUrl}#${normalizedId}`);
    }
};
