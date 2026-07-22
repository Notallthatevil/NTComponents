using NTComponents.MCP.Catalog;

namespace NTComponents.MCP.Tests.Validation;

public class CatalogInputValidation_Tests {
    /// <summary>Behavior source: MCP tool descriptions define the inclusive limit range as 1 through 200.</summary>
    [Theory]
    [InlineData(1)]
    [InlineData(200)]
    public void WithBoundaryLimit_AllLimitedOperationsSucceed(int limit) {
        var catalog = new NTComponentsCatalog();

        var components = catalog.ListComponents(limit: limit);
        var references = catalog.ListReferences(limit: limit);
        var searchResults = catalog.Search("button", limit);

        components.Should().HaveCountLessThanOrEqualTo(limit);
        references.Should().HaveCountLessThanOrEqualTo(limit);
        searchResults.Should().HaveCountLessThanOrEqualTo(limit);
    }

    /// <summary>Behavior source: MCP tool descriptions define limits outside 1 through 200 as invalid instead of values to normalize.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(201)]
    public void WithOutOfRangeLimit_AllLimitedOperationsRejectInput(int limit) {
        var catalog = new NTComponentsCatalog();

        var componentAction = () => catalog.ListComponents(limit: limit);
        var referenceAction = () => catalog.ListReferences(limit: limit);
        var searchAction = () => catalog.Search("button", limit);

        componentAction.Should().Throw<CatalogValidationException>().Which.ParameterName.Should().Be("limit");
        referenceAction.Should().Throw<CatalogValidationException>().Which.ParameterName.Should().Be("limit");
        searchAction.Should().Throw<CatalogValidationException>().Which.ParameterName.Should().Be("limit");
    }

    /// <summary>Behavior source: the reference-list tool documents Enum and Helper kinds; existing matching is case-insensitive.</summary>
    [Theory]
    [InlineData("Enum", "Enum")]
    [InlineData("enum", "Enum")]
    [InlineData("Helper", "Helper")]
    [InlineData("helper", "Helper")]
    public void WithDocumentedReferenceKind_ReturnsOnlyThatKind(string kind, string expectedKind) {
        var catalog = new NTComponentsCatalog();

        var references = catalog.ListReferences(kind: kind, limit: 200);

        references.Should().NotBeEmpty().And.OnlyContain(reference => reference.Kind == expectedKind);
    }

    /// <summary>Behavior source: the reference-list tool defines the closed set Enum or Helper, so other supplied values are invalid.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Widget")]
    public void WithInvalidReferenceKind_RejectsInput(string kind) {
        var catalog = new NTComponentsCatalog();

        var action = () => catalog.ListReferences(kind: kind);

        action.Should().Throw<CatalogValidationException>().Which.ParameterName.Should().Be("kind");
    }

    /// <summary>Behavior source: search_ntcomponents documents query as required search text.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void WithBlankRequiredSearchQuery_RejectsInput(string? query) {
        var catalog = new NTComponentsCatalog();

        var action = () => catalog.Search(query!);

        action.Should().Throw<CatalogValidationException>().Which.ParameterName.Should().Be("query");
    }

    /// <summary>Behavior source: catalog search-query inputs have an inclusive maximum length of 512 characters.</summary>
    [Fact]
    public void WithMaximumLengthQuery_AllQueryOperationsSucceed() {
        var catalog = new NTComponentsCatalog();
        var query = new string('a', 512);

        var components = catalog.ListComponents(query: query);
        var references = catalog.ListReferences(query: query);
        var search = catalog.Search(query);

        components.Should().BeEmpty();
        references.Should().BeEmpty();
        search.Should().BeEmpty();
    }

    /// <summary>Behavior source: catalog search-query inputs reject more than 512 characters to bound public request work.</summary>
    [Fact]
    public void WithOverMaximumLengthQuery_AllQueryOperationsRejectInput() {
        var catalog = new NTComponentsCatalog();
        var query = new string('a', 513);

        var componentAction = () => catalog.ListComponents(query: query);
        var referenceAction = () => catalog.ListReferences(query: query);
        var searchAction = () => catalog.Search(query);

        componentAction.Should().Throw<CatalogValidationException>().Which.ParameterName.Should().Be("query");
        referenceAction.Should().Throw<CatalogValidationException>().Which.ParameterName.Should().Be("query");
        searchAction.Should().Throw<CatalogValidationException>().Which.ParameterName.Should().Be("query");
    }

    /// <summary>Behavior source: get tools require a component or reference type name, and invalid input is distinct from a missing valid name.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void WithBlankRequiredLookupName_BothLookupsRejectInput(string? name) {
        var catalog = new NTComponentsCatalog();

        var componentAction = () => catalog.GetComponent(name!);
        var referenceAction = () => catalog.GetReference(name!);

        componentAction.Should().Throw<CatalogValidationException>().Which.ParameterName.Should().Be("name");
        referenceAction.Should().Throw<CatalogValidationException>().Which.ParameterName.Should().Be("name");
    }

    /// <summary>Behavior source: the existing lookup contract represents a valid unknown name as a missing result, not a validation error.</summary>
    [Fact]
    public void WithValidUnknownLookupName_ReturnsMissingResult() {
        var catalog = new NTComponentsCatalog();

        var component = catalog.GetComponent("NTDoesNotExist");
        var reference = catalog.GetReference("NTDoesNotExist");

        component.Should().BeNull();
        reference.Should().BeNull();
    }
}
