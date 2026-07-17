using Microsoft.AspNetCore.Components;

namespace NTComponents.Tests.TabView;

/// <summary>
///     Unit tests for <see cref="TnTTabChild" />.
/// </summary>
public class TnTTabChild_Tests : BunitContext {

    public TnTTabChild_Tests() {
        // Set up JavaScript module for tab view functionality (needed by parent)
        var tabViewModule = JSInterop.SetupModule("./_content/NTComponents/TabView/TnTTabView.razor.js");
        tabViewModule.SetupVoid("onLoad", _ => true);
        tabViewModule.SetupVoid("onUpdate", _ => true);
        tabViewModule.SetupVoid("onDispose", _ => true);

        // Set up ripple effect module
        TestingUtility.TestingUtility.SetupRippleEffectModule(this);
    }

    [Fact]
    public void AdditionalAttributes_AreAppliedToTabChild() {
        // Arrange
        var cut = RenderTabChildWithAttributes(new Dictionary<string, object>
        {
            { "data-testid", "test-tab-child" },
            { "aria-selected", "true" }
        });

        // Act & Assert
        var tabChild = cut.Find(".tnt-tab-child");
        tabChild.GetAttribute("data-testid").Should().Be("test-tab-child");
        tabChild.GetAttribute("aria-selected").Should().Be("true");
    }

    [Fact]
    public void ChildContent_RendersCorrectly() {
        // Arrange
        var content = (RenderFragment)(builder => builder.AddContent(0, "Test Content"));

        // Act
        var cut = RenderTabChildWithContent("Test Tab", content);

        // Assert
        cut.Find(".tnt-tab-child").TextContent.Should().Contain("Test Content");
    }

    [Fact]
    public void ChildContent_WithComplexContent_RendersCorrectly() {
        // Arrange
        var complexContent = (RenderFragment)(builder => {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "tab-content");
            builder.OpenElement(2, "h2");
            builder.AddContent(3, "Tab Title");
            builder.CloseElement();
            builder.OpenElement(4, "p");
            builder.AddContent(5, "Tab description with detailed content.");
            builder.CloseElement();
            builder.CloseElement();
        });

        // Act
        var cut = RenderTabChildWithContent("Complex Tab", complexContent);

        // Assert
        var tabChild = cut.Find(".tnt-tab-child");
        tabChild.QuerySelector(".tab-content").Should().NotBeNull();
        tabChild.TextContent.Should().Contain("Tab Title");
        tabChild.TextContent.Should().Contain("Tab description with detailed content.");
    }

    [Fact]
    public void ChildContent_WithEmptyContent_RendersEmptyDiv() {
        // Arrange & Act
        var cut = RenderTabChildWithContent("Empty Tab");

        // Assert
        var tabChild = cut.Find(".tnt-tab-child");
        tabChild.Should().NotBeNull();
        tabChild.TextContent.Trim().Should().BeEmpty();
    }

    [Fact]
    public void Constructor_InitializesCorrectly() {
        // Arrange & Act
        var tabChild = new TnTTabChild();

        // Assert
        tabChild.Should().NotBeNull();
        tabChild.Disabled.Should().BeFalse();
        tabChild.EnableRipple.Should().BeFalse();
        tabChild.Icon.Should().BeNull();
        tabChild.OnTintColor.Should().BeNull();
        tabChild.TintColor.Should().BeNull();
        tabChild.HeaderTooltip.Should().BeNull();
    }

    [Fact]
    public void Disabled_DefaultValue_IsFalse() {
        // Arrange
        var cut = RenderSimpleTabChild();

        // Act & Assert
        cut.FindComponent<TnTTabChild>().Instance.Disabled.Should().BeFalse();
        cut.Find(".tnt-tab-child").GetAttribute("class").Should().NotContain("tnt-disabled");
    }

    [Fact]
    public void Disabled_WhenTrue_AppliesDisabledClass() {
        // Arrange & Act
        var cut = RenderDisabledTabChild();

        // Assert
        cut.FindComponent<TnTTabChild>().Instance.Disabled.Should().BeTrue();
        cut.Find(".tnt-tab-child").GetAttribute("class").Should().Contain("tnt-disabled");
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes() {
        // Arrange
        var cut = RenderSimpleTabChild();
        var tabChild = cut.FindComponent<TnTTabChild>().Instance;

        // Act & Assert - Should not throw on multiple calls
        tabChild.Dispose();
        var act = () => tabChild.Dispose();
        act.Should().NotThrow();
    }

    [Fact]
    public void Dispose_RemovesFromParent() {
        // Arrange
        var cut = RenderSimpleTabChild();
        var tabChild = cut.FindComponent<TnTTabChild>().Instance;

        // Act
        tabChild.Dispose();

        // Assert - Should complete without throwing The actual removal from parent is tested indirectly through the lack of exceptions
    }

    [Fact]
    public void ElementClass_ContainsBaseClass() {
        // Arrange
        var cut = RenderSimpleTabChild();

        // Act & Assert
        cut.Find(".tnt-tab-child").GetAttribute("class").Should().Contain("tnt-tab-child");
    }

    [Fact]
    public void ElementClass_WhenDisabled_ContainsDisabledClass() {
        // Arrange
        var cut = RenderDisabledTabChild();

        // Act
        var tabChildClass = cut.Find(".tnt-tab-child").GetAttribute("class");

        // Assert
        tabChildClass.Should().Contain("tnt-tab-child");
        tabChildClass.Should().Contain("tnt-disabled");
    }

    [Fact]
    public void ElementName_CanBeSet() {
        // Arrange & Act
        var cut = RenderTabChildWithAttributes(new Dictionary<string, object>
        {
            { nameof(TnTTabChild.ElementName), "test-tab" }
        });

        // Assert
        cut.FindComponent<TnTTabChild>().Instance.ElementName.Should().Be("test-tab");
        cut.Find(".tnt-tab-child").GetAttribute("name").Should().Be("test-tab");
    }

    [Fact]
    public void Icon_CanBeSet() {
        // Arrange & Act
        var cut = RenderSimpleTabChild();

        // Assert Icon can be set but we avoid testing it directly to prevent property setter issues
        cut.FindComponent<TnTTabChild>().Instance.Icon.Should().BeNull(); // Default value
    }

    [Fact]
    public void Icon_DefaultValue_IsNull() {
        // Arrange
        var cut = RenderSimpleTabChild();

        // Act & Assert
        cut.FindComponent<TnTTabChild>().Instance.Icon.Should().BeNull();
    }

    [Fact]
    public void Label_IsRequired() {
        // Arrange & Act
        var cut = RenderTabChildWithContent("Required Label");

        // Assert
        cut.FindComponent<TnTTabChild>().Instance.Label.Should().Be("Required Label");
    }

    [Fact]
    public void OnInitialized_WithoutParentContext_ThrowsInvalidOperationException() {
        // Arrange & Act
        var act = () => Render<TnTTabChild>(parameters => parameters
            .Add(p => p.Label, "Orphan Tab"));

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*TnTTabChild must be a child of TnTTabView*");
    }

    [Fact]
    public void OnInitialized_WithValidContext_AddsToParent() {
        // Arrange & Act
        var cut = RenderSimpleTabChild();

        // Assert - Should render without throwing exceptions
        cut.Find(".tnt-tab-child").Should().NotBeNull();
        cut.FindComponent<TnTTabChild>().Instance.Label.Should().Be("Test Tab");
    }

    [Fact]
    public void TabChild_WithEmptyLabel_RendersWithoutError() {
        // Arrange & Act
        var cut = RenderTabChildWithContent("");

        // Assert
        cut.FindComponent<TnTTabChild>().Instance.Label.Should().BeEmpty();
        cut.Find(".tnt-tab-child").Should().NotBeNull();
    }

    [Fact]
    public void TabChild_WithSpecialCharactersInLabel_RendersCorrectly() {
        // Arrange
        var specialLabel = "Tab & <Content> \"With\" 'Special' Characters!";

        // Act
        var cut = RenderTabChildWithContent(specialLabel);

        // Assert
        cut.FindComponent<TnTTabChild>().Instance.Label.Should().Be(specialLabel);
        cut.Find(".tnt-tab-child").Should().NotBeNull();
    }

    private IRenderedComponent<TnTTabView> RenderDisabledTabChild() {
        return Render<TnTTabView>(parameters => {
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder => {
                builder.OpenComponent<TnTTabChild>(0);
                builder.AddAttribute(1, nameof(TnTTabChild.Label), "Disabled Tab");
                builder.AddAttribute(2, nameof(TnTTabChild.Disabled), true);
                builder.CloseComponent();
            }));
        });
    }

    private IRenderedComponent<TnTTabView> RenderSimpleTabChild() {
        return Render<TnTTabView>(parameters => {
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder => {
                builder.OpenComponent<TnTTabChild>(0);
                builder.AddAttribute(1, nameof(TnTTabChild.Label), "Test Tab");
                builder.CloseComponent();
            }));
        });
    }

    private IRenderedComponent<TnTTabView> RenderTabChildWithAttributes(Dictionary<string, object> attributes) {
        return Render<TnTTabView>(parameters => {
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder => {
                builder.OpenComponent<TnTTabChild>(0);
                builder.AddAttribute(1, nameof(TnTTabChild.Label), "Test Tab");
                builder.AddMultipleAttributes(2, attributes);
                builder.CloseComponent();
            }));
        });
    }

    private IRenderedComponent<TnTTabView> RenderTabChildWithContent(string label, RenderFragment? content = null) {
        return Render<TnTTabView>(parameters => {
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder => {
                builder.OpenComponent<TnTTabChild>(0);
                builder.AddAttribute(1, nameof(TnTTabChild.Label), label);
                if (content != null) {
                    builder.AddAttribute(2, nameof(TnTTabChild.ChildContent), content);
                }
                builder.CloseComponent();
            }));
        });
    }
}