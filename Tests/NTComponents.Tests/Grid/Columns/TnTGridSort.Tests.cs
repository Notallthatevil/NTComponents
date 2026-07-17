using System.Linq.Expressions;
using System.Reflection;
using NTComponents.Grid;

namespace NTComponents.Tests.Grid.Columns;

/// <summary>
///     Unit tests for <see cref="TnTGridSort{TGridItem}" />.
/// </summary>
public class TnTGridSort_Tests {

    [Fact]
    public void Append_PreservesFlipDirectionsFromOriginal() {
        // Arrange
        var sort1 = TnTGridSort<TestGridItem>.ByAscending(x => x.Name);
        sort1.FlipDirections = true;
        var sort2 = TnTGridSort<TestGridItem>.ByDescending(x => x.Age);

        // Act
        var result = sort1.Append(sort2);

        // Assert
        result.FlipDirections.Should().BeTrue();
    }

    [Fact]
    public void Append_WithDuplicateProperty_ThrowsInvalidOperationException() {
        // Arrange
        var sort1 = TnTGridSort<TestGridItem>.ByAscending(x => x.Name);
        var sort2 = TnTGridSort<TestGridItem>.ByDescending(x => x.Name);

        // Act & Assert
        var act = () => sort1.Append(sort2);
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*property*Name*already added*");
    }

    [Fact]
    public void Append_WithNull_ReturnsOriginalSort() {
        // Arrange
        var sort = TnTGridSort<TestGridItem>.ByAscending(x => x.Name);

        // Act
        var result = sort.Append(null);
        var properties = result.ToPropertyList().ToList();

        // Assert
        properties.Should().HaveCount(1);
        properties[0].PropertyName.Should().Be("Name");
        properties[0].Direction.Should().Be(SortDirection.Ascending);
    }

    [Fact]
    public void Append_WithOtherSort_CombinesBoth() {
        // Arrange
        var sort1 = TnTGridSort<TestGridItem>.ByAscending(x => x.Name);
        var sort2 = TnTGridSort<TestGridItem>.ByDescending(x => x.Age);

        // Act
        var result = sort1.Append(sort2);
        var properties = result.ToPropertyList().ToList();

        // Assert
        properties.Should().HaveCount(2);
        properties[0].PropertyName.Should().Be("Name");
        properties[0].Direction.Should().Be(SortDirection.Ascending);
        properties[1].PropertyName.Should().Be("Age");
        properties[1].Direction.Should().Be(SortDirection.Descending);
    }

    [Fact]
    public void Apply_EmptyQueryable_ReturnsEmpty() {
        // Arrange
        var items = new TestGridItem[0].AsQueryable();
        var sort = TnTGridSort<TestGridItem>.ByAscending(x => x.Name);

        // Act
        var result = sort.Apply(items).ToList();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Apply_InternalOverload_IgnoresAscendingParameter() {
        // Arrange
        var items = new[] {
            new TestGridItem { Name = "Charlie", Age = 30 },
            new TestGridItem { Name = "Alice", Age = 25 },
            new TestGridItem { Name = "Bob", Age = 35 }
        }.AsQueryable();
        var sort = TnTGridSort<TestGridItem>.ByDescending(x => x.Name);

        // Act
        var resultWithTrue = sort.Apply(items, true).ToList();
        var resultWithFalse = sort.Apply(items, false).ToList();

        // Assert
        resultWithTrue.Should().HaveCount(3);
        resultWithTrue[0].Name.Should().Be("Charlie");
        resultWithTrue[1].Name.Should().Be("Bob");
        resultWithTrue[2].Name.Should().Be("Alice");

        resultWithFalse.Should().HaveCount(3);
        resultWithFalse[0].Name.Should().Be("Charlie");
        resultWithFalse[1].Name.Should().Be("Bob");
        resultWithFalse[2].Name.Should().Be("Alice");
    }

    [Fact]
    public void Apply_MultipleProperties_SortsCorrectly() {
        // Arrange
        var items = new[] {
            new TestGridItem { Name = "Alice", Age = 30 },
            new TestGridItem { Name = "Bob", Age = 25 },
            new TestGridItem { Name = "Alice", Age = 20 },
            new TestGridItem { Name = "Bob", Age = 35 }
        }.AsQueryable();
        var sort = TnTGridSort<TestGridItem>.ByAscending(x => x.Name)
                                           .ThenDescending(x => x.Age);

        // Act
        var result = sort.Apply(items).ToList();

        // Assert
        result.Should().HaveCount(4);
        result[0].Name.Should().Be("Alice");
        result[0].Age.Should().Be(30);
        result[1].Name.Should().Be("Alice");
        result[1].Age.Should().Be(20);
        result[2].Name.Should().Be("Bob");
        result[2].Age.Should().Be(35);
        result[3].Name.Should().Be("Bob");
        result[3].Age.Should().Be(25);
    }

    [Fact]
    public void Apply_SingleItem_ReturnsSingleItem() {
        // Arrange
        var items = new[] { new TestGridItem { Name = "Alice", Age = 25 } }.AsQueryable();
        var sort = TnTGridSort<TestGridItem>.ByAscending(x => x.Name);

        // Act
        var result = sort.Apply(items).ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Alice");
    }

    [Fact]
    public void Apply_SingleProperty_SortsCorrectly() {
        // Arrange
        var items = new[] {
            new TestGridItem { Name = "Charlie", Age = 30 },
            new TestGridItem { Name = "Alice", Age = 25 },
            new TestGridItem { Name = "Bob", Age = 35 }
        }.AsQueryable();
        var sort = TnTGridSort<TestGridItem>.ByAscending(x => x.Name);

        // Act
        var result = sort.Apply(items).ToList();

        // Assert
        result.Should().HaveCount(3);
        result[0].Name.Should().Be("Alice");
        result[1].Name.Should().Be("Bob");
        result[2].Name.Should().Be("Charlie");
    }

    [Fact]
    public void Apply_WithBooleanAscending_SortsCorrectly() {
        // Arrange
        var items = new[] {
            new TestGridItem { Name = "Alice", IsActive = true },
            new TestGridItem { Name = "Bob", IsActive = false },
            new TestGridItem { Name = "Charlie", IsActive = true }
        }.AsQueryable();
        var sort = TnTGridSort<TestGridItem>.ByAscending(x => x.IsActive);

        // Act
        var result = sort.Apply(items).ToList();

        // Assert
        result.Should().HaveCount(3);
        result[0].IsActive.Should().BeFalse();
        result[1].IsActive.Should().BeTrue();
        result[2].IsActive.Should().BeTrue();
    }

    [Fact]
    public void Apply_WithDateTime_SortsCorrectly() {
        // Arrange
        var date1 = new DateTime(2023, 1, 1);
        var date2 = new DateTime(2023, 6, 1);
        var date3 = new DateTime(2023, 12, 1);
        var items = new[] {
            new TestGridItem { Name = "Alice", Created = date2 },
            new TestGridItem { Name = "Bob", Created = date3 },
            new TestGridItem { Name = "Charlie", Created = date1 }
        }.AsQueryable();
        var sort = TnTGridSort<TestGridItem>.ByAscending(x => x.Created);

        // Act
        var result = sort.Apply(items).ToList();

        // Assert
        result.Should().HaveCount(3);
        result[0].Created.Should().Be(date1);
        result[1].Created.Should().Be(date2);
        result[2].Created.Should().Be(date3);
    }

    [Fact]
    public void Apply_WithDecimal_SortsCorrectly() {
        // Arrange
        var items = new[] {
            new TestGridItem { Name = "Alice", Price = 15.99m },
            new TestGridItem { Name = "Bob", Price = 25.50m },
            new TestGridItem { Name = "Charlie", Price = 9.99m }
        }.AsQueryable();
        var sort = TnTGridSort<TestGridItem>.ByDescending(x => x.Price);

        // Act
        var result = sort.Apply(items).ToList();

        // Assert
        result.Should().HaveCount(3);
        result[0].Price.Should().Be(25.50m);
        result[1].Price.Should().Be(15.99m);
        result[2].Price.Should().Be(9.99m);
    }

    [Fact]
    public void Apply_WithDescending_SortsCorrectly() {
        // Arrange
        var items = new[] {
            new TestGridItem { Name = "Alice", Age = 25 },
            new TestGridItem { Name = "Charlie", Age = 30 },
            new TestGridItem { Name = "Bob", Age = 35 }
        }.AsQueryable();
        var sort = TnTGridSort<TestGridItem>.ByDescending(x => x.Age);

        // Act
        var result = sort.Apply(items).ToList();

        // Assert
        result.Should().HaveCount(3);
        result[0].Age.Should().Be(35);
        result[1].Age.Should().Be(30);
        result[2].Age.Should().Be(25);
    }

    [Fact]
    public void Apply_WithFlippedDirections_SortsCorrectly() {
        // Arrange
        var items = new[] {
            new TestGridItem { Name = "Charlie", Age = 30 },
            new TestGridItem { Name = "Alice", Age = 25 },
            new TestGridItem { Name = "Bob", Age = 35 }
        }.AsQueryable();
        var sort = TnTGridSort<TestGridItem>.ByAscending(x => x.Name);
        sort.FlipDirections = true;

        // Act
        var result = sort.Apply(items).ToList();

        // Assert
        result.Should().HaveCount(3);
        result[0].Name.Should().Be("Charlie");
        result[1].Name.Should().Be("Bob");
        result[2].Name.Should().Be("Alice");
    }

    [Fact]
    public void ByAscending_CreatesGridSortWithSingleProperty() {
        // Act
        var sort = TnTGridSort<TestGridItem>.ByAscending(x => x.Name);
        var properties = sort.ToPropertyList().ToList();

        // Assert
        properties.Should().HaveCount(1);
        properties[0].PropertyName.Should().Be("Name");
        properties[0].Direction.Should().Be(SortDirection.Ascending);
    }

    [Fact]
    public void ByAscending_WithComplexExpression_ThrowsArgumentException() {
        // Act & Assert
        var act = () => TnTGridSort<TestGridItem>.ByAscending(x => x.Name.ToLower());
        act.Should().Throw<ArgumentException>()
           .WithMessage("*can't be represented as a property name*");
    }

    [Fact]
    public void ByAscending_WithCustomComparer_CreatesGridSortWithComparer() {
        // Arrange
        var comparer = new CustomStringComparer();

        // Act
        var sort = TnTGridSort<TestGridItem>.ByAscending(x => x.Name, comparer);
        var properties = sort.ToPropertyList().ToList();

        // Assert
        properties.Should().HaveCount(1);
        properties[0].PropertyName.Should().Be("Name");
        properties[0].Direction.Should().Be(SortDirection.Ascending);
    }

    [Fact]
    public void ByAscending_WithNullExpression_ThrowsNullReferenceException() {
        // Act & Assert
        var act = () => TnTGridSort<TestGridItem>.ByAscending((Expression<Func<TestGridItem, string>>)null!);
        act.Should().Throw<NullReferenceException>();
    }

    [Fact]
    public void ByDescending_CreatesGridSortWithSingleProperty() {
        // Act
        var sort = TnTGridSort<TestGridItem>.ByDescending(x => x.Age);
        var properties = sort.ToPropertyList().ToList();

        // Assert
        properties.Should().HaveCount(1);
        properties[0].PropertyName.Should().Be("Age");
        properties[0].Direction.Should().Be(SortDirection.Descending);
    }

    [Fact]
    public void ByDescending_WithCustomComparer_CreatesGridSortWithComparer() {
        // Arrange
        var comparer = new CustomStringComparer();

        // Act
        var sort = TnTGridSort<TestGridItem>.ByDescending(x => x.Name, comparer);
        var properties = sort.ToPropertyList().ToList();

        // Assert
        properties.Should().HaveCount(1);
        properties[0].PropertyName.Should().Be("Name");
        properties[0].Direction.Should().Be(SortDirection.Descending);
    }

    [Fact]
    public void ByDescending_WithNullExpression_ThrowsNullReferenceException() {
        // Act & Assert
        var act = () => TnTGridSort<TestGridItem>.ByDescending((Expression<Func<TestGridItem, int>>)null!);
        act.Should().Throw<NullReferenceException>();
    }

    [Fact]
    public void Constructor_IsPrivate() {
        // Arrange & Act
        var constructors = typeof(TnTGridSort<TestGridItem>).GetConstructors(BindingFlags.Public | BindingFlags.Instance);

        // Assert
        constructors.Should().BeEmpty("Constructor should be private to enforce factory method usage");
    }

    [Fact]
    public void FlipDirections_InitiallyFalse() {
        // Act
        var sort = TnTGridSort<TestGridItem>.ByAscending(x => x.Name);

        // Assert
        sort.FlipDirections.Should().BeFalse();
    }

    [Fact]
    public void FlipDirections_SetToFalse_RestoresOriginalDirections() {
        // Arrange
        var sort = TnTGridSort<TestGridItem>.ByAscending(x => x.Name)
                                           .ThenDescending(x => x.Age);
        sort.FlipDirections = true;

        // Act
        sort.FlipDirections = false;
        var properties = sort.ToPropertyList().ToList();

        // Assert
        sort.FlipDirections.Should().BeFalse();
        properties[0].Direction.Should().Be(SortDirection.Ascending);
        properties[1].Direction.Should().Be(SortDirection.Descending);
    }

    [Fact]
    public void FlipDirections_SetToSameValue_DoesNotChangeDirections() {
        // Arrange
        var sort = TnTGridSort<TestGridItem>.ByAscending(x => x.Name);
        var originalProperties = sort.ToPropertyList().ToList();

        // Act
        sort.FlipDirections = false; // Setting to same value (false)
        var newProperties = sort.ToPropertyList().ToList();

        // Assert
        newProperties[0].Direction.Should().Be(originalProperties[0].Direction);
    }

    [Fact]
    public void FlipDirections_SetToTrue_FlipsAllDirections() {
        // Arrange
        var sort = TnTGridSort<TestGridItem>.ByAscending(x => x.Name)
                                           .ThenDescending(x => x.Age);

        // Act
        sort.FlipDirections = true;
        var properties = sort.ToPropertyList().ToList();

        // Assert
        sort.FlipDirections.Should().BeTrue();
        properties[0].Direction.Should().Be(SortDirection.Descending);
        properties[1].Direction.Should().Be(SortDirection.Ascending);
    }

    [Fact]
    public void ThenAscending_AddsSecondarySort() {
        // Act
        var sort = TnTGridSort<TestGridItem>.ByAscending(x => x.Name)
                                           .ThenAscending(x => x.Age);
        var properties = sort.ToPropertyList().ToList();

        // Assert
        properties.Should().HaveCount(2);
        properties[0].PropertyName.Should().Be("Name");
        properties[0].Direction.Should().Be(SortDirection.Ascending);
        properties[1].PropertyName.Should().Be("Age");
        properties[1].Direction.Should().Be(SortDirection.Ascending);
    }

    [Fact]
    public void ThenAscending_WithCustomComparer_AddsPropertyWithComparer() {
        // Arrange
        var comparer = new CustomStringComparer();

        // Act
        var sort = TnTGridSort<TestGridItem>.ByAscending(x => x.Age)
                                           .ThenAscending(x => x.Name, comparer);
        var properties = sort.ToPropertyList().ToList();

        // Assert
        properties.Should().HaveCount(2);
        properties[0].PropertyName.Should().Be("Age");
        properties[1].PropertyName.Should().Be("Name");
        properties[1].Direction.Should().Be(SortDirection.Ascending);
    }

    [Fact]
    public void ThenAscending_WithDuplicateProperty_ThrowsInvalidOperationException() {
        // Arrange
        var sort = TnTGridSort<TestGridItem>.ByAscending(x => x.Name);

        // Act & Assert
        var act = () => sort.ThenAscending(x => x.Name);
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*property*Name*already added*");
    }

    [Fact]
    public void ThenDescending_AddsSecondarySort() {
        // Act
        var sort = TnTGridSort<TestGridItem>.ByAscending(x => x.Name)
                                           .ThenDescending(x => x.Age);
        var properties = sort.ToPropertyList().ToList();

        // Assert
        properties.Should().HaveCount(2);
        properties[0].PropertyName.Should().Be("Name");
        properties[0].Direction.Should().Be(SortDirection.Ascending);
        properties[1].PropertyName.Should().Be("Age");
        properties[1].Direction.Should().Be(SortDirection.Descending);
    }

    [Fact]
    public void ThenDescending_WithDuplicateProperty_ThrowsInvalidOperationException() {
        // Arrange
        var sort = TnTGridSort<TestGridItem>.ByDescending(x => x.Age);

        // Act & Assert
        var act = () => sort.ThenDescending(x => x.Age);
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*property*Age*already added*");
    }

    [Fact]
    public void ToPropertyList_ReturnsCorrectProperties() {
        // Arrange
        var sort = TnTGridSort<TestGridItem>.ByAscending(x => x.Name)
                                           .ThenDescending(x => x.Age)
                                           .ThenAscending(x => x.Created);

        // Act
        var properties = sort.ToPropertyList().ToList();

        // Assert
        properties.Should().HaveCount(3);
        properties.Should().Contain(p => p.PropertyName == "Name" && p.Direction == SortDirection.Ascending);
        properties.Should().Contain(p => p.PropertyName == "Age" && p.Direction == SortDirection.Descending);
        properties.Should().Contain(p => p.PropertyName == "Created" && p.Direction == SortDirection.Ascending);
    }

    [Fact]
    public void ToPropertyList_WithFlippedDirections_ReturnsFlippedProperties() {
        // Arrange
        var sort = TnTGridSort<TestGridItem>.ByAscending(x => x.Name)
                                           .ThenDescending(x => x.Age);
        sort.FlipDirections = true;

        // Act
        var properties = sort.ToPropertyList().ToList();

        // Assert
        properties.Should().HaveCount(2);
        properties.Should().Contain(p => p.PropertyName == "Name" && p.Direction == SortDirection.Descending);
        properties.Should().Contain(p => p.PropertyName == "Age" && p.Direction == SortDirection.Ascending);
    }

    /// <summary>
    ///     Custom comparer for testing.
    /// </summary>
    private class CustomStringComparer : IComparer<string> {

        public int Compare(string? x, string? y) {
            // Reverse alphabetical order for testing
            return string.Compare(y, x, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    ///     Test model for grid sorting tests.
    /// </summary>
    private class TestGridItem {
        public int Age { get; set; }
        public DateTime Created { get; set; }
        public bool IsActive { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }
}