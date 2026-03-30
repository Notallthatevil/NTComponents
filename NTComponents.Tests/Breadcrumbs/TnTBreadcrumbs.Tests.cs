using Bunit;

namespace NTComponents.Tests.Breadcrumbs;

/// <summary>
///     Unit tests for <see cref="TnTBreadcrumbs" />.
/// </summary>
public class TnTBreadcrumbs_Tests : BunitContext {
    [Fact]
    public void AdditionalAttributes_AreAppliedToRootNav() {
        // Arrange
        var items = CreateStandardItems();

        // Act
        var cut = Render<TnTBreadcrumbs>(parameters => parameters
            .Add(component => component.Items, items)
            .AddUnmatched("data-testid", "breadcrumbs")
            .AddUnmatched("class", "custom-class"));

        // Assert
        var nav = cut.Find("nav");
        nav.GetAttribute("data-testid").Should().Be("breadcrumbs");
        nav.GetAttribute("class").Should().Contain("custom-class");
        nav.GetAttribute("class").Should().Contain("tnt-breadcrumbs");
    }

    [Fact]
    public void CustomAriaLabel_IsAppliedToNavigationLandmark() {
        // Arrange
        var items = CreateStandardItems();

        // Act
        var cut = Render<TnTBreadcrumbs>(parameters => parameters
            .Add(component => component.Items, items)
            .Add(component => component.AriaLabel, "Page path"));

        // Assert
        cut.Find("nav").GetAttribute("aria-label").Should().Be("Page path");
    }

    [Fact]
    public void EmptyItems_ThrowsInvalidOperationException() {
        // Arrange
        var items = Array.Empty<TnTBreadcrumbItem>();

        // Act
        var action = () => Render<TnTBreadcrumbs>(parameters => parameters
            .Add(component => component.Items, items));

        // Assert
        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ExplicitCurrentItem_RendersOnlyThatItemAsCurrentPage() {
        // Arrange
        var items = new[] {
            new TnTBreadcrumbItem { Text = "Home", Href = "/" },
            new TnTBreadcrumbItem { Text = "Reports", IsCurrent = true },
            new TnTBreadcrumbItem { Text = "Quarterly", Href = "/reports/quarterly" }
        };

        // Act
        var cut = Render<TnTBreadcrumbs>(parameters => parameters
            .Add(component => component.Items, items));

        // Assert
        var currentItems = cut.FindAll("[aria-current='page']");
        currentItems.Should().HaveCount(1);
        currentItems[0].TextContent.Trim().Should().Be("Reports");
        cut.FindAll("a").Should().HaveCount(2);
    }

    [Fact]
    public void IconOnlyItem_WithAriaLabel_RendersAccessibleLink() {
        // Arrange
        var items = new[] {
            new TnTBreadcrumbItem { Icon = MaterialIcon.Home, Href = "/", AriaLabel = "Home" },
            new TnTBreadcrumbItem { Text = "Settings" }
        };

        // Act
        var cut = Render<TnTBreadcrumbs>(parameters => parameters
            .Add(component => component.Items, items));

        // Assert
        var homeLink = cut.Find("a");
        homeLink.GetAttribute("aria-label").Should().Be("Home");
        homeLink.ClassList.Should().Contain("tnt-breadcrumbs__crumb--icon-only");
    }

    [Fact]
    public void IconOnlyItem_WithoutAriaLabel_ThrowsInvalidOperationException() {
        // Arrange
        var items = new[] {
            new TnTBreadcrumbItem { Icon = MaterialIcon.Home, Href = "/" },
            new TnTBreadcrumbItem { Text = "Settings" }
        };

        // Act
        var action = () => Render<TnTBreadcrumbs>(parameters => parameters
            .Add(component => component.Items, items));

        // Assert
        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void MultipleCurrentItems_ThrowsInvalidOperationException() {
        // Arrange
        var items = new[] {
            new TnTBreadcrumbItem { Text = "Home", IsCurrent = true },
            new TnTBreadcrumbItem { Text = "Settings", IsCurrent = true }
        };

        // Act
        var action = () => Render<TnTBreadcrumbs>(parameters => parameters
            .Add(component => component.Items, items));

        // Assert
        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void NullItems_ThrowsArgumentNullException() {
        // Arrange & Act
        var action = () => Render<TnTBreadcrumbs>(parameters => parameters
            .Add(component => component.Items, (IReadOnlyList<TnTBreadcrumbItem>)null!));

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void StandardItems_RenderSemanticBreadcrumbMarkup() {
        // Arrange
        var items = CreateStandardItems();

        // Act
        var cut = Render<TnTBreadcrumbs>(parameters => parameters
            .Add(component => component.Items, items));

        // Assert
        cut.Find("nav[aria-label='Breadcrumb']").Should().NotBeNull();
        cut.Find("ol.tnt-breadcrumbs__list").Should().NotBeNull();
        cut.FindAll("li.tnt-breadcrumbs__item").Should().HaveCount(3);
        cut.FindAll(".tnt-breadcrumbs__separator").Should().HaveCount(2);
    }

    [Fact]
    public void DisabledIntermediateItem_RendersAsDisabledText() {
        // Arrange
        var items = new[] {
            new TnTBreadcrumbItem { Text = "Home", Href = "/" },
            new TnTBreadcrumbItem { Text = "Archived", Href = "/archived", Disabled = true },
            new TnTBreadcrumbItem { Text = "2026" }
        };

        // Act
        var cut = Render<TnTBreadcrumbs>(parameters => parameters
            .Add(component => component.Items, items));

        // Assert
        cut.FindAll("a").Should().HaveCount(1);
        var disabledItem = cut.FindAll(".tnt-breadcrumbs__crumb--disabled").Single();
        disabledItem.GetAttribute("aria-disabled").Should().Be("true");
        disabledItem.TextContent.Trim().Should().Be("Archived");
    }

    [Fact]
    public void WhenNoCurrentItemIsProvided_LastItemBecomesCurrentPage() {
        // Arrange
        var items = CreateStandardItems();

        // Act
        var cut = Render<TnTBreadcrumbs>(parameters => parameters
            .Add(component => component.Items, items));

        // Assert
        var currentItem = cut.Find("[aria-current='page']");
        currentItem.TextContent.Trim().Should().Be("Quarterly");
        currentItem.TagName.Should().Be("SPAN");
        cut.FindAll("a").Should().HaveCount(2);
    }

    private static IReadOnlyList<TnTBreadcrumbItem> CreateStandardItems() => new[] {
        new TnTBreadcrumbItem { Text = "Home", Href = "/" },
        new TnTBreadcrumbItem { Text = "Reports", Href = "/reports" },
        new TnTBreadcrumbItem { Text = "Quarterly" }
    };
}
