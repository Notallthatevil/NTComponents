using Bunit.JSInterop;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace NTComponents.Tests.Form;

public class NTTypeahead_Branches_Tests : BunitContext {
    private const string JsModulePath = "./_content/NTComponents/Form/NTTypeahead.razor.js";
    private static readonly string?[] Suggestions = ["Alpha", "Beta", "Gamma"];

    public NTTypeahead_Branches_Tests() {
        SetRendererInfo(new RendererInfo("WebAssembly", true));
        SetupModule(JSInterop);
    }

    /// <summary>Behavior source: ItemTextSelector renders an item as field text and its documented default uses the item's string representation.</summary>
    [Fact]
    public void DefaultItemTextSelector_RendersEachItemsStringRepresentation() {
        var cut = RenderTypeahead(itemsLookup: (_, _) => Task.FromResult<IEnumerable<string?>>(Suggestions));

        cut.Find("input[role='combobox']").Input("a");

        cut.WaitForAssertion(() => {
            var labels = cut.FindAll(".nt-combobox-option-label");
            labels.Should().HaveCount(3);
            labels.Select(label => label.TextContent).Should().Equal("Alpha", "Beta", "Gamma");
        });
    }

    /// <summary>Behavior source: MenuItemAppearance controls the popup row density and Condensed is a supported public appearance.</summary>
    [Fact]
    public void CondensedMenuAppearance_AddsCondensedContractClass() {
        var cut = RenderTypeahead(configure: parameters => parameters.Add(component => component.MenuItemAppearance, NTMenuItemAppearance.Condensed));

        cut.Find(".nt-input").GetAttribute("class").Should().Contain("nt-typeahead-menu-items-condensed");
    }

    /// <summary>Behavior source: the component owns asynchronous search and JavaScript enhancement resources through IAsyncDisposable.</summary>
    [Fact]
    public async Task DisposeAsync_ReleasesJsEnhancementAndCanBeRepeated() {
        var cut = RenderTypeahead();

        await cut.Instance.DisposeAsync();
        await cut.Instance.DisposeAsync();

        JSInterop.VerifyInvoke("onDispose", 1);
    }

    /// <summary>Behavior source: ItemValueParser converts a native form-post value back into the selected item.</summary>
    [Fact]
    public void NativeFormValue_WithParser_ReturnsParsedItem() {
        var model = new StringModel();
        var cut = Render<TestableNTTypeahead<string?>>(parameters => AddRequiredParameters(parameters, model)
            .Add(component => component.ItemValueParser, value => value?.ToUpperInvariant()));

        var success = cut.Instance.Parse("beta", out var result, out var error);

        success.Should().BeTrue();
        result.Should().Be("BETA");
        error.Should().BeNull();
    }

    /// <summary>Behavior source: an empty native form-post value represents no selected item when no custom parser is supplied.</summary>
    [Fact]
    public void NativeFormValue_WhenEmpty_ReturnsNoSelection() {
        var model = new StringModel { Value = "Alpha" };
        var cut = Render<TestableNTTypeahead<string?>>(parameters => AddRequiredParameters(parameters, model));

        var success = cut.Instance.Parse(string.Empty, out var result, out var error);

        success.Should().BeTrue();
        result.Should().BeNull();
        error.Should().BeNull();
    }

    /// <summary>Behavior source: without ItemValueParser, a nonempty native value cannot be accepted as a selected item and must return a field-specific validation error.</summary>
    [Fact]
    public void NativeFormValue_WithoutParser_RejectsNonemptyTextWithoutChangingSelection() {
        var model = new StringModel { Value = "Alpha" };
        var cut = Render<TestableNTTypeahead<string?>>(parameters => AddRequiredParameters(parameters, model)
            .Add(component => component.DisplayName, "City"));

        var success = cut.Instance.Parse("beta", out var result, out var error);

        success.Should().BeFalse();
        result.Should().Be("Alpha");
        error.Should().Be("The City field is not valid.");
    }

    /// <summary>Behavior source: focusing an enabled typeahead reopens already-loaded suggestions for keyboard and pointer selection.</summary>
    [Fact]
    public async Task Focus_AfterBlur_ReopensLoadedSuggestions() {
        var cut = RenderTypeahead();
        cut.Find("input[role='combobox']").Input("a");
        cut.WaitForAssertion(() => cut.FindAll(".nt-combobox-option").Should().HaveCount(3));
        await cut.Find("input[role='combobox']").BlurAsync(new FocusEventArgs());

        await cut.Find("input[role='combobox']").FocusAsync(new FocusEventArgs());

        cut.Find("input[role='combobox']").GetAttribute("aria-expanded").Should().Be("true");
        cut.FindAll(".nt-combobox-option").Should().HaveCount(3);
    }

    /// <summary>Behavior source: disabled and read-only fields do not accept user input or start async lookup.</summary>
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void Input_WhenInteractionIsBlocked_DoesNotSearchOrClearSelection(bool disabled, bool readOnly) {
        var model = new StringModel { Value = "Alpha" };
        var lookupCount = 0;
        var cut = RenderTypeahead(model, parameters => parameters
            .Add(component => component.Disabled, disabled)
            .Add(component => component.ReadOnly, readOnly), (_, _) => {
                lookupCount++;
                return Task.FromResult<IEnumerable<string?>>(Suggestions);
            });

        cut.Find("input[role='combobox']").Input("beta");

        lookupCount.Should().Be(0);
        model.Value.Should().Be("Alpha");
        cut.Find("input[role='combobox']").GetAttribute("aria-expanded").Should().Be("false");
    }

    /// <summary>Behavior source: ResetSelectionOnInput=false preserves a selected value while the user refines independent SearchText.</summary>
    [Fact]
    public void Input_WhenResetSelectionIsDisabled_PreservesSelectedValueAndFormPostValue() {
        var model = new StringModel { Value = "Alpha" };
        var cut = RenderTypeahead(model, parameters => parameters.Add(component => component.ResetSelectionOnInput, false));

        cut.Find("input[role='combobox']").Input("g");

        cut.WaitForAssertion(() => cut.FindAll(".nt-combobox-option").Should().HaveCount(3));
        model.Value.Should().Be("Alpha");
        cut.Find("input[type='hidden']").GetAttribute("value").Should().Be("Alpha");
    }

    /// <summary>Behavior source: ResetSelectionOnInput only clears the selected value when typed text differs from its ItemTextSelector representation.</summary>
    [Fact]
    public void Input_WhenTextStillMatchesSelection_DoesNotClearOrInvokeBindAfter() {
        var model = new StringModel { Value = "Alpha" };
        var bindAfterCount = 0;
        var cut = RenderTypeahead(model, parameters => parameters.Add(component => component.BindAfter, EventCallback.Factory.Create<string?>(this, _ => bindAfterCount++)));

        cut.Find("input[role='combobox']").Input("Alpha");

        cut.WaitForAssertion(() => cut.FindAll(".nt-combobox-option").Should().HaveCount(3));
        model.Value.Should().Be("Alpha");
        bindAfterCount.Should().Be(0);
    }

    /// <summary>Behavior source: ResetValueOnEscape controls whether Escape clears both typed query and selected value.</summary>
    [Theory]
    [InlineData(true, null, null)]
    [InlineData(false, "Alpha", "a")]
    public async Task Escape_RespectsResetValueContract(bool resetValue, string? expectedValue, string? expectedSearchText) {
        var model = new StringModel { Value = "Alpha" };
        var cut = RenderTypeahead(model, parameters => parameters
            .Add(component => component.ResetSelectionOnInput, false)
            .Add(component => component.ResetValueOnEscape, resetValue));
        cut.Find("input[role='combobox']").Input("a");
        cut.WaitForAssertion(() => cut.FindAll(".nt-combobox-option").Should().HaveCount(3));

        await cut.Find("input[role='combobox']").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

        model.Value.Should().Be(expectedValue);
        cut.Find("input[role='combobox']").GetAttribute("value").Should().Be(expectedSearchText);
        cut.Find("input[role='combobox']").GetAttribute("aria-expanded").Should().Be("false");
    }

    /// <summary>Behavior source: ArrowUp and ArrowDown cycle through available suggestions and expose the active option through aria-activedescendant.</summary>
    [Fact]
    public async Task ArrowNavigation_MovesBackwardAndWrapsForward() {
        var cut = RenderTypeahead();
        cut.Find("input[role='combobox']").Input("a");
        cut.WaitForAssertion(() => cut.FindAll(".nt-combobox-option").Should().HaveCount(3));

        await cut.Find("input[role='combobox']").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowUp" });
        cut.Find("input[role='combobox']").GetAttribute("aria-activedescendant").Should().Be(cut.FindAll(".nt-combobox-option")[2].GetAttribute("id"));
        await cut.Find("input[role='combobox']").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown" });

        cut.Find("input[role='combobox']").GetAttribute("aria-activedescendant").Should().Be(cut.FindAll(".nt-combobox-option")[0].GetAttribute("id"));
    }

    /// <summary>Behavior source: Enter selects the active suggestion and publishes the documented value, item-selection, and BindAfter contracts.</summary>
    [Fact]
    public async Task Enter_WithActiveSuggestion_SelectsItAndPublishesBindingContracts() {
        var model = new StringModel();
        string? selected = null;
        string? bindAfter = null;
        var cut = RenderTypeahead(model, parameters => parameters
            .Add(component => component.ItemSelectedCallback, EventCallback.Factory.Create<string?>(this, value => selected = value))
            .Add(component => component.BindAfter, EventCallback.Factory.Create<string?>(this, value => bindAfter = value)));
        cut.Find("input[role='combobox']").Input("a");
        cut.WaitForAssertion(() => cut.FindAll(".nt-combobox-option").Should().HaveCount(3));

        await cut.Find("input[role='combobox']").KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        model.Value.Should().Be("Alpha");
        selected.Should().Be("Alpha");
        bindAfter.Should().Be("Alpha");
        cut.Find("input[role='combobox']").GetAttribute("value").Should().Be("Alpha");
        cut.Find("input[type='hidden']").GetAttribute("value").Should().Be("Alpha");
        cut.Find("input[role='combobox']").GetAttribute("aria-expanded").Should().Be("false");
    }

    /// <summary>Behavior source: disabled and read-only fields do not respond to keyboard commands or clear their selected value.</summary>
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task KeyDown_WhenInteractionIsBlocked_PreservesSelection(bool disabled, bool readOnly) {
        var model = new StringModel { Value = "Alpha" };
        var cut = RenderTypeahead(model, parameters => parameters
            .Add(component => component.Disabled, disabled)
            .Add(component => component.ReadOnly, readOnly));

        await cut.Find("input[role='combobox']").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

        model.Value.Should().Be("Alpha");
        cut.Find("input[type='hidden']").GetAttribute("value").Should().Be("Alpha");
    }

    /// <summary>Behavior source: keyboard selection and movement operate on available suggestions, so they are observable no-ops when none exist.</summary>
    [Fact]
    public async Task KeyboardSelectionAndMovement_WithNoSuggestions_AreNoOps() {
        var cut = RenderTypeahead();

        await cut.Find("input[role='combobox']").KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });
        await cut.Find("input[role='combobox']").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown" });
        await cut.Find("input[role='combobox']").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowUp" });

        cut.Find("input[role='combobox']").HasAttribute("aria-activedescendant").Should().BeFalse();
        cut.Find("input[role='combobox']").GetAttribute("aria-expanded").Should().Be("false");
        JSInterop.Invocations.Should().NotContain(invocation => invocation.Identifier == "scrollActiveOptionIntoView");
    }

    /// <summary>Behavior source: SearchText is nullable and MinimumSearchLength prevents lookup for an empty query, clearing stale suggestions instead.</summary>
    [Fact]
    public async Task NullInput_ClearsLoadedSuggestionsWithoutAnotherLookup() {
        var lookupCount = 0;
        var cut = RenderTypeahead(itemsLookup: (_, _) => {
            lookupCount++;
            return Task.FromResult<IEnumerable<string?>>(Suggestions);
        });
        cut.Find("input[role='combobox']").Input("a");
        cut.WaitForAssertion(() => cut.FindAll(".nt-combobox-option").Should().HaveCount(3));

        await cut.Find("input[role='combobox']").TriggerEventAsync("oninput", new ChangeEventArgs { Value = null });

        cut.WaitForAssertion(() => cut.FindAll(".nt-combobox-option").Should().BeEmpty());
        lookupCount.Should().Be(1);
        cut.Find("input[role='combobox']").GetAttribute("aria-expanded").Should().Be("false");
    }

    /// <summary>Behavior source: MinimumSearchLength is the inclusive threshold before ItemsLookupFunc is invoked.</summary>
    [Fact]
    public void MinimumSearchLength_InvokesLookupOnlyAtThreshold() {
        var searches = new List<string?>();
        var cut = RenderTypeahead(configure: parameters => parameters.Add(component => component.MinimumSearchLength, 2), itemsLookup: (search, _) => {
            searches.Add(search);
            return Task.FromResult<IEnumerable<string?>>(Suggestions);
        });

        cut.Find("input[role='combobox']").Input("a");
        searches.Should().BeEmpty();
        cut.Find("input[role='combobox']").Input("al");

        cut.WaitForAssertion(() => searches.Should().Equal("al"));
    }

    /// <summary>Behavior source: MaxResults is the maximum number of rendered suggestions, so zero renders none after a completed lookup.</summary>
    [Fact]
    public void MaxResults_Zero_RendersNoSuggestionsAndShowsEmptyState() {
        var cut = RenderTypeahead(configure: parameters => parameters
            .Add(component => component.MaxResults, 0)
            .Add(component => component.EmptyText, "Nothing matched"));

        cut.Find("input[role='combobox']").Input("a");

        cut.WaitForAssertion(() => {
            cut.FindAll(".nt-combobox-option").Should().BeEmpty();
            cut.Find(".nt-combobox-empty").TextContent.Should().Be("Nothing matched");
        });
    }

    /// <summary>Behavior source: JavaScript scrolling is progressive enhancement; keyboard navigation remains usable when the browser disconnects or rejects the scroll call.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ArrowNavigation_WhenScrollEnhancementFails_RemainsUsable(bool disconnected) {
        using var context = new BunitContext();
        context.SetRendererInfo(new RendererInfo("WebAssembly", true));
        var module = SetupModule(context.JSInterop);
        var scroll = module.SetupVoid("scrollActiveOptionIntoView", _ => true);
        if (disconnected) {
            scroll.SetException<JSDisconnectedException>(new JSDisconnectedException("Disconnected"));
        }
        else {
            scroll.SetException<JSException>(new JSException("Scroll failed"));
        }
        var model = new StringModel();
        var cut = context.Render<NTTypeahead<string?>>(parameters => AddRequiredParameters(parameters, model));
        cut.Find("input[role='combobox']").Input("a");
        cut.WaitForAssertion(() => cut.FindAll(".nt-combobox-option").Should().HaveCount(3));

        await cut.Find("input[role='combobox']").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown" });

        cut.Find("input[role='combobox']").GetAttribute("aria-activedescendant").Should().Be(cut.FindAll(".nt-combobox-option")[1].GetAttribute("id"));
    }

    private IRenderedComponent<NTTypeahead<string?>> RenderTypeahead(StringModel? model = null, Action<ComponentParameterCollectionBuilder<NTTypeahead<string?>>>? configure = null, Func<string?, CancellationToken, Task<IEnumerable<string?>>>? itemsLookup = null) {
        model ??= new StringModel();
        return Render<NTTypeahead<string?>>(parameters => {
            AddRequiredParameters(parameters, model, itemsLookup);
            configure?.Invoke(parameters);
        });
    }

    private static ComponentParameterCollectionBuilder<TComponent> AddRequiredParameters<TComponent>(ComponentParameterCollectionBuilder<TComponent> parameters, StringModel model, Func<string?, CancellationToken, Task<IEnumerable<string?>>>? itemsLookup = null) where TComponent : NTTypeahead<string?> => parameters
        .Add(component => component.Value, model.Value)
        .Add(component => component.ValueChanged, EventCallback.Factory.Create<string?>(model, value => model.Value = value))
        .Add(component => component.ValueExpression, (Expression<Func<string?>>)(() => model.Value))
        .Add(component => component.ItemsLookupFunc, itemsLookup ?? ((_, _) => Task.FromResult<IEnumerable<string?>>(Suggestions)))
        .Add(component => component.DebounceMilliseconds, 0);

    private static BunitJSModuleInterop SetupModule(BunitJSInterop jsInterop) {
        var module = jsInterop.SetupModule(JsModulePath);
        module.SetupVoid("onLoad", _ => true).SetVoidResult();
        module.SetupVoid("onUpdate", _ => true).SetVoidResult();
        module.SetupVoid("onDispose", _ => true).SetVoidResult();
        module.SetupVoid("scrollActiveOptionIntoView", _ => true).SetVoidResult();
        return module;
    }

    private sealed class TestableNTTypeahead<TItem> : NTTypeahead<TItem> {
        public bool Parse(string? value, [MaybeNullWhen(false)] out TItem? result, [NotNullWhen(false)] out string? validationErrorMessage) => TryParseValueFromString(value, out result, out validationErrorMessage);
    }

    private sealed class StringModel {
        public string? Value { get; set; }
    }
}
