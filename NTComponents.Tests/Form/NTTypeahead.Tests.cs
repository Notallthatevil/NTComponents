using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;

namespace NTComponents.Tests.Form;

public class NTTypeahead_Tests : BunitContext {
    private const string JsModulePath = "./_content/NTComponents/Form/NTTypeahead.razor.js";

    private static readonly IReadOnlyList<CityOption> CityOptions = [
        new("Austin", "Texas"),
        new("Boston", "Massachusetts"),
        new("Dallas", "Texas")
    ];

    public NTTypeahead_Tests() {
        var module = JSInterop.SetupModule(JsModulePath);
        module.SetupVoid("onLoad", _ => true).SetVoidResult();
        module.SetupVoid("onDispose", _ => true).SetVoidResult();
        module.SetupVoid("scrollActiveOptionIntoView", _ => true).SetVoidResult();
    }

    private sealed record CityOption(string Name, string State);

    private sealed class RequiredModel {
        [Required]
        public CityOption? City { get; set; }
    }

    private sealed class TestModel {
        public CityOption? City { get; set; }
    }

    [Fact]
    public void Renders_NT_Field_Combobox() {
        var cut = RenderTypeahead(configure: parameters => parameters
            .Add(p => p.ElementId, "city-typeahead")
            .Add(p => p.Label, "City")
            .Add(p => p.SupportingText, "Search by city"));

        var input = cut.Find("input[role='combobox']");
        input.GetAttribute("id").Should().Be("city-typeahead");
        input.HasAttribute("name").Should().BeFalse();
        input.HasAttribute("onkeypress").Should().BeFalse();
        input.GetAttribute("aria-controls").Should().Be("city-typeahead-listbox");
        input.GetAttribute("data-nt-typeahead-input").Should().Be("true");
        cut.Find("input[type='hidden']").GetAttribute("name").Should().Be("model.City");
        cut.Find(".nt-input").GetAttribute("class").Should().Contain("nt-typeahead");
        cut.Find(".nt-input-supporting").TextContent.Should().Be("Search by city");
    }

    [Fact]
    public void Input_Searches_And_Renders_Results() {
        var cut = RenderTypeahead();

        cut.Find("input[role='combobox']").Input("a");

        cut.WaitForAssertion(() => {
            var options = cut.FindAll(".nt-combobox-option");
            options.Should().HaveCount(2);
            options[0].TextContent.Should().Contain("Austin");
            options[1].TextContent.Should().Contain("Dallas");
            cut.Find("input[role='combobox']").GetAttribute("aria-expanded").Should().Be("true");
        });
    }

    [Fact]
    public void Input_Searches_And_Caps_Rendered_Results() {
        var cities = Enumerable.Range(0, 20)
            .Select(index => new CityOption($"City {index}", "Test"))
            .ToArray();
        var cut = RenderTypeahead(
            configure: parameters => parameters.Add(p => p.MaxResults, 3),
            itemsLookupFunc: (_, _) => Task.FromResult<IEnumerable<CityOption>>(cities));

        cut.Find("input[role='combobox']").Input("city");

        cut.WaitForAssertion(() => cut.FindAll(".nt-combobox-option").Should().HaveCount(3));
    }

    [Fact]
    public void Input_Searches_And_Uses_Default_Result_Cap() {
        var cities = Enumerable.Range(0, 60)
            .Select(index => new CityOption($"City {index}", "Test"))
            .ToArray();
        var cut = RenderTypeahead(itemsLookupFunc: (_, _) => Task.FromResult<IEnumerable<CityOption>>(cities));

        cut.Find("input[role='combobox']").Input("city");

        cut.WaitForAssertion(() => cut.FindAll(".nt-combobox-option").Should().HaveCount(50));
    }

    [Fact]
    public void Input_Searches_And_Allows_Unbounded_Results_When_MaxResults_Is_Null() {
        var cities = Enumerable.Range(0, 60)
            .Select(index => new CityOption($"City {index}", "Test"))
            .ToArray();
        var cut = RenderTypeahead(
            configure: parameters => parameters.Add(p => p.MaxResults, null),
            itemsLookupFunc: (_, _) => Task.FromResult<IEnumerable<CityOption>>(cities));

        cut.Find("input[role='combobox']").Input("city");

        cut.WaitForAssertion(() => cut.FindAll(".nt-combobox-option").Should().HaveCount(60));
    }

    [Fact]
    public void Empty_Results_Keep_Aria_Controls_On_Listbox() {
        var cut = RenderTypeahead(itemsLookupFunc: (_, _) => Task.FromResult<IEnumerable<CityOption>>([]));
        var input = cut.Find("input[role='combobox']");

        input.Input("z");

        cut.WaitForAssertion(() => {
            var listboxId = input.GetAttribute("aria-controls");
            cut.Find($"#{listboxId}").GetAttribute("role").Should().Be("listbox");
            cut.Find(".nt-combobox-empty").GetAttribute("role").Should().Be("status");
        });
    }

    [Fact]
    public async Task Arrow_Keys_Update_Active_Option_And_Request_Scroll() {
        var cut = RenderTypeahead();
        var input = cut.Find("input[role='combobox']");

        input.Input("a");
        cut.WaitForAssertion(() => cut.FindAll(".nt-combobox-option").Should().HaveCount(2));
        await input.KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown" });

        var options = cut.FindAll(".nt-combobox-option");
        cut.Find("input[role='combobox']").GetAttribute("aria-activedescendant").Should().Be(options[1].GetAttribute("id"));
        options[1].GetAttribute("class").Should().Contain("nt-combobox-option-active");
        options[1].GetAttribute("tabindex").Should().Be("-1");
        JSInterop.VerifyInvoke("scrollActiveOptionIntoView", 1);
    }

    [Fact]
    public async Task Tab_Defers_Active_Item_Selection_Until_Blur_To_Allow_Default_Focus_Move() {
        var model = new TestModel();
        var cut = RenderTypeahead(model);
        var input = cut.Find("input[role='combobox']");

        input.Input("a");
        cut.WaitForAssertion(() => cut.FindAll(".nt-combobox-option").Should().HaveCount(2));
        await input.KeyDownAsync(new KeyboardEventArgs { Key = "Tab" });

        model.City.Should().BeNull();
        cut.Find("input[role='combobox']").GetAttribute("value").Should().Be("a");

        await input.BlurAsync(new FocusEventArgs());

        model.City.Should().Be(CityOptions[0]);
        cut.Find("input[role='combobox']").GetAttribute("value").Should().Be("Austin");
        cut.FindAll(".nt-combobox-option").Should().BeEmpty();
    }

    [Fact]
    public async Task Pointer_Option_Is_Not_Tabbable_And_Selects_Once() {
        var model = new TestModel();
        var selectedCount = 0;
        var bindAfterCount = 0;
        var cut = RenderTypeahead(model, parameters => parameters
            .Add(p => p.ItemSelectedCallback, EventCallback.Factory.Create<CityOption?>(this, _ => selectedCount++))
            .Add(p => p.BindAfter, EventCallback.Factory.Create<CityOption?>(this, _ => bindAfterCount++)));

        cut.Find("input[role='combobox']").Input("bos");
        cut.WaitForAssertion(() => cut.FindAll(".nt-combobox-option").Should().ContainSingle());
        var option = cut.Find(".nt-combobox-option");

        option.GetAttribute("tabindex").Should().Be("-1");
        await option.TriggerEventAsync("onpointerdown", new PointerEventArgs());

        model.City.Should().Be(CityOptions[1]);
        selectedCount.Should().Be(1);
        bindAfterCount.Should().Be(1);
    }

    [Fact]
    public void Input_Searching_Renders_Ring_Progress() {
        var releaseLookup = new TaskCompletionSource<IEnumerable<CityOption>>(TaskCreationOptions.RunContinuationsAsynchronously);
        Func<string?, CancellationToken, Task<IEnumerable<CityOption>>> lookup = async (_, cancellationToken) => await releaseLookup.Task.WaitAsync(cancellationToken);
        var cut = RenderTypeahead(configure: parameters => parameters.Add(p => p.LoadingText, "Loading cities"), itemsLookupFunc: lookup);

        cut.Find("input[role='combobox']").Input("a");

        cut.WaitForAssertion(() => {
            var progress = cut.Find(".nt-typeahead-progress .nt-progress.nt-progress-ring.nt-progress-indeterminate");
            progress.GetAttribute("role").Should().Be("progressbar");
            progress.GetAttribute("aria-label").Should().Be("Loading cities");
        });

        releaseLookup.SetResult([]);
    }

    [Fact]
    public void Rapid_Input_Cancels_Previous_Search_Without_Disposed_Token_Exception() {
        var searches = new List<(string? Search, CancellationToken CancellationToken, TaskCompletionSource<IEnumerable<CityOption>> Completion)>();
        Func<string?, CancellationToken, Task<IEnumerable<CityOption>>> lookup = (search, cancellationToken) => {
            var completion = new TaskCompletionSource<IEnumerable<CityOption>>(TaskCreationOptions.RunContinuationsAsynchronously);
            searches.Add((search, cancellationToken, completion));
            return completion.Task;
        };
        var cut = RenderTypeahead(itemsLookupFunc: lookup);

        cut.Find("input[role='combobox']").Input("a");
        cut.WaitForAssertion(() => searches.Should().ContainSingle());
        cut.Find("input[role='combobox']").Input("bo");
        cut.WaitForAssertion(() => searches.Should().HaveCount(2));

        searches[0].CancellationToken.IsCancellationRequested.Should().BeTrue();
        searches[0].Completion.SetResult([CityOptions[0], CityOptions[2]]);
        searches[1].Completion.SetResult([CityOptions[1]]);

        cut.WaitForAssertion(() => {
            var options = cut.FindAll(".nt-combobox-option");
            options.Should().ContainSingle();
            options[0].TextContent.Should().Contain("Boston");
        });
    }

    [Fact]
    public void Rapid_Input_During_Debounce_Only_Invokes_Latest_Search() {
        var searches = new List<string?>();
        Func<string?, CancellationToken, Task<IEnumerable<CityOption>>> lookup = (search, _) => {
            searches.Add(search);
            return Task.FromResult<IEnumerable<CityOption>>(CityOptions);
        };
        var cut = RenderTypeahead(
            itemsLookupFunc: lookup,
            debounceMilliseconds: 100);
        var input = cut.Find("input[role='combobox']");

        input.Input("a");
        input.Input("ad");
        input.Input("ada");

        cut.WaitForAssertion(() => searches.Should().ContainSingle().Which.Should().Be("ada"));
    }

    [Fact]
    public async Task Pointer_Selects_Item_Updates_Value_And_Callbacks() {
        var model = new TestModel();
        CityOption? selected = null;
        CityOption? bindAfterValue = null;
        var cut = RenderTypeahead(model, parameters => parameters
            .Add(p => p.ItemSelectedCallback, EventCallback.Factory.Create<CityOption?>(this, value => selected = value))
            .Add(p => p.BindAfter, EventCallback.Factory.Create<CityOption?>(this, value => bindAfterValue = value)));

        cut.Find("input[role='combobox']").Input("bos");
        cut.WaitForAssertion(() => cut.FindAll(".nt-combobox-option").Should().ContainSingle());
        await cut.Find(".nt-combobox-option").TriggerEventAsync("onpointerdown", new PointerEventArgs());

        model.City.Should().Be(CityOptions[1]);
        selected.Should().Be(CityOptions[1]);
        bindAfterValue.Should().Be(CityOptions[1]);
        cut.Find("input[role='combobox']").GetAttribute("value").Should().Be("Boston");
        cut.FindAll(".nt-combobox-option").Should().BeEmpty();
    }

    [Fact]
    public async Task SearchText_Bind_Updates_When_User_Types_And_Selects_Item() {
        string? searchText = null;
        var cut = RenderTypeahead(configure: parameters => parameters
            .Add(p => p.SearchText, searchText)
            .Add(p => p.SearchTextChanged, EventCallback.Factory.Create<string?>(this, value => searchText = value)));

        cut.Find("input[role='combobox']").Input("bos");

        cut.WaitForAssertion(() => searchText.Should().Be("bos"));
        cut.WaitForAssertion(() => cut.FindAll(".nt-combobox-option").Should().ContainSingle());
        await cut.Find(".nt-combobox-option").TriggerEventAsync("onpointerdown", new PointerEventArgs());

        searchText.Should().Be("Boston");
        cut.Find("input[role='combobox']").GetAttribute("value").Should().Be("Boston");
    }

    [Fact]
    public void Parent_Rerendered_SearchText_Bind_Does_Not_Cancel_Pending_Search() {
        var cut = Render<ControlledSearchTextWrapper>();

        cut.Find("input[role='combobox']").Input("aus");

        cut.WaitForAssertion(() => cut.Instance.SearchText.Should().Be("aus"));
        cut.WaitForAssertion(() => {
            var options = cut.FindAll(".nt-combobox-option");
            options.Should().ContainSingle();
            options[0].TextContent.Should().Contain("Austin");
        });
    }

    [Fact]
    public void Parent_Rerendered_NTForm_SearchText_Bind_Does_Not_Cancel_Delayed_Search() {
        var cut = Render<ControlledSearchTextFormWrapper>();

        cut.Find("input[role='combobox']").Input("aus");

        cut.WaitForAssertion(() => cut.Instance.SearchText.Should().Be("aus"));
        cut.WaitForAssertion(() => cut.Instance.SearchCount.Should().Be(1));
        cut.WaitForAssertion(() => {
            var options = cut.FindAll(".nt-combobox-option");
            options.Should().ContainSingle();
            options[0].TextContent.Should().Contain("Austin");
            cut.Find("input[role='combobox']").GetAttribute("aria-expanded").Should().Be("true");
        });
    }

    [Fact]
    public void SearchText_Parameter_Sets_Input_Value() {
        var cut = RenderTypeahead(configure: parameters => parameters.Add(p => p.SearchText, "Dallas"));

        cut.Find("input[role='combobox']").GetAttribute("value").Should().Be("Dallas");

        cut.Render(parameters => parameters.Add(p => p.SearchText, "Austin"));

        cut.Find("input[role='combobox']").GetAttribute("value").Should().Be("Austin");
    }

    [Fact]
    public void SearchText_Parameter_Change_Clears_Stale_Results() {
        var cut = RenderTypeahead(configure: parameters => parameters.Add(p => p.SearchText, "a"));

        cut.Find("input[role='combobox']").Input("a");
        cut.WaitForAssertion(() => cut.FindAll(".nt-combobox-option").Should().HaveCount(2));

        cut.Render(parameters => parameters.Add(p => p.SearchText, "Boston"));

        cut.FindAll(".nt-combobox-option").Should().BeEmpty();
    }

    [Fact]
    public void Selected_Item_Renders_Hidden_Form_Post_Value() {
        var model = new TestModel {
            City = CityOptions[0]
        };

        var cut = RenderTypeahead(model, parameters => parameters.Add(p => p.ItemValueSelector, item => $"{item.Name}|{item.State}"));

        cut.Find("input[role='combobox']").HasAttribute("name").Should().BeFalse();
        var hiddenInput = cut.Find("input[type='hidden']");
        hiddenInput.GetAttribute("name").Should().Be("model.City");
        hiddenInput.GetAttribute("value").Should().Be("Austin|Texas");
    }

    [Fact]
    public void SubmitValue_False_Renders_No_Named_Form_Post_Control() {
        var model = new TestModel {
            City = CityOptions[0]
        };

        var cut = RenderTypeahead(model, parameters => parameters.Add(p => p.SubmitValue, false));

        cut.Find("input[role='combobox']").HasAttribute("name").Should().BeFalse();
        cut.FindAll("input[type='hidden']").Should().BeEmpty();
    }

    [Fact]
    public void Typing_After_Selection_Clears_Selected_Form_Value() {
        var model = new TestModel {
            City = CityOptions[0]
        };
        var cut = RenderTypeahead(model);

        cut.Find("input[role='combobox']").Input("B");

        cut.WaitForAssertion(() => model.City.Should().BeNull());
    }

    [Fact]
    public void Blur_Validates_Required_Selected_Item() {
        var model = new RequiredModel();

        var cut = Render<NTForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.ChildContent, (EditContext _) => builder => {
                builder.OpenComponent<DataAnnotationsValidator>(0);
                builder.CloseComponent();
                builder.OpenComponent<NTTypeahead<CityOption>>(1);
                builder.AddAttribute(2, nameof(NTTypeahead<CityOption>.Value), model.City);
                builder.AddAttribute(3, nameof(NTTypeahead<CityOption>.ValueChanged), EventCallback.Factory.Create<CityOption?>(this, value => model.City = value));
                builder.AddAttribute(4, nameof(NTTypeahead<CityOption>.ValueExpression), (Expression<Func<CityOption?>>)(() => model.City));
                builder.AddAttribute(5, nameof(NTTypeahead<CityOption>.ItemsLookupFunc), CitySearchAsync);
                builder.AddAttribute(6, nameof(NTTypeahead<CityOption>.ItemTextSelector), (Func<CityOption, string>)(item => item.Name));
                builder.AddAttribute(7, nameof(NTTypeahead<CityOption>.DebounceMilliseconds), 0);
                builder.CloseComponent();
            }));

        cut.Find("input[role='combobox']").Blur();

        cut.Find(".nt-input").GetAttribute("class").Should().Contain("nt-input-invalid");
        cut.Find(".nt-input-error-text").TextContent.Should().Be("The City field is required.");
        cut.Find("input[role='combobox']").GetAttribute("aria-invalid").Should().Be("true");
    }

    [Fact]
    public async Task Selecting_Item_Satisfies_NTForm_Required_Validation() {
        var model = new RequiredModel();

        var cut = Render<NTForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.ChildContent, (EditContext _) => builder => {
                builder.OpenComponent<DataAnnotationsValidator>(0);
                builder.CloseComponent();
                builder.OpenComponent<NTTypeahead<CityOption>>(1);
                builder.AddAttribute(2, nameof(NTTypeahead<CityOption>.Value), model.City);
                builder.AddAttribute(3, nameof(NTTypeahead<CityOption>.ValueChanged), EventCallback.Factory.Create<CityOption?>(this, value => model.City = value));
                builder.AddAttribute(4, nameof(NTTypeahead<CityOption>.ValueExpression), (Expression<Func<CityOption?>>)(() => model.City));
                builder.AddAttribute(5, nameof(NTTypeahead<CityOption>.ItemsLookupFunc), CitySearchAsync);
                builder.AddAttribute(6, nameof(NTTypeahead<CityOption>.ItemTextSelector), (Func<CityOption, string>)(item => item.Name));
                builder.AddAttribute(7, nameof(NTTypeahead<CityOption>.DebounceMilliseconds), 0);
                builder.CloseComponent();
            }));

        cut.Find("input[role='combobox']").Blur();
        cut.Find(".nt-input-error-text").TextContent.Should().Be("The City field is required.");
        cut.Find("input[role='combobox']").Input("aus");
        cut.WaitForAssertion(() => cut.FindAll(".nt-combobox-option").Should().ContainSingle());
        await cut.Find(".nt-combobox-option").TriggerEventAsync("onpointerdown", new PointerEventArgs());

        model.City.Should().Be(CityOptions[0]);
        cut.Find(".nt-input").GetAttribute("class").Should().NotContain("nt-input-invalid");
        cut.FindAll(".nt-input-error-text").Should().BeEmpty();
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
                builder.OpenComponent<NTTypeahead<CityOption>>(0);
                builder.AddAttribute(1, nameof(NTTypeahead<CityOption>.Value), model.City);
                builder.AddAttribute(2, nameof(NTTypeahead<CityOption>.ValueChanged), EventCallback.Factory.Create<CityOption?>(this, value => model.City = value));
                builder.AddAttribute(3, nameof(NTTypeahead<CityOption>.ValueExpression), (Expression<Func<CityOption?>>)(() => model.City));
                builder.AddAttribute(4, nameof(NTTypeahead<CityOption>.ItemsLookupFunc), CitySearchAsync);
                builder.AddAttribute(5, nameof(NTTypeahead<CityOption>.ItemTextSelector), (Func<CityOption, string>)(item => item.Name));
                builder.CloseComponent();
            }));

        var rootClass = cut.Find(".nt-input").GetAttribute("class")!;
        rootClass.Should().Contain("nt-input-filled");
        rootClass.Should().Contain("nt-input-dense");
        rootClass.Should().Contain("nt-input-disabled");
        cut.Find("input[role='combobox']").HasAttribute("disabled").Should().BeTrue();
    }

    private IRenderedComponent<NTTypeahead<CityOption>> RenderTypeahead(TestModel? model = null, Action<ComponentParameterCollectionBuilder<NTTypeahead<CityOption>>>? configure = null, Func<string?, CancellationToken, Task<IEnumerable<CityOption>>>? itemsLookupFunc = null, int debounceMilliseconds = 0) {
        model ??= new TestModel();
        return Render<NTTypeahead<CityOption>>(parameters => {
            parameters
                .Add(p => p.Value, model.City)
                .Add(p => p.ValueChanged, EventCallback.Factory.Create<CityOption?>(this, value => model.City = value))
                .Add(p => p.ValueExpression, (Expression<Func<CityOption?>>)(() => model.City))
                .Add(p => p.ItemsLookupFunc, itemsLookupFunc ?? CitySearchAsync)
                .Add(p => p.ItemTextSelector, item => item.Name)
                .Add(p => p.ItemSupportingTextSelector, item => item.State)
                .Add(p => p.DebounceMilliseconds, debounceMilliseconds);
            configure?.Invoke(parameters);
        });
    }

    private static Task<IEnumerable<CityOption>> CitySearchAsync(string? search, CancellationToken cancellationToken) {
        var results = CityOptions
            .Where(item => item.Name.Contains(search ?? string.Empty, StringComparison.OrdinalIgnoreCase))
            .AsEnumerable();
        return Task.FromResult(results);
    }

    private sealed class ControlledSearchTextWrapper : ComponentBase {
        private readonly TestModel _model = new();

        public string? SearchText { get; private set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder) {
            builder.OpenComponent<NTTypeahead<CityOption>>(0);
            builder.AddAttribute(1, nameof(NTTypeahead<CityOption>.Value), _model.City);
            builder.AddAttribute(2, nameof(NTTypeahead<CityOption>.ValueChanged), EventCallback.Factory.Create<CityOption?>(this, value => _model.City = value));
            builder.AddAttribute(3, nameof(NTTypeahead<CityOption>.ValueExpression), (Expression<Func<CityOption?>>)(() => _model.City));
            builder.AddAttribute(4, nameof(NTTypeahead<CityOption>.ItemsLookupFunc), (Func<string?, CancellationToken, Task<IEnumerable<CityOption>>>)CitySearchAsync);
            builder.AddAttribute(5, nameof(NTTypeahead<CityOption>.ItemTextSelector), (Func<CityOption, string>)(item => item.Name));
            builder.AddAttribute(6, nameof(NTTypeahead<CityOption>.SearchText), SearchText);
            builder.AddAttribute(7, nameof(NTTypeahead<CityOption>.SearchTextChanged), EventCallback.Factory.Create<string?>(this, OnSearchTextChanged));
            builder.AddAttribute(8, nameof(NTTypeahead<CityOption>.DebounceMilliseconds), 100);
            builder.CloseComponent();
        }

        private Task OnSearchTextChanged(string? value) {
            SearchText = value;
            return Task.CompletedTask;
        }
    }

    private sealed class ControlledSearchTextFormWrapper : ComponentBase {
        private readonly TestModel _model = new();

        public string? SearchText { get; private set; }
        public int SearchCount { get; private set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder) {
            builder.OpenComponent<NTForm>(0);
            builder.AddAttribute(1, nameof(NTForm.Model), _model);
            builder.AddAttribute(2, nameof(NTForm.ChildContent), (RenderFragment<EditContext>)(_ => formBuilder => {
                formBuilder.OpenComponent<DataAnnotationsValidator>(0);
                formBuilder.CloseComponent();
                formBuilder.OpenComponent<NTTypeahead<CityOption>>(1);
                formBuilder.AddAttribute(2, nameof(NTTypeahead<CityOption>.Value), _model.City);
                formBuilder.AddAttribute(3, nameof(NTTypeahead<CityOption>.ValueChanged), EventCallback.Factory.Create<CityOption?>(this, value => _model.City = value));
                formBuilder.AddAttribute(4, nameof(NTTypeahead<CityOption>.ValueExpression), (Expression<Func<CityOption?>>)(() => _model.City));
                formBuilder.AddAttribute(5, nameof(NTTypeahead<CityOption>.ItemsLookupFunc), (Func<string?, CancellationToken, Task<IEnumerable<CityOption>>>)DelayedCitySearchAsync);
                formBuilder.AddAttribute(6, nameof(NTTypeahead<CityOption>.ItemTextSelector), (Func<CityOption, string>)(item => item.Name));
                formBuilder.AddAttribute(7, nameof(NTTypeahead<CityOption>.SearchText), SearchText);
                formBuilder.AddAttribute(8, nameof(NTTypeahead<CityOption>.SearchTextChanged), EventCallback.Factory.Create<string?>(this, OnSearchTextChanged));
                formBuilder.AddAttribute(9, nameof(NTTypeahead<CityOption>.DebounceMilliseconds), 100);
                formBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        }

        private Task OnSearchTextChanged(string? value) {
            SearchText = value;
            return Task.CompletedTask;
        }

        private async Task<IEnumerable<CityOption>> DelayedCitySearchAsync(string? search, CancellationToken cancellationToken) {
            await Task.Delay(25, cancellationToken);
            SearchCount++;
            return await CitySearchAsync(search, cancellationToken);
        }
    }
}
