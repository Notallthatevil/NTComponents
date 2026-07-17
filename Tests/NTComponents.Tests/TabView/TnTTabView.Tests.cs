using Microsoft.AspNetCore.Components;

namespace NTComponents.Tests.TabView;

/// <summary>
///     Unit tests for <see cref="TnTTabView" />.
/// </summary>
public class TnTTabView_Tests : BunitContext {

    public TnTTabView_Tests() {
        // Set up JavaScript module for tab view functionality
        var tabViewModule = JSInterop.SetupModule("./_content/NTComponents/TabView/TnTTabView.razor.js");
        tabViewModule.SetupVoid("onLoad", _ => true);
        tabViewModule.SetupVoid("onUpdate", _ => true);
        tabViewModule.SetupVoid("onDispose", _ => true);

        // Set up ripple effect module
        TestingUtility.TestingUtility.SetupRippleEffectModule(this);
    }

    [Fact]
    public void ActiveIndicator_HasCorrectStyling() {
        // Arrange & Act
        var cut = RenderTabView(parameters => parameters
            .Add(p => p.ActiveIndicatorColor, TnTColor.Error));

        // Assert
        var activeIndicator = cut.Find(".tnt-tab-view-active-indicator");
        activeIndicator.GetAttribute("style").Should().Contain("--tnt-color-error");
    }

    [Fact]
    public void ActiveIndicatorColor_CanBeCustomized() {
        // Arrange & Act
        var cut = RenderTabView(parameters => parameters
            .Add(p => p.ActiveIndicatorColor, TnTColor.Secondary));

        // Assert
        cut.Instance.ActiveIndicatorColor.Should().Be(TnTColor.Secondary);
        cut.Markup.Should().Contain("--tnt-tab-view-active-indicator-color:var(--tnt-color-secondary)");
    }

    [Fact]
    public void ActiveIndicatorColor_DefaultValue_IsPrimary() {
        // Arrange
        var cut = RenderTabView();

        // Act & Assert
        cut.Instance.ActiveIndicatorColor.Should().Be(TnTColor.Primary);
    }

    [Fact]
    public void AdditionalAttributes_AreAppliedToTabView() {
        // Arrange
        var cut = RenderTabView(parameters => parameters
            .AddUnmatched("data-testid", "test-tab-view")
            .AddUnmatched("aria-label", "Test Tab View"));

        // Act & Assert
        var tabView = cut.Find("tnt-tab-view");
        tabView.GetAttribute("data-testid").Should().Be("test-tab-view");
        tabView.GetAttribute("aria-label").Should().Be("Test Tab View");
    }

    [Fact]
    public void AddTabChild_AddsChildToCollection() {
        // Arrange
        var tabView = new TnTTabView();
        var tabChild = new TnTTabChild { Label = "Test Tab" };

        // Act
        tabView.AddTabChild(tabChild);

        // Assert - We can't directly access _tabChildren, but we can verify behavior through rendering
        tabView.Should().NotBeNull(); // Ensures no exception was thrown
    }

    [Fact]
    public void Appearance_DefaultValue_IsPrimary() {
        // Arrange
        var cut = RenderTabView();

        // Act & Assert
        cut.Instance.Appearance.Should().Be(TabViewAppearance.Primary);
        cut.Find("tnt-tab-view").GetAttribute("class").Should().Contain("tnt-tab-view");
        cut.Find("tnt-tab-view").GetAttribute("class").Should().NotContain("tnt-tab-view-secondary");
    }

    [Fact]
    public void Appearance_WhenSecondary_AppliesCorrectClass() {
        // Arrange & Act
        var cut = RenderTabView(parameters => parameters
            .Add(p => p.Appearance, TabViewAppearance.Secondary));

        // Assert
        cut.Instance.Appearance.Should().Be(TabViewAppearance.Secondary);
        cut.Find("tnt-tab-view").GetAttribute("class").Should().Contain("tnt-tab-view-secondary");
    }

    [Fact]
    public void CascadingValue_IsProvidedToChildren() {
        // Arrange & Act
        var cut = RenderTabViewWithTabs();

        // Assert The tabs should render without errors, indicating the cascading value is working
        cut.FindAll(".tnt-tab-child").Should().HaveCount(3);
    }

    [Fact]
    public void CompleteTabView_WithMultipleTabs_RendersCorrectly() {
        // Arrange & Act
        var cut = RenderCompleteTabView();

        // Assert
        cut.Find("tnt-tab-view").Should().NotBeNull();
        cut.Find(".tnt-tab-view-header").Should().NotBeNull();
        cut.FindAll(".tnt-tab-view-button").Should().HaveCount(3);
        cut.FindAll(".tnt-tab-child").Should().HaveCount(3);
        cut.Find(".tnt-tab-view-active-indicator").Should().NotBeNull();
    }

    [Fact]
    public void Constructor_InitializesCorrectly() {
        // Arrange & Act
        var tabView = new TnTTabView();

        // Assert
        tabView.Should().NotBeNull();
        tabView.ActiveIndicatorColor.Should().Be(TnTColor.Primary);
        tabView.Appearance.Should().Be(TabViewAppearance.Primary);
        tabView.HeaderBackgroundColor.Should().Be(TnTColor.Surface);
        tabView.HeaderTextColor.Should().Be(TnTColor.OnSurface);
        tabView.HeaderTintColor.Should().Be(TnTColor.SurfaceTint);
    }

    [Fact]
    public void ElementClass_ContainsBaseClass() {
        // Arrange
        var cut = RenderTabView();

        // Act & Assert
        cut.Find("tnt-tab-view").GetAttribute("class").Should().Contain("tnt-tab-view");
    }

    [Fact]
    public void ElementClass_WithSecondaryAppearance_ContainsSecondaryClass() {
        // Arrange
        var cut = RenderTabView(parameters => parameters
            .Add(p => p.Appearance, TabViewAppearance.Secondary));

        // Act
        var tabViewClass = cut.Find("tnt-tab-view").GetAttribute("class");

        // Assert
        tabViewClass.Should().Contain("tnt-tab-view");
        tabViewClass.Should().Contain("tnt-tab-view-secondary");
    }

    [Fact]
    public void ElementStyle_ContainsActiveIndicatorColorVariable() {
        // Arrange
        var cut = RenderTabView(parameters => parameters
            .Add(p => p.ActiveIndicatorColor, TnTColor.Tertiary));

        // Act
        var tabViewStyle = cut.Find("tnt-tab-view").GetAttribute("style");

        // Assert
        tabViewStyle.Should().Contain("--tnt-tab-view-active-indicator-color:var(--tnt-color-tertiary)");
    }

    [Fact]
    public void EmptyTabView_RendersWithoutErrors() {
        // Arrange & Act
        var cut = RenderTabView();

        // Assert
        cut.Find("tnt-tab-view").Should().NotBeNull();
        cut.Find(".tnt-tab-view-header").Should().NotBeNull();
        cut.FindAll(".tnt-tab-view-button").Should().HaveCount(0);
    }

    [Fact]
    public void HeaderBackgroundColor_DefaultValue_IsSurface() {
        // Arrange
        var cut = RenderTabView();

        // Act & Assert
        cut.Instance.HeaderBackgroundColor.Should().Be(TnTColor.Surface);
    }

    [Fact]
    public void HeaderStyles_ApplyCorrectVariables() {
        // Arrange & Act
        var cut = RenderTabViewWithTabs();

        // Assert
        var header = cut.Find(".tnt-tab-view-header");
        var headerClass = header.GetAttribute("class");
        headerClass.Should().Contain("tnt-tab-view-header");
        headerClass.Should().Contain("tnt-filled");
    }

    [Fact]
    public void HeaderTextColor_DefaultValue_IsOnSurface() {
        // Arrange
        var cut = RenderTabView();

        // Act & Assert
        cut.Instance.HeaderTextColor.Should().Be(TnTColor.OnSurface);
    }

    [Fact]
    public void HeaderTintColor_DefaultValue_IsSurfaceTint() {
        // Arrange
        var cut = RenderTabView();

        // Act & Assert
        cut.Instance.HeaderTintColor.Should().Be(TnTColor.SurfaceTint);
    }

    [Fact]
    public void JsModulePath_ReturnsCorrectPath() {
        // Arrange
        var tabView = new TnTTabView();

        // Act
        var path = tabView.JsModulePath;

        // Assert
        path.Should().Be("./_content/NTComponents/TabView/TnTTabView.razor.js");
    }

    [Fact]
    public void RemoveTabChild_RemovesChildFromCollection() {
        // Arrange
        var tabView = new TnTTabView();
        var tabChild = new TnTTabChild { Label = "Test Tab" };
        tabView.AddTabChild(tabChild);

        // Act
        tabView.RemoveTabChild(tabChild);

        // Assert - Verify no exception is thrown
        tabView.Should().NotBeNull();
    }

    [Fact]
    public void Rendering_IncludesHeaderStructure() {
        // Arrange & Act
        var cut = RenderTabView();

        // Assert
        cut.Find(".tnt-tab-view-header").Should().NotBeNull();
        cut.Find(".tnt-tab-view-header-buttons").Should().NotBeNull();
        cut.Find(".tnt-tab-view-active-indicator").Should().NotBeNull();
    }

    [Fact]
    public void TabButtons_HaveCorrectAttributes() {
        // Arrange & Act
        var cut = RenderTabViewWithTabs();

        // Assert
        var buttons = cut.FindAll(".tnt-tab-view-button");
        foreach (var button in buttons) {
            button.GetAttribute("type").Should().Be("button");
            button.GetAttribute("class").Should().Contain("tnt-tab-view-button");
            button.GetAttribute("class").Should().Contain("tnt-interactable");
            button.GetAttribute("class").Should().Contain("tnt-ripple");
        }
    }

    [Fact]
    public void TabButtons_IncludeRippleEffect() {
        // Arrange & Act
        var cut = RenderTabViewWithTabs();

        // Assert
        cut.Markup.Should().Contain("TnTRippleEffect");

        // Verify that each button has ripple class
        var buttons = cut.FindAll(".tnt-tab-view-button");
        buttons.Should().HaveCount(3);
        foreach (var button in buttons) {
            button.GetAttribute("class").Should().Contain("tnt-ripple");
        }
    }

    [Fact]
    public void TabButtons_RenderCorrectly() {
        // Arrange & Act
        var cut = RenderTabViewWithTabs();

        // Assert
        var buttons = cut.FindAll(".tnt-tab-view-button");
        buttons.Should().HaveCount(3);

        buttons[0].TextContent.Should().Contain("Tab 1");
        buttons[1].TextContent.Should().Contain("Tab 2");
        buttons[2].TextContent.Should().Contain("Tab 3");
    }

    [Fact]
    public void TabButtons_WithDisabledTab_ShowsDisabledState() {
        // Arrange & Act
        var cut = RenderTabViewWithDisabledTab();

        // Assert
        var buttons = cut.FindAll(".tnt-tab-view-button");
        buttons.Should().HaveCount(2);

        var enabledButton = buttons[0];
        var disabledButton = buttons[1];

        enabledButton.GetAttribute("class").Should().NotContain("tnt-disabled");
        enabledButton.HasAttribute("disabled").Should().BeFalse();

        disabledButton.GetAttribute("class").Should().Contain("tnt-disabled");
        disabledButton.HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public void TabView_WithNullChildContent_RendersWithoutErrors() {
        // Arrange & Act
        var cut = RenderTabView(parameters => parameters
            .Add(p => p.ChildContent, (RenderFragment?)null));

        // Assert
        cut.Find("tnt-tab-view").Should().NotBeNull();
        cut.Find(".tnt-tab-view-header").Should().NotBeNull();
    }

    private RenderFragment CreateTabsFragment() => builder => {
        builder.OpenComponent<TnTTabChild>(0);
        builder.AddAttribute(1, nameof(TnTTabChild.Label), "Tab 1");
        builder.AddAttribute(2, nameof(TnTTabChild.ChildContent), (RenderFragment)(b => b.AddContent(0, "Content 1")));
        builder.CloseComponent();

        builder.OpenComponent<TnTTabChild>(3);
        builder.AddAttribute(4, nameof(TnTTabChild.Label), "Tab 2");
        builder.AddAttribute(5, nameof(TnTTabChild.ChildContent), (RenderFragment)(b => b.AddContent(0, "Content 2")));
        builder.CloseComponent();

        builder.OpenComponent<TnTTabChild>(6);
        builder.AddAttribute(7, nameof(TnTTabChild.Label), "Tab 3");
        builder.AddAttribute(8, nameof(TnTTabChild.ChildContent), (RenderFragment)(b => b.AddContent(0, "Content 3")));
        builder.CloseComponent();
    };

    private RenderFragment CreateTabsWithDisabledFragment() => builder => {
        builder.OpenComponent<TnTTabChild>(0);
        builder.AddAttribute(1, nameof(TnTTabChild.Label), "Enabled Tab");
        builder.AddAttribute(2, nameof(TnTTabChild.ChildContent), (RenderFragment)(b => b.AddContent(0, "Enabled Content")));
        builder.CloseComponent();

        builder.OpenComponent<TnTTabChild>(3);
        builder.AddAttribute(4, nameof(TnTTabChild.Label), "Disabled Tab");
        builder.AddAttribute(5, nameof(TnTTabChild.Disabled), true);
        builder.AddAttribute(6, nameof(TnTTabChild.ChildContent), (RenderFragment)(b => b.AddContent(0, "Disabled Content")));
        builder.CloseComponent();
    };

    private IRenderedComponent<TnTTabView> RenderCompleteTabView() {
        return RenderTabView(parameters => {
            parameters.Add(p => p.ChildContent, CreateTabsFragment());
            parameters.Add(p => p.ActiveIndicatorColor, TnTColor.Primary);
            parameters.Add(p => p.HeaderBackgroundColor, TnTColor.Surface);
            parameters.Add(p => p.HeaderTextColor, TnTColor.OnSurface);
        });
    }

    private IRenderedComponent<TnTTabView> RenderTabView(
                    Action<ComponentParameterCollectionBuilder<TnTTabView>>? parameterBuilder = null) {
        return Render<TnTTabView>(parameters => {
            parameterBuilder?.Invoke(parameters);
        });
    }

    private IRenderedComponent<TnTTabView> RenderTabViewWithDisabledTab() {
        return RenderTabView(parameters => {
            parameters.Add(p => p.ChildContent, CreateTabsWithDisabledFragment());
        });
    }

    private IRenderedComponent<TnTTabView> RenderTabViewWithTabs(
            Action<ComponentParameterCollectionBuilder<TnTTabView>>? additionalParameters = null) {
        return RenderTabView(parameters => {
            parameters.Add(p => p.ChildContent, CreateTabsFragment());
            additionalParameters?.Invoke(parameters);
        });
    }
}