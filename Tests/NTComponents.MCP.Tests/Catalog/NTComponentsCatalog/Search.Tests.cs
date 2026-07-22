using NTComponents.MCP.Catalog;

namespace NTComponents.MCP.Tests.Catalog;

public class Search_Tests {
    private readonly NTComponentsCatalog _catalog = new();

    [Theory]
    [InlineData("virtualized table", "NTDataGrid")]
    [InlineData("dialog elevation", "NTDialog")]
    public void WithMultiTermQuery_RanksDocumentMatchingAllTermsFirst(string query, string expectedName) {
        var results = _catalog.Search(query);

        results.Should().NotBeEmpty();
        results[0].Name.Should().Be(expectedName);
    }

    [Fact]
    public void WithRenderCompatibilityQuery_RanksClassifiedComponentsAboveAttributeDefinition() {
        var results = _catalog.Search("render compatibility", 200);

        var firstComponent = results.First(result => result.Category == "Component");
        var attributeDefinition = results.Single(result => result.Name == "NTDocumentationAttribute");
        firstComponent.Score.Should().BeGreaterThan(attributeDefinition.Score);
        results.Should().Contain(result => result.Name == "NTComponentRenderCompatibility");
    }

    [Theory]
    [InlineData("NTDialog", "NTDialog")]
    [InlineData("NTComponents.NTDialog", "NTDialog")]
    [InlineData("NTElevation", "NTElevation")]
    public void WithExactTypeName_RanksExactMatchFirst(string query, string expectedName) {
        var results = _catalog.Search(query);

        results.Should().NotBeEmpty();
        results[0].Name.Should().Be(expectedName);
        if (results.Count > 1) {
            results[0].Score.Should().BeGreaterThan(results[1].Score);
        }
    }

    [Fact]
    public void WithPublicMethodName_FindsDeclaringComponent() {
        var results = _catalog.Search("RefreshDataGridAsync");

        results.Should().Contain(result => result.Name == "NTDataGrid" && result.Category == "Component");
    }

    [Theory]
    [InlineData("OnParametersSet")]
    [InlineData("OnParametersSetAsync")]
    public void WithComponentInfrastructureMethodName_DoesNotReturnComponents(string methodName) {
        var results = _catalog.Search(methodName, 200);

        results.Should().NotContain(result => result.Category == "Component");
    }

    [Fact]
    public void WithPublicHelperMethodName_FindsDeclaringHelpers() {
        var results = _catalog.Search("ToCssClass", 200);

        results.Should().Contain(result => result.Name == "NTElevationExt" && result.Category == "Helper");
    }

    [Fact]
    public void WithEnumValueAndTypeName_RanksEnumFirst() {
        var results = _catalog.Search("medium elevation");

        results.Should().NotBeEmpty();
        results[0].Name.Should().Be("NTElevation");
        results[0].Category.Should().Be("Enum");
    }

    [Fact]
    public void WithCompatibilityAndComponentTerms_RanksMatchingComponentFirst() {
        var results = _catalog.Search("interactive required rich text");

        results.Should().NotBeEmpty();
        results[0].Name.Should().Be("NTRichTextEditor");
    }

    [Fact]
    public void WithTiedScores_OrdersNamesOrdinally() {
        var results = _catalog.Search("render compatibility", 200);
        var tiedResults = results.GroupBy(result => result.Score).FirstOrDefault(group => group.Count() > 1);

        tiedResults.Should().NotBeNull();
        tiedResults!.Select(result => result.Name).Should().Equal(tiedResults.Select(result => result.Name).OrderBy(name => name, StringComparer.Ordinal));
    }

    [Fact]
    public void WithMaximumDistinctTerms_AllQueryOperationsAcceptInput() {
        var query = string.Join(' ', Enumerable.Range(1, 16).Select(index => $"term{index}"));

        var componentAction = () => _catalog.ListComponents(query: query);
        var referenceAction = () => _catalog.ListReferences(query: query);
        var searchAction = () => _catalog.Search(query);

        componentAction.Should().NotThrow();
        referenceAction.Should().NotThrow();
        searchAction.Should().NotThrow();
    }

    [Fact]
    public void WithTooManyDistinctTerms_AllQueryOperationsRejectInput() {
        var query = string.Join(' ', Enumerable.Range(1, 17).Select(index => $"term{index}"));

        var componentAction = () => _catalog.ListComponents(query: query);
        var referenceAction = () => _catalog.ListReferences(query: query);
        var searchAction = () => _catalog.Search(query);

        componentAction.Should().Throw<CatalogValidationException>().Which.ParameterName.Should().Be("query");
        referenceAction.Should().Throw<CatalogValidationException>().Which.ParameterName.Should().Be("query");
        searchAction.Should().Throw<CatalogValidationException>().Which.ParameterName.Should().Be("query");
    }

    [Fact]
    public void WithEquivalentDistinctTerms_ReturnsEquivalentRankings() {
        var expected = _catalog.Search("dialog elevation", 200);

        var actual = _catalog.Search("  DIALOG, dialog; elevation  ", 200);

        actual.Should().Equal(expected);
    }
}
