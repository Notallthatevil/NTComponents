/**
 * @jest-environment jsdom
 */
import { jest } from '@jest/globals';
import { onDispose, onLoad, onUpdate } from '../TnTInputDateTime.razor.js';

describe('TnTInputDateTime picker behavior', () => {
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
        max = null,
        min = null,
        mode = 'date',
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
        input.dataset.tntDtpOpenOnFocus = 'true';

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

        const timePanel = document.createElement('div');
        timePanel.dataset.tntDtpTimePanel = '';
        const hourInput = document.createElement('input');
        hourInput.dataset.tntDtpHour = '';
        timePanel.appendChild(hourInput);
        const minuteInput = document.createElement('input');
        minuteInput.dataset.tntDtpMinute = '';
        timePanel.appendChild(minuteInput);
        const secondInput = document.createElement('input');
        secondInput.dataset.tntDtpSecond = '';
        timePanel.appendChild(secondInput);
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

        const actions = document.createElement('div');
        const todayButton = document.createElement('button');
        todayButton.type = 'button';
        todayButton.dataset.tntDtpAction = 'today';
        actions.appendChild(todayButton);
        const nowButton = document.createElement('button');
        nowButton.type = 'button';
        nowButton.dataset.tntDtpAction = 'now';
        actions.appendChild(nowButton);
        const confirmButton = document.createElement('button');
        confirmButton.type = 'button';
        confirmButton.dataset.tntDtpAction = 'confirm';
        actions.appendChild(confirmButton);
        picker.appendChild(actions);

        document.body.appendChild(input);
        document.body.appendChild(picker);

        setRect(input, { height: 36, left: 80, top: 90, width: 240 });
        setRect(picker, { height: 300, left: 0, top: 0, width: 320 });

        return {
            confirmButton,
            hourInput,
            input,
            minuteInput,
            monthLabel,
            nowButton,
            picker,
            secondInput,
            yearLabel,
        };
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

    test('opens on focus and shows current month/year when date value is empty', () => {
        const { input, monthLabel, picker } = createPickerFixture({ mode: 'date', value: '' });

        onLoad(null, null);
        input.dispatchEvent(new Event('focus'));

        const expectedLabel = new Intl.DateTimeFormat(undefined, {
            month: 'long',
            year: 'numeric',
        }).format(new Date(2026, 2, 1));

        expect(picker.classList.contains('tnt-dtp-open')).toBe(true);
        expect(monthLabel.textContent).toBe(expectedLabel);
    });

    test('now populates datetime input with current date and time', () => {
        const { input, nowButton } = createPickerFixture({ mode: 'datetime', value: '' });

        onLoad(null, null);
        input.dispatchEvent(new Event('focus'));
        nowButton.dispatchEvent(new MouseEvent('click', { bubbles: true }));

        expect(input.value).toMatch(/^2026-03-15T14:05:09(?:\.000)?$/);
    });

    test('now populates month input with current month', () => {
        const { input, nowButton } = createPickerFixture({ mode: 'month', value: '' });

        onLoad(null, null);
        input.dispatchEvent(new Event('focus'));
        nowButton.dispatchEvent(new MouseEvent('click', { bubbles: true }));

        expect(input.value).toBe('2026-03');
    });

    test('now populates time input with current time', () => {
        const { input, nowButton } = createPickerFixture({ mode: 'time', value: '' });

        onLoad(null, null);
        input.dispatchEvent(new Event('focus'));
        nowButton.dispatchEvent(new MouseEvent('click', { bubbles: true }));

        expect(input.value).toBe('14:05:09');
    });

    test('min and max dates disable out-of-range calendar days', () => {
        const { input, picker } = createPickerFixture({
            max: '2026-03-20',
            min: '2026-03-10',
            mode: 'date',
            value: '',
        });

        onLoad(null, null);
        input.dispatchEvent(new Event('focus'));

        const outOfRange = picker.querySelector('[data-tnt-dtp-year="2026"][data-tnt-dtp-month="2"][data-tnt-dtp-day="5"]');
        const inRange = picker.querySelector('[data-tnt-dtp-year="2026"][data-tnt-dtp-month="2"][data-tnt-dtp-day="15"]');

        expect(outOfRange).not.toBeNull();
        expect(inRange).not.toBeNull();
        expect(outOfRange.disabled).toBe(true);
        expect(inRange.disabled).toBe(false);
    });

    test('disabled date values are blocked in the calendar', () => {
        const { input, picker } = createPickerFixture({
            disabledDates: '2026-03-15',
            mode: 'date',
            value: '',
        });

        onLoad(null, null);
        input.dispatchEvent(new Event('focus'));

        const disabledDate = picker.querySelector('[data-tnt-dtp-year="2026"][data-tnt-dtp-month="2"][data-tnt-dtp-day="15"]');

        expect(disabledDate).not.toBeNull();
        expect(disabledDate.disabled).toBe(true);
    });

    test('disabled time values block confirm until time changes', () => {
        const { confirmButton, input, secondInput } = createPickerFixture({
            disabledTimes: '14:05:09',
            mode: 'time',
            value: '14:05:09',
        });

        onLoad(null, null);
        input.dispatchEvent(new Event('focus'));

        expect(confirmButton.disabled).toBe(true);

        secondInput.value = '10';
        secondInput.dispatchEvent(new Event('input', { bubbles: true }));

        expect(confirmButton.disabled).toBe(false);
    });

    test('disposing one input keeps other inputs functional', () => {
        const first = createPickerFixture({ mode: 'date' });
        const second = createPickerFixture({ mode: 'date' });

        onLoad(null, null);
        first.input.dispatchEvent(new Event('focus'));
        onDispose(first.input, null);

        second.input.dispatchEvent(new Event('focus'));
        onUpdate(null, null);

        expect(second.picker.classList.contains('tnt-dtp-open')).toBe(true);
    });

    test('tab on input closes picker', () => {
        const { input, picker } = createPickerFixture({ mode: 'date', value: '' });

        onLoad(null, null);
        input.dispatchEvent(new Event('focus'));
        input.dispatchEvent(new KeyboardEvent('keydown', { bubbles: true, key: 'Tab' }));

        expect(picker.classList.contains('tnt-dtp-open')).toBe(false);
    });

    test('enter on input confirms current selection and closes picker', () => {
        const { input, picker } = createPickerFixture({ mode: 'date', value: '' });

        onLoad(null, null);
        input.dispatchEvent(new Event('focus'));
        input.dispatchEvent(new KeyboardEvent('keydown', { bubbles: true, key: 'Enter' }));

        expect(input.value).toBe('2026-03-15');
        expect(picker.classList.contains('tnt-dtp-open')).toBe(false);
    });

    test('picker controls are removed from tab order', () => {
        const { hourInput, nowButton, picker } = createPickerFixture({ mode: 'datetime' });

        onLoad(null, null);
        onUpdate(null, null);

        expect(picker.querySelectorAll('[tabindex="-1"]').length).toBeGreaterThan(0);
        expect(hourInput.getAttribute('tabindex')).toBe('-1');
        expect(nowButton.getAttribute('tabindex')).toBe('-1');
    });

    test('small viewport uses modal layout class', () => {
        const { input, picker } = createPickerFixture({ mode: 'date', value: '' });
        Object.defineProperty(window, 'innerWidth', { configurable: true, value: 500 });

        onLoad(null, null);
        input.dispatchEvent(new Event('focus'));

        expect(picker.classList.contains('tnt-dtp-modal')).toBe(true);
    });

    test('small viewport modal renders immediately without waiting for animation frame', () => {
        const { input, picker } = createPickerFixture({ mode: 'date', value: '' });
        Object.defineProperty(window, 'innerWidth', { configurable: true, value: 500 });
        const rafSpy = jest.fn(() => 1);
        global.requestAnimationFrame = rafSpy;

        onLoad(null, null);
        input.dispatchEvent(new Event('focus'));

        expect(rafSpy).not.toHaveBeenCalled();
        expect(picker.classList.contains('tnt-dtp-modal')).toBe(true);
        expect(picker.style.visibility).toBe('visible');
    });

    test('click after focus does not trigger a second open render cycle', () => {
        const { input } = createPickerFixture({ mode: 'date', value: '' });
        const rafSpy = jest.fn(callback => {
            callback();
            return 1;
        });
        global.requestAnimationFrame = rafSpy;

        onLoad(null, null);
        input.dispatchEvent(new Event('focus'));
        expect(rafSpy).toHaveBeenCalledTimes(1);

        input.dispatchEvent(new Event('click', { bubbles: true }));
        expect(rafSpy).toHaveBeenCalledTimes(1);
    });

    test('small viewport click after focus does not reopen modal picker', () => {
        const { input, picker } = createPickerFixture({ mode: 'date', value: '' });
        Object.defineProperty(window, 'innerWidth', { configurable: true, value: 500 });
        const setAttributeSpy = jest.spyOn(picker, 'setAttribute');

        onLoad(null, null);
        input.dispatchEvent(new Event('focus'));
        input.dispatchEvent(new Event('click', { bubbles: true }));

        const openStateWrites = setAttributeSpy.mock.calls.filter(([name, value]) => name === 'aria-hidden' && value === 'false');
        expect(openStateWrites).toHaveLength(1);
    });

    test('desktop layout aligns picker left edge to the label left edge', () => {
        const { input, picker } = createPickerFixture({ mode: 'date', value: '' });
        const label = document.createElement('label');
        document.body.appendChild(label);
        label.appendChild(input);

        setRect(label, { height: 72, left: 36, top: 60, width: 520 });
        setRect(input, { height: 36, left: 180, top: 88, width: 240 });

        onLoad(null, null);
        input.dispatchEvent(new Event('focus'));

        expect(picker.classList.contains('tnt-dtp-modal')).toBe(false);
        expect(picker.style.left).toBe('36px');
    });

    test('desktop layout offsets below label bounds to avoid label overlap', () => {
        const { input, picker } = createPickerFixture({ mode: 'date', value: '' });
        const label = document.createElement('label');
        document.body.appendChild(label);
        label.appendChild(input);

        setRect(label, { height: 100, left: 40, top: 60, width: 520 });
        setRect(input, { height: 36, left: 180, top: 88, width: 240 });

        onLoad(null, null);
        input.dispatchEvent(new Event('focus'));

        expect(picker.classList.contains('tnt-dtp-modal')).toBe(false);
        expect(picker.style.top).toBe('166px');
    });

    test('small viewport opens modal when input is already focused before load', () => {
        const { input, picker } = createPickerFixture({ mode: 'date', value: '' });
        Object.defineProperty(window, 'innerWidth', { configurable: true, value: 500 });

        input.focus();
        expect(document.activeElement).toBe(input);

        onLoad(null, null);

        expect(picker.classList.contains('tnt-dtp-open')).toBe(true);
        expect(picker.classList.contains('tnt-dtp-modal')).toBe(true);
    });
});
