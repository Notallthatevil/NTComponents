/**
 * @jest-environment jsdom
 */
import { jest } from '@jest/globals';
import { onDispose, onLoad, onUpdate } from '../NTInputDateTime.razor.js';

describe('NTInputDateTime picker behavior', () => {
    const fixedNow = new Date(2026, 2, 15, 14, 5, 9);
    let idCounter = 0;

    function completeRect(overrides = {}) {
        const rect = {
            width: 240,
            height: 40,
            top: 100,
            left: 100,
            right: 340,
            bottom: 140,
            x: 100,
            y: 100,
            ...overrides,
            toJSON: () => { },
        };

        if (overrides.left !== undefined && overrides.x === undefined) {
            rect.x = rect.left;
        }
        if (overrides.top !== undefined && overrides.y === undefined) {
            rect.y = rect.top;
        }
        if ((overrides.left !== undefined || overrides.width !== undefined) && overrides.right === undefined) {
            rect.right = rect.left + rect.width;
        }
        if ((overrides.top !== undefined || overrides.height !== undefined) && overrides.bottom === undefined) {
            rect.bottom = rect.top + rect.height;
        }

        return rect;
    }

    function setRect(element, rect) {
        jest.spyOn(element, 'getBoundingClientRect').mockReturnValue(completeRect(rect));
    }

    function createPickerFixture({
        disabledDates = null,
        disabledTimes = null,
        lazy = false,
        max = null,
        min = null,
        mode = 'date',
        openOnFocus = false,
        value = '',
    } = {}) {
        const suffix = ++idCounter;
        const pickerId = `tnt-dtp-test-${suffix}`;

        const input = document.createElement('input');
        input.type = mode === 'datetime' ? 'datetime-local' : mode;
        input.value = value;
        input.dataset.tntDtpInput = 'true';
        input.dataset.tntDtpTarget = pickerId;
        input.dataset.tntDtpMode = mode;
        input.dataset.tntDtpOpenOnFocus = openOnFocus ? 'true' : 'false';

        const label = document.createElement('label');
        label.appendChild(input);

        const trigger = document.createElement('button');
        trigger.type = 'button';
        trigger.dataset.tntDtpTrigger = 'true';
        trigger.dataset.tntDtpTarget = pickerId;

        if (min) {
            input.min = min;
        }
        if (max) {
            input.max = max;
        }
        if (disabledDates) {
            input.dataset.tntDtpDisabledDates = disabledDates;
        }
        if (disabledTimes) {
            input.dataset.tntDtpDisabledTimes = disabledTimes;
        }

        const picker = document.createElement('div');
        picker.id = pickerId;
        picker.tabIndex = -1;
        picker.dataset.tntDtpPicker = 'true';
        picker.dataset.tntDtpMode = mode;
        const rendersDatePanel = mode === 'date' || mode === 'datetime';
        const rendersMonthPanel = mode === 'date' || mode === 'datetime' || mode === 'month';
        const rendersYearPanel = mode === 'date' || mode === 'datetime';
        const rendersTimePanel = mode === 'datetime' || mode === 'time';

        const headline = document.createElement('div');
        headline.dataset.tntDtpHeadline = '';
        picker.appendChild(headline);

        const subhead = document.createElement('div');
        subhead.dataset.tntDtpSubhead = '';
        picker.appendChild(subhead);

        const datePanel = document.createElement('div');
        datePanel.dataset.tntDtpDatePanel = '';
        const monthLabel = document.createElement('div');
        monthLabel.dataset.tntDtpMonthLabel = '';
        datePanel.appendChild(monthLabel);
        const calendarYearLabel = document.createElement('div');
        calendarYearLabel.dataset.tntDtpCalendarYearLabel = '';
        datePanel.appendChild(calendarYearLabel);
        const showMonthsButton = document.createElement('button');
        showMonthsButton.type = 'button';
        showMonthsButton.dataset.tntDtpAction = 'show-months';
        showMonthsButton.appendChild(monthLabel);
        const menuIcon = document.createElement('span');
        menuIcon.className = 'tnt-dtp-menu-button-icon';
        showMonthsButton.appendChild(menuIcon);
        datePanel.appendChild(showMonthsButton);
        const showYearsButton = document.createElement('button');
        showYearsButton.type = 'button';
        showYearsButton.dataset.tntDtpAction = 'show-years';
        showYearsButton.appendChild(calendarYearLabel);
        datePanel.appendChild(showYearsButton);
        const dayGrid = document.createElement('div');
        for (let i = 0; i < 42; i += 1) {
            const dayButton = document.createElement('button');
            dayButton.type = 'button';
            dayButton.dataset.tntDtpDayIndex = String(i);
            dayGrid.appendChild(dayButton);
        }
        datePanel.appendChild(dayGrid);
        picker.appendChild(datePanel);

        const monthPanel = document.createElement('div');
        monthPanel.dataset.tntDtpMonthPanel = '';
        const yearLabel = document.createElement('div');
        yearLabel.dataset.tntDtpYearLabel = '';
        monthPanel.appendChild(yearLabel);
        const monthGrid = document.createElement('div');
        monthGrid.dataset.tntDtpMonthGrid = '';
        for (let i = 0; i < 12; i += 1) {
            const monthButton = document.createElement('button');
            monthButton.type = 'button';
            monthButton.dataset.tntDtpMonthIndex = String(i);
            monthGrid.appendChild(monthButton);
        }
        monthPanel.appendChild(monthGrid);
        picker.appendChild(monthPanel);

        const yearPanel = document.createElement('div');
        yearPanel.dataset.tntDtpYearPanel = '';
        const yearList = document.createElement('div');
        yearList.dataset.tntDtpYearList = '';
        yearPanel.appendChild(yearList);
        picker.appendChild(yearPanel);

        const timePanel = document.createElement('div');
        timePanel.dataset.tntDtpTimePanel = '';
        const hourInput = document.createElement('input');
        hourInput.dataset.tntDtpHour = '';
        timePanel.appendChild(hourInput);
        const minuteInput = document.createElement('input');
        minuteInput.dataset.tntDtpMinute = '';
        timePanel.appendChild(minuteInput);
        const amButton = document.createElement('button');
        amButton.type = 'button';
        amButton.dataset.tntDtpAction = 'set-am';
        amButton.dataset.tntDtpMeridiem = 'am';
        timePanel.appendChild(amButton);
        const pmButton = document.createElement('button');
        pmButton.type = 'button';
        pmButton.dataset.tntDtpAction = 'set-pm';
        pmButton.dataset.tntDtpMeridiem = 'pm';
        timePanel.appendChild(pmButton);
        picker.appendChild(timePanel);

        if (lazy || !rendersDatePanel) {
            datePanel.remove();
        }
        if (lazy || !rendersMonthPanel) {
            monthPanel.remove();
        }
        if (lazy || !rendersYearPanel) {
            yearPanel.remove();
        }
        if (lazy || !rendersTimePanel) {
            timePanel.remove();
        }

        const actions = document.createElement('div');
        const confirmButton = document.createElement('button');
        confirmButton.type = 'button';
        confirmButton.dataset.tntDtpAction = 'confirm';
        actions.appendChild(confirmButton);
        const cancelButton = document.createElement('button');
        cancelButton.type = 'button';
        cancelButton.dataset.tntDtpAction = 'cancel';
        actions.appendChild(cancelButton);
        picker.appendChild(actions);

        const root = document.createElement('div');
        root.className = 'nt-input-date-time';
        root.appendChild(label);
        root.appendChild(trigger);
        root.appendChild(picker);

        document.body.appendChild(root);

        setRect(label, { height: 48, left: 80, top: 84, width: 240 });
        setRect(input, { height: 36, left: 80, top: 90, width: 240 });
        setRect(picker, { height: 300, left: 0, top: 0, width: 320 });

        return {
            confirmButton,
            calendarYearLabel,
            cancelButton,
            hourInput,
            input,
            label,
            minuteInput,
            monthLabel,
            picker,
            showMonthsButton,
            showYearsButton,
            trigger,
            root,
            yearList,
            yearLabel,
        };
    }

    function openPicker(trigger) {
        onLoad(trigger.closest('.nt-input-date-time'), null);
        trigger.dispatchEvent(new MouseEvent('click', { bubbles: true }));
    }

    function createPageScriptFixture(options = {}) {
        const fixture = createPickerFixture(options);
        const script = document.createElement('tnt-page-script');
        document.body.insertBefore(script, fixture.root);
        return { ...fixture, script };
    }

    beforeEach(() => {
        document.body.innerHTML = '';
        jest.clearAllMocks();
        jest.useFakeTimers();
        jest.setSystemTime(fixedNow);

        Object.defineProperty(window, 'innerWidth', { configurable: true, value: 1280 });
        Object.defineProperty(window, 'innerHeight', { configurable: true, value: 720 });

        global.requestAnimationFrame = callback => {
            callback();
            return 1;
        };
        global.cancelAnimationFrame = () => { };
    });

    afterEach(() => {
        const trackedInputs = document.querySelectorAll('input[data-tnt-dtp-input="true"]');
        trackedInputs.forEach(input => onDispose(input, null));
        document.body.innerHTML = '';
        jest.useRealTimers();
        jest.restoreAllMocks();
    });

    test('opens from trigger and shows current month/year when date value is empty', () => {
        const { calendarYearLabel, input, monthLabel, picker, root, trigger } = createPickerFixture({ mode: 'date', value: '' });

        onLoad(root, null);
        openPicker(trigger);

        const expectedMonthLabel = new Intl.DateTimeFormat(undefined, { month: 'long' }).format(new Date(2026, 2, 1));
        const expectedYearLabel = new Intl.DateTimeFormat(undefined, { year: 'numeric' }).format(new Date(2026, 2, 1));

        expect(picker.classList.contains('tnt-dtp-open')).toBe(true);
        expect(monthLabel.textContent).toBe(expectedMonthLabel);
        expect(calendarYearLabel.textContent).toBe(expectedYearLabel);
        expect(document.activeElement).toBe(input);
    });

    test('creates picker panel controls lazily on first open', () => {
        const { picker, trigger } = createPickerFixture({ lazy: true, mode: 'date', value: '' });

        expect(picker.querySelectorAll('[data-tnt-dtp-day-index]')).toHaveLength(0);
        expect(picker.querySelectorAll('[data-tnt-dtp-month-index]')).toHaveLength(0);

        openPicker(trigger);

        expect(picker.querySelectorAll('[data-tnt-dtp-day-index]')).toHaveLength(42);
        expect(picker.querySelectorAll('[data-tnt-dtp-month-index]')).toHaveLength(12);
        expect(picker.querySelector('[data-tnt-dtp-year-list]')).not.toBeNull();
        expect(picker.querySelector('[data-tnt-dtp-hour]')).toBeNull();
    });

    test.each([
        ['datetime', { dayCount: 42, hasTime: true, monthCount: 12, hasYearList: true }],
        ['month', { dayCount: 0, hasTime: false, monthCount: 12, hasYearList: false }],
        ['time', { dayCount: 0, hasTime: true, monthCount: 0, hasYearList: false }],
    ])('creates lazy picker panel controls for %s mode', (mode, expectations) => {
        const { picker, trigger } = createPickerFixture({ lazy: true, mode, value: mode === 'datetime' ? '2026-03-15T14:05' : '' });

        openPicker(trigger);

        expect(picker.querySelectorAll('[data-tnt-dtp-day-index]')).toHaveLength(expectations.dayCount);
        expect(picker.querySelectorAll('[data-tnt-dtp-month-index]')).toHaveLength(expectations.monthCount);
        expect(picker.querySelector('[data-tnt-dtp-year-list]') !== null).toBe(expectations.hasYearList);
        expect(picker.querySelector('[data-tnt-dtp-hour]') !== null).toBe(expectations.hasTime);
    });

    test('month menu button switches docked date picker to month selection view', () => {
        const { cancelButton, confirmButton, picker, showMonthsButton, trigger } = createPickerFixture({ mode: 'date', value: '2026-03-15' });

        openPicker(trigger);
        showMonthsButton.dispatchEvent(new MouseEvent('click', { bubbles: true }));

        expect(picker.classList.contains('tnt-dtp-view-month')).toBe(true);
        expect(cancelButton.textContent).toBe('Back');
        expect(cancelButton.getAttribute('aria-label')).toBe('Back to calendar');
        expect(confirmButton.hidden).toBe(true);

        cancelButton.dispatchEvent(new MouseEvent('click', { bubbles: true }));

        expect(picker.classList.contains('tnt-dtp-view-month')).toBe(false);
        expect(cancelButton.textContent).toBe('Cancel');
        expect(confirmButton.hidden).toBe(false);
    });

    test('year menu button switches docked date picker to scrollable year selection view', () => {
        const { calendarYearLabel, cancelButton, confirmButton, picker, showYearsButton, trigger, yearList } = createPickerFixture({ mode: 'date', value: '2026-03-15' });
        yearList.setAttribute('b-testscope', '');

        openPicker(trigger);
        showYearsButton.dispatchEvent(new MouseEvent('click', { bubbles: true }));

        const selectedYear = yearList.querySelector('[data-tnt-dtp-year-option="2026"]');

        expect(picker.classList.contains('tnt-dtp-view-year')).toBe(true);
        expect(cancelButton.textContent).toBe('Back');
        expect(confirmButton.hidden).toBe(true);
        expect(yearList.children.length).toBeLessThanOrEqual(300);
        expect(yearList.children.length).toBeGreaterThan(100);
        expect(selectedYear).not.toBeNull();
        expect(selectedYear.hasAttribute('b-testscope')).toBe(true);

        yearList.querySelector('[data-tnt-dtp-year-option="2030"]').dispatchEvent(new MouseEvent('click', { bubbles: true }));

        expect(picker.classList.contains('tnt-dtp-view-year')).toBe(false);
        expect(calendarYearLabel.textContent).toBe('2030');
    });

    test('year list extends upward to year 0001', () => {
        const { showYearsButton, trigger, yearList } = createPickerFixture({ mode: 'date', value: '0105-03-15' });

        Object.defineProperty(yearList, 'clientHeight', { configurable: true, value: 240 });
        Object.defineProperty(yearList, 'scrollHeight', { configurable: true, value: 1200 });

        openPicker(trigger);
        showYearsButton.dispatchEvent(new MouseEvent('click', { bubbles: true }));

        expect(yearList.querySelector('[data-tnt-dtp-year-option="1"]')).toBeNull();

        yearList.scrollTop = 0;
        yearList.dispatchEvent(new Event('scroll'));

        const firstYear = yearList.querySelector('[data-tnt-dtp-year-option="1"]');
        expect(firstYear).not.toBeNull();
        expect(firstYear.textContent).toBe('0001');
        expect(yearList.children.length).toBeLessThanOrEqual(300);
    });

    test('year list extends downward to year 9999 without unbounded growth', () => {
        const { showYearsButton, trigger, yearList } = createPickerFixture({ mode: 'date', value: '9800-03-15' });

        Object.defineProperty(yearList, 'clientHeight', { configurable: true, value: 240 });
        Object.defineProperty(yearList, 'scrollHeight', { configurable: true, value: 1200 });

        openPicker(trigger);
        showYearsButton.dispatchEvent(new MouseEvent('click', { bubbles: true }));

        expect(yearList.querySelector('[data-tnt-dtp-year-option="9999"]')).toBeNull();

        yearList.scrollTop = 960;
        yearList.dispatchEvent(new Event('scroll'));
        yearList.dispatchEvent(new Event('scroll'));

        const lastYear = yearList.querySelector('[data-tnt-dtp-year-option="9999"]');
        expect(lastYear).not.toBeNull();
        expect(lastYear.textContent).toBe('9999');
        expect(yearList.children.length).toBeLessThanOrEqual(300);
    });

    test('time input works without a visible seconds field', () => {
        const { confirmButton, hourInput, input, minuteInput, trigger } = createPickerFixture({ mode: 'time', value: '09:15:22' });

        openPicker(trigger);
        hourInput.value = '11';
        minuteInput.value = '45';
        hourInput.dispatchEvent(new Event('input', { bubbles: true }));
        confirmButton.dispatchEvent(new MouseEvent('click', { bubbles: true }));

        expect(input.value).toBe('11:45:00');
    });

    test('existing hidden seconds are not preserved for time confirmation', () => {
        const { confirmButton, input, trigger } = createPickerFixture({ mode: 'time', value: '09:15:22' });

        openPicker(trigger);
        confirmButton.dispatchEvent(new MouseEvent('click', { bubbles: true }));

        expect(input.value).toBe('09:15:00');
    });

    test('min and max dates disable out-of-range calendar days', () => {
        const { input, picker, trigger } = createPickerFixture({
            max: '2026-03-20',
            min: '2026-03-10',
            mode: 'date',
            value: '',
        });

        openPicker(trigger);

        const outOfRange = picker.querySelector('[data-tnt-dtp-year="2026"][data-tnt-dtp-month="2"][data-tnt-dtp-day="5"]');
        const inRange = picker.querySelector('[data-tnt-dtp-year="2026"][data-tnt-dtp-month="2"][data-tnt-dtp-day="15"]');

        expect(outOfRange).not.toBeNull();
        expect(inRange).not.toBeNull();
        expect(outOfRange.disabled).toBe(true);
        expect(inRange.disabled).toBe(false);
    });

    test('disabled date values are blocked in the calendar', () => {
        const { input, picker, trigger } = createPickerFixture({
            disabledDates: '2026-03-15',
            mode: 'date',
            value: '',
        });

        openPicker(trigger);

        const disabledDate = picker.querySelector('[data-tnt-dtp-year="2026"][data-tnt-dtp-month="2"][data-tnt-dtp-day="15"]');

        expect(disabledDate).not.toBeNull();
        expect(disabledDate.disabled).toBe(true);
    });

    test('disabled date values refresh when attributes change before reopen', () => {
        const { input, picker, root, trigger } = createPickerFixture({ mode: 'date', value: '' });

        openPicker(trigger);
        let targetDate = picker.querySelector('[data-tnt-dtp-year="2026"][data-tnt-dtp-month="2"][data-tnt-dtp-day="15"]');
        expect(targetDate.disabled).toBe(false);

        input.dataset.tntDtpDisabledDates = '2026-03-15';
        onUpdate(root, null);

        targetDate = picker.querySelector('[data-tnt-dtp-year="2026"][data-tnt-dtp-month="2"][data-tnt-dtp-day="15"]');
        expect(targetDate.disabled).toBe(true);
    });

    test('disabled time values block confirm until time changes', () => {
        const { confirmButton, input, minuteInput, trigger } = createPickerFixture({
            disabledTimes: '14:05:00',
            mode: 'time',
            value: '14:05:09',
        });

        openPicker(trigger);

        expect(confirmButton.disabled).toBe(true);

        minuteInput.value = '06';
        minuteInput.dispatchEvent(new Event('input', { bubbles: true }));

        expect(confirmButton.disabled).toBe(false);
    });

    test('empty time confirmation emits zero seconds because seconds are not editable', () => {
        const { confirmButton, input, trigger } = createPickerFixture({ mode: 'time', value: '' });

        openPicker(trigger);
        confirmButton.dispatchEvent(new MouseEvent('click', { bubbles: true }));

        expect(input.value).toBe('14:05:00');
    });

    test('datetime confirmation respects the input format attribute', () => {
        const { confirmButton, input, trigger } = createPickerFixture({ mode: 'datetime', value: '2026-03-15T14:05:09' });
        input.setAttribute('format', 'yyyy-MM-ddTHH:mm');

        openPicker(trigger);
        confirmButton.dispatchEvent(new MouseEvent('click', { bubbles: true }));

        expect(input.value).toBe('2026-03-15T14:05');
    });

    test('existing hidden seconds are not preserved for datetime confirmation', () => {
        const { confirmButton, input, trigger } = createPickerFixture({ mode: 'datetime', value: '2026-03-15T14:05:09' });

        openPicker(trigger);
        confirmButton.dispatchEvent(new MouseEvent('click', { bubbles: true }));

        expect(input.value).toBe('2026-03-15T14:05');
    });

    test('disposing one input keeps other inputs functional', () => {
        const first = createPickerFixture({ mode: 'date' });
        const second = createPickerFixture({ mode: 'date' });

        openPicker(first.trigger);
        onDispose(first.input, null);

        openPicker(second.trigger);

        expect(second.picker.classList.contains('tnt-dtp-open')).toBe(true);
    });

    test('disposing page-script element scope removes disconnected input listeners', () => {
        const { input, picker, root, trigger } = createPickerFixture({ mode: 'date' });

        openPicker(trigger);
        onDispose(root, null);
        picker.classList.add('tnt-dtp-open');
        input.dispatchEvent(new KeyboardEvent('keydown', { bubbles: true, key: 'Tab' }));

        expect(picker.classList.contains('tnt-dtp-open')).toBe(true);
    });

    test('disposing disconnected page-script element scope removes input listeners', () => {
        const { input, picker, script, trigger } = createPageScriptFixture({ mode: 'date' });

        onLoad(script, null);
        trigger.dispatchEvent(new MouseEvent('click', { bubbles: true }));
        script.remove();
        onDispose(script, null);
        picker.classList.add('tnt-dtp-open');
        input.dispatchEvent(new KeyboardEvent('keydown', { bubbles: true, key: 'Tab' }));

        expect(picker.classList.contains('tnt-dtp-open')).toBe(true);
    });

    test('tab on input closes picker', () => {
        const { input, picker, trigger } = createPickerFixture({ mode: 'date', value: '' });

        openPicker(trigger);
        input.dispatchEvent(new KeyboardEvent('keydown', { bubbles: true, key: 'Tab' }));

        expect(picker.classList.contains('tnt-dtp-open')).toBe(false);
    });

    test('enter on input confirms current selection and closes picker', () => {
        const { input, picker, trigger } = createPickerFixture({ mode: 'date', value: '' });

        openPicker(trigger);
        input.dispatchEvent(new KeyboardEvent('keydown', { bubbles: true, key: 'Enter' }));

        expect(input.value).toBe('2026-03-15');
        expect(picker.classList.contains('tnt-dtp-open')).toBe(false);
    });

    test('confirm emits input and change events with the selected form value', () => {
        const { confirmButton, input, picker, trigger } = createPickerFixture({ mode: 'date', value: '2026-03-15' });
        const inputHandler = jest.fn();
        const changeHandler = jest.fn();
        input.addEventListener('input', inputHandler);
        input.addEventListener('change', changeHandler);

        openPicker(trigger);
        picker.querySelector('[data-tnt-dtp-year="2026"][data-tnt-dtp-month="2"][data-tnt-dtp-day="16"]').dispatchEvent(new MouseEvent('click', { bubbles: true }));
        confirmButton.dispatchEvent(new MouseEvent('click', { bubbles: true }));

        expect(input.value).toBe('2026-03-16');
        expect(inputHandler).toHaveBeenCalledTimes(1);
        expect(changeHandler).toHaveBeenCalledTimes(1);
    });

    test('picker controls are removed from tab order', () => {
        const { confirmButton, hourInput, picker, root } = createPickerFixture({ mode: 'datetime' });

        onLoad(root, null);
        onUpdate(root, null);

        expect(picker.querySelectorAll('[tabindex="-1"]').length).toBeGreaterThan(0);
        expect(hourInput.getAttribute('tabindex')).toBe('-1');
        expect(confirmButton.getAttribute('tabindex')).toBe('-1');
    });

    test('small viewport uses modal layout class', () => {
        const { input, picker, trigger } = createPickerFixture({ mode: 'date', value: '' });
        Object.defineProperty(window, 'innerWidth', { configurable: true, value: 500 });

        openPicker(trigger);

        expect(picker.classList.contains('tnt-dtp-modal')).toBe(true);
        expect(picker.getAttribute('aria-modal')).toBe('true');
    });

    test('small viewport modal renders immediately without waiting for animation frame', () => {
        const { picker, trigger } = createPickerFixture({ mode: 'date', value: '' });
        Object.defineProperty(window, 'innerWidth', { configurable: true, value: 500 });
        const rafSpy = jest.fn(() => 1);
        global.requestAnimationFrame = rafSpy;

        openPicker(trigger);

        expect(rafSpy).not.toHaveBeenCalled();
        expect(picker.classList.contains('tnt-dtp-modal')).toBe(true);
        expect(picker.style.visibility).toBe('visible');
    });

    test('small viewport input focus does not open modal picker', () => {
        const { input, picker, root } = createPickerFixture({ mode: 'date', openOnFocus: true, value: '' });
        Object.defineProperty(window, 'innerWidth', { configurable: true, value: 500 });

        onLoad(root, null);
        input.dispatchEvent(new Event('focus'));

        expect(picker.classList.contains('tnt-dtp-open')).toBe(false);
        expect(picker.classList.contains('tnt-dtp-modal')).toBe(false);
    });

    test('desktop input focus opens docked picker when open on focus is enabled', () => {
        const { input, picker, root } = createPickerFixture({ mode: 'date', openOnFocus: true, value: '' });

        onLoad(root, null);
        input.dispatchEvent(new Event('focus'));

        expect(picker.classList.contains('tnt-dtp-open')).toBe(true);
        expect(picker.classList.contains('tnt-dtp-modal')).toBe(false);
    });

    test('clicking the input after trigger-open does not trigger a second open render cycle', () => {
        const { input, trigger } = createPickerFixture({ mode: 'date', value: '' });
        const rafSpy = jest.fn(callback => {
            callback();
            return 1;
        });
        global.requestAnimationFrame = rafSpy;

        openPicker(trigger);
        expect(rafSpy).toHaveBeenCalledTimes(1);

        input.dispatchEvent(new Event('click', { bubbles: true }));
        expect(rafSpy).toHaveBeenCalledTimes(1);
    });

    test('small viewport input click after trigger-open does not reopen modal picker', () => {
        const { input, picker, trigger } = createPickerFixture({ mode: 'date', value: '' });
        Object.defineProperty(window, 'innerWidth', { configurable: true, value: 500 });
        const setAttributeSpy = jest.spyOn(picker, 'setAttribute');

        openPicker(trigger);
        input.dispatchEvent(new Event('click', { bubbles: true }));

        const openStateWrites = setAttributeSpy.mock.calls.filter(([name, value]) => name === 'aria-hidden' && value === 'false');
        expect(openStateWrites).toHaveLength(1);
    });

    test('desktop layout aligns picker left edge to the label left edge', () => {
        const { input, label, picker, trigger } = createPickerFixture({ mode: 'date', value: '' });
        setRect(label, { height: 72, left: 36, top: 60, width: 520 });
        setRect(input, { height: 36, left: 180, top: 88, width: 240 });

        openPicker(trigger);

        expect(picker.classList.contains('tnt-dtp-modal')).toBe(false);
        expect(picker.style.left).toBe('36px');
    });

    test('desktop layout offsets below label bounds to avoid label overlap', () => {
        const { input, label, picker, trigger } = createPickerFixture({ mode: 'date', value: '' });
        setRect(label, { height: 100, left: 40, top: 60, width: 520 });
        setRect(input, { height: 36, left: 180, top: 88, width: 240 });

        openPicker(trigger);

        expect(picker.classList.contains('tnt-dtp-modal')).toBe(false);
        expect(picker.style.top).toBe('166px');
    });

    test('small viewport does not auto-open when input is already focused before load', () => {
        const { input, picker, root } = createPickerFixture({ mode: 'date', value: '' });
        Object.defineProperty(window, 'innerWidth', { configurable: true, value: 500 });

        input.focus();
        expect(document.activeElement).toBe(input);

        onLoad(root, null);

        expect(picker.classList.contains('tnt-dtp-open')).toBe(false);
        expect(picker.classList.contains('tnt-dtp-modal')).toBe(false);
    });
});
