using NTComponents.Core;

namespace NTComponents.Tests.Core {

    public class TnTDebouncerTests {

        [Fact]
        public async Task ActionToken_IsCanceled_WhenSubsequentCallOccurs() {
            // Arrange
            var debouncer = new TnTDebouncer(millisecondsDelay: 20);
            var actionStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            bool wasCanceled = false;

            // Act
            var first = debouncer.DebounceAsync(async token => {
                actionStarted.TrySetResult();
                try {
                    await Task.Delay(TimeSpan.FromMinutes(1), token);
                }
                catch (OperationCanceledException) {
                    wasCanceled = true;
                }
            });
            await actionStarted.Task; // ensure first action started
            var second = debouncer.DebounceAsync(_ => Task.CompletedTask);
            await Task.WhenAll(first, second);

            // Assert
            wasCanceled.Should().BeTrue();
        }

        [Fact]
        public async Task CancelAsync_PreventsPendingExecution() {
            // Arrange
            var debouncer = new TnTDebouncer(millisecondsDelay: 60);
            bool executed = false;
            var task = debouncer.DebounceAsync(_ => { executed = true; return Task.CompletedTask; });

            // Act
            await debouncer.CancelAsync();
            await task; // should complete without running the action
            await Task.Delay(80, Xunit.TestContext.Current.CancellationToken); // ensure enough time has passed beyond delay

            // Assert
            executed.Should().BeFalse();
        }

        [Fact]
        public async Task Debounce_ExecutesEachAction_WhenCallsSpacedBeyondDelay() {
            // Arrange
            var debouncer = new TnTDebouncer(millisecondsDelay: 25);
            int callCount = 0;

            // Act
            await debouncer.DebounceAsync(_ => { callCount++; return Task.CompletedTask; });
            await Task.Delay(40, Xunit.TestContext.Current.CancellationToken);
            await debouncer.DebounceAsync(_ => { callCount++; return Task.CompletedTask; });

            // Assert
            callCount.Should().Be(2);
        }

        [Fact]
        public async Task Debounce_ExecutesOnlyLastAction_WhenSecondCallWithinDelay() {
            // Arrange
            var debouncer = new TnTDebouncer(millisecondsDelay: 120);
            int callCount = 0;
            string? last = null;

            // Act
            var first = debouncer.DebounceAsync(_ => { callCount++; last = "A"; return Task.CompletedTask; });
            await Task.Delay(20, Xunit.TestContext.Current.CancellationToken);
            var second = debouncer.DebounceAsync(_ => { callCount++; last = "B"; return Task.CompletedTask; });
            await Task.WhenAll(first, second);

            // Assert
            callCount.Should().Be(1);
            last.Should().Be("B");
        }

        [Fact]
        public async Task Debounce_ExecutesOnlyOneAction_WhenCallsStartConcurrently() {
            // Arrange
            var debouncer = new TnTDebouncer(millisecondsDelay: 60);
            var callCount = 0;

            // Act
            var tasks = Enumerable.Range(0, 10)
                .Select(_ => debouncer.DebounceAsync(_ => {
                    Interlocked.Increment(ref callCount);
                    return Task.CompletedTask;
                }))
                .ToArray();
            await Task.WhenAll(tasks);

            // Assert
            callCount.Should().Be(1);
        }

        [Fact]
        public async Task DebounceForResult_ReturnsDefault_WhenCanceledByNextCall() {
            // Arrange
            var debouncer = new TnTDebouncer(millisecondsDelay: 40);

            // Act
            var first = debouncer.DebounceForResultAsync<string>(_ => Task.FromResult("first"));
            var second = debouncer.DebounceForResultAsync<string>(_ => Task.FromResult("second"));
            var results = await Task.WhenAll(first, second);

            // Assert
            results[0].Should().BeNull();
            results[1].Should().Be("second");
        }

        [Fact]
        public async Task DebounceForResult_ReturnsResult_WhenNotCanceled() {
            // Arrange
            var debouncer = new TnTDebouncer(millisecondsDelay: 30);

            // Act
            var result = await debouncer.DebounceForResultAsync<int>(_ => Task.FromResult(42));

            // Assert
            result.Should().Be(42);
        }

        [Fact]
        public async Task Dispose_CancelsPendingDelay_AndPreventsExecution() {
            // Arrange
            var debouncer = new TnTDebouncer(millisecondsDelay: 60);
            bool executed = false;
            var task = debouncer.DebounceAsync(_ => { executed = true; return Task.CompletedTask; });

            // Act
            debouncer.Dispose();
            await task; // swallow cancellation internally
            await Task.Delay(80, Xunit.TestContext.Current.CancellationToken);

            // Assert
            executed.Should().BeFalse();
        }

        [Fact]
        public async Task ZeroDelay_RunsImmediately() {
            // Arrange
            var debouncer = new TnTDebouncer(millisecondsDelay: 0);
            bool executed = false;

            // Act
            await debouncer.DebounceAsync(_ => { executed = true; return Task.CompletedTask; });

            // Assert
            executed.Should().BeTrue();
        }
    }
}
