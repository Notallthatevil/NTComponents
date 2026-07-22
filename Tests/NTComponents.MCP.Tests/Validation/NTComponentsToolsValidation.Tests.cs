using ModelContextProtocol;
using NTComponents.MCP.Catalog;
using NTComponents.MCP.Tools;

namespace NTComponents.MCP.Tests.Validation;

public class NTComponentsToolsValidation_Tests {
    /// <summary>Behavior source: MCP validation failures must be returned as tool errors rather than generic server failures.</summary>
    [Fact]
    public void WithInvalidCatalogInput_ThrowsMcpExceptionWithActionableMessage() {
        var tools = new NTComponentsTools(new NTComponentsCatalog());

        var action = () => tools.ListComponents(limit: 0);

        action.Should().Throw<McpException>().WithMessage("limit must be between 1 and 200.");
    }

    /// <summary>Behavior source: the existing MCP lookup contract represents a valid unknown name as Found=false.</summary>
    [Fact]
    public void WithValidUnknownName_ReturnsMissingLookupResult() {
        var tools = new NTComponentsTools(new NTComponentsCatalog());

        var component = tools.GetComponent("NTDoesNotExist");
        var reference = tools.GetReferenceType("NTDoesNotExist");

        component.Found.Should().BeFalse();
        component.Error.Should().NotBeNull();
        reference.Found.Should().BeFalse();
        reference.Error.Should().NotBeNull();
    }
}
