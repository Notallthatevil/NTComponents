using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Bunit.TestDoubles;
using NTComponents.Virtualization;

namespace NTComponents.Tests.Grid;

public class NTDataGrid_Tests : BunitContext {
    private readonly IQueryable<TestGridItem> _items = new[] {
        new TestGridItem(1, "Gamma", new DateOnly(2026, 1, 3), 30.25m),
        new TestGridItem(2, "Alpha", new DateOnly(2026, 1, 1), 10m),
        new TestGridItem(3, "Beta", new DateOnly(2026, 1, 2), 20.5m)
    }.AsQueryable();

    public NTDataGrid_Tests() {
        SetRendererInfo(new RendererInfo("Server", true));
        var module = JSInterop.SetupModule("./_content/NTComponents/Virtualization/NTVirtualize.razor.js");
        module.SetupVoid("onLoad", _ => true).SetVoidResult();
        module.SetupVoid("onUpdate", _ => true).SetVoidResult();
        module.SetupVoid("onDispose", _ => true).SetVoidResult();
        module.SetupVoid("init", _ => true).SetVoidResult();
        module.SetupVoid("updateRenderState", _ => true).SetVoidResult();
        JSInterop.SetupVoid("NTComponents.updateUri", _ => true).SetVoidResult();
    }

    [Fact]
    public void DirectItems_Render_Semantic_Table_With_Property_Columns() {
        var cut = RenderGrid();

        cut.WaitForAssertion(() => {
            cut.Find("table").Should().NotBeNull();
            cut.Find("caption").TextContent.Should().Be("Invoices");
            cut.FindAll("th[scope='col']").Should().HaveCount(3);
            cut.FindAll("tbody tr").Should().HaveCount(3);
            cut.Markup.Should().Contain("Gamma");
            cut.Markup.Should().Contain(30.25m.ToString("C", System.Globalization.CultureInfo.CurrentCulture));
            cut.FindAll("[hidden]").Should().BeEmpty();
        });
    }

    [Fact]
    public void PropertyColumn_Updates_Generated_Title_When_Property_Changes() {
        System.Linq.Expressions.Expression<Func<TestGridItem, object>> property = item => item.DaysOpen;
        RenderFragment columns = builder => {
            builder.OpenComponent<NTPropertyColumn<TestGridItem, object>>(0);
            builder.AddAttribute(1, nameof(NTPropertyColumn<TestGridItem, object>.Property), property);
            builder.CloseComponent();
        };
        var cut = RenderGrid(columns);

        cut.WaitForAssertion(() => cut.Find("th").TextContent.Should().Contain("Days Open"));
        property = item => item.Name;
        cut.Render(parameters => parameters
            .Add(grid => grid.Items, _items)
            .Add(grid => grid.Caption, "Invoices")
            .Add(grid => grid.ChildContent, columns));

        cut.WaitForAssertion(() => cut.Find("th").TextContent.Should().Contain("Name").And.NotContain("Days Open"));
    }

    [Fact]
    public void TemplateColumn_Renders_Custom_Cell_Template() {
        var cut = RenderGrid(columns: builder => {
            builder.OpenComponent<NTPropertyColumn<TestGridItem, string>>(0);
            builder.AddAttribute(1, nameof(NTPropertyColumn<TestGridItem, string>.Title), "Name");
            builder.AddAttribute(2, nameof(NTPropertyColumn<TestGridItem, string>.Property), (System.Linq.Expressions.Expression<Func<TestGridItem, string>>)(item => item.Name));
            builder.CloseComponent();
            builder.OpenComponent<NTTemplateColumn<TestGridItem>>(3);
            builder.AddAttribute(4, nameof(NTTemplateColumn<TestGridItem>.Title), "Actions");
            builder.AddAttribute(5, nameof(NTTemplateColumn<TestGridItem>.ChildContent), (RenderFragment<TestGridItem>)(item => cellBuilder => cellBuilder.AddContent(0, $"Open {item.Id}")));
            builder.CloseComponent();
        });

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Open 1"));
    }

    [Fact]
    public void TemplateColumn_WithSortBy_Sorts_Direct_Items_Without_Sortable_Parameter() {
        var sortBy = NTGridSort<TestGridItem>.ByAscending(item => item.Created);
        var cut = RenderGrid(columns: builder => {
            builder.OpenComponent<NTPropertyColumn<TestGridItem, DateOnly>>(0);
            builder.AddAttribute(1, nameof(NTPropertyColumn<TestGridItem, DateOnly>.Title), "Created");
            builder.AddAttribute(2, nameof(NTPropertyColumn<TestGridItem, DateOnly>.Property), (System.Linq.Expressions.Expression<Func<TestGridItem, DateOnly>>)(item => item.Created));
            builder.CloseComponent();
            builder.OpenComponent<NTTemplateColumn<TestGridItem>>(3);
            builder.AddAttribute(4, nameof(NTTemplateColumn<TestGridItem>.Title), "CustomDate");
            builder.AddAttribute(5, nameof(NTTemplateColumn<TestGridItem>.SortBy), sortBy);
            builder.AddAttribute(6, nameof(NTTemplateColumn<TestGridItem>.ChildContent), (RenderFragment<TestGridItem>)(item => cellBuilder => cellBuilder.AddContent(0, item.Name)));
            builder.CloseComponent();
        });

        cut.WaitForAssertion(() => cut.FindAll(".nt-data-grid-sort-link").Should().HaveCount(2));
        cut.FindAll(".nt-data-grid-sort-link")[1].Click();

        cut.WaitForAssertion(() => {
            cut.FindAll("tbody tr")[0].TextContent.Should().Contain("Alpha");
            cut.FindAll(".nt-data-grid-sort-link-sorted").Should().ContainSingle();
            cut.FindAll("th")[1].GetAttribute("aria-sort").Should().Be("ascending");
        });

        cut.FindAll(".nt-data-grid-sort-link")[1].Click();

        cut.WaitForAssertion(() => {
            cut.FindAll("tbody tr")[0].TextContent.Should().Contain("Gamma");
            cut.FindAll("th")[1].GetAttribute("aria-sort").Should().Be("descending");
            cut.Find(".nt-data-grid-sort-indicator").ClassList.Should().Contain("nt-data-grid-sort-indicator-descending");
        });
    }

    [Fact]
    public async Task Local_Sort_Enumerates_Source_Once_Per_Refresh() {
        var source = new CountingEnumerable<TestGridItem>(_items.ToArray());
        var sortBy = NTGridSort<TestGridItem>.ByAscending(item => item.Name);
        var cut = Render<NTDataGrid<TestGridItem>>(parameters => parameters
            .Add(grid => grid.Items, source.AsQueryable())
            .Add(grid => grid.ChildContent, builder => {
                builder.OpenComponent<NTTemplateColumn<TestGridItem>>(0);
                builder.AddAttribute(1, nameof(NTTemplateColumn<TestGridItem>.Title), "Name");
                builder.AddAttribute(2, nameof(NTTemplateColumn<TestGridItem>.SortBy), sortBy);
                builder.AddAttribute(3, nameof(NTTemplateColumn<TestGridItem>.InitialSortDirection), SortDirection.Ascending);
                builder.AddAttribute(4, nameof(NTTemplateColumn<TestGridItem>.ChildContent), (RenderFragment<TestGridItem>)(item => cellBuilder => cellBuilder.AddContent(0, item.Name)));
                builder.CloseComponent();
            }));

        cut.WaitForAssertion(() => cut.FindAll("tbody tr")[0].TextContent.Should().Contain("Alpha"));
        source.ResetEnumerationCount();

        await cut.InvokeAsync(() => cut.Instance.RefreshDataGridAsync());

        source.EnumerationCount.Should().Be(1);
        cut.FindAll("tbody tr")[0].TextContent.Should().Contain("Alpha");
    }

    [Fact]
    public void TemplateColumn_WithSortBy_And_SortableFalse_Does_Not_Render_Sort_Link() {
        var sortBy = NTGridSort<TestGridItem>.ByAscending(item => item.Created);
        var cut = RenderGrid(columns: builder => {
            builder.OpenComponent<NTTemplateColumn<TestGridItem>>(0);
            builder.AddAttribute(1, nameof(NTTemplateColumn<TestGridItem>.Title), "Custom date");
            builder.AddAttribute(2, nameof(NTTemplateColumn<TestGridItem>.SortBy), sortBy);
            builder.AddAttribute(3, nameof(NTTemplateColumn<TestGridItem>.Sortable), false);
            builder.AddAttribute(4, nameof(NTTemplateColumn<TestGridItem>.ChildContent), (RenderFragment<TestGridItem>)(item => cellBuilder => cellBuilder.AddContent(0, item.Name)));
            builder.CloseComponent();
        });

        cut.WaitForAssertion(() => cut.FindAll(".nt-data-grid-sort-link").Should().BeEmpty());
    }

    [Fact]
    public void TemplateColumn_WithSortBy_Expands_Provider_Sort_Descriptors() {
        var captured = new List<NTDataGridItemsProviderRequest<TestGridItem>>();
        var sortBy = NTGridSort<TestGridItem>.ByDescending(item => (int?)item.Id).ThenAscending(item => item.Name);
        var cut = Render<NTDataGrid<TestGridItem>>(parameters => parameters
            .Add(grid => grid.ItemsProvider, request => {
                captured.Add(request);
                return ValueTask.FromResult(new NTItemsProviderResult<TestGridItem>(_items.ToArray(), _items.Count()));
            })
            .Add(grid => grid.ChildContent, builder => {
                builder.OpenComponent<NTTemplateColumn<TestGridItem>>(0);
                builder.AddAttribute(1, nameof(NTTemplateColumn<TestGridItem>.Title), "Custom name");
                builder.AddAttribute(2, nameof(NTTemplateColumn<TestGridItem>.SortBy), sortBy);
                builder.AddAttribute(3, nameof(NTTemplateColumn<TestGridItem>.ChildContent), (RenderFragment<TestGridItem>)(item => cellBuilder => cellBuilder.AddContent(0, item.Name)));
                builder.CloseComponent();
            }));

        cut.WaitForAssertion(() => cut.Find(".nt-data-grid-sort-link").Should().NotBeNull());
        cut.Find(".nt-data-grid-sort-link").Click();

        cut.WaitForAssertion(() => {
            var sorts = captured.Last().Sorts;
            sorts.Should().HaveCount(2);
            sorts[0].Should().Be(new NTSortDescriptor("Id", SortDirection.Descending));
            sorts[1].Should().Be(new NTSortDescriptor("Name", SortDirection.Ascending));
            cut.Find("th").GetAttribute("aria-sort").Should().Be("descending");
            cut.Find(".nt-data-grid-sort-indicator").ClassList.Should().Contain("nt-data-grid-sort-indicator-descending");
        });

        cut.Find(".nt-data-grid-sort-link").Click();

        cut.WaitForAssertion(() => {
            var sorts = captured.Last().Sorts;
            sorts.Should().HaveCount(2);
            sorts[0].Should().Be(new NTSortDescriptor("Id", SortDirection.Ascending));
            sorts[1].Should().Be(new NTSortDescriptor("Name", SortDirection.Descending));
            cut.Find("th").GetAttribute("aria-sort").Should().Be("ascending");
            cut.Find(".nt-data-grid-sort-indicator").ClassList.Should().Contain("nt-data-grid-sort-indicator-ascending");
        });
    }

    [Fact]
    public async Task Provider_Sort_Plan_Is_Reused_Until_Sorts_Change() {
        var captured = new List<NTDataGridItemsProviderRequest<TestGridItem>>();
        var sortBy = NTGridSort<TestGridItem>.ByAscending(item => item.Id).ThenDescending(item => item.Name);
        var cut = Render<NTDataGrid<TestGridItem>>(parameters => parameters
            .Add(grid => grid.ItemsProvider, request => {
                captured.Add(request);
                return ValueTask.FromResult(new NTItemsProviderResult<TestGridItem>(_items.ToArray(), _items.Count()));
            })
            .Add(grid => grid.ChildContent, builder => {
                builder.OpenComponent<NTTemplateColumn<TestGridItem>>(0);
                builder.AddAttribute(1, nameof(NTTemplateColumn<TestGridItem>.Title), "Custom name");
                builder.AddAttribute(2, nameof(NTTemplateColumn<TestGridItem>.SortBy), sortBy);
                builder.AddAttribute(3, nameof(NTTemplateColumn<TestGridItem>.InitialSortDirection), SortDirection.Ascending);
                builder.AddAttribute(4, nameof(NTTemplateColumn<TestGridItem>.ChildContent), (RenderFragment<TestGridItem>)(item => cellBuilder => cellBuilder.AddContent(0, item.Name)));
                builder.CloseComponent();
            }));

        cut.WaitForAssertion(() => captured.Last().Sorts.Should().Equal(
            new NTSortDescriptor("Id", SortDirection.Ascending),
            new NTSortDescriptor("Name", SortDirection.Descending)));
        captured.Clear();

        await cut.InvokeAsync(() => cut.Instance.RefreshDataGridAsync());
        await cut.InvokeAsync(() => cut.Instance.RefreshDataGridAsync());

        captured.Should().HaveCount(2);
        captured[0].Sorts.Should().BeSameAs(captured[1].Sorts);
        var cachedSorts = captured[1].Sorts;

        cut.Find(".nt-data-grid-sort-link").Click();

        cut.WaitForAssertion(() => {
            captured.Last().Sorts.Should().NotBeSameAs(cachedSorts);
            captured.Last().Sorts.Should().Equal(
                new NTSortDescriptor("Id", SortDirection.Descending),
                new NTSortDescriptor("Name", SortDirection.Ascending));
        });
    }

    [Fact]
    public void Provider_Sort_Plan_Is_Rebuilt_When_Column_Sort_Changes() {
        var captured = new List<NTDataGridItemsProviderRequest<TestGridItem>>();
        var sortBy = NTGridSort<TestGridItem>.ByAscending(item => item.Id);
        NTDataGridItemsProvider<TestGridItem> provider = request => {
            captured.Add(request);
            return ValueTask.FromResult(new NTItemsProviderResult<TestGridItem>(_items.ToArray(), _items.Count()));
        };
        RenderFragment columns = builder => {
            builder.OpenComponent<NTTemplateColumn<TestGridItem>>(0);
            builder.AddAttribute(1, nameof(NTTemplateColumn<TestGridItem>.Title), "Custom name");
            builder.AddAttribute(2, nameof(NTTemplateColumn<TestGridItem>.SortBy), sortBy);
            builder.AddAttribute(3, nameof(NTTemplateColumn<TestGridItem>.InitialSortDirection), SortDirection.Ascending);
            builder.AddAttribute(4, nameof(NTTemplateColumn<TestGridItem>.ChildContent), (RenderFragment<TestGridItem>)(item => cellBuilder => cellBuilder.AddContent(0, item.Name)));
            builder.CloseComponent();
        };
        var cut = Render<NTDataGrid<TestGridItem>>(parameters => parameters
            .Add(grid => grid.ItemsProvider, provider)
            .Add(grid => grid.ChildContent, columns));

        cut.WaitForAssertion(() => captured.Last().Sorts.Should().Equal(new NTSortDescriptor("Id", SortDirection.Ascending)));
        var cachedSorts = captured.Last().Sorts;
        sortBy = NTGridSort<TestGridItem>.ByAscending(item => item.Name);

        cut.Render(parameters => parameters
            .Add(grid => grid.ItemsProvider, provider)
            .Add(grid => grid.ChildContent, columns));

        cut.WaitForAssertion(() => {
            captured.Last().Sorts.Should().NotBeSameAs(cachedSorts);
            captured.Last().Sorts.Should().Equal(new NTSortDescriptor("Name", SortDirection.Ascending));
        });
    }

    [Fact]
    public void PropertyColumn_Uses_Property_Name_For_Provider_Sort_Instead_Of_Display_Title() {
        var captured = new List<NTDataGridItemsProviderRequest<TestGridItem>>();
        var cut = Render<NTDataGrid<TestGridItem>>(parameters => parameters
            .Add(grid => grid.ItemsProvider, request => {
                captured.Add(request);
                return ValueTask.FromResult(new NTItemsProviderResult<TestGridItem>(_items.ToArray(), _items.Count()));
            })
            .Add(grid => grid.ChildContent, builder => {
                builder.OpenComponent<NTPropertyColumn<TestGridItem, int>>(0);
                builder.AddAttribute(1, nameof(NTPropertyColumn<TestGridItem, int>.Title), "Days Open");
                builder.AddAttribute(2, nameof(NTPropertyColumn<TestGridItem, int>.Property), (System.Linq.Expressions.Expression<Func<TestGridItem, int>>)(item => item.DaysOpen));
                builder.CloseComponent();
            }));

        cut.WaitForAssertion(() => cut.Find("th").TextContent.Should().Contain("Days Open"));
        cut.Find(".nt-data-grid-sort-link").Click();

        cut.WaitForAssertion(() => {
            captured.Last().Sorts.Should().Equal(new NTSortDescriptor("DaysOpen", SortDirection.Ascending));
        });
    }

    [Fact]
    public void TemplateColumn_WithDescendingInitialSort_Passes_Descending_Provider_Descriptor() {
        var captured = new List<NTDataGridItemsProviderRequest<TestGridItem>>();
        var sortBy = NTGridSort<TestGridItem>.ByDescending(item => item.Id);
        var cut = Render<NTDataGrid<TestGridItem>>(parameters => parameters
            .Add(grid => grid.ItemsProvider, request => {
                captured.Add(request);
                return ValueTask.FromResult(new NTItemsProviderResult<TestGridItem>(_items.ToArray(), _items.Count()));
            })
            .Add(grid => grid.ChildContent, builder => {
                builder.OpenComponent<NTTemplateColumn<TestGridItem>>(0);
                builder.AddAttribute(1, nameof(NTTemplateColumn<TestGridItem>.Title), "Custom name");
                builder.AddAttribute(2, nameof(NTTemplateColumn<TestGridItem>.SortBy), sortBy);
                builder.AddAttribute(3, nameof(NTTemplateColumn<TestGridItem>.InitialSortDirection), SortDirection.Descending);
                builder.AddAttribute(4, nameof(NTTemplateColumn<TestGridItem>.ChildContent), (RenderFragment<TestGridItem>)(item => cellBuilder => cellBuilder.AddContent(0, item.Name)));
                builder.CloseComponent();
            }));

        cut.WaitForAssertion(() => {
            captured.Last().Sorts.Should().Equal(new NTSortDescriptor("Id", SortDirection.Descending));
            cut.Find("th").GetAttribute("aria-sort").Should().Be("descending");
        });
    }

    [Fact]
    public void EmptyItems_Render_Empty_State() {
        var cut = Render<NTDataGrid<TestGridItem>>(parameters => parameters
            .Add(grid => grid.Items, Array.Empty<TestGridItem>().AsQueryable())
            .Add(grid => grid.EmptyText, "Nothing here")
            .Add(grid => grid.ChildContent, DefaultColumns));

        cut.WaitForAssertion(() => {
            cut.Find(".nt-data-grid-row-empty").TextContent.Should().Contain("Nothing here");
            cut.Find(".nt-data-grid-row-empty td").GetAttribute("colspan").Should().Be("3");
        });
    }

    [Fact]
    public void Header_Sort_Control_Replaces_Url_Without_Navigating_When_Interactive() {
        var navigationManager = (BunitNavigationManager)Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo("https://example.test/orders?existing=true&ntdg-page=5");
        var navigationCount = navigationManager.History.Count;
        var cut = RenderGrid();

        cut.WaitForAssertion(() => {
            var sortControl = cut.Find(".nt-data-grid-sort-link");
            sortControl.TagName.Should().Be("BUTTON");
            sortControl.GetAttribute("type").Should().Be("button");
            sortControl.HasAttribute("href").Should().BeFalse();
        });

        cut.Find(".nt-data-grid-sort-link").Click();

        cut.WaitForAssertion(() => {
            var invocation = JSInterop.Invocations.Last(invocation => invocation.Identifier == "NTComponents.updateUri");
            var uri = invocation.Arguments[0].Should().BeOfType<string>().Subject;
            uri.Should().Contain("existing=true");
            uri.Should().Contain("ntdg-page=1");
            uri.Should().Contain("ntdg-sort=Name%3Aasc");
            typeof(NTDataGrid<TestGridItem>).GetField("_currentUri", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(cut.Instance).Should().Be(uri);
            navigationManager.History.Should().HaveCount(navigationCount);
        });
    }

    [Fact]
    public void Header_Sort_Link_Uses_Query_Parameters_For_Static_Ssr() {
        SetRendererInfo(new RendererInfo("Static", false));
        Services.GetRequiredService<NavigationManager>().NavigateTo("https://example.test/orders?existing=true");
        var cut = RenderGrid();

        cut.WaitForAssertion(() => {
            var link = cut.Find(".nt-data-grid-sort-link");
            link.TagName.Should().Be("A");
            link.GetAttribute("href").Should().Contain("ntdg-sort=Name%3Aasc");
            link.GetAttribute("href").Should().Contain("ntdg-page=1");
            link.GetAttribute("href").Should().NotContain("ntdg-pageSize");
            link.GetAttribute("href").Should().Contain("existing=true");
        });
    }

    [Fact]
    public void Header_Sort_Link_Does_Not_Render_Icon_When_Unsorted() {
        var cut = RenderGrid();

        cut.WaitForAssertion(() => {
            cut.Find(".nt-data-grid-sort-link").ClassList.Should().NotContain("nt-data-grid-sort-link-sorted");
            cut.FindAll(".nt-data-grid-sort-indicator").Should().BeEmpty();
        });
    }

    [Fact]
    public void Header_Sort_Link_Renders_Material_Icon_When_Sorted() {
        var cut = RenderGrid(columns: builder => {
            builder.OpenComponent<NTPropertyColumn<TestGridItem, string>>(0);
            builder.AddAttribute(1, nameof(NTPropertyColumn<TestGridItem, string>.Title), "Name");
            builder.AddAttribute(2, nameof(NTPropertyColumn<TestGridItem, string>.Property), (System.Linq.Expressions.Expression<Func<TestGridItem, string>>)(item => item.Name));
            builder.AddAttribute(3, nameof(NTPropertyColumn<TestGridItem, string>.InitialSortDirection), SortDirection.Ascending);
            builder.CloseComponent();
        });

        cut.WaitForAssertion(() => {
            var link = cut.Find(".nt-data-grid-sort-link");
            var indicator = cut.Find(".nt-data-grid-sort-indicator");
            link.ClassList.Should().Contain("nt-data-grid-sort-link-sorted");
            indicator.ClassList.Should().Contain("tnt-icon");
            indicator.ClassList.Should().Contain("material-symbols-outlined");
            indicator.ClassList.Should().Contain("nt-data-grid-sort-indicator-ascending");
            indicator.TextContent.Should().Be("arrow_drop_down");
            cut.FindAll(".nt-data-grid-sort-order").Should().BeEmpty();
        });
    }

    [Fact]
    public void Header_Sort_Link_Renders_Sort_Order_When_Multiple_Columns_Are_Sorted() {
        var cut = RenderGrid(columns: builder => {
            builder.OpenComponent<NTPropertyColumn<TestGridItem, string>>(0);
            builder.AddAttribute(1, nameof(NTPropertyColumn<TestGridItem, string>.Title), "Name");
            builder.AddAttribute(2, nameof(NTPropertyColumn<TestGridItem, string>.Property), (System.Linq.Expressions.Expression<Func<TestGridItem, string>>)(item => item.Name));
            builder.AddAttribute(3, nameof(NTPropertyColumn<TestGridItem, string>.InitialSortDirection), SortDirection.Ascending);
            builder.CloseComponent();
            builder.OpenComponent<NTPropertyColumn<TestGridItem, DateOnly>>(4);
            builder.AddAttribute(5, nameof(NTPropertyColumn<TestGridItem, DateOnly>.Title), "Created");
            builder.AddAttribute(6, nameof(NTPropertyColumn<TestGridItem, DateOnly>.Property), (System.Linq.Expressions.Expression<Func<TestGridItem, DateOnly>>)(item => item.Created));
            builder.AddAttribute(7, nameof(NTPropertyColumn<TestGridItem, DateOnly>.InitialSortDirection), SortDirection.Descending);
            builder.CloseComponent();
        });

        cut.WaitForAssertion(() => {
            var orders = cut.FindAll(".nt-data-grid-sort-order");
            orders.Should().HaveCount(2);
            orders[0].TextContent.Should().Be("1");
            orders[0].GetAttribute("aria-label").Should().Be("Sort priority 1");
            orders[1].TextContent.Should().Be("2");
            orders[1].GetAttribute("aria-label").Should().Be("Sort priority 2");
            cut.FindAll(".nt-data-grid-sort-link-multi-sorted").Should().HaveCount(2);
        });
    }

    [Fact]
    public void Header_Sort_Link_Does_Not_Render_Multiple_Sort_Order_When_Multi_Sort_Is_Disabled() {
        var cut = RenderGrid(parameters => parameters.Add(grid => grid.AllowMultiSort, false), columns: builder => {
            builder.OpenComponent<NTPropertyColumn<TestGridItem, string>>(0);
            builder.AddAttribute(1, nameof(NTPropertyColumn<TestGridItem, string>.Title), "Name");
            builder.AddAttribute(2, nameof(NTPropertyColumn<TestGridItem, string>.Property), (System.Linq.Expressions.Expression<Func<TestGridItem, string>>)(item => item.Name));
            builder.AddAttribute(3, nameof(NTPropertyColumn<TestGridItem, string>.InitialSortDirection), SortDirection.Ascending);
            builder.CloseComponent();
            builder.OpenComponent<NTPropertyColumn<TestGridItem, DateOnly>>(4);
            builder.AddAttribute(5, nameof(NTPropertyColumn<TestGridItem, DateOnly>.Title), "Created");
            builder.AddAttribute(6, nameof(NTPropertyColumn<TestGridItem, DateOnly>.Property), (System.Linq.Expressions.Expression<Func<TestGridItem, DateOnly>>)(item => item.Created));
            builder.AddAttribute(7, nameof(NTPropertyColumn<TestGridItem, DateOnly>.InitialSortDirection), SortDirection.Descending);
            builder.CloseComponent();
        });

        cut.WaitForAssertion(() => {
            cut.FindAll(".nt-data-grid-sort-order").Should().BeEmpty();
            cut.FindAll(".nt-data-grid-sort-link-sorted").Should().HaveCount(1);
            cut.FindAll("th")[0].GetAttribute("aria-sort").Should().Be("ascending");
            cut.FindAll("th")[1].HasAttribute("aria-sort").Should().BeFalse();
        });
    }

    [Fact]
    public void Header_Click_Sorts_Direct_Items_In_Place() {
        var navigationManager = (BunitNavigationManager)Services.GetRequiredService<NavigationManager>();
        var cut = RenderGrid();
        var uri = navigationManager.Uri;

        cut.WaitForAssertion(() => cut.FindAll("tbody tr").Should().HaveCount(3));
        cut.Find(".nt-data-grid-sort-link").Click();

        cut.WaitForAssertion(() => {
            cut.FindAll("tbody tr")[0].TextContent.Should().Contain("Alpha");
            navigationManager.Uri.Should().Be(uri);
            navigationManager.History.Should().BeEmpty();
        });
    }

    [Fact]
    public void Header_Click_Cycles_Tri_State() {
        var cut = RenderGrid();

        cut.WaitForAssertion(() => cut.FindAll(".nt-data-grid-sort-indicator").Should().BeEmpty());

        cut.Find(".nt-data-grid-sort-link").Click();
        cut.WaitForAssertion(() => {
            cut.Find("th").GetAttribute("aria-sort").Should().Be("ascending");
            cut.Find(".nt-data-grid-sort-indicator").ClassList.Should().Contain("nt-data-grid-sort-indicator-ascending");
            cut.FindAll("tbody tr")[0].TextContent.Should().Contain("Alpha");
        });

        cut.Find(".nt-data-grid-sort-link").Click();
        cut.WaitForAssertion(() => {
            cut.Find("th").GetAttribute("aria-sort").Should().Be("descending");
            var indicator = cut.Find(".nt-data-grid-sort-indicator");
            indicator.ClassList.Should().Contain("nt-data-grid-sort-indicator-descending");
            indicator.ClassList.Should().NotContain("nt-data-grid-sort-indicator-ascending");
            cut.FindAll("tbody tr")[0].TextContent.Should().Contain("Gamma");
        });

        cut.Find(".nt-data-grid-sort-link").Click();
        cut.WaitForAssertion(() => {
            cut.Find("th").HasAttribute("aria-sort").Should().BeFalse();
            cut.Find(".nt-data-grid-sort-link").ClassList.Should().NotContain("nt-data-grid-sort-link-sorted");
            cut.FindAll(".nt-data-grid-sort-indicator").Should().BeEmpty();
            cut.Find(".nt-data-grid-sort-link").HasAttribute("href").Should().BeFalse();
        });
    }

    [Fact]
    public void Header_Click_Replaces_Current_Column_Sort_When_Multi_Sort_Is_Disabled() {
        var captured = new List<NTDataGridItemsProviderRequest<TestGridItem>>();
        var cut = Render<NTDataGrid<TestGridItem>>(parameters => parameters
            .Add(grid => grid.ItemsProvider, request => {
                captured.Add(request);
                return ValueTask.FromResult(new NTItemsProviderResult<TestGridItem>(_items.ToArray(), _items.Count()));
            })
            .Add(grid => grid.AllowMultiSort, false)
            .Add(grid => grid.ChildContent, builder => {
                builder.OpenComponent<NTPropertyColumn<TestGridItem, string>>(0);
                builder.AddAttribute(1, nameof(NTPropertyColumn<TestGridItem, string>.Title), "Name");
                builder.AddAttribute(2, nameof(NTPropertyColumn<TestGridItem, string>.Property), (System.Linq.Expressions.Expression<Func<TestGridItem, string>>)(item => item.Name));
                builder.AddAttribute(3, nameof(NTPropertyColumn<TestGridItem, string>.InitialSortDirection), SortDirection.Ascending);
                builder.CloseComponent();
                builder.OpenComponent<NTPropertyColumn<TestGridItem, DateOnly>>(4);
                builder.AddAttribute(5, nameof(NTPropertyColumn<TestGridItem, DateOnly>.Title), "Created");
                builder.AddAttribute(6, nameof(NTPropertyColumn<TestGridItem, DateOnly>.Property), (System.Linq.Expressions.Expression<Func<TestGridItem, DateOnly>>)(item => item.Created));
                builder.CloseComponent();
            }));

        cut.WaitForAssertion(() => captured.Should().Contain(request => request.Sorts.Count == 1 && request.Sorts[0].PropertyName == "Name"));

        var createdSortLink = cut.FindAll(".nt-data-grid-sort-link")[1];
        createdSortLink.HasAttribute("href").Should().BeFalse();
        createdSortLink.Click();

        cut.WaitForAssertion(() => {
            var latestSorts = captured.Last().Sorts;
            latestSorts.Should().ContainSingle();
            latestSorts[0].PropertyName.Should().Be("Created");
            latestSorts[0].Direction.Should().Be(SortDirection.Ascending);
            cut.FindAll(".nt-data-grid-sort-link-sorted").Should().HaveCount(1);
            cut.FindAll("th")[0].HasAttribute("aria-sort").Should().BeFalse();
            cut.FindAll("th")[1].GetAttribute("aria-sort").Should().Be("ascending");
        });
    }

    [Fact]
    public void Header_Uses_Body_Text_Alignment_When_Header_Text_Alignment_Is_Not_Set() {
        var cut = RenderGrid(columns: builder => {
            builder.OpenComponent<NTPropertyColumn<TestGridItem, string>>(0);
            builder.AddAttribute(1, nameof(NTPropertyColumn<TestGridItem, string>.Title), "Name");
            builder.AddAttribute(2, nameof(NTPropertyColumn<TestGridItem, string>.Property), (System.Linq.Expressions.Expression<Func<TestGridItem, string>>)(item => item.Name));
            builder.AddAttribute(3, nameof(NTPropertyColumn<TestGridItem, string>.TextAlign), TextAlign.Left);
            builder.CloseComponent();
            builder.OpenComponent<NTPropertyColumn<TestGridItem, decimal>>(4);
            builder.AddAttribute(5, nameof(NTPropertyColumn<TestGridItem, decimal>.Title), "Amount");
            builder.AddAttribute(6, nameof(NTPropertyColumn<TestGridItem, decimal>.Property), (System.Linq.Expressions.Expression<Func<TestGridItem, decimal>>)(item => item.Amount));
            builder.AddAttribute(7, nameof(NTPropertyColumn<TestGridItem, decimal>.TextAlign), TextAlign.Right);
            builder.AddAttribute(8, nameof(NTPropertyColumn<TestGridItem, decimal>.HeaderTextAlign), TextAlign.Left);
            builder.CloseComponent();
        });

        cut.WaitForAssertion(() => {
            var headers = cut.FindAll("th");
            var firstRowCells = cut.FindAll("tbody tr")[0].QuerySelectorAll("td");
            headers[0].ClassList.Should().Contain("nt-data-grid-align-left");
            firstRowCells[0].ClassList.Should().Contain("nt-data-grid-align-left");
            firstRowCells[0].QuerySelector(".nt-data-grid-cell-content").Should().NotBeNull();
            headers[1].ClassList.Should().Contain("nt-data-grid-align-left");
            firstRowCells[1].ClassList.Should().Contain("nt-data-grid-align-right");
            firstRowCells[1].QuerySelector(".nt-data-grid-cell-content").Should().NotBeNull();
        });
    }

    [Fact]
    public void Href_Generation_Reuses_The_Parsed_Uri_State() {
        Services.GetRequiredService<NavigationManager>().NavigateTo("https://example.test/orders?filter=hello%20world&ntdg-pageSize=99");
        var cut = Render<NTDataGrid<TestGridItem>>(parameters => parameters
            .Add(grid => grid.Items, _items)
            .Add(grid => grid.ShowPagination, true)
            .Add(grid => grid.PageSize, 1)
            .Add(grid => grid.ChildContent, DefaultColumns));
        var buildPageHref = typeof(NTDataGrid<TestGridItem>).GetMethod("BuildPageHref", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        var uriStateField = typeof(NTDataGrid<TestGridItem>).GetField("_uriState", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;

        var first = (string)buildPageHref.Invoke(cut.Instance, [1])!;
        var firstUriState = uriStateField.GetValue(cut.Instance);
        var second = (string)buildPageHref.Invoke(cut.Instance, [1])!;

        firstUriState.Should().NotBeNull();
        uriStateField.GetValue(cut.Instance).Should().BeSameAs(firstUriState);
        second.Should().Be(first);
        first.Should().Contain("filter=hello%20world");
        first.Should().Contain("ntdg-page=2");
        first.Should().NotContain("ntdg-pageSize");
    }

    [Fact]
    public void Page_Size_Options_Are_Normalized_Once_Per_Input_State() {
        IReadOnlyList<int> pageSizeOptions = [50, 10, 10, 0];
        var cut = Render<NTDataGrid<TestGridItem>>(parameters => parameters
            .Add(grid => grid.Items, _items)
            .Add(grid => grid.ShowPagination, true)
            .Add(grid => grid.PageSize, 25)
            .Add(grid => grid.PageSizeOptions, pageSizeOptions)
            .Add(grid => grid.ChildContent, DefaultColumns));
        var getOptions = typeof(NTDataGrid<TestGridItem>).GetMethod("GetResolvedPageSizeOptions", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;

        var first = (IReadOnlyList<int>)getOptions.Invoke(cut.Instance, null)!;
        var second = (IReadOnlyList<int>)getOptions.Invoke(cut.Instance, null)!;

        first.Should().Equal(10, 25, 50);
        second.Should().BeSameAs(first);

        pageSizeOptions = [5, 25];
        cut.Render(parameters => parameters
            .Add(grid => grid.Items, _items)
            .Add(grid => grid.ShowPagination, true)
            .Add(grid => grid.PageSize, 25)
            .Add(grid => grid.PageSizeOptions, pageSizeOptions)
            .Add(grid => grid.ChildContent, DefaultColumns));
        var updated = (IReadOnlyList<int>)getOptions.Invoke(cut.Instance, null)!;

        updated.Should().Equal(5, 25);
        updated.Should().NotBeSameAs(first);
    }

    [Fact]
    public void Cell_Classes_Update_When_Alignment_Parameters_Change() {
        var textAlign = TextAlign.Left;
        var headerTextAlign = TextAlign.Right;
        RenderFragment columns = builder => {
            builder.OpenComponent<NTPropertyColumn<TestGridItem, string>>(0);
            builder.AddAttribute(1, nameof(NTPropertyColumn<TestGridItem, string>.Title), "Name");
            builder.AddAttribute(2, nameof(NTPropertyColumn<TestGridItem, string>.Property), (System.Linq.Expressions.Expression<Func<TestGridItem, string>>)(item => item.Name));
            builder.AddAttribute(3, nameof(NTPropertyColumn<TestGridItem, string>.TextAlign), textAlign);
            builder.AddAttribute(4, nameof(NTPropertyColumn<TestGridItem, string>.HeaderTextAlign), headerTextAlign);
            builder.CloseComponent();
        };
        var cut = RenderGrid(columns: columns);

        cut.WaitForAssertion(() => {
            cut.Find("th").ClassList.Should().Contain("nt-data-grid-align-right");
            cut.Find("tbody td").ClassList.Should().Contain("nt-data-grid-align-left");
        });

        textAlign = TextAlign.Center;
        headerTextAlign = TextAlign.Justify;
        cut.Render(parameters => parameters
            .Add(grid => grid.Items, _items)
            .Add(grid => grid.Caption, "Invoices")
            .Add(grid => grid.ChildContent, columns));

        cut.WaitForAssertion(() => {
            cut.Find("th").ClassList.Should().Contain("nt-data-grid-align-justify").And.NotContain("nt-data-grid-align-right");
            cut.Find("tbody td").ClassList.Should().Contain("nt-data-grid-align-center").And.NotContain("nt-data-grid-align-left");
        });
    }

    [Fact]
    public void Rows_Are_Not_Interactive_When_Row_Click_Callback_Is_Not_Set() {
        var cut = RenderGrid();

        cut.WaitForAssertion(() => {
            var row = cut.Find("tbody tr");
            row.ClassList.Should().NotContain("nt-data-grid-row-clickable");
            row.HasAttribute("tabindex").Should().BeFalse();
        });
    }

    [Fact]
    public void Row_Click_Invokes_Row_Click_Callback() {
        TestGridItem? clicked = null;
        var cut = RenderGrid(parameters => parameters.Add(grid => grid.OnRowClicked, EventCallback.Factory.Create<TestGridItem>(this, item => clicked = item)));

        cut.WaitForAssertion(() => cut.Find("tbody tr").ClassList.Should().Contain("nt-data-grid-row-clickable"));
        cut.Find("tbody tr").Click();

        clicked.Should().Be(_items.First());
    }

    [Fact]
    public async Task Row_Enter_Key_Invokes_Row_Click_Callback() {
        TestGridItem? clicked = null;
        var cut = RenderGrid(parameters => parameters.Add(grid => grid.OnRowClicked, EventCallback.Factory.Create<TestGridItem>(this, item => clicked = item)));

        cut.WaitForAssertion(() => cut.Find("tbody tr").GetAttribute("tabindex").Should().Be("0"));
        await cut.Find("tbody tr").KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        clicked.Should().Be(_items.First());
    }

    [Fact]
    public void Striped_Appearance_Direct_Items_Adds_Stable_Row_Stripe_Classes() {
        var cut = RenderGrid(parameters => parameters.Add(grid => grid.Appearance, NTDataGridAppearance.Striped));

        cut.WaitForAssertion(() => {
            cut.Find(".nt-data-grid").ClassList.Should().Contain("nt-data-grid-striped");
            var rows = cut.FindAll("tbody tr");
            rows[0].ClassList.Should().Contain("nt-data-grid-row-striped-odd");
            rows[1].ClassList.Should().Contain("nt-data-grid-row-striped-even");
            rows[2].ClassList.Should().Contain("nt-data-grid-row-striped-odd");
        });
    }

    [Fact]
    public void Striped_Appearance_Provider_Page_Uses_Data_Index_Not_Dom_Position() {
        var cut = Render<NTDataGrid<TestGridItem>>(parameters => parameters
            .Add(grid => grid.ItemsProvider, request => {
                var items = _items.Skip(request.StartIndex).Take(request.Count ?? 10).ToArray();
                return ValueTask.FromResult(new NTItemsProviderResult<TestGridItem>(items, _items.Count()));
            })
            .Add(grid => grid.ShowPagination, true)
            .Add(grid => grid.PageSize, 2)
            .Add(grid => grid.Appearance, NTDataGridAppearance.Striped)
            .Add(grid => grid.ChildContent, DefaultColumns));

        cut.WaitForAssertion(() => cut.FindAll("tbody tr")[1].ClassList.Should().Contain("nt-data-grid-row-striped-even"));
        cut.Find(".nt-data-grid-pagination-buttons .pagination-next-page").Click();

        cut.WaitForAssertion(() => {
            var row = cut.Find("tbody tr");
            row.TextContent.Should().Contain("Beta");
            row.ClassList.Should().Contain("nt-data-grid-row-striped-odd");
        });
    }

    [Fact]
    public void Pagination_Renders_Links_And_Requests_Page_Range() {
        var navigationManager = (BunitNavigationManager)Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo("https://example.test/orders?existing=true");
        var navigationCount = navigationManager.History.Count;
        var captured = new List<NTDataGridItemsProviderRequest<TestGridItem>>();
        var cut = Render<NTDataGrid<TestGridItem>>(parameters => parameters
            .Add(grid => grid.ItemsProvider, request => {
                captured.Add(request);
                var items = _items.Skip(request.StartIndex).Take(request.Count ?? 10).ToArray();
                return ValueTask.FromResult(new NTItemsProviderResult<TestGridItem>(items, _items.Count()));
            })
            .Add(grid => grid.ShowPagination, true)
            .Add(grid => grid.PageSize, 2)
            .Add(grid => grid.ChildContent, DefaultColumns));

        cut.WaitForAssertion(() => {
            captured.Should().Contain(request => request.StartIndex == 0 && request.Count == 2);
            cut.FindAll(".nt-data-grid-pagination-buttons .pagination-btn").Should().HaveCount(6);
            cut.Find(".nt-data-grid-pagination-buttons .current-page").TextContent.Should().Contain("1");
            cut.Find(".nt-data-grid-pagination-buttons .pagination-next-page").GetAttribute("href").Should().Contain("ntdg-page=2");
            cut.Find(".nt-data-grid-pagination-buttons .pagination-next-page").GetAttribute("href").Should().NotContain("ntdg-pageSize");
            cut.FindAll(".nt-data-grid-page-status").Should().BeEmpty();
            cut.Find(".nt-data-grid-page-size select").GetAttribute("value").Should().Be("2");
        });

        cut.Find(".nt-data-grid-pagination-buttons .pagination-next-page").Click();

        cut.WaitForAssertion(() => {
            captured.Should().Contain(request => request.StartIndex == 2 && request.Count == 2);
            var invocation = JSInterop.Invocations.Last(invocation => invocation.Identifier == "NTComponents.updateUri");
            var uri = invocation.Arguments[0].Should().BeOfType<string>().Subject;
            uri.Should().Contain("existing=true");
            uri.Should().Contain("ntdg-page=2");
            navigationManager.History.Should().HaveCount(navigationCount);
        });
    }

    [Fact]
    public void Page_Size_Select_Changes_Page_Size_And_Resets_To_First_Page() {
        var captured = new List<NTDataGridItemsProviderRequest<TestGridItem>>();
        var pageSizeChanged = 0;
        var pageIndexChanged = -1;
        var cut = Render<NTDataGrid<TestGridItem>>(parameters => parameters
            .Add(grid => grid.ItemsProvider, request => {
                captured.Add(request);
                var items = _items.Skip(request.StartIndex).Take(request.Count ?? 10).ToArray();
                return ValueTask.FromResult(new NTItemsProviderResult<TestGridItem>(items, _items.Count()));
            })
            .Add(grid => grid.ShowPagination, true)
            .Add(grid => grid.PageIndex, 1)
            .Add(grid => grid.PageSize, 2)
            .Add(grid => grid.PageSizeOptions, [1, 2, 3])
            .Add(grid => grid.PageSizeChanged, EventCallback.Factory.Create<int>(this, value => pageSizeChanged = value))
            .Add(grid => grid.PageIndexChanged, EventCallback.Factory.Create<int>(this, value => pageIndexChanged = value))
            .Add(grid => grid.ChildContent, DefaultColumns));

        cut.WaitForAssertion(() => captured.Should().Contain(request => request.StartIndex == 2 && request.Count == 2));
        cut.Find(".nt-data-grid-page-size select").Change("3");

        cut.WaitForAssertion(() => {
            pageSizeChanged.Should().Be(3);
            pageIndexChanged.Should().Be(0);
            captured.Should().Contain(request => request.StartIndex == 0 && request.Count == 3);
            var invocation = JSInterop.Invocations.Last(invocation => invocation.Identifier == "NTComponents.updateUri");
            invocation.Arguments[0].Should().BeOfType<string>().Subject.Should().Contain("ntdg-page=1");
        });
    }

    [Fact]
    public void Pagination_Query_Page_Uses_One_Based_Page_Number_And_Ignores_Page_Size() {
        Services.GetRequiredService<NavigationManager>().NavigateTo("https://example.test/orders?ntdg-page=2&ntdg-pageSize=99");
        var captured = new List<NTDataGridItemsProviderRequest<TestGridItem>>();
        var cut = Render<NTDataGrid<TestGridItem>>(parameters => parameters
            .Add(grid => grid.ItemsProvider, request => {
                captured.Add(request);
                var items = _items.Skip(request.StartIndex).Take(request.Count ?? 10).ToArray();
                return ValueTask.FromResult(new NTItemsProviderResult<TestGridItem>(items, _items.Count()));
            })
            .Add(grid => grid.ShowPagination, true)
            .Add(grid => grid.PageSize, 2)
            .Add(grid => grid.ChildContent, DefaultColumns));

        cut.WaitForAssertion(() => {
            captured.Should().Contain(request => request.StartIndex == 2 && request.Count == 2);
            cut.Find(".nt-data-grid-pagination-buttons .current-page").TextContent.Should().Contain("2");
            cut.Find(".nt-data-grid-pagination-buttons .pagination-previous-page").GetAttribute("href").Should().Contain("ntdg-page=1");
            cut.Find(".nt-data-grid-pagination-buttons .pagination-previous-page").GetAttribute("href").Should().NotContain("ntdg-pageSize");
        });
    }

    [Fact]
    public void TotalItemCountChanged_Fires_When_Provider_Count_Changes() {
        var total = 0;
        var cut = Render<NTDataGrid<TestGridItem>>(parameters => parameters
            .Add(grid => grid.ItemsProvider, request => ValueTask.FromResult(new NTItemsProviderResult<TestGridItem>(_items.ToArray(), 42)))
            .Add(grid => grid.TotalItemCountChanged, EventCallback.Factory.Create<int>(this, value => total = value))
            .Add(grid => grid.ChildContent, DefaultColumns));

        cut.WaitForAssertion(() => total.Should().Be(42));
    }

    [Fact]
    public async Task RefreshDataGridAsync_Requeries_ItemsProvider() {
        var calls = 0;
        var useSecondItems = false;
        var firstItems = new[] { new TestGridItem(1, "Old", new DateOnly(2026, 1, 1), 1) };
        var secondItems = new[] { new TestGridItem(2, "New", new DateOnly(2026, 1, 2), 2) };
        var cut = Render<NTDataGrid<TestGridItem>>(parameters => parameters
            .Add(grid => grid.ItemsProvider, _ => {
                calls++;
                var items = useSecondItems ? secondItems : firstItems;
                return ValueTask.FromResult(new NTItemsProviderResult<TestGridItem>(items, items.Length));
            })
            .Add(grid => grid.ChildContent, DefaultColumns));

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Old"));
        var initialCalls = calls;
        useSecondItems = true;
        await cut.InvokeAsync(() => cut.Instance.RefreshDataGridAsync());

        cut.WaitForAssertion(() => {
            calls.Should().BeGreaterThan(initialCalls);
            cut.Markup.Should().Contain("New");
            cut.Markup.Should().NotContain("Old");
        });
    }

    [Fact]
    public async Task NonVirtual_Refresh_Cancels_Superseded_Provider_And_Ignores_Its_Result() {
        var pending = new List<(NTDataGridItemsProviderRequest<TestGridItem> Request, TaskCompletionSource<NTItemsProviderResult<TestGridItem>> Completion)>();
        var holdRequests = false;
        var initialItems = new[] { new TestGridItem(1, "Initial", new DateOnly(2026, 1, 1), 1) };
        var cut = Render<NTDataGrid<TestGridItem>>(parameters => parameters
            .Add(grid => grid.ItemsProvider, request => {
                if (!holdRequests) {
                    return ValueTask.FromResult(new NTItemsProviderResult<TestGridItem>(initialItems, initialItems.Length));
                }

                var completion = new TaskCompletionSource<NTItemsProviderResult<TestGridItem>>(TaskCreationOptions.RunContinuationsAsynchronously);
                pending.Add((request, completion));
                return new ValueTask<NTItemsProviderResult<TestGridItem>>(completion.Task);
            })
            .Add(grid => grid.ChildContent, DefaultColumns));
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Initial"));
        holdRequests = true;

        var firstRefresh = cut.InvokeAsync(() => cut.Instance.RefreshDataGridAsync());
        cut.WaitForAssertion(() => pending.Should().HaveCount(1));
        var secondRefresh = cut.InvokeAsync(() => cut.Instance.RefreshDataGridAsync());
        cut.WaitForAssertion(() => pending.Should().HaveCount(2));

        pending[0].Request.CancellationToken.IsCancellationRequested.Should().BeTrue();
        pending[1].Request.CancellationToken.IsCancellationRequested.Should().BeFalse();

        var newestItems = new[] { new TestGridItem(2, "Newest", new DateOnly(2026, 1, 2), 2) };
        pending[1].Completion.SetResult(new NTItemsProviderResult<TestGridItem>(newestItems, newestItems.Length));
        await secondRefresh;
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Newest"));

        var staleItems = new[] { new TestGridItem(3, "Stale", new DateOnly(2026, 1, 3), 3) };
        pending[0].Completion.SetResult(new NTItemsProviderResult<TestGridItem>(staleItems, staleItems.Length));
        await firstRefresh;

        cut.Markup.Should().Contain("Newest");
        cut.Markup.Should().NotContain("Stale");
    }

    [Fact]
    public async Task NonVirtual_Dispose_Cancels_Provider_And_Does_Not_Swallow_Disposal_Cancellation() {
        var pending = new TaskCompletionSource<NTItemsProviderResult<TestGridItem>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var pendingToken = CancellationToken.None;
        var holdRequests = false;
        var initialItems = new[] { new TestGridItem(1, "Initial", new DateOnly(2026, 1, 1), 1) };
        var cut = Render<NTDataGrid<TestGridItem>>(parameters => parameters
            .Add(grid => grid.ItemsProvider, request => {
                if (!holdRequests) {
                    return ValueTask.FromResult(new NTItemsProviderResult<TestGridItem>(initialItems, initialItems.Length));
                }

                pendingToken = request.CancellationToken;
                return new ValueTask<NTItemsProviderResult<TestGridItem>>(pending.Task);
            })
            .Add(grid => grid.ChildContent, DefaultColumns));
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Initial"));
        holdRequests = true;
        var refresh = cut.InvokeAsync(() => cut.Instance.RefreshDataGridAsync());
        cut.WaitForAssertion(() => pendingToken.CanBeCanceled.Should().BeTrue());

        cut.Instance.Dispose();

        pendingToken.IsCancellationRequested.Should().BeTrue();
        pending.SetResult(new NTItemsProviderResult<TestGridItem>(initialItems, initialItems.Length));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => refresh);
        Record.Exception(cut.Instance.Dispose).Should().BeNull();
    }

    [Fact]
    public async Task Superseding_Refresh_Disposes_Its_Source_When_Previous_Cancellation_Callback_Throws() {
        var pending = new TaskCompletionSource<NTItemsProviderResult<TestGridItem>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var refreshCancellationField = typeof(NTDataGrid<TestGridItem>).GetField("_refreshCancellation", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        NTDataGrid<TestGridItem>? grid = null;
        CancellationTokenSource? failedRefreshCancellation = null;
        var cancellationCallbackRegistered = false;
        var holdRequests = false;
        var initialItems = new[] { new TestGridItem(1, "Initial", new DateOnly(2026, 1, 1), 1) };
        var cut = Render<NTDataGrid<TestGridItem>>(parameters => parameters
            .Add(component => component.ItemsProvider, request => {
                if (!holdRequests) {
                    return ValueTask.FromResult(new NTItemsProviderResult<TestGridItem>(initialItems, initialItems.Length));
                }

                request.CancellationToken.Register(() => {
                    failedRefreshCancellation = (CancellationTokenSource?)refreshCancellationField.GetValue(grid);
                    throw new InvalidOperationException("Cancellation callback failed.");
                });
                cancellationCallbackRegistered = true;
                return new ValueTask<NTItemsProviderResult<TestGridItem>>(pending.Task);
            })
            .Add(component => component.ChildContent, DefaultColumns));
        grid = cut.Instance;
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Initial"));
        holdRequests = true;
        var firstRefresh = cut.InvokeAsync(() => cut.Instance.RefreshDataGridAsync());
        cut.WaitForAssertion(() => cancellationCallbackRegistered.Should().BeTrue());

        var secondRefresh = cut.InvokeAsync(() => cut.Instance.RefreshDataGridAsync());

        await Assert.ThrowsAsync<AggregateException>(() => secondRefresh);
        failedRefreshCancellation.Should().NotBeNull();
        Action readFailedToken = () => _ = failedRefreshCancellation!.Token;
        readFailedToken.Should().Throw<ObjectDisposedException>();
        refreshCancellationField.GetValue(cut.Instance).Should().BeNull();

        pending.SetResult(new NTItemsProviderResult<TestGridItem>(initialItems, initialItems.Length));
        await firstRefresh;
    }

    [Fact]
    public async Task Dispose_Disposes_Component_Source_When_Cancellation_Callback_Throws() {
        var pending = new TaskCompletionSource<NTItemsProviderResult<TestGridItem>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var componentCancellationField = typeof(NTDataGrid<TestGridItem>).GetField("_componentCancellation", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        var cancellationCallbackRegistered = false;
        var holdRequests = false;
        var initialItems = new[] { new TestGridItem(1, "Initial", new DateOnly(2026, 1, 1), 1) };
        var cut = Render<NTDataGrid<TestGridItem>>(parameters => parameters
            .Add(grid => grid.ItemsProvider, request => {
                if (!holdRequests) {
                    return ValueTask.FromResult(new NTItemsProviderResult<TestGridItem>(initialItems, initialItems.Length));
                }

                request.CancellationToken.Register(() => throw new InvalidOperationException("Cancellation callback failed."));
                cancellationCallbackRegistered = true;
                return new ValueTask<NTItemsProviderResult<TestGridItem>>(pending.Task);
            })
            .Add(grid => grid.ChildContent, DefaultColumns));
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Initial"));
        holdRequests = true;
        var refresh = cut.InvokeAsync(() => cut.Instance.RefreshDataGridAsync());
        cut.WaitForAssertion(() => cancellationCallbackRegistered.Should().BeTrue());
        var componentCancellation = (CancellationTokenSource)componentCancellationField.GetValue(cut.Instance)!;

        Action dispose = cut.Instance.Dispose;

        dispose.Should().Throw<AggregateException>();
        Action readComponentToken = () => _ = componentCancellation.Token;
        readComponentToken.Should().Throw<ObjectDisposedException>();
        Record.Exception(cut.Instance.Dispose).Should().BeNull();

        pending.SetResult(new NTItemsProviderResult<TestGridItem>(initialItems, initialItems.Length));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => refresh);
    }

    [Fact]
    public void Virtualized_Grid_Uses_Virtualized_Provider_Range() {
        var captured = new List<NTDataGridItemsProviderRequest<TestGridItem>>();
        var cut = Render<NTDataGrid<TestGridItem>>(parameters => parameters
            .Add(grid => grid.ItemsProvider, request => {
                captured.Add(request);
                return ValueTask.FromResult(new NTItemsProviderResult<TestGridItem>(_items.ToArray(), _items.Count()));
            })
            .Add(grid => grid.Virtualize, true)
            .Add(grid => grid.VirtualizationInitialItemCount, 3)
            .Add(grid => grid.ChildContent, DefaultColumns));

        var virtualize = cut.FindComponent<NTVirtualize<TestGridItem>>();
        cut.InvokeAsync(() => virtualize.Instance.LoadItems(0, 0, 0, 3));

        cut.WaitForAssertion(() => captured.Should().Contain(request => request.StartIndex == 0 && request.Count > 0));
    }

    [Fact]
    public void Virtualized_Grid_Uses_Query_Prefix_As_Scroll_Restoration_Key() {
        var cut = Render<NTDataGrid<TestGridItem>>(parameters => parameters
            .Add(grid => grid.ItemsProvider, _ => ValueTask.FromResult(new NTItemsProviderResult<TestGridItem>(_items.ToArray(), _items.Count())))
            .Add(grid => grid.Virtualize, true)
            .Add(grid => grid.QueryParameterPrefix, "jobs")
            .Add(grid => grid.ChildContent, DefaultColumns));

        var virtualize = cut.FindComponent<NTVirtualize<TestGridItem>>();

        virtualize.Instance.ScrollRestorationKey.Should().Be("jobs-scroll");
    }

    [Fact]
    public async Task RefreshDataGridAsync_Virtualized_Resets_Cached_Items() {
        var firstItems = new[] { new TestGridItem(1, "Old", new DateOnly(2026, 1, 1), 1) };
        var secondItems = new[] { new TestGridItem(2, "New", new DateOnly(2026, 1, 2), 2) };
        var useSecondItems = false;
        var cut = Render<NTDataGrid<TestGridItem>>(parameters => parameters
            .Add(grid => grid.ItemsProvider, _ => {
                var items = useSecondItems ? secondItems : firstItems;
                return ValueTask.FromResult(new NTItemsProviderResult<TestGridItem>(items, items.Length));
            })
            .Add(grid => grid.Virtualize, true)
            .Add(grid => grid.VirtualizationInitialItemCount, 1)
            .Add(grid => grid.ChildContent, DefaultColumns));

        var virtualize = cut.FindComponent<NTVirtualize<TestGridItem>>();
        await cut.InvokeAsync(() => virtualize.Instance.LoadItems(0, 0, 0, 1));
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Old"));

        useSecondItems = true;
        await cut.InvokeAsync(() => cut.Instance.RefreshDataGridAsync());
        virtualize = cut.FindComponent<NTVirtualize<TestGridItem>>();
        await cut.InvokeAsync(() => virtualize.Instance.LoadItems(0, 0, 0, 1));

        cut.WaitForAssertion(() => {
            cut.Markup.Should().Contain("New");
            cut.Markup.Should().NotContain("Old");
        });
    }

    [Fact]
    public async Task Virtualized_Grid_Resets_When_Provider_Changes() {
        var firstItems = new[] { new TestGridItem(1, "Old", new DateOnly(2026, 1, 1), 1) };
        var secondItems = new[] { new TestGridItem(2, "New", new DateOnly(2026, 1, 2), 2) };
        NTDataGridItemsProvider<TestGridItem> provider = request => ValueTask.FromResult(new NTItemsProviderResult<TestGridItem>(firstItems, firstItems.Length));
        var cut = Render<NTDataGrid<TestGridItem>>(parameters => parameters
            .Add(grid => grid.ItemsProvider, provider)
            .Add(grid => grid.Virtualize, true)
            .Add(grid => grid.VirtualizationInitialItemCount, 1)
            .Add(grid => grid.ChildContent, DefaultColumns));

        var virtualize = cut.FindComponent<NTVirtualize<TestGridItem>>();
        await cut.InvokeAsync(() => virtualize.Instance.LoadItems(0, 0, 0, 1));
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Old"));

        provider = request => ValueTask.FromResult(new NTItemsProviderResult<TestGridItem>(secondItems, secondItems.Length));
        cut.Render(parameters => parameters
            .Add(grid => grid.ItemsProvider, provider)
            .Add(grid => grid.Virtualize, true)
            .Add(grid => grid.VirtualizationInitialItemCount, 1)
            .Add(grid => grid.ChildContent, DefaultColumns));

        virtualize = cut.FindComponent<NTVirtualize<TestGridItem>>();
        await cut.InvokeAsync(() => virtualize.Instance.LoadItems(0, 0, 0, 1));
        cut.WaitForAssertion(() => {
            cut.Markup.Should().Contain("New");
            cut.Markup.Should().NotContain("Old");
        });
    }

    [Fact]
    public async Task Virtualized_Grid_Bounds_Row_Index_Cache() {
        var cut = Render<NTDataGrid<TestGridItem>>(parameters => parameters
            .Add(grid => grid.ItemsProvider, request => {
                var items = Enumerable.Range(request.StartIndex + 1, request.Count ?? 1)
                    .Select(index => new TestGridItem(index, $"Item {index}", new DateOnly(2026, 1, 1), index))
                    .ToArray();
                return ValueTask.FromResult(new NTItemsProviderResult<TestGridItem>(items, 100));
            })
            .Add(grid => grid.Virtualize, true)
            .Add(grid => grid.VirtualizationInitialItemCount, 2)
            .Add(grid => grid.VirtualizationMaxCachedItemCount, 4)
            .Add(grid => grid.Appearance, NTDataGridAppearance.Striped)
            .Add(grid => grid.ChildContent, DefaultColumns));

        var virtualize = cut.FindComponent<NTVirtualize<TestGridItem>>();
        for (var startIndex = 0; startIndex < 20; startIndex += 2) {
            await cut.InvokeAsync(() => virtualize.Instance.LoadItems(startIndex * 36, 0, startIndex, 2));
        }

        var rowIndices = typeof(NTDataGrid<TestGridItem>).GetField("_rowIndices", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(cut.Instance);
        rowIndices.Should().BeAssignableTo<IReadOnlyDictionary<object, int>>().Subject.Should().HaveCountLessThanOrEqualTo(4);
    }

    [Fact]
    public async Task Virtualized_Grid_Replaces_Visible_Rows_When_Range_Changes() {
        var cut = Render<NTDataGrid<TestGridItem>>(parameters => parameters
            .Add(grid => grid.ItemsProvider, request => {
                var items = Enumerable.Range(request.StartIndex + 1, request.Count ?? 1)
                    .Select(index => new TestGridItem(index, $"Item {index}", new DateOnly(2026, 1, 1), index))
                    .ToArray();
                return ValueTask.FromResult(new NTItemsProviderResult<TestGridItem>(items, 100));
            })
            .Add(grid => grid.Virtualize, true)
            .Add(grid => grid.VirtualizationInitialItemCount, 2)
            .Add(grid => grid.VirtualizationPlaceholderPreloadWindowCount, 0)
            .Add(grid => grid.VirtualizationBackgroundPreloadWindowCount, 0)
            .Add(grid => grid.ChildContent, DefaultColumns));

        var virtualize = cut.FindComponent<NTVirtualize<TestGridItem>>();
        await cut.InvokeAsync(() => virtualize.Instance.LoadItems(0, 98 * 36, 0, 2));
        cut.WaitForAssertion(() => {
            cut.Markup.Should().Contain("Item 1");
            cut.Markup.Should().Contain("Item 2");
            cut.Markup.Should().NotContain("Item 41");
        });

        await cut.InvokeAsync(() => virtualize.Instance.LoadItems(40 * 36, 58 * 36, 40, 2));
        cut.WaitForAssertion(() => {
            cut.Markup.Should().Contain("Item 41");
            cut.Markup.Should().Contain("Item 42");
            cut.Markup.Should().NotContain("Item 1");
            cut.Markup.Should().NotContain("Item 2");
        });
    }

    [Fact]
    public async Task Virtualized_Grid_Uses_Cached_Rows_When_Range_Is_Revisited_By_Default() {
        var captured = new List<NTDataGridItemsProviderRequest<TestGridItem>>();
        var cut = Render<NTDataGrid<TestGridItem>>(parameters => parameters
            .Add(grid => grid.ItemsProvider, request => {
                captured.Add(request);
                var items = Enumerable.Range(request.StartIndex + 1, request.Count ?? 1)
                    .Select(index => new TestGridItem(index, $"Item {index}", new DateOnly(2026, 1, 1), index))
                    .ToArray();
                return ValueTask.FromResult(new NTItemsProviderResult<TestGridItem>(items, 100));
            })
            .Add(grid => grid.Virtualize, true)
            .Add(grid => grid.VirtualizationInitialItemCount, 2)
            .Add(grid => grid.VirtualizationPlaceholderPreloadWindowCount, 0)
            .Add(grid => grid.VirtualizationBackgroundPreloadWindowCount, 0)
            .Add(grid => grid.ChildContent, DefaultColumns));

        var virtualize = cut.FindComponent<NTVirtualize<TestGridItem>>();
        await cut.InvokeAsync(() => virtualize.Instance.LoadItems(0, 98 * 36, 0, 2));
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Item 2"));

        await cut.InvokeAsync(() => virtualize.Instance.LoadItems(10 * 36, 88 * 36, 10, 2));
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Item 12"));

        await cut.InvokeAsync(() => virtualize.Instance.LoadItems(0, 98 * 36, 0, 2));
        cut.WaitForAssertion(() => {
            cut.Markup.Should().Contain("Item 1");
            cut.Markup.Should().Contain("Item 2");
            captured.Count(request => request.StartIndex == 0 && request.Count == 2).Should().Be(1);
        });
    }

    [Fact]
    public void Virtualized_Grid_Allows_Explicit_Cached_Row_Revalidation() {
        var defaultGrid = Render<NTDataGrid<TestGridItem>>(parameters => parameters
            .Add(grid => grid.ItemsProvider, _ => ValueTask.FromResult(new NTItemsProviderResult<TestGridItem>(_items.ToArray(), _items.Count())))
            .Add(grid => grid.Virtualize, true)
            .Add(grid => grid.ChildContent, DefaultColumns));
        var revalidatingGrid = Render<NTDataGrid<TestGridItem>>(parameters => parameters
            .Add(grid => grid.ItemsProvider, _ => ValueTask.FromResult(new NTItemsProviderResult<TestGridItem>(_items.ToArray(), _items.Count())))
            .Add(grid => grid.Virtualize, true)
            .Add(grid => grid.VirtualizationRevalidateCachedItems, true)
            .Add(grid => grid.ChildContent, DefaultColumns));

        defaultGrid.FindComponent<NTVirtualize<TestGridItem>>().Instance.RevalidateCachedItems.Should().BeFalse();
        revalidatingGrid.FindComponent<NTVirtualize<TestGridItem>>().Instance.RevalidateCachedItems.Should().BeTrue();
    }

    [Fact]
    public void TemplateColumn_Uses_Updated_ChildContent() {
        var templateText = "Open";
        RenderFragment columns = builder => {
            builder.OpenComponent<NTTemplateColumn<TestGridItem>>(0);
            builder.AddAttribute(1, nameof(NTTemplateColumn<TestGridItem>.Title), "Actions");
            builder.AddAttribute(2, nameof(NTTemplateColumn<TestGridItem>.ChildContent), (RenderFragment<TestGridItem>)(item => cellBuilder => cellBuilder.AddContent(0, $"{templateText} {item.Id}")));
            builder.CloseComponent();
        };
        var cut = RenderGrid(columns);

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Open 1"));
        templateText = "View";
        cut.Render(parameters => parameters
            .Add(grid => grid.Items, _items)
            .Add(grid => grid.Caption, "Invoices")
            .Add(grid => grid.ChildContent, columns));

        cut.WaitForAssertion(() => {
            cut.Markup.Should().Contain("View 1");
            cut.Markup.Should().NotContain("Open 1");
        });
    }

    [Fact]
    public void Virtualized_Grid_Uses_Density_Default_Item_Size() {
        var standard = Render<NTDataGrid<TestGridItem>>(parameters => parameters
            .Add(grid => grid.ItemsProvider, _ => ValueTask.FromResult(new NTItemsProviderResult<TestGridItem>(_items.ToArray(), _items.Count())))
            .Add(grid => grid.Virtualize, true)
            .Add(grid => grid.VirtualizationInitialItemCount, 3)
            .Add(grid => grid.ChildContent, DefaultColumns));

        var compact = Render<NTDataGrid<TestGridItem>>(parameters => parameters
            .Add(grid => grid.ItemsProvider, _ => ValueTask.FromResult(new NTItemsProviderResult<TestGridItem>(_items.ToArray(), _items.Count())))
            .Add(grid => grid.Virtualize, true)
            .Add(grid => grid.VirtualizationInitialItemCount, 3)
            .Add(grid => grid.Density, NTDataGridDensity.Compact)
            .Add(grid => grid.ChildContent, DefaultColumns));

        standard.Find(".nt-data-grid-scroll").GetAttribute("style").Should().Contain("max-height: 108px");
        compact.Find(".nt-data-grid-scroll").GetAttribute("style").Should().Contain("max-height: 84px");
    }

    [Fact]
    public async Task Virtualized_Grid_Renders_Full_Row_Skeleton_Placeholder_While_Loading() {
        var items = Enumerable.Range(1, 25)
            .Select(index => new TestGridItem(index, $"Item {index}", new DateOnly(2026, 1, 1).AddDays(index), index))
            .ToArray();
        var pending = new TaskCompletionSource<NTItemsProviderResult<TestGridItem>>();
        var holdSecondRequest = false;
        var cut = Render<NTDataGrid<TestGridItem>>(parameters => parameters
            .Add(grid => grid.ItemsProvider, request => {
                if (holdSecondRequest && request.StartIndex == 10) {
                    return new ValueTask<NTItemsProviderResult<TestGridItem>>(pending.Task);
                }

                return ValueTask.FromResult(new NTItemsProviderResult<TestGridItem>(items.Skip(request.StartIndex).Take(request.Count ?? items.Length).ToArray(), items.Length));
            })
            .Add(grid => grid.Virtualize, true)
            .Add(grid => grid.VirtualizationItemSize, 64)
            .Add(grid => grid.VirtualizationInitialItemCount, 4)
            .Add(grid => grid.ChildContent, DefaultColumns));

        var virtualize = cut.FindComponent<NTVirtualize<TestGridItem>>();
        await cut.InvokeAsync(() => virtualize.Instance.LoadItems(0, 0, 0, 4));
        cut.WaitForAssertion(() => cut.FindAll("tbody tr:not([aria-hidden='true'])").Should().NotBeEmpty());

        holdSecondRequest = true;
        await cut.InvokeAsync(() => virtualize.Instance.LoadItems(640, 0, 10, 2));

        cut.WaitForAssertion(() => {
            var placeholder = cut.Find(".nt-data-grid-row-placeholder");
            placeholder.GetAttribute("style").Should().Contain("--nt-data-grid-placeholder-row-height: 64px");
            placeholder.GetAttribute("aria-hidden").Should().Be("true");
            var cell = placeholder.QuerySelector("td")!;
            cell.GetAttribute("colspan").Should().Be("3");
            cell.QuerySelector(".nt-data-grid-row-skeleton .nt-skeleton")!.GetAttribute("style").Should().Contain("--nt-skeleton-height:64px");
        });

        pending.SetResult(new NTItemsProviderResult<TestGridItem>(items.Skip(10).Take(2).ToArray(), items.Length));
        cut.WaitForAssertion(() => cut.FindAll(".nt-data-grid-row-placeholder").Should().BeEmpty());
    }

    [Fact]
    public void Invalid_Source_Combinations_Throw() {
        var bothSources = () => Render<NTDataGrid<TestGridItem>>(parameters => parameters
            .Add(grid => grid.Items, _items)
            .Add(grid => grid.ItemsProvider, _ => ValueTask.FromResult(new NTItemsProviderResult<TestGridItem>()))
            .Add(grid => grid.ChildContent, DefaultColumns));

        var noSources = () => Render<NTDataGrid<TestGridItem>>(parameters => parameters
            .Add(grid => grid.ChildContent, DefaultColumns));

        bothSources.Should().Throw<InvalidOperationException>().WithMessage("*either Items or ItemsProvider, not both*");
        noSources.Should().Throw<InvalidOperationException>().WithMessage("*requires Items or ItemsProvider*");
    }

    [Fact]
    public void Virtualization_And_Pagination_Are_Mutually_Exclusive() {
        var act = () => Render<NTDataGrid<TestGridItem>>(parameters => parameters
            .Add(grid => grid.Items, _items)
            .Add(grid => grid.Virtualize, true)
            .Add(grid => grid.ShowPagination, true)
            .Add(grid => grid.ChildContent, DefaultColumns));

        act.Should().Throw<InvalidOperationException>().WithMessage("*Virtualize*ShowPagination*");
    }

    private IRenderedComponent<NTDataGrid<TestGridItem>> RenderGrid(RenderFragment? columns = null) => RenderGrid(null, columns);

    private IRenderedComponent<NTDataGrid<TestGridItem>> RenderGrid(Action<ComponentParameterCollectionBuilder<NTDataGrid<TestGridItem>>>? configure, RenderFragment? columns = null) =>
        Render<NTDataGrid<TestGridItem>>(parameters => {
            parameters
                .Add(grid => grid.Items, _items)
                .Add(grid => grid.Caption, "Invoices")
                .Add(grid => grid.ChildContent, columns ?? DefaultColumns);
            configure?.Invoke(parameters);
        });

    private static RenderFragment DefaultColumns => builder => {
        builder.OpenComponent<NTPropertyColumn<TestGridItem, string>>(0);
        builder.AddAttribute(1, nameof(NTPropertyColumn<TestGridItem, string>.Title), "Name");
        builder.AddAttribute(2, nameof(NTPropertyColumn<TestGridItem, string>.Property), (System.Linq.Expressions.Expression<Func<TestGridItem, string>>)(item => item.Name));
        builder.CloseComponent();
        builder.OpenComponent<NTPropertyColumn<TestGridItem, DateOnly>>(3);
        builder.AddAttribute(4, nameof(NTPropertyColumn<TestGridItem, DateOnly>.Title), "Created");
        builder.AddAttribute(5, nameof(NTPropertyColumn<TestGridItem, DateOnly>.Property), (System.Linq.Expressions.Expression<Func<TestGridItem, DateOnly>>)(item => item.Created));
        builder.AddAttribute(6, nameof(NTPropertyColumn<TestGridItem, DateOnly>.Format), "yyyy-MM-dd");
        builder.CloseComponent();
        builder.OpenComponent<NTPropertyColumn<TestGridItem, decimal>>(7);
        builder.AddAttribute(8, nameof(NTPropertyColumn<TestGridItem, decimal>.Title), "Amount");
        builder.AddAttribute(9, nameof(NTPropertyColumn<TestGridItem, decimal>.Property), (System.Linq.Expressions.Expression<Func<TestGridItem, decimal>>)(item => item.Amount));
        builder.AddAttribute(10, nameof(NTPropertyColumn<TestGridItem, decimal>.Format), "C");
        builder.AddAttribute(11, nameof(NTPropertyColumn<TestGridItem, decimal>.TextAlign), TextAlign.Right);
        builder.CloseComponent();
    };

    private sealed class CountingEnumerable<T>(IEnumerable<T> items) : IEnumerable<T> {
        public int EnumerationCount { get; private set; }

        public IEnumerator<T> GetEnumerator() {
            EnumerationCount++;
            return items.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

        public void ResetEnumerationCount() => EnumerationCount = 0;
    }

    private sealed record TestGridItem(int Id, string Name, DateOnly Created, decimal Amount) {
        public int DaysOpen => Id;
    }
}
