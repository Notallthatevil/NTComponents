using System.Text.Json;
using ModelContextProtocol;
using NTComponents.MCP.Catalog;
using NTComponents.MCP.Resources;

namespace NTComponents.MCP.Tests.Resources;

public class NTComponentsResources_Tests {
    private readonly NTComponentsResources _resources = new(new NTComponentsCatalog());

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
    public void Component_WithSimpleName_ReturnsExpandedStructuredDocumentation() {
        using var document = JsonDocument.Parse(_resources.GetComponent("NTButton"));

        document.RootElement.GetProperty("name").GetString().Should().Be("NTButton");
        document.RootElement.GetProperty("parameters").GetArrayLength().Should().BeGreaterThan(0);
        document.RootElement.GetProperty("relatedEnums").EnumerateArray().Select(item => item.GetProperty("name").GetString()).Should().Contain("NTButtonVariant");
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
        document.RootElement.GetProperty("fields").EnumerateArray().Select(item => item.GetProperty("name").GetString()).Should().Contain("Filled");
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
}
