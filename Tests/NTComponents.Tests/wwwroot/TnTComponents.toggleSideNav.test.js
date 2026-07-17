import { jest } from '@jest/globals';
import '../../../NTComponents/wwwroot/NTComponents.lib.module.js';

describe('NTComponents.toggleSideNav', () => {
   beforeEach(() => {
      document.body.innerHTML = '';
      jest.clearAllMocks();
   });

   test('toggles tnt-toggle class on side nav toggle indicators', () => {
      const layout = document.createElement('div');
      layout.className = 'tnt-layout';

      const navContainer = document.createElement('div');
      navContainer.className = 'tnt-side-nav-toggle-indicator';

      const toggler = document.createElement('div');
      toggler.className = 'tnt-toggle-indicator';
      toggler.classList.add('tnt-toggle');

      const button = document.createElement('button');

      navContainer.appendChild(toggler);
      layout.appendChild(navContainer);
      layout.appendChild(button);
      document.body.appendChild(layout);

      const event = { target: button };

      NTComponents.toggleSideNav(event);

      expect(toggler.classList.contains('tnt-toggle')).toBe(false);
   });

   test('adds tnt-toggle class if not present', () => {
      const layout = document.createElement('div');
      layout.className = 'tnt-layout';

      const navContainer = document.createElement('div');
      navContainer.className = 'tnt-side-nav-toggle-indicator';

      const toggler = document.createElement('div');
      toggler.className = 'tnt-toggle-indicator';

      const button = document.createElement('button');

      navContainer.appendChild(toggler);
      layout.appendChild(navContainer);
      layout.appendChild(button);
      document.body.appendChild(layout);

      const event = { target: button };

      NTComponents.toggleSideNav(event);

      expect(toggler.classList.contains('tnt-toggle')).toBe(true);
   });

   test('only toggles the closest side nav indicator', () => {
      const layout1 = document.createElement('div');
      layout1.className = 'tnt-layout';
      const navContainer1 = document.createElement('div');
      navContainer1.className = 'tnt-side-nav-toggle-indicator';
      const toggler1 = document.createElement('div');
      toggler1.className = 'tnt-toggle-indicator tnt-toggle';
      const button1 = document.createElement('button');
      navContainer1.appendChild(toggler1);
      layout1.appendChild(navContainer1);
      layout1.appendChild(button1);

      const layout2 = document.createElement('div');
      layout2.className = 'tnt-layout';
      const navContainer2 = document.createElement('div');
      navContainer2.className = 'tnt-side-nav-toggle-indicator';
      const toggler2 = document.createElement('div');
      toggler2.className = 'tnt-toggle-indicator tnt-toggle';
      const button2 = document.createElement('button');
      navContainer2.appendChild(toggler2);
      layout2.appendChild(navContainer2);
      layout2.appendChild(button2);

      document.body.appendChild(layout1);
      document.body.appendChild(layout2);

      const event = { target: button1 };
      NTComponents.toggleSideNav(event);

      expect(toggler1.classList.contains('tnt-toggle')).toBe(false);
      expect(toggler2.classList.contains('tnt-toggle')).toBe(true);
   });
});
