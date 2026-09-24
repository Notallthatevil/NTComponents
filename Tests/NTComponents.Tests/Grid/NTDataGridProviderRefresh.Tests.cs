using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System.Linq.Expressions;

namespace NTComponents.Tests.Grid;

public class NTDataGridProviderRefresh_Tests : BunitContext {
    public NTDataGridProviderRefresh_Tests() {
        SetRendererInfo(new RendererInfo("Server", true));
        JSInterop.SetupVoid("NTComponents.updateUri", _ => true).SetVoidResult();
    }

    [Fact]
    public async Task InitialColumns_UseOneUncanceledProviderRequestAfterConfigurationSettles() {
        var requests = new List<NTDataGridItemsProviderRequest<GridItem>>();
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var items = new[] { new GridItem(1, "First", 10), new GridItem(2, "Second", 20) };

        var cut = Render<NTDataGrid<GridItem>>(parameters => parameters
            .Add(grid => grid.ItemsProvider, async request => {
                requests.Add(request);
                await release.Task.WaitAsync(request.CancellationToken);
                return new NTItemsProviderResult<GridItem>(items, 3);
            })
            .Add(grid => grid.ShowPagination, true)
            .Add(grid => grid.PageSize, 2)
            .Add(grid => grid.ChildContent, Columns));

        cut.WaitForAssertion(() => requests.Should().NotBeEmpty());
        await cut.InvokeAsync(async () => await Task.Yield());

        requests.Should().ContainSingle();
        requests[0].StartIndex.Should().Be(0);
        requests[0].Count.Should().Be(2);
        requests[0].Sorts.Should().Equal(new NTSortDescriptor("Name", SortDirection.Ascending));
        requests[0].CancellationToken.IsCancellationRequested.Should().BeFalse();

        release.SetResult();
        cut.WaitForAssertion(() => {
            cut.FindAll("thead th").Should().HaveCount(3);
            cut.FindAll("tbody tr.nt-data-grid-row").Should().HaveCount(2);
        });
        requests.Should().ContainSingle();
    }

    [Fact]
    public void SortingPagingPageSizeAndProviderReplacement_EachRefreshOnce() {
        var calls = new List<NTDataGridItemsProviderRequest<GridItem>>();
        var replacementCalls = new List<NTDataGridItemsProviderRequest<GridItem>>();
        NTDataGridItemsProvider<GridItem> provider = request => {
            calls.Add(request);
            return ValueTask.FromResult(CreateResult(request));
        };
        var cut = Render<NTDataGrid<GridItem>>(parameters => parameters
            .Add(grid => grid.ItemsProvider, provider)
            .Add(grid => grid.ShowPagination, true)
            .Add(grid => grid.PageSize, 2)
            .Add(grid => grid.ChildContent, Columns));
        cut.WaitForAssertion(() => calls.Should().HaveCount(1));

        cut.FindAll("thead button").Single(button => button.TextContent.Contains("Name", StringComparison.Ordinal)).Click();
        cut.WaitForAssertion(() => calls.Should().HaveCount(2));
        calls[1].Sorts.Should().Equal(new NTSortDescriptor("Name", SortDirection.Descending));

        cut.Find(".pagination-next-page").Click();
        cut.WaitForAssertion(() => calls.Should().HaveCount(3));
        calls[2].StartIndex.Should().Be(2);
        calls[2].Count.Should().Be(2);

        cut.Find(".nt-data-grid-page-size select").Change("3");
        cut.WaitForAssertion(() => calls.Should().HaveCount(4));
        calls[3].StartIndex.Should().Be(0);
        calls[3].Count.Should().Be(3);

        NTDataGridItemsProvider<GridItem> replacement = request => {
            replacementCalls.Add(request);
            return ValueTask.FromResult(CreateResult(request));
        };
        cut.Render(parameters => parameters
            .Add(grid => grid.ItemsProvider, replacement)
            .Add(grid => grid.ShowPagination, true)
            .Add(grid => grid.PageSize, 3)
            .Add(grid => grid.ChildContent, Columns));

        cut.WaitForAssertion(() => replacementCalls.Should().HaveCount(1));
        replacementCalls[0].Count.Should().Be(3);
        calls.Should().HaveCount(4);
    }

    [Fact]
    public void UnchangedParentRender_DoesNotRefreshInitialSortAgain() {
        var calls = new List<NTDataGridItemsProviderRequest<GridItem>>();
        NTDataGridItemsProvider<GridItem> provider = request => {
            calls.Add(request);
            return ValueTask.FromResult(CreateResult(request));
        };
        var cut = Render<NTDataGrid<GridItem>>(parameters => parameters
            .Add(grid => grid.ItemsProvider, provider)
            .Add(grid => grid.ShowPagination, true)
            .Add(grid => grid.PageSize, 2)
            .Add(grid => grid.ChildContent, Columns));
        cut.WaitForAssertion(() => calls.Should().HaveCount(1));

        cut.Render(parameters => parameters
            .Add(grid => grid.ItemsProvider, provider)
            .Add(grid => grid.ShowPagination, true)
            .Add(grid => grid.PageSize, 2)
            .Add(grid => grid.ChildContent, Columns));

        calls.Should().ContainSingle();
    }

    [Fact]
    public void DataAndSortColumnChanges_EachRefreshOnce() {
        var calls = new List<NTDataGridItemsProviderRequest<GridItem>>();
        NTDataGridItemsProvider<GridItem> provider = request => {
            calls.Add(request);
            return ValueTask.FromResult(CreateResult(request));
        };
        var format = "D2";
        Expression<Func<GridItem, int>> property = item => item.Id;
        RenderFragment columns = builder => {
            builder.OpenComponent<NTPropertyColumn<GridItem, int>>(0);
            builder.AddAttribute(1, nameof(NTPropertyColumn<GridItem, int>.Property), property);
            builder.AddAttribute(2, nameof(NTPropertyColumn<GridItem, int>.Format), format);
            builder.AddAttribute(3, nameof(NTPropertyColumn<GridItem, int>.InitialSortDirection), SortDirection.Ascending);
            builder.CloseComponent();
        };
        var cut = Render<NTDataGrid<GridItem>>(parameters => parameters
            .Add(grid => grid.ItemsProvider, provider)
            .Add(grid => grid.ShowPagination, true)
            .Add(grid => grid.PageSize, 2)
            .Add(grid => grid.ChildContent, columns));
        cut.WaitForAssertion(() => calls.Should().HaveCount(1));
        calls[0].Sorts.Should().Equal(new NTSortDescriptor("Id", SortDirection.Ascending));

        format = "D3";
        cut.Render(parameters => parameters
            .Add(grid => grid.ItemsProvider, provider)
            .Add(grid => grid.ShowPagination, true)
            .Add(grid => grid.PageSize, 2)
            .Add(grid => grid.ChildContent, columns));
        cut.WaitForAssertion(() => calls.Should().HaveCount(2));
        calls[1].Sorts.Should().Equal(new NTSortDescriptor("Id", SortDirection.Ascending));

        property = item => item.Amount;
        cut.Render(parameters => parameters
            .Add(grid => grid.ItemsProvider, provider)
            .Add(grid => grid.ShowPagination, true)
            .Add(grid => grid.PageSize, 2)
            .Add(grid => grid.ChildContent, columns));
        cut.WaitForAssertion(() => calls.Should().HaveCount(3));
        calls[2].Sorts.Should().Equal(new NTSortDescriptor("Amount", SortDirection.Ascending));
    }

    [Fact]
    public async Task PagingWhileSortRequestIsPending_CancelsOnlySupersededRequest() {
        var pending = new List<NTDataGridItemsProviderRequest<GridItem>>();
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var holdRequests = false;
        NTDataGridItemsProvider<GridItem> provider = async request => {
            if (holdRequests) {
                pending.Add(request);
                await release.Task.WaitAsync(request.CancellationToken);
            }

            return CreateResult(request);
        };
        var cut = Render<NTDataGrid<GridItem>>(parameters => parameters
            .Add(grid => grid.ItemsProvider, provider)
            .Add(grid => grid.ShowPagination, true)
            .Add(grid => grid.PageSize, 2)
            .Add(grid => grid.ChildContent, Columns));
        cut.WaitForAssertion(() => cut.FindAll("tbody tr.nt-data-grid-row").Should().HaveCount(2));
        holdRequests = true;

        cut.FindAll("thead button").Single(button => button.TextContent.Contains("Name", StringComparison.Ordinal)).Click();
        cut.WaitForAssertion(() => pending.Should().HaveCount(1));
        pending[0].CancellationToken.IsCancellationRequested.Should().BeFalse();

        cut.Find(".pagination-next-page").Click();
        cut.WaitForAssertion(() => pending.Should().HaveCount(2));
        pending[0].CancellationToken.IsCancellationRequested.Should().BeTrue();
        pending[1].CancellationToken.IsCancellationRequested.Should().BeFalse();
        pending[1].StartIndex.Should().Be(2);

        release.SetResult();
        cut.WaitForAssertion(() => cut.Find("tbody").TextContent.Should().Contain("Third"));
        pending[1].CancellationToken.IsCancellationRequested.Should().BeFalse();
    }

    private static NTItemsProviderResult<GridItem> CreateResult(NTDataGridItemsProviderRequest<GridItem> request) {
        var items = new[] { new GridItem(1, "First", 10), new GridItem(2, "Second", 20), new GridItem(3, "Third", 30), new GridItem(4, "Fourth", 40), new GridItem(5, "Fifth", 50) };
        return new NTItemsProviderResult<GridItem>(items.Skip(request.StartIndex).Take(request.Count ?? items.Length).ToArray(), items.Length);
    }

    private static RenderFragment Columns => builder => {
        builder.OpenComponent<NTPropertyColumn<GridItem, int>>(0);
        builder.AddAttribute(1, nameof(NTPropertyColumn<GridItem, int>.Property), (Expression<Func<GridItem, int>>)(item => item.Id));
        builder.CloseComponent();
        builder.OpenComponent<NTPropertyColumn<GridItem, string>>(2);
        builder.AddAttribute(3, nameof(NTPropertyColumn<GridItem, string>.Property), (Expression<Func<GridItem, string>>)(item => item.Name));
        builder.AddAttribute(4, nameof(NTPropertyColumn<GridItem, string>.InitialSortDirection), SortDirection.Ascending);
        builder.CloseComponent();
        builder.OpenComponent<NTPropertyColumn<GridItem, int>>(5);
        builder.AddAttribute(6, nameof(NTPropertyColumn<GridItem, int>.Property), (Expression<Func<GridItem, int>>)(item => item.Amount));
        builder.CloseComponent();
    };

    private sealed record GridItem(int Id, string Name, int Amount);
}
