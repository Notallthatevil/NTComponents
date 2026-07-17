import { jest } from '@jest/globals';
import '../../../NTComponents/wwwroot/NTComponents.lib.module.js';

describe('NTComponents.enforceCurrencyFormat', () => {
   beforeEach(() => {
      document.body.innerHTML = '';
      jest.clearAllMocks();
   });

   test('prevents non-numeric, non-modifier, and non-decimal keys', () => {
      const event = new KeyboardEvent('keydown', { keyCode: 65 });
      const preventSpy = jest.spyOn(event, 'preventDefault');

      NTComponents.enforceCurrencyFormat(event);

      expect(preventSpy).toHaveBeenCalled();
   });

   test('allows decimal point key (190)', () => {
      const event = new KeyboardEvent('keydown', { keyCode: 190 });
      const preventSpy = jest.spyOn(event, 'preventDefault');

      NTComponents.enforceCurrencyFormat(event);

      expect(preventSpy).not.toHaveBeenCalled();
   });

   test('allows comma key (188)', () => {
      const event = new KeyboardEvent('keydown', { keyCode: 188 });
      const preventSpy = jest.spyOn(event, 'preventDefault');

      NTComponents.enforceCurrencyFormat(event);

      expect(preventSpy).not.toHaveBeenCalled();
   });

   test('allows numpad decimal key (110)', () => {
      const event = new KeyboardEvent('keydown', { keyCode: 110 });
      const preventSpy = jest.spyOn(event, 'preventDefault');

      NTComponents.enforceCurrencyFormat(event);

      expect(preventSpy).not.toHaveBeenCalled();
   });

   test('allows numeric and modifier keys', () => {
      const validKeyCodes = [48, 96, 8, 9, 13, 46];

      validKeyCodes.forEach((keyCode) => {
         const event = new KeyboardEvent('keydown', { keyCode });
         const preventSpy = jest.spyOn(event, 'preventDefault');

         NTComponents.enforceCurrencyFormat(event);

         expect(preventSpy).not.toHaveBeenCalled();
      });
   });
});
