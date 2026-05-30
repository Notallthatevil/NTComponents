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
        buttonType.BaseTypeFullName.Should().Be("NTComponents.Core.TnTComponentBase");
        buttonSizeProperty.Should().NotBeNull();
        buttonSizeProperty!.Summary.Should().Contain("The size of the button.");
    }

    [Fact]
    public void Model_Contains_Method_And_Field_Documentation() {
        // Arrange
        var model = GeneratedCodeDocumentation.Model;

        // Act
        var wizardStepType = model.Types.FirstOrDefault(x => x.FullName == "NTComponents.Wizard.TnTWizardStepBase");
        var renderMethod = wizardStepType?.Methods.FirstOrDefault(x => x.Name == "Render");

        var sizeEnumType = model.Types.FirstOrDefault(x => x.FullName == "NTComponents.Size");
        var smallestField = sizeEnumType?.Fields.FirstOrDefault(x => x.Name == "Smallest");

        // Assert
        renderMethod.Should().NotBeNull();
        renderMethod!.Summary.Should().Contain("Renders the content of the wizard step.");
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
                 x.DeclaringTypeFullName == "NTComponents.Core.TnTComponentBase");

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
    public void Model_Identifies_Obsolete_Types() {
        // Arrange
        var model = GeneratedCodeDocumentation.Model;

        // Act
        var cardType = model.Types.FirstOrDefault(x => x.FullName == "NTComponents.TnTCard");

        // Assert
        cardType.Should().NotBeNull();
        cardType!.IsObsolete.Should().BeTrue();
        cardType.ObsoleteMessage.Should().Be("Use NTCard instead.");
        cardType.IsObsoleteError.Should().BeFalse();
    }
}
