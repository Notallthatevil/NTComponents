using Microsoft.AspNetCore.Components;

namespace NTComponents.Tests.Form.TnTInputBase;

public class ElementName_Tests : BunitContext {

    public ElementName_Tests() => SetRendererInfo(new RendererInfo("WebAssembly", true));

    [Fact]
    public void NestedMemberBinding_RendersDeterministicFieldPath() {
        var model = new NameModel();

        var cut = RenderInput(() => model.Nested.Value, model.Nested.Value, value => model.Nested.Value = value);

        cut.Find("input").GetAttribute("name").Should().Be("model.Nested.Value");
        cut.Instance.ElementName.Should().Be("model.Nested.Value");
    }

    [Fact]
    public void ArrayIndexBinding_RendersIndexedFieldPath() {
        var model = new NameModel();

        var cut = RenderInput(() => model.Items[1].Value, model.Items[1].Value, value => model.Items[1].Value = value);

        cut.Find("input").GetAttribute("name").Should().Be("model.Items[1].Value");
    }

    [Fact]
    public void DictionaryIndexBinding_RendersKeyedFieldPath() {
        var model = new NameModel();

        var cut = RenderInput(() => model.KeyedItems["primary"].Value, model.KeyedItems["primary"].Value, value => model.KeyedItems["primary"].Value = value);

        cut.Find("input").GetAttribute("name").Should().Be("model.KeyedItems[primary].Value");
    }

    private IRenderedComponent<NameInput> RenderInput(System.Linq.Expressions.Expression<Func<string?>> expression, string? value, Action<string?> valueChanged) {
        return Render<NameInput>(parameters => parameters
            .Add(component => component.ValueExpression, expression)
            .Add(component => component.Value, value)
            .Add(component => component.ValueChanged, EventCallback.Factory.Create(this, valueChanged)));
    }

    private sealed class NameModel {
        public NameItem Nested { get; } = new();
        public NameItem[] Items { get; } = [new(), new()];
        public Dictionary<string, NameItem> KeyedItems { get; } = new() { ["primary"] = new() };
    }

    private sealed class NameItem {
        public string? Value { get; set; }
    }

    private sealed class NameInput : global::NTComponents.TnTInputBase<string?> {
        public override InputType Type => InputType.Text;

        protected override bool TryParseValueFromString(string? value, out string? result, out string validationErrorMessage) {
            result = value;
            validationErrorMessage = string.Empty;
            return true;
        }
    }
}
