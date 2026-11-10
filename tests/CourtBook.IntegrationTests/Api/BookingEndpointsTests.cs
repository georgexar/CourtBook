using System.Net;
using System.Net.Http.Json;
using CourtBook.Application.Bookings;
using Xunit;

namespace CourtBook.IntegrationTests.Api;

[Collection(ApiCollection.Name)]
public sealed class BookingEndpointsTests(CourtBookApiFactory factory)
{
    [Fact]
    public async Task CreateBooking_AsPlayer_Returns201AndBookingCanBeRead()
    {
        var courtId = await factory.CreateOpenCourtAsync();
        var player = factory.CreateClient();
        var auth = await player.RegisterPlayerAsync();
        player.UseToken(auth.AccessToken);

        var response = await player.PostAsJsonAsync("/api/bookings", Request(courtId), ApiTestHelpers.Json);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var bookingId = await ApiTestHelpers.ReadIdAsync(response);
        var booking = await player.GetFromJsonAsync<BookingResponse>($"/api/bookings/{bookingId}", ApiTestHelpers.Json);

        Assert.NotNull(booking);
        Assert.Equal(courtId, booking.CourtId);
        Assert.Equal(auth.UserId, booking.UserId);   // ο χρήστης βγήκε από το JWT, όχι από το body!
    }

    [Fact]
    public async Task CreateBooking_ForAlreadyBookedSlot_Returns400WithRuleViolation()
    {
        var courtId = await factory.CreateOpenCourtAsync();
        var firstPlayer = await factory.CreatePlayerClientAsync();
        var secondPlayer = await factory.CreatePlayerClientAsync();

        var first = await firstPlayer.PostAsJsonAsync("/api/bookings", Request(courtId), ApiTestHelpers.Json);
        var second = await secondPlayer.PostAsJsonAsync("/api/bookings", Request(courtId), ApiTestHelpers.Json);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
        Assert.Contains("Booking rules violated", await second.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task GetBooking_OfAnotherPlayer_Returns404()
    {
        var courtId = await factory.CreateOpenCourtAsync();
        var owner = await factory.CreatePlayerClientAsync();
        var stranger = await factory.CreatePlayerClientAsync();

        var created = await owner.PostAsJsonAsync("/api/bookings", Request(courtId), ApiTestHelpers.Json);
        var bookingId = await ApiTestHelpers.ReadIdAsync(created);

        var response = await stranger.GetAsync($"/api/bookings/{bookingId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static CreateBookingRequest Request(Guid courtId)
    {
        var start = ApiTestHelpers.NextWeekAt(19);
        return new CreateBookingRequest(courtId, start, start.AddMinutes(90));
    }
}