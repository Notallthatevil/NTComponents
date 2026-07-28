using System.Text.Json;
using ModelContextProtocol;
using NTComponents.MCP.Catalog;
using NTComponents.MCP.Resources;

namespace NTComponents.MCP.Tests.Resources;

public class NTComponentsResources_Tests {
    private readonly NTComponentsCatalog _catalog = new();
    private readonly NTComponentsResources _resources;

    public NTComponentsResources_Tests() {
        _resources = new(_catalog);
    }

    /// <summary>Behavior source: the ntcomponents_catalog resource description promises current versions, documentation location, catalog counts, and supported filters.</summary>
    [Fact]
    public void CatalogOverview_ReturnsStructuredNonemptyCatalog() {
        using var document = JsonDocument.Parse(_resources.GetCatalogOverview());

        document.RootElement.GetProperty("serverVersion").GetString().Should().NotBeNullOrWhiteSpace();
        document.RootElement.GetProperty("componentsVersion").GetString().Should().NotBeNullOrWhiteSpace();
        document.RootElement.GetProperty("documentationBaseUrl").GetString().Should().Be("https://ntcomponents.nttechnologies.dev");
        document.RootElement.GetProperty("componentCount").GetInt32().Should().BeGreaterThan(0);
        document.RootElement.GetProperty("referenceTypeCount").GetInt32().Should().BeGreaterThan(0);
        document.RootElement.GetProperty("referenceScopes").EnumerateArray().Select(item => item.GetString()).Should().Equal("ComponentApi", "LibraryApi");
    }

    /// <summary>Behavior source: the component resource description accepts a simple type name and promises structured documentation for that component.</summary>
    [Fact]
    public void Component_WithSimpleName_ReturnsStructuredDocumentationWithoutExpandedEnums() {
        var json = _resources.GetComponent("NTButton");
        using var document = JsonDocument.Parse(json);

        json.Should().NotContain("\r\n");
        document.RootElement.GetProperty("name").GetString().Should().Be("NTButton");
        document.RootElement.GetProperty("parameters").GetArrayLength().Should().BeGreaterThan(0);
        document.RootElement.GetProperty("relatedTypes").EnumerateArray().Select(item => item.GetProperty("name").GetString()).Should().Contain("NTButtonVariant");
        document.RootElement.GetProperty("relatedEnums").GetArrayLength().Should().Be(0);
        document.RootElement.GetProperty("documentationUrl").GetString().Should().Be("https://ntcomponents.nttechnologies.dev/components/ntbutton");
    }

    /// <summary>Behavior source: the component resource is a single-item lookup, and the established lookup contract distinguishes an absent valid name from a returned document.</summary>
    [Fact]
    public void Component_WithUnknownName_ReturnsToolErrorAndAllowsSubsequentRead() {
        var action = () => _resources.GetComponent("NTDoesNotExist");

        action.Should().Throw<McpException>().WithMessage("*NTDoesNotExist*");
        JsonDocument.Parse(_resources.GetCatalogOverview()).RootElement.GetProperty("componentCount").GetInt32().Should().BeGreaterThan(0);
    }

    /// <summary>Behavior source: the reference resource description accepts a full type name and promises structured documentation for one enum or helper.</summary>
    [Fact]
    public void Reference_WithFullName_ReturnsEnumValuesAndUsage() {
        using var document = JsonDocument.Parse(_resources.GetReference("NTComponents.NTButtonVariant"));

        document.RootElement.GetProperty("name").GetString().Should().Be("NTButtonVariant");
        document.RootElement.GetProperty("kind").GetString().Should().Be("Enum");
        document.RootElement.GetProperty("members").GetProperty("items").EnumerateArray().Select(item => item.GetProperty("name").GetString()).Should().Contain("Filled");
        document.RootElement.GetProperty("usedByComponents").EnumerateArray().Select(item => item.GetString()).Should().Contain("NTButton");
    }

    /// <summary>Behavior source: the reference resource is a single-item lookup, and the established lookup contract distinguishes an absent valid name from a returned document.</summary>
    [Fact]
    public void Reference_WithUnknownName_ReturnsToolErrorAndAllowsSubsequentRead() {
        var action = () => _resources.GetReference("NTDoesNotExist");

        action.Should().Throw<McpException>().WithMessage("*NTDoesNotExist*");
        JsonDocument.Parse(_resources.GetCatalogOverview()).RootElement.GetProperty("referenceTypeCount").GetInt32().Should().BeGreaterThan(0);
    }

    /// <summary>Behavior source: the catalog resource is advertised as read-only, so repeated reads must return identical structured content.</summary>
    [Fact]
    public void CatalogOverview_WhenRepeated_IsIdempotent() {
        var first = _resources.GetCatalogOverview();

        var second = _resources.GetCatalogOverview();

        second.Should().Be(first);
    }

    /// <summary>Behavior source: exhaustive resources are an on-demand escape hatch, but each individual document remains bounded for model context.</summary>
    [Fact]
    public void EveryDocumentationResource_StaysWithinSerializedBudget() {
        var components = _catalog.ListComponentPage(limit: 200).Items;
        var references = _catalog.ListReferencePage(limit: 200).Items;

        components.Should().NotBeEmpty();
        references.Should().NotBeEmpty();
        foreach (var component in components) {
            _resources.GetComponent(component.Name).Length.Should().BeLessThan(30_000, $"{component.Name} should defer enum values to its reference resource");
        }

        foreach (var reference in references) {
            _resources.GetReference(reference.Name).Length.Should().BeLessThan(20_000, $"{reference.Name} should remain suitable for targeted retrieval");
        }
    }
}
