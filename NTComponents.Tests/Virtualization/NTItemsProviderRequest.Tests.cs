using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
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
}
