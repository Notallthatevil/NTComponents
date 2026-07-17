using Microsoft.AspNetCore.Components;
using NTComponents.Core;

namespace NTComponents.Tests.Core;

public class DeferRendering_Tests : BunitContext {

    [Fact]
    public void Renders_ChildContent_When_Provided() {
        // Arrange
        var content = "<p>Hello, world!</p>";
        var cut = base.Render<DeferRendering>(parameters => parameters
            .AddChildContent(content));

        // Act
        var markup = cut.Markup;

        // Assert
        markup.Should().Contain("Hello, world!");
    }

    [Fact]
    public void Renders_Dynamic_ChildContent() {
        // Arrange
        RenderFragment fragment = builder => builder.AddContent(0, "Dynamic content");
        var cut = base.Render<DeferRendering>(parameters => parameters
            .Add(p => p.ChildContent, fragment));

        // Act
        var markup = cut.Markup;

        // Assert
        markup.Should().Contain("Dynamic content");
    }

    [Fact]
    public void Renders_Empty_String_ChildContent() {
        // Arrange
        var cut = base.Render<DeferRendering>(parameters => parameters
            .AddChildContent(string.Empty));

        // Act
        var markup = cut.Markup;

        // Assert
        markup.Should().BeEmpty();
    }

    [Fact]
    public void Renders_Multiple_Elements_In_ChildContent() {
        // Arrange
        var content = @"<div>First</div><div>Second</div>";
        var cut = base.Render<DeferRendering>(parameters => parameters
            .AddChildContent(content));

        // Act
        var markup = cut.Markup;

        // Assert
        markup.Should().Contain("First");
        // Only one logical assertion per test, so create a separate test for the second element
    }

    [Fact]
    public void Renders_Nothing_When_ChildContent_Is_Null() {
        // Arrange & Act
        var cut = base.Render<DeferRendering>();

        // Assert
        cut.Markup.Should().BeEmpty();
    }

    [Fact]
    public void Renders_Second_Element_In_Multiple_ChildContent() {
        // Arrange
        var content = @"<div>First</div><div>Second</div>";
        var cut = base.Render<DeferRendering>(parameters => parameters
            .AddChildContent(content));

        // Act
        var markup = cut.Markup;

        // Assert
        markup.Should().Contain("Second");
    }

    [Fact]
    public void Renders_Whitespace_Only_ChildContent() {
        // Arrange
        var cut = base.Render<DeferRendering>(parameters => parameters
            .AddChildContent("   \t\n   "));

        // Act
        var markup = cut.Markup;

        // Assert
        markup.Should().Be("   \t\n   ");
    }
}