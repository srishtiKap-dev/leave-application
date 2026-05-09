using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace LeavePortal.IntegrationTests;

public sealed class ApiSmokeTests
{
    [Fact]
    public async Task OpenApi_endpoint_is_available()
    {
        await using var app = new TestingFactory();
        var client = app.CreateClient();
        var response = await client.GetAsync("/openapi/v1.json");
        Assert.True(response.StatusCode == HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
    }
}

file sealed class TestingFactory : WebApplicationFactory<Program>
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        return base.CreateHost(builder);
    }
}
