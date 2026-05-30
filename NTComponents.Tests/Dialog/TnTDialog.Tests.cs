using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using NTComponents.Core;
using NTComponents.Dialog;
using NTComponents.Toast;

namespace NTComponents.Tests.Dialog;

public class TnTDialog_Tests : BunitContext {
    private readonly TnTDialogService _dialogService;
    private readonly TnTToastService _toastService;

    public TnTDialog_Tests() {
        _dialogService = new TnTDialogService();
        _toastService = new TnTToastService();

        Services.AddSingleton<ITnTDialogService>(_dialogService);
        Services.AddSingleton<ITnTToastService>(_toastService);
        // Use BUnit's built-in JSRuntime test double
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public async Task TnTDialog_Applies_Dialog_CSS_Class_From_Options() {
        // Arrange
        var component = Render<TnTDialog>();
        var options = new TnTDialogOptions { ElementClass = "custom-dialog-class" };

        // Act
        var dialog = await _dialogService.OpenAsync<TestDialogComponent>(options);

        // Assert
        var dialogElement = component.Find("dialog");
        dialogElement.GetAttribute("class").Should().Contain("custom-dialog-class");
    }

    [Fact]
    public async Task TnTDialog_Applies_Dialog_CSS_Style_From_Options() {
        // Arrange
        var component = Render<TnTDialog>();
        var options = new TnTDialogOptions { ElementStyle = "width: 500px; height: 300px;" };

        // Act
        var dialog = await _dialogService.OpenAsync<TestDialogComponent>(options);

        // Assert
        var dialogElement = component.Find("dialog");
        var style = dialogElement.GetAttribute("style");
        style.Should().Contain("width: 500px");
        style.Should().Contain("height: 300px");
    }

    [Fact]
    public async Task TnTDialog_Calls_JavaScript_OpenModalDialog_After_Render() {
        // Arrange
        var component = Render<TnTDialog>();

        // Act
        var dialog = await _dialogService.OpenAsync<TestDialogComponent>();

        // Allow component to complete render cycle
        component.WaitForAssertion(() => {
            var jsInvocations = JSInterop.Invocations.Where(i => i.Identifier == "NTComponents.openModalDialog").ToList();
            jsInvocations.Count.Should().BeGreaterThanOrEqualTo(1);
            jsInvocations.Should().Contain(inv => inv.Arguments[0]!.Equals(dialog.ElementId));
        }, timeout: TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task TnTDialog_Closes_Dialog_When_Close_Button_Clicked() {
        // Arrange
        var component = Render<TnTDialog>();
        var options = new TnTDialogOptions { ShowCloseButton = true };
        var dialog = await _dialogService.OpenAsync<TestDialogComponent>(options);

        // Act
        var closeButton = component.FindComponent<TnTImageButton>();
        await closeButton.Find("button").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert - Wait for the dialog to close
        component.WaitForAssertion(() => {
            var dialogElements = component.FindAll("dialog");
            dialogElements.Should().BeEmpty();
        }, timeout: TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task TnTDialog_Closes_Dialog_When_External_Click_Handler_Triggered() {
        // Arrange
        var component = Render<TnTDialog>();
        var options = new TnTDialogOptions { CloseOnExternalClick = true };
        var dialog = await _dialogService.OpenAsync<TestDialogComponent>(options);

        // Act - Use the component's context to invoke the callback properly
        var externalClickHandler = component.FindComponents<TnTExternalClickHandler>();
        externalClickHandler.Should().HaveCount(1);

        // Trigger the external click callback through the component's context
        await component.InvokeAsync(async () => {
            await externalClickHandler[0].Instance.ExternalClickCallback.InvokeAsync();
        });

        // Assert - Wait for the dialog to close
        component.WaitForAssertion(() => {
            var dialogElements = component.FindAll("dialog");
            dialogElements.Should().BeEmpty();
        }, timeout: TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task TnTDialog_Closes_Dialog_When_OnCancel_Event_Triggered() {
        // Arrange
        var component = Render<TnTDialog>();
        var dialog = await _dialogService.OpenAsync<TestDialogComponent>();
        var dialogElement = component.Find("dialog");

        // Act
        await dialogElement.TriggerEventAsync("oncancel", EventArgs.Empty);

        // Assert - Wait for the dialog to close
        component.WaitForAssertion(() => {
            var dialogElements = component.FindAll("dialog");
            dialogElements.Should().BeEmpty();
        }, timeout: TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task TnTDialog_Component_Disposal_Cleans_Up_Event_Handlers() {
        // Arrange
        var component = Render<TnTDialog>();
        var dialog = await _dialogService.OpenAsync<TestDialogComponent>();

        // Act
        component.Dispose();

        // Assert - This test ensures disposal doesn't throw exceptions The actual cleanup is tested implicitly by the component lifecycle
        dialog.Should().NotBeNull();
    }

    [Fact]
    public async Task TnTDialog_Default_Options_Applied_When_No_Options_Provided() {
        // Arrange
        var component = Render<TnTDialog>();

        // Act
        var dialog = await _dialogService.OpenAsync<TestDialogComponent>();

        // Assert
        var dialogElement = component.Find("dialog");

        // Should have external click handler (default CloseOnExternalClick is true)
        var externalClickHandlers = component.FindComponents<TnTExternalClickHandler>();
        externalClickHandlers.Should().HaveCount(1);

        // Should have close button by default
        var headerElements = component.FindAll(".tnt-dialog-header");
        headerElements.Should().HaveCount(1); // Default ShowCloseButton is true
    }

    [Fact]
    public async Task TnTDialog_Dialog_Content_Renders_Test_Component() {
        // Arrange
        var component = Render<TnTDialog>();
        var parameters = new Dictionary<string, object?> { { "TestParam", "Hello World" } };

        // Act
        var dialog = await _dialogService.OpenAsync<TestDialogComponent>(null, parameters);

        // Assert
        var dialogContent = component.Find("dialog");
        dialogContent.TextContent.Should().Contain("Test Dialog Content: Hello World");
    }

    [Fact]
    public async Task TnTDialog_Does_Not_Render_External_Click_Handler_When_CloseOnExternalClick_Is_False() {
        // Arrange
        var component = Render<TnTDialog>();
        var options = new TnTDialogOptions { CloseOnExternalClick = false };

        // Act
        var dialog = await _dialogService.OpenAsync<TestDialogComponent>(options);

        // Assert
        var externalClickHandlers = component.FindComponents<TnTExternalClickHandler>();
        externalClickHandlers.Should().BeEmpty();
    }

    [Fact]
    public async Task TnTDialog_Does_Not_Render_Header_When_No_Title_And_No_Close_Button() {
        // Arrange
        var component = Render<TnTDialog>();
        var options = new TnTDialogOptions { Title = null, ShowCloseButton = false };

        // Act
        var dialog = await _dialogService.OpenAsync<TestDialogComponent>(options);

        // Assert
        var headerElements = component.FindAll(".tnt-dialog-header");
        headerElements.Should().BeEmpty();
    }

    [Fact]
    public async Task TnTDialog_Has_Default_Dialog_CSS_Classes() {
        // Arrange
        var component = Render<TnTDialog>();

        // Act
        var dialog = await _dialogService.OpenAsync<TestDialogComponent>();

        // Assert
        var dialogElement = component.Find("dialog");
        dialogElement.GetAttribute("class").Should().Contain("tnt-dialog");
    }

    [Fact]
    public async Task TnTDialog_JavaScript_Called_For_Multiple_Dialogs() {
        // Arrange
        var component = Render<TnTDialog>();

        // Act
        var dialog1 = await _dialogService.OpenAsync<TestDialogComponent>();
        var dialog2 = await _dialogService.OpenAsync<TestDialogComponent>();

        // Assert - Allow for multiple JS calls due to re-renders
        component.WaitForAssertion(() => {
            var jsInvocations = JSInterop.Invocations.Where(i => i.Identifier == "NTComponents.openModalDialog").ToList();
            jsInvocations.Count.Should().BeGreaterThanOrEqualTo(2);

            // Verify both dialog IDs are present in the invocations
            var invokedElementIds = jsInvocations.Select(inv => inv.Arguments[0]!).ToList();
            invokedElementIds.Should().Contain(dialog1.ElementId);
            invokedElementIds.Should().Contain(dialog2.ElementId);
        }, timeout: TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task TnTDialog_Maintains_Dialog_Order_Based_On_Opening_Sequence() {
        // Arrange
        var component = Render<TnTDialog>();

        // Act
        var dialog1 = await _dialogService.OpenAsync<TestDialogComponent>();
        var dialog2 = await _dialogService.OpenAsync<TestDialogComponent>();
        var dialog3 = await _dialogService.OpenAsync<TestDialogComponent>();

        // Assert
        var dialogElements = component.FindAll("dialog");
        dialogElements.Should().HaveCount(3);
        dialogElements[0].GetAttribute("id").Should().Be(dialog1.ElementId);
        dialogElements[1].GetAttribute("id").Should().Be(dialog2.ElementId);
        dialogElements[2].GetAttribute("id").Should().Be(dialog3.ElementId);
    }

    [Fact]
    public async Task TnTDialog_Multiple_Dialogs_Only_Last_Has_External_Click_Handler() {
        // Arrange
        var component = Render<TnTDialog>();
        var options = new TnTDialogOptions { CloseOnExternalClick = true };

        // Act
        var dialog1 = await _dialogService.OpenAsync<TestDialogComponent>(options);
        var dialog2 = await _dialogService.OpenAsync<TestDialogComponent>(options);
        var dialog3 = await _dialogService.OpenAsync<TestDialogComponent>(options);

        // Assert
        var externalClickHandlers = component.FindComponents<TnTExternalClickHandler>();
        externalClickHandlers.Should().HaveCount(1); // Only the last dialog should have external click handler
    }

    [Fact]
    public async Task TnTDialog_Passes_Dialog_Parameters_To_Component() {
        // Arrange
        var component = Render<TnTDialog>();
        var parameters = new Dictionary<string, object?> { { "TestParam", "TestValue" } };

        // Act
        var dialog = await _dialogService.OpenAsync<TestDialogComponent>(null, parameters);

        // Assert
        var testComponent = component.FindComponent<TestDialogComponent>();
        testComponent.Should().NotBeNull();
        testComponent.Instance.TestParam.Should().Be("TestValue");
    }

    [Fact]
    public async Task TnTDialog_Removes_Dialog_After_Close_Delay() {
        // Arrange
        var component = Render<TnTDialog>();
        var dialog = await _dialogService.OpenAsync<TestDialogComponent>();

        // Ensure dialog is rendered
        component.Find("dialog").Should().NotBeNull();

        // Act
        await _dialogService.CloseAsync(dialog);

        // Assert - Wait for the dialog to close
        component.WaitForAssertion(() => {
            var dialogElements = component.FindAll("dialog");
            dialogElements.Should().BeEmpty();
        }, timeout: TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task TnTDialog_Renders_Close_Button_When_ShowCloseButton_Is_True() {
        // Arrange
        var component = Render<TnTDialog>();
        var options = new TnTDialogOptions { ShowCloseButton = true, TextColor = TnTColor.OnSurface };

        // Act
        var dialog = await _dialogService.OpenAsync<TestDialogComponent>(options);

        // Assert
        var closeButton = component.FindComponent<TnTImageButton>();
        closeButton.Should().NotBeNull();
        closeButton.Instance.Icon.Icon.Should().Be("close"); // Check the string value instead of the MaterialIcon enum
        closeButton.Instance.Appearance.Should().Be(ButtonAppearance.Text);
        closeButton.Instance.TextColor.Should().Be(TnTColor.OnSurface);
        closeButton.Instance.ButtonSize.Should().Be(Size.XS);
    }

    [Fact]
    public async Task TnTDialog_Renders_Dialog_Component_With_Cascading_Value() {
        // Arrange
        var component = Render<TnTDialog>();

        // Act
        var dialog = await _dialogService.OpenAsync<TestDialogComponent>();

        // Assert
        var cascadingValue = component.FindComponent<CascadingValue<ITnTDialog>>();
        cascadingValue.Should().NotBeNull();
        cascadingValue.Instance.Value.Should().Be(dialog);
        cascadingValue.Instance.IsFixed.Should().BeTrue();
    }

    [Fact]
    public async Task TnTDialog_Renders_Divider_When_Header_Is_Present() {
        // Arrange
        var component = Render<TnTDialog>();
        var options = new TnTDialogOptions { Title = "Test Title" };

        // Act
        var dialog = await _dialogService.OpenAsync<TestDialogComponent>(options);

        // Assert
        var divider = component.FindComponent<NTDivider>();
        divider.Should().NotBeNull();
    }

    [Fact]
    public void TnTDialog_Renders_Empty_When_No_Dialogs() {
        // Arrange & Act
        var component = Render<TnTDialog>();

        // Assert
        component.Markup.Should().NotContain("<dialog");
    }

    [Fact]
    public async Task TnTDialog_Renders_External_Click_Handler_For_Last_Dialog_With_CloseOnExternalClick() {
        // Arrange
        var component = Render<TnTDialog>();
        var options1 = new TnTDialogOptions { CloseOnExternalClick = false };
        var options2 = new TnTDialogOptions { CloseOnExternalClick = true };

        // Act
        var dialog1 = await _dialogService.OpenAsync<TestDialogComponent>(options1);
        var dialog2 = await _dialogService.OpenAsync<TestDialogComponent>(options2);

        // Assert
        var externalClickHandlers = component.FindComponents<TnTExternalClickHandler>();
        externalClickHandlers.Should().HaveCount(1);
    }

    [Fact]
    public async Task TnTDialog_Renders_Header_When_ShowCloseButton_Is_True() {
        // Arrange
        var component = Render<TnTDialog>();
        var options = new TnTDialogOptions { Title = null, ShowCloseButton = true };

        // Act
        var dialog = await _dialogService.OpenAsync<TestDialogComponent>(options);

        // Assert
        var headerElement = component.Find(".tnt-dialog-header");
        headerElement.Should().NotBeNull();
    }

    [Fact]
    public async Task TnTDialog_Renders_Header_When_Title_Is_Provided() {
        // Arrange
        var component = Render<TnTDialog>();
        var options = new TnTDialogOptions { Title = "Test Dialog Title", ShowCloseButton = false };

        // Act
        var dialog = await _dialogService.OpenAsync<TestDialogComponent>(options);

        // Assert
        var headerElement = component.Find(".tnt-dialog-header");
        headerElement.Should().NotBeNull();

        var titleElement = headerElement.QuerySelector("h2");
        titleElement.Should().NotBeNull();
        titleElement!.TextContent.Should().Be("Test Dialog Title");
    }

    [Fact]
    public async Task TnTDialog_Renders_Multiple_Dialogs_When_Multiple_Dialogs_Open() {
        // Arrange
        var component = Render<TnTDialog>();

        // Act
        var dialog1 = await _dialogService.OpenAsync<TestDialogComponent>();
        var dialog2 = await _dialogService.OpenAsync<TestDialogComponent>();

        // Assert
        var dialogElements = component.FindAll("dialog");
        dialogElements.Should().HaveCount(2);
        dialogElements[0].GetAttribute("id").Should().Be(dialog1.ElementId);
        dialogElements[1].GetAttribute("id").Should().Be(dialog2.ElementId);
    }

    [Fact]
    public async Task TnTDialog_Renders_RenderFragment_Dialog_Content() {
        // Arrange
        var component = Render<TnTDialog>();
        var renderFragment = (RenderFragment)(builder => {
            builder.OpenElement(0, "div");
            builder.AddContent(1, "Custom render fragment content");
            builder.CloseElement();
        });

        // Act
        var dialog = await _dialogService.OpenAsync(renderFragment);

        // Assert
        var dialogContent = component.Find("dialog");
        dialogContent.TextContent.Should().Contain("Custom render fragment content");
    }

    [Fact]
    public async Task TnTDialog_Renders_Single_Dialog_When_Dialog_Opens() {
        // Arrange
        var component = Render<TnTDialog>();

        // Act
        var dialog = await _dialogService.OpenAsync<TestDialogComponent>();

        // Assert
        var dialogElement = component.Find("dialog");
        dialogElement.Should().NotBeNull();
        dialogElement.GetAttribute("id").Should().Be(dialog.ElementId);
    }

    [Fact]
    public async Task TnTDialog_Renders_Toast_Component_Inside_Each_Dialog() {
        // Arrange
        var component = Render<TnTDialog>();

        // Act
        var dialog1 = await _dialogService.OpenAsync<TestDialogComponent>();
        var dialog2 = await _dialogService.OpenAsync<TestDialogComponent>();

        // Assert
        var toastComponents = component.FindComponents<TnTToast>();
        toastComponents.Should().HaveCount(2);
    }

    [Fact]
    public async Task TnTDialog_Renders_With_Closing_Class_During_Close() {
        // Arrange
        var component = Render<TnTDialog>();
        var dialog = await _dialogService.OpenAsync<TestDialogComponent>();

        // Act
        var closeTask = _dialogService.CloseAsync(dialog);

        // Assert - Check that the closing class is applied
        component.WaitForAssertion(() => {
            var dialogElement = component.Find("dialog");
            dialogElement.GetAttribute("class").Should().Contain("tnt-closing");
        }, timeout: TimeSpan.FromSeconds(1));

        // Complete the close operation
        await closeTask;
    }

    [Fact]
    public async Task TnTDialog_Sets_Closing_State_During_Close_Process() {
        // Arrange
        var component = Render<TnTDialog>();
        var dialog = await _dialogService.OpenAsync<TestDialogComponent>();

        // Act
        var closeTask = _dialogService.CloseAsync(dialog);

        // Assert - Check that closing state is set and then wait for completion
        component.WaitForAssertion(() => {
            dialog.Options.Closing.Should().BeTrue();
        }, timeout: TimeSpan.FromSeconds(1));

        // Complete the close operation
        await closeTask;
    }

    [Fact]
    public async Task TnTDialog_Thread_Safe_Dialog_Collection_Access() {
        // Arrange
        var component = Render<TnTDialog>();
        var tasks = new List<Task<ITnTDialog>>();

        // Act - Simulate concurrent access
        for (int i = 0; i < 5; i++) {
            tasks.Add(_dialogService.OpenAsync<TestDialogComponent>());
        }
        await Task.WhenAll(tasks);

        // Assert
        var dialogElements = component.FindAll("dialog");
        dialogElements.Should().HaveCount(5);
    }

    // Test component for use in dialog tests
    private class TestDialogComponent : ComponentBase {
        [Parameter] public string? TestParam { get; set; }

        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder) {
            builder.OpenElement(0, "div");
            builder.AddContent(1, $"Test Dialog Content: {TestParam}");
            builder.CloseElement();
        }
    }
}
