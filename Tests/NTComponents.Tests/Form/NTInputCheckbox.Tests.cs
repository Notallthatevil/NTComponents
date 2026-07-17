using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;

namespace NTComponents.Tests.Form;

public class NTInputCheckbox_Tests : BunitContext {
    private sealed class RequiredModel {
        [Range(typeof(bool), "true", "true", ErrorMessage = "You must agree")]
        public bool Agreed { get; set; }
    }

    private sealed class TestModel {
        public bool Enabled { get; set; }
    }

    [Fact]
    public void Renders_Label_And_Input_Association() {
        var model = new TestModel();

        var cut = RenderCheckbox(model, parameters => parameters
            .Add(p => p.ElementId, "email-opt-in")
            .Add(p => p.Label, "Email updates")
            .Add(p => p.SupportingText, "Occasional product updates"));

        var input = cut.Find("input[type=checkbox]");
        var label = cut.Find("label.nt-checkbox-label");

        input.GetAttribute("id").Should().Be("email-opt-in");
        label.GetAttribute("for").Should().Be("email-opt-in");
        input.GetAttribute("aria-describedby").Should().Contain("email-opt-in-supporting");
        cut.Find("#email-opt-in-supporting").TextContent.Should().Be("Occasional product updates");
    }

    [Fact]
    public void Omits_DescribedBy_When_No_Description_Is_Rendered() {
        var cut = RenderCheckbox();

        cut.Find("input[type=checkbox]").HasAttribute("aria-describedby").Should().BeFalse();
    }

    [Fact]
    public void Merges_External_DescribedBy_With_Internal_Description() {
        var cut = RenderCheckbox(configure: parameters => parameters
            .Add(p => p.ElementId, "email-opt-in")
            .Add(p => p.SupportingText, "Occasional product updates")
            .Add(p => p.AdditionalAttributes, new Dictionary<string, object> {
                ["aria-describedby"] = "external-help"
            }));

        GetIdReferences(cut.Find("input[type=checkbox]").GetAttribute("aria-describedby")).Should().BeEquivalentTo("external-help", "email-opt-in-supporting");
    }

    [Fact]
    public void Merges_External_DescribedBy_With_Internal_Error() {
        var cut = RenderCheckbox(configure: parameters => parameters
            .Add(p => p.ElementId, "terms")
            .Add(p => p.ErrorText, "Accept terms before submitting")
            .Add(p => p.AdditionalAttributes, new Dictionary<string, object> {
                ["aria-describedby"] = "legal-copy"
            }));

        GetIdReferences(cut.Find("input[type=checkbox]").GetAttribute("aria-describedby")).Should().BeEquivalentTo("legal-copy", "terms-error");
    }

    [Fact]
    public void Generated_Input_Id_Is_Deterministic_For_Field() {
        var model = new TestModel();

        using var first = RenderCheckbox(model, parameters => parameters.Add(p => p.Label, "Enabled"));
        var firstInputId = first.Find("input[type=checkbox]").GetAttribute("id");

        using var second = RenderCheckbox(model, parameters => parameters.Add(p => p.Label, "Enabled"));

        firstInputId.Should().NotBeNullOrWhiteSpace();
        firstInputId.Should().StartWith("nt-checkbox-model-enabled-");
        second.Find("input[type=checkbox]").GetAttribute("id").Should().Be(firstInputId);
    }

    [Fact]
    public void Change_Updates_Bound_Value_And_Invokes_BindAfter() {
        var model = new TestModel();
        var callbackValue = false;

        var cut = RenderCheckbox(model, parameters => parameters.Add(p => p.BindAfter, EventCallback.Factory.Create<bool>(this, value => callbackValue = value)));

        cut.Find("input[type=checkbox]").Change(true);

        model.Enabled.Should().BeTrue();
        callbackValue.Should().BeTrue();
    }

    [Fact]
    public void Indeterminate_Renders_Mixed_Accessibility_And_Class() {
        var cut = RenderCheckbox(configure: parameters => parameters
            .Add(p => p.Label, "Select all")
            .Add(p => p.Indeterminate, true));

        cut.Find(".nt-checkbox").GetAttribute("class").Should().Contain("nt-checkbox-indeterminate");
        cut.Find("input[type=checkbox]").GetAttribute("aria-checked").Should().Be("mixed");
        cut.Find("input[type=checkbox]").GetAttribute("data-indeterminate").Should().Be("true");
    }

    [Fact]
    public void Leading_And_Trailing_Icons_Render_When_Set() {
        var cut = RenderCheckbox(configure: parameters => parameters
            .Add(p => p.Label, "Alerts")
            .Add(p => p.LeadingIcon, MaterialIcon.Notifications)
            .Add(p => p.TrailingIcon, MaterialIcon.Info));

        cut.Find(".nt-checkbox").GetAttribute("class").Should().Contain("nt-checkbox-has-leading-icon").And.Contain("nt-checkbox-has-trailing-icon");
        cut.Find(".nt-checkbox-leading").TextContent.Should().Contain("notifications");
        cut.Find(".nt-checkbox-trailing").TextContent.Should().Contain("info");
    }

    [Fact]
    public void Trailing_Variant_Renders_Trailing_Control_Class() {
        var cut = RenderCheckbox(configure: parameters => parameters
            .Add(p => p.Label, "Email updates")
            .Add(p => p.Variant, NTInputCheckboxVariant.Trailing));

        var rootClass = cut.Find(".nt-checkbox").GetAttribute("class");

        rootClass.Should().Contain("nt-checkbox-trailing-control");
        rootClass.Should().NotContain("nt-checkbox-leading-control");
    }

    [Fact]
    public void Disabled_Comes_From_Form_And_Removes_Form_Post_Fallback() {
        var model = new TestModel();

        var cut = Render<NTForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Disabled, true)
            .Add(p => p.ChildContent, (EditContext _) => builder => {
                builder.OpenComponent<NTInputCheckbox>(0);
                builder.AddAttribute(1, nameof(NTInputCheckbox.Value), model.Enabled);
                builder.AddAttribute(2, nameof(NTInputCheckbox.ValueChanged), EventCallback.Factory.Create<bool>(this, value => model.Enabled = value));
                builder.AddAttribute(3, nameof(NTInputCheckbox.ValueExpression), (Expression<Func<bool>>)(() => model.Enabled));
                builder.AddAttribute(4, nameof(NTInputCheckbox.Label), "Enabled");
                builder.CloseComponent();
            }));

        cut.Find(".nt-checkbox").GetAttribute("class").Should().Contain("nt-checkbox-disabled");
        cut.Find("input[type=checkbox]").HasAttribute("disabled").Should().BeTrue();
        cut.FindAll("input[type=hidden]").Should().BeEmpty();
    }

    [Fact]
    public void ReadOnly_Disables_Native_Checkbox_And_Posts_Current_Value() {
        SetRendererInfo(new RendererInfo("Static", false));
        var model = new TestModel { Enabled = true };

        var cut = RenderCheckbox(model, parameters => parameters.Add(p => p.ReadOnly, true));
        var checkbox = cut.Find("input[type=checkbox]");
        var canPostEditableCheckboxValue = !checkbox.HasAttribute("disabled") && !string.IsNullOrWhiteSpace(checkbox.GetAttribute("name"));

        checkbox.HasAttribute("disabled").Should().BeTrue();
        canPostEditableCheckboxValue.Should().BeFalse();
        cut.Find(".nt-checkbox").GetAttribute("class").Should().Contain("nt-checkbox-readonly");

        var hiddenInput = cut.FindAll("input[type=hidden]").Should().ContainSingle().Which;
        hiddenInput.GetAttribute("name").Should().NotBeNullOrWhiteSpace();
        hiddenInput.GetAttribute("value").Should().Be("true");
    }

    [Fact]
    public void ReadOnly_Posts_False_When_Current_Value_Is_False() {
        SetRendererInfo(new RendererInfo("Static", false));
        var model = new TestModel();

        var cut = RenderCheckbox(model, parameters => parameters.Add(p => p.ReadOnly, true));
        var hiddenInput = cut.FindAll("input[type=hidden]").Should().ContainSingle().Which;

        cut.Find("input[type=checkbox]").HasAttribute("disabled").Should().BeTrue();
        hiddenInput.GetAttribute("value").Should().Be("false");
    }

    [Fact]
    public void Renders_Hidden_False_Form_Post_Fallback_After_Checkbox() {
        var model = new TestModel();

        var cut = RenderCheckbox(model);
        var inputs = cut.FindAll("input");

        inputs.Should().HaveCount(2);
        inputs[0].GetAttribute("type").Should().Be("checkbox");
        inputs[1].GetAttribute("type").Should().Be("hidden");
        inputs[1].GetAttribute("name").Should().Be(inputs[0].GetAttribute("name"));
        inputs[1].GetAttribute("value").Should().Be("false");
    }

    [Fact]
    public void Additional_Attributes_Do_Not_Override_Owned_Input_Attributes() {
        var cut = RenderCheckbox(configure: parameters => parameters.Add(p => p.AdditionalAttributes, new Dictionary<string, object> {
            ["class"] = "external-class",
            ["type"] = "text",
            ["name"] = "posted.enabled",
            ["data-field"] = "enabled"
        }));

        var input = cut.Find("input[type=checkbox]");

        input.GetAttribute("type").Should().Be("checkbox");
        input.GetAttribute("name").Should().Be("posted.enabled");
        input.GetAttribute("data-field").Should().Be("enabled");
        input.GetAttribute("class").Should().BeNull();
    }

    [Fact]
    public void Color_Overrides_Emit_Component_Css_Variables() {
        var cut = RenderCheckbox(configure: parameters => parameters
            .Add(p => p.DisabledContainerColor, TnTColor.SurfaceContainerLow)
            .Add(p => p.DisabledIconColor, TnTColor.Surface)
            .Add(p => p.DisabledOutlineColor, TnTColor.OutlineVariant)
            .Add(p => p.ErrorColor, TnTColor.Error)
            .Add(p => p.ErrorIconColor, TnTColor.OnError)
            .Add(p => p.FocusOutlineColor, TnTColor.Primary)
            .Add(p => p.HoverOutlineColor, TnTColor.OnSurface)
            .Add(p => p.IconColor, TnTColor.Tertiary)
            .Add(p => p.LabelColor, TnTColor.Secondary)
            .Add(p => p.OutlineColor, TnTColor.Outline)
            .Add(p => p.PressedOutlineColor, TnTColor.Tertiary)
            .Add(p => p.SelectedContainerColor, TnTColor.Primary)
            .Add(p => p.SelectedIconColor, TnTColor.OnPrimary)
            .Add(p => p.SelectedStateLayerColor, TnTColor.Primary)
            .Add(p => p.StateLayerColor, TnTColor.OnSurface)
            .Add(p => p.SupportingTextColor, TnTColor.OnSurfaceVariant));

        var style = cut.Find(".nt-checkbox").GetAttribute("style");

        style.Should().Contain("--nt-checkbox-disabled-container-color:var(--tnt-color-surface-container-low);");
        style.Should().Contain("--nt-checkbox-disabled-icon-color:var(--tnt-color-surface);");
        style.Should().Contain("--nt-checkbox-disabled-outline-color:var(--tnt-color-outline-variant);");
        style.Should().Contain("--nt-checkbox-error-color:var(--tnt-color-error);");
        style.Should().Contain("--nt-checkbox-error-icon-color:var(--tnt-color-on-error);");
        style.Should().Contain("--nt-checkbox-focus-outline-color:var(--tnt-color-primary);");
        style.Should().Contain("--nt-checkbox-hover-outline-color:var(--tnt-color-on-surface);");
        style.Should().Contain("--nt-checkbox-icon-color:var(--tnt-color-tertiary);");
        style.Should().Contain("--nt-checkbox-label-color:var(--tnt-color-secondary);");
        style.Should().Contain("--nt-checkbox-outline-color:var(--tnt-color-outline);");
        style.Should().Contain("--nt-checkbox-pressed-outline-color:var(--tnt-color-tertiary);");
        style.Should().Contain("--nt-checkbox-selected-container-color:var(--tnt-color-primary);");
        style.Should().Contain("--nt-checkbox-selected-icon-color:var(--tnt-color-on-primary);");
        style.Should().Contain("--nt-checkbox-selected-state-layer-color:var(--tnt-color-primary);");
        style.Should().Contain("--nt-checkbox-state-layer-color:var(--tnt-color-on-surface);");
        style.Should().Contain("--nt-checkbox-supporting-text-color:var(--tnt-color-on-surface-variant);");
    }

    [Fact]
    public void Color_Override_Style_Updates_When_Parameters_Change() {
        var model = new TestModel();

        var cut = RenderCheckbox(model, parameters => parameters
            .Add(p => p.SelectedContainerColor, TnTColor.Primary)
            .Add(p => p.SelectedIconColor, TnTColor.OnPrimary));

        cut.Find(".nt-checkbox").GetAttribute("style").Should().Contain("--nt-checkbox-selected-container-color:var(--tnt-color-primary);");

        cut.Render(parameters => {
            parameters.Add(p => p.Value, model.Enabled);
            parameters.Add(p => p.ValueChanged, EventCallback.Factory.Create<bool>(this, value => model.Enabled = value));
            parameters.Add(p => p.ValueExpression, () => model.Enabled);
            parameters.Add(p => p.SelectedContainerColor, TnTColor.Secondary);
            parameters.Add(p => p.SelectedIconColor, TnTColor.OnSecondary);
        });

        var style = cut.Find(".nt-checkbox").GetAttribute("style");
        style.Should().Contain("--nt-checkbox-selected-container-color:var(--tnt-color-secondary);");
        style.Should().Contain("--nt-checkbox-selected-icon-color:var(--tnt-color-on-secondary);");
        style.Should().NotContain("--nt-checkbox-selected-container-color:var(--tnt-color-primary);");
    }

    [Fact]
    public void Color_Override_Style_Clears_When_Parameter_Is_Null() {
        var model = new TestModel();

        var cut = RenderCheckbox(model, parameters => parameters.Add(p => p.SelectedContainerColor, TnTColor.Primary));

        cut.Find(".nt-checkbox").GetAttribute("style").Should().Contain("--nt-checkbox-selected-container-color:var(--tnt-color-primary);");

        cut.Render(parameters => {
            parameters.Add(p => p.Value, model.Enabled);
            parameters.Add(p => p.ValueChanged, EventCallback.Factory.Create<bool>(this, value => model.Enabled = value));
            parameters.Add(p => p.ValueExpression, () => model.Enabled);
            parameters.Add(p => p.SelectedContainerColor, (TnTColor?)null);
        });

        cut.Find(".nt-checkbox").HasAttribute("style").Should().BeFalse();
    }

    [Fact]
    public void Validation_Error_Uses_Error_Accessibility() {
        var model = new RequiredModel();

        var cut = Render<EditForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.ChildContent, (EditContext _) => builder => {
                builder.OpenComponent<DataAnnotationsValidator>(0);
                builder.CloseComponent();
                builder.OpenComponent<NTInputCheckbox>(1);
                builder.AddAttribute(2, nameof(NTInputCheckbox.Value), model.Agreed);
                builder.AddAttribute(3, nameof(NTInputCheckbox.ValueChanged), EventCallback.Factory.Create<bool>(this, value => model.Agreed = value));
                builder.AddAttribute(4, nameof(NTInputCheckbox.ValueExpression), (Expression<Func<bool>>)(() => model.Agreed));
                builder.AddAttribute(5, nameof(NTInputCheckbox.Label), "Agree");
                builder.CloseComponent();
            }));

        cut.Find("form").Submit();

        var input = cut.Find("input[type=checkbox]");
        var checkboxClass = cut.Find(".nt-checkbox").GetAttribute("class");
        checkboxClass.Should().Contain("nt-invalid");
        checkboxClass.Should().Contain("nt-modified");
        cut.Find(".nt-checkbox-error-text").TextContent.Should().Be("You must agree");
        input.GetAttribute("aria-invalid").Should().Be("true");
        input.GetAttribute("aria-errormessage").Should().EndWith("-error");
    }

    [Fact]
    public void Required_Parameter_Validates_On_Submit() {
        var model = new TestModel();

        var cut = RenderRequiredCheckbox(model);

        cut.Find("form").Submit();

        var input = cut.Find("input[type=checkbox]");
        var checkboxClass = cut.Find(".nt-checkbox").GetAttribute("class");
        checkboxClass.Should().Contain("nt-invalid");
        checkboxClass.Should().Contain("nt-modified");
        cut.Find(".nt-checkbox-error-text").TextContent.Should().Be("Accept terms before submitting");
        input.GetAttribute("aria-invalid").Should().Be("true");
        input.GetAttribute("aria-errormessage").Should().EndWith("-error");
        input.GetAttribute("aria-required").Should().Be("true");
        input.HasAttribute("required").Should().BeFalse();
    }

    [Fact]
    public void Required_Parameter_Clears_Component_Validation_On_Dispose() {
        var model = new TestModel();
        var editContext = new EditContext(model);
        var showCheckbox = true;

        var cut = Render<EditForm>(parameters => parameters
            .Add(p => p.EditContext, editContext)
            .Add(p => p.ChildContent, (EditContext _) => builder => {
                if (!showCheckbox) {
                    return;
                }

                builder.OpenComponent<NTInputCheckbox>(0);
                builder.AddAttribute(1, nameof(NTInputCheckbox.Value), model.Enabled);
                builder.AddAttribute(2, nameof(NTInputCheckbox.ValueChanged), EventCallback.Factory.Create<bool>(this, value => model.Enabled = value));
                builder.AddAttribute(3, nameof(NTInputCheckbox.ValueExpression), (Expression<Func<bool>>)(() => model.Enabled));
                builder.AddAttribute(4, nameof(NTInputCheckbox.Label), "Accept terms");
                builder.AddAttribute(5, nameof(NTInputCheckbox.Required), true);
                builder.AddAttribute(6, nameof(NTInputCheckbox.RequiredErrorText), "Accept terms before submitting");
                builder.CloseComponent();
            }));

        cut.Find("form").Submit();
        editContext.GetValidationMessages().Should().Contain("Accept terms before submitting");

        showCheckbox = false;
        cut.Render();

        editContext.GetValidationMessages().Should().BeEmpty();
    }

    [Fact]
    public void Required_Parameter_Revalidates_When_Checked_And_Unchecked() {
        var model = new TestModel();
        var cut = RenderRequiredCheckbox(model);
        var input = cut.Find("input[type=checkbox]");

        cut.Find("form").Submit();
        cut.FindAll(".nt-checkbox-error-text").Should().HaveCount(1);

        input.Change(true);
        cut.FindAll(".nt-checkbox-error-text").Should().BeEmpty();
        cut.Find(".nt-checkbox").GetAttribute("class").Should().NotContain("nt-invalid");

        input.Change(false);
        cut.Find(".nt-checkbox-error-text").TextContent.Should().Be("Accept terms before submitting");
        cut.Find(".nt-checkbox").GetAttribute("class").Should().Contain("nt-invalid");
    }

    [Fact]
    public void Bound_Checkbox_Renders_And_Changes_After_Inherits_Cleanup() {
        var model = new TestModel();

        var cut = RenderCheckbox(model, parameters => parameters.Add(p => p.Label, "Enabled"));

        cut.Instance.Should().BeAssignableTo<InputBase<bool>>();
        cut.Find("label.nt-checkbox-label").GetAttribute("for").Should().Be(cut.Find("input[type=checkbox]").GetAttribute("id"));

        cut.Find("input[type=checkbox]").Change(true);

        model.Enabled.Should().BeTrue();
    }

    private IRenderedComponent<NTInputCheckbox> RenderCheckbox(TestModel? model = null, Action<ComponentParameterCollectionBuilder<NTInputCheckbox>>? configure = null) {
        model ??= new TestModel();
        return Render<NTInputCheckbox>(parameters => {
            parameters.Add(p => p.Value, model.Enabled);
            parameters.Add(p => p.ValueChanged, EventCallback.Factory.Create<bool>(this, value => model.Enabled = value));
            parameters.Add(p => p.ValueExpression, () => model.Enabled);
            configure?.Invoke(parameters);
        });
    }

    private IRenderedComponent<EditForm> RenderRequiredCheckbox(TestModel model) {
        return Render<EditForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.ChildContent, (EditContext _) => builder => {
                builder.OpenComponent<DataAnnotationsValidator>(0);
                builder.CloseComponent();
                builder.OpenComponent<NTInputCheckbox>(1);
                builder.AddAttribute(2, nameof(NTInputCheckbox.Value), model.Enabled);
                builder.AddAttribute(3, nameof(NTInputCheckbox.ValueChanged), EventCallback.Factory.Create<bool>(this, value => model.Enabled = value));
                builder.AddAttribute(4, nameof(NTInputCheckbox.ValueExpression), (Expression<Func<bool>>)(() => model.Enabled));
                builder.AddAttribute(5, nameof(NTInputCheckbox.Label), "Accept terms");
                builder.AddAttribute(6, nameof(NTInputCheckbox.Required), true);
                builder.AddAttribute(7, nameof(NTInputCheckbox.RequiredErrorText), "Accept terms before submitting");
                builder.CloseComponent();
            }));
    }

    private static string[] GetIdReferences(string? value) => value?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? [];
}
