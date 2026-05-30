import { jest } from '@jest/globals';
import { addSnackbar, clearSnackbars, closeSnackbar, closeSnackbarFromBlazor, onDispose, onLoad, queueSnackbar } from '../../NTComponents/Snackbar/NTSnackbar.razor.js';

describe('NTSnackbar module', () => {
   beforeEach(() => {
      jest.useFakeTimers();
      document.body.innerHTML = '';
      clearSnackbars();
   });

   afterEach(() => {
      jest.runOnlyPendingTimers();
      jest.useRealTimers();
      document.body.innerHTML = '';
   });

   function createHost() {
      const host = document.createElement('div');
      host.className = 'nt-snackbar-container nt-snackbar-bottom-center';
      host.dataset.ntSnackbarHost = 'true';
      document.body.appendChild(host);
      return host;
   }

   function loadHostFromPageScript(host = createHost()) {
      const pageScript = document.createElement('tnt-page-script');
      host.after(pageScript);
      onLoad(pageScript);
      return host;
   }

   test('onLoad registers the host and exposes the static bridge', () => {
      const host = loadHostFromPageScript();

      window.NTSnackbar.addSnackbar('Saved');

      expect(host.querySelector('.nt-snackbar-message').textContent).toBe('Saved');
      expect(host.querySelector('.nt-snackbar').getAttribute('role')).toBe('status');
      expect(host.querySelector('.nt-snackbar').classList.contains('nt-elevation-medium')).toBe(true);
      expect(host.querySelector('.nt-snackbar').style.getPropertyValue('--nt-snackbar-background-color')).toBe('var(--tnt-color-inverse-surface)');
   });

   test('queueSnackbar waits for a host when called before setup', () => {
      queueSnackbar('Queued before render');

      const host = loadHostFromPageScript();

      expect(host.querySelector('.nt-snackbar-message').textContent).toBe('Queued before render');
   });

   test('addSnackbar renders action and close buttons for action snackbars', async () => {
      const host = loadHostFromPageScript();
      const actionCallback = jest.fn();

      addSnackbar({ message: 'Email archived', actionLabel: 'Undo', actionCallback });
      host.querySelector('.nt-snackbar-action').click();
      await Promise.resolve();
      jest.advanceTimersByTime(200);

      expect(actionCallback).toHaveBeenCalledTimes(1);
      expect(host.querySelector('.nt-snackbar')).toBeNull();
   });

   test('queued snackbars render one at a time', () => {
      const host = loadHostFromPageScript();

      queueSnackbar('First', { timeout: 0 });
      queueSnackbar('Second', { timeout: 0 });

      expect(host.querySelector('.nt-snackbar-message').textContent).toBe('First');

      closeSnackbar();
      jest.advanceTimersByTime(200);

      expect(host.querySelector('.nt-snackbar-message').textContent).toBe('Second');
   });

   test('dotnet action callback closes only after action succeeds and notifies close', async () => {
      const host = loadHostFromPageScript();
      const dotNetReference = { invokeMethodAsync: jest.fn(() => Promise.resolve()) };

      addSnackbar({
         message: 'Email archived',
         actionLabel: 'Undo',
         dotNetActionMethod: 'InvokeActionFromJavaScript',
         dotNetCloseMethod: 'NotifyClosedFromJavaScript',
         dotNetReference,
         id: 'service-snackbar',
         timeout: 0
      });

      host.querySelector('.nt-snackbar-action').click();
      await Promise.resolve();
      jest.advanceTimersByTime(200);

      expect(dotNetReference.invokeMethodAsync).toHaveBeenCalledWith('InvokeActionFromJavaScript', 'service-snackbar');
      expect(dotNetReference.invokeMethodAsync).toHaveBeenCalledWith('NotifyClosedFromJavaScript', 'service-snackbar');
      expect(host.querySelector('.nt-snackbar')).toBeNull();
   });

   test('dotnet action rejection keeps snackbar visible', async () => {
      const host = loadHostFromPageScript();
      const dotNetReference = { invokeMethodAsync: jest.fn(() => Promise.reject(new Error('boom'))) };

      addSnackbar({
         message: 'Email archived',
         actionLabel: 'Undo',
         dotNetActionMethod: 'InvokeActionFromJavaScript',
         dotNetCloseMethod: 'NotifyClosedFromJavaScript',
         dotNetReference,
         id: 'service-snackbar',
         timeout: 0
      });

      host.querySelector('.nt-snackbar-action').click();
      await Promise.resolve();
      jest.advanceTimersByTime(200);

      expect(host.querySelector('.nt-snackbar-message').textContent).toBe('Email archived');
      expect(dotNetReference.invokeMethodAsync).not.toHaveBeenCalledWith('NotifyClosedFromJavaScript', 'service-snackbar');
   });

   test('closeSnackbarFromBlazor closes without dotnet close notification', () => {
      const host = loadHostFromPageScript();
      const dotNetReference = { invokeMethodAsync: jest.fn(() => Promise.resolve()) };

      addSnackbar({
         message: 'Service close',
         dotNetCloseMethod: 'NotifyClosedFromJavaScript',
         dotNetReference,
         id: 'service-snackbar',
         timeout: 0
      });

      expect(closeSnackbarFromBlazor('service-snackbar')).toBe(true);
      jest.advanceTimersByTime(200);

      expect(host.querySelector('.nt-snackbar')).toBeNull();
      expect(dotNetReference.invokeMethodAsync).not.toHaveBeenCalled();
   });

   test('timeout closes the active snackbar', () => {
      const host = loadHostFromPageScript();

      addSnackbar('Timed');
      jest.advanceTimersByTime(6000);
      jest.advanceTimersByTime(200);

      expect(host.querySelector('.nt-snackbar')).toBeNull();
   });

   test('onDispose clears host state and active JavaScript snackbar', () => {
      const host = loadHostFromPageScript();
      addSnackbar('Visible');

      onDispose(host);

      expect(host.querySelector('.nt-snackbar')).toBeNull();
      expect(closeSnackbar()).toBe(false);
   });
});
