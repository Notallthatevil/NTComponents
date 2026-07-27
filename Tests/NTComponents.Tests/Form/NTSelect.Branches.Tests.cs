using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.Linq.Expressions;

namespace NTComponents.Tests.Form;

public class NTSelect_Branches_Tests : BunitContext {

    [Fact]
    public void InvalidBoolean_LeavesValueAndRendersFieldValidationError() {
        var model = new BranchModel { Enabled = true };
        var cut = RenderWithEditContext(model, builder => {
            builder.OpenComponent<NTSelect<bool>>(0);
            builder.AddAttribute(1, nameof(NTSelect<bool>.ValueExpression), (Expression<Func<bool>>)(() => model.Enabled));
            builder.AddAttribute(2, nameof(NTSelect<bool>.Value), model.Enabled);
            builder.AddAttribute(3, nameof(NTSelect<bool>.ValueChanged), EventCallback.Factory.Create<bool>(this, value => model.Enabled = value));
            builder.AddAttribute(4, nameof(NTSelect<bool>.ChildContent), (RenderFragment)(options => options.AddMarkupContent(0, "<option value=\"true\">Yes</option>")));
            builder.CloseComponent();
        });

        cut.Find("select").Change("not-a-boolean");

        model.Enabled.Should().BeTrue();
        cut.Find(".nt-input-error-text").TextContent.Should().Be("The Enabled field is not valid.");
    }

    [Fact]
    public void NullableBoolean_WithValidText_UpdatesValue() {
        var model = new BranchModel();
        var cut = Render<NTSelect<bool?>>(parameters => parameters
            .Add(component => component.ValueExpression, () => model.OptionalEnabled)
            .Add(component => component.Value, model.OptionalEnabled)
            .Add(component => component.ValueChanged, EventCallback.Factory.Create<bool?>(this, value => model.OptionalEnabled = value))
            .AddChildContent("<option value=\"false\">No</option>"));

        cut.Find("select").Change("false");

        model.OptionalEnabled.Should().BeFalse();
    }

    [Fact]
    public void NonNullableBoolean_WithEmptyText_LeavesValueAndRendersValidationError() {
        var model = new BranchModel { Enabled = true };
        var cut = RenderWithEditContext(model, builder => {
            builder.OpenComponent<NTSelect<bool>>(0);
            builder.AddAttribute(1, nameof(NTSelect<bool>.ValueExpression), (Expression<Func<bool>>)(() => model.Enabled));
            builder.AddAttribute(2, nameof(NTSelect<bool>.Value), model.Enabled);
            builder.AddAttribute(3, nameof(NTSelect<bool>.ValueChanged), EventCallback.Factory.Create<bool>(this, value => model.Enabled = value));
            builder.AddAttribute(4, nameof(NTSelect<bool>.ChildContent), (RenderFragment)(options => options.AddMarkupContent(0, "<option value=\"\">Unset</option>")));
            builder.CloseComponent();
        });

        cut.Find("select").Change(string.Empty);

        model.Enabled.Should().BeTrue();
        cut.Find(".nt-input-error-text").TextContent.Should().Be("The Enabled field is not valid.");
    }

    [Fact]
    public void InvalidEnum_LeavesValueAndRendersDisplayNameValidationError() {
        var model = new BranchModel { Mode = BranchMode.Alpha };
        var cut = RenderWithEditContext(model, builder => {
            builder.OpenComponent<NTSelect<BranchMode>>(0);
            builder.AddAttribute(1, nameof(NTSelect<BranchMode>.ValueExpression), (Expression<Func<BranchMode>>)(() => model.Mode));
            builder.AddAttribute(2, nameof(NTSelect<BranchMode>.Value), model.Mode);
            builder.AddAttribute(3, nameof(NTSelect<BranchMode>.ValueChanged), EventCallback.Factory.Create<BranchMode>(this, value => model.Mode = value));
            builder.AddAttribute(4, nameof(NTSelect<BranchMode>.DisplayName), "Operating mode");
            builder.AddAttribute(5, nameof(NTSelect<BranchMode>.ChildContent), (RenderFragment)(options => options.AddMarkupContent(0, "<option value=\"Alpha\">Alpha</option>")));
            builder.CloseComponent();
        });

        cut.Find("select").Change("missing");

        model.Mode.Should().Be(BranchMode.Alpha);
        cut.Find(".nt-input-error-text").TextContent.Should().Be("The Operating mode field is not valid.");
    }

    [Fact]
    public void NullableEnum_WithCaseInsensitiveValue_UpdatesValue() {
        var model = new BranchModel();
        var cut = Render<NTSelect<BranchMode?>>(parameters => parameters
            .Add(component => component.ValueExpression, () => model.OptionalMode)
            .Add(component => component.Value, model.OptionalMode)
            .Add(component => component.ValueChanged, EventCallback.Factory.Create<BranchMode?>(this, value => model.OptionalMode = value))
            .AddChildContent("<option value=\"beta\">Beta</option>"));

        cut.Find("select").Change("beta");

        model.OptionalMode.Should().Be(BranchMode.Beta);
    }

    [Fact]
    public void NonNullableEnum_WithEmptyText_LeavesValueAndRendersValidationError() {
        var model = new BranchModel { Mode = BranchMode.Beta };
        var cut = RenderWithEditContext(model, builder => {
            builder.OpenComponent<NTSelect<BranchMode>>(0);
            builder.AddAttribute(1, nameof(NTSelect<BranchMode>.ValueExpression), (Expression<Func<BranchMode>>)(() => model.Mode));
            builder.AddAttribute(2, nameof(NTSelect<BranchMode>.Value), model.Mode);
            builder.AddAttribute(3, nameof(NTSelect<BranchMode>.ValueChanged), EventCallback.Factory.Create<BranchMode>(this, value => model.Mode = value));
            builder.AddAttribute(4, nameof(NTSelect<BranchMode>.ChildContent), (RenderFragment)(options => options.AddMarkupContent(0, "<option value=\"\">Unset</option>")));
            builder.CloseComponent();
        });

        cut.Find("select").Change(string.Empty);

        model.Mode.Should().Be(BranchMode.Beta);
        cut.Find(".nt-input-error-text").TextContent.Should().Be("The Mode field is not valid.");
    }

    [Fact]
    public void InvalidNumber_LeavesValueAndRendersFieldValidationError() {
        var model = new BranchModel { Number = 7 };
        var cut = RenderWithEditContext(model, builder => {
            builder.OpenComponent<NTSelect<int>>(0);
            builder.AddAttribute(1, nameof(NTSelect<int>.ValueExpression), (Expression<Func<int>>)(() => model.Number));
            builder.AddAttribute(2, nameof(NTSelect<int>.Value), model.Number);
            builder.AddAttribute(3, nameof(NTSelect<int>.ValueChanged), EventCallback.Factory.Create<int>(this, value => model.Number = value));
            builder.AddAttribute(4, nameof(NTSelect<int>.ChildContent), (RenderFragment)(options => options.AddMarkupContent(0, "<option value=\"7\">Seven</option>")));
            builder.CloseComponent();
        });

        cut.Find("select").Change("not-a-number");

        model.Number.Should().Be(7);
        cut.Find(".nt-input-error-text").TextContent.Should().Be("The Number field is not valid.");
    }

    [Fact]
    public void DisabledSelect_IgnoresChangeAndBindAfter() {
        var model = new BranchModel { Status = "alpha" };
        string? boundValue = null;
        var cut = RenderStringSelect(model, parameters => parameters
            .Add(component => component.Disabled, true)
            .Add(component => component.BindAfter, EventCallback.Factory.Create<string?>(this, value => boundValue = value)));

        cut.Find("select").Change("beta");

        model.Status.Should().Be("alpha");
        boundValue.Should().BeNull();
    }

    [Fact]
    public void ReadOnlySelect_IgnoresChangeAndBindAfter() {
        var model = new BranchModel { Status = "alpha" };
        string? boundValue = null;
        var cut = RenderStringSelect(model, parameters => parameters
            .Add(component => component.ReadOnly, true)
            .Add(component => component.BindAfter, EventCallback.Factory.Create<string?>(this, value => boundValue = value)));

        cut.Find("select").Change("beta");

        model.Status.Should().Be("alpha");
        boundValue.Should().BeNull();
    }

    [Fact]
    public void NullChangeValue_ClearsNullableStringSelection() {
        var model = new BranchModel { Status = "alpha" };
        var cut = RenderStringSelect(model);

        cut.Find("select").TriggerEvent("onchange", new ChangeEventArgs { Value = null });

        model.Status.Should().BeNull();
    }

    [Fact]
    public void AdditionalClass_IsMergedIntoNativeSelectClass() {
        var cut = RenderStringSelect(configure: parameters => parameters.Add(component => component.AdditionalAttributes, new Dictionary<string, object> { ["class"] = "caller-select" }));

        var classes = cut.Find("select").GetAttribute("class");
        classes.Should().Contain("nt-input-control").And.Contain("nt-select-control").And.Contain("caller-select");
    }

    [Fact]
    public void CustomTrailingIcon_ReplacesDefaultIndicatorIcon() {
        var cut = RenderStringSelect(configure: parameters => parameters.Add(component => component.TrailingIcon, MaterialIcon.Search));

        cut.Find(".nt-select-indicator").TextContent.Should().Contain("search");
        cut.Markup.Should().NotContain("arrow_drop_down");
    }

    private IRenderedComponent<CascadingValue<EditContext>> RenderWithEditContext(BranchModel model, RenderFragment childContent) {
        var editContext = new EditContext(model);
        return Render<CascadingValue<EditContext>>(parameters => parameters
            .Add(component => component.Value, editContext)
            .Add(component => component.ChildContent, childContent));
    }

    private IRenderedComponent<NTSelect<string?>> RenderStringSelect(BranchModel? model = null, Action<ComponentParameterCollectionBuilder<NTSelect<string?>>>? configure = null) {
        model ??= new BranchModel();
        return Render<NTSelect<string?>>(parameters => {
            parameters
                .Add(component => component.ValueExpression, () => model.Status)
                .Add(component => component.Value, model.Status)
                .Add(component => component.ValueChanged, EventCallback.Factory.Create<string?>(this, value => model.Status = value))
                .AddChildContent("<option value=\"alpha\">Alpha</option><option value=\"beta\">Beta</option>");
            configure?.Invoke(parameters);
        });
    }

    private sealed class BranchModel {
        public string? Status { get; set; }
        public bool Enabled { get; set; }
        public bool? OptionalEnabled { get; set; }
        public BranchMode Mode { get; set; }
        public BranchMode? OptionalMode { get; set; }
        public int Number { get; set; }
    }

    private enum BranchMode {
        Alpha,
        Beta
    }
}
