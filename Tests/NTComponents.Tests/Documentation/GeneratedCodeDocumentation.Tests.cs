using Microsoft.AspNetCore.Components;
using NTComponents;
using NTComponents.GeneratedDocumentation;

namespace NTComponents.Tests.Documentation;

public class GeneratedCodeDocumentation_Tests {

    [Fact]
    public void Model_Contains_Assembly_Metadata() {
        // Act
        var model = GeneratedCodeDocumentation.Model;

        // Assert
        model.AssemblyName.Should().Be("NTComponents");
        model.Types.Should().NotBeEmpty();
    }

    [Fact]
    public void Model_Contains_Class_And_Property_Documentation() {
        // Arrange
        var model = GeneratedCodeDocumentation.Model;

        // Act
        var buttonType = model.Types.FirstOrDefault(x => x.FullName == "NTComponents.TnTButton");
        var buttonSizeProperty = buttonType?.Properties.FirstOrDefault(x => x.Name == "ButtonSize");

        // Assert
        buttonType.Should().NotBeNull();
        buttonType!.Summary.Should().Contain("Represents a customizable button component.");
        buttonSizeProperty.Should().NotBeNull();
        buttonSizeProperty!.Summary.Should().Contain("The size of the button.");
    }

    [Fact]
    public void Model_Contains_Field_Documentation() {
        // Arrange
        var model = GeneratedCodeDocumentation.Model;

        // Act
        var sizeEnumType = model.Types.FirstOrDefault(x => x.FullName == "NTComponents.Size");
        var smallestField = sizeEnumType?.Fields.FirstOrDefault(x => x.Name == "Smallest");

        // Assert
        smallestField.Should().NotBeNull();
        smallestField!.Summary.Should().Contain("The smallest size.");
    }

    [Fact]
    public void Model_Contains_Inherited_Members_With_Base_Metadata() {
        // Arrange
        var model = GeneratedCodeDocumentation.Model;

        // Act
        var buttonType = model.Types.FirstOrDefault(x => x.FullName == "NTComponents.TnTButton");
        var inheritedElementTitleProperty = buttonType?.Properties.FirstOrDefault(
            x => x.Name == "ElementTitle" &&
                 x.IsFromBaseType &&
                 x.DeclaringTypeFullName == "NTComponents.Core.NTComponentBase");

        // Assert
        buttonType.Should().NotBeNull();
        inheritedElementTitleProperty.Should().NotBeNull();
    }

    [Fact]
    public void Model_Groups_Component_Parameters() {
        // Arrange
        var model = GeneratedCodeDocumentation.Model;

        // Act
        var buttonType = model.Types.FirstOrDefault(x => x.FullName == "NTComponents.TnTButton");
        var buttonSizeParameter = buttonType?.Parameters.FirstOrDefault(x => x.Name == "ButtonSize");

        // Assert
        buttonType.Should().NotBeNull();
        buttonType!.Parameters.Should().OnlyContain(x => x.IsParameter);
        buttonSizeParameter.Should().NotBeNull();
        buttonSizeParameter!.Summary.Should().Contain("The size of the button.");
    }

    [Fact]
    public void Model_Contains_Declared_Parameter_Default_Expressions() {
        // Arrange
        var model = GeneratedCodeDocumentation.Model;

        // Act
        var buttonType = model.Types.First(x => x.FullName == "NTComponents.NTButton");
        var shapeParameter = buttonType.Parameters.First(x => x.Name == "Shape");
        var elevationParameter = buttonType.Parameters.First(x => x.Name == "Elevation");

        // Assert
        shapeParameter.DefaultValueExpression.Should().Be("ButtonShape.Round");
        elevationParameter.DefaultValueExpression.Should().BeEmpty();
    }

    [Fact]
    public void PropertyDocumentation_LegacyConstructor_DefaultsDeclaredExpressionToEmpty() {
        var property = new PropertyDocumentation("Name", "Signature", "Public", "string", "string", "Summary", string.Empty, "DeclaringType", false, true, false, false, false, string.Empty, false);

        property.DefaultValueExpression.Should().BeEmpty();
    }

    [Fact]
    public void Model_Groups_Cascading_Parameters() {
        // Arrange
        var model = GeneratedCodeDocumentation.Model;

        // Act
        var accordionType = model.Types.FirstOrDefault(x => x.FullName == "NTComponents.TnTAccordion");
        var parentAccordion = accordionType?.CascadingParameters.FirstOrDefault(x => x.Name == "_parentAccordion");

        // Assert
        accordionType.Should().NotBeNull();
        accordionType!.CascadingParameters.Should().OnlyContain(x => x.IsCascadingParameter);
        parentAccordion.Should().NotBeNull();
        parentAccordion!.Summary.Should().Contain("Gets or sets the parent accordion.");
    }

    [Fact]
    public void Model_Contains_Remarks_And_Source_Metadata() {
        // Arrange
        var model = GeneratedCodeDocumentation.Model;

        // Act
        var buttonType = model.Types.FirstOrDefault(x => x.FullName == "NTComponents.NTButton");

        // Assert
        buttonType.Should().NotBeNull();
        buttonType!.Remarks.Should().Contain("Use the lowest-emphasis variant");
        buttonType.SourceFolder.Should().Be("Buttons");
        buttonType.SourceFileName.Should().Be("NTButton.razor.cs");
    }

    [Fact]
    public void Model_Preserves_Inline_Xml_Documentation_References() {
        // Arrange
        var model = GeneratedCodeDocumentation.Model;

        // Act
        var buttonType = model.Types.First(x => x.FullName == "NTComponents.NTButton");
        var requestType = model.Types.First(x => x.FullName == "NTComponents.NTDataGridItemsProviderRequest<TItem>");
        var countProperty = requestType.Properties.First(x => x.Name == "Count");
        var colorType = model.Types.First(x => x.FullName == "NTComponents.TnTColor");
        var richTextEditorType = model.Types.First(x => x.FullName == "NTComponents.NTRichTextEditor");
        var errorMessageProperty = richTextEditorType.Properties.First(x => x.Name == "ErrorMessage");

        // Assert
        buttonType.Remarks.Should().Contain("NTButtonVariant.Filled for the primary action");
        requestType.Summary.Should().Be("Represents a data request issued by NTDataGrid.");
        countProperty.Summary.Should().Contain("or null when the provider may return all items");
        colorType.Remarks.Should().Contain("areas of the UI. Primary, Secondary");
        errorMessageProperty.Summary.Should().Contain("Prefer NTFormControlBaseCore.ErrorText for new code");
    }

    [Fact]
    public void Model_Contains_Field_Constant_Values() {
        // Arrange
        var model = GeneratedCodeDocumentation.Model;

        // Act
        var buttonVariantType = model.Types.FirstOrDefault(x => x.FullName == "NTComponents.NTButtonVariant");
        var filledField = buttonVariantType?.Fields.FirstOrDefault(x => x.Name == "Filled");

        // Assert
        filledField.Should().NotBeNull();
        filledField!.ConstantValue.Should().NotBeEmpty();
    }

    [Fact]
    public void Model_Contains_Render_Compatibility_Metadata() {
        // Arrange
        var model = GeneratedCodeDocumentation.Model;

        // Act
        var buttonType = model.Types.FirstOrDefault(x => x.FullName == "NTComponents.NTButton");

        // Assert
        buttonType.Should().NotBeNull();
        buttonType!.RenderCompatibility.Should().Be("ProgressivelyEnhanced");
        buttonType.IsSsrCompatible.Should().BeTrue();
        buttonType.CompatibilitySummary.Should().Contain("native button");
        buttonType.CompatibilityDetails.Should().Contain("EventCallback");
    }

    [Fact]
    public void Model_Classifies_All_Public_Nt_Components_Render_Compatibility() {
        // Arrange
        var model = GeneratedCodeDocumentation.Model;
        var documentedTypes = model.Types.ToLookup(type => NormalizeDocumentationFullName(type.FullName), StringComparer.Ordinal);

        // Act
        var unclassifiedComponents = typeof(NTButton).Assembly.GetTypes()
            .Where(type => type.IsPublic)
            .Where(type => type.IsClass)
            .Where(type => !type.IsAbstract)
            .Where(type => typeof(IComponent).IsAssignableFrom(type))
            .Where(type => type.Name.StartsWith("NT", StringComparison.Ordinal))
            .Where(type => !type.Name.StartsWith("TnT", StringComparison.Ordinal))
            .Select(type => new {
                Type = type,
                Documentation = documentedTypes[NormalizeRuntimeFullName(type)]
            })
            .Where(item => !item.Documentation.Any(type => type.RenderCompatibility != "Unknown"))
            .Select(item => item.Type.FullName)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        // Assert
        unclassifiedComponents.Should().BeEmpty("every public NT component should declare NTDocumentationAttribute render compatibility metadata");
    }

    private static string NormalizeRuntimeFullName(Type type) {
        var fullName = type.FullName ?? type.Name;
        var genericIndex = fullName.IndexOf('`');
        return genericIndex < 0 ? fullName : fullName[..genericIndex];
    }

    private static string NormalizeDocumentationFullName(string fullName) {
        var genericIndex = fullName.IndexOf('<');
        return genericIndex < 0 ? fullName : fullName[..genericIndex];
    }
}
