using NTComponents.Grid;

namespace NTComponents.Tests.Grid;

/// <summary>
///     Unit tests for <see cref="TnTPaginationState" />.
/// </summary>
public class TnTPaginationState_Tests {

    private readonly IQueryable<TestItem> _testItems = new[]
        {
        new TestItem { Id = 1, Name = "Item 1" },
        new TestItem { Id = 2, Name = "Item 2" },
        new TestItem { Id = 3, Name = "Item 3" },
        new TestItem { Id = 4, Name = "Item 4" },
        new TestItem { Id = 5, Name = "Item 5" },
        new TestItem { Id = 6, Name = "Item 6" },
        new TestItem { Id = 7, Name = "Item 7" },
        new TestItem { Id = 8, Name = "Item 8" },
        new TestItem { Id = 9, Name = "Item 9" },
        new TestItem { Id = 10, Name = "Item 10" },
        new TestItem { Id = 11, Name = "Item 11" },
        new TestItem { Id = 12, Name = "Item 12" },
        new TestItem { Id = 13, Name = "Item 13" },
        new TestItem { Id = 14, Name = "Item 14" },
        new TestItem { Id = 15, Name = "Item 15" }
    }.AsQueryable();

    [Fact]
    public void Apply_WithEmptySource_ReturnsEmptyResult() {
        // Arrange
        var paginationState = new TnTPaginationState();
        var emptySource = Enumerable.Empty<TestItem>().AsQueryable();

        // Act
        var result = paginationState.Apply(emptySource).ToList();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Apply_WithFirstPage_ReturnsFirstItems() {
        // Arrange
        var paginationState = new TnTPaginationState();
        paginationState.ItemsPerPage = 5;

        // Act
        var result = paginationState.Apply(_testItems).ToList();

        // Assert
        result.Should().HaveCount(5);
        result[0].Id.Should().Be(1);
        result[1].Id.Should().Be(2);
        result[2].Id.Should().Be(3);
        result[3].Id.Should().Be(4);
        result[4].Id.Should().Be(5);
    }

    [Fact]
    public void Apply_WithLastPartialPage_ReturnsRemainingItems() {
        // Arrange
        var paginationState = new TnTPaginationState();
        paginationState.ItemsPerPage = 7;
        _ = paginationState.SetCurrentPageIndexAsync(2); // Third page

        // Act
        var result = paginationState.Apply(_testItems).ToList();

        // Assert
        result.Should().HaveCount(1); // Only 1 item left (15 items, 7 per page, page 2 = items 15)
        result[0].Id.Should().Be(15);
    }

    [Fact]
    public void Apply_WithPageBeyondData_ReturnsEmptyResult() {
        // Arrange
        var paginationState = new TnTPaginationState();
        paginationState.ItemsPerPage = 10;
        _ = paginationState.SetCurrentPageIndexAsync(5); // Far beyond available data

        // Act
        var result = paginationState.Apply(_testItems).ToList();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Apply_WithSecondPage_ReturnsCorrectItems() {
        // Arrange
        var paginationState = new TnTPaginationState();
        paginationState.ItemsPerPage = 5;
        _ = paginationState.SetCurrentPageIndexAsync(1);

        // Act
        var result = paginationState.Apply(_testItems).ToList();

        // Assert
        result.Should().HaveCount(5);
        result[0].Id.Should().Be(6);
        result[1].Id.Should().Be(7);
        result[2].Id.Should().Be(8);
        result[3].Id.Should().Be(9);
        result[4].Id.Should().Be(10);
    }

    [Fact]
    public void Apply_WithZeroItemsPerPage_ReturnsEmptyResult() {
        // Arrange
        var paginationState = new TnTPaginationState();
        paginationState.ItemsPerPage = 0;

        // Act
        var result = paginationState.Apply(_testItems).ToList();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_InitializesWithDefaultValues() {
        // Arrange & Act
        var paginationState = new TnTPaginationState();

        // Assert
        paginationState.CurrentPageIndex.Should().Be(0);
        paginationState.ItemsPerPage.Should().Be(10);
        paginationState.TotalItemCount.Should().BeNull();
        paginationState.LastPageIndex.Should().BeNull();
    }

    [Fact]
    public async Task FullPaginationWorkflow_WorksCorrectly() {
        // Arrange
        var paginationState = new TnTPaginationState();
        paginationState.ItemsPerPage = 6;

        // Act & Assert - Set initial total count
        await paginationState.SetTotalItemCountAsync(15);
        paginationState.TotalItemCount.Should().Be(15);
        paginationState.LastPageIndex.Should().Be(2); // (15-1) / 6 = 2

        // Navigate to different pages and verify results
        var page0 = paginationState.Apply(_testItems).ToList();
        page0.Should().HaveCount(6);
        page0[0].Id.Should().Be(1);

        await paginationState.SetCurrentPageIndexAsync(1);
        var page1 = paginationState.Apply(_testItems).ToList();
        page1.Should().HaveCount(6);
        page1[0].Id.Should().Be(7);

        await paginationState.SetCurrentPageIndexAsync(2);
        var page2 = paginationState.Apply(_testItems).ToList();
        page2.Should().HaveCount(3); // Last page has only 3 items
        page2[0].Id.Should().Be(13);
    }

    [Fact]
    public void GetHashCode_WithDifferentCurrentPageIndex_ReturnsDifferentHashCode() {
        // Arrange
        var state1 = new TnTPaginationState();
        var state2 = new TnTPaginationState();
        _ = state2.SetCurrentPageIndexAsync(1);

        // Act
        var hash1 = state1.GetHashCode();
        var hash2 = state2.GetHashCode();

        // Assert
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void GetHashCode_WithDifferentItemsPerPage_ReturnsDifferentHashCode() {
        // Arrange
        var state1 = new TnTPaginationState { ItemsPerPage = 5 };
        var state2 = new TnTPaginationState { ItemsPerPage = 10 };

        // Act
        var hash1 = state1.GetHashCode();
        var hash2 = state2.GetHashCode();

        // Assert
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void GetHashCode_WithDifferentTotalItemCount_ReturnsDifferentHashCode() {
        // Arrange
        var state1 = new TnTPaginationState();
        var state2 = new TnTPaginationState();
        _ = state2.SetTotalItemCountAsync(50);

        // Act
        var hash1 = state1.GetHashCode();
        var hash2 = state2.GetHashCode();

        // Assert
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void GetHashCode_WithSameValues_ReturnsSameHashCode() {
        // Arrange
        var state1 = new TnTPaginationState();
        state1.ItemsPerPage = 5;
        _ = state1.SetCurrentPageIndexAsync(2);
        _ = state1.SetTotalItemCountAsync(50);

        var state2 = new TnTPaginationState();
        state2.ItemsPerPage = 5;
        _ = state2.SetCurrentPageIndexAsync(2);
        _ = state2.SetTotalItemCountAsync(50);

        // Act
        var hash1 = state1.GetHashCode();
        var hash2 = state2.GetHashCode();

        // Assert
        hash1.Should().Be(hash2);
    }

    [Fact]
    public void ItemsPerPage_AffectsLastPageIndex() {
        // Arrange
        var paginationState = new TnTPaginationState();
        paginationState.ItemsPerPage = 5;

        // Act
        _ = paginationState.SetTotalItemCountAsync(23);

        // Assert
        paginationState.LastPageIndex.Should().Be(4); // (23-1) / 5 = 4
    }

    [Fact]
    public void ItemsPerPage_CanBeSet() {
        // Arrange
        var paginationState = new TnTPaginationState();

        // Act
        paginationState.ItemsPerPage = 5;

        // Assert
        paginationState.ItemsPerPage.Should().Be(5);
    }

    [Fact]
    public void ItemsPerPage_WithZeroValue_AllowsZero() {
        // Arrange
        var paginationState = new TnTPaginationState();

        // Act
        paginationState.ItemsPerPage = 0;

        // Assert
        paginationState.ItemsPerPage.Should().Be(0);
    }

    [Fact]
    public void LastPageIndex_WithExactMultiple_ReturnsCorrectIndex() {
        // Arrange
        var paginationState = new TnTPaginationState();

        // Act
        _ = paginationState.SetTotalItemCountAsync(20); // Exactly 2 full pages

        // Assert
        paginationState.LastPageIndex.Should().Be(1); // (20-1) / 10 = 1
    }

    [Fact]
    public void LastPageIndex_WithNullTotalItemCount_ReturnsNull() {
        // Arrange
        var paginationState = new TnTPaginationState();

        // Act & Assert
        paginationState.LastPageIndex.Should().BeNull();
    }

    [Fact]
    public void LastPageIndex_WithPartialPage_ReturnsCorrectIndex() {
        // Arrange
        var paginationState = new TnTPaginationState();

        // Act
        _ = paginationState.SetTotalItemCountAsync(25); // 2 full pages + 5 items

        // Assert
        paginationState.LastPageIndex.Should().Be(2); // (25-1) / 10 = 2
    }

    [Fact]
    public void LastPageIndex_WithSingleItem_ReturnsZero() {
        // Arrange
        var paginationState = new TnTPaginationState();

        // Act
        _ = paginationState.SetTotalItemCountAsync(1);

        // Assert
        paginationState.LastPageIndex.Should().Be(0); // (1-1) / 10 = 0
    }

    [Fact]
    public void LastPageIndex_WithZeroItems_ReturnsZero() {
        // Arrange
        var paginationState = new TnTPaginationState();

        // Act
        _ = paginationState.SetTotalItemCountAsync(0);

        // Assert
        paginationState.LastPageIndex.Should().Be(0); // (0-1) / 10 = 0 (integer division)
    }

    [Fact]
    public async Task LastPageIndex_WithZeroItemsPerPage_ThrowsDivisionByZero() {
        // Arrange
        var paginationState = new TnTPaginationState();
        paginationState.ItemsPerPage = 0;
        await paginationState.SetTotalItemCountAsync(10);

        // Act & Assert
        var act = () => paginationState.LastPageIndex;
        act.Should().Throw<DivideByZeroException>();
    }

    [Fact]
    public async Task MultipleConsecutiveUpdates_HandleCorrectly() {
        // Arrange
        var paginationState = new TnTPaginationState();

        // Act
        await paginationState.SetTotalItemCountAsync(100);
        await paginationState.SetCurrentPageIndexAsync(5);
        await paginationState.SetTotalItemCountAsync(60); // Should adjust page from 5 to 5 (still valid)
        await paginationState.SetTotalItemCountAsync(30); // Should adjust page from 5 to 2

        // Assert
        paginationState.TotalItemCount.Should().Be(30);
        paginationState.CurrentPageIndex.Should().Be(2);
        paginationState.LastPageIndex.Should().Be(2);
    }

    [Fact]
    public async Task PageAdjustment_WhenTotalItemsReduced_WorksCorrectly() {
        // Arrange
        var paginationState = new TnTPaginationState();
        paginationState.ItemsPerPage = 5;
        await paginationState.SetTotalItemCountAsync(25); // 5 pages (0-4)
        await paginationState.SetCurrentPageIndexAsync(4); // Go to last page

        // Act - Reduce total items so current page is no longer valid
        await paginationState.SetTotalItemCountAsync(12); // Now only 3 pages (0-2)

        // Assert
        paginationState.CurrentPageIndex.Should().Be(2); // Should move to last valid page
        paginationState.LastPageIndex.Should().Be(2);
        paginationState.TotalItemCount.Should().Be(12);
    }

    [Fact]
    public async Task SetCurrentPageIndexAsync_ReturnsCompletedTask() {
        // Arrange
        var paginationState = new TnTPaginationState();

        // Act
        var task = paginationState.SetCurrentPageIndexAsync(2);

        // Assert
        task.Should().NotBeNull();
        task.IsCompleted.Should().BeTrue();
        await task; // Should not throw
    }

    [Fact]
    public async Task SetCurrentPageIndexAsync_UpdatesCurrentPageIndex() {
        // Arrange
        var paginationState = new TnTPaginationState();

        // Act
        await paginationState.SetCurrentPageIndexAsync(3);

        // Assert
        paginationState.CurrentPageIndex.Should().Be(3);
    }

    [Fact]
    public async Task SetCurrentPageIndexAsync_WithNegativeValue_SetsNegativeIndex() {
        // Arrange
        var paginationState = new TnTPaginationState();

        // Act
        await paginationState.SetCurrentPageIndexAsync(-1);

        // Assert The method doesn't validate the input, so it allows negative values
        paginationState.CurrentPageIndex.Should().Be(-1);
    }

    [Fact]
    public async Task SetTotalItemCountAsync_ReturnsCompletedTask() {
        // Arrange
        var paginationState = new TnTPaginationState();

        // Act
        var task = paginationState.SetTotalItemCountAsync(40);

        // Assert
        task.Should().NotBeNull();
        task.IsCompleted.Should().BeTrue();
        await task; // Should not throw
    }

    [Fact]
    public async Task SetTotalItemCountAsync_UpdatesTotalItemCount() {
        // Arrange
        var paginationState = new TnTPaginationState();

        // Act
        await paginationState.SetTotalItemCountAsync(50);

        // Assert
        paginationState.TotalItemCount.Should().Be(50);
    }

    [Fact]
    public async Task SetTotalItemCountAsync_WithCurrentPageBeyondLastPage_AdjustsCurrentPageIndex() {
        // Arrange
        var paginationState = new TnTPaginationState();
        await paginationState.SetCurrentPageIndexAsync(5); // Page 5 (6th page)
        await paginationState.SetTotalItemCountAsync(100); // Should allow page 5

        // Act
        await paginationState.SetTotalItemCountAsync(25); // Only allows pages 0-2

        // Assert
        paginationState.CurrentPageIndex.Should().Be(2); // Should move to last valid page
        paginationState.LastPageIndex.Should().Be(2);
    }

    [Fact]
    public async Task SetTotalItemCountAsync_WithCurrentPageValid_DoesNotAdjustPage() {
        // Arrange
        var paginationState = new TnTPaginationState();
        await paginationState.SetCurrentPageIndexAsync(2);
        await paginationState.SetTotalItemCountAsync(50); // Pages 0-4 available

        // Act
        await paginationState.SetTotalItemCountAsync(40); // Pages 0-3 available, page 2 still valid

        // Assert
        paginationState.CurrentPageIndex.Should().Be(2); // Should remain at 2
    }

    [Fact]
    public async Task SetTotalItemCountAsync_WithCurrentPageZero_DoesNotAdjustPage() {
        // Arrange
        var paginationState = new TnTPaginationState(); // CurrentPageIndex is 0

        // Act
        await paginationState.SetTotalItemCountAsync(5); // Reduces available pages

        // Assert
        paginationState.CurrentPageIndex.Should().Be(0); // Should remain at 0
    }

    [Fact]
    public async Task SetTotalItemCountAsync_WithNegativeValue_SetsNegativeCount() {
        // Arrange
        var paginationState = new TnTPaginationState();

        // Act
        await paginationState.SetTotalItemCountAsync(-5);

        // Assert The method doesn't validate the input, so it allows negative values
        paginationState.TotalItemCount.Should().Be(-5);
    }

    [Fact]
    public async Task SetTotalItemCountAsync_WithSameValue_ReturnsCompletedTask() {
        // Arrange
        var paginationState = new TnTPaginationState();
        await paginationState.SetTotalItemCountAsync(30);

        // Act
        var task = paginationState.SetTotalItemCountAsync(30);

        // Assert
        task.Should().NotBeNull();
        task.IsCompleted.Should().BeTrue();
    }

    /// <summary>
    ///     Test model for pagination tests.
    /// </summary>
    private class TestItem {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}