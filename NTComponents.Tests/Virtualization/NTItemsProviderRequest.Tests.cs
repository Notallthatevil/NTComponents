using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using NTComponents;
using NTComponents.Virtualization;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace NTComponents.Tests.Virtualization;

public class NTItemsProviderRequest_Tests {

    [Fact]
    public async Task BindAsync_Parses_Sorts_QueryValues() {
        var context = new DefaultHttpContext();
        context.Request.Query = new QueryCollection(new Dictionary<string, StringValues> {
            [nameof(NTItemsProviderRequest.StartIndex)] = "5",
            [nameof(NTItemsProviderRequest.Count)] = "10",
            [nameof(NTItemsProviderRequest.Sorts)] = new StringValues(["Name,Ascending", "Age,Descending"])
        });

        var request = await NTItemsProviderRequest.BindAsync(context);

        request.Should().NotBeNull();
        request.Value.StartIndex.Should().Be(5);
        request.Value.Count.Should().Be(10);
        request.Value.Sorts.Should().Equal("Name,Ascending", "Age,Descending");

        var sorts = request.Value.SortOnProperties;
        sorts.Should().HaveCount(2);
        sorts[0].Key.Should().Be("Name");
        sorts[0].Value.Should().Be(SortDirection.Ascending);
        sorts[1].Key.Should().Be("Age");
        sorts[1].Value.Should().Be(SortDirection.Descending);
    }

    [Fact]
    public async Task BindAsync_Parses_Legacy_SortOnProperties_QueryValue() {
        var context = new DefaultHttpContext();
        context.Request.Query = new QueryCollection(new Dictionary<string, StringValues> {
            [nameof(NTItemsProviderRequest.StartIndex)] = "0",
            [nameof(NTItemsProviderRequest.SortOnProperties)] = "[Name,Ascending],[Age,Descending]"
        });

        var request = await NTItemsProviderRequest.BindAsync(context);

        request.Should().NotBeNull();
        request.Value.Sorts.Should().Equal("[Name,Ascending],[Age,Descending]");
        request.Value.SortOnProperties.Select(sort => sort.Key).Should().Equal("Name", "Age");
    }

    [Fact]
    public void SortOnProperties_Parses_Single_Csv_Sort_Value() {
        var request = new NTItemsProviderRequest {
            StartIndex = 0,
            Sorts = ["Name,Ascending,Age,Descending"]
        };

        request.SortOnProperties.Should().Equal([
            new KeyValuePair<string, SortDirection>("Name", SortDirection.Ascending),
            new KeyValuePair<string, SortDirection>("Age", SortDirection.Descending)
        ]);
    }

    [Fact]
    public void SortOnProperties_Defaults_Invalid_Direction_To_Ascending() {
        var request = new NTItemsProviderRequest {
            StartIndex = 0,
            Sorts = ["Name,Invalid"]
        };

        request.SortOnProperties.Should().Equal([
            new KeyValuePair<string, SortDirection>("Name", SortDirection.Ascending)
        ]);
    }

    [Fact]
    public void SortOnProperties_Ignores_Empty_Sort_Values() {
        var request = new NTItemsProviderRequest {
            StartIndex = 0,
            Sorts = ["", "   "]
        };

        request.SortOnProperties.Should().BeEmpty();
    }

    [Fact]
    public void SortOnProperties_Allows_Null_Sorts() {
        var request = new NTItemsProviderRequest {
            StartIndex = 0,
            Sorts = null!
        };

        request.SortOnProperties.Should().BeEmpty();
    }

    [Fact]
    public void SortOnProperties_Is_Ignored_By_Query_Object_Serializers() {
        var property = typeof(NTItemsProviderRequest).GetProperty(nameof(NTItemsProviderRequest.SortOnProperties));

        property.Should().NotBeNull();
        property!.GetCustomAttributes(inherit: false).Should().Contain(attribute => attribute is IgnoreDataMemberAttribute);
        property.GetCustomAttributes(inherit: false).Should().Contain(attribute => attribute is JsonIgnoreAttribute);
    }

    [Fact]
    public void ImplicitOperator_To_NTVirtualizeItemsProviderRequest_Copies_Values() {
        var request = new NTItemsProviderRequest {
            StartIndex = 4,
            Count = 12,
            Sorts = ["Name,Ascending", "Age,Descending"]
        };

        NTVirtualizeItemsProviderRequest<TestItem> virtualizeRequest = request;

        virtualizeRequest.StartIndex.Should().Be(4);
        virtualizeRequest.Count.Should().Be(12);
        virtualizeRequest.CancellationToken.CanBeCanceled.Should().BeFalse();
        virtualizeRequest.CancellationToken.IsCancellationRequested.Should().BeFalse();
        virtualizeRequest.SortOnProperties.Should().Equal([
            new KeyValuePair<string, SortDirection>("Name", SortDirection.Ascending),
            new KeyValuePair<string, SortDirection>("Age", SortDirection.Descending)
        ]);
    }

    [Fact]
    public void ImplicitOperator_From_NTVirtualizeItemsProviderRequest_Copies_Values() {
        var virtualizeRequest = new NTVirtualizeItemsProviderRequest<TestItem> {
            StartIndex = 3,
            Count = 9,
            CancellationToken = new CancellationToken(canceled: true),
            SortOnProperties = [
                new KeyValuePair<string, SortDirection>("Name", SortDirection.Ascending),
                new KeyValuePair<string, SortDirection>("Age", SortDirection.Descending)
            ]
        };

        NTItemsProviderRequest request = virtualizeRequest;

        request.StartIndex.Should().Be(3);
        request.Count.Should().Be(9);
        request.Sorts.Should().Equal("Name,Ascending", "Age,Descending");
    }

    [Fact]
    public void ImplicitOperator_To_NTDataGridItemsProviderRequest_Copies_Values() {
        var request = new NTItemsProviderRequest {
            StartIndex = 7,
            Count = 14,
            Sorts = ["Name,Ascending", "Age,Descending"]
        };

        NTDataGridItemsProviderRequest<TestItem> gridRequest = request;

        gridRequest.StartIndex.Should().Be(7);
        gridRequest.Count.Should().Be(14);
        gridRequest.CancellationToken.CanBeCanceled.Should().BeFalse();
        gridRequest.CancellationToken.IsCancellationRequested.Should().BeFalse();
        gridRequest.Sorts.Should().Equal([
            new NTSortDescriptor("Name", SortDirection.Ascending),
            new NTSortDescriptor("Age", SortDirection.Descending)
        ]);
    }

    [Fact]
    public void ImplicitOperator_From_NTDataGridItemsProviderRequest_Copies_Values() {
        var gridRequest = new NTDataGridItemsProviderRequest<TestItem> {
            StartIndex = 2,
            Count = 6,
            CancellationToken = new CancellationToken(canceled: true),
            Sorts = [
                new NTSortDescriptor("Name", SortDirection.Ascending),
                new NTSortDescriptor("Age", SortDirection.Descending)
            ]
        };

        NTItemsProviderRequest request = gridRequest;

        request.StartIndex.Should().Be(2);
        request.Count.Should().Be(6);
        request.Sorts.Should().Equal("Name,Ascending", "Age,Descending");
    }

    private sealed class TestItem;
}
