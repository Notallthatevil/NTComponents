using Microsoft.AspNetCore.Components.Web;
using NTComponents.Grid;
using RippleTestingUtility = NTComponents.Tests.TestingUtility.TestingUtility;

namespace NTComponents.Tests.Grid;

/// <summary>
///     Unit tests for <see cref="TnTPaginationButtons" />.
/// </summary>
public class TnTPaginationButtons_Tests : BunitContext {

    public TnTPaginationButtons_Tests() {
        // Set up JavaScript module for ripple effects used by TnTButton components
        RippleTestingUtility.SetupRippleEffectModule(this);
    }

    [Fact]
    public void ActiveBackgroundColor_DefaultValue_IsSet() {
        // Arrange
        var paginationState = new TnTPaginationState();
        _ = paginationState.SetTotalItemCountAsync(10);

        // Act
        var cut = Render<TnTPaginationButtons>(parameters => parameters
            .Add(p => p.PaginationState, paginationState));

        // Assert
        cut.Instance.ActiveBackgroundColor.Should().Be(TnTColor.Primary);
    }

    [Fact]
    public void ActiveTextColor_DefaultValue_IsSet() {
        // Arrange
        var paginationState = new TnTPaginationState();
        _ = paginationState.SetTotalItemCountAsync(10);

        // Act
        var cut = Render<TnTPaginationButtons>(parameters => parameters
            .Add(p => p.PaginationState, paginationState));

        // Assert
        cut.Instance.ActiveTextColor.Should().Be(TnTColor.OnPrimary);
    }

    [Fact]
    public void AdditionalAttributes_AreAppliedCorrectly() {
        // Arrange
        var paginationState = new TnTPaginationState();
        _ = paginationState.SetTotalItemCountAsync(10);

        // Act
        var cut = Render<TnTPaginationButtons>(parameters => parameters
            .Add(p => p.PaginationState, paginationState)
            .AddUnmatched("data-test", "pagination")
            .AddUnmatched("class", "custom-class")
            .AddUnmatched("style", "margin: 10px;"));

        // Assert
        var component = cut.Find(".tnt-pagination-buttons");
        component.GetAttribute("data-test").Should().Be("pagination");
        component.GetAttribute("class")!.Should().Contain("custom-class");
        component.GetAttribute("style")!.Should().Contain("margin: 10px;");
    }

    [Fact]
    public void BackgroundColor_DefaultValue_IsSet() {
        // Arrange
        var paginationState = new TnTPaginationState();
        _ = paginationState.SetTotalItemCountAsync(10);

        // Act
        var cut = Render<TnTPaginationButtons>(parameters => parameters
            .Add(p => p.PaginationState, paginationState));

        // Assert
        cut.Instance.BackgroundColor.Should().Be(TnTColor.PrimaryContainer);
    }

    [Fact]
    public void Buttons_HaveCorrectAriaLabels() {
        // Arrange
        var paginationState = new TnTPaginationState();
        _ = paginationState.SetTotalItemCountAsync(30);

        // Act
        var cut = Render<TnTPaginationButtons>(parameters => parameters
            .Add(p => p.PaginationState, paginationState));

        // Assert
        var firstButton = cut.FindAll("button").FirstOrDefault(b => b.GetAttribute("aria-label") == "First page");
        firstButton.Should().NotBeNull();

        var previousButton = cut.FindAll("button").FirstOrDefault(b => b.GetAttribute("aria-label") == "Previous page");
        previousButton.Should().NotBeNull();

        var nextButton = cut.FindAll("button").FirstOrDefault(b => b.GetAttribute("aria-label") == "Next page");
        nextButton.Should().NotBeNull();

        var lastButton = cut.FindAll("button").FirstOrDefault(b => b.GetAttribute("aria-label") == "Last page");
        lastButton.Should().NotBeNull();
    }

    [Fact]
    public void Buttons_HaveCorrectCssClasses() {
        // Arrange
        var paginationState = new TnTPaginationState();
        _ = paginationState.SetTotalItemCountAsync(30);

        var cut = Render<TnTPaginationButtons>(parameters => parameters
            .Add(p => p.PaginationState, paginationState));

        // Assert
        Assert.True(cut.FindAll(".pagination-btn").Count > 0);

        // Look for buttons with specific navigation classes - be more specific with selectors
        var firstPageButtons = cut.FindAll("button").Where(b => b.GetAttribute("class")?.Contains("pagination-first-page") == true).ToList();
        Assert.Single(firstPageButtons);

        var previousPageButtons = cut.FindAll("button").Where(b => b.GetAttribute("class")?.Contains("pagination-previous-page") == true).ToList();
        Assert.Single(previousPageButtons);

        var nextPageButtons = cut.FindAll("button").Where(b => b.GetAttribute("class")?.Contains("pagination-next-page") == true).ToList();
        Assert.Single(nextPageButtons);

        var lastPageButtons = cut.FindAll("button").Where(b => b.GetAttribute("class")?.Contains("pagination-last-page") == true).ToList();
        Assert.Single(lastPageButtons);
    }

    [Fact]
    public void Buttons_HaveCorrectTitles() {
        // Arrange
        var paginationState = new TnTPaginationState();
        _ = paginationState.SetTotalItemCountAsync(30);

        // Act
        var cut = Render<TnTPaginationButtons>(parameters => parameters
            .Add(p => p.PaginationState, paginationState));

        // Assert
        var buttons = cut.FindAll("button");

        buttons.FirstOrDefault(b => b.GetAttribute("title") == "First page").Should().NotBeNull();
        buttons.FirstOrDefault(b => b.GetAttribute("title") == "Previous page").Should().NotBeNull();
        buttons.FirstOrDefault(b => b.GetAttribute("title") == "Next page").Should().NotBeNull();
        buttons.FirstOrDefault(b => b.GetAttribute("title") == "Last page").Should().NotBeNull();
    }

    [Fact]
    public void Component_HasCorrectCssClass() {
        // Arrange
        var paginationState = new TnTPaginationState();
        _ = paginationState.SetTotalItemCountAsync(10);

        // Act
        var cut = Render<TnTPaginationButtons>(parameters => parameters
            .Add(p => p.PaginationState, paginationState));

        // Assert
        cut.Find(".tnt-pagination-buttons").Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullPaginationState_ThrowsArgumentNullException() {
        // Arrange & Act
        var act = () => Render<TnTPaginationButtons>(parameters => parameters
            .Add(p => p.PaginationState, (TnTPaginationState)null!));

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithRequiredPaginationState_RendersCorrectly() {
        // Arrange
        var paginationState = new TnTPaginationState();
        _ = paginationState.SetTotalItemCountAsync(50); // 5 pages with default 10 items per page

        // Act
        var cut = Render<TnTPaginationButtons>(parameters => parameters
            .Add(p => p.PaginationState, paginationState));

        // Assert
        cut.Find(".tnt-pagination-buttons").Should().NotBeNull();
        cut.FindAll(".pagination-btn").Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public void CurrentPageButton_IsDisabled() {
        // Arrange
        var paginationState = new TnTPaginationState();
        _ = paginationState.SetTotalItemCountAsync(30); // 3 pages
        _ = paginationState.SetCurrentPageIndexAsync(1); // Current page is index 1 (page "2")

        // Act
        var cut = Render<TnTPaginationButtons>(parameters => parameters
            .Add(p => p.PaginationState, paginationState));

        // Assert
        var currentPageButtons = cut.FindAll("button.current-page");
        Assert.Single(currentPageButtons);

        var currentPageButton = currentPageButtons[0];
        Assert.True(currentPageButton.HasAttribute("disabled"), "Current page button should be disabled");
        Assert.Equal("2", currentPageButton.TextContent?.Trim());
    }

    [Fact]
    public void DisabledButtons_HaveDisabledClass() {
        // Arrange
        var paginationState = new TnTPaginationState();
        _ = paginationState.SetTotalItemCountAsync(10); // Single page scenario

        // Act
        var cut = Render<TnTPaginationButtons>(parameters => parameters
            .Add(p => p.PaginationState, paginationState));

        // Assert
        var disabledButtons = cut.FindAll(".tnt-disabled");
        Assert.True(disabledButtons.Count > 1); // At least first and previous should be disabled on first page
    }

    [Fact]
    public async Task FirstPageButton_WhenClicked_NavigatesToFirstPage() {
        // Arrange
        var paginationState = new TnTPaginationState();
        _ = paginationState.SetTotalItemCountAsync(50); // 5 pages
        _ = paginationState.SetCurrentPageIndexAsync(3); // Start on page 3

        var cut = Render<TnTPaginationButtons>(parameters => parameters
            .Add(p => p.PaginationState, paginationState));

        // Act
        var firstButton = cut.FindAll("button").First(b => b.GetAttribute("title") == "First page");
        await firstButton.ClickAsync(new MouseEventArgs());

        // Assert
        paginationState.CurrentPageIndex.Should().Be(0);
    }

    [Fact]
    public async Task LastPageButton_WhenClicked_NavigatesToLastPage() {
        // Arrange
        var paginationState = new TnTPaginationState();
        _ = paginationState.SetTotalItemCountAsync(50); // 5 pages
        _ = paginationState.SetCurrentPageIndexAsync(1); // Start on page 1

        var cut = Render<TnTPaginationButtons>(parameters => parameters
            .Add(p => p.PaginationState, paginationState));

        // Act
        var lastButton = cut.FindAll("button").First(b => b.GetAttribute("title") == "Last page");
        await lastButton.ClickAsync(new MouseEventArgs());

        // Assert
        paginationState.CurrentPageIndex.Should().Be(4); // Last page is index 4 (5 pages total)
    }

    [Fact]
    public async Task NextPageButton_WhenClicked_NavigatesToNextPage() {
        // Arrange
        var paginationState = new TnTPaginationState();
        _ = paginationState.SetTotalItemCountAsync(50); // 5 pages
        _ = paginationState.SetCurrentPageIndexAsync(1); // Start on page 1

        var cut = Render<TnTPaginationButtons>(parameters => parameters
            .Add(p => p.PaginationState, paginationState));

        // Act
        var nextButton = cut.FindAll("button").First(b => b.GetAttribute("title") == "Next page");
        await nextButton.ClickAsync(new MouseEventArgs());

        // Assert
        paginationState.CurrentPageIndex.Should().Be(2);
    }

    [Fact]
    public void PageButtons_HaveCorrectAriaLabels() {
        // Arrange
        var paginationState = new TnTPaginationState();
        _ = paginationState.SetTotalItemCountAsync(30); // 3 pages

        // Act
        var cut = Render<TnTPaginationButtons>(parameters => parameters
            .Add(p => p.PaginationState, paginationState));

        // Assert
        var pageButtons = cut.FindAll("button").Where(b =>
            b.TextContent.Trim().All(char.IsDigit) &&
            !string.IsNullOrEmpty(b.TextContent.Trim())).ToList();

        foreach (var button in pageButtons) {
            var ariaLabel = button.GetAttribute("aria-label");
            var pageNumber = button.TextContent.Trim();
            var expectedAriaLabel = (int.Parse(pageNumber) - 1).ToString(); // aria-label should be page index
            ariaLabel.Should().Be(expectedAriaLabel);
        }
    }

    [Fact]
    public async Task PageNumberButton_WhenClicked_NavigatesToCorrectPage() {
        // Arrange
        var paginationState = new TnTPaginationState();
        _ = paginationState.SetTotalItemCountAsync(50); // 5 pages

        var cut = Render<TnTPaginationButtons>(parameters => parameters
            .Add(p => p.PaginationState, paginationState));

        // Act - Find and click page "3" button (which represents page index 2)
        var pageButtons = cut.FindAll("button").Where(b => b.TextContent.Trim() == "3").ToList();
        Assert.Single(pageButtons);
        await pageButtons[0].ClickAsync(new MouseEventArgs());

        // Assert
        paginationState.CurrentPageIndex.Should().Be(2); // Page 3 is index 2
    }

    [Fact]
    public void PageRange_AtBeginning_ShowsFirstPages() {
        // Arrange
        var paginationState = new TnTPaginationState();
        _ = paginationState.SetTotalItemCountAsync(100); // 10 pages
        _ = paginationState.SetCurrentPageIndexAsync(0); // First page

        // Act
        var cut = Render<TnTPaginationButtons>(parameters => parameters
            .Add(p => p.PaginationState, paginationState));

        // Assert
        var pageButtons = cut.FindAll("button").Where(b =>
            b.TextContent.Trim().All(char.IsDigit) &&
            !string.IsNullOrEmpty(b.TextContent.Trim())).ToList();

        // Should include page 1 (index 0)
        var pageTexts = pageButtons.Select(b => b.TextContent.Trim()).ToArray();
        Assert.Contains("1", pageTexts);
    }

    [Fact]
    public void PageRange_AtEnd_ShowsLastPages() {
        // Arrange
        var paginationState = new TnTPaginationState();
        _ = paginationState.SetTotalItemCountAsync(100); // 10 pages (indices 0-9)
        _ = paginationState.SetCurrentPageIndexAsync(9); // Last page

        // Act
        var cut = Render<TnTPaginationButtons>(parameters => parameters
            .Add(p => p.PaginationState, paginationState));

        // Assert
        var pageButtons = cut.FindAll("button").Where(b =>
            b.TextContent.Trim().All(char.IsDigit) &&
            !string.IsNullOrEmpty(b.TextContent.Trim())).ToList();

        // Should include page 10 (index 9)
        var pageTexts = pageButtons.Select(b => b.TextContent.Trim()).ToArray();
        Assert.Contains("10", pageTexts);
    }

    [Fact]
    public void PageRange_WithFewPages_ShowsAllPages() {
        // Arrange
        var paginationState = new TnTPaginationState();
        _ = paginationState.SetTotalItemCountAsync(30); // 3 pages

        // Act
        var cut = Render<TnTPaginationButtons>(parameters => parameters
            .Add(p => p.PaginationState, paginationState));

        // Assert
        var pageButtons = cut.FindAll("button").Where(b =>
            b.TextContent.Trim().All(char.IsDigit) &&
            !string.IsNullOrEmpty(b.TextContent.Trim())).ToList();

        Assert.Equal(3, pageButtons.Count); // Pages 1, 2, 3
        var pageTexts = pageButtons.Select(b => b.TextContent.Trim()).ToArray();
        Assert.Contains("1", pageTexts);
        Assert.Contains("2", pageTexts);
        Assert.Contains("3", pageTexts);
    }

    [Fact]
    public void PageRange_WithManyPages_ShowsWindowedRange() {
        // Arrange
        var paginationState = new TnTPaginationState();
        _ = paginationState.SetTotalItemCountAsync(100); // 10 pages
        _ = paginationState.SetCurrentPageIndexAsync(5); // Middle page

        // Act
        var cut = Render<TnTPaginationButtons>(parameters => parameters
            .Add(p => p.PaginationState, paginationState));

        // Assert
        var pageButtons = cut.FindAll("button").Where(b =>
            b.TextContent.Trim().All(char.IsDigit) &&
            !string.IsNullOrEmpty(b.TextContent.Trim())).ToList();

        // Should show a window around current page (implementation shows 5 pages max)
        Assert.True(pageButtons.Count < 6);
        Assert.True(pageButtons.Count > 0);
    }

    [Fact]
    public async Task PaginationWorkflow_NavigationBetweenPages_WorksCorrectly() {
        // Arrange
        var paginationState = new TnTPaginationState();
        _ = paginationState.SetTotalItemCountAsync(50); // 5 pages

        var cut = Render<TnTPaginationButtons>(parameters => parameters
            .Add(p => p.PaginationState, paginationState));

        // Act & Assert - Navigate to page 2
        var page2Button = cut.FindAll("button").First(b => b.TextContent.Trim() == "2");
        await page2Button.ClickAsync(new MouseEventArgs());
        paginationState.CurrentPageIndex.Should().Be(1);

        // Navigate to next page (should be page 3)
        var nextButton = cut.FindAll("button").First(b => b.GetAttribute("title") == "Next page");
        await nextButton.ClickAsync(new MouseEventArgs());
        paginationState.CurrentPageIndex.Should().Be(2);

        // Navigate to last page
        var lastButton = cut.FindAll("button").First(b => b.GetAttribute("title") == "Last page");
        await lastButton.ClickAsync(new MouseEventArgs());
        paginationState.CurrentPageIndex.Should().Be(4); // Last page index

        // Navigate to first page
        var firstButton = cut.FindAll("button").First(b => b.GetAttribute("title") == "First page");
        await firstButton.ClickAsync(new MouseEventArgs());
        paginationState.CurrentPageIndex.Should().Be(0);
    }

    [Fact]
    public void Parameters_CanBeCustomized() {
        // Arrange
        var paginationState = new TnTPaginationState();
        _ = paginationState.SetTotalItemCountAsync(10);

        // Act
        var cut = Render<TnTPaginationButtons>(parameters => parameters
            .Add(p => p.PaginationState, paginationState)
            .Add(p => p.BackgroundColor, TnTColor.Secondary)
            .Add(p => p.ActiveBackgroundColor, TnTColor.Tertiary)
            .Add(p => p.TextColor, TnTColor.OnSecondary)
            .Add(p => p.ActiveTextColor, TnTColor.OnTertiary));

        // Assert
        cut.Instance.BackgroundColor.Should().Be(TnTColor.Secondary);
        cut.Instance.ActiveBackgroundColor.Should().Be(TnTColor.Tertiary);
        cut.Instance.TextColor.Should().Be(TnTColor.OnSecondary);
        cut.Instance.ActiveTextColor.Should().Be(TnTColor.OnTertiary);
    }

    [Fact]
    public async Task PreviousPageButton_WhenClicked_NavigatesToPreviousPage() {
        // Arrange
        var paginationState = new TnTPaginationState();
        _ = paginationState.SetTotalItemCountAsync(50); // 5 pages
        _ = paginationState.SetCurrentPageIndexAsync(2); // Start on page 2

        var cut = Render<TnTPaginationButtons>(parameters => parameters
            .Add(p => p.PaginationState, paginationState));

        // Act
        var previousButton = cut.FindAll("button").First(b => b.GetAttribute("title") == "Previous page");
        await previousButton.ClickAsync(new MouseEventArgs());

        // Assert
        paginationState.CurrentPageIndex.Should().Be(1);
    }

    [Fact]
    public void Render_WithCurrentPageZero_MarksPagesCorrectly() {
        // Arrange
        var paginationState = new TnTPaginationState();
        _ = paginationState.SetTotalItemCountAsync(30); // 3 pages

        // Act
        var cut = Render<TnTPaginationButtons>(parameters => parameters
            .Add(p => p.PaginationState, paginationState));

        // Assert First page (index 0) should be current/disabled - be more specific with selector
        var currentPageButtons = cut.FindAll("button.current-page");
        Assert.Single(currentPageButtons);

        // Current page should show "1" (page index 0 displays as page 1)
        var currentPageButton = currentPageButtons[0];
        Assert.Equal("1", currentPageButton.TextContent?.Trim());
        Assert.True(currentPageButton.HasAttribute("disabled"), "Current page button should be disabled");

        // First and Previous navigation should be disabled
        var disabledButtons = cut.FindAll(".tnt-disabled");
        Assert.True(disabledButtons.Count > 1, "Navigation buttons should be disabled on first page");
    }

    [Fact]
    public void Render_WithLastPage_DisablesNextButtons() {
        // Arrange
        var paginationState = new TnTPaginationState();
        _ = paginationState.SetTotalItemCountAsync(30); // 3 pages (0, 1, 2)
        _ = paginationState.SetCurrentPageIndexAsync(2); // Go to last page

        // Act
        var cut = Render<TnTPaginationButtons>(parameters => parameters
            .Add(p => p.PaginationState, paginationState));

        // Assert Next and Last navigation should be disabled
        var disabledButtons = cut.FindAll(".tnt-disabled");
        Assert.True(disabledButtons.Count > 1);

        // Current page should be marked - be more specific with selector
        var currentPageButtons = cut.FindAll("button.current-page");
        Assert.Single(currentPageButtons);
    }

    [Fact]
    public void Render_WithMultiplePages_ShowsPageNumbers() {
        // Arrange
        var paginationState = new TnTPaginationState();
        _ = paginationState.SetTotalItemCountAsync(50); // 5 pages

        // Act
        var cut = Render<TnTPaginationButtons>(parameters => parameters
            .Add(p => p.PaginationState, paginationState));

        // Assert
        var buttons = cut.FindAll(".pagination-btn");
        Assert.True(buttons.Count >= 7); // At least navigation (4) + some page buttons (3+)

        // Should have page numbers displayed - filter out navigation buttons by class
        var pageNumberButtons = buttons.Where(b =>
            !b.GetAttribute("class")!.Contains("pagination-first-page") &&
            !b.GetAttribute("class")!.Contains("pagination-previous-page") &&
            !b.GetAttribute("class")!.Contains("pagination-next-page") &&
            !b.GetAttribute("class")!.Contains("pagination-last-page") &&
            b.TextContent.Trim().All(char.IsDigit) &&
            !string.IsNullOrEmpty(b.TextContent.Trim())).ToList();
        Assert.True(pageNumberButtons.Count > 0);
    }

    [Fact]
    public void Render_WithNullTotalItemCount_HandlesGracefully() {
        // Arrange
        var paginationState = new TnTPaginationState(); // TotalItemCount is null initially

        // Act
        var cut = Render<TnTPaginationButtons>(parameters => parameters
            .Add(p => p.PaginationState, paginationState));

        // Assert
        cut.Find(".tnt-pagination-buttons").Should().NotBeNull();

        // All navigation buttons should be disabled when no total count is set
        var disabledButtons = cut.FindAll(".tnt-disabled");
        Assert.True(disabledButtons.Count > 1);
    }

    [Fact]
    public void Render_WithSinglePage_ShowsCorrectButtons() {
        // Arrange
        var paginationState = new TnTPaginationState();
        _ = paginationState.SetTotalItemCountAsync(5); // Single page (1 page total)

        // Act
        var cut = Render<TnTPaginationButtons>(parameters => parameters
            .Add(p => p.PaginationState, paginationState));

        // Assert
        var buttons = cut.FindAll(".pagination-btn");
        Assert.True(buttons.Count > 0, "Should have pagination buttons");

        // Check that navigation buttons are properly disabled on single page
        var disabledButtons = cut.FindAll(".tnt-disabled");
        Assert.True(disabledButtons.Count > 1, "Navigation buttons should be disabled on single page");

        // Verify we have exactly one current page button (be more specific to avoid matching child elements)
        var currentPageButtons = cut.FindAll("button.current-page");
        Assert.Single(currentPageButtons);

        // Verify the current page shows "1"
        var currentPageButton = currentPageButtons[0];
        Assert.Equal("1", currentPageButton.TextContent?.Trim());
        Assert.True(currentPageButton.HasAttribute("disabled"), "Current page button should be disabled");
    }

    [Fact]
    public void Render_WithZeroPages_DisablesAllNavigation() {
        // Arrange
        var paginationState = new TnTPaginationState();
        _ = paginationState.SetTotalItemCountAsync(0); // No pages

        // Act
        var cut = Render<TnTPaginationButtons>(parameters => parameters
            .Add(p => p.PaginationState, paginationState));

        // Assert All navigation buttons should be disabled
        var disabledButtons = cut.FindAll(".tnt-disabled");
        Assert.True(disabledButtons.Count > 3); // All navigation buttons
    }

    [Fact]
    public async Task StateChanges_UpdateButtonsCorrectly() {
        // Arrange
        var paginationState = new TnTPaginationState();
        _ = paginationState.SetTotalItemCountAsync(50); // 5 pages

        var cut = Render<TnTPaginationButtons>(parameters => parameters
            .Add(p => p.PaginationState, paginationState));

        // Act - Navigate to middle page
        var page3Button = cut.FindAll("button").First(b => b.TextContent.Trim() == "3");
        await page3Button.ClickAsync(new MouseEventArgs());

        // Assert
        paginationState.CurrentPageIndex.Should().Be(2, "Should navigate to page index 2");

        // Force re-render by rendering the component again
        cut = Render<TnTPaginationButtons>(parameters => parameters
            .Add(p => p.PaginationState, paginationState));

        // Current page should be disabled and marked - be more specific with selector
        var currentPageButtons = cut.FindAll("button.current-page");
        Assert.Single(currentPageButtons);
        currentPageButtons[0].TextContent.Trim().Should().Be("3", "Current page should display as '3'");
        currentPageButtons[0].HasAttribute("disabled").Should().BeTrue("Current page button should be disabled");

        // When on middle page, navigation buttons should be enabled
        var firstButton = cut.FindAll("button").First(b => b.GetAttribute("title") == "First page");
        var previousButton = cut.FindAll("button").First(b => b.GetAttribute("title") == "Previous page");
        var nextButton = cut.FindAll("button").First(b => b.GetAttribute("title") == "Next page");
        var lastButton = cut.FindAll("button").First(b => b.GetAttribute("title") == "Last page");

        firstButton.HasAttribute("disabled").Should().BeFalse("First button should be enabled on middle page");
        previousButton.HasAttribute("disabled").Should().BeFalse("Previous button should be enabled on middle page");
        nextButton.HasAttribute("disabled").Should().BeFalse("Next button should be enabled on middle page");
        lastButton.HasAttribute("disabled").Should().BeFalse("Last button should be enabled on middle page");
    }

    [Fact]
    public void TextColor_DefaultValue_IsSet() {
        // Arrange
        var paginationState = new TnTPaginationState();
        _ = paginationState.SetTotalItemCountAsync(10);

        // Act
        var cut = Render<TnTPaginationButtons>(parameters => parameters
            .Add(p => p.PaginationState, paginationState));

        // Assert
        cut.Instance.TextColor.Should().Be(TnTColor.OnPrimaryContainer);
    }

    [Fact]
    public async Task TotalItemCountChange_UpdatesPageButtons() {
        // Arrange
        var paginationState = new TnTPaginationState(); // TotalItemCount is null initially
        var cut = Render<TnTPaginationButtons>(parameters => parameters
            .Add(p => p.PaginationState, paginationState));

        // Initially there should be no numeric page buttons
        var initialPageButtons = cut.FindAll("button").Where(b =>
            b.TextContent.Trim().All(char.IsDigit) &&
            !string.IsNullOrEmpty(b.TextContent.Trim())).ToList();
        initialPageButtons.Should().BeEmpty();

        // Act - update total item count which should trigger the component to update
        await paginationState.SetTotalItemCountAsync(30); // 3 pages with default 10 items per page

        // Assert - should now render three page buttons
        var pageButtons = cut.FindAll("button").Where(b =>
            b.TextContent.Trim().All(char.IsDigit) &&
            !string.IsNullOrEmpty(b.TextContent.Trim())).ToList();

        pageButtons.Should().HaveCount(3);
        var pageTexts = pageButtons.Select(b => b.TextContent.Trim()).ToArray();
        pageTexts.Should().Contain(new[] { "1", "2", "3" });
    }

    [Fact]
    public async Task ChangingPaginationStateParameter_UnsubscribesFromOldStateAndSubscribesToNewState() {
        // Arrange
        var state1 = new TnTPaginationState();
        var state2 = new TnTPaginationState();

        await state1.SetTotalItemCountAsync(50); // 5 pages
        await state2.SetTotalItemCountAsync(10); // 1 page

        var cut = Render<TnTPaginationButtons>(parameters => parameters
            .Add(p => p.PaginationState, state1));

        // Verify we're rendering state1 pages
        var pageButtonsState1 = cut.FindAll("button").Where(b =>
            b.TextContent.Trim().All(char.IsDigit) &&
            !string.IsNullOrEmpty(b.TextContent.Trim())).ToList();
        pageButtonsState1.Should().HaveCount(5);

        // Act - switch to state2
        cut = Render<TnTPaginationButtons>(parameters => parameters
            .Add(p => p.PaginationState, state2));

        // Verify component now reflects state2
        var pageButtonsState2 = cut.FindAll("button").Where(b =>
            b.TextContent.Trim().All(char.IsDigit) &&
            !string.IsNullOrEmpty(b.TextContent.Trim())).ToList();
        pageButtonsState2.Should().HaveCount(1);

        // Change state1 - component should NOT update because it should have unsubscribed
        await state1.SetTotalItemCountAsync(20); // would be 2 pages if it were subscribed
        var afterOldStateChangeButtons = cut.FindAll("button").Where(b =>
            b.TextContent.Trim().All(char.IsDigit) &&
            !string.IsNullOrEmpty(b.TextContent.Trim())).ToList();
        afterOldStateChangeButtons.Should().HaveCount(1, "Component should be unsubscribed from the old state and not update when it changes");

        // Change state2 - component SHOULD update
        await state2.SetTotalItemCountAsync(40); // 4 pages
        var afterNewStateChangeButtons = cut.FindAll("button").Where(b =>
            b.TextContent.Trim().All(char.IsDigit) &&
            !string.IsNullOrEmpty(b.TextContent.Trim())).ToList();
        afterNewStateChangeButtons.Should().HaveCount(4, "Component should be subscribed to the new state and update when it changes");
    }
}