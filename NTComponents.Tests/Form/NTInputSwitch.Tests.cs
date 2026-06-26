using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;

namespace NTComponents.Tests.Form;

public class NTInputSwitch_Tests : BunitContext {
    private sealed class RequiredModel {
        [Range(typeof(bool), "true", "true", ErrorMessage = "Enable this setting")]
        public bool Enabled { get; set; }
    }

    private sealed class TestModel {
        public bool Enabled { get; set; }
    }

    [Fact]
    public void Renders_Label_And_Switch_Association() {
        var cut = RenderSwitch(configure: parameters => parameters
            .Add(p => p.ElementId, "dark-theme")
            .Add(p => p.Label, "Dark theme")
            .Add(p => p.SupportingText, "Applies immediately"));

        var input = cut.Find("input[type=checkbox]");
        var label = cut.Find("label.nt-switch-label");

        input.GetAttribute("id").Should().Be("dark-theme");
        input.GetAttribute("role").Should().Be("switch");
        input.GetAttribute("aria-checked").Should().Be("false");
        label.GetAttribute("for").Should().Be("dark-theme");
        input.GetAttribute("aria-describedby").Should().Contain("dark-theme-supporting");
        cut.Find("#dark-theme-supporting").TextContent.Should().Be("Applies immediately");
    }

    [Fact]
    public void Omits_DescribedBy_When_No_Description_Is_Rendered() {
        var cut = RenderSwitch();

        cut.Find("input[type=checkbox]").HasAttribute("aria-describedby").Should().BeFalse();
    }

    [Fact]
    public void Merges_External_DescribedBy_With_Internal_Description() {
        var cut = RenderSwitch(configure: parameters => parameters
            .Add(p => p.ElementId, "alerts")
            .Add(p => p.SupportingText, "Sends notifications")
            .Add(p => p.AdditionalAttributes, new Dictionary<string, object> {
                ["aria-describedby"] = "external-help"
            }));

        GetIdReferences(cut.Find("input[type=checkbox]").GetAttribute("aria-describedby")).Should().BeEquivalentTo("external-help", "alerts-supporting");
    }

    [Fact]
    public void Generated_Input_Id_Is_Deterministic_For_Field() {
        var model = new TestModel();

        using var first = RenderSwitch(model, parameters => parameters.Add(p => p.Label, "Enabled"));
        var firstInputId = first.Find("input[type=checkbox]").GetAttribute("id");

        using var second = RenderSwitch(model, parameters => parameters.Add(p => p.Label, "Enabled"));

        firstInputId.Should().NotBeNullOrWhiteSpace();
        firstInputId.Should().StartWith("nt-switch-model-enabled-");
        second.Find("input[type=checkbox]").GetAttribute("id").Should().Be(firstInputId);
    }

    [Fact]
    public void Change_Updates_Bound_Value_And_Invokes_BindAfter() {
        var model = new TestModel();
        var callbackValue = false;

        var cut = RenderSwitch(model, parameters => parameters.Add(p => p.BindAfter, EventCallback.Factory.Create<bool>(this, value => callbackValue = value)));

        cut.Find("input[type=checkbox]").Change(true);

        model.Enabled.Should().BeTrue();
        callbackValue.Should().BeTrue();
        cut.Find("input[type=checkbox]").GetAttribute("aria-checked").Should().Be("true");
    }

    [Fact]
    public void Leading_And_Trailing_Icons_Render_When_Set() {
        var cut = RenderSwitch(configure: parameters => parameters
            .Add(p => p.Label, "Alerts")
            .Add(p => p.LeadingIcon, MaterialIcon.Notifications)
            .Add(p => p.TrailingIcon, MaterialIcon.Info));

        cut.Find(".nt-switch").GetAttribute("class").Should().Contain("nt-switch-has-leading-icon").And.Contain("nt-switch-has-trailing-icon");
        cut.Find(".nt-switch-leading").TextContent.Should().Contain("notifications");
        cut.Find(".nt-switch-trailing").TextContent.Should().Contain("info");
    }

    [Fact]
    public void Handle_Icons_Are_Opt_In() {
        using var defaultSwitch = RenderSwitch();

        defaultSwitch.Find(".nt-switch").GetAttribute("class").Should().NotContain("nt-switch-has-handle-icon");
        defaultSwitch.FindAll(".nt-switch-handle-icon").Should().BeEmpty();

        var optInSwitch = RenderSwitch(configure: parameters => parameters.Add(p => p.ShowHandleIcon, true));

        optInSwitch.Find(".nt-switch").GetAttribute("class").Should().Contain("nt-switch-has-handle-icon");
        optInSwitch.FindAll(".nt-switch-handle-icon").Should().HaveCount(2);
    }

    [Fact]
    public void Trailing_Variant_Renders_Trailing_Control_Class() {
        var cut = RenderSwitch(configure: parameters => parameters
            .Add(p => p.Label, "Email alerts")
            .Add(p => p.Variant, NTSwitchVariant.Trailing));

        var rootClass = cut.Find(".nt-switch").GetAttribute("class");

        rootClass.Should().Contain("nt-switch-trailing-control");
        rootClass.Should().NotContain("nt-switch-leading-control");
    }

    [Fact]
    public void Disabled_Comes_From_Form_And_Removes_Form_Post_Fallback() {
        var model = new TestModel();

        var cut = Render<NTForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Disabled, true)
            .Add(p => p.ChildContent, (EditContext _) => builder => {
                builder.OpenComponent<NTInputSwitch>(0);
                builder.AddAttribute(1, nameof(NTInputSwitch.Value), model.Enabled);
                builder.AddAttribute(2, nameof(NTInputSwitch.ValueChanged), EventCallback.Factory.Create<bool>(this, value => model.Enabled = value));
                builder.AddAttribute(3, nameof(NTInputSwitch.ValueExpression), (Expression<Func<bool>>)(() => model.Enabled));
                builder.AddAttribute(4, nameof(NTInputSwitch.Label), "Enabled");
                builder.CloseComponent();
            }));

        cut.Find(".nt-switch").GetAttribute("class").Should().Contain("nt-switch-disabled");
        cut.Find("input[type=checkbox]").HasAttribute("disabled").Should().BeTrue();
        cut.FindAll("input[type=hidden]").Should().BeEmpty();
    }

    [Fact]
    public void ReadOnly_Disables_Native_Switch_And_Posts_Current_Value() {
        SetRendererInfo(new RendererInfo("Static", false));
        var model = new TestModel { Enabled = true };

        var cut = RenderSwitch(model, parameters => parameters.Add(p => p.ReadOnly, true));
        var switchInput = cut.Find("input[type=checkbox]");
        var canPostEditableSwitchValue = !switchInput.HasAttribute("disabled") && !string.IsNullOrWhiteSpace(switchInput.GetAttribute("name"));

        switchInput.HasAttribute("disabled").Should().BeTrue();
        canPostEditableSwitchValue.Should().BeFalse();
        cut.Find(".nt-switch").GetAttribute("class").Should().Contain("nt-switch-readonly");

        var hiddenInput = cut.FindAll("input[type=hidden]").Should().ContainSingle().Which;
        hiddenInput.GetAttribute("name").Should().NotBeNullOrWhiteSpace();
        hiddenInput.GetAttribute("value").Should().Be("true");
    }

    [Fact]
    public void Renders_Hidden_False_Form_Post_Fallback_After_Switch() {
        var cut = RenderSwitch();
        var inputs = cut.FindAll("input");

        inputs.Should().HaveCount(2);
        inputs[0].GetAttribute("type").Should().Be("checkbox");
        inputs[1].GetAttribute("type").Should().Be("hidden");
        inputs[1].GetAttribute("name").Should().Be(inputs[0].GetAttribute("name"));
        inputs[1].GetAttribute("value").Should().Be("false");
    }

    [Fact]
    public void Additional_Attributes_Do_Not_Override_Owned_Input_Attributes() {
        var cut = RenderSwitch(configure: parameters => parameters.Add(p => p.AdditionalAttributes, new Dictionary<string, object> {
            ["class"] = "external-class",
            ["type"] = "text",
            ["role"] = "checkbox",
            ["name"] = "posted.enabled",
            ["data-field"] = "enabled"
        }));

        var input = cut.Find("input[type=checkbox]");

        input.GetAttribute("type").Should().Be("checkbox");
        input.GetAttribute("role").Should().Be("switch");
        input.GetAttribute("name").Should().Be("posted.enabled");
        input.GetAttribute("data-field").Should().Be("enabled");
        input.GetAttribute("class").Should().BeNull();
    }

    [Fact]
    public void Color_Overrides_Emit_Component_Css_Variables() {
        var cut = RenderSwitch(configure: parameters => parameters
            .Add(p => p.DisabledHandleColor, TnTColor.SurfaceContainerLow)
            .Add(p => p.DisabledIconColor, TnTColor.Surface)
            .Add(p => p.DisabledTrackColor, TnTColor.OutlineVariant)
            .Add(p => p.ErrorColor, TnTColor.Error)
            .Add(p => p.IconColor, TnTColor.Tertiary)
            .Add(p => p.LabelColor, TnTColor.Secondary)
            .Add(p => p.SelectedHandleColor, TnTColor.OnPrimary)
            .Add(p => p.SelectedIconColor, TnTColor.Primary)
            .Add(p => p.SelectedStateLayerColor, TnTColor.Primary)
            .Add(p => p.SelectedTrackColor, TnTColor.Primary)
            .Add(p => p.StateLayerColor, TnTColor.OnSurface)
            .Add(p => p.SupportingTextColor, TnTColor.Secondary)
            .Add(p => p.UnselectedHandleColor, TnTColor.Outline)
            .Add(p => p.UnselectedIconColor, TnTColor.SurfaceContainerHighest)
            .Add(p => p.UnselectedOutlineColor, TnTColor.Outline)
            .Add(p => p.UnselectedTrackColor, TnTColor.SurfaceContainerHighest));

        var style = cut.Find(".nt-switch").GetAttribute("style");

        style.Should().Contain("--nt-switch-selected-track-color:var(--tnt-color-primary);");
        style.Should().Contain("--nt-switch-unselected-track-color:var(--tnt-color-surface-container-highest);");
        style.Should().Contain("--nt-switch-selected-handle-color:var(--tnt-color-on-primary);");
        style.Should().Contain("--nt-switch-disabled-track-color:var(--tnt-color-outline-variant);");
    }

    [Fact]
    public void Validation_Error_Uses_Error_Accessibility() {
        var model = new RequiredModel();

        var cut = Render<EditForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.ChildContent, (EditContext _) => builder => {
                builder.OpenComponent<DataAnnotationsValidator>(0);
                builder.CloseComponent();
                builder.OpenComponent<NTInputSwitch>(1);
                builder.AddAttribute(2, nameof(NTInputSwitch.Value), model.Enabled);
                builder.AddAttribute(3, nameof(NTInputSwitch.ValueChanged), EventCallback.Factory.Create<bool>(this, value => model.Enabled = value));
                builder.AddAttribute(4, nameof(NTInputSwitch.ValueExpression), (Expression<Func<bool>>)(() => model.Enabled));
                builder.AddAttribute(5, nameof(NTInputSwitch.Label), "Enabled");
                builder.CloseComponent();
            }));

        cut.Find("form").Submit();

        var input = cut.Find("input[type=checkbox]");
        cut.Find(".nt-switch").GetAttribute("class").Should().Contain("nt-invalid");
        cut.Find(".nt-switch-error-text").TextContent.Should().Be("Enable this setting");
        input.GetAttribute("aria-invalid").Should().Be("true");
        input.GetAttribute("aria-errormessage").Should().EndWith("-error");
    }

    [Fact]
    public void Required_Parameter_Revalidates_When_Checked_And_Unchecked() {
        var model = new TestModel();
        var cut = RenderRequiredSwitch(model);
        var input = cut.Find("input[type=checkbox]");

        cut.Find("form").Submit();
        cut.FindAll(".nt-switch-error-text").Should().HaveCount(1);

        input.Change(true);
        cut.FindAll(".nt-switch-error-text").Should().BeEmpty();
        cut.Find(".nt-switch").GetAttribute("class").Should().NotContain("nt-invalid");

        input.Change(false);
        cut.Find(".nt-switch-error-text").TextContent.Should().Be("Enable alerts before submitting");
        cut.Find(".nt-switch").GetAttribute("class").Should().Contain("nt-invalid");
    }

    [Fact]
    public void Required_Parameter_Validates_On_Blur_After_Tab_Through() {
        var model = new TestModel();
        var cut = RenderRequiredSwitch(model);
        var input = cut.Find("input[type=checkbox]");

        input.Blur();

        cut.Find(".nt-switch-error-text").TextContent.Should().Be("Enable alerts before submitting");
        input.GetAttribute("aria-required").Should().Be("true");
        input.HasAttribute("required").Should().BeFalse();
    }

    [Fact]
    public void Bound_Switch_Renders_And_Changes_After_Inherits_Cleanup() {
        var model = new TestModel();

        var cut = RenderSwitch(model, parameters => parameters.Add(p => p.Label, "Enabled"));

        cut.Instance.Should().BeAssignableTo<InputBase<bool>>();
        cut.Find("label.nt-switch-label").GetAttribute("for").Should().Be(cut.Find("input[type=checkbox]").GetAttribute("id"));

        cut.Find("input[type=checkbox]").Change(true);

        model.Enabled.Should().BeTrue();
    }

    private IRenderedComponent<NTInputSwitch> RenderSwitch(TestModel? model = null, Action<ComponentParameterCollectionBuilder<NTInputSwitch>>? configure = null) {
        model ??= new TestModel();
        return Render<NTInputSwitch>(parameters => {
            parameters.Add(p => p.Value, model.Enabled);
            parameters.Add(p => p.ValueChanged, EventCallback.Factory.Create<bool>(this, value => model.Enabled = value));
            parameters.Add(p => p.ValueExpression, () => model.Enabled);
            configure?.Invoke(parameters);
        });
    }

    private IRenderedComponent<EditForm> RenderRequiredSwitch(TestModel model) {
        return Render<EditForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.ChildContent, (EditContext _) => builder => {
                builder.OpenComponent<DataAnnotationsValidator>(0);
                builder.CloseComponent();
                builder.OpenComponent<NTInputSwitch>(1);
                builder.AddAttribute(2, nameof(NTInputSwitch.Value), model.Enabled);
                builder.AddAttribute(3, nameof(NTInputSwitch.ValueChanged), EventCallback.Factory.Create<bool>(this, value => model.Enabled = value));
                builder.AddAttribute(4, nameof(NTInputSwitch.ValueExpression), (Expression<Func<bool>>)(() => model.Enabled));
                builder.AddAttribute(5, nameof(NTInputSwitch.Label), "Alerts");
                builder.AddAttribute(6, nameof(NTInputSwitch.Required), true);
                builder.AddAttribute(7, nameof(NTInputSwitch.RequiredErrorText), "Enable alerts before submitting");
                builder.CloseComponent();
            }));
    }

    private static string[] GetIdReferences(string? value) => value?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? [];
}
