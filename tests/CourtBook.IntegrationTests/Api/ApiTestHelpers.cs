using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CourtBook.Application.Auth;
using CourtBook.Application.Common;
using CourtBook.Application.SportsCenters;
using CourtBook.Domain.Enums;

namespace CourtBook.IntegrationTests.Api;

internal static class ApiTestHelpers
{
    // Ίδιες ρυθμίσεις JSON με το API (camelCase + enums ως κείμενο)
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public const string PlayerPassword = "Player2026Pass";

    public static void UseToken(this HttpClient client, string token) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    public static async Task<AuthResponse> RegisterPlayerAsync(this HttpClient client)
    {
        var request = new RegisterRequest($"player-{Guid.NewGuid():N}@test.local", PlayerPassword, "Test Player");

        var response = await client.PostAsJsonAsync("/api/auth/register", request, Json);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<AuthResponse>(Json))!;
    }

    public static async Task<HttpClient> CreatePlayerClientAsync(this CourtBookApiFactory factory)
    {
        var client = factory.CreateClient();
        var auth = await client.RegisterPlayerAsync();
        client.UseToken(auth.AccessToken);
        return client;
    }

    public static async Task<HttpClient> CreateAdminClientAsync(this CourtBookApiFactory factory)
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(CourtBookApiFactory.AdminEmail, CourtBookApiFactory.AdminPassword),
            Json);
        response.EnsureSuccessStatusCode();

        var auth = (await response.Content.ReadFromJsonAsync<AuthResponse>(Json))!;
        client.UseToken(auth.AccessToken);
        return client;
    }

    /// <summary>Φτιάχνει (ως admin) κέντρο ανοιχτό κάθε μέρα 08:00–23:00 με ένα γήπεδο.</summary>
    public static async Task<Guid> CreateOpenCourtAsync(this CourtBookApiFactory factory)
    {
        using var admin = await factory.CreateAdminClientAsync();

        var centerResponse = await admin.PostAsJsonAsync(
            "/api/centers",
            new CreateSportsCenterRequest($"Center {Guid.NewGuid():N}", "Test Street 1", "Athens", null),
            Json);
        var centerId = await ReadIdAsync(centerResponse);

        var days = Enum.GetValues<DayOfWeek>()
            .Select(day => new OpeningHoursDto(day, new TimeOnly(8, 0), new TimeOnly(23, 0)))
            .ToList();

        var hoursResponse = await admin.PutAsJsonAsync(
            $"/api/centers/{centerId}/opening-hours", new SetOpeningHoursRequest(days), Json);
        hoursResponse.EnsureSuccessStatusCode();

        var courtResponse = await admin.PostAsJsonAsync(
            $"/api/centers/{centerId}/courts", new AddCourtRequest("Court 1", SportType.Padel), Json);

        return await ReadIdAsync(courtResponse);
    }

    /// <summary>Διαβάζει το { "id": ... } που επιστρέφουν τα POST μας.</summary>
    public static async Task<Guid> ReadIdAsync(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<IdResponse>(Json);
        return body!.Id;
    }

    /// <summary>Μια μέρα της επόμενης εβδομάδας, στις hour:00 ώρα Ελλάδας.</summary>
    public static DateTimeOffset NextWeekAt(int hour)
    {
        var timeZone = AppTimeZone.Default;
        var localNow = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, timeZone);
        var local = DateOnly.FromDateTime(localNow.DateTime).AddDays(7).ToDateTime(new TimeOnly(hour, 0));

        return new DateTimeOffset(local, timeZone.GetUtcOffset(local));
    }

    private sealed record IdResponse(Guid Id);
}