using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NTComponents.Virtualization;
using System.Net.Http.Json;

namespace NTComponents.IntegrationTests.Virtualization;

public sealed class NTItemsProviderRequest_IntegrationTests {
    [Fact]
    public async Task WithQueryValues_BindsControllerParameter() {
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddControllers().AddApplicationPart(typeof(NTItemsProviderRequestTestController).Assembly);
        await using var app = builder.Build();
        app.MapControllers();
        await app.StartAsync(cancellationToken);

        var response = await app.GetTestClient().GetAsync("/test/nt-items-provider-request?StartIndex=5&Count=10&Sorts=Name%2CAscending&Sorts=Age%2CDescending", cancellationToken);

        response.EnsureSuccessStatusCode();
        var request = await response.Content.ReadFromJsonAsync<NTItemsProviderRequest>(cancellationToken);
        request.Should().NotBeNull();
        request!.StartIndex.Should().Be(5);
        request.Count.Should().Be(10);
        request.Sorts.Should().Equal("Name,Ascending", "Age,Descending");
    }
}

[ApiController]
[Route("test/nt-items-provider-request")]
public sealed class NTItemsProviderRequestTestController : ControllerBase {
    [HttpGet]
    public NTItemsProviderRequest Get([FromQuery] NTItemsProviderRequest request) => request;
}
