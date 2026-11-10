using System.Net;
using Xunit;

namespace CourtBook.IntegrationTests.Api;

[Collection(ApiCollection.Name)]
public sealed class HealthEndpointsTests(CourtBookApiFactory factory)
{
    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task HealthEndpoint_WithoutToken_ReturnsHealthy(string url)
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }
}