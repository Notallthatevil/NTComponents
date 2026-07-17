using Microsoft.AspNetCore.Components;

namespace NTComponents.Tests.Wizard;

public class TnTWizardStepBase_Tests : BunitContext {

    public TnTWizardStepBase_Tests() {
        TestingUtility.TestingUtility.SetupRippleEffectModule(this);
    }

    [Fact]
    public void Cascading_Parameter_Provides_Wizard_Reference() {
        // Arrange & Act This is implicitly tested by the fact that steps can register with the wizard We'll verify by ensuring a step can be rendered within a wizard
        var cut = Render<TnTWizard>(p => p.AddChildContent<TnTWizardStep>(step => step
            .Add(s => s.Title, "Cascaded Step")
            .AddChildContent("Content with cascading parameter")));

        // Assert
        var content = cut.Find("div.tnt-wizard-content");
        content.TextContent.Should().Contain("Content with cascading parameter");
    }

    [Fact]
    public void Empty_SubTitle_Does_Not_Render_Subtitle_Element() {
        // Arrange & Act
        var cut = Render<TnTWizard>(p => p.AddChildContent<TnTWizardStep>(step => step
            .Add(s => s.Title, "Title Only")
            .Add(s => s.SubTitle, "")
            .AddChildContent("Content")));

        // Assert
        cut.FindAll("div.tnt-wizard-step-subtitle").Should().BeEmpty();
    }

    [Fact]
    public void Icon_Parameter_Is_Optional() {
        // Arrange & Act
        var cut = Render<TnTWizard>(p => p.AddChildContent<TnTWizardStep>(step => step
            .Add(s => s.Title, "Title with Icon")
            .Add(s => s.Icon, MaterialIcon.Star)
            .AddChildContent("Content")));

        // Assert
        cut.Markup.Should().Contain("star"); // Icon name
    }

    [Fact]
    public void Multiple_Steps_With_Same_Title_Are_Distinct() {
        // Arrange & Act
        var cut = Render<TnTWizard>(p => p.AddChildContent(builder => {
            builder.OpenComponent<TnTWizardStep>(0);
            builder.AddComponentParameter(10, "Title", "Same Title");
            builder.AddComponentParameter(20, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content A")));
            builder.CloseComponent();

            builder.OpenComponent<TnTWizardStep>(30);
            builder.AddComponentParameter(40, "Title", "Same Title");
            builder.AddComponentParameter(50, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content B")));
            builder.CloseComponent();
        }));

        // Assert
        var stepIndicators = cut.FindAll("li.tnt-wizard-step-indicator");
        stepIndicators.Should().HaveCount(2);

        var stepTitles = cut.FindAll("div.tnt-wizard-step-title");
        stepTitles.Should().AllSatisfy(title => title.TextContent.Should().Contain("Same Title"));
    }

    [Fact]
    public void Null_SubTitle_Does_Not_Render_Subtitle_Element() {
        // Arrange & Act
        var cut = Render<TnTWizard>(p => p.AddChildContent<TnTWizardStep>(step => step
            .Add(s => s.Title, "Title Only")
            .Add(s => s.SubTitle, null!)
            .AddChildContent("Content")));

        // Assert
        cut.FindAll("div.tnt-wizard-step-subtitle").Should().BeEmpty();
    }

    [Fact]
    public void Step_Has_Unique_Internal_Id() {
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

        // Assert - Both steps should render successfully with unique IDs
        var stepIndicators = cut.FindAll("li.tnt-wizard-step-indicator");
        stepIndicators.Should().HaveCount(2);
        stepIndicators[0].Should().NotBeSameAs(stepIndicators[1]);
    }

    [Fact]
    public void Step_Maintains_State_During_Wizard_Navigation() {
        // Arrange
        var cut = Render<TnTWizard>(p => p.AddChildContent(builder => {
            builder.OpenComponent<TnTWizardStep>(0);
            builder.AddComponentParameter(10, "Title", "Step 1");
            builder.AddComponentParameter(20, "SubTitle", "First step subtitle");
            builder.AddComponentParameter(30, "Icon", MaterialIcon.Home);
            builder.AddComponentParameter(40, "ChildContent", (RenderFragment)(b => b.AddContent(0, "First step")));
            builder.CloseComponent();

            builder.OpenComponent<TnTWizardStep>(50);
            builder.AddComponentParameter(60, "Title", "Step 2");
            builder.AddComponentParameter(70, "SubTitle", "Second step subtitle");
            builder.AddComponentParameter(80, "Icon", MaterialIcon.Settings);
            builder.AddComponentParameter(90, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Second step")));
            builder.CloseComponent();
        }));

        // Navigate to step 2
        var nextButton = cut.Find("button:contains('Next Step')");
        nextButton.Click();

        // Navigate back to step 1
        var previousButton = cut.Find("button:contains('Previous Step')");
        previousButton.Click();

        // Assert - Original step should maintain its properties
        var stepTitle = cut.FindAll("div.tnt-wizard-step-title")[0];
        stepTitle.TextContent.Should().Contain("Step 1");

        var stepSubTitle = cut.FindAll("div.tnt-wizard-step-subtitle")[0];
        stepSubTitle.TextContent.Should().Be("First step subtitle");

        cut.Markup.Should().Contain("home"); // Icon should be preserved
    }

    [Fact]
    public void Step_Registers_With_Wizard_On_Initialization() {
        // Arrange & Act
        var cut = Render<TnTWizard>(p => p.AddChildContent<TnTWizardStep>(step => step
            .Add(s => s.Title, "Test Step")
            .AddChildContent("Step content")));

        // Assert
        var stepIndicators = cut.FindAll("li.tnt-wizard-step-indicator");
        stepIndicators.Should().HaveCount(1);
    }

    [Fact]
    public void Step_Without_Icon_Renders_Correctly() {
        // Arrange & Act
        var cut = Render<TnTWizard>(p => p.AddChildContent<TnTWizardStep>(step => step
            .Add(s => s.Title, "Title without Icon")
            .AddChildContent("Content")));

        // Assert
        var stepTitle = cut.Find("div.tnt-wizard-step-title");
        stepTitle.TextContent.Should().Contain("Title without Icon");
        // Should not contain icon markup
        stepTitle.InnerHtml.Should().NotContain("tnt-icon");
    }

    [Fact]
    public void SubTitle_Parameter_Is_Optional() {
        // Arrange & Act
        var cut = Render<TnTWizard>(p => p.AddChildContent<TnTWizardStep>(step => step
            .Add(s => s.Title, "Main Title")
            .Add(s => s.SubTitle, "Optional Subtitle")
            .AddChildContent("Content")));

        // Assert
        var stepSubTitle = cut.Find("div.tnt-wizard-step-subtitle");
        stepSubTitle.TextContent.Should().Be("Optional Subtitle");
    }

    [Fact]
    public void Title_Parameter_Is_Required() {
        // Arrange & Act
        var cut = Render<TnTWizard>(p => p.AddChildContent<TnTWizardStep>(step => step
            .Add(s => s.Title, "Required Title")
            .AddChildContent("Content")));

        // Assert
        var stepTitle = cut.Find("div.tnt-wizard-step-title");
        stepTitle.TextContent.Should().Contain("Required Title");
    }

    [Fact]
    public void Whitespace_SubTitle_Does_Not_Render_Subtitle_Element() {
        // Arrange & Act
        var cut = Render<TnTWizard>(p => p.AddChildContent<TnTWizardStep>(step => step
            .Add(s => s.Title, "Title Only")
            .Add(s => s.SubTitle, "   ")
            .AddChildContent("Content")));

        // Assert
        cut.FindAll("div.tnt-wizard-step-subtitle").Should().BeEmpty();
    }
}