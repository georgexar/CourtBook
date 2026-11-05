using CourtBook.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;
using CourtBook.Domain.Common;
using CourtBook.Domain.Enums;
using CourtBook.Domain.ValueObjects;

namespace CourtBook.Domain.Entities;

public class SportsCenter : BaseEntity
{
    public const int NameMaxLength = 100;
    public const int AddressMaxLength = 200;
    public const int CityMaxLength = 100;
    public const int PhoneNumberMaxLength = 20;

    public string Name { get; private set; } = null!;
    public string Address { get; private set; } = null!;
    public string City { get; private set; } = null!;
    public string? PhoneNumber { get; private set; }

    private readonly List<Court> _courts = [];
    public IReadOnlyCollection<Court> Courts => _courts.AsReadOnly();
    private readonly List<DailyOpeningHours> _openingHours = [];
    public IReadOnlyCollection<DailyOpeningHours> OpeningHours => _openingHours.AsReadOnly();

    // Χρησιμοποιείται μόνο από το EF Core όταν διαβάζει από τη βάση
    private SportsCenter() { }

    public SportsCenter(string name, string address, string city, string? phoneNumber = null)
    {
        Rename(name);
        UpdateLocation(address, city);
        UpdatePhoneNumber(phoneNumber);
    }

    public void Rename(string name)
    {
        Name = Guard.Required(name, NameMaxLength, nameof(Name));
    }

    public void UpdateLocation(string address, string city)
    {
        Address = Guard.Required(address, AddressMaxLength, nameof(Address));
        City = Guard.Required(city, CityMaxLength, nameof(City));
    }

    public void UpdatePhoneNumber(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            PhoneNumber = null;
            return;
        }

        phoneNumber = phoneNumber.Trim();

        if (phoneNumber.Length > PhoneNumberMaxLength)
            throw new DomainException($"{nameof(PhoneNumber)} must be at most {PhoneNumberMaxLength} characters.");

        PhoneNumber = phoneNumber;
    }

    public Court AddCourt(string name, SportType sportType)
    {
        var court = new Court(Id, name, sportType);

        var nameExists = _courts.Any(c =>
            string.Equals(c.Name, court.Name, StringComparison.OrdinalIgnoreCase));

        if (nameExists)
            throw new DomainException($"A court named '{court.Name}' already exists in this sports center.");

        _courts.Add(court);
        return court;
    }
    public void SetOpeningHours(IEnumerable<DailyOpeningHours> openingHours)
    {
        var newSchedule = openingHours.ToList();

        var duplicateDay = newSchedule
            .GroupBy(h => h.DayOfWeek)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicateDay is not null)
            throw new DomainException($"Opening hours for {duplicateDay.Key} are defined more than once.");

        _openingHours.Clear();
        _openingHours.AddRange(newSchedule);
    }

    public DailyOpeningHours? GetOpeningHoursFor(DayOfWeek dayOfWeek)
    {
        return _openingHours.FirstOrDefault(h => h.DayOfWeek == dayOfWeek);
    }

}

