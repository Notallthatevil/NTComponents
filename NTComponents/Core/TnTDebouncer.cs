//MIT License

//Copyright(c) 2019 - 2024 HAVIT
//
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.
// https://github.com/havit/Havit.Blazor/blob/master/Havit.Blazor.Components.Web/Debouncer.cs

namespace NTComponents.Core;

/// <summary>
///     Debouncer helps you to debounce asynchronous actions. You can use it in your callbacks to prevent multiple calls of the same action in a short period of time.
/// </summary>
/// <param name="millisecondsDelay">The delay in milliseconds to wait before executing the action.</param>
public class TnTDebouncer(int millisecondsDelay = 300) : IDisposable {
    private readonly int _millisecondsDelay = millisecondsDelay;
    private readonly object _syncRoot = new();
    private CancellationTokenSource _debounceCancellationTokenSource = new();
    private bool _disposed;

    /// <summary>
    ///     Cancels the current debouncing action.
    /// </summary>
    /// <returns>A task that represents the asynchronous cancel operation.</returns>
    public Task CancelAsync() {
        lock (_syncRoot) {
            if (!_disposed) {
                _debounceCancellationTokenSource.Cancel();
            }

            return Task.CompletedTask;
        }
    }

    /// <summary>
    ///     Starts the debouncing.
    /// </summary>
    /// <param name="actionAsync">The asynchronous action to be executed. The <see cref="CancellationToken" /> gets canceled if the method is called again.</param>
    /// <returns>A task that represents the asynchronous debouncing operation.</returns>
    public async Task DebounceAsync(Func<CancellationToken, Task> actionAsync) {
        try {
            var cancellationToken = await DebounceAsync();

            await actionAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (TaskCanceledException) { }
        catch (ObjectDisposedException) { }
    }

    /// <summary>
    ///     Starts the debouncing and returns a result.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="funcAsync">The asynchronous function to be executed. The <see cref="CancellationToken" /> gets canceled if the method is called again.</param>
    /// <returns>A task that represents the asynchronous debouncing operation, containing the result of type <typeparamref name="T" />.</returns>
    public async Task<T> DebounceForResultAsync<T>(Func<CancellationToken, Task<T>> funcAsync) {
        try {
            var cancellationToken = await DebounceAsync();

            return await funcAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (TaskCanceledException) { }
        catch (ObjectDisposedException) { }
        return default!;
    }

    /// <summary>
    ///     Disposes the debouncer and cancels any ongoing debouncing actions.
    /// </summary>
    public void Dispose() {
        GC.SuppressFinalize(this);
        lock (_syncRoot) {
            if (_disposed) {
                return;
            }

            _disposed = true;
            _debounceCancellationTokenSource.Cancel();
            _debounceCancellationTokenSource.Dispose();
        }
    }

    /// <summary>
    ///     Handles the debouncing logic by canceling the previous action and waiting for the specified delay.
    /// </summary>
    /// <returns>A task that represents the asynchronous debouncing operation.</returns>
    private async Task<CancellationToken> DebounceAsync() {
        var cancellationToken = CreateNextDebounceToken();
        await Task.Delay(_millisecondsDelay, cancellationToken);
        return cancellationToken;
    }

    private CancellationToken CreateNextDebounceToken() {
        lock (_syncRoot) {
            ObjectDisposedException.ThrowIf(_disposed, this);

            _debounceCancellationTokenSource.Cancel();
            _debounceCancellationTokenSource.Dispose();
            _debounceCancellationTokenSource = new();
            return _debounceCancellationTokenSource.Token;
        }
    }
}
