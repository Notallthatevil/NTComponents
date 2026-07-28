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

        action.Should().Throw<McpException>().WithMessage("limit must be between 1 and 50.");
    }

    /// <summary>Behavior source: the existing MCP lookup contract represents a valid unknown name as Found=false.</summary>
    [Fact]
    public void WithValidUnknownName_ReturnsMissingLookupResult() {
        var tools = new NTComponentsTools(new NTComponentsCatalog());

        var component = tools.GetComponent("NTDoesNotExist");
        var componentMembers = tools.GetComponentMembers("NTDoesNotExist");
        var reference = tools.GetReferenceType("NTDoesNotExist");

        component.Found.Should().BeFalse();
        component.Error.Should().NotBeNull();
        componentMembers.Found.Should().BeFalse();
        componentMembers.Error.Should().NotBeNull();
        reference.Found.Should().BeFalse();
        reference.Error.Should().NotBeNull();
    }

    /// <summary>Behavior source: MCP paging is deliberately narrower than REST paging to protect model context size.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(51)]
    public void WithOutOfRangeMcpLimit_ThrowsMcpException(int limit) {
        var tools = new NTComponentsTools(new NTComponentsCatalog());

        var action = () => tools.ListComponents(limit: limit);

        action.Should().Throw<McpException>().WithMessage("limit must be between 1 and 50.");
    }
}
