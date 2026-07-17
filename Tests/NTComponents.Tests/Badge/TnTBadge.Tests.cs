namespace NTComponents.Tests.Badge;

public class TnTBadge_Tests : BunitContext {

    [Fact]
    public void Applies_Custom_Class_From_AdditionalAttributes() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "class", "my-custom" } };

        // Act
        var cut = Render<TnTBadge>(p => p
            .Add(b => b.AdditionalAttributes, attrs)
            .AddChildContent("Content"));
        var root = cut.Find("span.tnt-badge");

        // Assert
        root.GetAttribute("class")!.Should().Contain("my-custom");
        root.GetAttribute("class")!.Should().Contain("tnt-badge");
    }

    [Fact]
    public void Default_Style_Includes_Default_Color_Variables() {
        // Act
        var cut = Render<TnTBadge>();
        var style = cut.Find("span.tnt-badge").GetAttribute("style");
        // Assert
        style.Should().NotBeNull();
        style!.Should().Contain("--tnt-badge-background-color:var(--tnt-color-error)");
        style.Should().Contain("--tnt-badge-text-color:var(--tnt-color-on-error)");
    }

    [Fact]
    public void Dynamic_Color_Update_Changes_Style() {
        // Arrange - initial render
        var cut = Render<TnTBadge>(p => p
            .Add(b => b.BackgroundColor, TnTColor.Primary)
            .Add(b => b.TextColor, TnTColor.OnPrimary));
        var initial = cut.Find("span.tnt-badge").GetAttribute("style")!;
        initial.Should().Contain("primary");
        // Act - re-render with new parameters (render a new component instance simulating update)
        cut = Render<TnTBadge>(p => p
            .Add(b => b.BackgroundColor, TnTColor.Success)
            .Add(b => b.TextColor, TnTColor.OnSuccess));
        var updated = cut.Find("span.tnt-badge").GetAttribute("style")!;
        // Assert
        updated.Should().Contain("success");
        updated.Should().NotContain("primary)");
        updated.Should().Contain("on-success");
        updated.Should().NotContain("on-primary");
    }

    [Fact]
    public void Factory_CreateBadge_No_Class_Or_Style_Leaves_Empty_AdditionalAttributes() {
        // Act
        var badge = TnTBadge.CreateBadge("Simple");

        // Assert
        badge.BackgroundColor.Should().Be(TnTColor.Error); // default
        badge.TextColor.Should().Be(TnTColor.OnError); // default
        badge.AdditionalAttributes.Should().NotBeNull();
        badge.AdditionalAttributes!.Count.Should().Be(0);
    }

    [Fact]
    public void Factory_CreateBadge_Sets_Properties_And_AdditionalAttributes() {
        // Act
        var badge = TnTBadge.CreateBadge("Factory", TnTColor.Success, TextAlign.Center, TnTColor.OnSuccess, "extra-class", "margin:2px");

        // Assert
        badge.BackgroundColor.Should().Be(TnTColor.Success);
        badge.TextColor.Should().Be(TnTColor.OnSuccess);
        badge.TextAlignment.Should().Be(TextAlign.Center);
        badge.AdditionalAttributes.Should().NotBeNull();
        badge.AdditionalAttributes!.Should().ContainKey("class").WhoseValue.Should().Be("extra-class");
        badge.AdditionalAttributes!.Should().ContainKey("style").WhoseValue.Should().Be("margin:2px");
        badge.ChildContent.Should().NotBeNull();
    }

    [Fact]
    public void Has_TnTId_Attribute() {
        // Act
        var cut = Render<TnTBadge>();
        var root = cut.Find("span.tnt-badge");
        // Assert
        root.HasAttribute("tntid").Should().BeTrue();
        root.GetAttribute("tntid").Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Merges_Custom_Style_From_AdditionalAttributes() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "style", "color:red" } };

        // Act
        var cut = Render<TnTBadge>(p => p
            .Add(b => b.AdditionalAttributes, attrs)
            .AddChildContent("Styled"));
        var style = cut.Find("span.tnt-badge").GetAttribute("style");

        // Assert
        style.Should().NotBeNull();
        style!.Should().Contain("color:red");
        style.Should().Contain("--tnt-badge-background-color");
        style.Should().Contain("--tnt-badge-text-color");
    }

    [Fact]
    public void Merges_Multiple_Custom_Classes() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "class", "foo bar" } };
        // Act
        var cut = Render<TnTBadge>(p => p.Add(b => b.AdditionalAttributes, attrs));
        var cls = cut.Find("span.tnt-badge").GetAttribute("class")!;
        // Assert
        cls.Should().Contain("foo");
        cls.Should().Contain("bar");
        cls.Should().Contain("tnt-badge");
    }

    [Fact]
    public void Null_ChildContent_Renders_Empty_Inner_Span() {
        // Act
        var cut = Render<TnTBadge>();
        var inner = cut.Find("span.tnt-badge > span.tnt-badge-content");
        // Assert
        inner.TextContent.Should().Be(string.Empty);
    }

    [Fact]
    public void Renders_Badge_With_Default_Classes() {
        // Act
        var cut = Render<TnTBadge>(p => p.AddChildContent("Hello"));
        var root = cut.Find("span.tnt-badge");

        // Assert
        root.GetAttribute("class")!.Should().Contain("tnt-badge");
        root.GetAttribute("class")!.Should().Contain("tnt-components");
        cut.Markup.Should().Contain("Hello");
    }

    [Fact]
    public void Style_Custom_Semicolon_Variations_Normalized() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "style", "color:blue;" } };
        // Act
        var cut = Render<TnTBadge>(p => p.Add(b => b.AdditionalAttributes, attrs));
        var style = cut.Find("span.tnt-badge").GetAttribute("style")!;
        // Assert
        style.Count(c => c == ';').Should().BeGreaterThan(0); // has semicolons
        style.Should().Contain("color:blue");
    }

    [Fact]
    public void Style_Includes_Background_And_Text_Color_Variables() {
        // Act
        var cut = Render<TnTBadge>(p => p
            .Add(b => b.BackgroundColor, TnTColor.Primary)
            .Add(b => b.TextColor, TnTColor.OnPrimary)
            .AddChildContent("X"));
        var style = cut.Find("span.tnt-badge").GetAttribute("style");

        // Assert
        style.Should().NotBeNull();
        style!.Should().Contain("--tnt-badge-background-color:var(--tnt-color-primary)");
        style.Should().Contain("--tnt-badge-text-color:var(--tnt-color-on-primary)");
    }

    [Fact]
    public void TextAlignment_Does_Not_Add_Class_Currently() {
        // Sentinel regression test
        var cut = Render<TnTBadge>(p => p.Add(b => b.TextAlignment, TextAlign.Center));
        var cls = cut.Find("span.tnt-badge").GetAttribute("class")!;
        cls.Should().NotContain("tnt-text-align-center");
    }
}