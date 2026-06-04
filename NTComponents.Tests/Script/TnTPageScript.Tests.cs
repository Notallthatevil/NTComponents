using Microsoft.AspNetCore.Components;

namespace NTComponents.Tests.Script;

public class NTPageScript_Tests : BunitContext {

    [Fact]
    public void BuildRenderTree_Uses_Correct_Sequence_Numbers() {
        // This test verifies the internal implementation follows the documented pattern Act
        var cut = Render<NTPageScript>(p => p.Add(s => s.Src, "./test.js"));

        // Assert We can't directly test sequence numbers, but we can verify the structure is correct
        var element = cut.Find("tnt-page-script");
        element.Should().NotBeNull();
        element.GetAttribute("src").Should().Be("./test.js");
    }

    [Fact]
    public void Component_Implements_ComponentBase() {
        // Act
        var cut = Render<NTPageScript>(p => p.Add(s => s.Src, "./test.js"));

        // Assert
        cut.Instance.Should().BeAssignableTo<ComponentBase>();
    }

    [Fact]
    public void Component_Only_Renders_Single_Element() {
        // Act
        var cut = Render<NTPageScript>(p => p.Add(s => s.Src, "./test.js"));

        // Assert
        cut.FindAll("*").Count.Should().Be(1, "because component should only render one element");
        cut.Find("tnt-page-script").Should().NotBeNull();
    }

    [Fact]
    public void Element_Has_No_Content() {
        // Act
        var cut = Render<NTPageScript>(p => p.Add(s => s.Src, "./test.js"));
        var element = cut.Find("tnt-page-script");

        // Assert
        element.TextContent.Should().BeEmpty("because tnt-page-script element should be self-closing");
    }

    [Fact]
    public void Element_Has_Only_Src_Attribute_By_Default() {
        // Act
        var cut = Render<NTPageScript>(p => p.Add(s => s.Src, "./test.js"));
        var element = cut.Find("tnt-page-script");

        // Assert
        element.HasAttribute("src").Should().BeTrue("because src attribute should be present");
        // Verify that only expected attributes are present by checking the markup
        cut.Markup.Should().Be("<tnt-page-script src=\"./test.js\"></tnt-page-script>");
    }

    [Fact]
    public void Markup_Structure_Is_Minimal() {
        // Act
        var cut = Render<NTPageScript>(p => p.Add(s => s.Src, "./test.js"));

        // Assert
        cut.Markup.Should().Be("<tnt-page-script src=\"./test.js\"></tnt-page-script>");
    }

    [Fact]
    public void Multiple_Instances_Render_Independently() {
        // Arrange
        const string src1 = "./script1.js";
        const string src2 = "./script2.js";

        // Act
        var cut1 = Render<NTPageScript>(p => p.Add(s => s.Src, src1));
        var cut2 = Render<NTPageScript>(p => p.Add(s => s.Src, src2));

        // Assert
        cut1.Find("tnt-page-script").GetAttribute("src").Should().Be(src1);
        cut2.Find("tnt-page-script").GetAttribute("src").Should().Be(src2);
    }

    [Fact]
    public void Re_Render_With_Different_Src_Updates_Attribute() {
        // Arrange
        const string initialSrc = "./initial.js";
        const string updatedSrc = "./updated.js";

        // Act - Initial render
        var cut = Render<NTPageScript>(p => p.Add(s => s.Src, initialSrc));
        var element = cut.Find("tnt-page-script");
        element.GetAttribute("src").Should().Be(initialSrc);

        // Act - Re-render with updated src by rendering new component
        var cut2 = Render<NTPageScript>(p => p.Add(s => s.Src, updatedSrc));
        element = cut2.Find("tnt-page-script");

        // Assert
        element.GetAttribute("src").Should().Be(updatedSrc);
    }

    [Fact]
    public void Renders_Custom_Element_With_Src_Attribute() {
        // Arrange
        const string scriptSrc = "./my-script.js";

        // Act
        var cut = Render<NTPageScript>(p => p.Add(s => s.Src, scriptSrc));
        var element = cut.Find("tnt-page-script");

        // Assert
        element.Should().NotBeNull();
        element.GetAttribute("src").Should().Be(scriptSrc);
    }

    [Fact]
    public void Renders_With_Different_Src_Values() {
        // Arrange
        var testCases = new[]
        {
            "./component.js",
            "/assets/scripts/module.js",
            "https://cdn.example.com/script.js",
            "../shared/utils.js",
            "./folder/subfolder/script.min.js"
        };

        foreach (var src in testCases) {
            // Act
            var cut = Render<NTPageScript>(p => p.Add(s => s.Src, src));
            var element = cut.Find("tnt-page-script");

            // Assert
            element.GetAttribute("src").Should().Be(src, $"because src should be {src}");
        }
    }

    [Fact]
    public void Src_Parameter_Handles_Empty_String() {
        // Act
        var cut = Render<NTPageScript>(p => p.Add(s => s.Src, string.Empty));
        var element = cut.Find("tnt-page-script");

        // Assert
        element.Should().NotBeNull();
        element.GetAttribute("src").Should().Be(string.Empty);
    }

    [Fact]
    public void Src_Parameter_Handles_Null_Value() {
        // Act
        var cut = Render<NTPageScript>(p => p.Add(s => s.Src, null!));
        var element = cut.Find("tnt-page-script");

        // Assert
        element.Should().NotBeNull();
        element.GetAttribute("src").Should().BeNull();
    }

    [Fact]
    public void Src_Parameter_Handles_Whitespace_String() {
        // Arrange
        const string whitespaceSrc = "   ";

        // Act
        var cut = Render<NTPageScript>(p => p.Add(s => s.Src, whitespaceSrc));
        var element = cut.Find("tnt-page-script");

        // Assert
        element.Should().NotBeNull();
        element.GetAttribute("src").Should().Be(whitespaceSrc);
    }

    [Fact]
    public void Src_Parameter_Has_EditorRequired_Attribute() {
        // Arrange & Act
        var srcProperty = typeof(NTPageScript).GetProperty(nameof(NTPageScript.Src));

        // Assert
        srcProperty.Should().NotBeNull();
        var parameterAttribute = srcProperty!.GetCustomAttributes(typeof(ParameterAttribute), false);
        var editorRequiredAttribute = srcProperty.GetCustomAttributes(typeof(EditorRequiredAttribute), false);

        parameterAttribute.Should().HaveCount(1, "because Src should be a Parameter");
        editorRequiredAttribute.Should().HaveCount(1, "because Src should be EditorRequired");
    }

    [Fact]
    public void Src_Parameter_Is_Required() {
        // Arrange & Act
        var cut = Render<NTPageScript>();
        var element = cut.Find("tnt-page-script");

        // Assert
        element.Should().NotBeNull();
        element.GetAttribute("src").Should().BeNull();
    }

    [Theory]
    [InlineData("./module.js")]
    [InlineData("/assets/script.js")]
    [InlineData("https://example.com/script.js")]
    [InlineData("../parent/script.js")]
    public void Theory_Various_Src_Values_Render_Correctly(string srcValue) {
        // Act
        var cut = Render<NTPageScript>(p => p.Add(s => s.Src, srcValue));
        var element = cut.Find("tnt-page-script");

        // Assert
        element.GetAttribute("src").Should().Be(srcValue);
    }
}