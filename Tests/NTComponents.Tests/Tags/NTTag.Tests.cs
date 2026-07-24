namespace NTComponents.Tests.Tags;

public class NTTag_Tests : BunitContext {

    [Fact]
    public void Applies_Custom_Class_From_AdditionalAttributes() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "class", "my-custom" } };

        // Act
        var cut = Render<NTTag>(p => p
            .Add(tag => tag.AdditionalAttributes, attrs)
            .Add(tag => tag.Label, "Content"));
        var root = cut.Find("span.nt-tag");

        // Assert
        root.GetAttribute("class")!.Should().Contain("my-custom");
        root.GetAttribute("class")!.Should().Contain("nt-tag");
    }

    [Fact]
    public void Default_Style_Includes_Default_Color_Variables() {
        // Act
        var cut = Render<NTTag>(p => p.Add(tag => tag.Label, "Content"));
        var style = cut.Find("span.nt-tag").GetAttribute("style");

        // Assert
        style.Should().NotBeNull();
        style!.Should().Contain("--nt-tag-background-color:var(--tnt-color-secondary-container)");
        style.Should().Contain("--nt-tag-text-color:var(--tnt-color-on-secondary-container)");
    }

    [Fact]
    public void Default_Class_Includes_Default_Elevation() {
        // Act
        var cut = Render<NTTag>(p => p.Add(tag => tag.Label, "Docs"));

        // Assert
        cut.Find("span.nt-tag").GetAttribute("class")!.Should().Contain("nt-elevation-lowest");
    }

    [Fact]
    public void Elevation_Can_Be_Overridden() {
        // Act
        var cut = Render<NTTag>(p => p
            .Add(tag => tag.Label, "Docs")
            .Add(tag => tag.Elevation, NTElevation.High));

        // Assert
        var cls = cut.Find("span.nt-tag").GetAttribute("class")!;
        cls.Should().Contain("nt-elevation-high");
        cls.Should().NotContain("nt-elevation-lowest");
    }

    [Fact]
    public void Renders_Label() {
        // Act
        var cut = Render<NTTag>(p => p.Add(tag => tag.Label, "Docs"));

        // Assert
        cut.Find("span.nt-tag-label").TextContent.Should().Be("Docs");
        cut.FindAll(".nt-tag-icon").Should().BeEmpty();
    }
}
