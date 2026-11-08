using CourtBook.Domain.Entities;
using CourtBook.Domain.Enums;
using CourtBook.Domain.ValueObjects;

namespace CourtBook.UnitTests.TestData;

public static class DomainFactory
{
    public static readonly DateTimeOffset Now = new(2026, 11, 10, 12, 0, 0, TimeSpan.FromHours(2));


    public static SportsCenter CreateSportsCenter(
        string name = "Piraeus Sports Center",
        string address = "Akti Miaouli 10",
        string city = "Piraeus",
        string? phoneNumber = null)
    {
        return new SportsCenter(name, address, city, phoneNumber);
    }

    public static Court CreateCourt(
        string name = "Padel 1",
        SportType sportType = SportType.Padel)
    {
        var center = CreateSportsCenter();
        return center.AddCourt(name, sportType);
    }

    public static Booking CreateBooking(
        DateTimeOffset? startTime = null,
        TimeSpan? duration = null,
        Money? price = null)
    {
        var start = startTime ?? Now.AddDays(2);
        var end = start + (duration ?? TimeSpan.FromMinutes(90));

        return new Booking(Guid.CreateVersion7(), Guid.CreateVersion7(), start, end, price ?? new Money(48m), Now);
    }

}