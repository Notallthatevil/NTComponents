using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;

namespace NTComponents.Tests.Form;

public class NTSelect_Tests : BunitContext {
    private sealed class RequiredModel {
        [Required]
        public string? Status { get; set; }
    }

    private sealed class TestModel {
        public string? Status { get; set; }

        public bool Enabled { get; set; }

        public bool? OptionalEnabled { get; set; }

        public TestSelectEnum Mode { get; set; }

        public TestSelectEnum? OptionalMode { get; set; }

        public int UnsupportedValue { get; set; }
    }

    [Fact]
    public void Renders_Native_Select_With_Option_And_OptGroup_ChildContent() {
        var model = new TestModel();

        var cut = RenderSelect(model, parameters => parameters
            .Add(p => p.ElementId, "status-select")
            .Add(p => p.Label, "Status")
            .AddChildContent("""
                <option value="">Choose one</option>
                <optgroup label="Open">
                    <option value="new">New</option>
                    <option value="active">Active</option>
                </optgroup>
                """));

        var select = cut.Find("select");
        select.GetAttribute("id").Should().Be("status-select");
        select.GetAttribute("name").Should().Be("model.Status");
        select.GetAttribute("class").Should().Contain("nt-select-control");
        cut.Find("label.nt-input-container").GetAttribute("for").Should().Be("status-select");
        cut.FindAll("optgroup[label='Open']").Should().HaveCount(1);
        cut.FindAll("option").Should().HaveCount(3);
        cut.Find(".nt-select-indicator").Should().NotBeNull();
    }

    [Fact]
    public void Change_Updates_Bound_Value_And_Invokes_BindAfter() {
        var model = new TestModel();
        string? bindAfterValue = null;

        var cut = RenderSelect(model, parameters => parameters
            .Add(p => p.BindAfter, EventCallback.Factory.Create<string?>(this, value => bindAfterValue = value))
            .AddChildContent("""
                <option value="">Choose one</option>
                <option value="active">Active</option>
                """));

        cut.Find("select").Change("active");

        model.Status.Should().Be("active");
        bindAfterValue.Should().Be("active");
    }

    [Fact]
    public void Parses_Bool_Value() {
        var model = new TestModel();

        var cut = Render<NTSelect<bool>>(parameters => parameters
            .Add(p => p.Value, model.Enabled)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<bool>(this, value => model.Enabled = value))
            .Add(p => p.ValueExpression, (Expression<Func<bool>>)(() => model.Enabled))
            .AddChildContent("""
                <option value="false">No</option>
                <option value="true">Yes</option>
                """));

        cut.Find("select").Change("true");

        model.Enabled.Should().BeTrue();
    }

    [Fact]
    public void Parses_Nullable_Bool_Empty_Value() {
        var model = new TestModel { OptionalEnabled = true };

        var cut = Render<NTSelect<bool?>>(parameters => parameters
            .Add(p => p.Value, model.OptionalEnabled)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<bool?>(this, value => model.OptionalEnabled = value))
            .Add(p => p.ValueExpression, (Expression<Func<bool?>>)(() => model.OptionalEnabled))
            .AddChildContent("""
                <option value="">Unset</option>
                <option value="false">No</option>
                <option value="true">Yes</option>
                """));

        cut.Find("select").Change("");

        model.OptionalEnabled.Should().BeNull();
    }

    [Fact]
    public void Parses_Enum_Value() {
        var model = new TestModel();

        var cut = Render<NTSelect<TestSelectEnum>>(parameters => parameters
            .Add(p => p.Value, model.Mode)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<TestSelectEnum>(this, value => model.Mode = value))
            .Add(p => p.ValueExpression, (Expression<Func<TestSelectEnum>>)(() => model.Mode))
            .AddChildContent("""
                <option value="Alpha">Alpha</option>
                <option value="Beta">Beta</option>
                """));

        cut.Find("select").Change("Beta");

        model.Mode.Should().Be(TestSelectEnum.Beta);
    }

    [Fact]
    public void Parses_Nullable_Enum_Empty_Value() {
        var model = new TestModel { OptionalMode = TestSelectEnum.Beta };

        var cut = Render<NTSelect<TestSelectEnum?>>(parameters => parameters
            .Add(p => p.Value, model.OptionalMode)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<TestSelectEnum?>(this, value => model.OptionalMode = value))
            .Add(p => p.ValueExpression, (Expression<Func<TestSelectEnum?>>)(() => model.OptionalMode))
            .AddChildContent("""
                <option value="">Unset</option>
                <option value="Alpha">Alpha</option>
                <option value="Beta">Beta</option>
                """));

        cut.Find("select").Change("");

        model.OptionalMode.Should().BeNull();
    }

    [Fact]
    public void Unsupported_Value_Type_Throws() {
        var model = new TestModel();

        var act = () => Render<NTSelect<int>>(parameters => parameters
            .Add(p => p.Value, model.UnsupportedValue)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<int>(this, value => model.UnsupportedValue = value))
            .Add(p => p.ValueExpression, (Expression<Func<int>>)(() => model.UnsupportedValue))
            .AddChildContent("<option value=\"1\">One</option>"));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*supports string, bool, enum, nullable bool, and nullable enum values*");
    }

    [Fact]
    public void Multiple_Attribute_Throws() {
        var act = () => RenderSelect(configure: parameters => parameters
            .Add(p => p.AdditionalAttributes, new Dictionary<string, object> {
                ["multiple"] = true,
                ["data-field"] = "status"
            })
            .AddChildContent("<option value=\"active\">Active</option>"));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*does not support multi-select*");
    }

    [Fact]
    public void Inherits_Form_Appearance_Density_And_State() {
        var model = new TestModel();

        var cut = Render<NTForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Appearance, NTFormAppearance.Filled)
            .Add(p => p.Density, NTFormDensity.Dense)
            .Add(p => p.Disabled, true)
            .Add(p => p.ChildContent, (EditContext _) => builder => {
                builder.OpenComponent<NTSelect<string?>>(0);
                builder.AddAttribute(1, nameof(NTSelect<string?>.Value), model.Status);
                builder.AddAttribute(2, nameof(NTSelect<string?>.ValueChanged), EventCallback.Factory.Create<string?>(this, value => model.Status = value));
                builder.AddAttribute(3, nameof(NTSelect<string?>.ValueExpression), (Expression<Func<string?>>)(() => model.Status));
                builder.AddAttribute(4, nameof(NTSelect<string?>.ChildContent), (RenderFragment)(childBuilder => {
                    childBuilder.AddMarkupContent(0, "<option value=\"active\">Active</option>");
                }));
                builder.CloseComponent();
            }));

        var rootClass = cut.Find(".nt-input").GetAttribute("class")!;
        rootClass.Should().Contain("nt-input-filled");
        rootClass.Should().Contain("nt-input-dense");
        rootClass.Should().Contain("nt-input-disabled");
        cut.Find("select").HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public void Blur_Validates_Required_Field_And_Renders_Error_State() {
        var model = new RequiredModel();

        var cut = Render<NTForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.ChildContent, (EditContext _) => builder => {
                builder.OpenComponent<DataAnnotationsValidator>(0);
                builder.CloseComponent();
                builder.OpenComponent<NTSelect<string?>>(1);
                builder.AddAttribute(2, nameof(NTSelect<string?>.Value), model.Status);
                builder.AddAttribute(3, nameof(NTSelect<string?>.ValueChanged), EventCallback.Factory.Create<string?>(this, value => model.Status = value));
                builder.AddAttribute(4, nameof(NTSelect<string?>.ValueExpression), (Expression<Func<string?>>)(() => model.Status));
                builder.AddAttribute(5, nameof(NTSelect<string?>.ChildContent), (RenderFragment)(childBuilder => {
                    childBuilder.AddMarkupContent(0, "<option value=\"\">Choose one</option><option value=\"active\">Active</option>");
                }));
                builder.CloseComponent();
            }));

        cut.Find("select").Blur();

        cut.Find(".nt-input").GetAttribute("class").Should().Contain("nt-input-invalid");
        cut.Find(".nt-input-error-text").TextContent.Should().Be("The Status field is required.");
        cut.Find("select").GetAttribute("aria-invalid").Should().Be("true");
        cut.Find("select").GetAttribute("aria-errormessage").Should().EndWith("-error");
    }

    [Fact]
    public void ReadOnly_Select_Renders_Hidden_Form_Post_Value() {
        var model = new TestModel { Status = "active" };

        var cut = RenderSelect(model, parameters => parameters
            .Add(p => p.ReadOnly, true)
            .AddChildContent("""
                <option value="">Choose one</option>
                <option value="active">Active</option>
                """));

        cut.Find("select").HasAttribute("disabled").Should().BeTrue();
        cut.Find("input[type=hidden][name='model.Status']").GetAttribute("value").Should().Be("active");
    }

    private IRenderedComponent<NTSelect<string?>> RenderSelect(TestModel? model = null, Action<ComponentParameterCollectionBuilder<NTSelect<string?>>>? configure = null) {
        model ??= new TestModel();
        return Render<NTSelect<string?>>(parameters => {
            parameters
                .Add(p => p.Value, model.Status)
                .Add(p => p.ValueChanged, EventCallback.Factory.Create<string?>(this, value => model.Status = value))
                .Add(p => p.ValueExpression, (Expression<Func<string?>>)(() => model.Status));
            configure?.Invoke(parameters);
        });
    }

    private enum TestSelectEnum {
        Alpha,
        Beta
    }
}
