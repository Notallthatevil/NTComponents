using Microsoft.AspNetCore.Components;

namespace NTComponents.Tests.Form;

public class NTInputColor_Tests : BunitContext {

    private sealed class TestModel {
        public string? AccentColor { get; set; }
    }

    [Fact]
    public void Renders_Color_Input() {
        var cut = RenderInput();

        cut.Find("input").GetAttribute("type").Should().Be("color");
    }

    [Fact]
    public void Change_Updates_Value() {
        var model = new TestModel();
        var cut = RenderInput(model);

        cut.Find("input").Change("#123456");

        model.AccentColor.Should().Be("#123456");
    }

    private IRenderedComponent<NTInputColor> RenderInput(TestModel? model = null) {
        model ??= new TestModel();
        return Render<NTInputColor>(parameters => parameters
            .Add(p => p.Value, model.AccentColor)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<string?>(this, value => model.AccentColor = value))
            .Add(p => p.ValueExpression, () => model.AccentColor));
    }
}
