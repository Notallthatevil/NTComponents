using System.Linq.Expressions;

namespace NTComponents.Tests.Grid;

public class NTGridSort_Tests {
    private static readonly SortItem[] _items = [
        new("Bravo", 2, new SortGroup("Blue")),
        new("Alpha", 3, new SortGroup("Red")),
        new("Alpha", 1, new SortGroup("Blue"))
    ];

    [Fact]
    public void Apply_Enumerable_Uses_Configured_Primary_And_Secondary_Directions() {
        var sort = NTGridSort<SortItem>.ByAscending(item => item.Name).ThenDescending(item => item.Rank);

        var result = sort.Apply(_items.AsEnumerable(), SortDirection.Ascending, thenBy: false).ToArray();

        result.Select(item => (item.Name, item.Rank)).Should().Equal(("Alpha", 3), ("Alpha", 1), ("Bravo", 2));
        sort.GetSortDescriptors(SortDirection.Ascending).Should().Equal(
            new NTSortDescriptor("Name", SortDirection.Ascending),
            new NTSortDescriptor("Rank", SortDirection.Descending));
    }

    [Fact]
    public void Apply_Enumerable_Reverses_The_Entire_Sort_When_Direction_Changes() {
        var sort = NTGridSort<SortItem>.ByAscending(item => item.Name).ThenDescending(item => item.Rank);

        var result = sort.Apply(_items.AsEnumerable(), SortDirection.Descending, thenBy: false).ToArray();

        result.Select(item => (item.Name, item.Rank)).Should().Equal(("Bravo", 2), ("Alpha", 1), ("Alpha", 3));
        sort.GetSortDescriptors(SortDirection.Descending).Should().Equal(
            new NTSortDescriptor("Name", SortDirection.Descending),
            new NTSortDescriptor("Rank", SortDirection.Ascending));
    }

    [Fact]
    public void Apply_Enumerable_Appends_To_An_Existing_Order() {
        var source = _items.OrderBy(item => item.Group.Name);
        var sort = NTGridSort<SortItem>.ByAscending(item => item.Name);

        var result = sort.Apply(source, SortDirection.Ascending, thenBy: true).ToArray();

        result.Select(item => (item.Group.Name, item.Name, item.Rank)).Should().Equal(
            ("Blue", "Alpha", 1),
            ("Blue", "Bravo", 2),
            ("Red", "Alpha", 3));
    }

    [Fact]
    public void Apply_Queryable_Supports_Primary_And_Existing_Order_Paths() {
        var primarySort = NTGridSort<SortItem>.ByDescending(item => item.Rank);
        var primary = primarySort.Apply(_items.AsQueryable(), SortDirection.Descending, thenBy: false).ToArray();
        var ascending = NTGridSort<SortItem>.ByAscending(item => item.Rank)
            .ThenAscending(item => item.Name)
            .Apply(_items.AsQueryable(), SortDirection.Ascending, thenBy: false)
            .ToArray();
        var grouped = _items.AsQueryable().OrderBy(item => item.Group.Name);
        var secondarySort = NTGridSort<SortItem>.ByAscending(item => item.Name);

        var secondary = secondarySort.Apply(grouped, SortDirection.Descending, thenBy: true).ToArray();

        primary.Select(item => item.Rank).Should().Equal(3, 2, 1);
        ascending.Select(item => item.Rank).Should().Equal(1, 2, 3);
        secondary.Select(item => (item.Group.Name, item.Name, item.Rank)).Should().Equal(
            ("Blue", "Bravo", 2),
            ("Blue", "Alpha", 1),
            ("Red", "Alpha", 3));
        primarySort.GetSortDescriptors(SortDirection.Descending).Should().Equal(new NTSortDescriptor("Rank", SortDirection.Descending));
    }

    [Fact]
    public void Nested_And_Boxed_Members_Produce_Provider_Compatible_Property_Names() {
        var nested = NTGridSort<SortItem>.ByAscending(item => item.Group.Name);
        var boxed = NTGridSort<SortItem>.ByAscending<object>(item => item.Rank);

        nested.PropertyName.Should().Be("Group.Name");
        nested.StateSignature.Should().Be("Group.Name:asc");
        boxed.PropertyName.Should().Be("Rank");
        boxed.StateSignature.Should().Be("Rank:asc");
    }

    [Fact]
    public void Non_Member_And_Captured_Member_Expressions_Are_Rejected_As_Provider_Sorts() {
        var captured = new SortGroup("Captured");

        var methodCall = () => NTGridSort<SortItem>.ByAscending(item => item.Name.ToLowerInvariant());
        var capturedMember = () => NTGridSort<SortItem>.ByAscending(_ => captured.Name);

        methodCall.Should().Throw<ArgumentException>()
            .WithParameterName("expression")
            .WithMessage("*Only member expressions*");
        capturedMember.Should().Throw<ArgumentException>()
            .WithParameterName("expression")
            .WithMessage("*Only member expressions*");
    }

    [Fact]
    public void Null_Expressions_Are_Rejected_Before_Changing_The_Sort() {
        var sort = NTGridSort<SortItem>.ByAscending(item => item.Name);

        var create = () => NTGridSort<SortItem>.ByAscending<string>(null!);
        var append = () => sort.ThenDescending<int>(null!);

        create.Should().Throw<ArgumentNullException>().WithParameterName("expression");
        append.Should().Throw<ArgumentNullException>().WithParameterName("expression");
        sort.StateSignature.Should().Be("Name:asc");
    }

    private sealed record SortItem(string Name, int Rank, SortGroup Group);

    private sealed record SortGroup(string Name);
}
