using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.Linq.Expressions;

namespace NTComponents.Tests.Form;

public class NTCombobox_Tests : BunitContext {
    private const string JsModulePath = "./_content/NTComponents/Form/NTCombobox.razor.js";

    private static readonly IReadOnlyList<NTComboboxOption<string>> Options = [
        new("design", "Design") {
            SupportingText = "Visual work"
        },
        new("engineering", "Engineering") {
            SupportingText = "Implementation"
        },
        new("qa", "QA") {
            Disabled = true
        }
    ];

    public NTCombobox_Tests() {
        var module = JSInterop.SetupModule(JsModulePath);
        module.SetupVoid("onLoad", _ => true).SetVoidResult();
        module.SetupVoid("onUpdate", _ => true).SetVoidResult();
        module.SetupVoid("onDispose", _ => true).SetVoidResult();
    }

    private sealed class TestModel {
        public IReadOnlyList<string> Tags { get; set; } = [];
    }

    [Fact]
    public void Renders_Combobox_Control_And_Listbox_Contract_For_TypeScript_Module() {
        var cut = RenderCombobox(configure: parameters => parameters
            .Add(p => p.ElementId, "tags-combobox")
            .Add(p => p.Label, "Tags"));

        var input = cut.Find("input[role='combobox']");
        input.GetAttribute("id").Should().Be("tags-combobox");
        input.HasAttribute("readonly").Should().BeTrue();
        input.GetAttribute("aria-haspopup").Should().Be("listbox");
        input.GetAttribute("aria-expanded").Should().Be("false");
        input.GetAttribute("data-nt-combobox-input").Should().Be("true");
        input.GetAttribute("data-nt-combobox-listbox").Should().Be("tags-combobox-listbox");
        cut.Find("label.nt-input-container").GetAttribute("for").Should().Be("tags-combobox");

        var listbox = cut.Find("ul[role='listbox']");
        listbox.GetAttribute("aria-multiselectable").Should().Be("true");
        listbox.GetAttribute("id").Should().Be("tags-combobox-listbox");
        cut.Find(".nt-combobox-menu").HasAttribute("hidden").Should().BeTrue();
        cut.FindAll("[data-nt-combobox-option='true']").Should().HaveCount(3);
    }

    [Fact]
    public async Task NotifySelectionChanged_Updates_Bound_Value_And_Invokes_BindAfter() {
        var model = new TestModel();
        IReadOnlyList<string>? bindAfterValue = null;

        var cut = RenderCombobox(model, parameters => parameters
            .Add(p => p.BindAfter, EventCallback.Factory.Create<IReadOnlyList<string>?>(this, value => bindAfterValue = value)));

        await cut.InvokeAsync(() => cut.Instance.NotifyComboboxSelectionChanged(["design", "engineering"]));

        model.Tags.Should().Equal("design", "engineering");
        bindAfterValue.Should().Equal("design", "engineering");
        cut.FindAll(".nt-combobox-option-selected").Should().HaveCount(2);
        cut.Find(".nt-combobox-menu").HasAttribute("hidden").Should().BeFalse();
    }

    [Fact]
    public async Task NotifySelectionChanged_Ignores_Unknown_Values() {
        var model = new TestModel();
        var cut = RenderCombobox(model);

        await cut.InvokeAsync(() => cut.Instance.NotifyComboboxSelectionChanged(["engineering", "missing"]));

        model.Tags.Should().Equal("engineering");
        cut.FindAll(".nt-combobox-option-selected").Should().HaveCount(1);
    }

    [Fact]
    public void Selected_Values_Render_Hidden_Form_Post_Inputs() {
        var model = new TestModel { Tags = ["design", "engineering"] };

        var cut = RenderCombobox(model);

        var hiddenInputs = cut.FindAll("input[type='hidden'][name='model.Tags']");
        hiddenInputs.Should().HaveCount(2);
        hiddenInputs[0].GetAttribute("value").Should().Be("design");
        hiddenInputs[1].GetAttribute("value").Should().Be("engineering");
        cut.Find("input[role='combobox']").HasAttribute("name").Should().BeFalse();
    }

    [Fact]
    public void Inherits_Form_Appearance_Density_And_Disabled_State() {
        var model = new TestModel();

        var cut = Render<NTForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Appearance, NTFormAppearance.Filled)
            .Add(p => p.Density, NTFormDensity.Dense)
            .Add(p => p.Disabled, true)
            .Add(p => p.ChildContent, (EditContext _) => builder => {
                builder.OpenComponent<NTCombobox<string>>(0);
                builder.AddAttribute(1, nameof(NTCombobox<string>.Value), model.Tags);
                builder.AddAttribute(2, nameof(NTCombobox<string>.ValueChanged), EventCallback.Factory.Create<IReadOnlyList<string>>(this, value => model.Tags = value));
                builder.AddAttribute(3, nameof(NTCombobox<string>.ValueExpression), (Expression<Func<IReadOnlyList<string>>>)(() => model.Tags));
                builder.AddAttribute(4, nameof(NTCombobox<string>.Items), Options);
                builder.CloseComponent();
            }));

        var rootClass = cut.Find(".nt-input").GetAttribute("class")!;
        rootClass.Should().Contain("nt-input-filled");
        rootClass.Should().Contain("nt-input-dense");
        rootClass.Should().Contain("nt-input-disabled");
        cut.Find("input[role='combobox']").HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public void Condensed_MenuItemAppearance_Renders_Root_Class() {
        var cut = RenderCombobox(configure: parameters => parameters
            .Add(p => p.MenuItemAppearance, NTMenuItemAppearance.Condensed));

        cut.Find(".nt-input").GetAttribute("class").Should().Contain("nt-combobox-menu-items-condensed");
    }

    private IRenderedComponent<NTCombobox<string>> RenderCombobox(TestModel? model = null, Action<ComponentParameterCollectionBuilder<NTCombobox<string>>>? configure = null) {
        model ??= new TestModel();
        return Render<NTCombobox<string>>(parameters => {
            parameters
                .Add(p => p.Value, model.Tags)
                .Add(p => p.ValueChanged, EventCallback.Factory.Create<IReadOnlyList<string>>(this, value => model.Tags = value))
                .Add(p => p.ValueExpression, (Expression<Func<IReadOnlyList<string>>>)(() => model.Tags))
                .Add(p => p.Items, Options);
            configure?.Invoke(parameters);
        });
    }
}
