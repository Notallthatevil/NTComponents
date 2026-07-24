using NTComponents.MCP.Catalog;

namespace NTComponents.MCP.Tests.Catalog;

public class Paging_Tests {
    private readonly NTComponentsCatalog _catalog = new();

    [Fact]
    public void ListReferencePage_WithDefaultLimit_ReportsRemainingResults() {
        var page = _catalog.ListReferencePage();

        page.Items.Should().HaveCount(100);
        page.TotalCount.Should().BeGreaterThan(page.Items.Count);
        page.Offset.Should().Be(0);
        page.Limit.Should().Be(100);
        page.HasMore.Should().BeTrue();
        page.NextOffset.Should().Be(100);
    }

    [Fact]
    public void ListComponentPage_WithOffset_ReturnsTheRequestedSlice() {
        var completePage = _catalog.ListComponentPage(limit: 200);

        var page = _catalog.ListComponentPage(limit: 2, offset: 1);

        page.Items.Select(component => component.Name).Should().Equal(completePage.Items.Skip(1).Take(2).Select(component => component.Name));
        page.TotalCount.Should().Be(completePage.TotalCount);
        page.Offset.Should().Be(1);
        page.NextOffset.Should().Be(3);
    }

    [Theory]
    [InlineData("ComponentApi", true)]
    [InlineData("LibraryApi", false)]
    public void ListReferencePage_WithScope_FiltersByComponentUsage(string scope, bool isUsedByComponent) {
        var page = _catalog.ListReferencePage(scope: scope, limit: 200);

        page.Items.Should().NotBeEmpty();
        page.Items.Should().OnlyContain(reference => (reference.UsedByComponents.Count > 0) == isUsedByComponent);
        page.Items.Should().OnlyContain(reference => reference.Scope == scope);
    }

    [Fact]
    public void GetOverview_ProvidesCatalogIdentityAndDocumentationLocation() {
        var overview = _catalog.GetOverview();

        overview.ServerVersion.Should().NotBeNullOrWhiteSpace();
        overview.ComponentsVersion.Should().NotBeNullOrWhiteSpace();
        overview.DocumentationBaseUrl.Should().Be("https://ntcomponents.nttechnologies.dev");
        overview.ComponentCount.Should().BeGreaterThan(0);
        overview.ReferenceTypeCount.Should().BeGreaterThan(100);
    }
}
