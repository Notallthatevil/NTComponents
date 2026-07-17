using AngleSharp.Dom;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using System.ComponentModel.DataAnnotations;

namespace NTComponents.Tests.Wizard;

public class TnTWizard_NavigationAndValidation_Tests : BunitContext {

    public TnTWizard_NavigationAndValidation_Tests() {
        TestingUtility.TestingUtility.SetupRippleEffectModule(this);
        SetRendererInfo(new RendererInfo("WebAssembly", true));
    }

    [Fact]
    public async Task Step_Click_Allows_Immediate_Next_And_Fires_Next_Callback() {
        // Arrange
        var nextIndex = -1;
        var cut = RenderWizardWithThreeSteps(p => p
            .Add(w => w.OnNextButtonClicked, EventCallback.Factory.Create<int>(this, i => nextIndex = i)));

        // Act
        await cut.FindAll("li.tnt-wizard-step-indicator")[1].ClickAsync(new MouseEventArgs());

        // Assert
        var indicators = cut.FindAll("li.tnt-wizard-step-indicator");
        indicators[1].GetAttribute("class")!.Should().Contain("current-step");
        nextIndex.Should().Be(1);
    }

    [Fact]
    public async Task Step_Click_Does_Not_Allow_Unvisited_Non_Immediate_Next() {
        // Arrange
        var cut = RenderWizardWithThreeSteps();

        // Act
        await cut.FindAll("li.tnt-wizard-step-indicator")[2].ClickAsync(new MouseEventArgs());

        // Assert
        var indicators = cut.FindAll("li.tnt-wizard-step-indicator");
        indicators[0].GetAttribute("class")!.Should().Contain("current-step");
        indicators[2].GetAttribute("class")!.Should().NotContain("current-step");
    }

    [Fact]
    public async Task Step_Click_Allows_Navigation_To_Visited_Step() {
        // Arrange
        var cut = RenderWizardWithThreeSteps();
        await FindNextButton(cut).ClickAsync(new MouseEventArgs());

        // Act
        await cut.FindAll("li.tnt-wizard-step-indicator")[0].ClickAsync(new MouseEventArgs());

        // Assert
        var indicators = cut.FindAll("li.tnt-wizard-step-indicator");
        indicators[0].GetAttribute("class")!.Should().Contain("current-step");
    }

    [Fact]
    public void DisableButtons_Mode_Starts_Disabled_For_Invalid_Required_Model() {
        // Arrange
        var model = new WizardRequiredModel();
        var cut = RenderWizardWithFormStep(model, TnTWizardInvalidFormButtonBehavior.DisableButtons);

        // Assert
        cut.WaitForAssertion(() => FindNextButton(cut).HasAttribute("disabled").Should().BeTrue());
    }

    [Fact]
    public void GrayOutOnly_Mode_Starts_Gray_Not_Disabled_For_Invalid_Required_Model() {
        // Arrange
        var model = new WizardRequiredModel();
        var cut = RenderWizardWithFormStep(model, TnTWizardInvalidFormButtonBehavior.GrayOutOnly);

        // Assert
        cut.WaitForAssertion(() => {
            var nextButton = FindNextButton(cut);
            nextButton.HasAttribute("disabled").Should().BeFalse();
            nextButton.GetAttribute("class")!.Should().Contain("tnt-disabled");
        });
    }

    [Fact]
    public async Task GrayOutOnly_Mode_Click_Still_Validates_And_Blocks_Advance() {
        // Arrange
        var invalidSubmitCalled = false;
        var model = new WizardRequiredModel();
        var cut = Render<TnTWizard>(p => p
            .Add(w => w.InvalidFormButtonBehavior, TnTWizardInvalidFormButtonBehavior.GrayOutOnly)
            .AddChildContent(builder => {
                builder.OpenComponent<TnTWizardFormStep>(0);
                builder.AddComponentParameter(10, nameof(TnTWizardFormStep.Title), "Form");
                builder.AddComponentParameter(20, nameof(TnTWizardFormStep.Model), model);
                builder.AddComponentParameter(30, nameof(TnTWizardFormStep.OnInvalidSubmitCallback),
                    EventCallback.Factory.Create<object>(this, _ => invalidSubmitCalled = true));
                builder.AddComponentParameter(40, nameof(TnTWizardFormStep.ChildContent),
                    (RenderFragment<EditContext>)(_ => b => b.AddContent(0, "Form content")));
                builder.CloseComponent();

                builder.OpenComponent<TnTWizardStep>(50);
                builder.AddComponentParameter(60, nameof(TnTWizardStep.Title), "Step 2");
                builder.AddComponentParameter(70, nameof(TnTWizardStep.ChildContent), (RenderFragment)(b => b.AddContent(0, "Step 2 content")));
                builder.CloseComponent();
            }));

        // Act
        await FindNextButton(cut).ClickAsync(new MouseEventArgs());

        // Assert
        invalidSubmitCalled.Should().BeTrue();
        cut.FindAll("li.tnt-wizard-step-indicator")[0].GetAttribute("class")!.Should().Contain("current-step");
    }

    [Fact]
    public void DisableButtons_Mode_Reevaluates_On_Field_Change_Without_Global_Validation() {
        // Arrange
        var model = new WizardRequiredModel();
        var cut = RenderWizardWithFormStep(model, TnTWizardInvalidFormButtonBehavior.DisableButtons);
        cut.WaitForAssertion(() => FindNextButton(cut).HasAttribute("disabled").Should().BeTrue());

        var editContext = cut.FindComponent<TnTForm>().Instance.EditContext!;

        // Act
        model.Name = "valid";
        editContext.NotifyFieldChanged(new FieldIdentifier(model, nameof(WizardRequiredModel.Name)));

        // Assert
        cut.WaitForAssertion(() => FindNextButton(cut).HasAttribute("disabled").Should().BeFalse());
    }

    private IRenderedComponent<TnTWizard> RenderWizardWithThreeSteps(Action<ComponentParameterCollectionBuilder<TnTWizard>>? configure = null) {
        return Render<TnTWizard>(p => {
            configure?.Invoke(p);
            p.AddChildContent(builder => {
                builder.OpenComponent<TnTWizardStep>(0);
                builder.AddComponentParameter(10, nameof(TnTWizardStep.Title), "Step 1");
                builder.AddComponentParameter(20, nameof(TnTWizardStep.ChildContent), (RenderFragment)(b => b.AddContent(0, "Step 1 content")));
                builder.CloseComponent();

                builder.OpenComponent<TnTWizardStep>(30);
                builder.AddComponentParameter(40, nameof(TnTWizardStep.Title), "Step 2");
                builder.AddComponentParameter(50, nameof(TnTWizardStep.ChildContent), (RenderFragment)(b => b.AddContent(0, "Step 2 content")));
                builder.CloseComponent();

                builder.OpenComponent<TnTWizardStep>(60);
                builder.AddComponentParameter(70, nameof(TnTWizardStep.Title), "Step 3");
                builder.AddComponentParameter(80, nameof(TnTWizardStep.ChildContent), (RenderFragment)(b => b.AddContent(0, "Step 3 content")));
                builder.CloseComponent();
            });
        });
    }

    private IRenderedComponent<TnTWizard> RenderWizardWithFormStep(WizardRequiredModel model, TnTWizardInvalidFormButtonBehavior behavior) {
        return Render<TnTWizard>(p => p
            .Add(w => w.InvalidFormButtonBehavior, behavior)
            .AddChildContent(builder => {
                builder.OpenComponent<TnTWizardFormStep>(0);
                builder.AddComponentParameter(10, nameof(TnTWizardFormStep.Title), "Form");
                builder.AddComponentParameter(20, nameof(TnTWizardFormStep.Model), model);
                builder.AddComponentParameter(30, nameof(TnTWizardFormStep.ChildContent), (RenderFragment<EditContext>)(_ => b => b.AddContent(0, "Form content")));
                builder.CloseComponent();

                builder.OpenComponent<TnTWizardStep>(40);
                builder.AddComponentParameter(50, nameof(TnTWizardStep.Title), "Step 2");
                builder.AddComponentParameter(60, nameof(TnTWizardStep.ChildContent), (RenderFragment)(b => b.AddContent(0, "Step 2 content")));
                builder.CloseComponent();
            }));
    }

    private static IElement FindNextButton(IRenderedComponent<TnTWizard> cut) =>
        cut.FindAll("button").Single(b => b.TextContent.Contains("Next Step", StringComparison.Ordinal));

    private sealed class WizardRequiredModel {

        [Required]
        public string Name { get; set; } = string.Empty;
    }
}
