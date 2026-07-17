using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using System.ComponentModel.DataAnnotations;

namespace NTComponents.Tests.Wizard;

public class TnTWizardFormStep_Tests : BunitContext {

    public TnTWizardFormStep_Tests() {
        // Setup JSInterop for TnTRippleEffect
        var rippleModule = JSInterop.SetupModule("./_content/NTComponents/Core/TnTRippleEffect.razor.js");
        rippleModule.SetupVoid("onLoad", _ => true);
        rippleModule.SetupVoid("onUpdate", _ => true);
        rippleModule.SetupVoid("onDispose", _ => true);
        SetRendererInfo(new RendererInfo("WebAssembly", true));
    }

    [Fact]
    public void ChildContent_Is_Required() {
        // Arrange
        var model = new TestModel { Name = "Test" };

        // Act & Assert
        var act = () => Render<TnTWizard>(p => p.AddChildContent<TnTWizardFormStep>(step => step
            .Add(s => s.Title, "Form Step")
            .Add(s => s.Model, model)
            .Add(s => s.ChildContent, (RenderFragment<EditContext>)(context => builder => {
                builder.AddContent(0, "Required content");
            }))));

        // The component should render successfully with child content
        var cut = act();
        cut.Should().NotBeNull();
        var content = cut.Find("div.tnt-wizard-content");
        content.TextContent.Should().Contain("Required content");
    }

    [Fact]
    public void DataAnnotationsValidator_Can_Be_Disabled() {
        // Arrange
        var model = new TestModel { Name = "Test" };

        // Act
        var cut = Render<TnTWizard>(p => p.AddChildContent<TnTWizardFormStep>(step => step
            .Add(s => s.Title, "Form Step")
            .Add(s => s.Model, model)
            .Add(s => s.IncludeDataAnnotationsValidator, false)
            .Add(s => s.ChildContent, (RenderFragment<EditContext>)(context => builder => {
                builder.AddContent(0, "Form without validation");
            }))));

        // Assert
        cut.HasComponent<DataAnnotationsValidator>().Should().BeFalse();
    }

    [Fact]
    public void DataAnnotationsValidator_Included_By_Default() {
        // Arrange
        var model = new TestModel { Name = "Test" };

        // Act
        var cut = Render<TnTWizard>(p => p.AddChildContent<TnTWizardFormStep>(step => step
            .Add(s => s.Title, "Form Step")
            .Add(s => s.Model, model)
            .Add(s => s.ChildContent, (RenderFragment<EditContext>)(context => builder => {
                builder.AddContent(0, "Form with validation");
            }))));

        // Assert DataAnnotationsValidator should be present in the rendered output
        cut.HasComponent<DataAnnotationsValidator>().Should().BeTrue();
    }

    [Fact]
    public void Default_FormAppearance_Is_Outlined() {
        // Arrange
        var model = new TestModel { Name = "Test" };

        // Act
        var cut = Render<TnTWizard>(p => p.AddChildContent<TnTWizardFormStep>(step => step
            .Add(s => s.Title, "Form Step")
            .Add(s => s.Model, model)
            .Add(s => s.ChildContent, (RenderFragment<EditContext>)(context => builder => {
                builder.AddContent(0, "Default form");
            }))));

        // Assert
        var form = cut.FindComponent<TnTForm>();
        form.Instance.Appearance.Should().Be(FormAppearance.Outlined);
    }

    [Fact]
    public void Disabled_Form_Sets_Disabled_Property() {
        // Arrange
        var model = new TestModel { Name = "Test" };

        // Act
        var cut = Render<TnTWizard>(p => p.AddChildContent<TnTWizardFormStep>(step => step
            .Add(s => s.Title, "Form Step")
            .Add(s => s.Model, model)
            .Add(s => s.Disabled, true)
            .Add(s => s.ChildContent, (RenderFragment<EditContext>)(context => builder => {
                builder.AddContent(0, "Disabled form");
            }))));

        // Assert
        var form = cut.FindComponent<TnTForm>();
        form.Instance.Disabled.Should().BeTrue();
    }

    [Fact]
    public void EditContext_Passed_To_ChildContent() {
        // Arrange
        var model = new TestModel { Name = "Test" };
        EditContext? receivedContext = null;

        // Act
        var cut = Render<TnTWizard>(p => p.AddChildContent<TnTWizardFormStep>(step => step
            .Add(s => s.Title, "Form Step")
            .Add(s => s.Model, model)
            .Add(s => s.ChildContent, (RenderFragment<EditContext>)(context => builder => {
                receivedContext = context;
                builder.AddContent(0, "Context test");
            }))));

        // Assert
        receivedContext.Should().NotBeNull();
        receivedContext!.Model.Should().Be(model);
    }

    [Fact]
    public void Form_Step_Integrates_With_Regular_Steps() {
        // Arrange
        var model = new TestModel { Name = "Test" };

        // Act
        var cut = Render<TnTWizard>(p => p.AddChildContent(builder => {
            builder.OpenComponent<TnTWizardStep>(0);
            builder.AddComponentParameter(10, "Title", "Regular Step");
            builder.AddComponentParameter(20, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Regular content")));
            builder.CloseComponent();

            builder.OpenComponent<TnTWizardFormStep>(30);
            builder.AddComponentParameter(40, "Title", "Form Step");
            builder.AddComponentParameter(50, "Model", model);
            builder.AddComponentParameter(60, "ChildContent",
                (RenderFragment<EditContext>)(context => b => b.AddContent(0, "Form content")));
            builder.CloseComponent();

            builder.OpenComponent<TnTWizardStep>(70);
            builder.AddComponentParameter(80, "Title", "Final Step");
            builder.AddComponentParameter(90, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Final content")));
            builder.CloseComponent();
        }));

        // Assert
        var stepIndicators = cut.FindAll("li.tnt-wizard-step-indicator");
        stepIndicators.Should().HaveCount(3);

        var stepTitles = cut.FindAll("div.tnt-wizard-step-title");
        stepTitles[0].TextContent.Should().Contain("Regular Step");
        stepTitles[1].TextContent.Should().Contain("Form Step");
        stepTitles[2].TextContent.Should().Contain("Final Step");

        var content = cut.Find("div.tnt-wizard-content");
        content.TextContent.Should().Contain("Regular content");
    }

    [Fact]
    public void Form_Step_Outside_Wizard_Throws_Exception() {
        // Arrange
        var model = new TestModel { Name = "Test" };

        // Act & Assert
        var act = () => Render<TnTWizardFormStep>(p => p
            .Add(s => s.Title, "Orphan Form Step")
            .Add(s => s.Model, model)
            .Add(s => s.ChildContent, (RenderFragment<EditContext>)(context => builder => {
                builder.AddContent(0, "Orphan form");
            })));

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*TnTWizardStep must be used within a TnTWizard component*");
    }

    [Fact]
    public void Form_Step_Renders_With_Wizard_Form_Class() {
        // Arrange
        var model = new TestModel { Name = "Test" };

        // Act
        var cut = Render<TnTWizard>(p => p.AddChildContent<TnTWizardFormStep>(step => step
            .Add(s => s.Title, "Form Step")
            .Add(s => s.Model, model)
            .Add(s => s.ChildContent, (RenderFragment<EditContext>)(context => builder => {
                builder.AddContent(0, "Form content");
            }))));

        // Assert
        var form = cut.Find("form.tnt-wizard-form");
        form.Should().NotBeNull();
    }

    [Fact]
    public void FormAppearance_Sets_Correct_Appearance() {
        // Arrange
        var model = new TestModel { Name = "Test" };

        // Act
        var cut = Render<TnTWizard>(p => p.AddChildContent<TnTWizardFormStep>(step => step
            .Add(s => s.Title, "Form Step")
            .Add(s => s.Model, model)
            .Add(s => s.FormAppearance, FormAppearance.Filled)
            .Add(s => s.ChildContent, (RenderFragment<EditContext>)(context => builder => {
                builder.AddContent(0, "Filled form");
            }))));

        // Assert
        var form = cut.FindComponent<TnTForm>();
        form.Instance.Appearance.Should().Be(FormAppearance.Filled);
    }

    [Fact]
    public void FormName_Sets_Form_Name() {
        // Arrange
        var model = new TestModel { Name = "Test" };

        // Act
        var cut = Render<TnTWizard>(p => p.AddChildContent<TnTWizardFormStep>(step => step
            .Add(s => s.Title, "Form Step")
            .Add(s => s.Model, model)
            .Add(s => s.FormName, "wizard-form")
            .Add(s => s.ChildContent, (RenderFragment<EditContext>)(context => builder => {
                builder.AddContent(0, "Named form");
            }))));

        // Assert
        var form = cut.FindComponent<TnTForm>();
        form.Instance.FormName.Should().Be("wizard-form");
    }

    [Fact]
    public void Model_Is_Required() {
        // Arrange & Act & Assert
        var act = () => Render<TnTWizard>(p => p.AddChildContent<TnTWizardFormStep>(step => step
            .Add(s => s.Title, "Form Step")
            .Add(s => s.ChildContent, (RenderFragment<EditContext>)(context => builder => {
                builder.AddContent(0, "Form content");
            }))));

        // This should throw an ArgumentNullException during OnParametersSet
        act.Should().Throw<ArgumentNullException>()
           .WithMessage("*Model*");
    }

    [Fact]
    public void Multiple_Form_Steps_Each_Have_Own_Context() {
        // Arrange
        var model1 = new TestModel { Name = "Model 1" };
        var model2 = new TestModel { Name = "Model 2" };

        // Act
        var cut = Render<TnTWizard>(p => p.AddChildContent(builder => {
            builder.OpenComponent<TnTWizardFormStep>(0);
            builder.AddComponentParameter(10, "Title", "Form Step 1");
            builder.AddComponentParameter(20, "Model", model1);
            builder.AddComponentParameter(30, "ChildContent",
                (RenderFragment<EditContext>)(context => b => b.AddContent(0, $"Form 1: {context.Model}")));
            builder.CloseComponent();

            builder.OpenComponent<TnTWizardFormStep>(40);
            builder.AddComponentParameter(50, "Title", "Form Step 2");
            builder.AddComponentParameter(60, "Model", model2);
            builder.AddComponentParameter(70, "ChildContent",
                (RenderFragment<EditContext>)(context => b => b.AddContent(0, $"Form 2: {context.Model}")));
            builder.CloseComponent();
        }));

        // Assert - First step shows first model
        var content = cut.Find("div.tnt-wizard-content");
        content.TextContent.Should().Contain("Model 1");

        // Navigate to second step
        var nextButton = cut.Find("button:contains('Next Step')");
        nextButton.Click();

        // Assert - Second step shows second model
        content = cut.Find("div.tnt-wizard-content");
        content.TextContent.Should().Contain("Model 2");
    }

    [Fact]
    public async Task OnInvalidSubmitCallback_Invoked_For_Invalid_Form() {
        // Arrange
        var model = new TestModel { Name = "" }; // Invalid
        object? invalidSubmitModel = null;

        var cut = Render<TnTWizard>(p => p.AddChildContent(builder => {
            builder.OpenComponent<TnTWizardFormStep>(0);
            builder.AddComponentParameter(10, "Title", "Form Step");
            builder.AddComponentParameter(20, "Model", model);
            builder.AddComponentParameter(25, "OnInvalidSubmitCallback", EventCallback.Factory.Create<object>(this, m => invalidSubmitModel = m));
            builder.AddComponentParameter(30, "ChildContent",
                (RenderFragment<EditContext>)(context => b => {
                    b.AddContent(0, "Form content");
                }));
            builder.CloseComponent();

            builder.OpenComponent<TnTWizardStep>(40);
            builder.AddComponentParameter(50, "Title", "Step 2");
            builder.AddComponentParameter(60, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Next step")));
            builder.CloseComponent();
        }));

        var nextButton = cut.Find("button:contains('Next Step')");

        // Act - Try to advance with invalid form
        await nextButton.ClickAsync(new MouseEventArgs());

        // Assert
        invalidSubmitModel.Should().Be(model);
    }

    [Fact]
    public async Task OnValidSubmitCallback_Invoked_For_Valid_Form() {
        // Arrange
        var model = new TestModel { Name = "Valid Name" };
        object? validSubmitModel = null;

        var cut = Render<TnTWizard>(p => p.AddChildContent(builder => {
            builder.OpenComponent<TnTWizardFormStep>(0);
            builder.AddComponentParameter(10, "Title", "Form Step");
            builder.AddComponentParameter(20, "Model", model);
            builder.AddComponentParameter(25, "OnValidSubmitCallback", EventCallback.Factory.Create<object>(this, m => validSubmitModel = m));
            builder.AddComponentParameter(30, "ChildContent",
                (RenderFragment<EditContext>)(context => b => {
                    b.AddContent(0, "Form content");
                }));
            builder.CloseComponent();

            builder.OpenComponent<TnTWizardStep>(40);
            builder.AddComponentParameter(50, "Title", "Step 2");
            builder.AddComponentParameter(60, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Next step")));
            builder.CloseComponent();
        }));

        var nextButton = cut.Find("button:contains('Next Step')");

        // Act - Advance with valid form
        await nextButton.ClickAsync(new MouseEventArgs());

        // Assert
        validSubmitModel.Should().Be(model);
    }

    [Fact]
    public void ReadOnly_Form_Sets_ReadOnly_Property() {
        // Arrange
        var model = new TestModel { Name = "Test" };

        // Act
        var cut = Render<TnTWizard>(p => p.AddChildContent<TnTWizardFormStep>(step => step
            .Add(s => s.Title, "Form Step")
            .Add(s => s.Model, model)
            .Add(s => s.ReadOnly, true)
            .Add(s => s.ChildContent, (RenderFragment<EditContext>)(context => builder => {
                builder.AddContent(0, "ReadOnly form");
            }))));

        // Assert
        var form = cut.FindComponent<TnTForm>();
        form.Instance.ReadOnly.Should().BeTrue();
    }

    [Fact]
    public void Renders_Form_Step_With_Default_Settings() {
        // Arrange
        var model = new TestModel { Name = "Test" };

        // Act
        var cut = Render<TnTWizard>(p => p.AddChildContent<TnTWizardFormStep>(step => step
            .Add(s => s.Title, "Form Step")
            .Add(s => s.Model, model)
            .Add(s => s.ChildContent, (RenderFragment<EditContext>)(context => builder => {
                builder.AddContent(0, "Form content");
            }))));

        // Assert
        var content = cut.Find("div.tnt-wizard-content");
        content.TextContent.Should().Contain("Form content");

        var form = cut.Find("form.tnt-wizard-form");
        form.Should().NotBeNull();
    }

    [Fact]
    public async Task Valid_Form_Prevents_Next_Step_Advance_When_Invalid() {
        // Arrange
        var model = new TestModel { Name = "" }; // Invalid - required field is empty

        var cut = Render<TnTWizard>(p => p.AddChildContent(builder => {
            builder.OpenComponent<TnTWizardFormStep>(0);
            builder.AddComponentParameter(10, "Title", "Form Step");
            builder.AddComponentParameter(20, "Model", model);
            builder.AddComponentParameter(30, "ChildContent",
                (RenderFragment<EditContext>)(context => b => {
                    b.AddContent(0, "Form content");
                }));
            builder.CloseComponent();

            builder.OpenComponent<TnTWizardStep>(40);
            builder.AddComponentParameter(50, "Title", "Step 2");
            builder.AddComponentParameter(60, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Next step")));
            builder.CloseComponent();
        }));

        var nextButton = cut.Find("button:contains('Next Step')");

        // Act - Try to advance with invalid form
        await nextButton.ClickAsync(new MouseEventArgs());

        // Assert - Should still be on first step
        var content = cut.Find("div.tnt-wizard-content");
        content.TextContent.Should().NotContain("Next step");
    }

    private class TestModel {

        [Required(ErrorMessage = "Name is required")]
        public string Name { get; set; } = "";

        public override string ToString() => $"TestModel(Name={Name})";
    }
}