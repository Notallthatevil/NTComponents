const pickerStateByInput = new Map();
const DEFAULT_MODAL_VIEWPORT_WIDTH_BREAKPOINT = 750;

let activePickerState = null;
let pendingFrame = null;
let pointerDownInsideActivePicker = false;
let globalHandlersAttached = false;
let mutationObserver = null;
let mutationFrame = null;

function clamp(value, min, max) {
    return Math.min(Math.max(value, min), max);
}

function toInt(value, fallback = 0) {
    const parsed = Number.parseInt(value, 10);
    return Number.isFinite(parsed) ? parsed : fallback;
}

function padTwo(value) {
    return String(value).padStart(2, '0');
}

function padYear(value) {
    const sign = value < 0 ? '-' : '';
    const absoluteYear = Math.abs(value);
    return `${sign}${String(absoluteYear).padStart(4, '0')}`;
}

function createLocalDate(year, month, day, hour = 0, minute = 0, second = 0) {
    const date = new Date(0);
    date.setFullYear(year, month, day);
    date.setHours(hour, minute, second, 0);
    return date;
}

function toTwelveHour(hour24) {
    const normalized = clamp(hour24, 0, 23);
    const meridiem = normalized >= 12 ? 'pm' : 'am';
    const hour = normalized % 12 || 12;
    return { hour, meridiem };
}

function toTwentyFourHour(hour12, meridiem) {
    const normalized = clamp(hour12, 1, 12);
    if (meridiem === 'pm') {
        return normalized === 12 ? 12 : normalized + 12;
    }

    return normalized === 12 ? 0 : normalized;
}

function shiftYearMonth(year, month, delta) {
    const totalMonths = (year * 12) + month + delta;
    const nextYear = Math.floor(totalMonths / 12);
    const nextMonth = ((totalMonths % 12) + 12) % 12;
    return { year: nextYear, month: nextMonth };
}

function shouldUseModalLayout(state) {
    const viewportWidth = Math.max(document.documentElement?.clientWidth ?? 0, window.innerWidth ?? 0);
    const rawBreakpoint = state?.picker ? getComputedStyle(state.picker).getPropertyValue('--tnt-dtp-modal-breakpoint') : '';
    const parsedBreakpoint = Number.parseFloat(rawBreakpoint);
    const breakpoint = Number.isFinite(parsedBreakpoint) ? parsedBreakpoint : DEFAULT_MODAL_VIEWPORT_WIDTH_BREAKPOINT;
    return viewportWidth <= breakpoint;
}

function formatDateKey(year, month, day) {
    return `${padYear(year)}-${padTwo(month + 1)}-${padTwo(day)}`;
}

function formatTimeKey(hour, minute, second) {
    return `${padTwo(hour)}:${padTwo(minute)}:${padTwo(second)}`;
}

function isValidDate(year, month, day) {
    const candidate = createLocalDate(year, month, day);
    return candidate.getFullYear() === year && candidate.getMonth() === month && candidate.getDate() === day;
}

function normalizeDateToken(token) {
    const trimmed = token?.trim();
    if (!trimmed) {
        return null;
    }

    const match = /^(-?\d{4,})-(\d{2})-(\d{2})$/.exec(trimmed);
    if (!match) {
        return null;
    }

    const year = toInt(match[1], 0);
    const month = toInt(match[2], 0) - 1;
    const day = toInt(match[3], 0);
    if (month < 0 || month > 11 || day < 1 || day > 31 || !isValidDate(year, month, day)) {
        return null;
    }

    return formatDateKey(year, month, day);
}

function normalizeTimeToken(token) {
    const trimmed = token?.trim();
    if (!trimmed) {
        return null;
    }

    const match = /^(\d{2}):(\d{2})(?::(\d{2}))?$/.exec(trimmed);
    if (!match) {
        return null;
    }

    const hour = toInt(match[1], 0);
    const minute = toInt(match[2], 0);
    const second = toInt(match[3], 0);
    if (hour < 0 || hour > 23 || minute < 0 || minute > 59 || second < 0 || second > 59) {
        return null;
    }

    return formatTimeKey(hour, minute, second);
}

function parseDisabledSet(rawValue, normalizer) {
    const values = new Set();
    if (!rawValue) {
        return values;
    }

    const tokens = rawValue.split(/[,\s;|]+/);
    for (const token of tokens) {
        const normalized = normalizer(token);
        if (normalized) {
            values.add(normalized);
        }
    }

    return values;
}

function cloneDraft(draft) {
    return {
        day: draft.day,
        hour: draft.hour,
        minute: draft.minute,
        month: draft.month,
        second: draft.second,
        year: draft.year,
    };
}

function createDefaultDraft() {
    const now = new Date();
    return {
        day: now.getDate(),
        hour: now.getHours(),
        minute: now.getMinutes(),
        month: now.getMonth(),
        second: now.getSeconds(),
        year: now.getFullYear(),
    };
}

function parseMode(state) {
    const mode = state.input?.dataset?.tntDtpMode ?? state.picker?.dataset?.tntDtpMode ?? 'none';
    if (mode === 'date' || mode === 'month' || mode === 'time' || mode === 'datetime') {
        return mode;
    }

    return 'none';
}

function parseDraft(mode, value) {
    if (!value) {
        return null;
    }

    if (mode === 'date') {
        const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value);
        if (!match) {
            return null;
        }

        const year = toInt(match[1], 0);
        const month = clamp(toInt(match[2], 1), 1, 12) - 1;
        const day = clamp(toInt(match[3], 1), 1, 31);
        return {
            day,
            hour: 0,
            minute: 0,
            month,
            second: 0,
            year,
        };
    }

    if (mode === 'time') {
        const match = /^(\d{2}):(\d{2})(?::(\d{2})(?:\.\d{1,3})?)?$/.exec(value);
        if (!match) {
            return null;
        }

        const current = createDefaultDraft();
        return {
            day: current.day,
            hour: clamp(toInt(match[1], 0), 0, 23),
            minute: clamp(toInt(match[2], 0), 0, 59),
            month: current.month,
            second: clamp(toInt(match[3], 0), 0, 59),
            year: current.year,
        };
    }

    if (mode === 'month') {
        const match = /^(\d{4})-(\d{2})$/.exec(value);
        if (!match) {
            return null;
        }

        return {
            day: 1,
            hour: 0,
            minute: 0,
            month: clamp(toInt(match[2], 1), 1, 12) - 1,
            second: 0,
            year: toInt(match[1], 0),
        };
    }

    if (mode === 'datetime') {
        const match = /^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2})(?::(\d{2})(?:\.\d{1,3})?)?$/.exec(value);
        if (!match) {
            return null;
        }

        return {
            day: clamp(toInt(match[3], 1), 1, 31),
            hour: clamp(toInt(match[4], 0), 0, 23),
            minute: clamp(toInt(match[5], 0), 0, 59),
            month: clamp(toInt(match[2], 1), 1, 12) - 1,
            second: clamp(toInt(match[6], 0), 0, 59),
            year: toInt(match[1], 0),
        };
    }

    return null;
}

function formatDraft(mode, draft) {
    if (mode === 'date') {
        return `${padYear(draft.year)}-${padTwo(draft.month + 1)}-${padTwo(draft.day)}`;
    }

    if (mode === 'time') {
        return `${padTwo(draft.hour)}:${padTwo(draft.minute)}:${padTwo(draft.second)}`;
    }

    if (mode === 'month') {
        return `${padYear(draft.year)}-${padTwo(draft.month + 1)}`;
    }

    if (mode === 'datetime') {
        return `${padYear(draft.year)}-${padTwo(draft.month + 1)}-${padTwo(draft.day)}T${padTwo(draft.hour)}:${padTwo(draft.minute)}:${padTwo(draft.second)}`;
    }

    return '';
}

function toDateComparable(draft) {
    return (draft.year * 10000) + ((draft.month + 1) * 100) + draft.day;
}

function toTimeComparable(draft) {
    return (draft.hour * 3600) + (draft.minute * 60) + draft.second;
}

function toMonthComparable(draft) {
    return (draft.year * 100) + (draft.month + 1);
}

function toDateTimeComparable(draft) {
    return createLocalDate(draft.year, draft.month, draft.day, draft.hour, draft.minute, draft.second).getTime();
}

function updateConstraints(state) {
    state.minDraft = parseDraft(state.mode, state.input?.min);
    state.maxDraft = parseDraft(state.mode, state.input?.max);
}

function updateDisabledSets(state) {
    state.disabledDateSet = parseDisabledSet(state.input?.dataset?.tntDtpDisabledDates, normalizeDateToken);
    state.disabledTimeSet = parseDisabledSet(state.input?.dataset?.tntDtpDisabledTimes, normalizeTimeToken);
}

function clampDraftToConstraints(state, draft) {
    const candidate = cloneDraft(draft);

    if (state.mode === 'date') {
        const value = toDateComparable(candidate);
        if (state.minDraft && value < toDateComparable(state.minDraft)) {
            return cloneDraft(state.minDraft);
        }
        if (state.maxDraft && value > toDateComparable(state.maxDraft)) {
            return cloneDraft(state.maxDraft);
        }
        return candidate;
    }

    if (state.mode === 'time') {
        const value = toTimeComparable(candidate);
        if (state.minDraft && value < toTimeComparable(state.minDraft)) {
            return cloneDraft(state.minDraft);
        }
        if (state.maxDraft && value > toTimeComparable(state.maxDraft)) {
            return cloneDraft(state.maxDraft);
        }
        return candidate;
    }

    if (state.mode === 'month') {
        const value = toMonthComparable(candidate);
        if (state.minDraft && value < toMonthComparable(state.minDraft)) {
            return cloneDraft(state.minDraft);
        }
        if (state.maxDraft && value > toMonthComparable(state.maxDraft)) {
            return cloneDraft(state.maxDraft);
        }
        return candidate;
    }

    if (state.mode === 'datetime') {
        const value = toDateTimeComparable(candidate);
        if (state.minDraft && value < toDateTimeComparable(state.minDraft)) {
            return cloneDraft(state.minDraft);
        }
        if (state.maxDraft && value > toDateTimeComparable(state.maxDraft)) {
            return cloneDraft(state.maxDraft);
        }
    }

    return candidate;
}

function setInputValue(state, value) {
    if (!state.input) {
        return;
    }

    state.input.value = value;
    state.input.dispatchEvent(new Event('input', { bubbles: true }));
    state.input.dispatchEvent(new Event('change', { bubbles: true }));
}

function syncDraftFromInputValue(state) {
    updateConstraints(state);
    const parsed = parseDraft(state.mode, state.input?.value);

    if (!parsed) {
        const current = createDefaultDraft();
        state.draft = clampDraftToConstraints(state, current);
        state.viewYear = current.year;
        state.viewMonth = current.month;
        state.meridiem = toTwelveHour(state.draft.hour).meridiem;
        return;
    }

    state.draft = clampDraftToConstraints(state, parsed);
    state.viewYear = state.draft.year;
    state.viewMonth = state.draft.month;
    state.meridiem = toTwelveHour(state.draft.hour).meridiem;
}

function isDateDisabled(state, year, month, day) {
    if (state.disabledDateSet?.has(formatDateKey(year, month, day))) {
        return true;
    }

    const value = toDateComparable({ year, month, day });

    if (state.minDraft) {
        const minValue = toDateComparable(state.minDraft);
        if (value < minValue) {
            return true;
        }
    }

    if (state.maxDraft) {
        const maxValue = toDateComparable(state.maxDraft);
        if (value > maxValue) {
            return true;
        }
    }

    return false;
}

function isTimeDisabled(state, hour, minute, second) {
    return state.disabledTimeSet?.has(formatTimeKey(hour, minute, second)) ?? false;
}

function isDraftDisabled(state, draft) {
    if (!draft) {
        return false;
    }

    if (state.mode === 'date') {
        return isDateDisabled(state, draft.year, draft.month, draft.day);
    }

    if (state.mode === 'time') {
        return isTimeDisabled(state, draft.hour, draft.minute, draft.second);
    }

    if (state.mode === 'month') {
        return false;
    }

    if (state.mode === 'datetime') {
        return isDateDisabled(state, draft.year, draft.month, draft.day) || isTimeDisabled(state, draft.hour, draft.minute, draft.second);
    }

    return false;
}

function renderHeader(state) {
    const headline = state.picker?.querySelector('[data-tnt-dtp-headline]');
    const subhead = state.picker?.querySelector('[data-tnt-dtp-subhead]');

    if (!headline || !subhead) {
        return;
    }

    const local = createLocalDate(state.draft.year, state.draft.month, state.draft.day, state.draft.hour, state.draft.minute, state.draft.second);

    if (state.mode === 'time') {
        headline.textContent = new Intl.DateTimeFormat(undefined, { hour: '2-digit', minute: '2-digit', second: '2-digit', hour12: true }).format(local);
        subhead.textContent = 'Select a time';
        return;
    }

    if (state.mode === 'month') {
        headline.textContent = new Intl.DateTimeFormat(undefined, { month: 'long', year: 'numeric' }).format(local);
        subhead.textContent = 'Select a month';
        return;
    }

    const headlineText = new Intl.DateTimeFormat(undefined, { weekday: 'short', month: 'short', day: 'numeric' }).format(local);
    headline.textContent = headlineText;

    if (state.mode === 'date') {
        subhead.textContent = new Intl.DateTimeFormat(undefined, { year: 'numeric', month: 'long' }).format(local);
    }
    else {
        subhead.textContent = new Intl.DateTimeFormat(undefined, { hour: '2-digit', minute: '2-digit', second: '2-digit', hour12: true }).format(local);
    }
}

function renderTimeFields(state) {
    const hourInput = state.picker?.querySelector('[data-tnt-dtp-hour]');
    const minuteInput = state.picker?.querySelector('[data-tnt-dtp-minute]');
    const secondInput = state.picker?.querySelector('[data-tnt-dtp-second]');
    const amButton = state.picker?.querySelector('[data-tnt-dtp-meridiem="am"]');
    const pmButton = state.picker?.querySelector('[data-tnt-dtp-meridiem="pm"]');
    const twelveHour = toTwelveHour(state.draft.hour);

    state.meridiem = twelveHour.meridiem;

    if (hourInput) {
        hourInput.value = String(twelveHour.hour);
    }
    if (minuteInput) {
        minuteInput.value = padTwo(state.draft.minute);
    }
    if (secondInput) {
        secondInput.value = padTwo(state.draft.second);
    }
    if (amButton) {
        const isSelected = state.meridiem === 'am';
        amButton.classList.toggle('tnt-dtp-meridiem-selected', isSelected);
        amButton.setAttribute('aria-pressed', isSelected ? 'true' : 'false');
    }
    if (pmButton) {
        const isSelected = state.meridiem === 'pm';
        pmButton.classList.toggle('tnt-dtp-meridiem-selected', isSelected);
        pmButton.setAttribute('aria-pressed', isSelected ? 'true' : 'false');
    }
}

function renderCalendar(state) {
    const monthLabel = state.picker?.querySelector('[data-tnt-dtp-month-label]');
    const dayButtons = state.picker?.querySelectorAll('[data-tnt-dtp-day-index]');

    if (!monthLabel || !dayButtons?.length) {
        return;
    }

    const monthStart = createLocalDate(state.viewYear, state.viewMonth, 1);
    monthLabel.textContent = new Intl.DateTimeFormat(undefined, { year: 'numeric', month: 'long' }).format(monthStart);

    const startOffset = monthStart.getDay();
    const rangeStart = createLocalDate(state.viewYear, state.viewMonth, 1 - startOffset);
    const today = new Date();
    const todayKey = toDateComparable({ year: today.getFullYear(), month: today.getMonth(), day: today.getDate() });
    const selectedKey = toDateComparable(state.draft);

    dayButtons.forEach((button, index) => {
        const cellDate = createLocalDate(rangeStart.getFullYear(), rangeStart.getMonth(), rangeStart.getDate() + index);
        const year = cellDate.getFullYear();
        const month = cellDate.getMonth();
        const day = cellDate.getDate();
        const cellKey = toDateComparable({ year, month, day });
        const inCurrentMonth = month === state.viewMonth && year === state.viewYear;
        const selected = cellKey === selectedKey;
        const isToday = cellKey === todayKey;
        const disabled = isDateDisabled(state, year, month, day);

        button.textContent = String(day);
        button.dataset.tntDtpYear = String(year);
        button.dataset.tntDtpMonth = String(month);
        button.dataset.tntDtpDay = String(day);
        button.disabled = disabled;
        button.classList.toggle('tnt-dtp-outside-month', !inCurrentMonth);
        button.classList.toggle('tnt-dtp-selected-day', selected);
        button.classList.toggle('tnt-dtp-today', isToday);
        button.setAttribute('aria-selected', selected ? 'true' : 'false');
        button.setAttribute('aria-current', isToday ? 'date' : 'false');
    });
}

function isMonthDisabled(state, year, month) {
    const value = toMonthComparable({ year, month });

    if (state.minDraft) {
        const minValue = toMonthComparable(state.minDraft);
        if (value < minValue) {
            return true;
        }
    }

    if (state.maxDraft) {
        const maxValue = toMonthComparable(state.maxDraft);
        if (value > maxValue) {
            return true;
        }
    }

    return false;
}

function renderMonthGrid(state) {
    const yearLabel = state.picker?.querySelector('[data-tnt-dtp-year-label]');
    const monthButtons = state.picker?.querySelectorAll('[data-tnt-dtp-month-index]');

    if (!yearLabel || !monthButtons?.length) {
        return;
    }

    yearLabel.textContent = padYear(state.viewYear);

    const today = new Date();
    const todayKey = toMonthComparable({ year: today.getFullYear(), month: today.getMonth() });
    const selectedKey = toMonthComparable(state.draft);

    monthButtons.forEach(button => {
        const month = toInt(button.dataset.tntDtpMonthIndex, 0);
        const cellKey = toMonthComparable({ year: state.viewYear, month });
        const selected = cellKey === selectedKey;
        const isToday = cellKey === todayKey;
        const disabled = isMonthDisabled(state, state.viewYear, month);
        const monthLabel = new Intl.DateTimeFormat(undefined, { month: 'short' }).format(createLocalDate(state.viewYear, month, 1));

        button.textContent = monthLabel;
        button.dataset.tntDtpYear = String(state.viewYear);
        button.dataset.tntDtpMonth = String(month);
        button.disabled = disabled;
        button.classList.toggle('tnt-dtp-selected-day', selected);
        button.classList.toggle('tnt-dtp-today', isToday);
        button.setAttribute('aria-selected', selected ? 'true' : 'false');
        button.setAttribute('aria-current', isToday ? 'date' : 'false');
    });
}

function renderActionState(state) {
    const confirmButton = state.picker?.querySelector('[data-tnt-dtp-action="confirm"]');
    if (confirmButton) {
        confirmButton.disabled = isDraftDisabled(state, state.draft);
    }
}

function makePickerUntabbable(state) {
    if (!state?.picker) {
        return;
    }

    const focusableElements = state.picker.querySelectorAll('button, input, select, textarea, a[href], [tabindex]');
    for (const element of focusableElements) {
        element.setAttribute('tabindex', '-1');
    }
}

function renderPicker(state) {
    if (!state.picker) {
        return;
    }

    state.picker.classList.remove('tnt-dtp-mode-date', 'tnt-dtp-mode-month', 'tnt-dtp-mode-time', 'tnt-dtp-mode-datetime');
    state.picker.classList.add(`tnt-dtp-mode-${state.mode}`);
    state.picker.dataset.tntDtpMode = state.mode;

    renderHeader(state);

    if (state.mode === 'month') {
        renderMonthGrid(state);
    }

    if (state.mode === 'date' || state.mode === 'datetime') {
        renderCalendar(state);
    }

    if (state.mode === 'time' || state.mode === 'datetime') {
        renderTimeFields(state);
    }

    renderActionState(state);

    if (state.isOpen && (!state.isModal || !shouldUseModalLayout(state))) {
        schedulePositionUpdate();
    }
}

function isTargetInsideState(state, target) {
    if (!state || !target) {
        return false;
    }

    const surface = state.surface ?? state.picker?.querySelector?.('[data-tnt-dtp-surface]');
    const targetIsBackdrop = state.isModal && state.picker === target;

    if (targetIsBackdrop) {
        return false;
    }

    return state.input?.contains(target) || surface?.contains(target) || (!state.isModal && state.picker?.contains(target));
}

function getLabelAnchorRect(state) {
    const labelElement = state.input?.closest?.('label') ?? state.input?.labels?.[0] ?? null;
    if (labelElement?.getBoundingClientRect) {
        return labelElement.getBoundingClientRect();
    }

    return null;
}

function getHorizontalAnchorRect(state) {
    const labelRect = getLabelAnchorRect(state);
    if (labelRect) {
        return labelRect;
    }

    return state.input.getBoundingClientRect();
}

function positionPicker(state) {
    if (!state?.isOpen || !state.input?.isConnected || !state.picker?.isConnected) {
        return;
    }

    const picker = state.picker;
    const useModalLayout = shouldUseModalLayout(state);
    state.isModal = useModalLayout;
    picker.classList.toggle('tnt-dtp-modal', useModalLayout);

    if (useModalLayout) {
        picker.style.left = '0px';
        picker.style.top = '0px';
        picker.style.visibility = 'visible';
        return;
    }

    const inputRect = state.input.getBoundingClientRect();
    const horizontalAnchorRect = getHorizontalAnchorRect(state);
    const labelAnchorRect = getLabelAnchorRect(state);
    const anchorTop = labelAnchorRect ? Math.min(labelAnchorRect.top, inputRect.top) : inputRect.top;
    const anchorBottom = labelAnchorRect ? Math.max(labelAnchorRect.bottom, inputRect.bottom) : inputRect.bottom;

    picker.style.visibility = 'hidden';
    picker.style.left = '0px';
    picker.style.top = '0px';

    const pickerRect = picker.getBoundingClientRect();
    const viewportWidth = Math.max(document.documentElement?.clientWidth ?? 0, window.innerWidth ?? 0);
    const viewportHeight = Math.max(document.documentElement?.clientHeight ?? 0, window.innerHeight ?? 0);
    const edgePadding = 8;
    const offset = 6;

    const maxLeft = Math.max(edgePadding, viewportWidth - pickerRect.width - edgePadding);
    let left = clamp(horizontalAnchorRect.left, edgePadding, maxLeft);

    const spaceBelow = Math.max(0, viewportHeight - anchorBottom - edgePadding);
    const spaceAbove = Math.max(0, anchorTop - edgePadding);
    const openBelow = spaceBelow >= pickerRect.height || spaceBelow >= spaceAbove;

    const rawTop = openBelow
        ? anchorBottom + offset
        : anchorTop - pickerRect.height - offset;

    const maxTop = Math.max(edgePadding, viewportHeight - pickerRect.height - edgePadding);
    const top = clamp(rawTop, edgePadding, maxTop);

    picker.style.left = `${Math.round(left)}px`;
    picker.style.top = `${Math.round(top)}px`;
    picker.style.visibility = 'visible';
}

function schedulePositionUpdate() {
    if (!activePickerState?.isOpen) {
        return;
    }

    if (shouldUseModalLayout(activePickerState)) {
        if (pendingFrame !== null) {
            cancelAnimationFrame(pendingFrame);
            pendingFrame = null;
        }

        positionPicker(activePickerState);
        return;
    }

    if (pendingFrame !== null) {
        cancelAnimationFrame(pendingFrame);
    }

    pendingFrame = requestAnimationFrame(() => {
        pendingFrame = null;
        positionPicker(activePickerState);
    });
}

function closePicker(state, restoreFocus = false) {
    if (!state?.isOpen || !state.picker) {
        return;
    }

    state.isOpen = false;
    state.isModal = false;
    state.picker.classList.remove('tnt-dtp-open');
    state.picker.classList.remove('tnt-dtp-modal');
    state.picker.setAttribute('aria-hidden', 'true');
    state.picker.style.visibility = 'hidden';

    if (activePickerState === state) {
        activePickerState = null;
    }

    if (pendingFrame !== null) {
        cancelAnimationFrame(pendingFrame);
        pendingFrame = null;
    }

    if (restoreFocus && state.input?.focus) {
        state.input.focus({ preventScroll: true });
    }
}

function openPicker(state) {
    if (!state?.input || !state?.picker) {
        return;
    }

    if (state.input.disabled || state.input.readOnly) {
        return;
    }

    if (activePickerState && activePickerState !== state) {
        closePicker(activePickerState);
    }

    const nextMode = parseMode(state);
    if (nextMode === 'none') {
        return;
    }

    if (state.isOpen && activePickerState === state && state.mode === nextMode) {
        schedulePositionUpdate();
        return;
    }

    state.mode = nextMode;
    syncDraftFromInputValue(state);
    state.picker.classList.add('tnt-dtp-open');
    state.picker.setAttribute('aria-hidden', 'false');
    state.isOpen = true;
    activePickerState = state;

    if (shouldUseModalLayout(state)) {
        state.isModal = true;
        state.picker.classList.add('tnt-dtp-modal');
        state.picker.style.left = '0px';
        state.picker.style.top = '0px';
        state.picker.style.visibility = 'visible';
    }

    renderPicker(state);
}

function handleDaySelection(state, button) {
    if (!button || button.disabled) {
        return;
    }

    state.draft.year = toInt(button.dataset.tntDtpYear, state.draft.year);
    state.draft.month = toInt(button.dataset.tntDtpMonth, state.draft.month);
    state.draft.day = toInt(button.dataset.tntDtpDay, state.draft.day);
    state.viewYear = state.draft.year;
    state.viewMonth = state.draft.month;
    renderPicker(state);
}

function handlePickerAction(state, action) {
    if (!action) {
        return;
    }

    if (action === 'prev-month') {
        const previous = shiftYearMonth(state.viewYear, state.viewMonth, -1);
        state.viewYear = previous.year;
        state.viewMonth = previous.month;
        renderPicker(state);
        return;
    }

    if (action === 'next-month') {
        const next = shiftYearMonth(state.viewYear, state.viewMonth, 1);
        state.viewYear = next.year;
        state.viewMonth = next.month;
        renderPicker(state);
        return;
    }

    if (action === 'prev-year') {
        state.viewYear -= 1;
        renderPicker(state);
        return;
    }

    if (action === 'next-year') {
        state.viewYear += 1;
        renderPicker(state);
        return;
    }

    if (action === 'today') {
        const now = new Date();
        state.draft.year = now.getFullYear();
        state.draft.month = now.getMonth();
        state.draft.day = now.getDate();
        state.viewYear = state.draft.year;
        state.viewMonth = state.draft.month;
        state.draft = clampDraftToConstraints(state, state.draft);
        if (isDraftDisabled(state, state.draft)) {
            renderPicker(state);
            return;
        }

        setInputValue(state, formatDraft(state.mode, state.draft));
        renderPicker(state);
        return;
    }

    if (action === 'now') {
        const now = new Date();
        if (state.mode === 'time') {
            state.draft.hour = now.getHours();
            state.draft.minute = now.getMinutes();
            state.draft.second = now.getSeconds();
        }
        else if (state.mode === 'datetime') {
            state.draft.year = now.getFullYear();
            state.draft.month = now.getMonth();
            state.draft.day = now.getDate();
            state.draft.hour = now.getHours();
            state.draft.minute = now.getMinutes();
            state.draft.second = now.getSeconds();
            state.viewYear = state.draft.year;
            state.viewMonth = state.draft.month;
        }
        else if (state.mode === 'month') {
            state.draft.year = now.getFullYear();
            state.draft.month = now.getMonth();
            state.draft.day = 1;
            state.viewYear = state.draft.year;
            state.viewMonth = state.draft.month;
        }
        else {
            state.draft.year = now.getFullYear();
            state.draft.month = now.getMonth();
            state.draft.day = now.getDate();
            state.viewYear = state.draft.year;
            state.viewMonth = state.draft.month;
        }

        state.meridiem = toTwelveHour(state.draft.hour).meridiem;
        state.draft = clampDraftToConstraints(state, state.draft);
        if (isDraftDisabled(state, state.draft)) {
            renderPicker(state);
            return;
        }

        setInputValue(state, formatDraft(state.mode, state.draft));
        renderPicker(state);
        return;
    }

    if (action === 'set-am' || action === 'set-pm') {
        state.meridiem = action === 'set-pm' ? 'pm' : 'am';
        const currentHour = toTwelveHour(state.draft.hour).hour;
        state.draft.hour = toTwentyFourHour(currentHour, state.meridiem);
        state.draft = clampDraftToConstraints(state, state.draft);
        renderPicker(state);
        return;
    }

    if (action === 'clear') {
        setInputValue(state, '');
        closePicker(state);
        return;
    }

    if (action === 'cancel') {
        closePicker(state);
        return;
    }

    if (action === 'confirm') {
        state.draft = clampDraftToConstraints(state, state.draft);
        if (isDraftDisabled(state, state.draft)) {
            renderPicker(state);
            return;
        }

        setInputValue(state, formatDraft(state.mode, state.draft));
        closePicker(state);
    }
}

function syncDraftFromTimeFields(state) {
    const hourInput = state.picker?.querySelector('[data-tnt-dtp-hour]');
    const minuteInput = state.picker?.querySelector('[data-tnt-dtp-minute]');
    const secondInput = state.picker?.querySelector('[data-tnt-dtp-second]');

    if (!hourInput || !minuteInput || !secondInput) {
        return;
    }

    const fallbackHour = toTwelveHour(state.draft.hour).hour;
    const selectedMeridiem = state.meridiem === 'pm' ? 'pm' : 'am';
    const hour12 = clamp(toInt(hourInput.value, fallbackHour), 1, 12);
    state.draft.hour = toTwentyFourHour(hour12, selectedMeridiem);
    state.draft.minute = clamp(toInt(minuteInput.value, state.draft.minute), 0, 59);
    state.draft.second = clamp(toInt(secondInput.value, state.draft.second), 0, 59);
    state.draft = clampDraftToConstraints(state, state.draft);
    state.meridiem = toTwelveHour(state.draft.hour).meridiem;
    renderPicker(state);
}

function configureStateFromAttributes(state) {
    state.mode = parseMode(state);
    state.openOnFocus = state.input?.dataset?.tntDtpOpenOnFocus !== 'false';
    state.surface = state.picker?.querySelector?.('[data-tnt-dtp-surface]') ?? null;
    updateConstraints(state);
    updateDisabledSets(state);
    state.draft = clampDraftToConstraints(state, state.draft);

    if (state.picker) {
        state.picker.setAttribute('aria-hidden', state.isOpen ? 'false' : 'true');
        if (!state.isOpen) {
            state.picker.classList.remove('tnt-dtp-open');
            state.picker.style.visibility = 'hidden';
        }
    }

    makePickerUntabbable(state);
    renderPicker(state);
}

function createPickerState(input, picker) {
    const state = {
        draft: createDefaultDraft(),
        disabledDateSet: new Set(),
        disabledTimeSet: new Set(),
        input,
        isOpen: false,
        isModal: false,
        maxDraft: null,
        meridiem: 'am',
        minDraft: null,
        mode: 'none',
        openOnFocus: true,
        picker,
        surface: picker.querySelector?.('[data-tnt-dtp-surface]') ?? null,
        viewMonth: createDefaultDraft().month,
        viewYear: createDefaultDraft().year,
    };

    state.onInputFocus = () => {
        if (state.openOnFocus) {
            openPicker(state);
        }
    };

    state.onInputClick = () => {
        if (!state.isOpen) {
            openPicker(state);
        }
    };

    state.onInputInput = () => {
        const parsed = parseDraft(state.mode, state.input?.value);
        if (parsed) {
            state.draft = clampDraftToConstraints(state, parsed);
            state.viewYear = state.draft.year;
            state.viewMonth = state.draft.month;
            if (state.isOpen) {
                renderPicker(state);
            }
        }
    };

    state.onInputKeyDown = event => {
        if (event.key === 'Escape' && state.isOpen) {
            event.preventDefault();
            closePicker(state);
            return;
        }

        if (event.key === 'Enter' && state.isOpen) {
            event.preventDefault();
            handlePickerAction(state, 'confirm');
            return;
        }

        if (event.key === 'Tab' && state.isOpen) {
            closePicker(state);
            return;
        }

        if (event.key === 'ArrowDown' && event.altKey) {
            event.preventDefault();
            openPicker(state);
        }
    };

    state.onPickerClick = event => {
        const actionButton = event.target?.closest?.('[data-tnt-dtp-action]');
        if (actionButton) {
            event.preventDefault();
            handlePickerAction(state, actionButton.dataset.tntDtpAction);
            return;
        }

        const dayButton = event.target?.closest?.('[data-tnt-dtp-day-index]');
        if (dayButton) {
            event.preventDefault();
            handleDaySelection(state, dayButton);
            return;
        }

        const monthButton = event.target?.closest?.('[data-tnt-dtp-month-index]');
        if (monthButton) {
            event.preventDefault();
            state.draft.year = toInt(monthButton.dataset.tntDtpYear, state.draft.year);
            state.draft.month = toInt(monthButton.dataset.tntDtpMonth, state.draft.month);
            state.viewYear = state.draft.year;
            state.viewMonth = state.draft.month;
            renderPicker(state);
        }
    };

    state.onPickerInput = event => {
        const target = event.target;
        if (target?.matches?.('[data-tnt-dtp-hour], [data-tnt-dtp-minute], [data-tnt-dtp-second]')) {
            syncDraftFromTimeFields(state);
        }
    };

    state.onPickerKeyDown = event => {
        if (event.key === 'Escape') {
            event.preventDefault();
            closePicker(state, true);
            return;
        }

        if (event.key === 'Enter') {
            event.preventDefault();
            handlePickerAction(state, 'confirm');
            return;
        }

        if (event.key === 'Tab') {
            closePicker(state);
        }
    };

    input.addEventListener('focus', state.onInputFocus);
    input.addEventListener('click', state.onInputClick);
    input.addEventListener('input', state.onInputInput);
    input.addEventListener('keydown', state.onInputKeyDown);
    picker.addEventListener('click', state.onPickerClick);
    picker.addEventListener('input', state.onPickerInput);
    picker.addEventListener('keydown', state.onPickerKeyDown);

    configureStateFromAttributes(state);
    return state;
}

function cleanupPickerState(state) {
    if (!state) {
        return;
    }

    closePicker(state);

    state.input?.removeEventListener('focus', state.onInputFocus);
    state.input?.removeEventListener('click', state.onInputClick);
    state.input?.removeEventListener('input', state.onInputInput);
    state.input?.removeEventListener('keydown', state.onInputKeyDown);
    state.picker?.removeEventListener('click', state.onPickerClick);
    state.picker?.removeEventListener('input', state.onPickerInput);
    state.picker?.removeEventListener('keydown', state.onPickerKeyDown);
}

function shouldAutoOpenForFocusedInput(state) {
    if (!state?.input || state.isOpen || !state.openOnFocus) {
        return false;
    }

    if (state.input.disabled || state.input.readOnly) {
        return false;
    }

    return document.activeElement === state.input;
}

function synchronizePickers() {
    for (const [input, state] of pickerStateByInput) {
        if (!input.isConnected || !state.picker?.isConnected) {
            cleanupPickerState(state);
            pickerStateByInput.delete(input);
        }
    }

    const inputs = document.querySelectorAll('input[data-tnt-dtp-input="true"][data-tnt-dtp-target]');
    for (const input of inputs) {
        if (pickerStateByInput.has(input)) {
            const existing = pickerStateByInput.get(input);
            configureStateFromAttributes(existing);
            if (shouldAutoOpenForFocusedInput(existing)) {
                openPicker(existing);
            }
            continue;
        }

        const targetId = input.dataset.tntDtpTarget;
        const picker = targetId ? document.getElementById(targetId) : null;
        if (!picker) {
            continue;
        }

        const state = createPickerState(input, picker);
        pickerStateByInput.set(input, state);
        if (shouldAutoOpenForFocusedInput(state)) {
            openPicker(state);
        }
    }

    if (activePickerState && (!activePickerState.input?.isConnected || !activePickerState.picker?.isConnected)) {
        closePicker(activePickerState);
        activePickerState = null;
    }

    if (pickerStateByInput.size > 0) {
        attachGlobalHandlers();
    }
    else {
        detachGlobalHandlers();
    }
}

function onDocumentMouseDown(event) {
    pointerDownInsideActivePicker = isTargetInsideState(activePickerState, event.target);
}

function onDocumentClick(event) {
    if (!activePickerState) {
        return;
    }

    const clickedInside = isTargetInsideState(activePickerState, event.target);
    if (!pointerDownInsideActivePicker && !clickedInside) {
        closePicker(activePickerState);
    }

    pointerDownInsideActivePicker = false;
}

function onDocumentFocusIn(event) {
    if (!activePickerState) {
        return;
    }

    if (!isTargetInsideState(activePickerState, event.target)) {
        closePicker(activePickerState);
    }
}

function onWindowLayoutChange() {
    if (activePickerState?.isOpen && activePickerState.isModal && shouldUseModalLayout(activePickerState)) {
        return;
    }

    schedulePositionUpdate();
}

function onDocumentKeyDown(event) {
    if (!activePickerState) {
        return;
    }

    if (event.key === 'Escape') {
        closePicker(activePickerState, true);
    }
}

function attachGlobalHandlers() {
    if (globalHandlersAttached) {
        return;
    }

    document.addEventListener('mousedown', onDocumentMouseDown);
    document.addEventListener('click', onDocumentClick);
    document.addEventListener('focusin', onDocumentFocusIn);
    document.addEventListener('keydown', onDocumentKeyDown);
    window.addEventListener('resize', onWindowLayoutChange, { passive: true });
    window.addEventListener('scroll', onWindowLayoutChange, true);
    globalHandlersAttached = true;
}

function detachGlobalHandlers() {
    if (!globalHandlersAttached) {
        return;
    }

    document.removeEventListener('mousedown', onDocumentMouseDown);
    document.removeEventListener('click', onDocumentClick);
    document.removeEventListener('focusin', onDocumentFocusIn);
    document.removeEventListener('keydown', onDocumentKeyDown);
    window.removeEventListener('resize', onWindowLayoutChange);
    window.removeEventListener('scroll', onWindowLayoutChange, true);
    globalHandlersAttached = false;
}

function cleanupGlobalResourcesIfIdle() {
    if (pickerStateByInput.size > 0) {
        return;
    }

    activePickerState = null;
    pointerDownInsideActivePicker = false;

    if (pendingFrame !== null) {
        cancelAnimationFrame(pendingFrame);
        pendingFrame = null;
    }

    stopMutationObserver();
    detachGlobalHandlers();
}

function disposeAll() {
    for (const state of pickerStateByInput.values()) {
        cleanupPickerState(state);
    }

    pickerStateByInput.clear();
    cleanupGlobalResourcesIfIdle();
}

function getStateForElement(element) {
    if (!element) {
        return null;
    }

    if (pickerStateByInput.has(element)) {
        return pickerStateByInput.get(element);
    }

    for (const state of pickerStateByInput.values()) {
        if (state.picker === element || state.picker?.contains?.(element)) {
            return state;
        }
    }

    return null;
}

function disposeStateForElement(element) {
    const state = getStateForElement(element);
    if (!state) {
        synchronizePickers();
        cleanupGlobalResourcesIfIdle();
        return;
    }

    cleanupPickerState(state);
    if (state.input) {
        pickerStateByInput.delete(state.input);
    }

    cleanupGlobalResourcesIfIdle();
}

function isElementNode(node) {
    return node && node.nodeType === Node.ELEMENT_NODE;
}

function containsPickerTargets(node) {
    if (!isElementNode(node)) {
        return false;
    }

    if (node.matches?.('input[data-tnt-dtp-input="true"], [data-tnt-dtp-picker="true"]')) {
        return true;
    }

    return node.querySelector?.('input[data-tnt-dtp-input="true"], [data-tnt-dtp-picker="true"]') !== null;
}

function shouldSynchronizeFromMutations(mutations) {
    for (const mutation of mutations) {
        if (mutation.type === 'attributes') {
            if (containsPickerTargets(mutation.target)) {
                return true;
            }
            continue;
        }

        if (mutation.type === 'childList') {
            for (const addedNode of mutation.addedNodes) {
                if (containsPickerTargets(addedNode)) {
                    return true;
                }
            }

            for (const removedNode of mutation.removedNodes) {
                if (containsPickerTargets(removedNode)) {
                    return true;
                }
            }
        }
    }

    return false;
}

function scheduleMutationSync() {
    if (mutationFrame !== null) {
        cancelAnimationFrame(mutationFrame);
    }

    mutationFrame = requestAnimationFrame(() => {
        mutationFrame = null;
        synchronizePickers();
    });
}

function ensureMutationObserver() {
    if (mutationObserver || typeof MutationObserver === 'undefined') {
        return;
    }

    const root = document.body ?? document.documentElement;
    if (!root) {
        return;
    }

    mutationObserver = new MutationObserver(mutations => {
        if (shouldSynchronizeFromMutations(mutations)) {
            scheduleMutationSync();
        }
    });

    mutationObserver.observe(root, {
        childList: true,
        subtree: true,
        attributes: true,
        attributeFilter: [
            'data-tnt-dtp-input',
            'data-tnt-dtp-target',
            'data-tnt-dtp-mode',
            'data-tnt-dtp-open-on-focus',
            'disabled',
            'readonly',
            'min',
            'max',
            'data-tnt-dtp-disabled-dates',
            'data-tnt-dtp-disabled-times',
        ],
    });
}

function stopMutationObserver() {
    if (mutationObserver) {
        mutationObserver.disconnect();
        mutationObserver = null;
    }

    if (mutationFrame !== null) {
        cancelAnimationFrame(mutationFrame);
        mutationFrame = null;
    }
}

export function onLoad(element, dotNetRef) {
    ensureMutationObserver();
    synchronizePickers();
}

export function onUpdate(element, dotNetRef) {
    ensureMutationObserver();
    synchronizePickers();
}

export function onDispose(element, dotNetRef) {
    disposeStateForElement(element);
}
