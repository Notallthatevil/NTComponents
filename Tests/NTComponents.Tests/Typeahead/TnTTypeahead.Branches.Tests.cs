using Bunit.JSInterop;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.Linq.Expressions;

namespace NTComponents.Tests.Typeahead;

public class TnTTypeahead_Branches_Tests : BunitContext {
    private const string TypeaheadModulePath = "./_content/NTComponents/Typeahead/TnTTypeahead.razor.js";
    private readonly BunitJSModuleInterop _typeaheadModule;

    public TnTTypeahead_Branches_Tests() {
        SetRendererInfo(new RendererInfo("WebAssembly", true));
        var rippleModule = JSInterop.SetupModule("./_content/NTComponents/Core/TnTRippleEffect.razor.js");
        rippleModule.SetupVoid("onLoad", _ => true).SetVoidResult();
        rippleModule.SetupVoid("onUpdate", _ => true).SetVoidResult();
        rippleModule.SetupVoid("onDispose", _ => true).SetVoidResult();
        _typeaheadModule = JSInterop.SetupModule(TypeaheadModulePath);
        _typeaheadModule.SetupVoid("onLoad", _ => true).SetVoidResult();
        _typeaheadModule.SetupVoid("onUpdate", _ => true).SetVoidResult();
        _typeaheadModule.SetupVoid("onDispose", _ => true).SetVoidResult();
    }

    /// <summary>Behavior source: CloseDropdownFromJs documents that it closes visible dropdown content and resets its suggestion state.</summary>
    [Fact]
    public async Task CloseDropdownFromJs_WithVisibleResults_HidesResultsAndPreservesQuery() {
        var cut = RenderTypeahead((_, _) => Task.FromResult<IEnumerable<string>>(["Alpha", "Beta"]));
        cut.Find("input").Input("a");
        cut.WaitForAssertion(() => cut.FindAll(".tnt-typeahead-list-item").Should().HaveCount(2));

        await cut.InvokeAsync(cut.Instance.CloseDropdownFromJs);

        cut.FindAll(".tnt-typeahead-content").Should().BeEmpty();
        cut.Find("input").GetAttribute("value").Should().Be("a");
    }

    /// <summary>Behavior source: CloseDropdownFromJs documents that empty-result dropdown content is closed, and repeated closing is an idempotent no-op.</summary>
    [Fact]
    public async Task CloseDropdownFromJs_WithNoResults_IsIdempotent() {
        var cut = RenderTypeahead((_, _) => Task.FromResult<IEnumerable<string>>([]));
        cut.Find("input").Input("missing");
        cut.WaitForAssertion(() => cut.FindAll(".tnt-typeahead-no-results").Should().ContainSingle());

        await cut.InvokeAsync(cut.Instance.CloseDropdownFromJs);
        await cut.InvokeAsync(cut.Instance.CloseDropdownFromJs);

        cut.FindAll(".tnt-typeahead-content").Should().BeEmpty();
        cut.Find("input").GetAttribute("value").Should().Be("missing");
    }

    /// <summary>Behavior source: ErrorMessage is the configured validation error, and ValueExpression identifies the field whose EditContext messages control its visibility.</summary>
    [Fact]
    public async Task ValidationStateChanged_ForIdentifiedField_ShowsAndClearsConfiguredError() {
        var model = new FormModel();
        var editContext = new EditContext(model);
        var messages = new ValidationMessageStore(editContext);
        var field = FieldIdentifier.Create(() => model.Value);
        var host = RenderInEditContext(editContext, model, includeValueExpression: true);

        messages.Add(field, "Required");
        await host.InvokeAsync(editContext.NotifyValidationStateChanged);
        host.WaitForAssertion(() => host.Markup.Should().Contain("Choose a value"));

        messages.Clear(field);
        await host.InvokeAsync(editContext.NotifyValidationStateChanged);
        host.WaitForAssertion(() => host.Markup.Should().NotContain("Choose a value"));
    }

    /// <summary>Behavior source: ErrorMessage participates in the ambient EditContext even when no field-specific ValueExpression is supplied.</summary>
    [Fact]
    public async Task ValidationStateChanged_WithoutValueExpression_UsesAllEditContextMessages() {
        var model = new FormModel();
        var editContext = new EditContext(model);
        var messages = new ValidationMessageStore(editContext);
        var host = RenderInEditContext(editContext, model, includeValueExpression: false);

        messages.Add(new FieldIdentifier(model, string.Empty), "Invalid form");
        await host.InvokeAsync(editContext.NotifyValidationStateChanged);

        host.WaitForAssertion(() => host.Markup.Should().Contain("Choose a value"));
    }

    /// <summary>Behavior source: selecting a suggestion publishes ItemSelectedCallback and ValueChanged; the ASP.NET EditContext contract requires a bound field change notification.</summary>
    [Fact]
    public void SelectingSuggestion_InEditContext_PublishesValueAndNotifiesBoundField() {
        var model = new FormModel();
        var editContext = new EditContext(model);
        FieldIdentifier? changedField = null;
        editContext.OnFieldChanged += (_, args) => changedField = args.FieldIdentifier;
        var host = RenderInEditContext(editContext, model, includeValueExpression: true);
        var cut = host.FindComponent<TnTTypeahead<string>>();
        cut.Find("input").Input("a");
        cut.WaitForAssertion(() => cut.FindAll(".tnt-typeahead-list-item").Should().HaveCount(2));

        cut.FindAll(".tnt-typeahead-list-item")[1].Click();

        model.Value.Should().Be("Beta");
        changedField.Should().Be(FieldIdentifier.Create(() => model.Value));
    }

    /// <summary>Behavior source: changing DebounceMilliseconds changes the delay used for subsequent searches on the same component instance.</summary>
    [Fact]
    public async Task DebounceMilliseconds_ChangedOnSameInstance_AppliesNewDelay() {
        var searches = new List<string?>();
        var cut = RenderTypeahead((value, _) => {
            searches.Add(value);
            return Task.FromResult<IEnumerable<string>>(["Alpha"]);
        }, 60_000);
        await cut.InvokeAsync(() => cut.Find("input").Input("old"));
        searches.Should().BeEmpty();

        cut.Render(parameters => AddRequiredParameters(parameters, (value, _) => {
            searches.Add(value);
            return Task.FromResult<IEnumerable<string>>(["Alpha"]);
        }, 0));
        await cut.InvokeAsync(() => cut.Find("input").Input("new"));

        cut.WaitForAssertion(() => searches.Should().Equal("new"));
    }

    /// <summary>Behavior source: component disposal releases owned resources, and repeated disposal must not repeat cleanup or corrupt state.</summary>
    [Fact]
    public async Task DisposeAsync_Repeatedly_ReleasesPageScriptOnceAndUnsubscribesValidation() {
        var model = new FormModel();
        var editContext = new EditContext(model);
        var host = RenderInEditContext(editContext, model, includeValueExpression: true);
        var cut = host.FindComponent<TnTTypeahead<string>>();

        await cut.Instance.DisposeAsync();
        await cut.Instance.DisposeAsync();
        await host.InvokeAsync(editContext.NotifyValidationStateChanged);
        _typeaheadModule.VerifyInvoke("onDispose", 1);
    }

    private IRenderedComponent<TnTTypeahead<string>> RenderTypeahead(Func<string?, CancellationToken, Task<IEnumerable<string>>> itemsLookup, int debounceMilliseconds = 0) => Render<TnTTypeahead<string>>(parameters => AddRequiredParameters(parameters, itemsLookup, debounceMilliseconds));

    private static ComponentParameterCollectionBuilder<TnTTypeahead<string>> AddRequiredParameters(ComponentParameterCollectionBuilder<TnTTypeahead<string>> parameters, Func<string?, CancellationToken, Task<IEnumerable<string>>> itemsLookup, int debounceMilliseconds) => parameters
        .Add(component => component.ItemsLookupFunc, itemsLookup)
        .Add(component => component.DebounceMilliseconds, debounceMilliseconds);

    private IRenderedComponent<CascadingValue<EditContext>> RenderInEditContext(EditContext editContext, FormModel model, bool includeValueExpression) => Render<CascadingValue<EditContext>>(parameters => parameters
        .Add(component => component.Value, editContext)
        .Add(component => component.ChildContent, builder => {
            builder.OpenComponent<TnTTypeahead<string>>(0);
            builder.AddAttribute(1, nameof(TnTTypeahead<string>.ItemsLookupFunc), (Func<string?, CancellationToken, Task<IEnumerable<string>>>)((_, _) => Task.FromResult<IEnumerable<string>>(["Alpha", "Beta"])));
            builder.AddAttribute(2, nameof(TnTTypeahead<string>.DebounceMilliseconds), 0);
            builder.AddAttribute(3, nameof(TnTTypeahead<string>.Value), model.Value);
            builder.AddAttribute(4, nameof(TnTTypeahead<string>.ValueChanged), EventCallback.Factory.Create<string>(model, value => model.Value = value));
            builder.AddAttribute(5, nameof(TnTTypeahead<string>.ErrorMessage), "Choose a value");
            builder.AddAttribute(6, nameof(TnTTypeahead<string>.ResetValueOnSelect), false);
            builder.AddAttribute(7, nameof(TnTTypeahead<string>.RefocusAfterSelect), false);
            if (includeValueExpression) {
                builder.AddAttribute(8, nameof(TnTTypeahead<string>.ValueExpression), (Expression<Func<string?>>)(() => model.Value));
            }
            builder.CloseComponent();
        }));

    private sealed class FormModel {
        public string? Value { get; set; }
    }
}
