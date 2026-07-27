using NTComponents.Core;

namespace NTComponents.Tests.Core;

public class TnTCancellationTokenComponentBase_Tests : BunitContext {

    [Fact]
    public void CancellationToken_IsCanceled_AfterDispose() {
        // Arrange
        var component = new TestCancellationTokenComponent();

        // Act
        component.Dispose();

        // Assert After dispose, ExposedToken should be CancellationToken.None
        component.ExposedToken.Should().Be(CancellationToken.None);
    }

    [Fact]
    public async Task CancellationToken_IsCanceled_AfterDisposeAsync() {
        // Arrange
        var component = new TestCancellationTokenComponent();

        // Act
        await component.DisposeAsync();

        // Assert After dispose, ExposedToken should be CancellationToken.None
        component.ExposedToken.Should().Be(CancellationToken.None);
    }

    [Fact]
    public void CancellationToken_IsNotCanceled_BeforeDisposal() {
        // Arrange
        var component = new TestCancellationTokenComponent();

        // Act & Assert
        component.ExposedToken.IsCancellationRequested.Should().BeFalse();
    }

    [Fact]
    public void Dispose_IsIdempotent() {
        // Arrange
        var component = new TestCancellationTokenComponent();

        // Act
        component.Dispose();
        var exception = Record.Exception(() => component.Dispose());

        // Assert
        exception.Should().BeNull();
    }

    [Fact]
    public void Dispose_SetsDisposedFlag() {
        // Arrange
        var component = new TestCancellationTokenComponent();

        // Act
        component.Dispose();

        // Assert
        component.IsDisposed.Should().BeTrue();
    }

    [Fact]
    public async Task DisposeAsync_IsIdempotent() {
        // Arrange
        var component = new TestCancellationTokenComponent();

        // Act
        await component.DisposeAsync();
        var exception = await Record.ExceptionAsync(() => component.DisposeAsync().AsTask());

        // Assert
        exception.Should().BeNull();
    }

    [Fact]
    public void Dispose_WhenCancellationTokenSourceWasAlreadyDisposed_StillCompletesCleanup() {
        var component = new TestCancellationTokenComponent();
        var disposedSource = new CancellationTokenSource();
        disposedSource.Dispose();
        component.ReplaceCancellationTokenSource(disposedSource);

        component.Dispose();

        component.IsDisposed.Should().BeTrue();
        component.ExposedToken.Should().Be(CancellationToken.None);
    }

    [Fact]
    public async Task DisposeAsync_WhenCancellationTokenSourceWasAlreadyDisposed_StillCompletesCleanup() {
        var component = new TestCancellationTokenComponent();
        var disposedSource = new CancellationTokenSource();
        disposedSource.Dispose();
        component.ReplaceCancellationTokenSource(disposedSource);

        await component.DisposeAsync();

        component.IsDisposed.Should().BeTrue();
        component.ExposedToken.Should().Be(CancellationToken.None);
    }

    private class TestCancellationTokenComponent : TnTCancellationTokenComponentBase {
        public override string? ElementClass => null;
        public override string? ElementStyle => null;
        public CancellationToken ExposedToken => CancellationToken;

        public bool IsDisposed => typeof(TnTCancellationTokenComponentBase)
            .GetField("_disposed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.GetValue(this) as bool? ?? false;

        public void ReplaceCancellationTokenSource(CancellationTokenSource cancellationTokenSource) {
            var field = typeof(TnTCancellationTokenComponentBase)
                .GetField("_cancellationTokenSource", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
            ((CancellationTokenSource?)field.GetValue(this))?.Dispose();
            field.SetValue(this, cancellationTokenSource);
        }
    }
}
