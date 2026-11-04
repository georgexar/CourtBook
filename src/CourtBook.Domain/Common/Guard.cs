namespace CourtBook.Domain.Common;

public static class Guard
{
    public static string Required(string? value, int maxLength, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException($"{fieldName} is required.");

        value = value.Trim();

        if (value.Length > maxLength)
            throw new DomainException($"{fieldName} must be at most {maxLength} characters.");

        return value;
    }

    public static TEnum ValidEnum<TEnum>(TEnum value, string fieldName)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
            throw new DomainException($"{fieldName} has an invalid value: {value}.");

        return value;
    }


    public static Guid NotEmpty(Guid value, string fieldName)
    {
        if (value == Guid.Empty)
            throw new DomainException($"{fieldName} is required.");

        return value;
    }


}