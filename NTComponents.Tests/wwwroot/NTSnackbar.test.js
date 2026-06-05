import { jest } from '@jest/globals';
import { addSnackbar, clearSnackbars, closeSnackbar, closeSnackbarFromBlazor, onDispose, onLoad, queueSnackbar } from '../../NTComponents/Snackbar/NTSnackbar.razor.js';

describe('NTSnackbar module', () => {
   let consoleErrorSpy;

   beforeEach(() => {
      jest.useFakeTimers();
      consoleErrorSpy = jest.spyOn(console, 'error').mockImplementation(() => { });
      document.body.innerHTML = '';
      clearSnackbars();
   });

   afterEach(() => {
      jest.runOnlyPendingTimers();
      consoleErrorSpy.mockRestore();
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
      expect(host.querySelector('.nt-snackbar').style.getPropertyValue('--nt-snackbar-background-color')).toBe('');
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

   test('queued snackbar waits for the closing snackbar to leave the DOM', () => {
      const host = loadHostFromPageScript();

      queueSnackbar('First', { timeout: 0 });
      closeSnackbar();
      queueSnackbar('Second', { timeout: 0 });

      expect(host.querySelectorAll('.nt-snackbar')).toHaveLength(1);
      expect(host.querySelector('.nt-snackbar-message').textContent).toBe('First');
      expect(host.querySelector('.nt-snackbar').classList.contains('nt-closing')).toBe(true);

      jest.advanceTimersByTime(200);

      expect(host.querySelectorAll('.nt-snackbar')).toHaveLength(1);
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
      await Promise.resolve();
      jest.advanceTimersByTime(200);

      expect(host.querySelector('.nt-snackbar-message').textContent).toBe('Email archived');
      expect(dotNetReference.invokeMethodAsync).not.toHaveBeenCalledWith('NotifyClosedFromJavaScript', 'service-snackbar');
      expect(consoleErrorSpy).toHaveBeenCalledWith('Snackbar action failed.', expect.any(Error));
   });

   test('action clicks are ignored while an async action is in progress', async () => {
      const host = loadHostFromPageScript();
      let resolveAction;
      const actionCallback = jest.fn(() => new Promise(resolve => {
         resolveAction = resolve;
      }));

      addSnackbar({ message: 'Email archived', actionLabel: 'Undo', actionCallback, timeout: 0 });
      const action = host.querySelector('.nt-snackbar-action');

      action.click();
      action.click();
      expect(actionCallback).toHaveBeenCalledTimes(1);
      expect(action.disabled).toBe(true);

      resolveAction();
      await Promise.resolve();
      jest.advanceTimersByTime(200);

      expect(host.querySelector('.nt-snackbar')).toBeNull();
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

   test('closeSnackbarFromBlazor removes matching pending snackbar without notifying dotnet', () => {
      const dotNetReference = { invokeMethodAsync: jest.fn(() => Promise.resolve()) };

      queueSnackbar({
         message: 'Pending service close',
         dotNetCloseMethod: 'NotifyClosedFromJavaScript',
         dotNetReference,
         id: 'pending-service-snackbar',
         timeout: 0
      });

      expect(closeSnackbarFromBlazor('pending-service-snackbar')).toBe(true);

      const host = loadHostFromPageScript();

      expect(host.querySelector('.nt-snackbar')).toBeNull();
      expect(dotNetReference.invokeMethodAsync).not.toHaveBeenCalled();
   });

   test('closeSnackbar finds a snackbar by id across registered hosts', () => {
      const firstHost = loadHostFromPageScript();
      const secondHost = loadHostFromPageScript(createHost());

      queueSnackbar({ message: 'First host snackbar', host: firstHost, id: 'first-host', timeout: 0 });
      queueSnackbar({ message: 'Second host snackbar', host: secondHost, id: 'second-host', timeout: 0 });

      expect(closeSnackbar('first-host')).toBe(true);
      jest.advanceTimersByTime(200);

      expect(firstHost.querySelector('.nt-snackbar')).toBeNull();
      expect(secondHost.querySelector('.nt-snackbar-message').textContent).toBe('Second host snackbar');
   });

   test('timeout closes the active snackbar and notifies dotnet', () => {
      const host = loadHostFromPageScript();
      const dotNetReference = { invokeMethodAsync: jest.fn(() => Promise.resolve()) };

      addSnackbar({
         message: 'Timed',
         dotNetCloseMethod: 'NotifyClosedFromJavaScript',
         dotNetReference,
         id: 'timed-snackbar'
      });
      jest.advanceTimersByTime(4000);
      jest.advanceTimersByTime(200);

      expect(host.querySelector('.nt-snackbar')).toBeNull();
      expect(dotNetReference.invokeMethodAsync).toHaveBeenCalledWith('NotifyClosedFromJavaScript', 'timed-snackbar');
   });

   test('close button closes the active snackbar and notifies dotnet', () => {
      const host = loadHostFromPageScript();
      const dotNetReference = { invokeMethodAsync: jest.fn(() => Promise.resolve()) };

      addSnackbar({
         message: 'Closable',
         dotNetCloseMethod: 'NotifyClosedFromJavaScript',
         dotNetReference,
         id: 'closable-snackbar',
         showClose: true,
         timeout: 0
      });
      host.querySelector('.nt-snackbar-close').click();
      jest.advanceTimersByTime(200);

      expect(host.querySelector('.nt-snackbar')).toBeNull();
      expect(dotNetReference.invokeMethodAsync).toHaveBeenCalledWith('NotifyClosedFromJavaScript', 'closable-snackbar');
   });

   test('clearSnackbars notifies dotnet for active and queued snackbars', () => {
      const host = loadHostFromPageScript();
      const dotNetReference = { invokeMethodAsync: jest.fn(() => Promise.resolve()) };

      addSnackbar({ message: 'Active', dotNetCloseMethod: 'NotifyClosedFromJavaScript', dotNetReference, id: 'active', timeout: 0 });
      addSnackbar({ message: 'Queued', dotNetCloseMethod: 'NotifyClosedFromJavaScript', dotNetReference, id: 'queued', timeout: 0 });

      clearSnackbars();

      expect(host.querySelector('.nt-snackbar')).toBeNull();
      expect(dotNetReference.invokeMethodAsync).toHaveBeenCalledWith('NotifyClosedFromJavaScript', 'active');
      expect(dotNetReference.invokeMethodAsync).toHaveBeenCalledWith('NotifyClosedFromJavaScript', 'queued');
   });

   test('clearSnackbars without a host clears every registered host', () => {
      const firstHost = loadHostFromPageScript();
      const secondHost = loadHostFromPageScript(createHost());

      queueSnackbar({ message: 'First host snackbar', host: firstHost, timeout: 0 });
      queueSnackbar({ message: 'Second host snackbar', host: secondHost, timeout: 0 });

      clearSnackbars();

      expect(firstHost.querySelector('.nt-snackbar')).toBeNull();
      expect(secondHost.querySelector('.nt-snackbar')).toBeNull();
   });

   test('clearSnackbars notifies dotnet for pending snackbars', () => {
      const dotNetReference = { invokeMethodAsync: jest.fn(() => Promise.resolve()) };

      queueSnackbar({ message: 'Pending', dotNetCloseMethod: 'NotifyClosedFromJavaScript', dotNetReference, id: 'pending', timeout: 0 });

      clearSnackbars();

      expect(dotNetReference.invokeMethodAsync).toHaveBeenCalledWith('NotifyClosedFromJavaScript', 'pending');
   });

   test('pending queue is capped and dropped dotnet snackbars are notified', () => {
      const dotNetReference = { invokeMethodAsync: jest.fn(() => Promise.resolve()) };

      for (let index = 0; index < 51; index++) {
         queueSnackbar({
            message: `Pending ${index}`,
            dotNetCloseMethod: 'NotifyClosedFromJavaScript',
            dotNetReference,
            id: `pending-${index}`,
            timeout: 0
         });
      }

      const host = loadHostFromPageScript();

      expect(dotNetReference.invokeMethodAsync).toHaveBeenCalledWith('NotifyClosedFromJavaScript', 'pending-0');
      expect(host.querySelector('.nt-snackbar-message').textContent).toBe('Pending 1');
   });

   test('host queue is capped and dropped dotnet snackbars are notified', () => {
      const host = loadHostFromPageScript();
      const dotNetReference = { invokeMethodAsync: jest.fn(() => Promise.resolve()) };

      queueSnackbar({ message: 'Active', host, timeout: 0 });
      for (let index = 0; index < 51; index++) {
         queueSnackbar({
            message: `Queued ${index}`,
            dotNetCloseMethod: 'NotifyClosedFromJavaScript',
            dotNetReference,
            host,
            id: `queued-${index}`,
            timeout: 0
         });
      }

      expect(dotNetReference.invokeMethodAsync).toHaveBeenCalledWith('NotifyClosedFromJavaScript', 'queued-0');

      closeSnackbar(undefined, host);
      jest.advanceTimersByTime(200);

      expect(host.querySelector('.nt-snackbar-message').textContent).toBe('Queued 1');
   });

   test('onDispose clears host state and notifies active and queued JavaScript snackbars', () => {
      const host = loadHostFromPageScript();
      const dotNetReference = { invokeMethodAsync: jest.fn(() => Promise.resolve()) };

      addSnackbar({ message: 'Visible', dotNetCloseMethod: 'NotifyClosedFromJavaScript', dotNetReference, id: 'visible', timeout: 0 });
      addSnackbar({ message: 'Queued', dotNetCloseMethod: 'NotifyClosedFromJavaScript', dotNetReference, id: 'queued', timeout: 0 });

      onDispose(host);

      expect(host.querySelector('.nt-snackbar')).toBeNull();
      expect(closeSnackbar()).toBe(false);
      expect(dotNetReference.invokeMethodAsync).toHaveBeenCalledWith('NotifyClosedFromJavaScript', 'visible');
      expect(dotNetReference.invokeMethodAsync).toHaveBeenCalledWith('NotifyClosedFromJavaScript', 'queued');
   });
});
