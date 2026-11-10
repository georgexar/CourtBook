using System.Net;
using System.Net.Http.Json;
using CourtBook.Application.Auth;
using CourtBook.Application.SportsCenters;
using Xunit;

namespace CourtBook.IntegrationTests.Api;

[Collection(ApiCollection.Name)]
public sealed class AuthEndpointsTests(CourtBookApiFactory factory)
{
    [Fact]
    public async Task Register_WithValidData_ReturnsTokenWithPlayerRole()
    {
        var client = factory.CreateClient();

        var auth = await client.RegisterPlayerAsync();

        Assert.False(string.IsNullOrWhiteSpace(auth.AccessToken));
        Assert.Contains("Player", auth.Roles);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var client = factory.CreateClient();
        var auth = await client.RegisterPlayerAsync();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login", new LoginRequest(auth.Email, "WrongPass123"), ApiTestHelpers.Json);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_Returns401()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/bookings/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PublicEndpoint_WithoutToken_Returns200()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/centers");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Player_CreatingSportsCenter_Returns403()
    {
        var player = await factory.CreatePlayerClientAsync();

        var response = await player.PostAsJsonAsync(
            "/api/centers",
            new CreateSportsCenterRequest("Not allowed", "Street 1", "Athens", null),
            ApiTestHelpers.Json);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}