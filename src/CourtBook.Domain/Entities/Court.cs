using CourtBook.Domain.Common;
using CourtBook.Domain.Enums;

namespace CourtBook.Domain.Entities;

public class Court : BaseEntity
{
    public const int NameMaxLength = 50;

    public Guid SportsCenterId { get; private set; }
    public string Name { get; private set; } = null!;
    public SportType SportType { get; private set; }
    public bool IsActive { get; private set; }

    // Για το EF Core
    private Court() { }

    // internal: μόνο κώδικας μέσα στο Domain μπορεί να φτιάξει γήπεδο
    internal Court(Guid sportsCenterId, string name, SportType sportType)
    {
        SportsCenterId = sportsCenterId;
        Name = Guard.Required(name, NameMaxLength, nameof(Name));
        SportType = Guard.ValidEnum(sportType, nameof(SportType));
        IsActive = true;
    }

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;
}