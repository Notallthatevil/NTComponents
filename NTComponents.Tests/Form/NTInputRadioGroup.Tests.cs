using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;

namespace NTComponents.Tests.Form;

public class NTInputRadioGroup_Tests : BunitContext {
    private sealed class RequiredModel {
        [Required]
        public string? Choice { get; set; }
    }

    private sealed class TestModel {
        public string? Choice { get; set; } = "email";
        public DemoPriority? Priority { get; set; }
    }

    private enum DemoPriority {
        Low,
        Medium,
        High
    }

    [Fact]
    public void Renders_Fieldset_Label_And_Radio_Association() {
        var model = new TestModel();

        var cut = RenderStringRadioGroup(model, parameters => parameters
            .Add(p => p.ElementId, "contact-method")
            .Add(p => p.Label, "Contact method")
            .Add(p => p.SupportingText, "Choose one"));

        var fieldset = cut.Find("fieldset.nt-radio-fieldset");
        var radios = cut.FindAll("input[type=radio]");

        fieldset.GetAttribute("id").Should().Be("contact-method");
        fieldset.GetAttribute("role").Should().Be("radiogroup");
        fieldset.GetAttribute("aria-labelledby").Should().Be("contact-method-label");
        fieldset.GetAttribute("aria-describedby").Should().Be("contact-method-supporting");
        cut.Find("legend.nt-radio-legend").TextContent.Should().Be("Contact method");
        radios.Should().HaveCount(3);
        radios.Should().OnlyContain(radio => radio.GetAttribute("name") == radios[0].GetAttribute("name"));
        cut.Find("#contact-method-supporting").TextContent.Should().Be("Choose one");
    }

    [Fact]
    public void Only_Selected_Option_Renders_Checked_Attribute() {
        var model = new TestModel { Choice = "email" };

        var cut = RenderStringRadioGroup(model);

        var radios = cut.FindAll("input[type=radio]");
        radios[0].HasAttribute("checked").Should().BeTrue();
        radios[1].HasAttribute("checked").Should().BeFalse();
        radios[2].HasAttribute("checked").Should().BeFalse();
    }

    [Fact]
    public void Change_Updates_Bound_Value_And_Invokes_BindAfter() {
        var model = new TestModel();
        string? callbackValue = null;

        var cut = RenderStringRadioGroup(model, parameters => parameters.Add(p => p.BindAfter, EventCallback.Factory.Create<string?>(this, value => callbackValue = value)));

        cut.Find("input[value=sms]").Change("sms");

        model.Choice.Should().Be("sms");
        callbackValue.Should().Be("sms");
        cut.Find("input[value=sms]").HasAttribute("checked").Should().BeTrue();
    }

    [Fact]
    public void Change_From_Second_To_Third_Rerenders_Native_Checked_State() {
        var model = new TestModel { Choice = "sms" };

        var cut = RenderStringRadioGroup(model);

        cut.Find("input[value=none]").Change("none");

        model.Choice.Should().Be("none");
        cut.Find("input[value=sms]").HasAttribute("checked").Should().BeFalse();
        cut.Find("input[value=none]").HasAttribute("checked").Should().BeTrue();
        cut.FindAll("input[checked]").Should().ContainSingle()
            .Which.GetAttribute("value").Should().Be("none");
    }

    [Fact]
    public void Static_Ssr_Renders_Native_Radio_Names_Without_Hidden_Fallbacks() {
        SetRendererInfo(new RendererInfo("Static", false));
        var model = new TestModel();

        var cut = RenderStringRadioGroup(model, parameters => parameters.Add(p => p.GroupName, "contact"));

        cut.FindAll("input[type=hidden]").Should().BeEmpty();
        cut.FindAll("input[type=radio]").Should().OnlyContain(input => input.GetAttribute("name") == "contact");
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
                builder.OpenComponent<NTInputRadioGroup<string?>>(0);
                builder.AddAttribute(1, nameof(NTInputRadioGroup<string?>.Value), model.Choice);
                builder.AddAttribute(2, nameof(NTInputRadioGroup<string?>.ValueChanged), EventCallback.Factory.Create<string?>(this, value => model.Choice = value));
                builder.AddAttribute(3, nameof(NTInputRadioGroup<string?>.ValueExpression), (Expression<Func<string?>>)(() => model.Choice));
                builder.AddAttribute(4, nameof(NTInputRadioGroup<string?>.ChildContent), RenderStringOptions());
                builder.CloseComponent();
            }));

        var rootClass = cut.Find(".nt-radio").GetAttribute("class")!;
        rootClass.Should().Contain("nt-radio-filled");
        rootClass.Should().Contain("nt-radio-dense");
        rootClass.Should().Contain("nt-radio-disabled");
        cut.Find(".nt-radio-active-indicator").Should().NotBeNull();
        cut.Find("fieldset").HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public void Field_Overrides_Form_Appearance_Density_And_State() {
        var model = new TestModel();

        var cut = Render<NTForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Appearance, NTFormAppearance.Filled)
            .Add(p => p.Density, NTFormDensity.Dense)
            .Add(p => p.Disabled, true)
            .Add(p => p.ChildContent, (EditContext _) => builder => {
                builder.OpenComponent<NTInputRadioGroup<string?>>(0);
                builder.AddAttribute(1, nameof(NTInputRadioGroup<string?>.Value), model.Choice);
                builder.AddAttribute(2, nameof(NTInputRadioGroup<string?>.ValueChanged), EventCallback.Factory.Create<string?>(this, value => model.Choice = value));
                builder.AddAttribute(3, nameof(NTInputRadioGroup<string?>.ValueExpression), (Expression<Func<string?>>)(() => model.Choice));
                builder.AddAttribute(4, nameof(NTInputRadioGroup<string?>.Appearance), NTFormAppearance.Outlined);
                builder.AddAttribute(5, nameof(NTInputRadioGroup<string?>.Density), NTFormDensity.Comfortable);
                builder.AddAttribute(6, nameof(NTInputRadioGroup<string?>.Disabled), false);
                builder.AddAttribute(7, nameof(NTInputRadioGroup<string?>.ChildContent), RenderStringOptions());
                builder.CloseComponent();
            }));

        var rootClass = cut.Find(".nt-radio").GetAttribute("class")!;
        rootClass.Should().Contain("nt-radio-outlined");
        rootClass.Should().Contain("nt-radio-comfortable");
        rootClass.Should().NotContain("nt-radio-disabled");
        cut.FindAll(".nt-radio-active-indicator").Should().BeEmpty();
        cut.Find("fieldset").HasAttribute("disabled").Should().BeFalse();
    }

    [Fact]
    public void Enum_Value_Parses_From_Native_Radio_Change() {
        var model = new TestModel();

        var cut = Render<NTInputRadioGroup<DemoPriority?>>(parameters => {
            parameters.Add(p => p.Value, model.Priority);
            parameters.Add(p => p.ValueChanged, EventCallback.Factory.Create<DemoPriority?>(this, value => model.Priority = value));
            parameters.Add(p => p.ValueExpression, (Expression<Func<DemoPriority?>>)(() => model.Priority));
            parameters.Add(p => p.ChildContent, builder => {
                builder.OpenComponent<NTInputRadio<DemoPriority?>>(0);
                builder.AddAttribute(1, nameof(NTInputRadio<DemoPriority?>.Value), DemoPriority.Low);
                builder.AddAttribute(2, nameof(NTInputRadio<DemoPriority?>.Label), "Low");
                builder.CloseComponent();
                builder.OpenComponent<NTInputRadio<DemoPriority?>>(3);
                builder.AddAttribute(4, nameof(NTInputRadio<DemoPriority?>.Value), DemoPriority.High);
                builder.AddAttribute(5, nameof(NTInputRadio<DemoPriority?>.Label), "High");
                builder.CloseComponent();
            });
        });

        cut.Find("input[value=High]").Change("High");

        model.Priority.Should().Be(DemoPriority.High);
    }

    [Fact]
    public void Validation_Error_Uses_Group_Error_Accessibility() {
        var model = new RequiredModel();

        var cut = Render<EditForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.ChildContent, (EditContext _) => builder => {
                builder.OpenComponent<DataAnnotationsValidator>(0);
                builder.CloseComponent();
                builder.OpenComponent<NTInputRadioGroup<string?>>(1);
                builder.AddAttribute(2, nameof(NTInputRadioGroup<string?>.Value), model.Choice);
                builder.AddAttribute(3, nameof(NTInputRadioGroup<string?>.ValueChanged), EventCallback.Factory.Create<string?>(this, value => model.Choice = value));
                builder.AddAttribute(4, nameof(NTInputRadioGroup<string?>.ValueExpression), (Expression<Func<string?>>)(() => model.Choice));
                builder.AddAttribute(5, nameof(NTInputRadioGroup<string?>.Label), "Contact method");
                builder.AddAttribute(6, nameof(NTInputRadioGroup<string?>.ChildContent), RenderStringOptions());
                builder.CloseComponent();
            }));

        cut.Find("form").Submit();

        var fieldset = cut.Find("fieldset.nt-radio-fieldset");
        cut.Find(".nt-radio").GetAttribute("class").Should().Contain("nt-radio-invalid");
        cut.Find(".nt-radio-error-text").TextContent.Should().Be("The Choice field is required.");
        fieldset.GetAttribute("aria-invalid").Should().Be("true");
        fieldset.GetAttribute("aria-errormessage").Should().EndWith("-error");
    }

    [Fact]
    public void Color_Overrides_Emit_Component_Css_Variables() {
        var cut = RenderStringRadioGroup(configure: parameters => parameters
            .Add(p => p.ActiveIndicatorColor, TnTColor.Secondary)
            .Add(p => p.BackgroundColor, TnTColor.SurfaceContainerHigh)
            .Add(p => p.DisabledContainerColor, TnTColor.SurfaceContainerLow)
            .Add(p => p.DisabledContentColor, TnTColor.OnSurfaceVariant)
            .Add(p => p.DisabledOutlineColor, TnTColor.OutlineVariant)
            .Add(p => p.ErrorColor, TnTColor.Error)
            .Add(p => p.FocusColor, TnTColor.Primary)
            .Add(p => p.HoverActiveIndicatorColor, TnTColor.OnSurface)
            .Add(p => p.HoverOutlineColor, TnTColor.OnSurface)
            .Add(p => p.LabelColor, TnTColor.Secondary)
            .Add(p => p.OutlineColor, TnTColor.Outline)
            .Add(p => p.SelectedColor, TnTColor.Primary)
            .Add(p => p.SelectedStateLayerColor, TnTColor.Primary)
            .Add(p => p.StateLayerColor, TnTColor.OnSurface)
            .Add(p => p.SupportingTextColor, TnTColor.Secondary)
            .Add(p => p.UnselectedColor, TnTColor.OnSurfaceVariant));

        var style = cut.Find(".nt-radio").GetAttribute("style");

        style.Should().Contain("--nt-radio-active-indicator-color:var(--tnt-color-secondary);");
        style.Should().Contain("--nt-radio-container-color:var(--tnt-color-surface-container-high);");
        style.Should().Contain("--nt-radio-disabled-container-color:var(--tnt-color-surface-container-low);");
        style.Should().Contain("--nt-radio-disabled-content-color:var(--tnt-color-on-surface-variant);");
        style.Should().Contain("--nt-radio-disabled-outline-color:var(--tnt-color-outline-variant);");
        style.Should().Contain("--nt-radio-error-color:var(--tnt-color-error);");
        style.Should().Contain("--nt-radio-focus-color:var(--tnt-color-primary);");
        style.Should().Contain("--nt-radio-hover-active-indicator-color:var(--tnt-color-on-surface);");
        style.Should().Contain("--nt-radio-hover-outline-color:var(--tnt-color-on-surface);");
        style.Should().Contain("--nt-radio-label-color:var(--tnt-color-secondary);");
        style.Should().Contain("--nt-radio-outline-color:var(--tnt-color-outline);");
        style.Should().Contain("--nt-radio-selected-color:var(--tnt-color-primary);");
        style.Should().Contain("--nt-radio-selected-state-layer-color:var(--tnt-color-primary);");
        style.Should().Contain("--nt-radio-state-layer-color:var(--tnt-color-on-surface);");
        style.Should().Contain("--nt-radio-supporting-text-color:var(--tnt-color-secondary);");
        style.Should().Contain("--nt-radio-unselected-color:var(--tnt-color-on-surface-variant);");
    }

    [Fact]
    public void Additional_Attributes_Do_Not_Override_Owned_Group_Attributes() {
        var cut = RenderStringRadioGroup(configure: parameters => parameters.Add(p => p.AdditionalAttributes, new Dictionary<string, object> {
            ["class"] = "external-class",
            ["role"] = "group",
            ["data-field"] = "choice"
        }));

        var fieldset = cut.Find("fieldset.nt-radio-fieldset");

        cut.Find(".nt-radio").GetAttribute("class").Should().Contain("external-class");
        fieldset.GetAttribute("role").Should().Be("radiogroup");
        fieldset.GetAttribute("data-field").Should().Be("choice");
    }

    private IRenderedComponent<NTInputRadioGroup<string?>> RenderStringRadioGroup(TestModel? model = null, Action<ComponentParameterCollectionBuilder<NTInputRadioGroup<string?>>>? configure = null) {
        model ??= new TestModel();
        return Render<NTInputRadioGroup<string?>>(parameters => {
            parameters.Add(p => p.Value, model.Choice);
            parameters.Add(p => p.ValueChanged, EventCallback.Factory.Create<string?>(this, value => model.Choice = value));
            parameters.Add(p => p.ValueExpression, (Expression<Func<string?>>)(() => model.Choice));
            parameters.Add(p => p.ChildContent, RenderStringOptions());
            configure?.Invoke(parameters);
        });
    }

    private static RenderFragment RenderStringOptions() => builder => {
        builder.OpenComponent<NTInputRadio<string?>>(0);
        builder.AddAttribute(1, nameof(NTInputRadio<string?>.Value), "email");
        builder.AddAttribute(2, nameof(NTInputRadio<string?>.Label), "Email");
        builder.CloseComponent();
        builder.OpenComponent<NTInputRadio<string?>>(3);
        builder.AddAttribute(4, nameof(NTInputRadio<string?>.Value), "sms");
        builder.AddAttribute(5, nameof(NTInputRadio<string?>.Label), "SMS");
        builder.CloseComponent();
        builder.OpenComponent<NTInputRadio<string?>>(6);
        builder.AddAttribute(7, nameof(NTInputRadio<string?>.Value), "none");
        builder.AddAttribute(8, nameof(NTInputRadio<string?>.Label), "None");
        builder.AddAttribute(9, nameof(NTInputRadio<string?>.SupportingText), "Do not contact");
        builder.CloseComponent();
    };
}
