namespace NTComponents.Tests.Tags;

public class NTTag_Tests : BunitContext {

    [Fact]
    public void Applies_Custom_Class_From_AdditionalAttributes() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "class", "my-custom" } };

        // Act
        var cut = Render<NTTag>(p => p
            .Add(tag => tag.AdditionalAttributes, attrs)
            .AddChildContent("Content"));
        var root = cut.Find("span.nt-tag");

        // Assert
        root.GetAttribute("class")!.Should().Contain("my-custom");
        root.GetAttribute("class")!.Should().Contain("nt-tag");
    }

    [Fact]
    public void Default_Style_Includes_Default_Color_Variables() {
        // Act
        var cut = Render<NTTag>();
        var style = cut.Find("span.nt-tag").GetAttribute("style");

        // Assert
        style.Should().NotBeNull();
        style!.Should().Contain("--nt-tag-background-color:var(--tnt-color-secondary-container)");
        style.Should().Contain("--nt-tag-text-color:var(--tnt-color-on-secondary-container)");
    }

    [Fact]
    public void Renders_Content_And_Icons() {
        // Act
        var cut = Render<NTTag>(p => p
            .Add(tag => tag.StartIcon, MaterialIcon.Article)
            .Add(tag => tag.EndIcon, MaterialIcon.Close)
            .AddChildContent("Docs"));

        // Assert
        cut.Find("span.nt-tag-content").TextContent.Should().Be("Docs");
        cut.FindAll(".nt-tag-icon").Count.Should().Be(2);
    }
}
