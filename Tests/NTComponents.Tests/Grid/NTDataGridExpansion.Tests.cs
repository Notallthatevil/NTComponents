using Microsoft.AspNetCore.Components;
using System.Linq.Expressions;

namespace NTComponents.Tests.Grid;

public class NTDataGridExpansion_Tests : BunitContext {
    private readonly IQueryable<Claim> _claims = Enumerable.Range(1, 6).Select(id => new Claim(id, [new Payment($"Medical {id}", id * 10m), new Payment($"Travel {id}", id)])).AsQueryable();

    public NTDataGridExpansion_Tests() {
        SetRendererInfo(new RendererInfo("Server", true));
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public async Task RepeatedExpansionRequests_DoNotRerenderDetails() {
        var cut = RenderGrid();
        await cut.InvokeAsync(() => cut.Instance.SetRowExpandedAsync(_claims.First(), true));
        var renderCount = cut.RenderCount;
        await cut.InvokeAsync(() => cut.Instance.SetRowExpandedAsync(_claims.First(), true));
        cut.RenderCount.Should().Be(renderCount);
        cut.FindAll(".nt-data-grid-detail-row").Should().ContainSingle();

        await cut.InvokeAsync(() => cut.Instance.ExpandAllRowsAsync());
        renderCount = cut.RenderCount;
        await cut.InvokeAsync(() => cut.Instance.ExpandAllRowsAsync());
        cut.RenderCount.Should().Be(renderCount);

        await cut.InvokeAsync(() => cut.Instance.CollapseAllRowsAsync());
        renderCount = cut.RenderCount;
        await cut.InvokeAsync(() => cut.Instance.CollapseAllRowsAsync());
        cut.RenderCount.Should().Be(renderCount);
        cut.FindAll(".nt-data-grid-detail-row").Should().BeEmpty();
    }

    [Theory]
    [InlineData(false, "Expand row", "Collapse row")]
    [InlineData(true, "Show payments", "Hide payments")]
    public void ExpansionText_TracksRowState(bool customize, string expandText, string collapseText) {
        var cut = RenderGrid(parameters => {
            if (customize) {
                parameters.Add(grid => grid.ExpandRowText, expandText).Add(grid => grid.CollapseRowText, collapseText);
            }
        });

        cut.Find("button[aria-expanded='false'] .nt-button-label").TextContent.Should().Be(expandText);
        cut.Find("button[aria-expanded='false']").Click();
        cut.WaitForAssertion(() => {
            cut.Find("button[aria-expanded='true'] .nt-button-label").TextContent.Should().Be(collapseText);
            cut.FindAll(".nt-data-grid-detail-row").Should().ContainSingle();
        });

        cut.Find("button[aria-expanded='true']").Click();
        cut.WaitForAssertion(() => {
            cut.Find("button[aria-expanded='false'] .nt-button-label").TextContent.Should().Be(expandText);
            cut.FindAll(".nt-data-grid-detail-row").Should().BeEmpty();
        });
    }

    [Fact]
    public async Task CustomDetailTemplate_ReceivesParentItem_WithoutRequiringSubTable() {
        var cut = RenderGrid();
        cut.Render(parameters => parameters.Add(grid => grid.RowDetailTemplate, claim => builder => {
            builder.OpenElement(0, "p");
            builder.AddContent(1, $"Notes for claim {claim.Id}");
            builder.CloseElement();
        }));
        await cut.InvokeAsync(() => cut.Instance.ExpandAllRowsAsync());
        cut.Find(".nt-data-grid-detail-scroll p").TextContent.Should().Be("Notes for claim 1");
        cut.FindAll("table").Should().ContainSingle();
    }

    [Fact]
    public void CollapsedRow_ExpandsIntoIndependentPaymentColumns() {
        var cut = RenderGrid();
        cut.FindAll("table").Should().ContainSingle();
        cut.FindAll("button").Single(button => button.TextContent.Contains("Expand row")).Click();

        cut.WaitForAssertion(() => {
            cut.FindAll("table").Should().HaveCount(2);
            cut.Find(".nt-data-grid-detail-row > td").GetAttribute("colspan").Should().Be("2");
            cut.FindAll(".nt-data-grid-detail-row table th").Should().HaveCount(2);
            cut.FindAll(".nt-data-grid-detail-row tbody tr").Should().HaveCount(2);
            cut.Find(".nt-data-grid-detail-scroll").GetAttribute("style").Should().Contain("max-height: 120px");
            cut.Find("button[aria-expanded]").GetAttribute("aria-expanded").Should().Be("true");
            cut.Markup.Should().Contain("Medical 1").And.Contain("Travel 1");
        });
        cut.Find("button[aria-expanded]").Click();
        cut.FindAll(".nt-data-grid-detail-row").Should().BeEmpty();
    }

    [Fact]
    public async Task RowClickCallback_CanToggleExpansion_WithoutDetailClicksTogglingParent() {
        NTDataGrid<Claim>? grid = null;
        var cut = RenderGrid(parameters => parameters.Add(value => value.OnRowClicked, claim => grid!.ToggleRowExpandedAsync(claim)));
        grid = cut.Instance;
        cut.Find(".nt-data-grid-row").Click();
        cut.FindAll(".nt-data-grid-detail-row").Should().ContainSingle();
        cut.Find(".nt-data-grid-detail-row .nt-data-grid-sort-link").Click();
        cut.FindAll(".nt-data-grid-detail-row").Should().ContainSingle();
        await cut.InvokeAsync(() => grid.SetRowExpandedAsync(_claims.First(), false));
        cut.FindAll(".nt-data-grid-detail-row").Should().BeEmpty();
    }

    [Fact]
    public async Task ExpandAll_AppliesToLaterPages_AndCollapseAllClearsIndividualChoices() {
        // Contract: expand/collapse-all applies to rows on other pages, including individually expanded rows.
        var cut = RenderGrid();
        await cut.InvokeAsync(() => cut.Instance.SetRowExpandedAsync(_claims.First(), true));
        cut.FindAll("button").Single(button => button.TextContent.Contains("Expand all")).Click();
        cut.Render(parameters => parameters.Add(grid => grid.PageIndex, 1));
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Medical 2"));
        await cut.InvokeAsync(() => cut.Instance.CollapseAllRowsAsync());
        cut.FindAll(".nt-data-grid-detail-row").Should().BeEmpty();
        cut.Render(parameters => parameters.Add(grid => grid.PageIndex, 0));
        cut.WaitForAssertion(() => cut.FindAll(".nt-data-grid-detail-row").Should().BeEmpty());
    }

    [Fact]
    public async Task StableRowKey_PreservesExpansionAcrossNewInstances() {
        var cut = RenderGrid();
        await cut.InvokeAsync(() => cut.Instance.SetRowExpandedAsync(_claims.First(), true));
        // Contract: IsRowExpanded documents stable RowKey preservation across provider refreshes.
        // Fresh payment arrays make the record unequal too, so value equality cannot mask a missing RowKey.
        var refreshedClaims = _claims.Select(claim => new Claim(claim.Id, new[] { new Payment($"Refreshed claim {claim.Id}", 99m) })).ToArray();
        refreshedClaims[0].Should().NotBe(_claims.First());
        cut.Render(parameters => parameters.Add(grid => grid.Items, refreshedClaims.AsQueryable()));
        cut.WaitForAssertion(() => {
            cut.FindAll(".nt-data-grid-detail-row").Should().ContainSingle();
            cut.Find(".nt-data-grid-detail-row").TextContent.Should().Contain("Refreshed claim 1").And.NotContain("Medical 1");
        });
    }

    [Fact]
    public async Task DefaultExpanded_IndividualCollapseSurvivesPaging_AndExpandAllRestoresIt() {
        // Contract: DefaultRowsExpanded includes later rows; SetRowExpandedAsync collapses one row;
        // ExpandAllRowsAsync expands all rows, including rows on other pages.
        var cut = RenderGrid(parameters => parameters.Add(grid => grid.DefaultRowsExpanded, true));
        await cut.InvokeAsync(() => cut.Instance.SetRowExpandedAsync(_claims.First(), false));
        cut.FindAll(".nt-data-grid-detail-row").Should().BeEmpty();

        cut.Render(parameters => parameters.Add(grid => grid.PageIndex, 1));
        cut.WaitForAssertion(() => cut.Find(".nt-data-grid-detail-row").TextContent.Should().Contain("Medical 2"));
        cut.Render(parameters => parameters.Add(grid => grid.PageIndex, 0));
        cut.WaitForAssertion(() => {
            cut.Find(".nt-data-grid-row").TextContent.Should().Contain("1");
            cut.FindAll(".nt-data-grid-detail-row").Should().BeEmpty();
        });

        await cut.InvokeAsync(() => cut.Instance.ExpandAllRowsAsync());
        cut.Find(".nt-data-grid-detail-row").TextContent.Should().Contain("Medical 1");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ChangingDefaultExpanded_ResetsIndividualExpansionChoices(bool initiallyExpanded) {
        // Contract: DefaultRowsExpanded explicitly resets individual expansion choices when changed.
        var cut = RenderGrid(parameters => parameters.Add(grid => grid.DefaultRowsExpanded, initiallyExpanded), paginate: false);
        await cut.InvokeAsync(() => cut.Instance.SetRowExpandedAsync(_claims.First(), !initiallyExpanded));
        cut.FindAll(".nt-data-grid-detail-row").Should().HaveCount(initiallyExpanded ? 5 : 1);

        cut.Render(parameters => parameters.Add(grid => grid.DefaultRowsExpanded, !initiallyExpanded));
        cut.FindAll(".nt-data-grid-detail-row").Should().HaveCount(initiallyExpanded ? 0 : 6);
        cut.Render(parameters => parameters.Add(grid => grid.DefaultRowsExpanded, initiallyExpanded));
        cut.FindAll(".nt-data-grid-detail-row").Should().HaveCount(initiallyExpanded ? 6 : 0);
    }

    [Fact]
    public void DefaultExpanded_RendersEveryParentWithAllPayments() {
        var cut = RenderGrid(parameters => parameters.Add(grid => grid.DefaultRowsExpanded, true), paginate: false);
        cut.WaitForAssertion(() => {
            cut.FindAll(".nt-data-grid-detail-row").Should().HaveCount(6);
            cut.FindAll(".nt-data-grid-detail-row tbody tr").Should().HaveCount(12);
        });
    }

    [Fact]
    public void StaticRendering_CanRenderAllDetails_WithDisabledExpansionControls() {
        SetRendererInfo(new RendererInfo("Static", false));
        var cut = RenderGrid(parameters => parameters.Add(grid => grid.DefaultRowsExpanded, true));
        cut.FindAll(".nt-data-grid-detail-row").Should().ContainSingle();
        cut.Find("button[aria-expanded]").HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public async Task VirtualizedProvider_RequestsOnlyParents_AndExpandsNewWindows() {
        var requests = new List<int>();
        var cut = RenderGrid(parameters => parameters
            .Add(grid => grid.Virtualize, true)
            .Add(grid => grid.DefaultRowsExpanded, true)
            .Add(grid => grid.ItemsProvider, request => {
                requests.Add(request.StartIndex);
                return ValueTask.FromResult(new NTItemsProviderResult<Claim>(_claims.Skip(request.StartIndex).Take(request.Count ?? 6).ToArray(), 6));
            }), paginate: false, provider: true);
        var virtualizer = cut.FindComponent<NTVirtualize<Claim>>();
        await cut.InvokeAsync(() => virtualizer.Instance.LoadItems(0, 180, 0, 1));
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Medical 1"));
        await cut.InvokeAsync(() => virtualizer.Instance.LoadItems(100, 0, 5, 1));
        cut.WaitForAssertion(() => {
            cut.Markup.Should().Contain("Medical 6").And.NotContain("Medical 1");
            cut.FindAll(".nt-data-grid-detail-row tbody tr").Should().HaveCount(2);
            cut.FindAll(".nt-data-grid-detail-row .nt-data-grid-pagination").Should().BeEmpty();
            cut.FindComponents<NTVirtualize<Payment>>().Should().BeEmpty();
        });
        requests.Should().Contain(0).And.Contain(5);
    }

    private IRenderedComponent<NTDataGrid<Claim>> RenderGrid(Action<ComponentParameterCollectionBuilder<NTDataGrid<Claim>>>? configure = null, bool paginate = true, bool provider = false) => Render<NTDataGrid<Claim>>(parameters => {
        if (!provider) {
            parameters.Add(grid => grid.Items, _claims);
        }
        parameters.Add(grid => grid.RowKey, claim => claim.Id)
            .Add(grid => grid.ShowPagination, paginate)
            .Add(grid => grid.PageSize, 1)
            .Add(grid => grid.RowDetailMaxHeight, "120px")
            .Add(grid => grid.ChildContent, builder => {
                builder.OpenComponent<NTPropertyColumn<Claim, int>>(0);
                builder.AddAttribute(1, "Property", (Expression<Func<Claim, int>>)(claim => claim.Id));
                builder.CloseComponent();
            })
            .Add(grid => grid.RowDetailTemplate, claim => builder => {
                builder.OpenComponent<NTDataGrid<Payment>>(0);
                builder.AddAttribute(1, "Items", claim.Payments.AsQueryable());
                builder.AddAttribute(2, "ChildContent", (RenderFragment)(columns => {
                    columns.OpenComponent<NTPropertyColumn<Payment, string>>(0);
                    columns.AddAttribute(1, "Property", (Expression<Func<Payment, string>>)(payment => payment.Description));
                    columns.CloseComponent();
                    columns.OpenComponent<NTPropertyColumn<Payment, decimal>>(2);
                    columns.AddAttribute(3, "Property", (Expression<Func<Payment, decimal>>)(payment => payment.Amount));
                    columns.CloseComponent();
                }));
                builder.CloseComponent();
            });
        configure?.Invoke(parameters);
    });

    private sealed record Claim(int Id, Payment[] Payments);
    private sealed record Payment(string Description, decimal Amount);
}
