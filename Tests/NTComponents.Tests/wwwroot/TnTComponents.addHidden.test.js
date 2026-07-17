import { jest } from '@jest/globals';
import '../../../NTComponents/wwwroot/NTComponents.lib.module.js';

describe('NTComponents.addHidden', () => {
   beforeEach(() => {
      document.body.innerHTML = '';
      jest.clearAllMocks();
   });

   test('adds tnt-hidden class to element', () => {
      const element = document.createElement('div');
      NTComponents.addHidden(element);
      expect(element.classList.contains('tnt-hidden')).toBe(true);
   });

   test('does not add class if element is null', () => {
      expect(() => NTComponents.addHidden(null)).not.toThrow();
   });

   test('does not add class if element lacks classList', () => {
      expect(() => NTComponents.addHidden({})).not.toThrow();
   });

   test('does not add class twice to same element', () => {
      const element = document.createElement('div');
      NTComponents.addHidden(element);
      NTComponents.addHidden(element);
      const count = Array.from(element.classList).filter(c => c === 'tnt-hidden').length;
      expect(count).toBe(1);
   });
});
