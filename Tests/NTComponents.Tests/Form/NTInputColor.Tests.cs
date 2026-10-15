using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace NTComponents.Tests.Form;

public class NTInputColor_Tests : BunitContext {

    private const string FieldSelector = "input[data-nt-input-color-input]";

    private sealed class TestModel {
        public string? AccentColor { get; set; }
    }

    [Fact]
    public void Renders_Text_Input_With_Color_Field_Attributes() {
        var cut = RenderInput(new TestModel { AccentColor = "#6750a4" });

        var input = cut.Find(FieldSelector);

        input.GetAttribute("type").Should().Be("text");
        input.GetAttribute("maxlength").Should().Be("9");
        input.GetAttribute("spellcheck").Should().Be("false");
        input.GetAttribute("autocomplete").Should().Be("off");
        input.GetAttribute("value").Should().Be("#6750a4");
    }

    [Fact]
    public void Renders_Swatch_Trigger_And_Picker() {
        var cut = RenderInput(new TestModel { AccentColor = "#6750a4" });

        cut.Find("[data-nt-input-color-swatch]").GetAttribute("style").Should().Contain("--nt-input-color-swatch: #6750a4");
        cut.Find("[data-nt-input-color-swatch]").Closest(".nt-input-leading")!.GetAttribute("aria-hidden").Should().Be("true");
        var trigger = cut.Find("button[data-nt-input-color-trigger]");
        trigger.GetAttribute("aria-label").Should().Be("Open color picker");
        trigger.HasAttribute("disabled").Should().BeFalse();
        var popover = cut.Find("[data-nt-input-color-popover]");
        popover.GetAttribute("popover").Should().Be("auto");
        trigger.GetAttribute("aria-controls").Should().Be(popover.Id);
        popover.QuerySelector("[data-nt-color-picker]")!.GetAttribute("data-value").Should().Be("#6750a4");
        popover.QuerySelector("[data-nt-color-picker]")!.GetAttribute("data-nt-color-embedded").Should().Be("true");
        popover.QuerySelectorAll("[data-nt-input-color-action]").Select(button => button.QuerySelector(".nt-button-label")!.TextContent.Trim()).Should().Equal("Cancel", "OK");
    }

    [Fact]
    public void Empty_Value_Renders_Neutral_Swatch() {
        var cut = RenderInput();

        cut.Find("[data-nt-input-color-swatch]").HasAttribute("style").Should().BeFalse();
        cut.Find("[data-nt-color-picker]").GetAttribute("data-value").Should().BeEmpty();
    }

    [Fact]
    public void ShowAlpha_Is_Passed_To_The_Picker() {
        var cut = RenderInput(configure: parameters => parameters.Add(p => p.ShowAlpha, true));

        cut.Find("[data-nt-input-color]").GetAttribute("data-show-alpha").Should().Be("true");
        cut.Find("[data-nt-color-picker]").GetAttribute("data-show-alpha").Should().Be("true");
        cut.FindAll("[data-nt-color-alpha]").Should().HaveCount(1);
    }

    [Fact]
    public void Without_ShowAlpha_The_Picker_Has_No_Opacity_Slider() {
        var cut = RenderInput();

        cut.Find("[data-nt-color-picker]").GetAttribute("data-show-alpha").Should().Be("false");
        cut.FindAll("[data-nt-color-alpha]").Should().BeEmpty();
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void Disabled_Or_ReadOnly_Disables_The_Trigger_And_Picker(bool disabled, bool readOnly) {
        var cut = RenderInput(configure: parameters => parameters.Add(p => p.Disabled, disabled).Add(p => p.ReadOnly, readOnly));

        cut.Find("button[data-nt-input-color-trigger]").HasAttribute("disabled").Should().BeTrue();
        cut.Find("[data-nt-color-picker]").GetAttribute("data-disabled").Should().Be("true");
    }

    [Theory]
    [InlineData("#6750a4", "#6750a4")]
    [InlineData("#abc", "#aabbcc")]
    [InlineData("#ABCDEF", "#abcdef")]
    [InlineData("ABCDEF", "#abcdef")]
    [InlineData("abc", "#aabbcc")]
    [InlineData("  #6750A4  ", "#6750a4")]
    public void Change_Parses_And_Normalizes_Valid_Values(string typed, string expected) {
        var model = new TestModel();
        var cut = RenderInput(model);

        cut.Find(FieldSelector).Change(typed);

        model.AccentColor.Should().Be(expected);
    }

    [Fact]
    public void Change_Drops_Alpha_Unless_ShowAlpha_Is_Enabled() {
        var model = new TestModel();
        var cut = RenderInput(model);

        cut.Find(FieldSelector).Change("#11223380");

        model.AccentColor.Should().Be("#112233");
    }

    [Theory]
    [InlineData("#11223380", "#11223380")]
    [InlineData("#112233ff", "#112233")]
    [InlineData("#11223388", "#11223388")]
    public void Change_Keeps_Alpha_Below_Full_When_ShowAlpha_Is_Enabled(string typed, string expected) {
        var model = new TestModel();
        var cut = RenderInput(model, parameters => parameters.Add(p => p.ShowAlpha, true));

        cut.Find(FieldSelector).Change(typed);

        model.AccentColor.Should().Be(expected);
    }

    [Fact]
    public void Change_To_Empty_Sets_Null() {
        var model = new TestModel { AccentColor = "#6750a4" };
        var cut = RenderInput(model);

        cut.Find(FieldSelector).Change(string.Empty);

        model.AccentColor.Should().BeNull();
    }

    [Theory]
    [InlineData("red")]
    [InlineData("#12")]
    [InlineData("#12345")]
    [InlineData("#1234567")]
    [InlineData("#gggggg")]
    public void Change_Rejects_Invalid_Values_With_A_Message(string typed) {
        var model = new TestModel { AccentColor = "#6750a4" };
        var cut = RenderInForm(model);

        cut.Find(FieldSelector).Change(typed);

        model.AccentColor.Should().Be("#6750a4");
        cut.Find(".nt-input-error-text").TextContent.Should().Be("The AccentColor field must be a hex color such as #6750a4.");
    }

    [Fact]
    public void Static_Value_Is_Rendered_As_Given() {
        var cut = RenderInput(new TestModel { AccentColor = "#6750a4" });

        cut.Find(FieldSelector).GetAttribute("value").Should().Be("#6750a4");
    }

    private IRenderedComponent<NTInputColor> RenderInput(TestModel? model = null, Action<ComponentParameterCollectionBuilder<NTInputColor>>? configure = null) {
        model ??= new TestModel();
        return Render<NTInputColor>(parameters => {
            parameters
                .Add(p => p.Value, model.AccentColor)
                .Add(p => p.ValueChanged, EventCallback.Factory.Create<string?>(this, value => model.AccentColor = value))
                .Add(p => p.ValueExpression, () => model.AccentColor);
            configure?.Invoke(parameters);
        });
    }

    private IRenderedComponent<NTForm> RenderInForm(TestModel model) {
        return Render<NTForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.ChildContent, (EditContext _) => builder => {
                builder.OpenComponent<NTInputColor>(0);
                builder.AddAttribute(1, nameof(NTInputColor.Value), model.AccentColor);
                builder.AddAttribute(2, nameof(NTInputColor.ValueChanged), EventCallback.Factory.Create<string?>(this, value => model.AccentColor = value));
                builder.AddAttribute(3, nameof(NTInputColor.ValueExpression), (Expression<Func<string?>>)(() => model.AccentColor));
                builder.CloseComponent();
            }));
    }
}
