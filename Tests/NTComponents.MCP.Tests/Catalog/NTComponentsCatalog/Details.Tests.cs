using NTComponents.MCP.Catalog;

namespace NTComponents.MCP.Tests.Catalog;

public class Details_Tests {
    [Fact]
    public void GetComponent_ExcludesFrameworkInfrastructureMethods() {
        var catalog = new NTComponentsCatalog();

        var component = catalog.GetComponent("NTButtonGroup");

        component.Should().NotBeNull();
        component!.Methods.Should().NotContain(method => method.Name == "OnParametersSet" || method.Name == "SetParametersAsync" || method.Name == "Dispose");
    }

    [Fact]
    public void GetComponent_PreservesConsumerMethodsAndAccessibility() {
        var catalog = new NTComponentsCatalog();

        var component = catalog.GetComponent("NTDialog");

        component.Should().NotBeNull();
        component!.Methods.Should().Contain(method => method.Name == "OpenAsync" && method.Accessibility == "Public");
        component.Methods.Should().NotContain(method => method.Name == "OnParametersSet" || method.Name == "OnAfterRenderAsync");
    }

    [Fact]
    public void GetComponent_IncludesParameterAccessibilityAndDeclaredDefaultExpression() {
        var catalog = new NTComponentsCatalog();

        var component = catalog.GetComponent("NTButton");

        component.Should().NotBeNull();
        component!.Parameters.Should().Contain(parameter => parameter.Name == "Label" && parameter.IsRequired && parameter.Accessibility == "Public");
        component.Parameters.Should().Contain(parameter => parameter.Name == "Variant" && parameter.Accessibility == "Public" && parameter.DefaultValueExpression == "NTButtonVariant.Filled");
        component.Parameters.Should().Contain(parameter => parameter.Name == "Elevation" && parameter.DefaultValueExpression == null);
    }

    [Fact]
    public void GetComponent_UsesDocumentedRazorExampleWhenAvailable() {
        var catalog = new NTComponentsCatalog();

        var component = catalog.GetComponent("NTAutocomplete");

        component.Should().NotBeNull();
        component!.RazorUsage.Should().Contain("<NTAutocomplete");
        component.RazorUsage.Should().Contain("@bind-Value=\"_city\"");
        component.RazorUsage.Should().Contain("<NTAutocompleteOptionGroup Label=\"Texas\">");
        component.RazorUsage.Should().NotContain("&lt;");
    }

    [Fact]
    public void GetComponent_GeneratesMinimalRazorUsageFromRequiredParameters() {
        var catalog = new NTComponentsCatalog();

        var component = catalog.GetComponent("NTButton");

        component.Should().NotBeNull();
        component!.RazorUsage.Should().StartWith("<NTButton");
        component.RazorUsage.Should().Contain("Label=\"TODO\"");
        component.RazorUsage.Should().EndWith("/>");
    }

    [Fact]
    public void GetComponent_OnlyExpandsRelatedEnumValuesWhenRequested() {
        var catalog = new NTComponentsCatalog();

        var summary = catalog.GetComponent("NTButton");
        var expanded = catalog.GetComponent("NTButton", includeRelatedEnumValues: true);

        summary.Should().NotBeNull();
        summary!.RelatedEnums.Should().BeEmpty();
        expanded.Should().NotBeNull();
        expanded!.RelatedEnums.Should().Contain(enumDetails => enumDetails.Name == "NTButtonVariant" && enumDetails.Values.Count > 0 && !enumDetails.IsTruncated);
        expanded.RelatedEnums.Should().Contain(enumDetails => enumDetails.Name == "TnTColor" && enumDetails.Values.Count == 20 && enumDetails.TotalValueCount > 20 && enumDetails.IsTruncated);
    }
}
