using System;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components.Rendering;
using Xunit;
using NTComponents.Core;

namespace NTComponents.Tests.Core;

public class TnTDisposableComponentBase_Tests : BunitContext {

    [Fact]
    public async Task Dispose_CanBeCalledAfterDisposeAsync() {
        // Arrange
        var component = new TestDisposableComponent();
        await component.DisposeAsync();

        // Act
        component.Dispose();

        // Assert
        component.DisposeCalled.Should().BeTrue();
        component.DisposeAsyncCoreCalled.Should().BeTrue();
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes_Idempotent() {
        // Arrange
        var component = new TestDisposableComponent();

        // Act
        component.Dispose();
        component.Dispose();

        // Assert
        component.DisposeCallCount.Should().Be(2);
    }

    [Fact]
    public void Dispose_SetsDisposeCalledAndDisposingArgTrue() {
        // Arrange
        var component = new TestDisposableComponent();

        // Act
        component.Dispose();

        // Assert
        component.DisposeCalled.Should().BeTrue();
        component.DisposingArg.Should().BeTrue();
    }

    [Fact]
    public async Task DisposeAsync_CanBeCalledAfterDispose() {
        // Arrange
        var component = new TestDisposableComponent();
        component.Dispose();

        // Act
        await component.DisposeAsync();

        // Assert
        component.DisposeCalled.Should().BeTrue();
        component.DisposeAsyncCoreCalled.Should().BeTrue();
    }

    [Fact]
    public async Task DisposeAsync_CanBeCalledMultipleTimes_Idempotent() {
        // Arrange
        var component = new TestDisposableComponent();

        // Act
        await component.DisposeAsync();
        await component.DisposeAsync();

        // Assert
        component.DisposeAsyncCoreCallCount.Should().Be(2);
    }

    [Fact]
    public async Task DisposeAsync_SetsDisposeAsyncCoreCalledAndDisposeCalledAndDisposingArgFalse() {
        // Arrange
        var component = new TestDisposableComponent();

        // Act
        await component.DisposeAsync();

        // Assert
        component.DisposeAsyncCoreCalled.Should().BeTrue();
        component.DisposeCalled.Should().BeTrue();
        component.DisposingArg.Should().BeFalse();
    }

    [Fact]
    public async Task DisposeAsyncCore_BaseImplementation_CompletesSynchronously() {
        // Arrange
        var component = new NoOverrideDisposableComponent();

        // Act
        var task = component.DisposeAsync();

        // Assert
        task.IsCompleted.Should().BeTrue();
        await task;
    }

    private class NoOverrideDisposableComponent : TnTDisposableComponentBase {
        public override string? ElementClass => null;
        public override string? ElementStyle => null;

        protected override void BuildRenderTree(RenderTreeBuilder builder) {
        }
    }

    private class TestDisposableComponent : TnTDisposableComponentBase {
        public int DisposeAsyncCoreCallCount { get; private set; }
        public bool DisposeAsyncCoreCalled { get; private set; }
        public int DisposeCallCount { get; private set; }
        public bool DisposeCalled { get; private set; }
        public bool DisposingArg { get; private set; }
        public override string? ElementClass => null;
        public override string? ElementStyle => null;

        protected override void BuildRenderTree(RenderTreeBuilder builder) {
            builder.OpenElement(0, "div");
            builder.AddContent(1, "Test");
            builder.CloseElement();
        }

        protected override void Dispose(bool disposing) {
            DisposeCalled = true;
            DisposingArg = disposing;
            DisposeCallCount++;
            base.Dispose(disposing);
        }

        protected override async ValueTask DisposeAsyncCore() {
            DisposeAsyncCoreCalled = true;
            DisposeAsyncCoreCallCount++;
            await base.DisposeAsyncCore();
        }
    }
}