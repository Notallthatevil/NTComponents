import { jest } from '@jest/globals';
import { closeDialog, closeDialogFromBlazor, isOpen, onDispose, onUpdate, openDialog, openDialogFromBlazor } from '../../NTComponents/Dialog/NTDialog.razor.js';

describe('NTDialog module', () => {
   beforeEach(() => {
      document.body.innerHTML = '';
      Object.defineProperty(HTMLDialogElement.prototype, 'showModal', {
         configurable: true,
         value: jest.fn(function () {
            this.open = true;
         })
      });
      Object.defineProperty(HTMLDialogElement.prototype, 'close', {
         configurable: true,
         value: jest.fn(function (returnValue = '') {
            this.returnValue = returnValue;
            this.open = false;
            this.dispatchEvent(new Event('close'));
         })
      });
      window.NTComponents = {};
      jest.clearAllMocks();
   });

   function createDialog(id = 'test-dialog') {
      const dialog = document.createElement('dialog');
      dialog.id = id;
      dialog.dataset.ntDialog = 'true';
      document.body.appendChild(dialog);
      return dialog;
   }

   async function finishCloseAnimation(dialog) {
      expect(dialog.classList.contains('nt-dialog-closing')).toBe(true);
      const event = new Event('animationend');
      Object.defineProperty(event, 'animationName', { value: 'nt-dialog-exit' });
      dialog.dispatchEvent(event);
      await Promise.resolve();
   }

   test('openDialog opens a native modal dialog by ID', async () => {
      const dialog = createDialog();

      await expect(openDialog('test-dialog')).resolves.toBe(true);

      expect(dialog.showModal).toHaveBeenCalled();
      expect(dialog.open).toBe(true);
      expect(window.NTComponents.dialog).toBeUndefined();
   });

   test('closeDialog closes an open dialog by ID', async () => {
      const dialog = createDialog();
      dialog.open = true;

      const closePromise = closeDialog('test-dialog', 'done');
      await Promise.resolve();
      await finishCloseAnimation(dialog);
      await expect(closePromise).resolves.toBe(true);

      expect(dialog.close).toHaveBeenCalledWith('done');
      expect(dialog.returnValue).toBe('done');
   });

   test('Blazor open and close helpers bypass lifecycle callbacks', async () => {
      const dialog = createDialog();
      const dotNetRef = { invokeMethodAsync: jest.fn() };
      onUpdate(dialog, dotNetRef);

      expect(openDialogFromBlazor(dialog)).toBe(true);
      expect(dotNetRef.invokeMethodAsync).not.toHaveBeenCalled();
      expect(isOpen(dialog)).toBe(true);

      const closePromise = closeDialogFromBlazor(dialog, 'closed');
      await finishCloseAnimation(dialog);
      await expect(closePromise).resolves.toBe(true);

      expect(dialog.returnValue).toBe('closed');
   });

   test('commandfor show-modal runs cancelable lifecycle when interactive', async () => {
      const dialog = createDialog();
      const button = document.createElement('button');
      button.setAttribute('command', 'show-modal');
      button.setAttribute('commandfor', 'test-dialog');
      document.body.appendChild(button);
      const dotNetRef = {
         invokeMethodAsync: jest.fn(() => Promise.resolve(true))
      };

      onUpdate(dialog, dotNetRef);
      button.click();
      await Promise.resolve();
      await Promise.resolve();

      expect(dotNetRef.invokeMethodAsync).toHaveBeenCalledWith('RequestOpenFromJavaScript');
      expect(dotNetRef.invokeMethodAsync).toHaveBeenCalledWith('NotifyOpenedFromJavaScript');
      expect(dialog.showModal).toHaveBeenCalled();
   });

   test('commandfor show-modal does not open when lifecycle cancels', async () => {
      const dialog = createDialog();
      const button = document.createElement('button');
      button.setAttribute('command', 'show-modal');
      button.setAttribute('commandfor', 'test-dialog');
      document.body.appendChild(button);
      const dotNetRef = {
         invokeMethodAsync: jest.fn(method => Promise.resolve(method !== 'RequestOpenFromJavaScript'))
      };

      onUpdate(dialog, dotNetRef);
      button.click();
      await Promise.resolve();
      await Promise.resolve();

      expect(dialog.showModal).not.toHaveBeenCalled();
      expect(dialog.open).toBe(false);
   });

   test('commandfor request-close runs cancelable lifecycle when interactive', async () => {
      const dialog = createDialog();
      dialog.open = true;
      const button = document.createElement('button');
      button.value = 'confirm';
      button.setAttribute('command', 'request-close');
      button.setAttribute('commandfor', 'test-dialog');
      document.body.appendChild(button);
      const dotNetRef = {
         invokeMethodAsync: jest.fn(() => Promise.resolve(true))
      };

      onUpdate(dialog, dotNetRef);
      button.click();
      await Promise.resolve();
      await Promise.resolve();
      await finishCloseAnimation(dialog);

      expect(dotNetRef.invokeMethodAsync).toHaveBeenCalledWith('RequestCloseFromJavaScript', 'confirm');
      expect(dialog.close).toHaveBeenCalledWith('confirm');
   });

   test('cancel is prevented when escape close is disabled', () => {
      const dialog = createDialog();
      dialog.dataset.ntDialogCloseOnEscape = 'false';

      onUpdate(document);
      const event = new Event('cancel', { bubbles: true, cancelable: true });
      const preventDefault = jest.spyOn(event, 'preventDefault');
      const stopPropagation = jest.spyOn(event, 'stopPropagation');
      dialog.dispatchEvent(event);

      expect(preventDefault).toHaveBeenCalled();
      expect(stopPropagation).toHaveBeenCalled();
   });

   test('backdrop click does not close by default', async () => {
      const dialog = createDialog();
      dialog.open = true;
      const dotNetRef = { invokeMethodAsync: jest.fn(() => Promise.resolve(true)) };

      onUpdate(dialog, dotNetRef);
      dialog.click();
      await Promise.resolve();

      expect(dotNetRef.invokeMethodAsync).not.toHaveBeenCalledWith('RequestCloseFromJavaScript', '');
      expect(dialog.close).not.toHaveBeenCalled();
      expect(dialog.open).toBe(true);
   });

   test('backdrop click closes when explicitly enabled', async () => {
      const dialog = createDialog();
      dialog.dataset.ntDialogCloseOnBackdrop = 'true';
      dialog.open = true;
      const dotNetRef = { invokeMethodAsync: jest.fn(() => Promise.resolve(true)) };

      onUpdate(dialog, dotNetRef);
      dialog.click();
      await Promise.resolve();
      await Promise.resolve();
      await finishCloseAnimation(dialog);

      expect(dotNetRef.invokeMethodAsync).toHaveBeenCalledWith('RequestCloseFromJavaScript', '');
      expect(dialog.close).toHaveBeenCalledWith('');
      expect(dialog.open).toBe(false);
   });

   test('onDispose removes document command handlers', async () => {
      const dialog = createDialog();
      const button = document.createElement('button');
      button.setAttribute('command', 'show-modal');
      button.setAttribute('commandfor', 'test-dialog');
      document.body.appendChild(button);
      const dotNetRef = { invokeMethodAsync: jest.fn(() => Promise.resolve(true)) };

      onUpdate(dialog, dotNetRef);
      onDispose(dialog);
      button.click();
      await Promise.resolve();

      expect(dotNetRef.invokeMethodAsync).not.toHaveBeenCalled();
   });
});
