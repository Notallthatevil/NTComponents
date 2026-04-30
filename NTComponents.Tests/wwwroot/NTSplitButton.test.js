import { jest } from '@jest/globals';
import { onLoad } from '../../NTComponents/Buttons/NTSplitButton.razor.js';

function createDotNetRef() {
   return {
      invokeMethodAsync: jest.fn(() => Promise.resolve())
   };
}

function createSplitButton({ expanded = false } = {}) {
   onLoad(null, null);

   const host = document.createElement('nt-split-button');
   if (expanded) {
      host.classList.add('nt-split-button-expanded');
   }

   const leadingButton = document.createElement('button');
   leadingButton.className = 'nt-split-button-segment nt-split-button-leading';

   const menuButton = document.createElement('button');
   menuButton.className = 'nt-split-button-segment nt-split-button-trailing';

   const panel = document.createElement('div');
   panel.className = 'nt-split-button-menu-panel';
   panel.dataset.closeOnItemClick = 'true';

   let isOpen = expanded;
   panel.matches = jest.fn((selector) => selector === ':popover-open' ? isOpen : false);
   panel.showPopover = jest.fn(() => {
      if (!isOpen) {
         isOpen = true;
         panel.dispatchEvent(new Event('toggle'));
      }
   });
   panel.hidePopover = jest.fn(() => {
      if (isOpen) {
         isOpen = false;
         panel.dispatchEvent(new Event('toggle'));
      }
   });

   host.append(leadingButton, menuButton, panel);
   document.body.appendChild(host);

   return { host, leadingButton, menuButton, panel };
}

describe('NTSplitButton custom element', () => {
   beforeEach(() => {
      document.body.innerHTML = '';
      jest.clearAllMocks();
   });

   test('native popover toggle notifies dotnet state changes', () => {
      const { host, panel } = createSplitButton();
      const dotNetRef = createDotNetRef();
      onLoad(host, dotNetRef);

      panel.showPopover();

      expect(dotNetRef.invokeMethodAsync).toHaveBeenCalledWith('NotifySplitButtonExpandedChanged', true);
      expect(host.classList.contains('nt-split-button-expanded')).toBe(true);
   });

   test('dotnet-applied expanded state does not echo a notification back to dotnet', () => {
      const { host, panel } = createSplitButton();
      const dotNetRef = createDotNetRef();
      onLoad(host, dotNetRef);
      dotNetRef.invokeMethodAsync.mockClear();

      host.classList.add('nt-split-button-expanded');
      host.update(dotNetRef);

      expect(panel.showPopover).toHaveBeenCalledTimes(1);
      expect(dotNetRef.invokeMethodAsync).not.toHaveBeenCalled();
   });

   test('leading button closes the popover through js and notifies dotnet', () => {
      const { host, leadingButton, panel } = createSplitButton({ expanded: true });
      const dotNetRef = createDotNetRef();
      onLoad(host, dotNetRef);
      dotNetRef.invokeMethodAsync.mockClear();

      leadingButton.click();

      expect(panel.hidePopover).toHaveBeenCalledTimes(1);
      expect(dotNetRef.invokeMethodAsync).toHaveBeenCalledWith('NotifySplitButtonExpandedChanged', false);
   });
});
