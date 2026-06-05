using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace NTComponents.Tests.Form;

public class NTForm_Tests : BunitContext {

    private sealed class ChildComponent : ComponentBase {

        [CascadingParameter]
        public NTForm? Form { get; set; }

        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder) {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "id", "child");
            builder.CloseElement();
        }
    }

    private sealed class TestModel {
        public string? Name { get; set; }
    }

    [Fact]
    public void Defaults_Are_Material_Input_Defaults() {
        var cut = Render<NTForm>(parameters => parameters
            .Add(p => p.Model, new TestModel())
            .Add(p => p.ChildContent, (EditContext _) => builder => { }));

        cut.Instance.Appearance.Should().Be(NTFormAppearance.Outlined);
        cut.Instance.Density.Should().Be(NTFormDensity.Standard);
        cut.Instance.Disabled.Should().BeFalse();
        cut.Instance.ReadOnly.Should().BeFalse();
        cut.Instance.ShowRequiredSupportingText.Should().BeFalse();
        cut.Instance.RequiredSupportingText.Should().Be("Required");
    }

    [Fact]
    public void Cascades_Form_Instance_To_Children() {
        var cut = Render<NTForm>(parameters => parameters
            .Add(p => p.Model, new TestModel())
            .Add(p => p.ChildContent, (EditContext _) => builder => {
                builder.OpenComponent<ChildComponent>(0);
                builder.CloseComponent();
            }));

        cut.FindComponent<ChildComponent>().Instance.Form.Should().Be(cut.Instance);
    }

    [Fact]
    public void Interactive_Renderer_Adds_Novalidate_When_Missing() {
        SetRendererInfo(new RendererInfo("WebAssembly", true));

        var cut = Render<NTForm>(parameters => parameters
            .Add(p => p.Model, new TestModel())
            .Add(p => p.ChildContent, (EditContext _) => builder => { }));

        cut.Find("form").HasAttribute("novalidate").Should().BeTrue();
    }

    [Fact]
    public void Nested_NTForm_Throws() {
        var model = new TestModel();

        var act = () => Render<NTForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.ChildContent, (EditContext _) => builder => {
                builder.OpenComponent<NTForm>(0);
                builder.AddAttribute(1, nameof(NTForm.Model), model);
                builder.AddAttribute(2, nameof(NTForm.ChildContent), (RenderFragment<EditContext>)(_ => childBuilder => { }));
                builder.CloseComponent();
            }));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("NTForm cannot be nested inside another NTForm.");
    }
}
