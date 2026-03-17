using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using System.Linq.Expressions;
using NTComponents.Core;

namespace NTComponents.Tests.Form;

public class NTInputSelect_Tests : BunitContext {

    private static readonly TestOption[] _options =
    [
        new(TestSelectEnum.Alpha, "Alpha"),
        new(TestSelectEnum.Beta, "Alphabet"),
        new(TestSelectEnum.Gamma, "Graphite"),
        new(TestSelectEnum.Delta, "Phantom")
    ];

    public NTInputSelect_Tests() {
        SetRendererInfo(new RendererInfo("WebAssembly", true));

        var module = JSInterop.SetupModule("./_content/NTComponents/Form/NTInputSelect.razor.js");
        module.SetupVoid("onLoad", _ => true).SetVoidResult();
        module.SetupVoid("onUpdate", _ => true).SetVoidResult();
        module.SetupVoid("onDispose", _ => true).SetVoidResult();
    }

    [Fact]
    public void Typing_Filters_By_Contains_And_Prioritizes_StartsWith() {
        var cut = RenderInputSelect();
        var input = cut.Find(".tnt-input-select-search-input");

        input.Input("ph");

        var options = cut.FindAll(".tnt-input-select-search-list-item");
        options.Should().HaveCount(4);
        options[0].TextContent.Should().Contain("Phantom");
        options[1].TextContent.Should().Contain("Alpha");
        options[2].TextContent.Should().Contain("Alphabet");
        options[3].TextContent.Should().Contain("Graphite");
    }

    [Fact]
    public void Selecting_Option_Updates_Bound_Value_And_Hidden_Input() {
        var model = new TestModel();
        var cut = RenderInputSelect(model);
        var input = cut.Find(".tnt-input-select-search-input");

        input.Input("gra");
        cut.Find(".tnt-input-select-search-list-item").Click();

        model.Value.Should().Be(TestSelectEnum.Gamma);
        cut.Find("input[type='hidden']").GetAttribute("value").Should().Be(TestSelectEnum.Gamma.ToString());
        cut.Find(".tnt-input-select-search-input").GetAttribute("value").Should().Be("Graphite");
    }

    [Fact]
    public void Selecting_Option_Persists_After_A_Follow_Up_Render() {
        var model = new TestModel();
        var cut = RenderInputSelect(model);
        var input = cut.Find(".tnt-input-select-search-input");

        input.Input("gra");
        cut.Find(".tnt-input-select-search-list-item").Click();
        cut.Render();

        model.Value.Should().Be(TestSelectEnum.Gamma);
        cut.Find("input[type='hidden']").GetAttribute("value").Should().Be(TestSelectEnum.Gamma.ToString());
        cut.Find(".tnt-input-select-search-input").GetAttribute("value").Should().Be("Graphite");
    }

    [Fact]
    public void Reopening_After_A_Selection_Shows_All_Options() {
        var model = new TestModel();
        var cut = RenderInputSelect(model);
        var input = cut.Find(".tnt-input-select-search-input");

        input.Input("gra");
        cut.Find(".tnt-input-select-search-list-item").Click();
        input.Blur();
        input.Focus();

        cut.FindAll(".tnt-input-select-search-list-item").Should().HaveCount(_options.Length);
    }

    [Fact]
    public void Allow_Freeform_Persists_A_Typed_Value() {
        var model = new StringTestModel();
        var cut = RenderStringInputSelect(model, parameters => parameters
            .Add(p => p.AllowFreeform, true));
        var input = cut.Find(".tnt-input-select-search-input");

        input.Input("Custom value");
        cut.Render();

        model.Value.Should().Be("Custom value");
        cut.Find("input[type='hidden']").GetAttribute("value").Should().Be("Custom value");
        cut.Find(".tnt-input-select-search-input").GetAttribute("value").Should().Be("Custom value");
    }

    [Fact]
    public void Typing_After_Selection_Clears_Selection() {
        var model = new TestModel { Value = TestSelectEnum.Gamma };
        var cut = RenderInputSelect(model);
        var input = cut.Find(".tnt-input-select-search-input");

        input.Input("other");

        model.Value.Should().BeNull();
        cut.Find("input[type='hidden']").GetAttribute("value").Should().BeEmpty();
    }

    [Fact]
    public async Task Arrow_Keys_And_Enter_Select_Focused_Item() {
        var model = new TestModel();
        var cut = RenderInputSelect(model);
        var input = cut.Find(".tnt-input-select-search-input");

        await input.KeyUpAsync(new KeyboardEventArgs { Key = "ArrowDown" });
        await input.KeyUpAsync(new KeyboardEventArgs { Key = "Enter" });

        model.Value.Should().Be(TestSelectEnum.Alpha);
    }

    [Fact]
    public void Initial_Render_Preserves_Selected_Hidden_Value() {
        var model = new TestModel { Value = TestSelectEnum.Gamma };
        var cut = RenderInputSelect(model);

        cut.Find("input[type='hidden']").GetAttribute("value").Should().Be(TestSelectEnum.Gamma.ToString());
    }

    [Fact]
    public void Removing_Selected_Option_Clears_Value_And_Hidden_Input() {
        var model = new TestModel { Value = TestSelectEnum.Alpha };
        var includeAlpha = true;

        RenderFragment options = builder => {
            if (includeAlpha) {
                builder.OpenComponent<NTInputSelectOption<TestSelectEnum?>>(0);
                builder.SetKey(TestSelectEnum.Alpha);
                builder.AddAttribute(1, nameof(NTInputSelectOption<TestSelectEnum?>.Value), TestSelectEnum.Alpha);
                builder.AddAttribute(2, nameof(NTInputSelectOption<TestSelectEnum?>.Label), "Alpha");
                builder.CloseComponent();
            }

            builder.OpenComponent<NTInputSelectOption<TestSelectEnum?>>(3);
            builder.SetKey(TestSelectEnum.Beta);
            builder.AddAttribute(4, nameof(NTInputSelectOption<TestSelectEnum?>.Value), TestSelectEnum.Beta);
            builder.AddAttribute(5, nameof(NTInputSelectOption<TestSelectEnum?>.Label), "Alphabet");
            builder.CloseComponent();
        };

        var cut = Render<NTInputSelect<TestSelectEnum?>>(parameters => parameters
            .Add(p => p.ValueExpression, () => model.Value)
            .Add(p => p.Value, model.Value)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<TestSelectEnum?>(this, v => model.Value = v))
            .Add(p => p.ChildContent, options));

        includeAlpha = false;
        cut.Render();

        model.Value.Should().BeNull();
        cut.Find("input[type='hidden']").GetAttribute("value").Should().BeEmpty();
        cut.Find(".tnt-input-select-search-input").GetAttribute("value").Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task Sort_Selector_Applies_To_Option_Children() {
        var model = new TestModel();
        var cut = RenderInputSelect(model, parameters => parameters
            .Add(p => p.SortSelector, option => option.Label)
            .Add(p => p.SortDirection, SortDirection.Descending));
        var input = cut.Find(".tnt-input-select-search-input");

        await input.KeyUpAsync(new KeyboardEventArgs { Key = "ArrowDown" });
        await input.KeyUpAsync(new KeyboardEventArgs { Key = "Enter" });

        model.Value.Should().Be(TestSelectEnum.Delta);
    }

    [Fact]
    public void Blur_Notifies_EditContext() {
        var model = new TestModel();
        var fieldChanged = false;

        RenderFragment<EditContext> childContent = context => builder => {
            context.OnFieldChanged += (_, __) => fieldChanged = true;

            builder.OpenComponent<NTInputSelect<TestSelectEnum?>>(0);
            builder.AddAttribute(1, nameof(NTInputSelect<TestSelectEnum?>.ValueExpression), (Expression<Func<TestSelectEnum?>>)(() => model.Value));
            builder.AddAttribute(2, nameof(NTInputSelect<TestSelectEnum?>.Value), model.Value);
            builder.AddAttribute(3, nameof(NTInputSelect<TestSelectEnum?>.ValueChanged), EventCallback.Factory.Create<TestSelectEnum?>(this, v => model.Value = v));
            builder.AddAttribute(4, nameof(NTInputSelect<TestSelectEnum?>.ChildContent), CreateOptionsContent());
            builder.CloseComponent();
        };

        var cut = Render<EditForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.ChildContent, childContent));

        cut.Find(".tnt-input-select-search-input").Blur();

        fieldChanged.Should().BeTrue();
    }

    private IRenderedComponent<NTInputSelect<TestSelectEnum?>> RenderInputSelect(
        TestModel? model = null,
        Action<ComponentParameterCollectionBuilder<NTInputSelect<TestSelectEnum?>>>? configure = null) {
        model ??= new TestModel();

        return Render<NTInputSelect<TestSelectEnum?>>(parameters => {
            parameters
                .Add(p => p.ValueExpression, () => model.Value)
                .Add(p => p.Value, model.Value)
                .Add(p => p.ValueChanged, EventCallback.Factory.Create<TestSelectEnum?>(this, v => model.Value = v))
                .Add(p => p.ChildContent, CreateOptionsContent());
            configure?.Invoke(parameters);
        });
    }

    private IRenderedComponent<NTInputSelect<string?>> RenderStringInputSelect(
        StringTestModel? model = null,
        Action<ComponentParameterCollectionBuilder<NTInputSelect<string?>>>? configure = null) {
        model ??= new StringTestModel();

        return Render<NTInputSelect<string?>>(parameters => {
            parameters
                .Add(p => p.ValueExpression, () => model.Value)
                .Add(p => p.Value, model.Value)
                .Add(p => p.ValueChanged, EventCallback.Factory.Create<string?>(this, v => model.Value = v))
                .Add(p => p.ChildContent, CreateStringOptionsContent());
            configure?.Invoke(parameters);
        });
    }

    private static RenderFragment CreateOptionsContent() => builder => {
        for (var index = 0; index < _options.Length; index++) {
            var option = _options[index];
            builder.OpenComponent<NTInputSelectOption<TestSelectEnum?>>(0);
            builder.SetKey(option.Value);
            builder.AddAttribute(1, nameof(NTInputSelectOption<TestSelectEnum?>.Value), option.Value);
            builder.AddAttribute(2, nameof(NTInputSelectOption<TestSelectEnum?>.Label), option.Label);
            builder.CloseComponent();
        }
    };

    private static RenderFragment CreateStringOptionsContent() => builder => {
        builder.OpenComponent<NTInputSelectOption<string?>>(0);
        builder.AddAttribute(1, nameof(NTInputSelectOption<string?>.Value), "Alpha");
        builder.AddAttribute(2, nameof(NTInputSelectOption<string?>.Label), "Alpha");
        builder.CloseComponent();

        builder.OpenComponent<NTInputSelectOption<string?>>(3);
        builder.AddAttribute(4, nameof(NTInputSelectOption<string?>.Value), "Graphite");
        builder.AddAttribute(5, nameof(NTInputSelectOption<string?>.Label), "Graphite");
        builder.CloseComponent();
    };

    private sealed class TestModel {
        public TestSelectEnum? Value { get; set; }
    }

    private sealed class StringTestModel {
        public string? Value { get; set; }
    }

    private sealed record TestOption(TestSelectEnum? Value, string Label);

    private enum TestSelectEnum {
        Alpha,
        Beta,
        Gamma,
        Delta
    }
}
