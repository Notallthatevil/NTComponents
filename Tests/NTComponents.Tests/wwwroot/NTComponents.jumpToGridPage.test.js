import { jest } from '@jest/globals';
import '../../../NTComponents/wwwroot/NTComponents.lib.module.js';

describe('NTComponents.jumpToGridPage', () => {
    let navigateTo;

    beforeEach(() => {
        navigateTo = jest.fn();
        window.Blazor = { navigateTo };
        document.body.innerHTML = `
            <select aria-label="Go to page" data-nt-data-grid-page-href="https://example.test/orders?existing=true&amp;ntdg-page={page}#results">
                <option value="1" selected>1</option>
                <option value="2">2</option>
                <option value="3">3</option>
            </select>`;
    });

    afterEach(() => {
        delete window.Blazor;
        document.body.innerHTML = '';
    });

    test('navigates to the page href with the chosen page filled in', () => {
        const select = document.querySelector('select');
        select.value = '3';

        NTComponents.jumpToGridPage(select);

        expect(navigateTo).toHaveBeenCalledTimes(1);
        expect(navigateTo).toHaveBeenCalledWith('https://example.test/orders?existing=true&ntdg-page=3#results');
    });

    test('runs from the inline onchange attribute the grid renders', () => {
        const select = document.querySelector('select');
        select.setAttribute('onchange', 'window.NTComponents?.jumpToGridPage(this)');
        select.value = '2';

        select.dispatchEvent(new Event('change'));

        expect(navigateTo).toHaveBeenCalledWith('https://example.test/orders?existing=true&ntdg-page=2#results');
    });

    test('encodes the chosen value before filling the page placeholder', () => {
        const select = document.querySelector('select');
        select.add(new Option('a b&c', 'a b&c', true, true));

        NTComponents.jumpToGridPage(select);

        expect(navigateTo).toHaveBeenCalledWith('https://example.test/orders?existing=true&ntdg-page=a%20b%26c#results');
    });

    test('falls back to a full navigation when Blazor is not on the page', () => {
        // jsdom performs hash-only navigations, so a same-document template proves location.assign ran without stubbing it.
        delete window.Blazor;
        const select = document.querySelector('select');
        select.dataset.ntDataGridPageHref = `${window.location.pathname}${window.location.search}#grid-page-{page}`;
        select.value = '2';

        NTComponents.jumpToGridPage(select);

        expect(window.location.hash).toBe('#grid-page-2');
    });

    test('ignores selects without a page href', () => {
        const select = document.querySelector('select');
        select.removeAttribute('data-nt-data-grid-page-href');

        NTComponents.jumpToGridPage(select);

        expect(navigateTo).not.toHaveBeenCalled();
    });
});
