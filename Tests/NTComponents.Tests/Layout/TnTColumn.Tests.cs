using Microsoft.AspNetCore.Components;

namespace NTComponents.Tests.Layout;

public class TnTColumn_Tests : BunitContext {

    [Fact]
    public void AdditionalAttributes_Class_Merged() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "class", "custom-col" } };

        // Act
        var cut = Render<TnTColumn>(p => p.Add(c => c.AdditionalAttributes, attrs));
        var cls = cut.Find("div").GetAttribute("class")!;

        // Assert
        cls.Should().Contain("custom-col");
        cls.Should().Contain("tnt-col");
    }

    [Fact]
    public void AdditionalAttributes_Style_Merged() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "style", "padding:15px" } };

        // Act
        var cut = Render<TnTColumn>(p => p.Add(c => c.AdditionalAttributes, attrs));
        var style = cut.Find("div").GetAttribute("style");

        // Assert
        style.Should().Contain("padding:15px");
    }

    [Fact]
    public void All_Base_Properties_Work_Together() {
        // Arrange
        var attrs = new Dictionary<string, object> {
            { "class", "custom-column" },
            { "style", "min-height:100px" },
            { "data-column", "main" }
        };

        // Act
        var cut = Render<TnTColumn>(p => p
            .Add(c => c.ElementId, "main-col")
            .Add(c => c.ElementTitle, "Main Column")
            .Add(c => c.ElementLang, "ar")
            .Add(c => c.AutoFocus, true)
            .Add(c => c.S, 6)
            .Add(c => c.M, 4)
            .Add(c => c.AdditionalAttributes, attrs)
            .AddChildContent("Column Content"));

        var div = cut.Find("div");

        // Assert
        div.GetAttribute("id")!.Should().Be("main-col");
        div.GetAttribute("title")!.Should().Be("Main Column");
        div.GetAttribute("lang")!.Should().Be("ar");
        div.HasAttribute("autofocus").Should().BeTrue();
        div.GetAttribute("class")!.Should().Contain("custom-column");
        div.GetAttribute("class")!.Should().Contain("tnt-col");
        div.GetAttribute("class")!.Should().Contain("s6");
        div.GetAttribute("class")!.Should().Contain("m4");
        div.GetAttribute("style").Should().Contain("min-height:100px");
        div.GetAttribute("data-column")!.Should().Be("main");
        cut.Markup.Should().Contain("Column Content");
    }

    [Fact]
    public void AutoFocus_True_Renders_Autofocus_Attribute() {
        // Arrange & Act
        var cut = Render<TnTColumn>(p => p.Add(c => c.AutoFocus, true));

        // Assert
        cut.Find("div").HasAttribute("autofocus").Should().BeTrue();
    }

    [Fact]
    public void ChildContent_Renders_Correctly() {
        // Arrange & Act
        var cut = Render<TnTColumn>(p => p.AddChildContent("<p>Column Content</p>"));

        // Assert
        cut.Markup.Should().Contain("<p>Column Content</p>");
    }

    [Fact]
    public void Complex_Grid_Configuration() {
        // Arrange & Act
        var cut = Render<TnTColumn>(p => p
            .Add(c => c.S, 12)
            .Add(c => c.M, 6)
            .Add(c => c.MOffset, 1)
            .Add(c => c.L, 4)
            .Add(c => c.LPush, 2)
            .Add(c => c.XL, 3)
            .Add(c => c.XLOffset, 1)
            .Add(c => c.XLPull, 1));

        var cls = cut.Find("div").GetAttribute("class")!;

        // Assert
        cls.Should().Contain("s12");
        cls.Should().Contain("m6");
        cls.Should().Contain("m1-offset");
        cls.Should().Contain("l4");
        cls.Should().Contain("l2-push");
        cls.Should().Contain("xl3");
        cls.Should().Contain("xl1-offset");
        cls.Should().Contain("xl1-pull");
    }

    [Fact]
    public void Default_Column_Has_S12_Class() {
        // Arrange & Act
        var cut = Render<TnTColumn>();
        var cls = cut.Find("div").GetAttribute("class")!;

        // Assert
        cls.Should().Contain("s12");
    }

    [Fact]
    public void Element_Reference_Captured() {
        // Arrange & Act
        var cut = Render<TnTColumn>();

        // Assert
        cut.Find("div").Should().NotBeNull();
    }

    [Fact]
    public void ElementId_Renders_Id_Attribute() {
        // Arrange & Act
        var cut = Render<TnTColumn>(p => p.Add(c => c.ElementId, "col-1"));

        // Assert
        cut.Find("div").GetAttribute("id")!.Should().Be("col-1");
    }

    [Fact]
    public void ElementLang_Renders_Lang_Attribute() {
        // Arrange & Act
        var cut = Render<TnTColumn>(p => p.Add(c => c.ElementLang, "zh"));

        // Assert
        cut.Find("div").GetAttribute("lang")!.Should().Be("zh");
    }

    [Fact]
    public void ElementTitle_Renders_Title_Attribute() {
        // Arrange & Act
        var cut = Render<TnTColumn>(p => p.Add(c => c.ElementTitle, "Grid column"));

        // Assert
        cut.Find("div").GetAttribute("title")!.Should().Be("Grid column");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(13)]
    public void Invalid_Offset_Values_Throw_ArgumentOutOfRangeException(int invalidOffset) {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            Render<TnTColumn>(p => p.Add(c => c.MOffset, invalidOffset)));

        exception.ParamName.Should().Be("MOffset");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(13)]
    [InlineData(100)]
    public void Invalid_Size_Values_Throw_ArgumentOutOfRangeException(int invalidSize) {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            Render<TnTColumn>(p => p.Add(c => c.S, invalidSize)));

        exception.ParamName.Should().Be("S");
        exception.Message.Should().Contain("Value must be within 0 and 12 inclusively.");
    }

    [Fact]
    public void Large_Pull_Adds_Correct_Class() {
        // Arrange & Act
        var cut = Render<TnTColumn>(p => p
            .Add(c => c.L, 8)
            .Add(c => c.LPull, 1));
        var cls = cut.Find("div").GetAttribute("class")!;

        // Assert
        cls.Should().Contain("l8");
        cls.Should().Contain("l1-pull");
    }

    [Fact]
    public void Large_Size_Adds_Correct_Class() {
        // Arrange & Act
        var cut = Render<TnTColumn>(p => p.Add(c => c.L, 4));
        var cls = cut.Find("div").GetAttribute("class")!;

        // Assert
        cls.Should().Contain("l4");
    }

    [Fact]
    public void Medium_Push_Adds_Correct_Class() {
        // Arrange & Act
        var cut = Render<TnTColumn>(p => p
            .Add(c => c.M, 4)
            .Add(c => c.MPush, 2));
        var cls = cut.Find("div").GetAttribute("class")!;

        // Assert
        cls.Should().Contain("m4");
        cls.Should().Contain("m2-push");
    }

    [Fact]
    public void Medium_Size_Adds_Correct_Class() {
        // Arrange & Act
        var cut = Render<TnTColumn>(p => p.Add(c => c.M, 8));
        var cls = cut.Find("div").GetAttribute("class")!;

        // Assert
        cls.Should().Contain("m8");
    }

    [Fact]
    public void Multiple_Sizes_All_Added() {
        // Arrange & Act
        var cut = Render<TnTColumn>(p => p
            .Add(c => c.S, 12)
            .Add(c => c.M, 6)
            .Add(c => c.L, 4)
            .Add(c => c.XL, 3));
        var cls = cut.Find("div").GetAttribute("class")!;

        // Assert
        cls.Should().Contain("s12");
        cls.Should().Contain("m6");
        cls.Should().Contain("l4");
        cls.Should().Contain("xl3");
    }

    [Fact]
    public void Null_ChildContent_Does_Not_Throw() {
        // Arrange & Act
        var cut = Render<TnTColumn>(p => p.Add(c => c.ChildContent, (RenderFragment?)null));

        // Assert
        cut.Find("div").Should().NotBeNull();
    }

    [Fact]
    public void Renders_Default_Column_With_Base_Class() {
        // Arrange & Act
        var cut = Render<TnTColumn>(p => p.AddChildContent("Column Content"));
        var div = cut.Find("div.tnt-col");
        var cls = div.GetAttribute("class")!;

        // Assert
        cls.Should().Contain("tnt-col");
        cls.Should().Contain("tnt-components");
        cut.Markup.Should().Contain("Column Content");
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(5, 3)]
    [InlineData(12, 0)]
    public void Size_With_Zero_Offset_Only_Shows_Size(int size, int offset) {
        // Arrange & Act
        var cut = Render<TnTColumn>(p => p
            .Add(c => c.L, size)
            .Add(c => c.LOffset, offset));
        var cls = cut.Find("div").GetAttribute("class")!;

        // Assert
        if (size > 0) {
            cls.Should().Contain($"l{size}");
            if (offset > 0) {
                cls.Should().Contain($"l{offset}-offset");
            }
            else {
                cls.Should().NotContain("-offset");
            }
        }
        else {
            cls.Should().NotContain($"l{size}");
        }
    }

    [Fact]
    public void Small_Offset_Adds_Correct_Class() {
        // Arrange & Act
        var cut = Render<TnTColumn>(p => p
            .Add(c => c.S, 6)
            .Add(c => c.SOffset, 3));
        var cls = cut.Find("div").GetAttribute("class")!;

        // Assert
        cls.Should().Contain("s6");
        cls.Should().Contain("s3-offset");
    }

    [Fact]
    public void Small_Size_Adds_Correct_Class() {
        // Arrange & Act
        var cut = Render<TnTColumn>(p => p.Add(c => c.S, 6));
        var cls = cut.Find("div").GetAttribute("class")!;

        // Assert
        cls.Should().Contain("s6");
        cls.Should().NotContain("s12");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(6)]
    [InlineData(12)]
    public void Valid_Size_Values_Work_Correctly(int validSize) {
        // Arrange & Act
        var cut = Render<TnTColumn>(p => p.Add(c => c.S, validSize));
        var cls = cut.Find("div").GetAttribute("class")!;

        // Assert
        cls.Should().Contain($"s{validSize}");
    }

    [Fact]
    public void XLarge_All_Properties_Adds_Correct_Classes() {
        // Arrange & Act
        var cut = Render<TnTColumn>(p => p
            .Add(c => c.XL, 6)
            .Add(c => c.XLOffset, 1)
            .Add(c => c.XLPush, 2)
            .Add(c => c.XLPull, 1));
        var cls = cut.Find("div").GetAttribute("class")!;

        // Assert
        cls.Should().Contain("xl6");
        cls.Should().Contain("xl1-offset");
        cls.Should().Contain("xl2-push");
        cls.Should().Contain("xl1-pull");
    }

    [Fact]
    public void XLarge_Size_Adds_Correct_Class() {
        // Arrange & Act
        var cut = Render<TnTColumn>(p => p.Add(c => c.XL, 3));
        var cls = cut.Find("div").GetAttribute("class")!;

        // Assert
        cls.Should().Contain("xl3");
    }

    [Fact]
    public void Zero_Size_Not_Added() {
        // Arrange & Act
        var cut = Render<TnTColumn>(p => p.Add(c => c.S, 0));
        var cls = cut.Find("div").GetAttribute("class")!;

        // Assert
        cls.Should().Contain("s12"); // Default when no sizes specified
        cls.Should().NotContain("s0");
    }
}