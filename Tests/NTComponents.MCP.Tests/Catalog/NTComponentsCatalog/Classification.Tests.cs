using NTComponents.MCP.Catalog;

namespace NTComponents.MCP.Tests.Catalog;

public class Classification_Tests {
    private static readonly string[] NestedImplementationTypeNames = [
        "DateTimeInputMetadata",
        "ItemRenderState",
        "NTDataGridObjectComparer",
        "ResolvedSort",
        "SchedulerEventSegment",
        "SearchOption",
        "SortPlan",
        "UriState",
    ];

    [Fact]
    public void WithGeneratedDocumentation_ExcludesNestedImplementationTypes() {
        var catalog = new NTComponentsCatalog();

        var componentNames = catalog.ListComponents(includeObsolete: true, limit: 200).Select(component => component.Name);

        componentNames.Should().NotContain(NestedImplementationTypeNames);
    }

    [Theory]
    [InlineData("NTButtonGroup")]
    [InlineData("NTDataGrid")]
    [InlineData("NTInputDateTime")]
    [InlineData("NTInputSelect")]
    [InlineData("NTScheduler")]
    public void WithGenericComponentName_ResolvesComponent(string componentName) {
        var catalog = new NTComponentsCatalog();

        var component = catalog.GetComponent(componentName);

        component.Should().NotBeNull();
        component!.Name.Should().Be(componentName);
    }
}
