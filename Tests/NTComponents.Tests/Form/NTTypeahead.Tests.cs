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
        module.SetupVoid("onUpdate", _ => true).SetVoidResult();
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
        var menu = cut.Find("[data-nt-typeahead-menu='true']");
        menu.HasAttribute("hidden").Should().BeTrue();
        menu.GetAttribute("aria-hidden").Should().Be("true");
        menu.GetAttribute("popover").Should().Be("manual");
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
            var menu = cut.Find("[data-nt-typeahead-menu='true']");
            menu.HasAttribute("hidden").Should().BeFalse();
            menu.GetAttribute("aria-hidden").Should().Be("false");
            menu.GetAttribute("popover").Should().Be("manual");
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
    public async Task Rapid_Input_With_Async_Lookup_Renders_Latest_Results() {
        var searches = new List<string?>();
        var lookupStarted = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseLookup = new TaskCompletionSource<IEnumerable<CityOption>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var cut = RenderTypeahead(
            configure: parameters => parameters.Add(p => p.MinimumSearchLength, 2),
            itemsLookupFunc: async (search, cancellationToken) => {
                searches.Add(search);
                lookupStarted.TrySetResult(cancellationToken);
                var results = await releaseLookup.Task;
                cancellationToken.ThrowIfCancellationRequested();
                return results;
            },
            debounceMilliseconds: 300);

        // Dispatch the whole burst before any debounce continuation can run on the renderer.
        await cut.InvokeAsync(async () => {
            foreach (var text in new[] { "C", "Ca", "Cas", "Case", "Casey" }) {
                await cut.Find("input[role='combobox']").InputAsync(text);
            }
        });
        var lookupToken = await lookupStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);
        searches.Should().Equal("Casey");
        lookupToken.IsCancellationRequested.Should().BeFalse();
        releaseLookup.SetResult([new CityOption("Casey", "Remote match")]);

        cut.WaitForAssertion(() => {
            cut.Find("input[role='combobox']").GetAttribute("value").Should().Be("Casey");
            cut.FindAll("[role='option']").Should().ContainSingle().Which.TextContent.Should().Contain("Casey");
            cut.FindAll(".nt-combobox-empty").Should().BeEmpty();
            cut.Find("input[role='combobox']").GetAttribute("aria-expanded").Should().Be("true");
        }, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Rapid_Input_With_Delayed_Selection_Reset_Does_Not_Cancel_Latest_Lookup() {
        var selectionResetStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseSelectionReset = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var lookupStarted = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseLookup = new TaskCompletionSource<IEnumerable<CityOption>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var searches = new List<string?>();
        var cut = RenderTypeahead(new TestModel { City = CityOptions[0] },
            configure: parameters => parameters
                .Add(p => p.MinimumSearchLength, 2)
                .Add(p => p.BindAfter, EventCallback.Factory.Create<CityOption?>(this, async _ => {
                    selectionResetStarted.SetResult();
                    await releaseSelectionReset.Task;
                })),
            itemsLookupFunc: async (search, cancellationToken) => {
                searches.Add(search);
                lookupStarted.SetResult(cancellationToken);
                var results = await releaseLookup.Task;
                cancellationToken.ThrowIfCancellationRequested();
                return results;
            },
            debounceMilliseconds: 300);

        // The first input yields in the public selection callback while the later keys are dispatched.
        var firstInput = cut.Find("input[role='combobox']").InputAsync("C");
        await selectionResetStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);
        foreach (var text in new[] { "Ca", "Cas", "Case", "Casey" }) {
            await cut.Find("input[role='combobox']").InputAsync(text);
        }
        var lookupToken = await lookupStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);
        searches.Should().Equal("Casey");
        releaseSelectionReset.SetResult();
        await firstInput.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);
        releaseLookup.SetResult([new CityOption("Casey", "Remote match")]);

        cut.WaitForAssertion(() => {
            cut.Find("input[role='combobox']").GetAttribute("value").Should().Be("Casey");
            cut.FindAll("[role='option']").Should().ContainSingle().Which.TextContent.Should().Contain("Casey");
            cut.FindAll(".nt-combobox-empty").Should().BeEmpty();
            cut.Find("input[role='combobox']").GetAttribute("aria-expanded").Should().Be("true");
            lookupToken.IsCancellationRequested.Should().BeFalse();
        }, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Duplicate_Input_During_Lookup_Preserves_Request_And_Results() {
        var searches = new List<string?>();
        var lookupStarted = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseLookup = new TaskCompletionSource<IEnumerable<CityOption>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var cut = RenderTypeahead(itemsLookupFunc: async (search, cancellationToken) => {
            searches.Add(search);
            lookupStarted.TrySetResult(cancellationToken);
            return await releaseLookup.Task.WaitAsync(cancellationToken);
        });

        await cut.Find("input[role='combobox']").InputAsync("Casey");
        var lookupToken = await lookupStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);
        await cut.Find("input[role='combobox']").InputAsync("Casey");
        releaseLookup.SetResult([new CityOption("Casey", "Remote match")]);

        cut.WaitForAssertion(() => cut.FindAll("[role='option']").Should().ContainSingle().Which.TextContent.Should().Contain("Casey"));
        searches.Should().Equal("Casey");
        lookupToken.IsCancellationRequested.Should().BeFalse();
        cut.FindAll(".nt-combobox-empty").Should().BeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Parent_Rerender_During_Rapid_Lookup_Preserves_Latest_Results(bool bindSearchText) {
        var cut = Render<AsyncSearchWrapper>(parameters => parameters.Add(p => p.BindSearchText, bindSearchText));
        foreach (var text in new[] { "C", "Ca", "Cas", "Case", "Casey" }) {
            await cut.Find("input[role='combobox']").InputAsync(text);
        }
        var lookupToken = await cut.Instance.LookupStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);
        await cut.InvokeAsync(() => cut.Render());
        cut.Instance.ReleaseLookup.SetResult([new CityOption("Casey", "Remote match")]);

        cut.WaitForAssertion(() => {
            cut.FindAll("[role='option']").Should().ContainSingle().Which.TextContent.Should().Contain("Casey");
            cut.Find("input[role='combobox']").GetAttribute("value").Should().Be("Casey");
            cut.FindAll(".nt-combobox-empty").Should().BeEmpty();
        });
        cut.Instance.Searches.Should().Equal("Casey");
        lookupToken.IsCancellationRequested.Should().BeFalse();
        if (bindSearchText) {
            cut.Instance.SearchText.Should().Be("Casey");
        }
    }

    [Fact]
    public async Task Superseded_Lookup_Throwing_Cancellation_Does_Not_Clear_Latest_Results() {
        var firstLookupStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstLookupCancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirstLookup = new TaskCompletionSource<IEnumerable<CityOption>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseLatestLookup = new TaskCompletionSource<IEnumerable<CityOption>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var searches = new List<string?>();
        var cut = RenderTypeahead(itemsLookupFunc: async (search, cancellationToken) => {
            searches.Add(search);
            if (search == "Ca") {
                firstLookupStarted.SetResult();
                try {
                    return await releaseFirstLookup.Task.WaitAsync(cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
                    firstLookupCancelled.SetResult();
                    throw;
                }
            }
            return await releaseLatestLookup.Task.WaitAsync(cancellationToken);
        });

        await cut.Find("input[role='combobox']").InputAsync("Ca");
        await firstLookupStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);
        await cut.Find("input[role='combobox']").InputAsync("Casey");
        await firstLookupCancelled.Task.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);
        releaseLatestLookup.SetResult([new CityOption("Casey", "Remote match")]);

        cut.WaitForAssertion(() => {
            cut.FindAll("[role='option']").Should().ContainSingle().Which.TextContent.Should().Contain("Casey");
            cut.FindAll(".nt-combobox-empty").Should().BeEmpty();
            cut.FindAll(".nt-typeahead-progress").Should().BeEmpty();
        });
        searches.Should().Equal("Ca", "Casey");
    }

    [Fact]
    public async Task Escape_During_Delayed_Selection_Reset_Does_Not_Restart_Queued_Input() {
        var selectionResetStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseSelectionReset = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var searches = new List<string?>();
        var cut = RenderTypeahead(new TestModel { City = CityOptions[0] },
            configure: parameters => parameters.Add(p => p.BindAfter, EventCallback.Factory.Create<CityOption?>(this, async _ => {
                selectionResetStarted.SetResult();
                await releaseSelectionReset.Task;
            })),
            itemsLookupFunc: (search, _) => {
                searches.Add(search);
                return Task.FromResult<IEnumerable<CityOption>>(CityOptions);
            });

        var pendingInput = cut.Find("input[role='combobox']").InputAsync("Ca");
        await selectionResetStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);
        await cut.Find("input[role='combobox']").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });
        releaseSelectionReset.SetResult();
        await pendingInput.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);

        searches.Should().BeEmpty();
        cut.Find("input[role='combobox']").GetAttribute("value").Should().BeNullOrEmpty();
        cut.Find("input[role='combobox']").GetAttribute("aria-expanded").Should().Be("false");
        cut.FindAll("[role='option']").Should().BeEmpty();
    }

    [Fact]
    public async Task Newer_Input_During_Async_Cancellation_Keeps_Latest_Search_Ownership() {
        var initialLookupStarted = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
        var latestLookupStarted = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancellationStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var pendingInitialLookup = new TaskCompletionSource<IEnumerable<CityOption>>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var releaseCancellation = new ManualResetEventSlim();
        var searches = new List<string?>();
        var cut = RenderTypeahead(itemsLookupFunc: async (search, cancellationToken) => {
            searches.Add(search);
            if (search == "Before") {
                initialLookupStarted.SetResult(cancellationToken);
                return await pendingInitialLookup.Task.WaitAsync(cancellationToken);
            }
            latestLookupStarted.SetResult(cancellationToken);
            // Keep the latest response pending past the obsolete debounce continuation.
            await Task.Delay(200, cancellationToken);
            return [new CityOption("Casey", "Remote match")];
        }, debounceMilliseconds: 10);

        await cut.Find("input[role='combobox']").InputAsync("Before");
        var initialToken = await initialLookupStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);
        await using var registration = initialToken.Register(() => {
            cancellationStarted.SetResult();
            releaseCancellation.Wait(Xunit.TestContext.Current.CancellationToken);
        });
        try {
            await cut.Find("input[role='combobox']").InputAsync("Ca");
            await cancellationStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);
            await cut.Find("input[role='combobox']").InputAsync("Casey");
            var latestToken = await latestLookupStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);
            releaseCancellation.Set();

            cut.WaitForAssertion(() => {
                cut.FindAll("[role='option']").Should().ContainSingle().Which.TextContent.Should().Contain("Casey");
                cut.FindAll(".nt-combobox-empty").Should().BeEmpty();
                latestToken.IsCancellationRequested.Should().BeFalse();
            }, TimeSpan.FromSeconds(5));
            searches.Should().Equal("Before", "Casey");
        }
        finally {
            releaseCancellation.Set();
        }
    }

    [Fact]
    public async Task Escape_Keeping_Text_Allows_Searching_The_Same_Query_Again() {
        var cut = RenderTypeahead(configure: parameters => parameters.Add(p => p.ResetValueOnEscape, false));
        await cut.Find("input[role='combobox']").InputAsync("aus");
        cut.WaitForAssertion(() => cut.FindAll("[role='option']").Should().ContainSingle());
        await cut.Find("input[role='combobox']").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });
        await cut.Find("input[role='combobox']").InputAsync("aus");

        cut.WaitForAssertion(() => {
            cut.FindAll("[role='option']").Should().ContainSingle().Which.TextContent.Should().Contain("Austin");
            cut.Find("input[role='combobox']").GetAttribute("aria-expanded").Should().Be("true");
        });
    }

    [Fact]
    public async Task External_SearchText_Reset_Does_Not_Restart_Queued_Input() {
        var selectionResetStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseSelectionReset = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var searches = new List<string?>();
        var model = new TestModel { City = CityOptions[0] };
        var cut = RenderTypeahead(model,
            configure: parameters => parameters.Add(p => p.BindAfter, EventCallback.Factory.Create<CityOption?>(this, async _ => {
                selectionResetStarted.SetResult();
                await releaseSelectionReset.Task;
            })),
            itemsLookupFunc: (search, _) => {
                searches.Add(search);
                return Task.FromResult<IEnumerable<CityOption>>(CityOptions);
            });

        var pendingInput = cut.Find("input[role='combobox']").InputAsync("Ca");
        await selectionResetStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);
        await cut.InvokeAsync(() => cut.Render(parameters => parameters.Add(p => p.SearchText, "Boston").Add(p => p.Value, model.City)));
        releaseSelectionReset.SetResult();
        await pendingInput.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);

        searches.Should().BeEmpty();
        cut.Find("input[role='combobox']").GetAttribute("value").Should().Be("Boston");
        cut.Find("input[role='combobox']").GetAttribute("aria-expanded").Should().Be("false");
        cut.FindAll("[role='option']").Should().BeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Selection_And_Clear_Do_Not_Restart_Queued_Input(bool clearSelection) {
        var selectionResetStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseSelectionReset = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var searches = new List<string?>();
        var model = new TestModel { City = CityOptions[0] };
        var cut = RenderTypeahead(model,
            configure: parameters => parameters
                .Add(p => p.Label, "City")
                .Add(p => p.ShowClearButton, true)
                .Add(p => p.ResetSelectionOnInput, false)
                .Add(p => p.BindAfter, EventCallback.Factory.Create<CityOption?>(this, async value => {
                    if (value is null && selectionResetStarted.TrySetResult()) {
                        await releaseSelectionReset.Task;
                    }
                })),
            itemsLookupFunc: (search, _) => {
                searches.Add(search);
                return Task.FromResult<IEnumerable<CityOption>>(CityOptions);
            });
        await cut.Find("input[role='combobox']").InputAsync("a");
        cut.WaitForAssertion(() => cut.FindAll("[role='option']").Should().HaveCount(3));
        await cut.InvokeAsync(() => cut.Render(parameters => parameters.Add(p => p.ResetSelectionOnInput, true)));

        var pendingInput = cut.Find("input[role='combobox']").InputAsync("Ca");
        await selectionResetStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);
        await cut.Find("[role='option']").TriggerEventAsync("onpointerdown", new PointerEventArgs());
        if (clearSelection) {
            await cut.Find("button[aria-label='Clear City']").ClickAsync();
        }
        releaseSelectionReset.SetResult();
        await pendingInput.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);

        searches.Should().Equal("a");
        model.City.Should().Be(clearSelection ? null : CityOptions[0]);
        cut.Find("input[role='combobox']").GetAttribute("value").Should().Be(clearSelection ? null : "Austin");
        cut.Find("input[role='combobox']").GetAttribute("aria-expanded").Should().Be("false");
        cut.FindAll("[role='option']").Should().BeEmpty();
    }

    [Fact]
    public async Task Escape_Cancellation_Completing_After_Newer_Lookup_Does_Not_Clear_Results() {
        var initialLookupStarted = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
        var latestLookupStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancellationStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var pendingLookup = new TaskCompletionSource<IEnumerable<CityOption>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseLatestLookup = new TaskCompletionSource<IEnumerable<CityOption>>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var releaseCancellation = new ManualResetEventSlim();
        var cut = RenderTypeahead(itemsLookupFunc: async (search, cancellationToken) => {
            if (search == "Before") {
                initialLookupStarted.SetResult(cancellationToken);
                return await pendingLookup.Task.WaitAsync(cancellationToken);
            }
            latestLookupStarted.SetResult();
            return await releaseLatestLookup.Task.WaitAsync(cancellationToken);
        });
        await cut.Find("input[role='combobox']").InputAsync("Before");
        var initialToken = await initialLookupStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);
        await using var registration = initialToken.Register(() => {
            cancellationStarted.SetResult();
            releaseCancellation.Wait(Xunit.TestContext.Current.CancellationToken);
        });
        try {
            var escape = cut.Find("input[role='combobox']").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });
            await cancellationStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);
            await cut.Find("input[role='combobox']").InputAsync("Casey");
            await latestLookupStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);
            releaseLatestLookup.SetResult([new CityOption("Casey", "Remote match")]);
            cut.WaitForAssertion(() => cut.FindAll("[role='option']").Should().ContainSingle());
            releaseCancellation.Set();
            await escape.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);

            cut.FindAll("[role='option']").Should().ContainSingle().Which.TextContent.Should().Contain("Casey");
            cut.Find("input[role='combobox']").GetAttribute("value").Should().Be("Casey");
            cut.Find("input[role='combobox']").GetAttribute("aria-expanded").Should().Be("true");
        }
        finally {
            releaseCancellation.Set();
        }
    }

    [Fact]
    public async Task Typing_After_Bound_Selection_Preserves_Query_And_Renders_Results() {
        var cut = Render<AsyncSearchWrapper>(parameters => parameters.Add(p => p.InitialValue, CityOptions[0]));
        await cut.Find("input[role='combobox']").InputAsync("Casey");
        await cut.Instance.LookupStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);
        cut.Instance.ReleaseLookup.SetResult([new CityOption("Casey", "Remote match")]);

        cut.WaitForAssertion(() => {
            cut.FindAll("[role='option']").Should().ContainSingle().Which.TextContent.Should().Contain("Casey");
            cut.Find("input[role='combobox']").GetAttribute("value").Should().Be("Casey");
            cut.FindAll(".nt-combobox-empty").Should().BeEmpty();
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
            debounceMilliseconds: 1_000);
        var input = cut.Find("input[role='combobox']");

        input.Input("a");
        input.Input("ad");
        input.Input("ada");

        cut.WaitForAssertion(() => searches.Should().ContainSingle().Which.Should().Be("ada"), TimeSpan.FromSeconds(5));
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
    public void Clear_Button_Is_Opt_In() {
        var model = new TestModel {
            City = CityOptions[0]
        };

        var cut = RenderTypeahead(model);

        cut.FindAll(".nt-typeahead-clear-button").Should().BeEmpty();
    }

    [Fact]
    public async Task Clear_Button_Renders_After_Selection_And_Clears_Value() {
        var model = new TestModel();
        string? searchText = null;
        var bindAfterValues = new List<CityOption?>();
        var cut = RenderTypeahead(model, parameters => parameters
            .Add(p => p.ShowClearButton, true)
            .Add(p => p.SearchTextChanged, EventCallback.Factory.Create<string?>(this, value => searchText = value))
            .Add(p => p.BindAfter, EventCallback.Factory.Create<CityOption?>(this, value => bindAfterValues.Add(value))));

        cut.FindAll(".nt-typeahead-clear-button").Should().BeEmpty();
        cut.Find("input[role='combobox']").Input("bos");
        cut.WaitForAssertion(() => cut.FindAll(".nt-combobox-option").Should().ContainSingle());
        await cut.Find(".nt-combobox-option").TriggerEventAsync("onpointerdown", new PointerEventArgs());

        var clearButton = cut.Find("button.nt-typeahead-clear-button");
        clearButton.GetAttribute("aria-label").Should().Be("Clear City");
        clearButton.TextContent.Should().Contain("close");
        searchText.Should().Be("Boston");

        await clearButton.ClickAsync(new MouseEventArgs());

        model.City.Should().BeNull();
        searchText.Should().BeNull();
        bindAfterValues.Should().Equal(CityOptions[1], null);
        cut.Find("input[role='combobox']").GetAttribute("value").Should().BeNullOrEmpty();
        cut.Find("input[type='hidden']").GetAttribute("value").Should().BeEmpty();
        cut.FindAll(".nt-typeahead-clear-button").Should().BeEmpty();
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

        cut.Find(".nt-input").GetAttribute("class").Should().Contain("nt-invalid");
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
        cut.Find(".nt-input").GetAttribute("class").Should().NotContain("nt-invalid");
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

    private sealed class AsyncSearchWrapper : ComponentBase {
        private readonly TestModel _model = new();

        [Parameter]
        public bool BindSearchText { get; set; }

        [Parameter]
        public CityOption? InitialValue { get; set; }

        public string? SearchText { get; private set; }
        public List<string?> Searches { get; } = [];
        public TaskCompletionSource<CancellationToken> LookupStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<IEnumerable<CityOption>> ReleaseLookup { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        protected override void OnInitialized() => _model.City = InitialValue;

        protected override void BuildRenderTree(RenderTreeBuilder builder) {
            builder.OpenComponent<NTTypeahead<CityOption>>(0);
            builder.AddAttribute(1, nameof(NTTypeahead<CityOption>.Value), _model.City);
            builder.AddAttribute(2, nameof(NTTypeahead<CityOption>.ValueChanged), EventCallback.Factory.Create<CityOption?>(this, value => _model.City = value));
            builder.AddAttribute(3, nameof(NTTypeahead<CityOption>.ValueExpression), (Expression<Func<CityOption?>>)(() => _model.City));
            builder.AddAttribute(4, nameof(NTTypeahead<CityOption>.ItemsLookupFunc), (Func<string?, CancellationToken, Task<IEnumerable<CityOption>>>)SearchAsync);
            builder.AddAttribute(5, nameof(NTTypeahead<CityOption>.ItemTextSelector), (Func<CityOption, string>)(item => item.Name));
            builder.AddAttribute(6, nameof(NTTypeahead<CityOption>.MinimumSearchLength), 2);
            if (BindSearchText) {
                builder.AddAttribute(7, nameof(NTTypeahead<CityOption>.SearchText), SearchText);
                builder.AddAttribute(8, nameof(NTTypeahead<CityOption>.SearchTextChanged), EventCallback.Factory.Create<string?>(this, value => SearchText = value));
            }
            builder.CloseComponent();
        }

        private async Task<IEnumerable<CityOption>> SearchAsync(string? search, CancellationToken cancellationToken) {
            Searches.Add(search);
            LookupStarted.TrySetResult(cancellationToken);
            return await ReleaseLookup.Task.WaitAsync(cancellationToken);
        }
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
