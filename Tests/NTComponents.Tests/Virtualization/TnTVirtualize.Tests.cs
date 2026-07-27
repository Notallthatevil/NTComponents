using Microsoft.AspNetCore.Components;
using NTComponents.Core;
using NTComponents.Virtualization;

namespace NTComponents.Tests.Virtualization;

public class TnTVirtualize_Tests : BunitContext {
    private const string JsModulePath = "./_content/NTComponents/Virtualization/TnTVirtualize.razor.js";

    public TnTVirtualize_Tests() {
        SetRendererInfo(new RendererInfo("Server", true));
        JSInterop.SetupModule(JsModulePath).SetupVoid().SetVoidResult();
    }

    [Fact]
    public async Task LoadMoreItems_Appends_Pages_And_Forwards_The_Request_Contract() {
        var requests = new List<TnTVirtualizeItemsProviderRequest<string>>();
        TnTVirtualizeItemsProvider<string> provider = request => {
            requests.Add(request);
            var items = request.StartIndex == 0 ? new[] { "Alpha", "Bravo" } : new[] { "Charlie", "Delta" };
            return ValueTask.FromResult(new TnTItemsProviderResult<string>(items, 4));
        };
        var cut = RenderVirtualize(provider, parameters => parameters
            .Add(component => component.LoadCount, 2)
            .Add(component => component.Sort, [new SortedProperty { PropertyName = "Name", Direction = SortDirection.Descending }]));

        await cut.InvokeAsync(() => cut.Instance.LoadMoreItems());

        requests.Should().ContainSingle();
        requests[0].StartIndex.Should().Be(0);
        requests[0].Count.Should().Be(2);
        requests[0].CancellationToken.CanBeCanceled.Should().BeTrue();
        requests[0].SortOnProperties.Should().Equal([new KeyValuePair<string, SortDirection>("Name", SortDirection.Descending)]);
        cut.Markup.Should().Contain("Alpha").And.Contain("Bravo");
        cut.Instance.Loading.Should().BeFalse();
        JSInterop.Invocations.Should().Contain(invocation => invocation.Identifier == "onNewItems");

        await cut.InvokeAsync(() => cut.Instance.LoadMoreItems());

        requests.Select(request => request.StartIndex).Should().Equal(0, 2);
        cut.Markup.Should().Contain("Alpha").And.Contain("Delta");
        cut.FindAll(".tnt-progress-indicator").Should().BeEmpty();
        cut.Find("div[style='height:0;width:0']").Should().NotBeNull();
    }

    [Fact]
    public async Task LoadMoreItems_Replaces_Items_When_The_Server_Total_Shrinks() {
        var responses = new Queue<TnTItemsProviderResult<string>>([
            new TnTItemsProviderResult<string>(["One", "Two", "Three"], 5),
            new TnTItemsProviderResult<string>(["Replacement"], 1)
        ]);
        var cut = RenderVirtualize(_ => ValueTask.FromResult(responses.Dequeue()));

        await cut.InvokeAsync(() => cut.Instance.LoadMoreItems());
        await cut.InvokeAsync(() => cut.Instance.LoadMoreItems());

        cut.Markup.Should().Contain("Replacement");
        cut.Markup.Should().NotContain("One").And.NotContain("Three");
        cut.FindAll(".tnt-progress-indicator").Should().BeEmpty();
    }

    [Fact]
    public async Task LoadMoreItems_Without_A_Provider_Leaves_The_Loading_State_Undisturbed() {
        var cut = Render<TnTVirtualize<string>>();

        await cut.InvokeAsync(() => cut.Instance.LoadMoreItems());

        cut.Instance.Loading.Should().BeTrue();
        cut.Markup.Should().NotContain("No items to show");
    }

    [Fact]
    public async Task Custom_Loading_And_Empty_Templates_Describe_The_Provider_Transition() {
        var result = new TaskCompletionSource<TnTItemsProviderResult<string>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var cut = RenderVirtualize(_ => new ValueTask<TnTItemsProviderResult<string>>(result.Task), parameters => parameters
            .Add(component => component.LoadingTemplate, builder => builder.AddContent(0, "Fetching records"))
            .Add(component => component.EmptyTemplate, builder => builder.AddContent(0, "No matching records")));

        var load = cut.Instance.LoadMoreItems();

        cut.Markup.Should().Contain("Fetching records").And.NotContain("No matching records");

        result.SetResult(new TnTItemsProviderResult<string>([], 0));
        await load;

        cut.Markup.Should().Contain("No matching records").And.NotContain("Fetching records");
        cut.Instance.Loading.Should().BeFalse();
    }

    [Fact]
    public async Task Default_Templates_Render_Items_And_The_Empty_Result() {
        var populated = Render<TnTVirtualize<string>>(parameters => parameters
            .Add(component => component.ItemsProvider, _ => ValueTask.FromResult(new TnTItemsProviderResult<string>(["Default item"], 1))));
        var empty = Render<TnTVirtualize<string>>(parameters => parameters
            .Add(component => component.ItemsProvider, _ => ValueTask.FromResult(new TnTItemsProviderResult<string>([], 0))));

        await populated.InvokeAsync(() => populated.Instance.LoadMoreItems());
        await empty.InvokeAsync(() => empty.Instance.LoadMoreItems());

        populated.Markup.Should().Contain("<div>Default item</div>");
        empty.Markup.Should().Contain("No items to show");
    }

    [Fact]
    public void Element_Class_And_Style_Merge_Consumer_Attributes() {
        var cut = RenderVirtualize(_ => ValueTask.FromResult(new TnTItemsProviderResult<string>([], 0)), parameters => parameters
            .AddUnmatched("class", "consumer-class")
            .AddUnmatched("style", "color: red"));

        cut.Instance.ElementClass.Should().Contain("tnt-virtualize-container").And.Contain("consumer-class");
        cut.Instance.ElementStyle.Should().Contain("color: red");
    }

    [Fact]
    public async Task RefreshDataAsync_Clears_Stale_Items_Resets_Scroll_And_Loads_Again() {
        var items = new[] { "Old" };
        var callCount = 0;
        TnTVirtualizeItemsProvider<string> provider = request => {
            callCount++;
            return ValueTask.FromResult(new TnTItemsProviderResult<string>(items, items.Length));
        };
        var cut = RenderVirtualize(provider);
        await cut.InvokeAsync(() => cut.Instance.LoadMoreItems());
        items = ["New"];

        await cut.InvokeAsync(() => cut.Instance.RefreshDataAsync());

        callCount.Should().Be(2);
        cut.Markup.Should().Contain("New").And.NotContain("Old");
        JSInterop.Invocations.Should().ContainSingle(invocation => invocation.Identifier == "resetScrollPosition");
    }

    [Fact]
    public async Task A_Changed_Provider_Resets_Previous_Items_Before_The_Next_Load() {
        TnTVirtualizeItemsProvider<string> firstProvider = _ => ValueTask.FromResult(new TnTItemsProviderResult<string>(["First"], 1));
        TnTVirtualizeItemsProvider<string> secondProvider = _ => ValueTask.FromResult(new TnTItemsProviderResult<string>(["Second"], 1));
        var cut = RenderVirtualize(firstProvider);
        await cut.InvokeAsync(() => cut.Instance.LoadMoreItems());

        cut.Render(parameters => parameters
            .Add(component => component.ItemsProvider, secondProvider)
            .Add(component => component.ItemTemplate, RenderItem));

        cut.Markup.Should().NotContain("First");
        cut.Instance.Loading.Should().BeTrue();

        await cut.InvokeAsync(() => cut.Instance.LoadMoreItems());

        cut.Markup.Should().Contain("Second").And.NotContain("First");
    }

    [Fact]
    public async Task A_New_Load_Cancels_The_InFlight_Request_And_Only_Renders_The_Latest_Result() {
        var firstResult = new TaskCompletionSource<TnTItemsProviderResult<string>>(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken firstToken = default;
        var callCount = 0;
        TnTVirtualizeItemsProvider<string> provider = request => {
            callCount++;
            if (callCount == 1) {
                firstToken = request.CancellationToken;
                request.CancellationToken.Register(() => firstResult.TrySetCanceled(request.CancellationToken));
                return new ValueTask<TnTItemsProviderResult<string>>(firstResult.Task);
            }

            return ValueTask.FromResult(new TnTItemsProviderResult<string>(["Latest"], 1));
        };
        var cut = RenderVirtualize(provider);

        var firstLoad = cut.Instance.LoadMoreItems();
        var secondLoad = cut.Instance.LoadMoreItems();
        await Task.WhenAll(firstLoad, secondLoad);

        firstToken.IsCancellationRequested.Should().BeTrue();
        callCount.Should().Be(2);
        cut.Markup.Should().Contain("Latest");
        cut.Instance.Loading.Should().BeFalse();
    }

    [Fact]
    public async Task Disposing_The_Component_Cancels_An_InFlight_Load() {
        var result = new TaskCompletionSource<TnTItemsProviderResult<string>>(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken providerToken = default;
        TnTVirtualizeItemsProvider<string> provider = request => {
            providerToken = request.CancellationToken;
            request.CancellationToken.Register(() => result.TrySetCanceled(request.CancellationToken));
            return new ValueTask<TnTItemsProviderResult<string>>(result.Task);
        };
        var cut = RenderVirtualize(provider);
        var load = cut.Instance.LoadMoreItems();

        cut.Instance.Dispose();
        await load;

        providerToken.IsCancellationRequested.Should().BeTrue();
        result.Task.IsCanceled.Should().BeTrue();
    }

    [Fact]
    public async Task Disposing_After_A_Completed_Load_Does_Not_Cancel_The_Completed_Request_Or_Reload() {
        var callCount = 0;
        CancellationToken requestToken = default;
        TnTVirtualizeItemsProvider<string> provider = request => {
            callCount++;
            requestToken = request.CancellationToken;
            return ValueTask.FromResult(new TnTItemsProviderResult<string>(["Completed"], 1));
        };
        var cut = RenderVirtualize(provider);
        await cut.InvokeAsync(() => cut.Instance.LoadMoreItems());

        cut.Instance.Dispose();

        callCount.Should().Be(1);
        requestToken.IsCancellationRequested.Should().BeFalse();
        cut.Markup.Should().Contain("Completed");
    }

    [Fact]
    public async Task Provider_Failures_Are_Propagated_To_The_Caller() {
        TnTVirtualizeItemsProvider<string> provider = _ => throw new InvalidOperationException("Provider unavailable");
        var cut = RenderVirtualize(provider);

        var load = () => cut.InvokeAsync(() => cut.Instance.LoadMoreItems());

        await load.Should().ThrowAsync<InvalidOperationException>().WithMessage("Provider unavailable");
        cut.Markup.Should().NotContain("No items to show");
    }

    [Fact]
    public void NonInfinite_Mode_Is_Rejected_With_An_Explicit_Contract() {
        var render = () => Render<TnTVirtualize<string>>(parameters => parameters
            .Add(component => component.InfiniteScroll, false)
            .Add(component => component.ItemsProvider, _ => ValueTask.FromResult(new TnTItemsProviderResult<string>([], 0))));

        render.Should().Throw<NotImplementedException>().WithMessage("Non-infinite scroll has not been implemented");
    }

    [Fact]
    public void Request_Conversions_Preserve_Paging_And_Sort_Contracts() {
        var sorts = new[] { new KeyValuePair<string, SortDirection>("Name", SortDirection.Descending) };
        var virtualRequest = new TnTVirtualizeItemsProviderRequest<string> {
            StartIndex = 7,
            Count = 3,
            SortOnProperties = sorts,
            CancellationToken = new CancellationToken(canceled: true)
        };

        TnTItemsProviderRequest generalRequest = virtualRequest;
        TnTVirtualizeItemsProviderRequest<string> roundTrip = generalRequest;

        generalRequest.StartIndex.Should().Be(7);
        generalRequest.Count.Should().Be(3);
        generalRequest.SortOnProperties.Should().Equal(sorts);
        roundTrip.StartIndex.Should().Be(7);
        roundTrip.Count.Should().Be(3);
        roundTrip.SortOnProperties.Should().Equal(sorts);
        roundTrip.CancellationToken.CanBeCanceled.Should().BeFalse();
    }

    private IRenderedComponent<TnTVirtualize<string>> RenderVirtualize(TnTVirtualizeItemsProvider<string> provider, Action<ComponentParameterCollectionBuilder<TnTVirtualize<string>>>? configure = null) =>
        Render<TnTVirtualize<string>>(parameters => {
            parameters.Add(component => component.ItemsProvider, provider);
            parameters.Add(component => component.ItemTemplate, RenderItem);
            configure?.Invoke(parameters);
        });

    private static RenderFragment RenderItem(string item) => builder => builder.AddContent(0, $"[{item}]");
}
