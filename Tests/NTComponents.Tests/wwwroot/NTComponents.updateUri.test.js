import '../../../NTComponents/wwwroot/NTComponents.lib.module.js';

describe('NTComponents.updateUri', () => {
    beforeEach(() => {
        history.replaceState({ navigationIndex: 3 }, '', '/orders');
    });

    test('replaces the URI while preserving the current history entry state', () => {
        NTComponents.updateUri('/orders?ntdg-page=2');

        expect(window.location.pathname + window.location.search).toBe('/orders?ntdg-page=2');
        expect(history.state).toEqual({ navigationIndex: 3 });
    });
});
