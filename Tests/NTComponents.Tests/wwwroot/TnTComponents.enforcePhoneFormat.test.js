import { jest } from '@jest/globals';
import '../../../NTComponents/wwwroot/NTComponents.lib.module.js';

describe('NTComponents.enforcePhoneFormat', () => {
   beforeEach(() => {
      document.body.innerHTML = '';
      jest.clearAllMocks();
   });

   test('prevents non-numeric and non-modifier keys', () => {
      const event = new KeyboardEvent('keydown', { keyCode: 65 });
      const preventSpy = jest.spyOn(event, 'preventDefault');

      NTComponents.enforcePhoneFormat(event);

      expect(preventSpy).toHaveBeenCalled();
   });

   test('allows numeric keys from number line', () => {
      for (let keyCode = 48; keyCode <= 57; keyCode++) {
         const event = new KeyboardEvent('keydown', { keyCode });
         const preventSpy = jest.spyOn(event, 'preventDefault');

         NTComponents.enforcePhoneFormat(event);

         expect(preventSpy).not.toHaveBeenCalled();
      }
   });

   test('allows numeric keys from number pad', () => {
      for (let keyCode = 96; keyCode <= 105; keyCode++) {
         const event = new KeyboardEvent('keydown', { keyCode });
         const preventSpy = jest.spyOn(event, 'preventDefault');

         NTComponents.enforcePhoneFormat(event);

         expect(preventSpy).not.toHaveBeenCalled();
      }
   });

   test('allows modifier keys', () => {
      const modifierKeyCodes = [
         8, // Backspace
         9, // Tab
         13, // Enter
         46, // Delete
         35, // End
         36, // Home
         37, 38, 39, 40, // Arrow keys
      ];

      modifierKeyCodes.forEach((keyCode) => {
         const event = new KeyboardEvent('keydown', { keyCode });
         const preventSpy = jest.spyOn(event, 'preventDefault');

         NTComponents.enforcePhoneFormat(event);

         expect(preventSpy).not.toHaveBeenCalled();
      });
   });
});
