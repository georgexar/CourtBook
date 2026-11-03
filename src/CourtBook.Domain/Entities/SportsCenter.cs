using CourtBook.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

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
        Name = Required(name, NameMaxLength, nameof(Name));
    }

    public void UpdateLocation(string address, string city)
    {
        Address = Required(address, AddressMaxLength, nameof(Address));
        City = Required(city, CityMaxLength, nameof(City));
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

    private static string Required(string value, int maxLength, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException($"{fieldName} is required.");

        value = value.Trim();

        if (value.Length > maxLength)
            throw new DomainException($"{fieldName} must be at most {maxLength} characters.");

        return value;
    }
}

