using Bunit.JSInterop;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System.Linq.Expressions;

namespace NTComponents.Tests.Form;

public class NTInputSelect_Branches_Tests : BunitContext {
    private const string JsModulePath = "./_content/NTComponents/Form/NTInputSelect.razor.js";

    public NTInputSelect_Branches_Tests() {
        SetRendererInfo(new RendererInfo("WebAssembly", true));
        SetupModule(JSInterop);
    }

    [Fact]
    public void AdditionalAttributes_OnlyForwardNonOwnedAttributesToSearchInput() {
        var attributes = new Dictionary<string, object> {
            ["class"] = "caller-class",
            ["style"] = "margin: 1px",
            ["name"] = "caller-name",
            ["value"] = "caller-value",
            ["type"] = "number",
            ["data-test"] = "forwarded",
            ["aria-label"] = "Search choices"
        };

        var cut = RenderStringSelect(configure: parameters => parameters.Add(component => component.AdditionalAttributes, attributes));

        var input = cut.Find(".tnt-input-select-search-input");
        input.GetAttribute("type").Should().Be("text");
        input.HasAttribute("name").Should().BeFalse();
        input.GetAttribute("class").Should().Be("tnt-input-select-search-input");
        input.GetAttribute("data-test").Should().Be("forwarded");
        input.GetAttribute("aria-label").Should().Be("Search choices");
    }

    [Theory]
    [InlineData(true, false, true)]
    [InlineData(false, true, true)]
    [InlineData(false, false, false)]
    public void Focus_WhenOpeningIsPrevented_LeavesDropdownClosed(bool disabled, bool readOnly, bool openOnFocus) {
        var cut = RenderStringSelect(configure: parameters => parameters
            .Add(component => component.Disabled, disabled)
            .Add(component => component.ReadOnly, readOnly)
            .Add(component => component.OpenOnFocus, openOnFocus));

        cut.Find(".tnt-input-select-search-input").Focus();

        cut.FindAll(".tnt-input-select-search-content").Should().BeEmpty();
    }

    [Fact]
    public async Task SetFocusAsync_FocusesSearchInput() {
        var cut = RenderStringSelect();

        await cut.InvokeAsync(() => cut.Instance.SetFocusAsync().AsTask());

        JSInterop.Invocations.Should().Contain(invocation => invocation.Identifier.Contains("focus", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void SelectedOptionLabelChange_UpdatesDisplayedTextAfterRender() {
        var model = new StringModel { Value = "alpha" };
        var label = "Alpha";
        RenderFragment options = builder => {
            builder.OpenComponent<NTInputSelectOption<string?>>(0);
            builder.AddAttribute(1, nameof(NTInputSelectOption<string?>.Value), "alpha");
            builder.AddAttribute(2, nameof(NTInputSelectOption<string?>.Label), label);
            builder.CloseComponent();
        };
        var cut = RenderStringSelect(model, options: options);

        label = "Renamed alpha";
        cut.Render();

        cut.Find(".tnt-input-select-search-input").GetAttribute("value").Should().Be("Renamed alpha");
        cut.Find("input[type=hidden]").GetAttribute("value").Should().Be("alpha");
    }

    [Fact]
    public void NullOptionLabel_RendersEmptyFallbackContent() {
        var model = new StringModel { Value = "empty-label" };
        RenderFragment options = builder => {
            builder.OpenComponent<NTInputSelectOption<string?>>(0);
            builder.AddAttribute(1, nameof(NTInputSelectOption<string?>.Value), "empty-label");
            builder.AddAttribute(2, nameof(NTInputSelectOption<string?>.Label), (string?)null);
            builder.CloseComponent();
        };
        var cut = RenderStringSelect(model, options: options);

        cut.Find(".tnt-input-select-search-input").Focus();

        cut.Find(".tnt-input-select-search-list-item").TextContent.Should().BeEmpty();
    }

    [Fact]
    public async Task ArrowDown_WhenOpen_MovesFocusAndWrapsToFirstOption() {
        var cut = RenderStringSelect();
        var input = cut.Find(".tnt-input-select-search-input");
        input.Focus();

        await input.KeyUpAsync(new KeyboardEventArgs { Key = "ArrowDown" });
        cut.Find(".tnt-focused").TextContent.Should().Contain("Beta");
        await input.KeyUpAsync(new KeyboardEventArgs { Key = "ArrowDown" });

        cut.Find(".tnt-focused").TextContent.Should().Contain("Alpha");
    }

    [Fact]
    public async Task ArrowUp_WhenClosed_OpensDropdownFocusedOnLastOption() {
        var cut = RenderStringSelect();

        await cut.Find(".tnt-input-select-search-input").KeyUpAsync(new KeyboardEventArgs { Key = "ArrowUp" });

        cut.Find(".tnt-focused").TextContent.Should().Contain("Beta");
    }

    [Fact]
    public async Task ArrowUp_WhenOpenAtFirstOption_WrapsToLastOption() {
        var cut = RenderStringSelect();
        var input = cut.Find(".tnt-input-select-search-input");
        input.Focus();

        await input.KeyUpAsync(new KeyboardEventArgs { Key = "ArrowUp" });

        cut.Find(".tnt-focused").TextContent.Should().Contain("Beta");
    }

    [Fact]
    public async Task ArrowUp_WhenOpenAfterMovingDown_MovesBackOneOption() {
        var cut = RenderStringSelect();
        var input = cut.Find(".tnt-input-select-search-input");
        input.Focus();
        await input.KeyUpAsync(new KeyboardEventArgs { Key = "ArrowDown" });

        await input.KeyUpAsync(new KeyboardEventArgs { Key = "ArrowUp" });

        cut.Find(".tnt-focused").TextContent.Should().Contain("Alpha");
    }

    [Fact]
    public async Task ArrowUp_WithNoOptions_OpensWithoutFocusingAnOption() {
        var model = new StringModel();
        var cut = Render<NTInputSelect<string?>>(parameters => parameters
            .Add(component => component.ValueExpression, () => model.Value)
            .Add(component => component.Value, model.Value)
            .Add(component => component.ValueChanged, EventCallback.Factory.Create<string?>(this, value => model.Value = value)));

        await cut.Find(".tnt-input-select-search-input").KeyUpAsync(new KeyboardEventArgs { Key = "ArrowUp" });

        cut.FindAll(".tnt-focused").Should().BeEmpty();
        cut.Find(".tnt-input-select-search-no-results").Should().NotBeNull();
    }

    [Fact]
    public async Task Escape_WithNoMatchingOptions_ClosesDropdown() {
        var cut = RenderStringSelect(configure: parameters => parameters.Add(component => component.NoResultsText, "Nothing matched"));
        var input = cut.Find(".tnt-input-select-search-input");
        input.Input("zzz");
        cut.Find(".tnt-input-select-search-no-results").TextContent.Should().Be("Nothing matched");

        await input.KeyUpAsync(new KeyboardEventArgs { Key = "Escape" });

        cut.FindAll(".tnt-input-select-search-content").Should().BeEmpty();
    }

    [Fact]
    public async Task Escape_WithOptions_ClosesDropdown() {
        var cut = RenderStringSelect();
        var input = cut.Find(".tnt-input-select-search-input");
        input.Focus();

        await input.KeyUpAsync(new KeyboardEventArgs { Key = "Escape" });

        cut.FindAll(".tnt-input-select-search-content").Should().BeEmpty();
    }

    [Fact]
    public async Task CloseDropdownFromJs_WhenOpen_ClosesAndCanBeRepeatedSafely() {
        var cut = RenderStringSelect();
        cut.Find(".tnt-input-select-search-input").Focus();

        await cut.InvokeAsync(cut.Instance.CloseDropdownFromJs);
        await cut.InvokeAsync(cut.Instance.CloseDropdownFromJs);

        cut.FindAll(".tnt-input-select-search-content").Should().BeEmpty();
    }

    [Fact]
    public void FreeformBoolean_WithValidText_UpdatesBoundValue() {
        var model = new BooleanModel();
        var cut = Render<NTInputSelect<bool>>(parameters => parameters
            .Add(component => component.ValueExpression, () => model.Value)
            .Add(component => component.Value, model.Value)
            .Add(component => component.ValueChanged, EventCallback.Factory.Create<bool>(this, value => model.Value = value))
            .Add(component => component.AllowFreeform, true));

        cut.Find(".tnt-input-select-search-input").Input("true");

        model.Value.Should().BeTrue();
        cut.Find("input[type=hidden]").GetAttribute("value").Should().Be("True");
    }

    [Fact]
    public void FreeformBoolean_WithMalformedText_RejectsValue() {
        var model = new BooleanModel { Value = true };
        var cut = Render<NTInputSelect<bool>>(parameters => parameters
            .Add(component => component.ValueExpression, () => model.Value)
            .Add(component => component.Value, model.Value)
            .Add(component => component.ValueChanged, EventCallback.Factory.Create<bool>(this, value => model.Value = value))
            .Add(component => component.AllowFreeform, true));

        cut.Find(".tnt-input-select-search-input").Input("not-a-boolean");

        model.Value.Should().BeTrue();
        cut.Find("input[type=hidden]").GetAttribute("value").Should().Be("True");
    }

    [Fact]
    public void FreeformNullableBoolean_WithValidText_UpdatesBoundValue() {
        var model = new NullableBooleanModel();
        var cut = Render<NTInputSelect<bool?>>(parameters => parameters
            .Add(component => component.ValueExpression, () => model.Value)
            .Add(component => component.Value, model.Value)
            .Add(component => component.ValueChanged, EventCallback.Factory.Create<bool?>(this, value => model.Value = value))
            .Add(component => component.AllowFreeform, true));

        cut.Find(".tnt-input-select-search-input").Input("false");

        model.Value.Should().BeFalse();
    }

    [Fact]
    public void FreeformNullableBoolean_WithEmptyText_ClearsBoundValue() {
        var model = new NullableBooleanModel { Value = true };
        var cut = Render<NTInputSelect<bool?>>(parameters => parameters
            .Add(component => component.ValueExpression, () => model.Value)
            .Add(component => component.Value, model.Value)
            .Add(component => component.ValueChanged, EventCallback.Factory.Create<bool?>(this, value => model.Value = value))
            .Add(component => component.AllowFreeform, true));

        cut.Find(".tnt-input-select-search-input").Input(string.Empty);

        model.Value.Should().BeNull();
    }

    [Fact]
    public void FreeformInteger_WithValidText_UpdatesBoundValue() {
        var model = new IntegerModel();
        var cut = Render<NTInputSelect<int>>(parameters => parameters
            .Add(component => component.ValueExpression, () => model.Value)
            .Add(component => component.Value, model.Value)
            .Add(component => component.ValueChanged, EventCallback.Factory.Create<int>(this, value => model.Value = value))
            .Add(component => component.AllowFreeform, true));

        cut.Find(".tnt-input-select-search-input").Input("42");

        model.Value.Should().Be(42);
    }

    [Fact]
    public void FreeformUnsupportedType_ThrowsTypeSpecificError() {
        var model = new UnsupportedModel { Value = [] };
        var cut = Render<NTInputSelect<Dictionary<string, string>>>(parameters => parameters
            .Add(component => component.ValueExpression, () => model.Value)
            .Add(component => component.Value, model.Value)
            .Add(component => component.ValueChanged, EventCallback.Factory.Create<Dictionary<string, string>>(this, value => model.Value = value))
            .Add(component => component.AllowFreeform, true));

        var act = () => cut.Find(".tnt-input-select-search-input").Input("unsupported");

        act.Should().Throw<InvalidOperationException>().WithMessage("*does not support the type*Dictionary*");
    }

    [Fact]
    public async Task SelectingOption_WithEditContext_NotifiesFieldChanged() {
        var model = new StringModel();
        var editContext = new EditContext(model);
        var changedFields = new List<FieldIdentifier>();
        editContext.OnFieldChanged += (_, args) => changedFields.Add(args.FieldIdentifier);
        var cut = Render<CascadingValue<EditContext>>(parameters => parameters
            .Add(component => component.Value, editContext)
            .Add(component => component.ChildContent, (RenderFragment)(builder => {
                builder.OpenComponent<NTInputSelect<string?>>(0);
                builder.AddAttribute(1, nameof(NTInputSelect<string?>.ValueExpression), (Expression<Func<string?>>)(() => model.Value));
                builder.AddAttribute(2, nameof(NTInputSelect<string?>.Value), model.Value);
                builder.AddAttribute(3, nameof(NTInputSelect<string?>.ValueChanged), EventCallback.Factory.Create<string?>(this, value => model.Value = value));
                builder.AddAttribute(4, nameof(NTInputSelect<string?>.ChildContent), StringOptions());
                builder.CloseComponent();
            })));
        var input = cut.Find(".tnt-input-select-search-input");

        await input.KeyUpAsync(new KeyboardEventArgs { Key = "ArrowDown" });
        await input.KeyUpAsync(new KeyboardEventArgs { Key = "Enter" });

        model.Value.Should().Be("alpha");
        changedFields.Should().NotBeEmpty().And.OnlyContain(field => field.FieldName == nameof(StringModel.Value));
    }

    [Fact]
    public void TypingWithEditContext_WhenSelectionNoLongerMatches_NotifiesFieldChanged() {
        var model = new StringModel { Value = "alpha" };
        var editContext = new EditContext(model);
        var changedFields = new List<FieldIdentifier>();
        editContext.OnFieldChanged += (_, args) => changedFields.Add(args.FieldIdentifier);
        var cut = Render<CascadingValue<EditContext>>(parameters => parameters
            .Add(component => component.Value, editContext)
            .Add(component => component.ChildContent, (RenderFragment)(builder => {
                builder.OpenComponent<NTInputSelect<string?>>(0);
                builder.AddAttribute(1, nameof(NTInputSelect<string?>.ValueExpression), (Expression<Func<string?>>)(() => model.Value));
                builder.AddAttribute(2, nameof(NTInputSelect<string?>.Value), model.Value);
                builder.AddAttribute(3, nameof(NTInputSelect<string?>.ValueChanged), EventCallback.Factory.Create<string?>(this, value => model.Value = value));
                builder.AddAttribute(4, nameof(NTInputSelect<string?>.ChildContent), StringOptions());
                builder.CloseComponent();
            })));

        cut.Find(".tnt-input-select-search-input").Input("unmatched");

        model.Value.Should().BeNull();
        changedFields.Should().NotBeEmpty().And.OnlyContain(field => field.FieldName == nameof(StringModel.Value));
    }

    [Fact]
    public void InitialUnknownSelection_WithEditContext_ClearsValueAndNotifiesFieldChanged() {
        var model = new StringModel { Value = "missing" };
        var editContext = new EditContext(model);
        var changedFields = new List<FieldIdentifier>();
        editContext.OnFieldChanged += (_, args) => changedFields.Add(args.FieldIdentifier);

        var cut = Render<CascadingValue<EditContext>>(parameters => parameters
            .Add(component => component.Value, editContext)
            .Add(component => component.ChildContent, (RenderFragment)(builder => {
                builder.OpenComponent<NTInputSelect<string?>>(0);
                builder.AddAttribute(1, nameof(NTInputSelect<string?>.ValueExpression), (Expression<Func<string?>>)(() => model.Value));
                builder.AddAttribute(2, nameof(NTInputSelect<string?>.Value), model.Value);
                builder.AddAttribute(3, nameof(NTInputSelect<string?>.ValueChanged), EventCallback.Factory.Create<string?>(this, value => model.Value = value));
                builder.AddAttribute(4, nameof(NTInputSelect<string?>.ChildContent), StringOptions());
                builder.CloseComponent();
            })));

        model.Value.Should().BeNull();
        cut.Find("input[type=hidden]").GetAttribute("value").Should().BeEmpty();
        changedFields.Should().NotBeEmpty().And.OnlyContain(field => field.FieldName == nameof(StringModel.Value));
    }

    [Fact]
    public async Task AscendingSort_SelectsLowestLabelFirst() {
        var model = new StringModel();
        var cut = RenderStringSelect(model, parameters => parameters
            .Add(component => component.SortSelector, option => option.Label)
            .Add(component => component.SortDirection, SortDirection.Ascending));
        var input = cut.Find(".tnt-input-select-search-input");

        await input.KeyUpAsync(new KeyboardEventArgs { Key = "ArrowDown" });
        await input.KeyUpAsync(new KeyboardEventArgs { Key = "Enter" });

        model.Value.Should().Be("alpha");
    }

    [Fact]
    public void FirstRender_WhenOnLoadDisconnects_DoesNotFailOrRunUpdate() {
        using var context = CreateContext(module => module.SetupVoid("onLoad", _ => true).SetException(new JSDisconnectedException("Disconnected")));
        var model = new StringModel();

        var cut = context.Render<NTInputSelect<string?>>(parameters => parameters
            .Add(component => component.ValueExpression, () => model.Value)
            .Add(component => component.Value, model.Value)
            .Add(component => component.ValueChanged, EventCallback.Factory.Create<string?>(this, value => model.Value = value)));

        cut.Instance.IsolatedJsModule.Should().NotBeNull();
        context.JSInterop.VerifyInvoke("onLoad", 1);
        context.JSInterop.VerifyNotInvoke("onUpdate");
    }

    [Fact]
    public async Task DisposeAsync_WhenOnDisposeDisconnects_ClearsManagedReferences() {
        using var context = CreateContext(module => module.SetupVoid("onDispose", _ => true).SetException(new JSDisconnectedException("Disconnected")));
        var model = new StringModel();
        var cut = context.Render<NTInputSelect<string?>>(parameters => parameters
            .Add(component => component.ValueExpression, () => model.Value)
            .Add(component => component.Value, model.Value)
            .Add(component => component.ValueChanged, EventCallback.Factory.Create<string?>(this, value => model.Value = value)));

        await cut.Instance.DisposeAsync();

        cut.Instance.IsolatedJsModule.Should().BeNull();
        cut.Instance.DotNetObjectRef.Should().BeNull();
        context.JSInterop.VerifyInvoke("onDispose", 1);
    }

    [Fact]
    public void Dispose_ReleasesDotNetObjectReference() {
        var input = new NTInputSelect<string?>();

        ((IDisposable)input).Dispose();
        ((IDisposable)input).Dispose();

        input.DotNetObjectRef.Should().BeNull();
    }

    [Fact]
    public async Task DisposeAsync_AfterSynchronousDispose_IsIdempotent() {
        var input = new NTInputSelect<string?>();
        ((IDisposable)input).Dispose();

        await input.DisposeAsync();

        input.IsolatedJsModule.Should().BeNull();
        input.DotNetObjectRef.Should().BeNull();
    }

    private static global::Bunit.BunitContext CreateContext(Action<BunitJSModuleInterop> configure) {
        var context = new global::Bunit.BunitContext();
        context.SetRendererInfo(new RendererInfo("WebAssembly", true));
        var module = SetupModule(context.JSInterop);
        configure(module);
        return context;
    }

    private static BunitJSModuleInterop SetupModule(BunitJSInterop jsInterop) {
        var module = jsInterop.SetupModule(JsModulePath);
        module.SetupVoid("onLoad", _ => true).SetVoidResult();
        module.SetupVoid("onUpdate", _ => true).SetVoidResult();
        module.SetupVoid("onDispose", _ => true).SetVoidResult();
        return module;
    }

    private IRenderedComponent<NTInputSelect<string?>> RenderStringSelect(StringModel? model = null, Action<ComponentParameterCollectionBuilder<NTInputSelect<string?>>>? configure = null, RenderFragment? options = null) {
        model ??= new StringModel();
        return Render<NTInputSelect<string?>>(parameters => {
            parameters
                .Add(component => component.ValueExpression, () => model.Value)
                .Add(component => component.Value, model.Value)
                .Add(component => component.ValueChanged, EventCallback.Factory.Create<string?>(this, value => model.Value = value))
                .Add(component => component.ChildContent, options ?? StringOptions());
            configure?.Invoke(parameters);
        });
    }

    private static RenderFragment StringOptions() => builder => {
        builder.OpenComponent<NTInputSelectOption<string?>>(0);
        builder.AddAttribute(1, nameof(NTInputSelectOption<string?>.Value), "alpha");
        builder.AddAttribute(2, nameof(NTInputSelectOption<string?>.Label), "Alpha");
        builder.CloseComponent();
        builder.OpenComponent<NTInputSelectOption<string?>>(3);
        builder.AddAttribute(4, nameof(NTInputSelectOption<string?>.Value), "beta");
        builder.AddAttribute(5, nameof(NTInputSelectOption<string?>.Label), "Beta");
        builder.CloseComponent();
    };

    private sealed class StringModel {
        public string? Value { get; set; }
    }

    private sealed class BooleanModel {
        public bool Value { get; set; }
    }

    private sealed class NullableBooleanModel {
        public bool? Value { get; set; }
    }

    private sealed class IntegerModel {
        public int Value { get; set; }
    }

    private sealed class UnsupportedModel {
        public Dictionary<string, string> Value { get; set; } = [];
    }
}
