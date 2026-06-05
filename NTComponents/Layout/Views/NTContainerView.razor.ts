type ScrollContainer = Window | HTMLElement;

interface HeadingEntry {
    heading: HTMLElement;
    id: string;
    text: string;
}

interface NTContainerViewRegistration {
    activeHeadingId: string;
    clickHandler: (event: MouseEvent) => void;
    headingSignature: string;
    headings: HeadingEntry[];
    navItems: HTMLElement[];
    observer: MutationObserver;
    pendingForceActiveUpdate: boolean;
    resizeHandler: () => void;
    scheduled: boolean;
    scrollContainer: ScrollContainer | null;
    scrollHandler: () => void;
    scrollUpdateScheduled: boolean;
    viewportListenersAttached: boolean;
}

const containerViewSelector = '[data-nt-container-view]';
const viewSelector = '[data-nt-container-view][data-nt-container-view-quick-nav-enabled="true"]';
const quickNavSelector = '[data-nt-container-view-quick-nav]';
const quickNavListSelector = '[data-nt-container-view-quick-nav-list]';
const headingSelector = 'h1, h2, h3, h4, h5, h6';
const withQuickNavClass = 'nt-container-view-with-quick-nav';
const activeLinkClass = 'nt-container-view-quick-nav-link-active';
const registeredViews = new Map<HTMLElement, NTContainerViewRegistration>();
let globalSyncScheduled = false;

function getContainerViews(element?: Element | null): HTMLElement[] {
    if (element instanceof HTMLElement) {
        const view = element.matches(containerViewSelector)
            ? element
            : element.closest<HTMLElement>(containerViewSelector);

        if (view) {
            return [view];
        }

        return Array.from(element.querySelectorAll<HTMLElement>(containerViewSelector));
    }

    return Array.from(document.querySelectorAll<HTMLElement>(containerViewSelector));
}

function getViews(element?: Element | null): HTMLElement[] {
    return getContainerViews(element)
        .filter(view => view.matches(viewSelector));
}

function getHeadingLevel(heading: HTMLElement): number {
    const level = Number.parseInt(heading.tagName.slice(1), 10);
    return Number.isInteger(level) ? level : 1;
}

function getHeadingText(heading: HTMLElement): string {
    return (heading.textContent ?? '').replace(/\s+/g, ' ').trim();
}

function getQuickNavParts(view: HTMLElement): { quickNav: HTMLElement; list: HTMLElement } | null {
    const quickNav = view.querySelector<HTMLElement>(quickNavSelector);
    const list = view.querySelector<HTMLElement>(quickNavListSelector);

    return quickNav && list
        ? { quickNav, list }
        : null;
}

function getScrollContainer(element: HTMLElement): ScrollContainer {
    let current = element.parentElement;

    while (current && current !== document.body && current !== document.documentElement) {
        const style = window.getComputedStyle(current);
        const overflowY = style.overflowY;
        const canScroll = (overflowY === 'auto' || overflowY === 'scroll' || overflowY === 'overlay')
            && current.scrollHeight > current.clientHeight;

        if (canScroll) {
            return current;
        }

        current = current.parentElement;
    }

    return window;
}

function getScrollTop(scrollContainer: ScrollContainer): number {
    return scrollContainer === window
        ? window.scrollY
        : (scrollContainer as HTMLElement).scrollTop;
}

function getHeadingTop(heading: HTMLElement, scrollContainer: ScrollContainer): number {
    if (scrollContainer === window) {
        return heading.getBoundingClientRect().top + window.scrollY;
    }

    const scrollElement = scrollContainer as HTMLElement;

    return heading.getBoundingClientRect().top
        - scrollElement.getBoundingClientRect().top
        + scrollElement.scrollTop;
}

function getFocusableHeadingIdFromLink(link: HTMLAnchorElement): string {
    try {
        return decodeURIComponent(link.hash.slice(1));
    }
    catch {
        return link.hash.slice(1);
    }
}

function slugify(value: string): string {
    return value
        .toLowerCase()
        .normalize('NFKD')
        .replace(/[\u0300-\u036f]/g, '')
        .replace(/[^a-z0-9]+/g, '-')
        .replace(/^-+|-+$/g, '');
}

function getUniqueId(baseId: string, heading: HTMLElement): string {
    let id = baseId;
    let suffix = 2;

    while (true) {
        const existing = document.getElementById(id);

        if (!existing || existing === heading) {
            return id;
        }

        id = `${baseId}-${suffix}`;
        suffix += 1;
    }
}

function ensureHeadingId(view: HTMLElement, heading: HTMLElement, index: number): string {
    if (heading.id) {
        return heading.id;
    }

    const text = getHeadingText(heading);
    const slug = slugify(text) || `section-${index + 1}`;
    const prefix = view.id ? `${view.id}-` : 'nt-container-heading-';
    const id = getUniqueId(`${prefix}${slug}`, heading);
    heading.id = id;
    return id;
}

function getHeadings(view: HTMLElement, quickNav: HTMLElement): HTMLElement[] {
    return Array.from(view.querySelectorAll<HTMLElement>(headingSelector))
        .filter(heading => !quickNav.contains(heading))
        .filter(heading => heading.closest(containerViewSelector) === view)
        .filter(heading => getHeadingText(heading).length > 0);
}

function getHeadingEntries(view: HTMLElement, quickNav: HTMLElement): HeadingEntry[] {
    return getHeadings(view, quickNav).map((heading, index) => ({
        heading,
        id: ensureHeadingId(view, heading, index),
        text: getHeadingText(heading)
    }));
}

function getHeadingSignature(headings: HeadingEntry[]): string {
    return headings
        .map(entry => `${entry.id}\u001f${entry.text}\u001f${getHeadingLevel(entry.heading)}`)
        .join('\u001e');
}

function getNavItems(list: HTMLElement): HTMLElement[] {
    return Array.from(list.querySelectorAll<HTMLElement>('li'));
}

function chooseActiveHeading(headings: HeadingEntry[], scrollContainer: ScrollContainer): HeadingEntry | null {
    if (headings.length === 0) {
        return null;
    }

    const firstTop = getHeadingTop(headings[0].heading, scrollContainer);
    const currentTop = getScrollTop(scrollContainer) + 1;
    let activeHeading = headings[0];
    let allTopsMatch = true;

    if (firstTop > currentTop) {
        return activeHeading;
    }

    for (let i = 1; i < headings.length; i += 1) {
        const top = getHeadingTop(headings[i].heading, scrollContainer);
        allTopsMatch = allTopsMatch && top === firstTop;

        if (top <= currentTop) {
            activeHeading = headings[i];
        }
        else {
            break;
        }
    }

    return allTopsMatch ? headings[0] : activeHeading;
}

function positionActiveSelector(list: HTMLElement, activeItem: HTMLElement | null): void {
    if (!activeItem) {
        list.style.setProperty('--nt-container-view-quick-nav-active-opacity', '0');
        return;
    }

    list.style.setProperty('--nt-container-view-quick-nav-active-top', `${activeItem.offsetTop}px`);
    list.style.setProperty('--nt-container-view-quick-nav-active-height', `${activeItem.offsetHeight}px`);
    list.style.setProperty('--nt-container-view-quick-nav-active-opacity', '1');
}

function setActiveHeading(view: HTMLElement, heading: HeadingEntry | HTMLElement | null, force = false): void {
    const registration = registeredViews.get(view);
    const parts = getQuickNavParts(view);

    if (!registration || !parts) {
        return;
    }

    const activeHeadingId = heading instanceof HTMLElement ? heading.id : heading?.id ?? '';

    if (!force && registration.activeHeadingId === activeHeadingId) {
        return;
    }

    registration.activeHeadingId = activeHeadingId;
    let activeItem: HTMLElement | null = null;

    registration.navItems.forEach(item => {
        const link = item.querySelector('a');
        const active = item.dataset.ntContainerViewHeadingId === activeHeadingId;

        if (link instanceof HTMLAnchorElement) {
            link.classList.toggle(activeLinkClass, active);

            if (active) {
                link.setAttribute('aria-current', 'location');
            }
            else {
                link.removeAttribute('aria-current');
            }
        }

        if (active) {
            activeItem = item;
        }
    });

    positionActiveSelector(parts.list, activeItem);
}

function updateActiveHeading(view: HTMLElement, force = false): void {
    const parts = getQuickNavParts(view);
    const registration = registeredViews.get(view);

    if (!parts || !registration || parts.quickNav.hidden || !registration.scrollContainer) {
        return;
    }

    setActiveHeading(view, chooseActiveHeading(registration.headings, registration.scrollContainer), force);
}

function scheduleActiveHeadingUpdate(view: HTMLElement, force = false): void {
    const registration = registeredViews.get(view);

    if (!registration) {
        return;
    }

    registration.pendingForceActiveUpdate = registration.pendingForceActiveUpdate || force;

    if (registration.scrollUpdateScheduled) {
        return;
    }

    registration.scrollUpdateScheduled = true;
    const callback = () => {
        registration.scrollUpdateScheduled = false;
        const forceUpdate = registration.pendingForceActiveUpdate;
        registration.pendingForceActiveUpdate = false;

        if (view.isConnected) {
            updateActiveHeading(view, forceUpdate);
        }
    };

    if (typeof requestAnimationFrame === 'function') {
        requestAnimationFrame(callback);
    }
    else {
        callback();
    }
}

function shouldLetBrowserHandleClick(event: MouseEvent): boolean {
    return event.defaultPrevented
        || event.button !== 0
        || event.altKey
        || event.ctrlKey
        || event.metaKey
        || event.shiftKey;
}

function scrollToHeading(heading: HTMLElement, scrollContainer: ScrollContainer): void {
    const reduceMotion = window.matchMedia?.('(prefers-reduced-motion: reduce)')?.matches === true;
    const top = getHeadingTop(heading, scrollContainer);
    const scrollOptions: ScrollToOptions = {
        top,
        behavior: reduceMotion ? 'auto' : 'smooth'
    };

    if (scrollContainer === window) {
        window.scrollTo(scrollOptions);
    }
    else if (typeof scrollContainer.scrollTo === 'function') {
        scrollContainer.scrollTo(scrollOptions);
    }
    else {
        (scrollContainer as HTMLElement).scrollTop = top;
    }
}

function handleQuickNavClick(view: HTMLElement, event: MouseEvent): void {
    if (shouldLetBrowserHandleClick(event)) {
        return;
    }

    const target = event.target instanceof Element ? event.target : null;
    const link = target?.closest<HTMLAnchorElement>(`${quickNavSelector} a[href^="#"]`);

    if (!(link instanceof HTMLAnchorElement)) {
        return;
    }

    const headingId = getFocusableHeadingIdFromLink(link);
    const registration = registeredViews.get(view);
    const headingEntry = registration?.headings.find(entry => entry.id === headingId);
    const heading = headingEntry?.heading ?? document.getElementById(headingId);

    if (!(heading instanceof HTMLElement)) {
        return;
    }

    event.preventDefault();
    history.pushState(null, '', `#${encodeURIComponent(headingId)}`);
    setActiveHeading(view, headingEntry ?? heading);
    scrollToHeading(heading, registration?.scrollContainer ?? getScrollContainer(view));
}

function attachViewportListeners(view: HTMLElement): void {
    const registration = registeredViews.get(view);

    if (!registration) {
        return;
    }

    if (!registration.viewportListenersAttached) {
        window.addEventListener('resize', registration.resizeHandler);
        registration.viewportListenersAttached = true;
    }

    syncScrollContainer(view);
}

function detachScrollContainer(registration: NTContainerViewRegistration): void {
    if (!registration.scrollContainer) {
        return;
    }

    registration.scrollContainer.removeEventListener('scroll', registration.scrollHandler);
    registration.scrollContainer = null;
}

function detachViewportListeners(registration: NTContainerViewRegistration): void {
    detachScrollContainer(registration);

    if (registration.viewportListenersAttached) {
        window.removeEventListener('resize', registration.resizeHandler);
        registration.viewportListenersAttached = false;
    }
}

function syncScrollContainer(view: HTMLElement): void {
    const registration = registeredViews.get(view);

    if (!registration) {
        return;
    }

    const scrollContainer = getScrollContainer(view);

    if (registration.scrollContainer === scrollContainer) {
        return;
    }

    detachScrollContainer(registration);
    scrollContainer.addEventListener('scroll', registration.scrollHandler, { passive: true });
    registration.scrollContainer = scrollContainer;
}

function observeView(view: HTMLElement): void {
    const registration = registeredViews.get(view);

    if (!registration) {
        return;
    }

    registration.observer.observe(view, {
        attributes: true,
        attributeFilter: ['id'],
        characterData: true,
        childList: true,
        subtree: true
    });
}

function renderQuickNav(view: HTMLElement): void {
    const registration = registeredViews.get(view);
    const parts = getQuickNavParts(view);

    if (!registration || !parts) {
        disposeView(view);
        return;
    }

    const headings = getHeadingEntries(view, parts.quickNav);
    const headingSignature = getHeadingSignature(headings);
    const hasHeadings = headings.length > 0;

    registration.headings = headings;

    if (registration.headingSignature !== headingSignature) {
        parts.list.replaceChildren(...headings.map(heading => {
            const item = document.createElement('li');
            const link = document.createElement('a');

            item.dataset.ntContainerViewHeadingId = heading.id;
            link.setAttribute('href', `#${encodeURIComponent(heading.id)}`);
            link.textContent = heading.text;
            item.append(link);

            return item;
        }));
        registration.headingSignature = headingSignature;
        registration.navItems = getNavItems(parts.list);
        registration.activeHeadingId = '';
    }

    parts.quickNav.hidden = !hasHeadings;
    view.classList.toggle(withQuickNavClass, hasHeadings);

    if (hasHeadings) {
        attachViewportListeners(view);
        updateActiveHeading(view, true);
    }
    else {
        detachViewportListeners(registration);
        positionActiveSelector(parts.list, null);
    }
}

function syncView(view: HTMLElement): void {
    const registration = registeredViews.get(view);
    registration?.observer.disconnect();

    renderQuickNav(view);

    if (registeredViews.has(view)) {
        observeView(view);
    }
}

function scheduleViewSync(view: HTMLElement): void {
    const registration = registeredViews.get(view);

    if (!registration || registration.scheduled) {
        return;
    }

    registration.scheduled = true;
    queueMicrotask(() => {
        registration.scheduled = false;

        if (view.isConnected) {
            syncView(view);
        }
    });
}

function enhanceView(view: HTMLElement): void {
    if (!getQuickNavParts(view)) {
        disposeView(view);
        return;
    }

    if (!registeredViews.has(view)) {
        const observer = new MutationObserver(() => scheduleViewSync(view));
        const clickHandler = (event: MouseEvent) => handleQuickNavClick(view, event);
        const scrollHandler = () => scheduleActiveHeadingUpdate(view);
        const resizeHandler = () => scheduleActiveHeadingUpdate(view, true);

        view.addEventListener('click', clickHandler);
        registeredViews.set(view, {
            activeHeadingId: '',
            clickHandler,
            headingSignature: '',
            headings: [],
            navItems: [],
            observer,
            pendingForceActiveUpdate: false,
            resizeHandler,
            scheduled: false,
            scrollContainer: null,
            scrollHandler,
            scrollUpdateScheduled: false,
            viewportListenersAttached: false
        });
    }

    syncView(view);
}

function disposeView(view: HTMLElement): void {
    const registration = registeredViews.get(view);

    if (!registration) {
        return;
    }

    registration.observer.disconnect();
    view.removeEventListener('click', registration.clickHandler);
    detachViewportListeners(registration);
    registeredViews.delete(view);
    view.classList.remove(withQuickNavClass);
}

function pruneDisconnectedViews(): void {
    registeredViews.forEach((_, view) => {
        if (!view.isConnected) {
            disposeView(view);
        }
    });
}

function sync(element?: Element | null): void {
    pruneDisconnectedViews();
    getContainerViews(element)
        .filter(view => !view.matches(viewSelector))
        .forEach(disposeView);
    getViews(element).forEach(enhanceView);
}

function scheduleGlobalSync(): void {
    if (globalSyncScheduled) {
        return;
    }

    globalSyncScheduled = true;
    queueMicrotask(() => {
        globalSyncScheduled = false;
        sync();
    });
}

export function onLoad(element?: Element | null): void {
    sync(element);
}

export function onUpdate(element?: Element | null): void {
    if (element == null) {
        scheduleGlobalSync();
        return;
    }

    sync(element);
}

export function onDispose(element?: Element | null): void {
    getContainerViews(element).forEach(disposeView);
    pruneDisconnectedViews();
}
