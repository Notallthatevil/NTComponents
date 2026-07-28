using System.Text.Json;
using NTComponents.MCP.Catalog;
using NTComponents.MCP.Contracts;
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
        page.TotalCount.Should().BeGreaterThanOrEqualTo(page.Items.Count);
    }

    /// <summary>Behavior source: get_nt_component returns compact usage guidance and directs clients to targeted members or the full resource for more detail.</summary>
    [Fact]
    public void GetComponent_WithKnownName_ReturnsSuccessfulStructuredLookup() {
        var result = _tools.GetComponent("NTButton");

        result.Found.Should().BeTrue();
        result.Error.Should().BeNull();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("NTButton");
        result.Value.RazorUsage.Should().Contain("<NTButton");
        result.Value.ParameterCount.Should().BeGreaterThan(0);
        result.Value.RelatedTypes.Should().Contain("NTButtonVariant");
        result.Value.ResourceUri.Should().Be("ntcomponents://components/NTButton");
    }

    /// <summary>Behavior source: get_nt_component_members provides a filtered, paged API surface only when an agent needs it.</summary>
    [Fact]
    public void GetComponentMembers_WithParameterQuery_ReturnsOnlyMatchingParameters() {
        var result = _tools.GetComponentMembers("NTButton", query: "variant", kind: "Parameter", limit: 5);

        result.Found.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Items.Should().NotBeEmpty();
        result.Value.Items.Should().OnlyContain(member => member.Kind == "Parameter" && member.Name.Contains("Variant", StringComparison.OrdinalIgnoreCase));
        result.Value.TotalCount.Should().Be(result.Value.Items.Count);
    }

    /// <summary>Behavior source: list_nt_reference_types defines Enum and ComponentApi as supported filters and promises a paged result.</summary>
    [Fact]
    public void ListReferenceTypes_WithDocumentedFilters_ReturnsMatchingPage() {
        var page = _tools.ListReferenceTypes(kind: "Enum", scope: "ComponentApi", limit: 5);

        page.Items.Should().HaveCount(5);
        page.Items.Should().OnlyContain(reference => reference.Kind == "Enum" && reference.Scope == "ComponentApi");
        page.TotalCount.Should().BeGreaterThan(page.Items.Count);
        page.NextOffset.Should().Be(5);
    }

    /// <summary>Behavior source: get_nt_reference_type accepts a full name and promises a successful LookupResult containing values and component usage for one reference type.</summary>
    [Fact]
    public void GetReferenceType_WithKnownFullName_ReturnsSuccessfulStructuredLookup() {
        var result = _tools.GetReferenceType("NTComponents.NTButtonVariant");

        result.Found.Should().BeTrue();
        result.Error.Should().BeNull();
        result.Value.Should().NotBeNull();
        result.Value!.Kind.Should().Be("Enum");
        result.Value.Members.Items.Should().Contain(field => field.Name == "Filled");
        result.Value.UsedByComponents.Should().Contain("NTButton");
    }

    /// <summary>Behavior source: very large reference types expose bounded continuation metadata instead of flooding a model context.</summary>
    [Fact]
    public void GetReferenceType_WithLargeEnum_ReturnsBoundedMemberPage() {
        var result = _tools.GetReferenceType("MaterialIcon");

        result.Found.Should().BeTrue();
        result.Value!.Members.Items.Should().HaveCount(10);
        result.Value.Members.TotalCount.Should().BeGreaterThan(1_000);
        result.Value.Members.NextOffset.Should().Be(10);

        var memberName = result.Value.Members.Items[0].Name;
        var filtered = _tools.GetReferenceType("MaterialIcon", query: memberName, limit: 50);
        filtered.Value!.Members.Items.Should().Contain(member => member.Name == memberName);
        filtered.Value.Members.Items.Should().OnlyContain(member => member.Name.Contains(memberName, StringComparison.OrdinalIgnoreCase) || member.Declaration.Contains(memberName, StringComparison.OrdinalIgnoreCase) || member.Summary.Contains(memberName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Behavior source: search_ntcomponents promises compact paged relevance-ranked results with documentation links and typo suggestions.</summary>
    [Fact]
    public void Search_WithDocumentedQuery_ReturnsRankedMetadata() {
        var page = _tools.Search("dialog elevation", limit: 2);

        page.Items.Should().HaveCount(2);
        page.Items[0].Name.Should().Be("NTDialog");
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

    /// <summary>Behavior source: default MCP responses are intentionally bounded so discovery and usage lookup do not consume large model contexts.</summary>
    [Fact]
    public void DefaultResponses_StayWithinTokenFriendlySerializedBudgets() {
        var components = JsonSerializer.Serialize(_tools.ListComponents());
        var references = JsonSerializer.Serialize(_tools.ListReferenceTypes());
        var search = JsonSerializer.Serialize(_tools.Search("button"));

        components.Length.Should().BeLessThan(7_000);
        references.Length.Should().BeLessThan(7_000);
        JsonSerializer.Serialize(_tools.GetComponent("NTButton")).Length.Should().BeLessThan(3_000);
        search.Length.Should().BeLessThan(4_000);
        components.Should().NotContain("\"IsObsolete\":false");
        references.Should().NotContain("\"IsObsolete\":false");
        search.Should().NotContain("MatchedFields");
    }

    /// <summary>Behavior source: every default component usage lookup stays compact regardless of the size of its full API documentation.</summary>
    [Fact]
    public void ComponentUsage_ForEveryPublicComponent_StaysWithinSerializedBudget() {
        var componentNames = GetAllNames(offset => _tools.ListComponents(limit: 50, offset: offset));

        componentNames.Should().NotBeEmpty();
        foreach (var componentName in componentNames) {
            JsonSerializer.Serialize(_tools.GetComponent(componentName)).Length.Should().BeLessThan(4_000, $"{componentName} should use targeted member/resource retrieval for exhaustive documentation");
        }
    }

    /// <summary>Behavior source: reference lookups remain bounded even though they expose complete values and members on demand.</summary>
    [Fact]
    public void ReferenceLookup_ForEveryPublicReference_StaysWithinSerializedBudget() {
        var referenceNames = GetAllNames(offset => _tools.ListReferenceTypes(limit: 50, offset: offset));

        referenceNames.Should().NotBeEmpty();
        foreach (var referenceName in referenceNames) {
            JsonSerializer.Serialize(_tools.GetReferenceType(referenceName)).Length.Should().BeLessThan(15_000, $"{referenceName} should remain suitable for targeted retrieval");
        }
    }

    private static IReadOnlyList<string> GetAllNames<T>(Func<int, McpPage<T>> getPage) {
        var names = new List<string>();
        int? offset = 0;
        while (offset is { } currentOffset) {
            var page = getPage(currentOffset);
            names.AddRange(page.Items.Select(item => item switch {
                McpComponentSummary component => component.Name,
                McpReferenceSummary reference => reference.Name,
                _ => throw new InvalidOperationException($"Unsupported summary type {typeof(T).Name}."),
            }));
            offset = page.NextOffset;
        }

        return names;
    }
}
