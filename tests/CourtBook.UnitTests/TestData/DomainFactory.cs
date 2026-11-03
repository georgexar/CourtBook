using CourtBook.Domain.Entities;
using CourtBook.Domain.Enums;

namespace CourtBook.UnitTests.TestData;

public static class DomainFactory
{
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
}