using Microsoft.AspNetCore.Components;
using NTComponents;

namespace NTComponents.Tests.Buttons;

public class TnTFabContainer_Tests : BunitContext {

    public TnTFabContainer_Tests() {
        TestingUtility.TestingUtility.SetupRippleEffectModule(this);
    }

    [Fact]
    public void Default_Rendering_Has_Base_Classes() {
        // Arrange & Act
        var cut = Render<TnTFabContainer>();
        var div = cut.Find("div.tnt-fab-container");

        // Assert
        div.GetAttribute("class").Should().Contain("tnt-position-bottomright");
        div.GetAttribute("class").Should().Contain("tnt-direction-vertical");
        div.GetAttribute("class").Should().NotContain("tnt-expandable");
    }

    [Theory]
    [InlineData(Corner.TopLeft, "tnt-position-topleft")]
    [InlineData(Corner.TopRight, "tnt-position-topright")]
    [InlineData(Corner.BottomLeft, "tnt-position-bottomleft")]
    [InlineData(Corner.BottomRight, "tnt-position-bottomright")]
    public void Position_Parameter_Sets_Correct_Class(Corner corner, string expectedClass) {
        // Arrange & Act
        var cut = Render<TnTFabContainer>(p => p.Add(c => c.Position, corner));
        var div = cut.Find("div.tnt-fab-container");

        // Assert
        div.GetAttribute("class").Should().Contain(expectedClass);
    }

    [Theory]
    [InlineData(LayoutDirection.Vertical, "tnt-direction-vertical")]
    [InlineData(LayoutDirection.Horizontal, "tnt-direction-horizontal")]
    public void Direction_Parameter_Sets_Correct_Class(LayoutDirection direction, string expectedClass) {
        // Arrange & Act
        var cut = Render<TnTFabContainer>(p => p.Add(c => c.Direction, direction));
        var div = cut.Find("div.tnt-fab-container");

        // Assert
        div.GetAttribute("class").Should().Contain(expectedClass);
    }

    [Theory]
    [InlineData(FabExpandMode.Never, false, false, false)]
    [InlineData(FabExpandMode.Always, true, true, false)]
    [InlineData(FabExpandMode.SmallScreens, true, false, true)]
    public void ExpandMode_Parameter_Sets_Correct_Classes(FabExpandMode mode, bool expandable, bool always, bool small) {
        // Arrange & Act
        var cut = Render<TnTFabContainer>(p => p.Add(c => c.ExpandMode, mode));
        var div = cut.Find("div.tnt-fab-container");
        var cls = div.GetAttribute("class")!;

        // Assert
        if (expandable) cls.Should().Contain("tnt-expandable"); else cls.Should().NotContain("tnt-expandable");
        if (always) cls.Should().Contain("tnt-expand-always"); else cls.Should().NotContain("tnt-expand-always");
        if (small) cls.Should().Contain("tnt-expand-small"); else cls.Should().NotContain("tnt-expand-small");
    }

    [Fact]
    public void Renders_ChildContent() {
        // Arrange & Act
        var cut = Render<TnTFabContainer>(p => p.AddChildContent("<span class='child'>Content</span>"));
        
        // Assert
        cut.Find("div.tnt-fab-children .child").Should().NotBeNull();
    }

    [Fact]
    public void Renders_ToggleContent() {
        // Arrange & Act
        var cut = Render<TnTFabContainer>(p => p.Add(c => c.ToggleContent, (RenderFragment)(builder => builder.AddMarkupContent(0, "<span class='toggle'>Toggle</span>"))));
        
        // Assert
        cut.Find("div.tnt-fab-toggle .toggle").Should().NotBeNull();
    }

    [Fact]
    public void Provides_CascadingValue_To_TnTFabButton() {
        // Arrange & Act
        var cut = Render<TnTFabContainer>(p => p.AddChildContent<TnTFabButton>());
        var button = cut.FindComponent<TnTFabButton>();

        // Assert
        button.Instance.Container.Should().NotBeNull();
        button.Instance.Container.Should().Be(cut.Instance);
        
        // Also check if button has the container class
        cut.Find("button").GetAttribute("class").Should().Contain("tnt-in-container");
    }
}
