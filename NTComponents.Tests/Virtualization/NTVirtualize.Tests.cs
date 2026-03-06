using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.JSInterop;
using NTComponents.Virtualization;
using System.Globalization;
using Bunit;

namespace NTComponents.Tests.Virtualization;

public class NTVirtualize_Tests : BunitContext {
    private const string JsModulePath = "./_content/NTComponents/Virtualization/NTVirtualize.razor.js";

    public NTVirtualize_Tests() {
        var module = JSInterop.SetupModule(JsModulePath);
        module.SetupVoid("onLoad", _ => true).SetVoidResult();
        module.SetupVoid("onUpdate", _ => true).SetVoidResult();
        module.SetupVoid("onDispose", _ => true).SetVoidResult();
        module.SetupVoid("init", _ => true).SetVoidResult();
        module.SetupVoid("updateRenderState", _ => true).SetVoidResult();
    }

    [Fact]
    public void OnParametersSet_Throws_If_ItemSize_Zero_Or_Negative() {
        // Arrange
        var items = new List<string> { "Item 1" };
        NTVirtualizeItemsProvider<string> provider = request =>
            new ValueTask<TnTItemsProviderResult<string>>(new TnTItemsProviderResult<string>(items, items.Count));

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => Render<NTVirtualize<string>>(p => p
            .Add(c => c.ItemsProvider, provider)
            .Add(c => c.ItemSize, 0)));

        Assert.Throws<InvalidOperationException>(() => Render<NTVirtualize<string>>(p => p
            .Add(c => c.ItemsProvider, provider)
            .Add(c => c.ItemSize, -1)));
    }

    [Fact]
    public void OnParametersSet_Throws_If_ItemsProvider_Null() {
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => Render<NTVirtualize<string>>(p => p
            .Add(c => c.ItemSize, 50)));
    }

    [Fact]
    public void InitialRender_Shows_Spacers_And_Calls_Init() {
        // Arrange
        var items = new List<string>();
        NTVirtualizeItemsProvider<string> provider = request =>
            new ValueTask<TnTItemsProviderResult<string>>(new TnTItemsProviderResult<string>(items, items.Count));

        // Act
        var cut = Render<NTVirtualize<string>>(p => p
            .Add(c => c.ItemsProvider, provider)
            .Add(c => c.SpacerElement, "div"));

        // Assert
        var spacers = cut.FindAll("div[aria-hidden='true']");
        spacers.Should().HaveCount(2);

        // One before, one after
        spacers[0].GetAttribute("style").Should().Contain("height: 0px");
        spacers[1].GetAttribute("style").Should().Contain("height: 0px");
    }

    [Fact]
    public void LoadItems_Triggers_RefreshData() {
        // Arrange
        var items = new List<string> { "Item 1", "Item 2", "Item 3" };
        var callCount = 0;
        NTVirtualizeItemsProvider<string> provider = request => {
            callCount++;
            var resultItems = items.Skip(request.StartIndex).Take(request.Count ?? items.Count).ToList();
            return new ValueTask<TnTItemsProviderResult<string>>(new TnTItemsProviderResult<string>(resultItems, items.Count));
        };

        var cut = Render<NTVirtualize<string>>(p => p
            .Add(c => c.ItemsProvider, provider)
            .Add(c => c.ItemTemplate, item => builder => builder.AddContent(0, item)));

        // Act - Simulate JS call to LoadItems
        cut.Instance.LoadItems(0, 0, 0, 2);

        // Assert
        callCount.Should().BeGreaterThan(0);
        cut.FindAll("div[aria-hidden='true']").Should().HaveCount(2);
    }

    [Fact]
    public void Render_Items_When_Loaded() {
        // Arrange
        var items = Enumerable.Range(1, 10).Select(i => $"Item {i}").ToList();
        NTVirtualizeItemsProvider<string> provider = request => {
            var resultItems = items.Skip(request.StartIndex).Take(request.Count ?? items.Count).ToList();
            return new ValueTask<TnTItemsProviderResult<string>>(new TnTItemsProviderResult<string>(resultItems, items.Count));
        };

        // Act
        var cut = Render<NTVirtualize<string>>(p => p
            .Add(c => c.ItemsProvider, provider)
            .Add(c => c.ItemTemplate, item => builder => builder.AddContent(0, $"-{item}-")));

        // Simulate JS telling us what to load
        cut.Instance.LoadItems(0, 500, 0, 5);
        cut.WaitForState(() => cut.Markup.Contains("-Item 1-"));

        // Assert
        cut.Markup.Should().Contain("-Item 1-");
        cut.Markup.Should().Contain("-Item 5-");
        cut.Markup.Should().NotContain("-Item 6-");
    }

    [Fact]
    public void EmptyTemplate_Shows_When_No_Items() {
        // Arrange
        NTVirtualizeItemsProvider<string> provider = request =>
            new ValueTask<TnTItemsProviderResult<string>>(new TnTItemsProviderResult<string>(new List<string>(), 0));

        // Act
        var cut = Render<NTVirtualize<string>>(p => p
            .Add(c => c.ItemsProvider, provider)
            .Add(c => c.ItemTemplate, item => builder => builder.AddContent(0, item))
            .Add(c => c.EmptyTemplate, builder => builder.AddContent(0, "No items found")));

        cut.Instance.LoadItems(0, 0, 0, 10);
        cut.WaitForState(() => cut.Markup.Contains("No items found"));

        // Assert
        cut.Markup.Should().Contain("No items found");
    }

    [Fact]
    public async Task RefreshDataAsync_Forces_Reload() {
        // Arrange
        var items = new List<string> { "Old Item" };
        var providerCount = 0;
        NTVirtualizeItemsProvider<string> provider = request => {
            providerCount++;
            return new ValueTask<TnTItemsProviderResult<string>>(new TnTItemsProviderResult<string>(items, items.Count));
        };

        var cut = Render<NTVirtualize<string>>(p => p
            .Add(c => c.ItemsProvider, provider)
            .Add(c => c.ItemTemplate, item => builder => builder.AddContent(0, item)));

        await cut.InvokeAsync(() => cut.Instance.LoadItems(0, 0, 0, 10));
        cut.WaitForState(() => providerCount > 0);
        var initialCount = providerCount;

        // Act
        items = new List<string> { "New Item" };
        await cut.InvokeAsync(() => cut.Instance.RefreshDataAsync());

        // Assert
        cut.WaitForState(() => cut.Markup.Contains("New Item"));
        providerCount.Should().BeGreaterThan(initialCount);
    }

    [Fact]
    public void ItemsProvider_Exception_Is_Thrown_In_Renderer() {
        // Arrange
        NTVirtualizeItemsProvider<string> provider = request => throw new Exception("Data fetch failed");

        // Act
        var cut = Render<NTVirtualize<string>>(p => p
            .Add(c => c.ItemsProvider, provider)
            .Add(c => c.ItemTemplate, item => builder => builder.AddContent(0, item)));

        // LoadItems will trigger RefreshDataCoreAsync which will catch the exception
        cut.InvokeAsync(() => cut.Instance.LoadItems(0, 0, 0, 10));

        // Now rendering should throw
        Assert.Throws<Exception>(() => cut.WaitForAssertion(() => cut.Markup.Should().NotBeNull()));
    }

    [Fact]
    public async Task LoadingTemplate_Is_Used_While_Loading() {
        // Arrange
        var tcs = new TaskCompletionSource<TnTItemsProviderResult<string>>();
        var firstCall = true;
        NTVirtualizeItemsProvider<string> provider = request => {
            if (firstCall) {
                firstCall = false;
                return new ValueTask<TnTItemsProviderResult<string>>(new TnTItemsProviderResult<string>(new List<string>(), 100));
            }
            return new ValueTask<TnTItemsProviderResult<string>>(tcs.Task);
        };

        var cut = Render<NTVirtualize<string>>(p => p
            .Add(c => c.ItemsProvider, provider)
            .Add(c => c.ItemTemplate, item => builder => builder.AddContent(0, item))
            .Add(c => c.LoadingTemplate, context => builder => builder.AddContent(0, "Loading...")));

        // Initial load to set total count
        await cut.InvokeAsync(() => cut.Instance.LoadItems(0, 0, 0, 10));
        cut.WaitForState(() => !firstCall);

        // Act - Trigger another load which will be slow
        await cut.InvokeAsync(() => cut.Instance.LoadItems(0, 0, 10, 5));

        // Assert
        cut.WaitForState(() => cut.Markup.Contains("Loading..."));

        // Finish loading - provide enough items to fill the requested range (10-15)
        tcs.SetResult(new TnTItemsProviderResult<string>(Enumerable.Range(10, 5).Select(i => $"Item {i}").ToList(), 100));
        cut.WaitForState(() => cut.Markup.Contains("Item 14"));
        cut.Markup.Should().NotContain("Loading...");
    }

    [Fact]
    public async Task Multiple_LoadItems_Calls_Respect_Last_One() {
        // Arrange
        var callCount = 0;
        NTVirtualizeItemsProvider<string> provider = request => {
            callCount++;
            return new ValueTask<TnTItemsProviderResult<string>>(new TnTItemsProviderResult<string>(new List<string> { $"Item {request.StartIndex}" }, 100));
        };

        var cut = Render<NTVirtualize<string>>(p => p
            .Add(c => c.ItemsProvider, provider)
            .Add(c => c.ItemTemplate, item => builder => builder.AddContent(0, item)));

        // Act
        await cut.InvokeAsync(() => cut.Instance.LoadItems(0, 0, 10, 5));
        await cut.InvokeAsync(() => cut.Instance.LoadItems(0, 0, 20, 5));

        cut.WaitForState(() => cut.Markup.Contains("Item 20"));

        // Assert
        cut.Markup.Should().Contain("Item 20");
        cut.Markup.Should().NotContain("Item 10");
        callCount.Should().Be(2);
    }

    [Fact]
    public async Task ItemSize_Is_Respected_In_Initial_Render() {
        // Arrange
        var items = new List<string>() { "item1" };
        NTVirtualizeItemsProvider<string> provider = request =>
            new ValueTask<TnTItemsProviderResult<string>>(new TnTItemsProviderResult<string>(items, items.Count));
        const float itemSize = 123f;
        // Act
        var cut = Render<NTVirtualize<string>>(p => p
            .Add(c => c.ItemsProvider, provider)
            .Add(c => c.ItemTemplate, item => builder => {
                builder.OpenElement(0, "div");
                builder.AddAttribute(10, "style", $"height: {itemSize}px");
                builder.AddContent(20, item);
                builder.CloseElement();
            })
            .Add(c => c.ItemSize, itemSize));
        await cut.InvokeAsync(() => cut.Instance.LoadItems(0, 0, 0, 5));
        cut.WaitForState(() => cut.Markup.Contains("item1"));

        // Assert
        cut.Markup.Should().Contain($"height: {itemSize}px");
    }

    [Fact]
    public async Task LoadItems_Handles_OutOfBounds_Indices() {
        // Arrange
        var items = Enumerable.Range(0, 5).Select(i => $"Item {i}").ToList();
        NTVirtualizeItemsProvider<string> provider = request =>
            new ValueTask<TnTItemsProviderResult<string>>(new TnTItemsProviderResult<string>(items.Skip(request.StartIndex).Take(request.Count ?? 0).ToList(), items.Count));

        var cut = Render<NTVirtualize<string>>(p => p
            .Add(c => c.ItemsProvider, provider)
            .Add(c => c.ItemTemplate, item => builder => builder.AddContent(0, item)));

        // Act - Request starting beyond total count
        await cut.InvokeAsync(() => cut.Instance.LoadItems(0, 0, 10, 5));

        // Assert - Should adjusted to last possible items
        cut.WaitForState(() => cut.Markup.Contains("Item 4"));
    }
}
