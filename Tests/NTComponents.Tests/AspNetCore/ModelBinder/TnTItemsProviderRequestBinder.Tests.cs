using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;
using NSubstitute;
using NTComponents.AspNetCore.ModelBinder;
using NTComponents.Virtualization;

namespace NTComponents.Tests.AspNetCore.ModelBinder;

public class TnTItemsProviderRequestBinder_Tests {

    [Fact]
    public async Task BindModelAsync_Parses_Query_With_Count_And_Sort() {
        // Arrange
        var query = new QueryCollection(new Dictionary<string, StringValues> {
            [nameof(TnTItemsProviderRequest.StartIndex)] = "5",
            [nameof(TnTItemsProviderRequest.Count)] = "10",
            [nameof(TnTItemsProviderRequest.SortOnProperties)] = "[Name,Ascending],[Age,Descending]"
        });

        var httpContext = Substitute.For<HttpContext>();
        var request = Substitute.For<HttpRequest>();
        request.Query.Returns(query);
        httpContext.Request.Returns(request);

        var provider = new EmptyModelMetadataProvider();
        var metadata = provider.GetMetadataForType(typeof(TnTItemsProviderRequest));

        var bindingContext = Substitute.For<ModelBindingContext>();
        bindingContext.ModelMetadata = metadata;
        bindingContext.BindingSource = BindingSource.Query;
        bindingContext.HttpContext.Returns(httpContext);

        var binder = new TnTItemsProviderRequestBinder();

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        bindingContext.Result.IsModelSet.Should().BeTrue();
        var model = bindingContext.Result.Model.Should().BeAssignableTo<TnTItemsProviderRequest>().Subject;
        model.StartIndex.Should().Be(5);
        model.Count.Should().Be(10);
        model.SortOnProperties.Should().HaveCount(2);

        var list = model.SortOnProperties.ToList();
        list[0].Key.Should().Be("Name");
        list[0].Value.Should().Be(SortDirection.Ascending);
        list[1].Key.Should().Be("Age");
        list[1].Value.Should().Be(SortDirection.Descending);
    }

    [Fact]
    public async Task BindModelAsync_Parses_Query_Without_Count() {
        // Arrange
        var query = new QueryCollection(new Dictionary<string, StringValues> {
            [nameof(TnTItemsProviderRequest.StartIndex)] = "2",
            [nameof(TnTItemsProviderRequest.SortOnProperties)] = "[Prop,Ascending]"
        });

        var httpContext = Substitute.For<HttpContext>();
        var request = Substitute.For<HttpRequest>();
        request.Query.Returns(query);
        httpContext.Request.Returns(request);

        var provider = new EmptyModelMetadataProvider();
        var metadata = provider.GetMetadataForType(typeof(TnTItemsProviderRequest));

        var bindingContext = Substitute.For<ModelBindingContext>();
        bindingContext.ModelMetadata = metadata;
        bindingContext.BindingSource = BindingSource.Query;
        bindingContext.HttpContext.Returns(httpContext);

        var binder = new TnTItemsProviderRequestBinder();

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        bindingContext.Result.IsModelSet.Should().BeTrue();
        var model = bindingContext.Result.Model.Should().BeAssignableTo<TnTItemsProviderRequest>().Subject;
        model.StartIndex.Should().Be(2);
        model.Count.Should().BeNull();
        model.SortOnProperties.Should().HaveCount(1);
        model.SortOnProperties.First().Key.Should().Be("Prop");
    }

    [Fact]
    public async Task BindModelAsync_Fails_When_StartIndex_Missing() {
        // Arrange
        var query = new QueryCollection(new Dictionary<string, StringValues> {
            [nameof(TnTItemsProviderRequest.Count)] = "5",
            [nameof(TnTItemsProviderRequest.SortOnProperties)] = "[X,Ascending]"
        });

        var httpContext = Substitute.For<HttpContext>();
        var request = Substitute.For<HttpRequest>();
        request.Query.Returns(query);
        httpContext.Request.Returns(request);

        var provider = new EmptyModelMetadataProvider();
        var metadata = provider.GetMetadataForType(typeof(TnTItemsProviderRequest));

        var bindingContext = Substitute.For<ModelBindingContext>();
        bindingContext.ModelMetadata = metadata;
        bindingContext.BindingSource = BindingSource.Query;
        bindingContext.HttpContext.Returns(httpContext);

        var binder = new TnTItemsProviderRequestBinder();

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        bindingContext.Result.IsModelSet.Should().BeFalse();
    }

    [Fact]
    public async Task BindModelAsync_Fails_When_StartIndex_Invalid() {
        // Arrange
        var query = new QueryCollection(new Dictionary<string, StringValues> {
            [nameof(TnTItemsProviderRequest.StartIndex)] = "not-an-int",
            [nameof(TnTItemsProviderRequest.Count)] = "5"
        });

        var httpContext = Substitute.For<HttpContext>();
        var request = Substitute.For<HttpRequest>();
        request.Query.Returns(query);
        httpContext.Request.Returns(request);

        var provider = new EmptyModelMetadataProvider();
        var metadata = provider.GetMetadataForType(typeof(TnTItemsProviderRequest));

        var bindingContext = Substitute.For<ModelBindingContext>();
        bindingContext.ModelMetadata = metadata;
        bindingContext.BindingSource = BindingSource.Query;
        bindingContext.HttpContext.Returns(httpContext);

        var binder = new TnTItemsProviderRequestBinder();

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        bindingContext.Result.IsModelSet.Should().BeFalse();
    }

    [Fact]
    public async Task BindModelAsync_Parses_Count_Invalid_As_Null() {
        // Arrange
        var query = new QueryCollection(new Dictionary<string, StringValues> {
            [nameof(TnTItemsProviderRequest.StartIndex)] = "1",
            [nameof(TnTItemsProviderRequest.Count)] = "abc",
            [nameof(TnTItemsProviderRequest.SortOnProperties)] = "[A,Ascending]"
        });

        var httpContext = Substitute.For<HttpContext>();
        var request = Substitute.For<HttpRequest>();
        request.Query.Returns(query);
        httpContext.Request.Returns(request);

        var provider = new EmptyModelMetadataProvider();
        var metadata = provider.GetMetadataForType(typeof(TnTItemsProviderRequest));

        var bindingContext = Substitute.For<ModelBindingContext>();
        bindingContext.ModelMetadata = metadata;
        bindingContext.BindingSource = BindingSource.Query;
        bindingContext.HttpContext.Returns(httpContext);

        var binder = new TnTItemsProviderRequestBinder();

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        bindingContext.Result.IsModelSet.Should().BeTrue();
        var model = bindingContext.Result.Model.Should().BeAssignableTo<TnTItemsProviderRequest>().Subject;
        model.StartIndex.Should().Be(1);
        model.Count.Should().BeNull();
    }

    [Fact]
    public async Task BindModelAsync_Parses_Multiple_Sort_QueryValues() {
        // Arrange - two separate query values for SortOnProperties
        var query = new QueryCollection(new Dictionary<string, StringValues> {
            [nameof(TnTItemsProviderRequest.StartIndex)] = "0",
            [nameof(TnTItemsProviderRequest.SortOnProperties)] = new StringValues(new[] { "[A,Ascending]", "[B,Descending]" })
        });

        var httpContext = Substitute.For<HttpContext>();
        var request = Substitute.For<HttpRequest>();
        request.Query.Returns(query);
        httpContext.Request.Returns(request);

        var provider = new EmptyModelMetadataProvider();
        var metadata = provider.GetMetadataForType(typeof(TnTItemsProviderRequest));

        var bindingContext = Substitute.For<ModelBindingContext>();
        bindingContext.ModelMetadata = metadata;
        bindingContext.BindingSource = BindingSource.Query;
        bindingContext.HttpContext.Returns(httpContext);

        var binder = new TnTItemsProviderRequestBinder();

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        bindingContext.Result.IsModelSet.Should().BeTrue();
        var model = bindingContext.Result.Model.Should().BeAssignableTo<TnTItemsProviderRequest>().Subject;
        model.SortOnProperties.Should().HaveCount(2);
        var list = model.SortOnProperties.ToList();
        list[0].Key.Should().Be("A");
        list[1].Key.Should().Be("B");
    }

    [Fact]
    public async Task BindModelAsync_Parses_No_Sort_Values_As_Empty_List() {
        // Arrange - StartIndex present but no SortOnProperties values
        var query = new QueryCollection(new Dictionary<string, StringValues> {
            [nameof(TnTItemsProviderRequest.StartIndex)] = "3"
        });

        var httpContext = Substitute.For<HttpContext>();
        var request = Substitute.For<HttpRequest>();
        request.Query.Returns(query);
        httpContext.Request.Returns(request);

        var provider = new EmptyModelMetadataProvider();
        var metadata = provider.GetMetadataForType(typeof(TnTItemsProviderRequest));

        var bindingContext = Substitute.For<ModelBindingContext>();
        bindingContext.ModelMetadata = metadata;
        bindingContext.BindingSource = BindingSource.Query;
        bindingContext.HttpContext.Returns(httpContext);

        var binder = new TnTItemsProviderRequestBinder();

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        bindingContext.Result.IsModelSet.Should().BeTrue();
        var model = bindingContext.Result.Model.Should().BeAssignableTo<TnTItemsProviderRequest>().Subject;
        model.SortOnProperties.Should().BeEmpty();
    }
}
