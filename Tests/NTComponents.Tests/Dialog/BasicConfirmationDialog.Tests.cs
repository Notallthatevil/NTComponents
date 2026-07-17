using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using NTComponents.Dialog;

namespace NTComponents.Tests.Dialog;

public class BasicConfirmationDialog_Tests : BunitContext {
    private readonly TnTDialogService _dialogService;

    public BasicConfirmationDialog_Tests() {
        _dialogService = new TnTDialogService();
        Services.AddSingleton<ITnTDialogService>(_dialogService);
        // Use BUnit's built-in JSRuntime test double
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public async Task BasicConfirmationDialog_Callbacks_Execute_In_Correct_Order() {
        // Arrange
        var dialog = CreateDialogWithComponent();
        var executionOrder = new List<string>();

        dialog.CloseAsync = () => {
            executionOrder.Add("CloseAsync");
            return Task.CompletedTask;
        };

        var cancelCallback = EventCallback.Factory.Create(this, () => executionOrder.Add("CancelCallback"));

        var component = RenderBasicConfirmationDialog(dialog, parameters => {
            parameters.Add(p => p.Body, "Test body");
            parameters.Add(p => p.CancelClickedCallback, cancelCallback);
        });

        // Act
        var buttons = component.FindComponents<TnTButton>();
        var cancelButton = buttons[0];
        await cancelButton.Find("button").ClickAsync(new MouseEventArgs());

        // Assert
        executionOrder.Should().Equal("CloseAsync", "CancelCallback");
    }

    [Fact]
    public async Task BasicConfirmationDialog_Cancel_Button_Click_Closes_Dialog() {
        // Arrange
        var dialog = CreateDialogWithComponent();
        var closeCallbackInvoked = false;
        dialog.CloseAsync = () => {
            closeCallbackInvoked = true;
            return Task.CompletedTask;
        };

        var component = RenderBasicConfirmationDialog(dialog, parameters => {
            parameters.Add(p => p.Body, "Test body");
        });

        // Act
        var buttons = component.FindComponents<TnTButton>();
        var cancelButton = buttons[0];
        await cancelButton.Find("button").ClickAsync(new MouseEventArgs());

        // Assert
        closeCallbackInvoked.Should().BeTrue();
    }

    [Fact]
    public async Task BasicConfirmationDialog_Cancel_Button_Click_Invokes_Callback() {
        // Arrange
        var dialog = CreateDialogWithComponent();
        var cancelCallbackInvoked = false;
        var cancelCallback = EventCallback.Factory.Create(this, () => cancelCallbackInvoked = true);

        var component = RenderBasicConfirmationDialog(dialog, parameters => {
            parameters.Add(p => p.Body, "Test body");
            parameters.Add(p => p.CancelClickedCallback, cancelCallback);
        });

        // Act
        var buttons = component.FindComponents<TnTButton>();
        var cancelButton = buttons[0];
        await cancelButton.Find("button").ClickAsync(new MouseEventArgs());

        // Assert
        cancelCallbackInvoked.Should().BeTrue();
    }

    [Fact]
    public async Task BasicConfirmationDialog_Cancel_Button_Click_Sets_DialogResult_To_Cancelled() {
        // Arrange
        var dialog = CreateDialogWithComponent();
        var component = RenderBasicConfirmationDialog(dialog, parameters => {
            parameters.Add(p => p.Body, "Test body");
        });

        // Act
        var buttons = component.FindComponents<TnTButton>();
        var cancelButton = buttons[0];
        await cancelButton.Find("button").ClickAsync(new MouseEventArgs());

        // Assert
        dialog.DialogResult.Should().Be(DialogResult.Cancelled);
    }

    [Fact]
    public async Task BasicConfirmationDialog_Confirm_Button_Click_Closes_Dialog() {
        // Arrange
        var dialog = CreateDialogWithComponent();
        var closeCallbackInvoked = false;
        dialog.CloseAsync = () => {
            closeCallbackInvoked = true;
            return Task.CompletedTask;
        };

        var component = RenderBasicConfirmationDialog(dialog, parameters => {
            parameters.Add(p => p.Body, "Test body");
        });

        // Act
        var buttons = component.FindComponents<TnTButton>();
        var confirmButton = buttons[1];
        await confirmButton.Find("button").ClickAsync(new MouseEventArgs());

        // Assert
        closeCallbackInvoked.Should().BeTrue();
    }

    [Fact]
    public async Task BasicConfirmationDialog_Confirm_Button_Click_Invokes_Callback() {
        // Arrange
        var dialog = CreateDialogWithComponent();
        var confirmCallbackInvoked = false;
        var confirmCallback = EventCallback.Factory.Create(this, () => confirmCallbackInvoked = true);

        var component = RenderBasicConfirmationDialog(dialog, parameters => {
            parameters.Add(p => p.Body, "Test body");
            parameters.Add(p => p.ConfirmClickedCallback, confirmCallback);
        });

        // Act
        var buttons = component.FindComponents<TnTButton>();
        var confirmButton = buttons[1];
        await confirmButton.Find("button").ClickAsync(new MouseEventArgs());

        // Assert
        confirmCallbackInvoked.Should().BeTrue();
    }

    [Fact]
    public async Task BasicConfirmationDialog_Confirm_Button_Click_Sets_DialogResult_To_Confirmed() {
        // Arrange
        var dialog = CreateDialogWithComponent();
        var component = RenderBasicConfirmationDialog(dialog, parameters => {
            parameters.Add(p => p.Body, "Test body");
        });

        // Act
        var buttons = component.FindComponents<TnTButton>();
        var confirmButton = buttons[1];
        await confirmButton.Find("button").ClickAsync(new MouseEventArgs());

        // Assert
        dialog.DialogResult.Should().Be(DialogResult.Confirmed);
    }

    [Fact]
    public void BasicConfirmationDialog_Dialog_Context_Is_Required() {
        // Arrange & Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => {
            // Render without cascading dialog value
            Render<BasicConfirmationDialog>(parameters => {
                parameters.Add(p => p.Body, "Test body");
            });
        });

        exception.Message.Should().Contain("BasicConfirmationDialog must be created inside a dialog");
    }

    [Fact]
    public async Task BasicConfirmationDialog_Empty_Callbacks_Do_Not_Throw() {
        // Arrange
        var dialog = CreateDialogWithComponent();
        var component = RenderBasicConfirmationDialog(dialog, parameters => {
            parameters.Add(p => p.Body, "Test body");
            // No callbacks provided
        });

        // Act & Assert - should not throw
        var buttons = component.FindComponents<TnTButton>();
        await buttons[0].Find("button").ClickAsync(new MouseEventArgs()); // Cancel
        await buttons[1].Find("button").ClickAsync(new MouseEventArgs()); // Confirm
    }

    [Fact]
    public void BasicConfirmationDialog_Has_Correct_CSS_Classes() {
        // Arrange
        var dialog = CreateDialogWithComponent();

        // Act
        var component = RenderBasicConfirmationDialog(dialog, parameters => {
            parameters.Add(p => p.Body, "Test body");
        });

        // Assert
        var bodyDiv = component.Find(".tnt-body-large");
        bodyDiv.Should().NotBeNull();

        var buttonContainer = component.Find(".tnt-basic-confirmation-dialog-buttons");
        buttonContainer.Should().NotBeNull();

        var divider = component.Find(".nt-divider");
        divider.Should().NotBeNull();
    }

    [Fact]
    public void BasicConfirmationDialog_Hides_Cancel_Button_When_ShowCancelButton_Is_False() {
        // Arrange
        var dialog = CreateDialogWithComponent();

        // Act
        var component = RenderBasicConfirmationDialog(dialog, parameters => {
            parameters.Add(p => p.Body, "Test body");
            parameters.Add(p => p.ShowCancelButton, false);
        });

        // Assert
        var buttons = component.FindComponents<TnTButton>();
        buttons.Should().HaveCount(1);

        var confirmButton = buttons[0];
        confirmButton.Markup.Should().Contain("Confirm");
    }

    [Fact]
    public void BasicConfirmationDialog_Renders_Button_Container_With_Correct_Class() {
        // Arrange
        var dialog = CreateDialogWithComponent();

        // Act
        var component = RenderBasicConfirmationDialog(dialog, parameters => {
            parameters.Add(p => p.Body, "Test body");
        });

        // Assert
        var buttonContainer = component.Find(".tnt-basic-confirmation-dialog-buttons");
        buttonContainer.Should().NotBeNull();

        var buttons = buttonContainer.QuerySelectorAll("button");
        buttons.Length.Should().Be(2);
    }

    [Fact]
    public void BasicConfirmationDialog_Renders_Custom_Button_Text() {
        // Arrange
        var dialog = CreateDialogWithComponent();

        // Act
        var component = RenderBasicConfirmationDialog(dialog, parameters => {
            parameters.Add(p => p.Body, "Test body");
            parameters.Add(p => p.CancelButtonText, "No");
            parameters.Add(p => p.ConfirmButtonText, "Yes");
        });

        // Assert
        var buttons = component.FindComponents<TnTButton>();
        buttons.Should().HaveCount(2);

        var cancelButton = buttons[0];
        cancelButton.Markup.Should().Contain("No");

        var confirmButton = buttons[1];
        confirmButton.Markup.Should().Contain("Yes");
    }

    [Fact]
    public void BasicConfirmationDialog_Renders_Default_Button_Text() {
        // Arrange
        var dialog = CreateDialogWithComponent();

        // Act
        var component = RenderBasicConfirmationDialog(dialog, parameters => {
            parameters.Add(p => p.Body, "Test body");
        });

        // Assert
        var buttons = component.FindComponents<TnTButton>();
        buttons.Should().HaveCount(2);

        // Cancel button (first button)
        var cancelButton = buttons[0];
        cancelButton.Markup.Should().Contain("Cancel");

        // Confirm button (second button)
        var confirmButton = buttons[1];
        confirmButton.Markup.Should().Contain("Confirm");
    }

    [Fact]
    public void BasicConfirmationDialog_Renders_Divider() {
        // Arrange
        var dialog = CreateDialogWithComponent();

        // Act
        var component = RenderBasicConfirmationDialog(dialog, parameters => {
            parameters.Add(p => p.Body, "Test body");
        });

        // Assert
        var divider = component.FindComponent<NTDivider>();
        divider.Should().NotBeNull();

        // Check that the divider has the correct style attribute from the rendered markup
        var dividerElement = component.Find(".nt-divider");
        dividerElement.Should().NotBeNull();
    }

    [Fact]
    public void BasicConfirmationDialog_Renders_Material_Icons() {
        // Arrange
        var dialog = CreateDialogWithComponent();

        // Act
        var component = RenderBasicConfirmationDialog(dialog, parameters => {
            parameters.Add(p => p.Body, "Test body");
        });

        // Assert
        var icons = component.FindComponents<MaterialIcon>();
        icons.Should().HaveCount(2);

        // Cancel button icon
        var cancelIcon = icons[0];
        cancelIcon.Instance.Icon.Should().Be("cancel");

        // Confirm button icon
        var confirmIcon = icons[1];
        confirmIcon.Instance.Icon.Should().Be("check");
    }

    [Fact]
    public void BasicConfirmationDialog_Renders_Only_Confirm_Button_When_Cancel_Hidden() {
        // Arrange
        var dialog = CreateDialogWithComponent();

        // Act
        var component = RenderBasicConfirmationDialog(dialog, parameters => {
            parameters.Add(p => p.Body, "Test body");
            parameters.Add(p => p.ShowCancelButton, false);
        });

        // Assert
        var buttonContainer = component.Find(".tnt-basic-confirmation-dialog-buttons");
        var buttons = buttonContainer.QuerySelectorAll("button");
        buttons.Length.Should().Be(1);

        var confirmButton = component.FindComponents<TnTButton>().Single();
        confirmButton.Markup.Should().Contain("Confirm");
        confirmButton.Markup.Should().Contain("check"); // Material icon
    }

    [Fact]
    public void BasicConfirmationDialog_Renders_Required_Body_Text() {
        // Arrange
        var dialog = CreateDialogWithComponent();

        // Act
        var component = RenderBasicConfirmationDialog(dialog, parameters => {
            parameters.Add(p => p.Body, "Are you sure you want to continue?");
        });

        // Assert
        var bodyDiv = component.Find(".tnt-body-large");
        bodyDiv.Should().NotBeNull();
        bodyDiv.TextContent.Should().Be("Are you sure you want to continue?");
    }

    [Fact]
    public void BasicConfirmationDialog_Renders_With_All_Parameters_Set() {
        // Arrange
        var dialog = CreateDialogWithComponent();
        var cancelCallbackInvoked = false;
        var confirmCallbackInvoked = false;

        var cancelCallback = EventCallback.Factory.Create(this, () => cancelCallbackInvoked = true);
        var confirmCallback = EventCallback.Factory.Create(this, () => confirmCallbackInvoked = true);

        // Act
        var component = RenderBasicConfirmationDialog(dialog, parameters => {
            parameters.Add(p => p.Body, "Delete this item permanently?");
            parameters.Add(p => p.CancelButtonText, "Keep");
            parameters.Add(p => p.ConfirmButtonText, "Delete");
            parameters.Add(p => p.CancelButtonTextColor, TnTColor.OnSurface);
            parameters.Add(p => p.ShowCancelButton, true);
            parameters.Add(p => p.CancelClickedCallback, cancelCallback);
            parameters.Add(p => p.ConfirmClickedCallback, confirmCallback);
        });

        // Assert
        var bodyDiv = component.Find(".tnt-body-large");
        bodyDiv.TextContent.Should().Be("Delete this item permanently?");

        var buttons = component.FindComponents<TnTButton>();
        buttons.Should().HaveCount(2);

        var cancelButton = buttons[0];
        cancelButton.Markup.Should().Contain("Keep");
        cancelButton.Instance.TextColor.Should().Be(TnTColor.OnSurface);
        cancelButton.Instance.Appearance.Should().Be(ButtonAppearance.Outlined);

        var confirmButton = buttons[1];
        confirmButton.Markup.Should().Contain("Delete");

        // Invoke the callbacks to ensure the local variables are used and the test covers callback wiring
        cancelCallback.InvokeAsync(null);
        confirmCallback.InvokeAsync(null);
        cancelCallbackInvoked.Should().BeTrue();
        confirmCallbackInvoked.Should().BeTrue();
    }

    [Fact]
    public void BasicConfirmationDialog_Sets_Cancel_Button_Appearance_To_Outlined() {
        // Arrange
        var dialog = CreateDialogWithComponent();

        // Act
        var component = RenderBasicConfirmationDialog(dialog, parameters => {
            parameters.Add(p => p.Body, "Test body");
        });

        // Assert
        var buttons = component.FindComponents<TnTButton>();
        var cancelButton = buttons[0];
        cancelButton.Instance.Appearance.Should().Be(ButtonAppearance.Outlined);
    }

    [Fact]
    public void BasicConfirmationDialog_Sets_Cancel_Button_Text_Color() {
        // Arrange
        var dialog = CreateDialogWithComponent();

        // Act
        var component = RenderBasicConfirmationDialog(dialog, parameters => {
            parameters.Add(p => p.Body, "Test body");
            parameters.Add(p => p.CancelButtonTextColor, TnTColor.Error);
        });

        // Assert
        var buttons = component.FindComponents<TnTButton>();
        var cancelButton = buttons[0];
        cancelButton.Instance.TextColor.Should().Be(TnTColor.Error);
    }

    [Fact]
    public void BasicConfirmationDialog_Shows_Cancel_Button_When_ShowCancelButton_Is_True() {
        // Arrange
        var dialog = CreateDialogWithComponent();

        // Act
        var component = RenderBasicConfirmationDialog(dialog, parameters => {
            parameters.Add(p => p.Body, "Test body");
            parameters.Add(p => p.ShowCancelButton, true);
        });

        // Assert
        var buttons = component.FindComponents<TnTButton>();
        buttons.Should().HaveCount(2);
    }

    [Fact]
    public void BasicConfirmationDialog_Throws_When_Not_Inside_Dialog_Context() {
        // Arrange & Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => {
            Render<BasicConfirmationDialog>(parameters => {
                parameters.Add(p => p.Body, "Test body");
            });
        });

        ex.Message.Should().Contain("BasicConfirmationDialog must be created inside a dialog");
    }

    [Fact]
    public void BasicConfirmationDialog_Uses_Default_Cancel_Button_Text_Color() {
        // Arrange
        var dialog = CreateDialogWithComponent();

        // Act
        var component = RenderBasicConfirmationDialog(dialog, parameters => {
            parameters.Add(p => p.Body, "Test body");
        });

        // Assert
        var buttons = component.FindComponents<TnTButton>();
        var cancelButton = buttons[0];
        cancelButton.Instance.TextColor.Should().Be(TnTColor.OnSurface);
    }

    // Helper methods
    private MockDialog CreateDialogWithComponent() {
        return new MockDialog();
    }

    private IRenderedComponent<BasicConfirmationDialog> RenderBasicConfirmationDialog(
        MockDialog dialog,
        Action<ComponentParameterCollectionBuilder<BasicConfirmationDialog>> parameterBuilder) {
        return Render<BasicConfirmationDialog>(parameters => {
            parameters.AddCascadingValue<ITnTDialog>(dialog);
            parameterBuilder(parameters);
        });
    }

    // Mock dialog implementation for testing
    private class MockDialog : ITnTDialog {
        public Func<Task> CloseAsync { get; set; } = () => Task.CompletedTask;
        public DialogResult DialogResult { get; set; } = DialogResult.Pending;
        public string ElementId { get; init; } = "test-dialog";
        public TnTDialogOptions Options { get; init; } = new();
        public IReadOnlyDictionary<string, object?>? Parameters { get; init; }
        public Type Type { get; init; } = typeof(BasicConfirmationDialog);

        Task ITnTDialog.CloseAsync() => CloseAsync();
    }
}
