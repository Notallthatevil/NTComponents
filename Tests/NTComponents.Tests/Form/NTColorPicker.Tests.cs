using Microsoft.AspNetCore.Components;

namespace NTComponents.Tests.Form;

public class NTColorPicker_Tests : BunitContext {

    public NTColorPicker_Tests() => JSInterop.Mode = JSRuntimeMode.Loose;

    [Fact]
    public void Renders_Area_Hue_Format_And_Value_Controls() {
        var cut = RenderPicker();

        var area = cut.Find("[data-nt-color-area]");
        area.GetAttribute("role").Should().Be("slider");
        area.GetAttribute("aria-label").Should().Be("Saturation and brightness");
        area.GetAttribute("tabindex").Should().Be("0");
        var hue = cut.Find("input[data-nt-color-hue]");
        hue.GetAttribute("type").Should().Be("range");
        hue.GetAttribute("min").Should().Be("0");
        hue.GetAttribute("max").Should().Be("360");
        hue.GetAttribute("aria-label").Should().Be("Hue");
        cut.FindAll("[data-nt-color-format]").Select(button => button.TextContent.Trim()).Should().Equal("Hex", "RGB", "HSL");
        cut.FindAll("[data-nt-color-format]").Select(button => button.GetAttribute("aria-pressed")).Should().Equal("true", "false", "false");
        cut.FindAll("[data-nt-color-field]").Select(field => field.GetAttribute("data-nt-color-field")).Should().Equal("hex", "r", "g", "b", "h", "s", "l");
        cut.Find(".nt-color-preview").GetAttribute("aria-hidden").Should().Be("true");
    }

    [Fact]
    public void Renders_Group_With_Default_And_Custom_Label() {
        RenderPicker().Find("[data-nt-color-picker]").GetAttribute("aria-label").Should().Be("Color picker");

        var cut = RenderPicker(parameters => parameters.Add(p => p.AriaLabel, "Brand color"));

        cut.Find("[data-nt-color-picker]").GetAttribute("aria-label").Should().Be("Brand color");
        cut.Find("[data-nt-color-picker]").GetAttribute("role").Should().Be("group");
    }

    [Fact]
    public void Opacity_Slider_And_Alpha_Field_Render_Only_With_ShowAlpha() {
        var without = RenderPicker();
        without.FindAll("[data-nt-color-alpha]").Should().BeEmpty();
        without.FindAll("[data-nt-color-field=a]").Should().BeEmpty();

        var with = RenderPicker(parameters => parameters.Add(p => p.ShowAlpha, true));
        var alpha = with.Find("input[data-nt-color-alpha]");
        alpha.GetAttribute("type").Should().Be("range");
        alpha.GetAttribute("max").Should().Be("100");
        alpha.GetAttribute("aria-label").Should().Be("Opacity");
        with.FindAll("[data-nt-color-field=a]").Should().HaveCount(1);
    }

    [Fact]
    public void Reflects_Value_In_The_Data_Attribute_And_Hidden_Input() {
        var cut = RenderPicker(parameters => parameters.Add(p => p.Value, "#6750a4").Add(p => p.Name, "Brand"));

        cut.Find("[data-nt-color-picker]").GetAttribute("data-value").Should().Be("#6750a4");
        var hidden = cut.Find("input[data-nt-color-value]");
        hidden.GetAttribute("type").Should().Be("hidden");
        hidden.GetAttribute("name").Should().Be("Brand");
        hidden.GetAttribute("value").Should().Be("#6750a4");
    }

    [Fact]
    public void Updating_Value_Updates_The_Rendered_Data_Attribute() {
        var cut = RenderPicker(parameters => parameters.Add(p => p.Value, "#6750a4"));

        cut.Render(parameters => parameters.Add(p => p.Value, "#ff0000"));

        cut.Find("[data-nt-color-picker]").GetAttribute("data-value").Should().Be("#ff0000");
        cut.Find("input[data-nt-color-value]").GetAttribute("value").Should().Be("#ff0000");
    }

    [Fact]
    public void Hidden_Input_Change_Fires_ValueChanged() {
        string? received = null;
        var cut = RenderPicker(parameters => parameters.Add(p => p.ValueChanged, EventCallback.Factory.Create<string?>(this, value => received = value)));

        cut.Find("input[data-nt-color-value]").Change("#12ab34");

        received.Should().Be("#12ab34");
        cut.Instance.Value.Should().Be("#12ab34");
    }

    [Fact]
    public void Disabled_Disables_Every_Control() {
        var cut = RenderPicker(parameters => parameters.Add(p => p.Disabled, true).Add(p => p.ShowAlpha, true));

        cut.Find("[data-nt-color-picker]").GetAttribute("data-disabled").Should().Be("true");
        cut.Find("[data-nt-color-picker]").GetAttribute("class").Should().Contain("nt-color-picker-disabled");
        cut.Find("[data-nt-color-area]").GetAttribute("tabindex").Should().Be("-1");
        cut.Find("[data-nt-color-hue]").HasAttribute("disabled").Should().BeTrue();
        cut.Find("[data-nt-color-alpha]").HasAttribute("disabled").Should().BeTrue();
        cut.FindAll("[data-nt-color-format]").Should().OnlyContain(button => button.HasAttribute("disabled"));
        cut.FindAll("[data-nt-color-field]").Should().OnlyContain(field => field.HasAttribute("disabled"));
    }

    [Fact]
    public void Passes_Through_Element_Attributes() {
        var cut = RenderPicker(parameters => parameters
            .Add(p => p.ElementId, "brand-picker")
            .AddUnmatched("data-testid", "picker")
            .AddUnmatched("class", "extra"));

        var root = cut.Find("[data-nt-color-picker]");
        root.Id.Should().Be("brand-picker");
        root.GetAttribute("data-testid").Should().Be("picker");
        root.GetAttribute("class").Should().Contain("nt-color-picker").And.Contain("extra");
    }

    private IRenderedComponent<NTColorPicker> RenderPicker(Action<ComponentParameterCollectionBuilder<NTColorPicker>>? configure = null) {
        return Render<NTColorPicker>(parameters => configure?.Invoke(parameters));
    }
}
