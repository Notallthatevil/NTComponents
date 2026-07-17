using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components.Web;

namespace NTComponents.Tests.Wizard;

public class TnTWizard_Tests : BunitContext {

    public TnTWizard_Tests() {
        TestingUtility.TestingUtility.SetupRippleEffectModule(this);
    }

    [Fact]
    public void AdditionalAttributes_Are_Applied() {
        // Arrange
        var attrs = new Dictionary<string, object> {
            { "class", "custom-wizard" },
            { "data-test", "wizard" }
        };

        // Act
        var cut = Render<TnTWizard>(p => p
            .Add(c => c.AdditionalAttributes, attrs)
            .AddChildContent("Content"));

        // Assert
        var wizard = cut.Find("div.tnt-wizard");
        var cls = wizard.GetAttribute("class")!;
        cls.Should().Contain("custom-wizard");
        cls.Should().Contain("tnt-wizard");
        wizard.GetAttribute("data-test").Should().Be("wizard");
    }

    [Fact]
    public void ElementId_Sets_Id_Attribute() {
        // Arrange & Act
        var cut = Render<TnTWizard>(p => p
            .Add(c => c.ElementId, "wizard-id")
            .AddChildContent("Content"));

        // Assert
        var wizard = cut.Find("div.tnt-wizard");
        wizard.GetAttribute("id").Should().Be("wizard-id");
    }

    [Fact]
    public void ElementTitle_Sets_Title_Attribute() {
        // Arrange & Act
        var cut = Render<TnTWizard>(p => p
            .Add(c => c.ElementTitle, "Wizard tooltip")
            .AddChildContent("Content"));

        // Assert
        var wizard = cut.Find("div.tnt-wizard");
        wizard.GetAttribute("title").Should().Be("Wizard tooltip");
    }

    [Fact]
    public void Has_TnTId_Attribute() {
        // Arrange & Act
        var cut = Render<TnTWizard>(p => p.AddChildContent("Content"));
        var wizard = cut.Find("div.tnt-wizard");

        // Assert
        wizard.HasAttribute("tntid").Should().BeTrue();
        wizard.GetAttribute("tntid").Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Multiple_Step_Types_Can_Be_Mixed() {
        // Arrange & Act
        var testModel = new TestModel { Name = "Test" };

        var cut = Render<TnTWizard>(p => p.AddChildContent(builder => {
            // Regular step
            builder.OpenComponent<TnTWizardStep>(0);
            builder.AddComponentParameter(10, "Title", "Step 1");
            builder.AddComponentParameter(20, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Regular step")));
            builder.CloseComponent();

            // Form step
            builder.OpenComponent<TnTWizardFormStep>(30);
            builder.AddComponentParameter(40, "Title", "Step 2");
            builder.AddComponentParameter(50, "Model", testModel);
            builder.AddComponentParameter(60, "ChildContent",
                (RenderFragment<EditContext>)(context => b => b.AddContent(0, "Form step")));
            builder.CloseComponent();
        }));

        // Assert
        var stepIndicators = cut.FindAll("li.tnt-wizard-step-indicator");
        stepIndicators.Should().HaveCount(2);

        var content = cut.Find("div.tnt-wizard-content");
        content.TextContent.Should().Contain("Regular step");
    }

    [Fact]
    public void Multiple_Steps_Render_All_Step_Indicators() {
        // Arrange & Act
        var cut = Render<TnTWizard>(p => p.AddChildContent(builder => {
            builder.OpenComponent<TnTWizardStep>(0);
            builder.AddComponentParameter(10, "Title", "Step 1");
            builder.AddComponentParameter(20, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content 1")));
            builder.CloseComponent();

            builder.OpenComponent<TnTWizardStep>(30);
            builder.AddComponentParameter(40, "Title", "Step 2");
            builder.AddComponentParameter(50, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content 2")));
            builder.CloseComponent();
        }));

        // Assert
        var stepIndicators = cut.FindAll("li.tnt-wizard-step-indicator");
        stepIndicators.Should().HaveCount(2);
    }

    [Fact]
    public void Multiple_Steps_Show_Correct_Current_Step() {
        // Arrange & Act
        var cut = Render<TnTWizard>(p => p.AddChildContent(builder => {
            builder.OpenComponent<TnTWizardStep>(0);
            builder.AddComponentParameter(10, "Title", "Step 1");
            builder.AddComponentParameter(20, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content 1")));
            builder.CloseComponent();

            builder.OpenComponent<TnTWizardStep>(30);
            builder.AddComponentParameter(40, "Title", "Step 2");
            builder.AddComponentParameter(50, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content 2")));
            builder.CloseComponent();
        }));

        // Assert
        var stepIndicators = cut.FindAll("li.tnt-wizard-step-indicator");
        stepIndicators[0].GetAttribute("class")!.Should().Contain("current-step");
        stepIndicators[1].GetAttribute("class")!.Should().NotContain("current-step");
    }

    [Fact]
    public async Task Next_Button_Advances_Step() {
        // Arrange
        var cut = Render<TnTWizard>(p => p.AddChildContent(builder => {
            builder.OpenComponent<TnTWizardStep>(0);
            builder.AddComponentParameter(10, "Title", "Step 1");
            builder.AddComponentParameter(20, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content 1")));
            builder.CloseComponent();

            builder.OpenComponent<TnTWizardStep>(30);
            builder.AddComponentParameter(40, "Title", "Step 2");
            builder.AddComponentParameter(50, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content 2")));
            builder.CloseComponent();
        }));

        var nextButton = cut.Find("button:contains('Next Step')");

        // Act
        await nextButton.ClickAsync(new());

        // Assert
        var stepIndicators = cut.FindAll("li.tnt-wizard-step-indicator");
        stepIndicators[0].GetAttribute("class")!.Should().Contain("completed-step");
        stepIndicators[1].GetAttribute("class")!.Should().Contain("current-step");

        var content = cut.Find("div.tnt-wizard-content");
        content.TextContent.Should().Contain("Content 2");
    }

    [Fact]
    public void NextButtonDisabled_Disables_Next_Button() {
        // Arrange & Act
        var cut = Render<TnTWizard>(p => p
            .Add(c => c.NextButtonDisabled, true)
            .AddChildContent(builder => {
                builder.OpenComponent<TnTWizardStep>(0);
                builder.AddComponentParameter(10, "Title", "Step 1");
                builder.AddComponentParameter(20, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content 1")));
                builder.CloseComponent();

                builder.OpenComponent<TnTWizardStep>(30);
                builder.AddComponentParameter(40, "Title", "Step 2");
                builder.AddComponentParameter(50, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content 2")));
                builder.CloseComponent();
            }));

        // Assert
        var nextButton = cut.Find("button:contains('Next Step')");
        nextButton.HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public void No_Previous_Button_On_First_Step() {
        // Arrange & Act
        var cut = Render<TnTWizard>(p => p.AddChildContent<TnTWizardStep>(step => step
            .Add(s => s.Title, "Step 1")
            .AddChildContent("Step 1 content")));

        // Assert
        var previousButtons = cut.FindAll("button:contains('PreviousStep')");
        previousButtons.Should().BeEmpty();
    }

    [Fact]
    public void No_Step_Shows_Default_Message() {
        // Arrange & Act
        var cut = Render<TnTWizard>(p => p.AddChildContent(""));

        // Assert
        var content = cut.Find("div.tnt-wizard-content");
        content.TextContent.Should().Contain("No step provided");
    }

    [Fact]
    public async Task OnNextButtonClicked_Callback_Invoked() {
        // Arrange
        var nextStepIndex = -1;
        var cut = Render<TnTWizard>(p => p
            .Add(c => c.OnNextButtonClicked, EventCallback.Factory.Create<int>(this, index => nextStepIndex = index))
            .AddChildContent(builder => {
                builder.OpenComponent<TnTWizardStep>(0);
                builder.AddComponentParameter(10, "Title", "Step 1");
                builder.AddComponentParameter(20, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content 1")));
                builder.CloseComponent();

                builder.OpenComponent<TnTWizardStep>(30);
                builder.AddComponentParameter(40, "Title", "Step 2");
                builder.AddComponentParameter(50, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content 2")));
                builder.CloseComponent();
            }));

        var nextButton = cut.Find("button:contains('Next Step')");

        // Act
        await nextButton.ClickAsync(new());

        // Assert
        nextStepIndex.Should().Be(1);
    }

    [Fact]
    public async Task OnPreviousButtonClicked_Callback_Invoked() {
        // Arrange
        var previousStepIndex = -1;
        var cut = Render<TnTWizard>(p => p
            .Add(c => c.OnPreviousButtonClicked, EventCallback.Factory.Create<int>(this, index => previousStepIndex = index))
            .AddChildContent(builder => {
                builder.OpenComponent<TnTWizardStep>(0);
                builder.AddComponentParameter(10, "Title", "Step 1");
                builder.AddComponentParameter(20, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content 1")));
                builder.CloseComponent();

                builder.OpenComponent<TnTWizardStep>(30);
                builder.AddComponentParameter(40, "Title", "Step 2");
                builder.AddComponentParameter(50, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content 2")));
                builder.CloseComponent();
            }));

        // Move to step 2
        var nextButton = cut.Find("button:contains('Next Step')");
        await nextButton.ClickAsync(new());

        // Act - Go back
        var previousButton = cut.Find("button:contains('Previous Step')");
        await previousButton.ClickAsync(new());

        // Assert
        previousStepIndex.Should().Be(1);
    }

    [Fact]
    public async Task Previous_Button_Goes_Back_Step() {
        // Arrange
        var cut = Render<TnTWizard>(p => p.AddChildContent(builder => {
            builder.OpenComponent<TnTWizardStep>(0);
            builder.AddComponentParameter(10, "Title", "Step 1");
            builder.AddComponentParameter(20, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content 1")));
            builder.CloseComponent();

            builder.OpenComponent<TnTWizardStep>(30);
            builder.AddComponentParameter(40, "Title", "Step 2");
            builder.AddComponentParameter(50, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content 2")));
            builder.CloseComponent();
        }));

        // Move to step 2
        var nextButton = cut.Find("button:contains('Next Step')");
        await nextButton.ClickAsync(new());

        // Act - Go back
        var previousButton = cut.Find("button:contains('Previous Step')");
        await previousButton.ClickAsync(new());

        // Assert
        var stepIndicators = cut.FindAll("li.tnt-wizard-step-indicator");
        stepIndicators[0].GetAttribute("class")!.Should().Contain("current-step");
        stepIndicators[1].GetAttribute("class")!.Should().NotContain("current-step");

        var content = cut.Find("div.tnt-wizard-content");
        content.TextContent.Should().Contain("Content 1");
    }

    [Fact]
    public void PreviousButtonDisabled_Disables_Previous_Button() {
        // Arrange
        var cut = Render<TnTWizard>(p => p
            .Add(c => c.PreviousButtonDisabled, true)
            .AddChildContent(builder => {
                builder.OpenComponent<TnTWizardStep>(0);
                builder.AddComponentParameter(10, "Title", "Step 1");
                builder.AddComponentParameter(20, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content 1")));
                builder.CloseComponent();

                builder.OpenComponent<TnTWizardStep>(30);
                builder.AddComponentParameter(40, "Title", "Step 2");
                builder.AddComponentParameter(50, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content 2")));
                builder.CloseComponent();
            }));

        // Move to step 2 first
        var nextButton = cut.Find("button:contains('Next Step')");
        nextButton.Click();

        // Act & Assert
        var previousButton = cut.Find("button:contains('Previous Step')");
        previousButton.HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public void Renders_Buttons_Container() {
        // Arrange & Act
        var cut = Render<TnTWizard>(p => p.AddChildContent("Content"));

        // Assert
        cut.Find("div.tnt-wizard-buttons").Should().NotBeNull();
    }

    [Fact]
    public void Renders_Content_Container() {
        // Arrange & Act
        var cut = Render<TnTWizard>(p => p.AddChildContent("Content"));

        // Assert
        cut.Find("div.tnt-wizard-content").Should().NotBeNull();
    }

    [Fact]
    public void Renders_Next_Button_When_Not_Last_Step() {
        // Arrange & Act
        var cut = Render<TnTWizard>(p => p.AddChildContent(builder => {
            builder.OpenComponent<TnTWizardStep>(0);
            builder.AddComponentParameter(10, "Title", "Step 1");
            builder.AddComponentParameter(20, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content 1")));
            builder.CloseComponent();

            builder.OpenComponent<TnTWizardStep>(30);
            builder.AddComponentParameter(40, "Title", "Step 2");
            builder.AddComponentParameter(50, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content 2")));
            builder.CloseComponent();
        }));

        // Assert
        var nextButton = cut.Find("button:contains('Next Step')");
        nextButton.Should().NotBeNull();
    }

    [Fact]
    public void Renders_Steps_Container() {
        // Arrange & Act
        var cut = Render<TnTWizard>(p => p.AddChildContent("Content"));

        // Assert
        cut.Find("ol.tnt-wizard-steps").Should().NotBeNull();
    }

    [Fact]
    public void Renders_Submit_Button_On_Last_Step() {
        // Arrange
        var cut = Render<TnTWizard>(p => p.AddChildContent<TnTWizardStep>(step => step
            .Add(s => s.Title, "Step 1")
            .AddChildContent("Step 1 content")));

        // Assert
        var submitButton = cut.Find("button:contains('Submit')");
        submitButton.Should().NotBeNull();
    }

    [Fact]
    public void Renders_Title_When_Set() {
        // Arrange & Act
        var cut = Render<TnTWizard>(p => p
            .Add(c => c.Title, "Test Wizard")
            .AddChildContent("Content"));

        // Assert
        var title = cut.Find("h1.tnt-wizard-title");
        title.TextContent.Should().Be("Test Wizard");
    }

    [Fact]
    public void Renders_Wizard_With_Default_Classes() {
        // Arrange & Act
        var cut = Render<TnTWizard>(p => p.AddChildContent("Content"));
        var wizard = cut.Find("div.tnt-wizard");

        // Assert
        var cls = wizard.GetAttribute("class")!;
        cls.Should().Contain("tnt-wizard");
        cls.Should().Contain("tnt-components");
    }

    [Fact]
    public void Single_Step_Shows_Current_Step_Class() {
        // Arrange & Act
        var cut = Render<TnTWizard>(p => p.AddChildContent<TnTWizardStep>(step => step
            .Add(s => s.Title, "Step 1")
            .AddChildContent("Step 1 content")));

        // Assert
        var stepIndicator = cut.Find("li.tnt-wizard-step-indicator");
        stepIndicator.GetAttribute("class")!.Should().Contain("current-step");
    }

    [Fact]
    public void Step_Content_Renders() {
        // Arrange & Act
        var cut = Render<TnTWizard>(p => p.AddChildContent<TnTWizardStep>(step => step
            .Add(s => s.Title, "Step 1")
            .AddChildContent("Test step content")));

        // Assert
        var content = cut.Find("div.tnt-wizard-content");
        content.TextContent.Should().Contain("Test step content");
    }

    [Fact]
    public void Step_Icon_Renders_When_Set() {
        // Arrange & Act
        var cut = Render<TnTWizard>(p => p.AddChildContent<TnTWizardStep>(step => step
            .Add(s => s.Title, "Step 1")
            .Add(s => s.Icon, MaterialIcon.Home)
            .AddChildContent("Step content")));

        // Assert
        cut.Markup.Should().Contain("home");
    }

    [Fact]
    public void Step_Index_Renders_Correctly() {
        // Arrange & Act
        var cut = Render<TnTWizard>(p => p.AddChildContent<TnTWizardStep>(step => step
            .Add(s => s.Title, "Step 1")
            .AddChildContent("Step content")));

        // Assert
        var stepIndex = cut.Find("span.tnt-wizard-step-index");
        stepIndex.TextContent.Should().Be("1");
    }

    [Fact]
    public void Step_SubTitle_Does_Not_Render_When_Empty() {
        // Arrange & Act
        var cut = Render<TnTWizard>(p => p.AddChildContent<TnTWizardStep>(step => step
            .Add(s => s.Title, "Step 1")
            .AddChildContent("Step content")));

        // Assert
        cut.FindAll("div.tnt-wizard-step-subtitle").Should().BeEmpty();
    }

    [Fact]
    public void Step_SubTitle_Renders_When_Set() {
        // Arrange & Act
        var cut = Render<TnTWizard>(p => p.AddChildContent<TnTWizardStep>(step => step
            .Add(s => s.Title, "Step 1")
            .Add(s => s.SubTitle, "Step subtitle")
            .AddChildContent("Step content")));

        // Assert
        var stepSubTitle = cut.Find("div.tnt-wizard-step-subtitle");
        stepSubTitle.TextContent.Should().Be("Step subtitle");
    }

    [Fact]
    public void Step_Title_Renders_Correctly() {
        // Arrange & Act
        var cut = Render<TnTWizard>(p => p.AddChildContent<TnTWizardStep>(step => step
            .Add(s => s.Title, "My Step")
            .AddChildContent("Step content")));

        // Assert
        var stepTitle = cut.Find("div.tnt-wizard-step-title");
        stepTitle.TextContent.Should().Contain("My Step");
    }

    [Fact]
    public async Task Submit_Button_Invokes_OnSubmitCallback() {
        // Arrange
        var submitInvoked = false;
        var cut = Render<TnTWizard>(p => p
            .Add(c => c.OnSubmitCallback, EventCallback.Factory.Create(this, () => submitInvoked = true))
            .AddChildContent<TnTWizardStep>(step => step
                .Add(s => s.Title, "Step 1")
                .AddChildContent("Step content")));

        var submitButton = cut.Find("button:contains('Submit')");

        // Act
        await submitButton.ClickAsync(new());

        // Assert
        submitInvoked.Should().BeTrue();
    }

    [Fact]
    public void SubmitButtonDisabled_Disables_Submit_Button() {
        // Arrange & Act
        var cut = Render<TnTWizard>(p => p
            .Add(c => c.SubmitButtonDisabled, true)
            .AddChildContent<TnTWizardStep>(step => step
                .Add(s => s.Title, "Step 1")
                .AddChildContent("Step content")));

        // Assert
        var submitButton = cut.Find("button:contains('Submit')");
        submitButton.HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public void Title_Does_Not_Render_When_Empty() {
        // Arrange & Act
        var cut = Render<TnTWizard>(p => p.AddChildContent("Content"));

        // Assert
        cut.FindAll("h1.tnt-wizard-title").Should().BeEmpty();
    }

    [Fact]
    public void Title_Does_Not_Render_When_Whitespace() {
        // Arrange & Act
        var cut = Render<TnTWizard>(p => p
            .Add(c => c.Title, "   ")
            .AddChildContent("Content"));

        // Assert
        cut.FindAll("h1.tnt-wizard-title").Should().BeEmpty();
    }

    [Fact]
    public async Task Enter_Key_Advances_To_Next_Step_When_Not_Last_Step() {
        // Arrange
        var cut = Render<TnTWizard>(p => p.AddChildContent(builder => {
        builder.OpenComponent<TnTWizardStep>(0);
    builder.AddComponentParameter(10, "Title", "Step 1");
   builder.AddComponentParameter(20, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content 1")));
        builder.CloseComponent();

            builder.OpenComponent<TnTWizardStep>(30);
       builder.AddComponentParameter(40, "Title", "Step 2");
         builder.AddComponentParameter(50, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content 2")));
            builder.CloseComponent();
        }));

        var contentDiv = cut.Find("div.tnt-wizard-content");

        // Act
        await contentDiv.KeyPressAsync(new KeyboardEventArgs { Key = "Enter" });

        // Assert
        var stepIndicators = cut.FindAll("li.tnt-wizard-step-indicator");
        stepIndicators[0].GetAttribute("class")!.Should().Contain("completed-step");
   stepIndicators[1].GetAttribute("class")!.Should().Contain("current-step");

 var content = cut.Find("div.tnt-wizard-content");
        content.TextContent.Should().Contain("Content 2");
    }

    [Fact]
    public async Task Enter_Key_Submits_Form_When_On_Last_Step() {
        // Arrange
        var submitInvoked = false;
        var cut = Render<TnTWizard>(p => p
   .Add(c => c.OnSubmitCallback, EventCallback.Factory.Create(this, () => submitInvoked = true))
  .AddChildContent<TnTWizardStep>(step => step
            .Add(s => s.Title, "Step 1")
      .AddChildContent("Step content")));

        var contentDiv = cut.Find("div.tnt-wizard-content");

        // Act
        await contentDiv.KeyPressAsync(new KeyboardEventArgs { Key = "Enter" });

 // Assert
        submitInvoked.Should().BeTrue();
    }

    [Fact]
    public async Task Enter_Key_Triggers_OnNextButtonClicked_Callback() {
    // Arrange
     var nextStepIndex = -1;
        var cut = Render<TnTWizard>(p => p
            .Add(c => c.OnNextButtonClicked, EventCallback.Factory.Create<int>(this, index => nextStepIndex = index))
   .AddChildContent(builder => {
 builder.OpenComponent<TnTWizardStep>(0);
 builder.AddComponentParameter(10, "Title", "Step 1");
                builder.AddComponentParameter(20, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content 1")));
    builder.CloseComponent();

                builder.OpenComponent<TnTWizardStep>(30);
         builder.AddComponentParameter(40, "Title", "Step 2");
    builder.AddComponentParameter(50, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content 2")));
                builder.CloseComponent();
          }));

        var contentDiv = cut.Find("div.tnt-wizard-content");

        // Act
        await contentDiv.KeyPressAsync(new KeyboardEventArgs { Key = "Enter" });

        // Assert
        nextStepIndex.Should().Be(1);
    }

    [Fact]
    public async Task Enter_Key_Does_Not_Advance_When_NextButtonDisabled() {
        // Arrange
    var cut = Render<TnTWizard>(p => p
  .Add(c => c.NextButtonDisabled, true)
            .AddChildContent(builder => {
            builder.OpenComponent<TnTWizardStep>(0);
         builder.AddComponentParameter(10, "Title", "Step 1");
        builder.AddComponentParameter(20, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content 1")));
         builder.CloseComponent();

            builder.OpenComponent<TnTWizardStep>(30);
  builder.AddComponentParameter(40, "Title", "Step 2");
      builder.AddComponentParameter(50, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content 2")));
       builder.CloseComponent();
        }));

        var contentDiv = cut.Find("div.tnt-wizard-content");

   // Act
        await contentDiv.KeyPressAsync(new KeyboardEventArgs { Key = "Enter" });

        // Assert
        var stepIndicators = cut.FindAll("li.tnt-wizard-step-indicator");
        stepIndicators[0].GetAttribute("class")!.Should().Contain("current-step");
        stepIndicators[1].GetAttribute("class")!.Should().NotContain("current-step");
    }

    [Fact]
    public async Task Enter_Key_Does_Not_Submit_When_SubmitButtonDisabled() {
      // Arrange
var submitInvoked = false;
        var cut = Render<TnTWizard>(p => p
            .Add(c => c.SubmitButtonDisabled, true)
            .Add(c => c.OnSubmitCallback, EventCallback.Factory.Create(this, () => submitInvoked = true))
   .AddChildContent<TnTWizardStep>(step => step
    .Add(s => s.Title, "Step 1")
.AddChildContent("Step content")));

        var contentDiv = cut.Find("div.tnt-wizard-content");

        // Act
     await contentDiv.KeyPressAsync(new KeyboardEventArgs { Key = "Enter" });

        // Assert
        submitInvoked.Should().BeFalse();
    }

    [Fact]
 public async Task Non_Enter_Key_Does_Not_Advance_Step() {
        // Arrange
    var cut = Render<TnTWizard>(p => p.AddChildContent(builder => {
    builder.OpenComponent<TnTWizardStep>(0);
    builder.AddComponentParameter(10, "Title", "Step 1");
            builder.AddComponentParameter(20, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content 1")));
            builder.CloseComponent();

            builder.OpenComponent<TnTWizardStep>(30);
    builder.AddComponentParameter(40, "Title", "Step 2");
       builder.AddComponentParameter(50, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content 2")));
  builder.CloseComponent();
        }));

      var contentDiv = cut.Find("div.tnt-wizard-content");

        // Act
        await contentDiv.KeyPressAsync(new KeyboardEventArgs { Key = "Space" });

        // Assert
        var stepIndicators = cut.FindAll("li.tnt-wizard-step-indicator");
        stepIndicators[0].GetAttribute("class")!.Should().Contain("current-step");
        stepIndicators[1].GetAttribute("class")!.Should().NotContain("current-step");
    }

    [Fact]
    public async Task Enter_Key_With_Multiple_Steps_Navigates_Correctly() {
      // Arrange
        var cut = Render<TnTWizard>(p => p.AddChildContent(builder => {
   builder.OpenComponent<TnTWizardStep>(0);
            builder.AddComponentParameter(10, "Title", "Step 1");
            builder.AddComponentParameter(20, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content 1")));
            builder.CloseComponent();

    builder.OpenComponent<TnTWizardStep>(30);
     builder.AddComponentParameter(40, "Title", "Step 2");
        builder.AddComponentParameter(50, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content 2")));
     builder.CloseComponent();

            builder.OpenComponent<TnTWizardStep>(60);
builder.AddComponentParameter(70, "Title", "Step 3");
        builder.AddComponentParameter(80, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content 3")));
            builder.CloseComponent();
        }));

        var contentDiv = cut.Find("div.tnt-wizard-content");

        // Act - Press Enter to go to step 2
        await contentDiv.KeyPressAsync(new KeyboardEventArgs { Key = "Enter" });

        // Assert step 2 is current
    var stepIndicators = cut.FindAll("li.tnt-wizard-step-indicator");
        stepIndicators[1].GetAttribute("class")!.Should().Contain("current-step");

    // Act - Press Enter again to go to step 3
        await contentDiv.KeyPressAsync(new KeyboardEventArgs { Key = "Enter" });

        // Assert step 3 is current
 stepIndicators = cut.FindAll("li.tnt-wizard-step-indicator");
        stepIndicators[2].GetAttribute("class")!.Should().Contain("current-step");
    }

    private class TestModel {

        [Required]
     public string Name { get; set; } = "";
    }
}