const pageScriptInfoBySrc = new Map();
const enhancedLoadBlazorInstances = new WeakSet();
const richTextEditorToolRegistry = {
    tools: new Map(),
    onChange: null
};
let rippleHandlersRegistered = false;
let bodyFillObserverStarted = false;
const rippleReleaseTimeoutByHost = new WeakMap();
const buttonInteractionRegistrationByElement = new WeakMap();

export function setRichTextEditorToolRegistryChangedCallback(callback) {
    richTextEditorToolRegistry.onChange = callback;
}

export function registerRichTextEditorTool(tool) {
    richTextEditorToolRegistry.tools.set(tool.command, tool);
    richTextEditorToolRegistry.onChange?.();
    return tool;
}

export function getRichTextEditorTool(command) {
    return richTextEditorToolRegistry.tools.get(command) ?? null;
}

export function getRichTextEditorToolState(editorState, tool) {
    const existingState = editorState.toolStates.get(tool.command);
    if (existingState) {
        return existingState;
    }

    const state = tool.createState();
    editorState.toolStates.set(tool.command, state);
    return state;
}

export function createRichTextEditorToolContext(element, editorState, host, tool) {
    return {
        element,
        editorState,
        host,
        toolState: getRichTextEditorToolState(editorState, tool)
    };
}

function registerPageScriptElement(src, element) {
    if (!src) {
        throw new Error('Must provide a non-empty value for the "src" attribute.');
    }

    let pageScriptInfo = pageScriptInfoBySrc.get(src);

    if (pageScriptInfo) {
        pageScriptInfo.referenceCount++;
        pageScriptInfo.elements.add(element);
        pageScriptInfo.module?.onUpdate?.(element);
    } else {
        pageScriptInfo = { referenceCount: 1, module: null, elements: new Set([element]) };
        pageScriptInfoBySrc.set(src, pageScriptInfo);
        initializePageScriptModule(src, pageScriptInfo);
    }
}

function unregisterPageScriptElement(src, element) {
    if (!src) {
        return;
    }

    const pageScriptInfo = pageScriptInfoBySrc.get(src);
    if (!pageScriptInfo) {
        return;
    }

    pageScriptInfo.referenceCount--;
    pageScriptInfo.elements.delete(element);
    pageScriptInfo.module?.onDispose?.(element);
}

async function initializePageScriptModule(src, pageScriptInfo) {
    // If the path is relative, normalize it by by making it an absolute URL
    // with document's the base HREF.
    if (src.startsWith("./")) {
        src = new URL(src.substr(2), document.baseURI).toString();
    }

    const module = await import(src);

    if (pageScriptInfo.referenceCount <= 0) {
        // All page-script elements with the same 'src' were
        // unregistered while we were loading the module.
        return;
    }

    pageScriptInfo.module = module;
    for (const element of pageScriptInfo.elements) {
        module.onLoad?.(element);
        module.onUpdate?.(element);
    }
}

function onEnhancedLoad() {
    // Start by invoking 'onDispose' on any modules that are no longer referenced.
    for (const [src, { module, referenceCount }] of pageScriptInfoBySrc) {
        if (referenceCount <= 0) {
            module?.onDispose?.();
            pageScriptInfoBySrc.delete(src);
        }
    }

    // Then invoke 'onUpdate' on the remaining modules.
    for (const { elements, module } of pageScriptInfoBySrc.values()) {
        for (const element of elements) {
            module?.onUpdate?.(element);
        }
    }

    window.NTComponents?.setupRipple?.();
    enhanceAutocompleteInputs();
}

function enhanceAutocompleteInputs() {
    if (!document.querySelector('[data-nt-autocomplete-input="true"]')) {
        return;
    }

    import('./FormV2/NTAutocomplete.razor.js')
        .then(module => module.enhanceAll?.(document))
        .catch(() => { });
}

function getRippleHost(element) {
    return element?.querySelector?.(':scope > .nt-button-ripple-host')
        ?? element?.querySelector?.(':scope .nt-navigation-rail-item-ripple.nt-button-ripple-host')
        ?? null;
}

function getRippleContainer(element) {
    const host = getRippleHost(element);
    if (host?.classList?.contains('nt-navigation-rail-item-ripple')) {
        return host.parentElement ?? element ?? null;
    }

    return element ?? null;
}

function clearRippleReleaseTimeout(host) {
    const existingTimeout = rippleReleaseTimeoutByHost.get(host);
    if (existingTimeout) {
        clearTimeout(existingTimeout);
        rippleReleaseTimeoutByHost.delete(host);
    }
}

function releaseRippleHost(host) {
    if (!host) {
        return;
    }

    clearRippleReleaseTimeout(host);

    const ripples = host.querySelectorAll(':scope > .nt-button-ripple');
    if (!ripples.length) {
        return;
    }

    ripples.forEach((ripple) => {
        ripple.classList.add('nt-button-ripple-releasing');
    });

    const timeoutId = setTimeout(() => {
        ripples.forEach((ripple) => ripple.remove());
        rippleReleaseTimeoutByHost.delete(host);
    }, 600);

    rippleReleaseTimeoutByHost.set(host, timeoutId);
}

function createRippleAtPosition(element, x, y) {
    const host = getRippleHost(element);
    const container = getRippleContainer(element);

    if (!host || !container || element.disabled) {
        return false;
    }

    clearRippleReleaseTimeout(host);

    const width = container.offsetWidth || 0;
    const height = container.offsetHeight || 0;
    const rippleElement = document.createElement('span');

    rippleElement.classList.add('nt-button-ripple');
    rippleElement.style.pointerEvents = 'none';
    rippleElement.style.setProperty('--nt-button-ripple-origin-x', `${x}px`);
    rippleElement.style.setProperty('--nt-button-ripple-origin-y', `${y}px`);
    rippleElement.style.setProperty('--nt-button-ripple-width', `${width * 2}px`);
    rippleElement.style.setProperty('--nt-button-ripple-height', `${height * 2}px`);
    host.appendChild(rippleElement);

    void rippleElement.offsetWidth;

    setTimeout(() => {
        rippleElement.classList.add('nt-button-ripple-active');
    }, 1);

    return true;
}

function createPointerRipple(event, element) {
    const container = getRippleContainer(element);
    if (!container) {
        return false;
    }

    const coords = getCoords(container);
    const pageX = event.pageX ?? (event.clientX + window.scrollX);
    const pageY = event.pageY ?? (event.clientY + window.scrollY);
    const x = pageX - coords.left;
    const y = pageY - coords.top;
    return createRippleAtPosition(element, x, y);
}

function createCenteredRipple(element) {
    const container = getRippleContainer(element);
    if (!container) {
        return false;
    }

    const width = container.offsetWidth || 0;
    const height = container.offsetHeight || 0;
    return createRippleAtPosition(element, width / 2, height / 2);
}

function findRippleElementFromEventTarget(target) {
    return target?.closest?.('.tnt-ripple') ?? null;
}

function isKeyboardRippleEvent(event) {
    return !event.repeat && (event.key === ' ' || event.key === 'Enter' || event.key === 'Spacebar');
}

function handleDelegatedRipplePointerDown(event) {
    const element = findRippleElementFromEventTarget(event.target);

    if (!element) {
        return;
    }

    createPointerRipple(event, element);
}

function handleDelegatedRipplePointerUp(event) {
    const element = findRippleElementFromEventTarget(event.target);

    if (element) {
        releaseRippleHost(getRippleHost(element));
        return;
    }

    document.querySelectorAll('.nt-button-ripple-host').forEach((host) => {
        releaseRippleHost(host);
    });
}

function handleDelegatedRippleKeyDown(event) {
    const element = findRippleElementFromEventTarget(event.target);
    if (!element || !isKeyboardRippleEvent(event)) {
        return;
    }

    createCenteredRipple(element);
}

function handleDelegatedRippleKeyUp(event) {
    const element = findRippleElementFromEventTarget(event.target);
    if (!element || !isKeyboardRippleEvent(event)) {
        return;
    }

    releaseRippleHost(getRippleHost(element));
}

function ensureRippleHandlers() {
    if (rippleHandlersRegistered) {
        return;
    }

    rippleHandlersRegistered = true;
    if (window.PointerEvent) {
        document.addEventListener('pointerdown', handleDelegatedRipplePointerDown);
        document.addEventListener('pointerup', handleDelegatedRipplePointerUp);
        document.addEventListener('pointercancel', handleDelegatedRipplePointerUp);
        document.addEventListener('pointerleave', handleDelegatedRipplePointerUp);
    } else {
        document.addEventListener('mousedown', handleDelegatedRipplePointerDown);
        document.addEventListener('mouseup', handleDelegatedRipplePointerUp);
        document.addEventListener('mouseleave', handleDelegatedRipplePointerUp);
    }

    document.addEventListener('keydown', handleDelegatedRippleKeyDown);
    document.addEventListener('keyup', handleDelegatedRippleKeyUp);
    window.addEventListener('blur', () => {
        document.querySelectorAll('.nt-button-ripple-host').forEach((host) => {
            releaseRippleHost(host);
        });
    });
}

function getRippleRegistrationElement(host) {
    return host?.closest?.('button, a, [role="button"], [tabindex]')
        ?? host?.parentElement
        ?? null;
}

function setPressedShape(element, isPressed) {
    element?.classList?.toggle('nt-button--pressed-shape', isPressed);
}

function registerButtonInteraction(element) {
    if (!element || buttonInteractionRegistrationByElement.has(element)) {
        return;
    }

    const onPointerDown = (event) => {
        setPressedShape(element, true);
        if (getRippleHost(element)) {
            createPointerRipple(event, element);
        }
    };

    const onPointerUp = () => {
        releaseRippleHost(getRippleHost(element));
    };

    const onKeyDown = (event) => {
        if (!isKeyboardRippleEvent(event)) {
            return;
        }

        setPressedShape(element, true);
        if (getRippleHost(element)) {
            createCenteredRipple(element);
        }
    };

    const onKeyUp = (event) => {
        if (!isKeyboardRippleEvent(event)) {
            return;
        }

        setPressedShape(element, false);
        releaseRippleHost(getRippleHost(element));
    };

    const onPointerExit = () => {
        setPressedShape(element, false);
        releaseRippleHost(getRippleHost(element));
    };

    const onBlur = () => {
        setPressedShape(element, false);
        releaseRippleHost(getRippleHost(element));
    };

    if (window.PointerEvent) {
        element.addEventListener('pointerdown', onPointerDown);
        element.addEventListener('pointerup', onPointerUp);
        element.addEventListener('pointercancel', onPointerExit);
        element.addEventListener('pointerleave', onPointerExit);
    } else {
        element.addEventListener('mousedown', onPointerDown);
        element.addEventListener('mouseup', onPointerUp);
        element.addEventListener('mouseleave', onPointerExit);
        element.addEventListener('touchstart', onPointerDown, { passive: true });
        element.addEventListener('touchend', onPointerExit);
        element.addEventListener('touchcancel', onPointerExit);
    }

    element.addEventListener('keydown', onKeyDown);
    element.addEventListener('keyup', onKeyUp);
    element.addEventListener('blur', onBlur);

    buttonInteractionRegistrationByElement.set(element, {
        onBlur,
        onKeyDown,
        onKeyUp,
        onPointerDown,
        onPointerExit,
        onPointerUp
    });
}

function registerButtonInteractions(element) {
    if (!element) {
        return;
    }

    const buttonSelector = '.nt-button, .nt-icon-button, .nt-btn-grp-btn, .nt-split-button-segment';

    if (element.matches?.(buttonSelector)) {
        registerButtonInteraction(element);
    }

    element.querySelectorAll?.(buttonSelector).forEach((button) => {
        registerButtonInteraction(button);
    });
}

function registerRippleHost(host) {
    const element = getRippleRegistrationElement(host);
    registerButtonInteraction(element);
}

function startButtonInteraction(script) {
    const element = script?.previousElementSibling;
    if (!element) {
        return;
    }

    let attempts = 0;
    const tryRegister = () => {
        if (typeof window.NTComponents?.registerButtonInteractions === 'function') {
            window.NTComponents.registerButtonInteractions(element);
            return;
        }

        if (attempts++ < 20) {
            setTimeout(tryRegister, 0);
        }
    };

    tryRegister();
}

function startRippleHost(script) {
    const host = script?.previousElementSibling;
    if (!host) {
        return;
    }

    let attempts = 0;
    const tryRegister = () => {
        if (typeof window.NTComponents?.registerRippleHost === 'function') {
            window.NTComponents.registerRippleHost(host);
            return;
        }

        if (attempts++ < 20) {
            setTimeout(tryRegister, 0);
        }
    };

    tryRegister();
}

function setupPageScriptElement() {
    if (customElements.get('tnt-page-script')) {
        return;
    }

    customElements.define('tnt-page-script', class extends HTMLElement {
        static observedAttributes = ['src'];

        pendingUnregisterTimeout = null;
        registeredSrc = null;

        connectedCallback() {
            this.cancelPendingUnregister();
            this.registerSrc(this.src);
        }

        attributeChangedCallback(name, oldValue, newValue) {
            if (name !== 'src') {
                return;
            }

            this.src = newValue;

            if (!this.isConnected || oldValue === newValue) {
                return;
            }

            this.unregisterSrc(oldValue);
            this.registerSrc(newValue);
        }

        disconnectedCallback() {
            this.scheduleUnregister(this.registeredSrc);
        }

        registerSrc(src) {
            this.cancelPendingUnregister();

            if (!src || this.registeredSrc === src) {
                return;
            }

            if (this.registeredSrc) {
                this.unregisterSrc(this.registeredSrc);
            }

            this.registeredSrc = src;
            registerPageScriptElement(src, this);
        }

        cancelPendingUnregister() {
            if (this.pendingUnregisterTimeout === null) {
                return;
            }

            clearTimeout(this.pendingUnregisterTimeout);
            this.pendingUnregisterTimeout = null;
        }

        scheduleUnregister(src) {
            this.cancelPendingUnregister();

            if (!src) {
                return;
            }

            this.pendingUnregisterTimeout = setTimeout(() => {
                this.pendingUnregisterTimeout = null;
                if (!this.isConnected) {
                    this.unregisterSrc(src);
                }
            }, 0);
        }

        unregisterSrc(src) {
            this.cancelPendingUnregister();

            if (!src || this.registeredSrc !== src) {
                return;
            }

            unregisterPageScriptElement(src, this);
            this.registeredSrc = null;
        }
    });
}

function setupEnhancedNavigation(blazor) {
    if (!blazor?.addEventListener || enhancedLoadBlazorInstances.has(blazor)) {
        return;
    }

    enhancedLoadBlazorInstances.add(blazor);
    blazor.addEventListener('enhancedload', onEnhancedLoad);
}

function setupBodyFillRemaining() {
    if (bodyFillObserverStarted) {
        return;
    }

    setupPageScriptElement();

    let body = document.querySelector('.tnt-body');
    if (body) {
        bodyFillObserverStarted = true;
        const bodyPadding = parseInt(getComputedStyle(body).paddingBottom, 10);
        const resizeObserver = new ResizeObserver(entries => {
            const hasFooter = document.querySelector('.tnt-footer');
            const fillRemaining = document.querySelectorAll('.tnt-fill-remaining');

            for (const fills of fillRemaining) {
                if (entries[0].target.scrollHeight > entries[0].target.clientHeight) {
                    break;
                }

                var rect = fills.getBoundingClientRect();
                const style = getComputedStyle(fills);
                let height = window.innerHeight - rect.top - bodyPadding;

                const margin = style.marginBottom;
                if (margin) {
                    height = height - parseInt(margin, 10);
                }

                if (hasFooter) {
                    height = height - hasFooter.getBoundingClientRect().height;
                }

                fills.style.height = `${height}px`
            }

        });
        resizeObserver.observe(body);
    }
}

function syncInputCounter(input, counter) {
    if (!input || !counter) {
        return;
    }

    const maxLength = input.maxLength > 0 ? input.maxLength : input.getAttribute('maxlength');
    if (!maxLength) {
        return;
    }

    const text = `${input.value?.length ?? 0}/${maxLength}`;
    counter.textContent = text;
    counter.setAttribute('aria-label', `Character count ${text}`);
}

export function updateInputCounter(input) {
    const root = input?.closest?.('.nt-input');
    const counter = root?.querySelector?.(':scope .nt-input-counter');
    syncInputCounter(input, counter);
}

function getTextAreaMaxVisibleLines(input) {
    const value = parseInt(input?.dataset?.ntTextareaMaxVisibleLines ?? '', 10);
    return Number.isFinite(value) && value > 0 ? value : null;
}

function getTextAreaMinVisibleLines(input) {
    const value = parseInt(input?.dataset?.ntTextareaMinVisibleLines ?? '', 10);
    return Number.isFinite(value) && value > 0 ? value : null;
}

function parseCssPixelValue(value) {
    const parsed = parseFloat(value);
    return Number.isFinite(parsed) ? parsed : 0;
}

function getTextAreaLineHeight(style) {
    const parsed = parseCssPixelValue(style.lineHeight);
    if (parsed > 0) {
        return parsed;
    }

    const fontSize = parseCssPixelValue(style.fontSize);
    return fontSize > 0 ? fontSize * 1.2 : 20;
}

function getTextAreaVerticalInset(style) {
    return parseCssPixelValue(style.paddingTop)
        + parseCssPixelValue(style.paddingBottom)
        + parseCssPixelValue(style.borderTopWidth)
        + parseCssPixelValue(style.borderBottomWidth);
}

function clearTextAreaAutoGrowInput(input) {
    if (!(input instanceof HTMLTextAreaElement)) {
        return;
    }

    input.style.removeProperty('height');
    input.style.removeProperty('min-height');
    input.style.removeProperty('overflow-y');
    input.closest?.('.nt-input')?.classList?.remove('nt-textarea-enhanced');
}

export function autoGrowTextArea(input) {
    if (!(input instanceof HTMLTextAreaElement) || input.dataset?.ntTextareaAutogrow !== 'true') {
        clearTextAreaAutoGrowInput(input);
        return;
    }

    input.closest?.('.nt-input')?.classList?.add('nt-textarea-enhanced');
    const style = window.getComputedStyle(input);
    const lineHeight = getTextAreaLineHeight(style);
    const verticalInset = getTextAreaVerticalInset(style);
    const minVisibleLines = getTextAreaMinVisibleLines(input);
    const maxVisibleLines = getTextAreaMaxVisibleLines(input);
    const minHeight = minVisibleLines === null ? 0 : Math.ceil(lineHeight * minVisibleLines + verticalInset);
    input.style.height = 'auto';
    input.style.minHeight = '0px';
    const scrollHeight = input.scrollHeight;
    input.style.minHeight = minHeight > 0 ? `${minHeight}px` : '';

    if (maxVisibleLines === null) {
        input.style.height = `${Math.ceil(Math.max(scrollHeight, minHeight))}px`;
        input.style.overflowY = 'hidden';
        return;
    }

    const maxHeight = Math.ceil(lineHeight * maxVisibleLines + verticalInset);
    input.style.height = `${Math.ceil(Math.max(Math.min(scrollHeight, maxHeight), minHeight))}px`;
    input.style.overflowY = scrollHeight > maxHeight ? 'scroll' : 'hidden';
}

function getInputCurrencySymbol(input) {
    const symbol = input?.getAttribute?.('currencySymbol');
    if (symbol) {
        return symbol;
    }

    let cultureCode = input?.getAttribute?.('cultureCode');
    if (!cultureCode) {
        cultureCode = 'en-US';
    }

    let currencyCode = input?.getAttribute?.('currencyCode');
    if (!currencyCode) {
        currencyCode = 'USD';
    }

    try {
        const formatter = new Intl.NumberFormat(cultureCode, {
            style: 'currency',
            currency: currencyCode,
            currencyDisplay: 'narrowSymbol'
        });
        const currencyPart = formatter.formatToParts(0).find(part => part.type === 'currency');
        return currencyPart?.value || currencyCode;
    } catch {
        return currencyCode;
    }
}

function getCurrencyInputOptions(input) {
    const decimalDigits = parseInt(input?.getAttribute?.('currencyDecimalDigits') ?? '2', 10);
    return {
        currencyCode: input?.getAttribute?.('currencyCode') || 'USD',
        decimalDigits: Number.isNaN(decimalDigits) ? 2 : decimalDigits,
        decimalSeparator: input?.getAttribute?.('currencyDecimalSeparator') || '.',
        groupSeparator: input?.getAttribute?.('currencyGroupSeparator') || ',',
        symbol: getInputCurrencySymbol(input)
    };
}

function limitCurrencyFraction(value, options) {
    if (!value || !options.decimalSeparator) {
        return value;
    }

    const decimalIndex = value.indexOf(options.decimalSeparator);
    if (decimalIndex < 0) {
        return value;
    }

    if (options.decimalDigits <= 0) {
        return value.substring(0, decimalIndex);
    }

    const fractionalStart = decimalIndex + options.decimalSeparator.length;
    const maximumLength = fractionalStart + options.decimalDigits;
    return value.length <= maximumLength ? value : value.substring(0, maximumLength);
}

function replaceAllLiteral(value, searchValue, replacementValue) {
    if (!searchValue) {
        return value;
    }

    return value.split(searchValue).join(replacementValue);
}

function toCurrencyDecimalValue(value, options) {
    if (!value) {
        return '';
    }

    let normalized = limitCurrencyFraction(value, options);
    normalized = replaceAllLiteral(normalized, options.symbol, '');
    normalized = replaceAllLiteral(normalized, options.currencyCode, '');
    normalized = normalized.replace(/\s|\u00a0/g, '');
    normalized = replaceAllLiteral(normalized, options.groupSeparator, '');
    if (options.decimalSeparator !== '.') {
        normalized = replaceAllLiteral(normalized, options.decimalSeparator, '.');
    }

    normalized = normalized.replace(/(?!^-)[^0-9.-]/g, '');
    const decimalIndex = normalized.indexOf('.');
    if (decimalIndex >= 0) {
        normalized = `${normalized.substring(0, decimalIndex + 1)}${normalized.substring(decimalIndex + 1).replace(/\./g, '')}`;
    }

    return normalized;
}

function syncCurrencyFormValue(input, options) {
    const container = input?.closest?.('.nt-input-control-container');
    const formValueInput = container?.querySelector?.('input[type="hidden"][name]');
    if (!formValueInput) {
        return;
    }

    formValueInput.value = toCurrencyDecimalValue(input.value, options);
}

export function prefixCurrencyInput(input) {
    if (!input) {
        return;
    }

    const options = getCurrencyInputOptions(input);
    if (input.disabled || input.readOnly) {
        syncCurrencyFormValue(input, options);
        return;
    }

    if (!input.value) {
        syncCurrencyFormValue(input, options);
        return;
    }

    const selectionStart = input.selectionStart;
    const selectionEnd = input.selectionEnd;
    let offset = 0;

    if (options.symbol && !input.value.startsWith(options.symbol)) {
        input.value = `${options.symbol}${input.value}`;
        offset += options.symbol.length;
    }

    const limitedValue = limitCurrencyFraction(input.value, options);
    if (limitedValue !== input.value) {
        input.value = limitedValue;
    }

    syncCurrencyFormValue(input, options);

    if (selectionStart !== null && selectionEnd !== null) {
        input.setSelectionRange(selectionStart + offset, selectionEnd + offset);
    }
}

function getPhoneMask(input) {
    return input?.getAttribute?.('phoneMask') || '(###) ###-####';
}

function countPhoneMaskDigits(mask) {
    return (mask.match(/#/g) || []).length;
}

function getPhoneCountryCodeDigitCount(mask) {
    const trimmedMask = (mask || '').trimStart();
    if (!trimmedMask.startsWith('+')) {
        return 0;
    }

    let digitCount = 0;
    for (const character of trimmedMask.substring(1)) {
        if (character !== '#') {
            break;
        }

        digitCount++;
    }

    return digitCount;
}

export function formatPhoneValue(value, mask = '(###) ###-####') {
    if (!value || !mask || !mask.includes('#')) {
        return value || '';
    }

    const maximumDigits = countPhoneMaskDigits(mask);
    const digits = value.replace(/\D/g, '').substring(0, maximumDigits);
    if (!digits) {
        return '';
    }

    let formatted = '';
    let digitIndex = 0;
    for (const character of mask) {
        if (character === '#') {
            if (digitIndex >= digits.length) {
                break;
            }

            formatted += digits[digitIndex];
            digitIndex++;
            continue;
        }

        if (digitIndex >= digits.length && formatted.length > 0) {
            break;
        }

        formatted += character;
    }

    return formatted;
}

export function normalizePhoneValue(value, mask = '(###) ###-####') {
    if (!value || !mask || !mask.includes('#')) {
        return value || '';
    }

    const maximumDigits = countPhoneMaskDigits(mask);
    const digits = value.replace(/\D/g, '').substring(0, maximumDigits);
    if (!digits) {
        return '';
    }

    const countryCodeDigitCount = getPhoneCountryCodeDigitCount(mask);
    return countryCodeDigitCount > 0 && digits.length > countryCodeDigitCount
        ? `${digits.substring(0, countryCodeDigitCount)} ${digits.substring(countryCodeDigitCount)}`
        : digits;
}

function syncPhoneFormValue(input) {
    const container = input?.closest?.('.nt-input-control-container');
    const formValueInput = container?.querySelector?.('input[type="hidden"][name]');
    if (!formValueInput) {
        return;
    }

    formValueInput.value = normalizePhoneValue(input.value, getPhoneMask(input));
}

export function applyPhoneMaskInput(input) {
    if (!input) {
        return;
    }

    if (input.disabled || input.readOnly) {
        syncPhoneFormValue(input);
        return;
    }

    const formatted = formatPhoneValue(input.value, getPhoneMask(input));
    if (formatted === input.value) {
        syncPhoneFormValue(input);
        return;
    }

    input.value = formatted;
    syncPhoneFormValue(input);
    if (input.selectionStart !== null && input.selectionEnd !== null) {
        input.setSelectionRange(input.value.length, input.value.length);
    }
}

function startBlazorComponents(blazor) {
    setupPageScriptElement();
    setupEnhancedNavigation(blazor);
    window.NTComponents?.setupRipple?.();
    setupBodyFillRemaining();
    enhanceAutocompleteInputs();
}

export function afterWebStarted(blazor) {
    startBlazorComponents(blazor);
}

export function afterStarted(blazor) {
    startBlazorComponents(blazor);
}
function getCoords(elem) { // crossbrowser version
    var box = elem.getBoundingClientRect();

    var body = document.body;
    var docEl = document.documentElement;

    var scrollTop = window.scrollY || docEl.scrollTop || body.scrollTop;
    var scrollLeft = window.scrollX || docEl.scrollLeft || body.scrollLeft;

    var clientTop = docEl.clientTop || body.clientTop || 0;
    var clientLeft = docEl.clientLeft || body.clientLeft || 0;

    var top = box.top + scrollTop - clientTop;
    var left = box.left + scrollLeft - clientLeft;

    return { top: Math.round(top), left: Math.round(left) };
}

const isNumericInput = (event) => {
    const key = event.keyCode;
    return ((key >= 48 && key <= 57) || // Allow number line
        (key >= 96 && key <= 105) // Allow number pad
    );
};

const isModifierKey = (event) => {
    const key = event.keyCode;
    return (event.shiftKey === true || key === 35 || key === 36) || // Allow Shift, Home, End
        (key === 8 || key === 9 || key === 13 || key === 46) || // Allow Backspace, Tab, Enter, Delete
        (key > 36 && key < 41) || // Allow left, up, right, down
        (
            // Allow Ctrl/Command + A,C,V,X,Z
            (event.ctrlKey === true || event.metaKey === true) &&
            (key === 65 || key === 67 || key === 86 || key === 88 || key === 90)
        )
};

const getAccordionChildContent = (accordion, child) => {
    return accordion?.getChildContent?.(child)
        ?? child?.querySelector?.(':scope > [data-accordion-content="true"]')
        ?? child?.querySelector?.(':scope > div:last-child')
        ?? child?.lastElementChild
        ?? null;
};

const getAccordionChildHeader = (accordion, child) => {
    return accordion?.getChildHeader?.(child)
        ?? child?.querySelector?.(':scope > h3 > [data-accordion-header="true"]')
        ?? child?.querySelector?.(':scope > h3')
        ?? child?.firstElementChild
        ?? null;
};

const resetAccordionElement = (accordion) => {
    if (!accordion) {
        return;
    }

    if (typeof accordion.resetChildren === 'function') {
        accordion.resetChildren();
        return;
    }

    accordion.querySelectorAll(':scope > [data-accordion-child="true"], :scope > .tnt-accordion-child')
        .forEach((child) => {
            const content = getAccordionChildContent(accordion, child);
            const header = getAccordionChildHeader(accordion, child);
            if (!content) {
                return;
            }

            content.classList.remove('tnt-expanded');
            content.classList.remove('tnt-collapsed');
            content.setAttribute('aria-hidden', 'true');
            header?.setAttribute('aria-expanded', 'false');

            child.querySelectorAll('tnt-accordion').forEach((nestedAccordion) => {
                resetAccordionElement(nestedAccordion);
            });
        });
};


window.NTComponents = {
    customAttribute: "tntid",
    addHidden: (element) => {
        if (element && element.classList && !element.classList.contains('tnt-hidden')) {
            element.classList.add('tnt-hidden');
        }
    },
    /**
     * Returns the color value for a given TnTColor enum variable name as a string.
     * @param {string} colorName - The TnTColor enum variable name (e.g., 'Primary', 'OnPrimaryContainer').
     * @returns {string|null} The color value as defined in CSS variables (e.g., 'var(--tnt-primary)'), or null if not found.
     */
    getColorValueFromEnumName: function (colorName) {
        if (!colorName || typeof colorName !== 'string') return null;

        // Convert PascalCase or camelCase to kebab-case (e.g., 'OnPrimaryContainer' -> 'on-primary-container')
        const kebab = colorName.replace(/(?<=.)([A-Z])/g, '-$1').toLowerCase();
        // Compose the CSS variable name
        const cssVar = `--tnt-color-${kebab}`;
        // Try to get the value from the root element
        let value = getComputedStyle(document.documentElement).getPropertyValue(cssVar);
        if (!value) return null;
        value = value.trim();

        // If value is in rgb(X, Y, Z) or rgba(X, Y, Z, A) format (commas or spaces), convert to hex
        // Support both rgb(224,224,255) and rgb(224 224 255)
        const rgbRegex = /^rgb\s*\(\s*(\d{1,3})[ ,]+(\d{1,3})[ ,]+(\d{1,3})\s*\)$/i;
        const rgbaRegex = /^rgba\s*\(\s*(\d{1,3})[ ,]+(\d{1,3})[ ,]+(\d{1,3})[ ,]+(0|1|0?\.\d+)\s*\)$/i;
        let match = value.match(rgbRegex);
        if (match) {
            // Convert rgb to hex
            const r = parseInt(match[1], 10).toString(16).padStart(2, '0');
            const g = parseInt(match[2], 10).toString(16).padStart(2, '0');
            const b = parseInt(match[3], 10).toString(16).padStart(2, '0');
            return `#${r}${g}${b}`;
        }
        match = value.match(rgbaRegex);
        if (match) {
            // Convert rgba to hex (ignore alpha for hex, or append as 2-digit hex if needed)
            const r = parseInt(match[1], 10).toString(16).padStart(2, '0');
            const g = parseInt(match[2], 10).toString(16).padStart(2, '0');
            const b = parseInt(match[3], 10).toString(16).padStart(2, '0');
            // Optionally include alpha as hex
            // const a = Math.round(parseFloat(match[4]) * 255).toString(16).padStart(2, '0');
            return `#${r}${g}${b}`;
        }
        return value;
    },
    openModalDialog: (dialogId) => {
        const dialog = document.getElementById(dialogId);
        if (dialog) {
            dialog.showModal();

            dialog.addEventListener('cancel', e => {
                e.preventDefault();
                e.stopPropagation();
            });
        }
    },
    enableRipple: (element) => {
        function setRippleOffset(e) {
            const boundingRect = element.getBoundingClientRect();
            const x = e.clientX - boundingRect.left - (boundingRect.width / 2);
            const y = e.clientY - boundingRect.top - (boundingRect.height / 2);
            element.style.setProperty('--ripple-offset-x', `${x}px`);
            element.style.setProperty('--ripple-offset-y', `${y}px`);
        }

        if (element) {
            element.addEventListener('click', setRippleOffset);
        }
    },
    downloadFileFromStream: async (fileName, contentStreamReference) => {
        const arrayBuffer = await contentStreamReference.arrayBuffer();
        const blob = new Blob([arrayBuffer]);
        NTComponents.downloadFileFromBlob(fileName, blob);
    },
    downloadFromUrl: async (fileName, url) => {
        const blob = await fetch(url).then(r => r.blob())
        NTComponents.downloadFileFromBlob(fileName, blob);
    },
    downloadFileFromBlob: (fileName, blob) => {
        const url = URL.createObjectURL(blob);
        const anchorElement = document.createElement('a');
        anchorElement.href = url;
        anchorElement.download = fileName ?? '';
        anchorElement.click();
        anchorElement.remove();
        URL.revokeObjectURL(url);
    },
    enforcePhoneFormat: (event) => {
        // Input must be of a valid number format or a modifier key, and not longer than ten digits
        if (!isNumericInput(event) && !isModifierKey(event)) {
            event.preventDefault();
        }
    },
    enforceCurrencyFormat: (event) => {
        // Input must be of a valid number format or a modifier key, and not longer than ten digits
        if (!isNumericInput(event) && !isModifierKey(event) && event.keyCode != 188 && event.keyCode != 190 && event.keyCode != 110) {
            event.preventDefault();
        }
    },
    formatToCurrency: (event) => {
        if (isModifierKey(event)) { return; }

        const numberRegex = new RegExp('[0-9.]', 'g');
        let numbers = '';
        let result;
        while ((result = numberRegex.exec(event.target.value)) != null) {
            numbers += result.toString();
        }

        let cultureCode = event.target.getAttribute('cultureCode');
        if (!cultureCode) {
            cultureCode = 'en-US';
        }

        let currencyCode = event.target.getAttribute('currencyCode');
        if (!currencyCode) {
            currencyCode = 'USD';
        }

        // Create our number formatter.
        const formatter = new Intl.NumberFormat(cultureCode, {
            style: 'currency',
            currency: currencyCode,
        });
        let formatted = formatter.format(numbers);
        if (!event.target.value.includes('.')) {
            formatted = formatted.substring(0, formatted.length - 3);
        }
        else {
            const cents = event.target.value.split('.')[1];
            formatted = formatted.substring(0, formatted.length - 3) + '.' + cents.substring(0, 2);
        }

        event.target.value = formatted;
    },
    formatToPhone: (event) => {
        if (isModifierKey(event)) { return; }
        event.target.value = formatPhoneValue(event.target.value, getPhoneMask(event.target));
    },
    getCurrentLocation: () => {
        return window.location.href;
    },
    setupRipple: () => {
        ensureRippleHandlers();

        document.querySelectorAll('.nt-button, .nt-icon-button').forEach((element) => {
            registerButtonInteraction(element);
        });

        const elements = document.querySelectorAll('.tnt-ripple');

        elements.forEach(element => {
            if (getRippleHost(element)) {
                return;
            }

            if (!element.querySelector('tnt-ripple-effect')) {
                element.appendChild(document.createElement('tnt-ripple-effect'));
            }
        });
    },
    registerButtonInteraction,
    registerButtonInteractions,
    registerRippleHost,
    startButtonInteraction,
    startRippleHost,
    applyPhoneMaskInput,
    formatPhoneValue,
    normalizePhoneValue,
    prefixCurrencyInput,
    autoGrowTextArea,
    updateInputCounter,
    toggleAccordionHeader: (e) => {
        // If the click bubbled up from a nested interactive element inside the header template,
        // don't toggle the accordion — let that element handle its own click.
        // currentTarget is only set when called from the native onclick attribute (real browser);
        // skip the guard when it is absent so synthetic events in tests are unaffected.
        if (e.currentTarget != null
            && e.target !== e.currentTarget
            && e.target?.closest?.('button, a, input, select, textarea') !== e.currentTarget) {
            return;
        }
        const header = e.currentTarget
            ?? e.target?.closest?.('[data-accordion-header="true"]')
            ?? e.target?.closest?.('button, h3');
        const child = header?.closest?.('[data-accordion-child="true"]')
            ?? header?.closest?.('.tnt-accordion-child')
            ?? header?.parentElement;
        const accordion = child?.closest?.('tnt-accordion');
        const getChildContent = (candidateChild) => getAccordionChildContent(accordion, candidateChild);
        const syncChildAccessibility = (candidateChild, expanded) => {
            const candidateContent = getChildContent(candidateChild);
            const candidateHeader = getAccordionChildHeader(accordion, candidateChild);
            if (candidateContent) {
                candidateContent.setAttribute('aria-hidden', expanded ? 'false' : 'true');
                candidateContent.toggleAttribute('inert', !expanded);
            }
            if (candidateHeader) {
                candidateHeader.setAttribute('aria-expanded', expanded ? 'true' : 'false');
            }
        };
        const setExpandedState = (candidateChild, expanded) => {
            if (typeof accordion?.setExpandedState === 'function') {
                accordion.setExpandedState(candidateChild, expanded);
                return;
            }

            const candidateContent = getChildContent(candidateChild);
            if (!candidateContent) {
                return;
            }

            candidateContent.classList.toggle('tnt-expanded', expanded);
            candidateContent.classList.toggle('tnt-collapsed', !expanded);
            syncChildAccessibility(candidateChild, expanded);
        };
        const closeChildren = (excludeChild) => {
            if (typeof accordion?.closeChildren === 'function') {
                accordion.closeChildren(excludeChild);
                return;
            }

            accordion?.querySelectorAll?.(':scope > [data-accordion-child="true"], :scope > .tnt-accordion-child')
                ?.forEach((candidateChild) => {
                    if (candidateChild === excludeChild) {
                        return;
                    }

                    const candidateContent = getChildContent(candidateChild);
                    if (candidateContent?.classList.contains('tnt-expanded')) {
                        setExpandedState(candidateChild, false);
                    }
                });
        };
        const updateChild = (candidateContent) => {
            accordion?.updateChild?.(candidateContent);
        };
        const resetNestedAccordions = (candidateContent) => {
            const nestedAccordions = candidateContent?.querySelectorAll?.('tnt-accordion');
            nestedAccordions?.forEach((nested) => {
                resetAccordionElement(nested);
            });
        };
        const limitToOneExpanded = typeof accordion?.limitToOneExpanded === 'function'
            ? accordion.limitToOneExpanded()
            : accordion?.classList?.contains('tnt-limit-one-expanded');
        const content = child ? getChildContent(child) : null;

        if (!header || !child || !accordion || !content || header.disabled) {
            return;
        }

        const isExpanded = content.classList.contains('tnt-expanded');
        if (limitToOneExpanded && !isExpanded) {
            closeChildren(child);
        }

        setExpandedState(child, !isExpanded);

        if (isExpanded) {
            resetNestedAccordions(content);
        }

        updateChild(content);

        if (accordion.dotNetRef) {
            const childId = child.getAttribute('data-accordion-child-id') ?? header.getAttribute('data-accordion-child-id');
            if (childId) {
                const childIdNumber = parseInt(childId, 10);
                if (!Number.isNaN(childIdNumber)) {
                    accordion.dotNetRef.invokeMethodAsync(isExpanded ? "SetAsClosed" : "SetAsOpened", childIdNumber);
                }
            }
        }
    },
    toggleSideNav: (event) => {
        const layout = event.target.closest('.tnt-layout');

        if (layout) {
            const sideNav = layout.querySelector(':scope > .tnt-side-nav-toggle-indicator');

            if (sideNav) {
                const toggler = sideNav.querySelector('.tnt-toggle-indicator');
                if (toggler && toggler.classList) {
                    toggler.classList.toggle('tnt-toggle');
                }
            }
        }
    },
    toggleSideNavGroup: (event) => {
        const toggler = event.target.parentElement.querySelector('.tnt-side-nav-menu-group-toggler');

        if (toggler && toggler.classList) {
            if (toggler.classList.contains('tnt-toggle')) {
                toggler.classList.remove('tnt-toggle');
            }
            else {
                toggler.classList.add('tnt-toggle');
            }
        }
    },
    stopEnter: (event) => {
        if (event.key === 'Enter') {
            // Prevent the default action (such as submitting a surrounding form)
            // in addition to stopping propagation so that selecting an item with Enter
            // in a typeahead does not cause the form to submit.
            if (typeof event.preventDefault === 'function') {
                event.preventDefault();
            }
            event.stopPropagation();
        }
    },
    stopEnterPropagation: (event) => {
        if (event.key === 'Enter') {
            // Preserve the default newline behavior while preventing parent handlers
            // from treating Enter inside multiline inputs as a submit/navigation key.
            event.stopPropagation();
        }
    },
    formKeyDownSupportingTextHandler: (event) => {
        const input = event.target;
        const container = input.closest('.tnt-input-container');
        if (!container) return;

        const supportingText = container.querySelector('.tnt-input-length');
        const maxLength = input.getAttribute('maxlength');

        if (supportingText && maxLength) {
            setTimeout(() => {
                supportingText.innerText = `${input.value.length}/${maxLength}`;
            }, 0);
        }
    },
    radioGroupKeyDownHandler: (event) => {
        const group = event.currentTarget;
        if (!group) return;

        // Number keys 1-9
        if (event.key >= '1' && event.key <= '9') {
            const index = parseInt(event.key) - 1;
            const radios = group.querySelectorAll('input[type="radio"]');

            if (index < radios.length) {
                const radio = radios[index];
                // Check if the individual radio or the group fieldset is disabled/readonly
                const isDisabled = radio.disabled || group.disabled || group.classList.contains('tnt-disabled');
                const isReadOnly = radio.readOnly || group.classList.contains('tnt-readonly');

                if (radio && !isDisabled && !isReadOnly) {
                    radio.click();
                    event.preventDefault();
                }
            }
        }
    },
    onThemeChanged: (dotNetHelper) => {
        const callback = () => {
            dotNetHelper.invokeMethodAsync('OnThemeChanged');
        };
        document.addEventListener('tnt-theme-changed', callback);
        return {
            dispose: () => document.removeEventListener('tnt-theme-changed', callback)
        };
    }
}
