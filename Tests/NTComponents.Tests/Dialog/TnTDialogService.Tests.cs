using Microsoft.AspNetCore.Components;
using NTComponents.Core;
using NTComponents.Dialog;

namespace NTComponents.Tests.Dialog;

public class TnTDialogService_Tests : BunitContext {

    [Fact]
    public async Task CloseAsync_With_No_OnClose_Handler_Completes_Successfully() {
        // Arrange
        var service = new TnTDialogService();
        var dialog = await service.OpenAsync<TestComponent>();

        // Act & Assert
        var closeTask = service.CloseAsync(dialog);
        await closeTask; // Should not throw
        closeTask.IsCompletedSuccessfully.Should().BeTrue();
    }

    [Fact]
    public async Task CloseAsync_With_OnClose_Handler_Invokes_Callback() {
        // Arrange
        var service = new TnTDialogService();
        var callbackInvoked = false;
        ITnTDialog? callbackDialog = null;

        service.OnClose += (dialog) => {
            callbackInvoked = true;
            callbackDialog = dialog;
            return Task.CompletedTask;
        };

        var testDialog = await service.OpenAsync<TestComponent>();

        // Act
        await service.CloseAsync(testDialog);

        // Assert
        callbackInvoked.Should().BeTrue();
        callbackDialog.Should().Be(testDialog);
    }

    [Fact]
    public void Constructor_Creates_DotNetObjectReference() {
        // Arrange & Act
        var service = new TnTDialogService();

        // Assert
        service.Reference.Should().NotBeNull();
        service.Reference.Value.Should().Be(service);
    }

    [Fact]
    public async Task Dialog_Implements_ITnTDialog() {
        // Arrange
        var service = new TnTDialogService();

        // Act
        var dialog = await service.OpenAsync<TestComponent>();

        // Assert
        dialog.Should().BeAssignableTo<ITnTDialog>();
    }

    [Fact]
    public async Task DialogImpl_CloseAsync_Calls_Service_CloseAsync() {
        // Arrange
        var service = new TnTDialogService();
        var closeCallbackInvoked = false;
        ITnTDialog? closedDialog = null;

        service.OnClose += (dialog) => {
            closeCallbackInvoked = true;
            closedDialog = dialog;
            return Task.CompletedTask;
        };

        var dialog = await service.OpenAsync<TestComponent>();

        // Act
        await dialog.CloseAsync();

        // Assert
        closeCallbackInvoked.Should().BeTrue();
        closedDialog.Should().Be(dialog);
    }

    [Fact]
    public async Task DialogImpl_DialogResult_Is_Mutable() {
        // Arrange
        var service = new TnTDialogService();
        var dialog = await service.OpenAsync<TestComponent>();

        // Act & Assert
        dialog.DialogResult.Should().Be(DialogResult.Pending);

        dialog.DialogResult = DialogResult.Confirmed;
        dialog.DialogResult.Should().Be(DialogResult.Confirmed);

        dialog.DialogResult = DialogResult.Failed;
        dialog.DialogResult.Should().Be(DialogResult.Failed);
    }

    [Fact]
    public async Task DialogImpl_Has_Unique_ElementId() {
        // Arrange
        var service = new TnTDialogService();

        // Act
        var dialog1 = await service.OpenAsync<TestComponent>();
        var dialog2 = await service.OpenAsync<TestComponent>();

        // Assert
        dialog1.ElementId.Should().NotBe(dialog2.ElementId);
        dialog1.ElementId.Should().NotBeNullOrEmpty();
        dialog2.ElementId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Multiple_OnClose_Handlers_Are_All_Invoked() {
        // Arrange
        var service = new TnTDialogService();
        var handler1Called = false;
        var handler2Called = false;

        service.OnClose += (dialog) => {
            handler1Called = true;
            return Task.CompletedTask;
        };

        service.OnClose += (dialog) => {
            handler2Called = true;
            return Task.CompletedTask;
        };

        var dialog = await service.OpenAsync<TestComponent>();

        // Act
        await service.CloseAsync(dialog);

        // Assert
        handler1Called.Should().BeTrue();
        handler2Called.Should().BeTrue();
    }

    [Fact]
    public async Task Multiple_OnOpen_Handlers_Are_All_Invoked() {
        // Arrange
        var service = new TnTDialogService();
        var handler1Called = false;
        var handler2Called = false;

        service.OnOpen += (dialog) => {
            handler1Called = true;
            return Task.CompletedTask;
        };

        service.OnOpen += (dialog) => {
            handler2Called = true;
            return Task.CompletedTask;
        };

        // Act
        await service.OpenAsync<TestComponent>();

        // Assert
        handler1Called.Should().BeTrue();
        handler2Called.Should().BeTrue();
    }

    [Fact]
    public async Task OnClose_Handler_Exception_Propagates_And_Breaks_Dialog_Closing() {
        // Arrange
        var service = new TnTDialogService();
        var goodHandlerCalled = false;

        service.OnClose += (dialog) => {
            throw new InvalidOperationException("Test exception");
        };

        service.OnClose += (dialog) => {
            goodHandlerCalled = true;
            return Task.CompletedTask;
        };

        var dialog = await service.OpenAsync<TestComponent>();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CloseAsync(dialog));

        exception.Message.Should().Be("Test exception");
        goodHandlerCalled.Should().BeFalse(); // Subsequent handlers should not be called
    }

    [Fact]
    public async Task OnOpen_Handler_Exception_Propagates_And_Breaks_Dialog_Creation() {
        // Arrange
        var service = new TnTDialogService();
        var goodHandlerCalled = false;

        service.OnOpen += (dialog) => {
            throw new InvalidOperationException("Test exception");
        };

        service.OnOpen += (dialog) => {
            goodHandlerCalled = true;
            return Task.CompletedTask;
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.OpenAsync<TestComponent>());

        exception.Message.Should().Be("Test exception");
        goodHandlerCalled.Should().BeFalse(); // Subsequent handlers should not be called
    }

    [Fact]
    public async Task OpenAsync_Generic_Creates_Dialog_With_Correct_Properties() {
        // Arrange
        var service = new TnTDialogService();
        var options = new TnTDialogOptions { Title = "Test Dialog" };
        var parameters = new Dictionary<string, object?> { { "TestParam", "TestValue" } };

        // Act
        var dialog = await service.OpenAsync<TestComponent>(options, parameters);

        // Assert
        dialog.Should().NotBeNull();
        dialog.Type.Should().Be(typeof(TestComponent));
        dialog.Options.Should().Be(options);
        dialog.Parameters.Should().BeSameAs(parameters);
        dialog.ElementId.Should().NotBeNullOrEmpty();
        dialog.DialogResult.Should().Be(DialogResult.Pending);
    }

    [Fact]
    public async Task OpenAsync_Generic_Invokes_OnOpen_Handler() {
        // Arrange
        var service = new TnTDialogService();
        var callbackInvoked = false;
        ITnTDialog? callbackDialog = null;

        service.OnOpen += (dialog) => {
            callbackInvoked = true;
            callbackDialog = dialog;
            return Task.CompletedTask;
        };

        // Act
        var dialog = await service.OpenAsync<TestComponent>();

        // Assert
        callbackInvoked.Should().BeTrue();
        callbackDialog.Should().Be(dialog);
    }

    [Fact]
    public async Task OpenAsync_Generic_With_No_Parameters_Uses_Defaults() {
        // Arrange
        var service = new TnTDialogService();

        // Act
        var dialog = await service.OpenAsync<TestComponent>();

        // Assert
        dialog.Should().NotBeNull();
        dialog.Type.Should().Be(typeof(TestComponent));
        dialog.Options.Should().NotBeNull();
        dialog.Parameters.Should().BeNull();
        dialog.ElementId.Should().NotBeNullOrEmpty();
        dialog.DialogResult.Should().Be(DialogResult.Pending);
    }

    [Fact]
    public async Task OpenAsync_RenderFragment_Creates_Dialog_With_DeferRendering() {
        // Arrange
        var service = new TnTDialogService();
        var renderFragment = (RenderFragment)((builder) => {
            builder.OpenElement(0, "div");
            builder.AddContent(1, "Test Content");
            builder.CloseElement();
        });
        var options = new TnTDialogOptions { Title = "Fragment Dialog" };

        // Act
        var dialog = await service.OpenAsync(renderFragment, options);

        // Assert
        dialog.Should().NotBeNull();
        dialog.Type.Should().Be(typeof(DeferRendering));
        dialog.Options.Should().Be(options);
        dialog.Parameters.Should().NotBeNull();
        dialog.Parameters!.Should().ContainKey(nameof(DeferRendering.ChildContent));
        dialog.Parameters![nameof(DeferRendering.ChildContent)].Should().Be(renderFragment);
    }

    [Fact]
    public async Task OpenAsync_RenderFragment_Null_RenderFragment_Throws_ArgumentNullException() {
        // Arrange
        var service = new TnTDialogService();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.OpenAsync(null!));
    }

    [Fact]
    public async Task OpenAsync_RenderFragment_With_No_Options_Uses_Default() {
        // Arrange
        var service = new TnTDialogService();
        var renderFragment = (RenderFragment)((builder) => {
            builder.AddContent(0, "Test");
        });

        // Act
        var dialog = await service.OpenAsync(renderFragment);

        // Assert
        dialog.Should().NotBeNull();
        dialog.Options.Should().NotBeNull();
        dialog.Type.Should().Be(typeof(DeferRendering));
    }

    [Fact]
    public async Task OpenForResultAsync_Generic_Creates_Dialog_With_Correct_Properties() {
        // Arrange
        var service = new TnTDialogService();
        var options = new TnTDialogOptions { Title = "Result Dialog" };
        var parameters = new Dictionary<string, object?> { { "Key", "Value" } };
        ITnTDialog? dialogFromHandler = null;

        service.OnOpen += (dialog) => {
            dialogFromHandler = dialog;
            dialog.DialogResult = DialogResult.Deleted;
            return Task.CompletedTask;
        };

        // Act
        var result = await service.OpenForResultAsync<TestComponent>(options, parameters);

        // Assert
        result.Should().Be(DialogResult.Deleted);
        dialogFromHandler.Should().NotBeNull();
        dialogFromHandler!.Type.Should().Be(typeof(TestComponent));
        dialogFromHandler.Options.Should().Be(options);
        dialogFromHandler.Parameters.Should().BeSameAs(parameters);
    }

    [Fact]
    public async Task OpenForResultAsync_Generic_With_No_OnOpen_Handler_Returns_Failed() {
        // Arrange
        var service = new TnTDialogService();

        // Act
        var result = await service.OpenForResultAsync<TestComponent>();

        // Assert
        result.Should().Be(DialogResult.Failed);
    }

    [Fact]
    public async Task OpenForResultAsync_Generic_With_OnOpen_Handler_Waits_For_Result() {
        // Arrange
        var service = new TnTDialogService();
        ITnTDialog? dialogFromHandler = null;

        service.OnOpen += (dialog) => {
            dialogFromHandler = dialog;
            // Simulate delayed dialog result setting
            Task.Run(async () => {
                await Task.Delay(100);
                dialog.DialogResult = DialogResult.Confirmed;
            });
            return Task.CompletedTask;
        };

        // Act
        var result = await service.OpenForResultAsync<TestComponent>();

        // Assert
        result.Should().Be(DialogResult.Confirmed);
        dialogFromHandler.Should().NotBeNull();
        dialogFromHandler!.DialogResult.Should().Be(DialogResult.Confirmed);
    }

    [Fact]
    public async Task OpenForResultAsync_Polls_DialogResult_Until_Not_Pending() {
        // Arrange
        var service = new TnTDialogService();
        ITnTDialog? dialogRef = null;

        service.OnOpen += (dialog) => {
            dialogRef = dialog;
            // Simulate changing result after some polls
            Task.Run(async () => {
                await Task.Delay(1200); // More than 2 polling intervals (500ms each)
                dialog.DialogResult = DialogResult.Confirmed;
            });
            return Task.CompletedTask;
        };

        var startTime = DateTime.UtcNow;

        // Act
        var result = await service.OpenForResultAsync<TestComponent>();

        var elapsedTime = DateTime.UtcNow - startTime;

        // Assert
        result.Should().Be(DialogResult.Confirmed);
        elapsedTime.TotalMilliseconds.Should().BeGreaterThan(1000); // Should have waited
        dialogRef.Should().NotBeNull();
        dialogRef!.DialogResult.Should().Be(DialogResult.Confirmed);
    }

    [Fact]
    public async Task OpenForResultAsync_RenderFragment_Creates_Dialog_With_DeferRendering() {
        // Arrange
        var service = new TnTDialogService();
        var renderFragment = (RenderFragment)((builder) => {
            builder.AddContent(0, "Result Fragment");
        });
        ITnTDialog? dialogFromHandler = null;

        service.OnOpen += (dialog) => {
            dialogFromHandler = dialog;
            dialog.DialogResult = DialogResult.Closed;
            return Task.CompletedTask;
        };

        // Act
        var result = await service.OpenForResultAsync(renderFragment);

        // Assert
        result.Should().Be(DialogResult.Closed);
        dialogFromHandler.Should().NotBeNull();
        dialogFromHandler!.Type.Should().Be(typeof(DeferRendering));
        dialogFromHandler.Parameters.Should().ContainKey(nameof(DeferRendering.ChildContent));
        dialogFromHandler.Parameters![nameof(DeferRendering.ChildContent)].Should().Be(renderFragment);
    }

    [Fact]
    public async Task OpenForResultAsync_RenderFragment_Null_RenderFragment_Throws_ArgumentNullException() {
        // Arrange
        var service = new TnTDialogService();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.OpenForResultAsync(null!));
    }

    [Fact]
    public void Service_Implements_ITnTDialogService() {
        // Arrange & Act
        var service = new TnTDialogService();

        // Assert
        service.Should().BeAssignableTo<ITnTDialogService>();
    }

    // Test component for generic dialog tests
    private class TestComponent : ComponentBase {
        [Parameter] public string? TestParam { get; set; }

        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder) {
            builder.OpenElement(0, "div");
            builder.AddContent(1, $"Test Component: {TestParam}");
            builder.CloseElement();
        }
    }
}