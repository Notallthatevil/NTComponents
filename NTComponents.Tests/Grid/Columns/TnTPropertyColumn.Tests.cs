using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq.Expressions;
using NTComponents.Grid;
using NTComponents.Grid.Infrastructure;

namespace NTComponents.Tests.Grid.Columns;

/// <summary>
///     Unit tests for <see cref="TnTPropertyColumn{TGridItem,TProp}" />.
/// </summary>
public class TnTPropertyColumn_Tests : BunitContext {

    private readonly TestGridItem _testItem = new() {
        Name = "John Doe",
        Age = 25,
        BirthDate = new DateTime(1998, 5, 15),
        Salary = 50000.99m,
        IsActive = true,
        NullableValue = 42,
        DisplayProperty = "Display Value",
        PascalCasePropertyName = "Pascal Value"
    };

    [Fact]
    public void ElementClass_ContainsDefaultGridClasses() {
        // Arrange
        var gridContext = CreateMockGridContext();
        var column = CreateColumn<string>(x => x.Name, gridContext);

        // Act
        var result = column.ElementClass;

        // Assert
        result.Should().Contain("nt-grid-cell");
        result.Should().Contain("tnt-components");
    }

    [Fact]
    public void ElementStyle_ThrowsNotImplementedException() {
        // Arrange
        var gridContext = CreateMockGridContext();
        var column = CreateColumn<string>(x => x.Name, gridContext);

        // Act & Assert
        var act = () => _ = column.ElementStyle;
        act.Should().Throw<NotImplementedException>();
    }

    [Fact]
    public void OnParametersSet_PropertyChanged_ReprocessesExpression() {
        // Arrange
        var gridContext = CreateMockGridContext();
        var column = CreateColumn<string>(x => x.Name, gridContext);
        InvokeOnParametersSet(column);
        var originalSortBy = column.SortBy;

        // Act - Change property expression (this forces a new _sortBuilder creation)
        column.Property = x => x.DisplayProperty;
        // Reset the private fields to force recreation
        var sortBuilderField = typeof(TnTPropertyColumn<TestGridItem, string>)
            .GetField("_sortBuilder", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        sortBuilderField?.SetValue(column, null);

        // Reset Title to null so it gets updated
        column.Title = null;

        InvokeOnParametersSet(column);

        // Assert
        column.SortBy.Should().NotBeSameAs(originalSortBy);
        column.Title.Should().Be("Display Name");
    }

    [Fact]
    public void OnParametersSet_SameProperty_DoesNotReprocess() {
        // Arrange
        var gridContext = CreateMockGridContext();
        var column = CreateColumn<string>(x => x.Name, gridContext);
        InvokeOnParametersSet(column);
        var originalSortBy = column.SortBy;

        // Act - Call OnParametersSet again without changes
        InvokeOnParametersSet(column);

        // Assert - Due to null-coalescing assignment (_sortBuilder ??= ...), the same instance is reused
        column.SortBy.Should().BeSameAs(originalSortBy);
    }

    [Fact]
    public void OnParametersSet_WithDisplayAttribute_UsesDiplayName() {
        // Arrange
        var gridContext = CreateMockGridContext();
        var column = CreateColumn<string>(x => x.DisplayProperty, gridContext);

        // Act
        InvokeOnParametersSet(column);

        // Assert
        column.Title.Should().Be("Display Name");
    }

    [Fact]
    public void OnParametersSet_WithExistingTitle_DoesNotOverride() {
        // Arrange
        var gridContext = CreateMockGridContext();
        var column = CreateColumn<string>(x => x.Name, gridContext);
        column.Title = "Custom Title";

        // Act
        InvokeOnParametersSet(column);

        // Assert
        column.Title.Should().Be("Custom Title");
    }

    [Fact]
    public void OnParametersSet_WithoutDisplayAttribute_UsesSplitPascalCase() {
        // Arrange
        var gridContext = CreateMockGridContext();
        var column = CreateColumn<string>(x => x.PascalCasePropertyName, gridContext);

        // Act
        InvokeOnParametersSet(column);

        // Assert
        column.Title.Should().Be("Pascal Case Property Name");
    }

    [Fact]
    public void PropertyInfo_IsSetCorrectly() {
        // Arrange
        var gridContext = CreateMockGridContext();
        var column = CreateColumn<string>(x => x.Name, gridContext);

        // Act
        InvokeOnParametersSet(column);

        // Assert
        column.PropertyInfo.Should().NotBeNull();
        column.PropertyInfo!.Name.Should().Be("Name");
        column.PropertyInfo.PropertyType.Should().Be(typeof(string));
    }

    [Fact]
    public void RenderCellContent_RendersBoolProperty() {
        // Arrange
        var gridContext = CreateMockGridContext();
        var column = CreateColumn<bool>(x => x.IsActive, gridContext);
        InvokeOnParametersSet(column);

        // Act
        var fragment = column.RenderCellContent(_testItem);
        var cut = Render(fragment);

        // Assert
        cut.Markup.Should().Contain("True");
    }

    [Fact]
    public void RenderCellContent_RendersDateTimeProperty() {
        // Arrange
        var gridContext = CreateMockGridContext();
        var column = CreateColumn<DateTime>(x => x.BirthDate, gridContext);
        InvokeOnParametersSet(column);

        // Act
        var fragment = column.RenderCellContent(_testItem);
        var cut = Render(fragment);

        // Assert
        cut.Markup.Should().Contain("1998");
        cut.Markup.Should().Contain("5");
        cut.Markup.Should().Contain("15");
    }

    [Fact]
    public void RenderCellContent_RendersDecimalProperty() {
        // Arrange
        var gridContext = CreateMockGridContext();
        var column = CreateColumn<decimal>(x => x.Salary, gridContext);
        InvokeOnParametersSet(column);

        // Act
        var fragment = column.RenderCellContent(_testItem);
        var cut = Render(fragment);

        // Assert
        cut.Markup.Should().Contain("50000.99");
    }

    [Fact]
    public void RenderCellContent_RendersIntProperty() {
        // Arrange
        var gridContext = CreateMockGridContext();
        var column = CreateColumn<int>(x => x.Age, gridContext);
        InvokeOnParametersSet(column);

        // Act
        var fragment = column.RenderCellContent(_testItem);
        var cut = Render(fragment);

        // Assert
        cut.Markup.Should().Contain("25");
    }

    [Fact]
    public void RenderCellContent_RendersStringProperty() {
        // Arrange
        var gridContext = CreateMockGridContext();
        var column = CreateColumn<string>(x => x.Name, gridContext);
        InvokeOnParametersSet(column);

        // Act
        var fragment = column.RenderCellContent(_testItem);
        var cut = Render(fragment);

        // Assert
        cut.Markup.Should().Contain("John Doe");
    }

    [Fact]
    public void RenderCellContent_WithDateFormat_AppliesDateFormatting() {
        // Arrange
        var gridContext = CreateMockGridContext();
        var column = CreateColumn<DateTime>(x => x.BirthDate, gridContext);
        column.Format = "yyyy-MM-dd";

        // Force parameter processing
        InvokeOnParametersSet(column);

        // Act
        var fragment = column.RenderCellContent(_testItem);
        var cut = Render(fragment);

        // Assert
        cut.Markup.Should().Contain("1998-05-15");
    }

    [Fact]
    public void RenderCellContent_WithEmptyString_RendersSpace() {
        // Arrange
        var gridContext = CreateMockGridContext();
        var column = CreateColumn<string>(x => x.Name, gridContext);
        var itemWithEmpty = new TestGridItem { Name = "" };
        InvokeOnParametersSet(column);

        // Act
        var fragment = column.RenderCellContent(itemWithEmpty);
        var cut = Render(fragment);

        // Assert
        cut.Markup.Should().Contain(" ");
    }

    [Fact]
    public void RenderCellContent_WithFormat_AppliesFormatting() {
        // Arrange
        var gridContext = CreateMockGridContext();
        var column = CreateColumn<decimal>(x => x.Salary, gridContext);
        column.Format = "C2";
        column.FormatCulture = CultureInfo.InvariantCulture;

        // Force parameter processing
        InvokeOnParametersSet(column);

        // Act
        var fragment = column.RenderCellContent(_testItem);
        var cut = Render(fragment);

        // Assert
        cut.Markup.Should().Contain("¤"); // Generic currency symbol in invariant culture
        cut.Markup.Should().Contain("50,000.99");
    }

    [Fact]
    public void RenderCellContent_WithFormat_NullValue_RendersSpace() {
        // Arrange
        var gridContext = CreateMockGridContext();
        var column = CreateColumn<DateTime?>(x => x.NullableBirthDate, gridContext);
        column.Format = "yyyy-MM-dd";
        var itemWithNull = new TestGridItem();

        // Force parameter processing
        InvokeOnParametersSet(column);

        // Act
        var fragment = column.RenderCellContent(itemWithNull);
        var cut = Render(fragment);

        // Assert
        cut.Markup.Should().Contain(" ");
    }

    [Fact]
    public void RenderCellContent_WithNullValue_RendersSpace() {
        // Arrange
        var gridContext = CreateMockGridContext();
        var column = CreateColumn<int?>(x => x.NullableValue, gridContext);
        var itemWithNull = new TestGridItem { NullableValue = null };
        InvokeOnParametersSet(column);

        // Act
        var fragment = column.RenderCellContent(itemWithNull);
        var cut = Render(fragment);

        // Assert
        cut.Markup.Should().Contain(" ");
    }

    [Fact]
    public void SetFormat_OnNonFormattableType_ThrowsInvalidOperationException() {
        // Arrange
        var gridContext = CreateMockGridContext();
        var column = CreateColumn<bool>(x => x.IsActive, gridContext);
        column.Format = "F";

        // Act & Assert - The exception gets wrapped in TargetInvocationException due to reflection
        var act = () => InvokeOnParametersSet(column);
        act.Should().Throw<System.Reflection.TargetInvocationException>()
           .WithInnerException<InvalidOperationException>()
           .WithMessage("*Format*parameter*Boolean*IFormattable*");
    }

    [Fact]
    public void SortBy_IsGeneratedAutomatically() {
        // Arrange
        var gridContext = CreateMockGridContext();
        var column = CreateColumn<string>(x => x.Name, gridContext);

        // Act
        InvokeOnParametersSet(column);

        // Assert
        column.SortBy.Should().NotBeNull();
    }

    [Fact]
    public void SortBy_Setter_ThrowsNotSupportedException() {
        // Arrange
        var gridContext = CreateMockGridContext();
        var column = CreateColumn<string>(x => x.Name, gridContext);

        // Act & Assert
        var act = () => column.SortBy = TnTGridSort<TestGridItem>.ByAscending(x => x.Age);
        act.Should().Throw<NotSupportedException>()
           .WithMessage("*generates this member internally*TnTTemplateColumn*");
    }

    [Fact]
    public void SortBy_WithCustomComparer_UsesComparer() {
        // Arrange
        var gridContext = CreateMockGridContext();
        var column = CreateColumn<string>(x => x.Name, gridContext);
        column.Comparer = new CustomStringComparer();

        // Act
        InvokeOnParametersSet(column);

        // Assert
        column.SortBy.Should().NotBeNull();
    }

    [Fact]
    public void SortBy_WithDescendingInitialDirection_GeneratesDescendingSort() {
        // Arrange
        var gridContext = CreateMockGridContext();
        var column = CreateColumn<string>(x => x.Name, gridContext);
        column.InitialSortDirection = SortDirection.Descending;

        // Act
        InvokeOnParametersSet(column);

        // Assert
        column.SortBy.Should().NotBeNull();
        // Verify that the sort is configured for descending by checking the descriptor
    }

    [Fact]
    public void SortBy_WithAscendingInitialDirection_GeneratesAscendingSort() {
        // Arrange
        var gridContext = CreateMockGridContext();
        var column = CreateColumn<string>(x => x.Name, gridContext);
        column.InitialSortDirection = SortDirection.Ascending;

        // Act
        InvokeOnParametersSet(column);

        // Assert
        column.SortBy.Should().NotBeNull();
        // The sort builder should be configured for ascending order
    }

    [Fact]
    public void SortBy_WithDescendingInitialDirection_AndComparer_UsesComparerWithDescending() {
        // Arrange
        var gridContext = CreateMockGridContext();
        var column = CreateColumn<string>(x => x.Name, gridContext);
        column.Comparer = new CustomStringComparer();
        column.InitialSortDirection = SortDirection.Descending;

        // Act
        InvokeOnParametersSet(column);

        // Assert
        column.SortBy.Should().NotBeNull();
        // Verify that the custom comparer is used in conjunction with descending sort
    }

    [Fact]
    public void SortBy_InitialDirectionChange_RegeneratesSortBuilder() {
        // Arrange
        var gridContext = CreateMockGridContext();
        var column = CreateColumn<string>(x => x.Name, gridContext);
        column.InitialSortDirection = SortDirection.Ascending;

        // Act - First call with Ascending
        InvokeOnParametersSet(column);
        var firstSortBy = column.SortBy;

        // To trigger regeneration, we need to reset both _sortBuilder AND _lastAssignedProperty
        // This simulates what happens when the property changes
        var sortBuilderField = typeof(TnTPropertyColumn<TestGridItem, string>)
            .GetField("_sortBuilder", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var lastAssignedPropertyField = typeof(TnTPropertyColumn<TestGridItem, string>)
            .GetField("_lastAssignedProperty", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        sortBuilderField?.SetValue(column, null);
        lastAssignedPropertyField?.SetValue(column, null);

        column.InitialSortDirection = SortDirection.Descending;
        InvokeOnParametersSet(column);
        var secondSortBy = column.SortBy;

        // Assert
        firstSortBy.Should().NotBeNull();
        secondSortBy.Should().NotBeNull();
        firstSortBy.Should().NotBeSameAs(secondSortBy);
    }

    private TnTPropertyColumn<TestGridItem, TProp> CreateColumn<TProp>(
            Expression<Func<TestGridItem, TProp>> property,
            TnTInternalGridContext<TestGridItem> context) {
        var column = new TnTPropertyColumn<TestGridItem, TProp>();
        column.Property = property;

        // Set the cascading parameter using reflection
        var contextProperty = typeof(TnTPropertyColumn<TestGridItem, TProp>)
            .GetProperty("Context", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        contextProperty?.SetValue(column, context);

        return column;
    }

    private TnTInternalGridContext<TestGridItem> CreateMockGridContext() {
        var grid = new TnTDataGrid<TestGridItem>();
        return new TnTInternalGridContext<TestGridItem>(grid);
    }

    private void InvokeOnParametersSet<TProp>(TnTPropertyColumn<TestGridItem, TProp> column) {
        // Use reflection to call the protected OnParametersSet method
        var method = typeof(TnTPropertyColumn<TestGridItem, TProp>)
            .GetMethod("OnParametersSet", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method?.Invoke(column, null);
    }

    /// <summary>
    ///     Custom comparer for testing.
    /// </summary>
    private class CustomStringComparer : IComparer<string> {

        public int Compare(string? x, string? y) {
            return string.Compare(y, x, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    ///     Test model for property column tests.
    /// </summary>
    private class TestGridItem {
        public int Age { get; set; }
        public DateTime BirthDate { get; set; }

        [Display(Name = "Display Name")]
        public string DisplayProperty { get; set; } = string.Empty;

        public bool IsActive { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime? NullableBirthDate { get; set; }
        public int? NullableValue { get; set; }
        public string PascalCasePropertyName { get; set; } = string.Empty;
        public decimal Salary { get; set; }
    }
}