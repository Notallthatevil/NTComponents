using System.Text.Json;
using NTComponents.MCP.Catalog;
using NTComponents.MCP.Tools;

namespace NTComponents.MCP.Tests.Tools;

public class NTComponentsTools_Tests {
    private readonly NTComponentsTools _tools = new(new NTComponentsCatalog());

    /// <summary>Behavior source: get_nt_catalog_overview promises versions, documentation URL, counts, folders, kinds, and supported reference scopes.</summary>
    [Fact]
    public void GetCatalogOverview_ReturnsAdvertisedCapabilities() {
        var overview = _tools.GetCatalogOverview();

        overview.ServerVersion.Should().NotBeNullOrWhiteSpace();
        overview.ComponentsVersion.Should().NotBeNullOrWhiteSpace();
        overview.DocumentationBaseUrl.Should().Be("https://ntcomponents.nttechnologies.dev");
        overview.ComponentCount.Should().BeGreaterThan(0);
        overview.ReferenceTypeCount.Should().BeGreaterThan(0);
        overview.ComponentFolders.Should().Contain("Buttons");
        overview.ReferenceKinds.Should().Equal("Enum", "Helper");
        overview.ReferenceScopes.Should().Equal("ComponentApi", "LibraryApi");
    }

    /// <summary>Behavior source: list_nt_components promises a paged result whose query and folder inputs narrow public component summaries.</summary>
    [Fact]
    public void ListComponents_WithDocumentedFilters_ReturnsMatchingPage() {
        var page = _tools.ListComponents(query: "button", folder: "Buttons", limit: 5);

        page.Items.Should().NotBeEmpty().And.HaveCountLessThanOrEqualTo(5);
        page.Items.Should().OnlyContain(component => component.Name.StartsWith("NT", StringComparison.Ordinal) && component.Folder == "Buttons");
        page.Items.Should().OnlyContain(component => component.DocumentationUrl.StartsWith("https://ntcomponents.nttechnologies.dev/components/", StringComparison.Ordinal));
        page.Offset.Should().Be(0);
        page.Limit.Should().Be(5);
    }

    /// <summary>Behavior source: get_nt_component accepts a simple type name and promises a successful LookupResult containing complete component usage guidance.</summary>
    [Fact]
    public void GetComponent_WithKnownName_ReturnsSuccessfulStructuredLookup() {
        var result = _tools.GetComponent("NTButton", includeRelatedEnumValues: true);

        result.Found.Should().BeTrue();
        result.Error.Should().BeNull();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("NTButton");
        result.Value.RazorUsage.Should().Contain("<NTButton");
        result.Value.RelatedEnums.Should().Contain(item => item.Name == "NTButtonVariant" && item.Values.Count > 0);
    }

    /// <summary>Behavior source: list_nt_reference_types defines Enum and ComponentApi as supported filters and promises a paged result.</summary>
    [Fact]
    public void ListReferenceTypes_WithDocumentedFilters_ReturnsMatchingPage() {
        var page = _tools.ListReferenceTypes(kind: "Enum", scope: "ComponentApi", limit: 5);

        page.Items.Should().HaveCount(5);
        page.Items.Should().OnlyContain(reference => reference.Kind == "Enum" && reference.Scope == "ComponentApi" && reference.UsedByComponents.Count > 0);
        page.Offset.Should().Be(0);
        page.Limit.Should().Be(5);
    }

    /// <summary>Behavior source: get_nt_reference_type accepts a full name and promises a successful LookupResult containing values and component usage for one reference type.</summary>
    [Fact]
    public void GetReferenceType_WithKnownFullName_ReturnsSuccessfulStructuredLookup() {
        var result = _tools.GetReferenceType("NTComponents.NTButtonVariant");

        result.Found.Should().BeTrue();
        result.Error.Should().BeNull();
        result.Value.Should().NotBeNull();
        result.Value!.Kind.Should().Be("Enum");
        result.Value.Fields.Should().Contain(field => field.Name == "Filled");
        result.Value.UsedByComponents.Should().Contain("NTButton");
    }

    /// <summary>Behavior source: search_ntcomponents promises paged relevance-ranked results with matched terms, matched fields, documentation links, and typo suggestions.</summary>
    [Fact]
    public void Search_WithDocumentedQuery_ReturnsRankedMetadata() {
        var page = _tools.Search("dialog elevation", limit: 2);

        page.Items.Should().HaveCount(2);
        page.Items[0].Name.Should().Be("NTDialog");
        page.Items[0].MatchedTerms.Should().Equal("dialog", "elevation");
        page.Items[0].MatchedFields.Should().NotBeEmpty();
        page.Items[0].DocumentationUrl.Should().StartWith("https://ntcomponents.nttechnologies.dev/");
        page.DidYouMean.Should().BeNull();
    }

    /// <summary>Behavior source: search_ntcomponents is explicitly advertised as read-only and idempotent, so repeated calls with fixed inputs must return identical structured content.</summary>
    [Fact]
    public void Search_WhenRepeated_IsIdempotent() {
        var first = JsonSerializer.Serialize(_tools.Search("dialog elevation", limit: 10));

        var second = JsonSerializer.Serialize(_tools.Search("dialog elevation", limit: 10));

        second.Should().Be(first);
    }
}
