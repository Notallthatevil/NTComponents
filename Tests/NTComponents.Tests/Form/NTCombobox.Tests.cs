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

    private sealed class NullStringValue {
        public override string? ToString() => null;
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
        cut.Find(".nt-combobox-menu").GetAttribute("popover").Should().Be("manual");
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

    // Behavior source: Disabled and ReadOnly field contracts prevent browser notifications from changing the bound selection.
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task NotifySelectionChanged_Does_Not_Update_A_Disabled_Or_ReadOnly_Field(bool disabled, bool readOnly) {
        var model = new TestModel { Tags = ["design"] };
        var bindAfterCalls = 0;
        var cut = RenderCombobox(model, parameters => parameters
            .Add(p => p.Disabled, disabled)
            .Add(p => p.ReadOnly, readOnly)
            .Add(p => p.BindAfter, EventCallback.Factory.Create<IReadOnlyList<string>?>(this, _ => bindAfterCalls++)));

        await cut.InvokeAsync(() => cut.Instance.NotifyComboboxSelectionChanged(["engineering"]));

        model.Tags.Should().Equal("design");
        bindAfterCalls.Should().Be(0);
    }

    // Behavior source: NotifyComboboxTouched is the documented browser blur/touched notification and closes the popup after selection.
    [Fact]
    public async Task NotifyTouched_Closes_An_Open_Menu() {
        var cut = RenderCombobox();
        await cut.InvokeAsync(() => cut.Instance.NotifyComboboxSelectionChanged(["design"]));
        cut.Find(".nt-combobox-menu").HasAttribute("hidden").Should().BeFalse();

        await cut.InvokeAsync(cut.Instance.NotifyComboboxTouched);

        cut.Find(".nt-combobox-menu").HasAttribute("hidden").Should().BeTrue();
        cut.Find("input[role='combobox']").GetAttribute("aria-expanded").Should().Be("false");
    }

    // Behavior source: Browser selection notifications open the popup, and an equivalent parent rerender preserves that open state.
    [Fact]
    public async Task Equivalent_Rerender_Preserves_Open_State_Class() {
        var cut = RenderCombobox();
        await cut.InvokeAsync(() => cut.Instance.NotifyComboboxSelectionChanged(["design"]));

        cut.Render();

        cut.Find(".nt-input").GetAttribute("class").Should().Contain("nt-combobox-open");
        cut.Find("input[role='combobox']").GetAttribute("aria-expanded").Should().Be("true");
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

    // Behavior source: An uninitialized nullable selection is the empty multi-selection state and emits no form values.
    [Fact]
    public void Null_Bound_List_Renders_As_Empty_Selection() {
        var model = new TestModel { Tags = null! };

        var cut = RenderCombobox(model);

        cut.Find("input[role='combobox']").GetAttribute("value").Should().BeNullOrEmpty();
        cut.FindAll(".nt-combobox-option-selected").Should().BeEmpty();
        cut.FindAll("input[type=hidden]").Should().BeEmpty();
    }

    // Behavior source: Selected summary text uses option labels when available and stable formatted values when an item is no longer present.
    [Fact]
    public void Selected_Value_Without_Current_Item_Uses_Formatted_Value() {
        var model = new TestModel { Tags = ["archived"] };

        var cut = RenderCombobox(model);

        cut.Find("input[role='combobox']").GetAttribute("value").Should().Be("archived");
    }

    // Behavior source: Option values are emitted through invariant formatting, including IFormattable value types.
    [Fact]
    public void Formattable_Option_Value_Uses_Invariant_Native_Text() {
        IReadOnlyList<int> values = [42];
        var items = new[] { new NTComboboxOption<int>(42, "Forty two") };
        var cut = Render<NTCombobox<int>>(parameters => parameters
            .Add(p => p.Value, values)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<IReadOnlyList<int>>(this, selected => values = selected))
            .Add(p => p.ValueExpression, () => values)
            .Add(p => p.Items, items));

        cut.Find("[data-nt-combobox-option='true']").GetAttribute("data-nt-combobox-value").Should().Be("42");
        cut.Find("input[role='combobox']").GetAttribute("value").Should().Be("Forty two");
    }

    // Behavior source: Nullable option values have a stable empty native representation rather than the text "null".
    [Fact]
    public void Null_Option_Value_Uses_Empty_Native_Text() {
        IReadOnlyList<string?> values = [null];
        var items = new[] { new NTComboboxOption<string?>(null, "None") };
        var cut = Render<NTCombobox<string?>>(parameters => parameters
            .Add(p => p.Value, values)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<IReadOnlyList<string?>>(this, selected => values = selected))
            .Add(p => p.ValueExpression, () => values)
            .Add(p => p.Items, items));

        cut.Find("[data-nt-combobox-option='true']").GetAttribute("data-nt-combobox-value").Should().BeEmpty();
        cut.Find("input[role='combobox']").GetAttribute("value").Should().Be("None");
    }

    // Behavior source: A value whose string representation is null has a stable empty native representation.
    [Fact]
    public void Null_ToString_Result_Uses_Empty_Native_Text() {
        IReadOnlyList<NullStringValue> values = [];
        var items = new[] { new NTComboboxOption<NullStringValue>(new NullStringValue(), "Empty representation") };
        var cut = Render<NTCombobox<NullStringValue>>(parameters => parameters
            .Add(p => p.Value, values)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<IReadOnlyList<NullStringValue>>(this, selected => values = selected))
            .Add(p => p.ValueExpression, () => values)
            .Add(p => p.Items, items));

        cut.Find("[data-nt-combobox-option='true']").GetAttribute("data-nt-combobox-value").Should().BeEmpty();
    }

    // Behavior source: Disabled options remain visible, and selected-state rendering uses the current bound values.
    [Fact]
    public void Selected_Disabled_Option_Renders_Both_States() {
        var model = new TestModel { Tags = ["qa"] };

        var cut = RenderCombobox(model);

        var optionClass = cut.Find("[data-nt-combobox-value='qa']").GetAttribute("class");
        optionClass.Should().Contain("nt-combobox-option-selected");
        optionClass.Should().Contain("nt-combobox-option-disabled");
    }

    // Behavior source: Comparer is documented to control display and selected-state rendering for custom equality semantics.
    [Fact]
    public void Custom_Comparer_Controls_Selected_Label_And_State() {
        var model = new TestModel { Tags = ["DESIGN"] };
        var cut = RenderCombobox(model, parameters => parameters.Add(p => p.Comparer, StringComparer.OrdinalIgnoreCase));

        cut.Find("input[role='combobox']").GetAttribute("value").Should().Be("Design");
        cut.Find("[data-nt-combobox-value='design']").GetAttribute("class").Should().Contain("nt-combobox-option-selected");
    }

    // Behavior source: Error text takes precedence over the normal dropdown indicator in the shared field adornment contract.
    [Fact]
    public void Error_Text_Replaces_Dropdown_Indicator_With_Error_Adornment() {
        var cut = RenderCombobox(configure: parameters => parameters.Add(p => p.ErrorText, "Choose at least one tag"));

        cut.FindAll(".nt-combobox-indicator").Should().BeEmpty();
        cut.Find(".nt-input-error-icon").TextContent.Should().Contain(MaterialIcon.Error.Icon);
    }

    // Behavior source: CssClass is the public field styling hook and must be preserved alongside the combobox control class.
    [Fact]
    public void Custom_Css_Class_Is_Appended_To_Combobox_Control() {
        var cut = RenderCombobox(configure: parameters => parameters.AddUnmatched("class", "custom-combobox"));

        cut.Find("input[role='combobox']").GetAttribute("class").Should().Contain("custom-combobox");
    }

    // Behavior source: IAsyncDisposable teardown is idempotent and safe before browser enhancement initializes a module.
    [Fact]
    public async Task DisposeAsync_Is_Idempotent_Before_Render() {
        var component = new NTCombobox<string>();

        var act = async () => {
            await component.DisposeAsync();
            await component.DisposeAsync();
        };

        await act.Should().NotThrowAsync();
    }

    // Behavior source: Component disposal must tolerate a disconnected browser circuit so teardown remains safe.
    [Fact]
    public async Task DisposeAsync_Ignores_JsDisconnectedException() {
        using var context = new BunitContext();
        var module = context.JSInterop.SetupModule(JsModulePath);
        module.SetupVoid("onLoad", _ => true).SetVoidResult();
        module.SetupVoid("onUpdate", _ => true).SetVoidResult();
        module.SetupVoid("onDispose", _ => true).SetException(new Microsoft.JSInterop.JSDisconnectedException("Disconnected"));
        IReadOnlyList<string> values = [];
        var cut = context.Render<NTCombobox<string>>(parameters => parameters
            .Add(p => p.Value, values)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<IReadOnlyList<string>>(this, selected => values = selected))
            .Add(p => p.ValueExpression, () => values)
            .Add(p => p.Items, Options));

        var act = () => cut.Instance.DisposeAsync().AsTask();

        await act.Should().NotThrowAsync();
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
