using CourtBook.Domain.Common;
using CourtBook.Domain.Entities;
using CourtBook.Domain.Enums;
using CourtBook.UnitTests.TestData;

namespace CourtBook.UnitTests.Domain.Entities;

public class SportsCenterTests
{
    private const string ValidName = "Piraeus Sports Center";
    private const string ValidAddress = "Akti Miaouli 10";
    private const string ValidCity = "Piraeus";
    private const string ValidPhoneNumber = "2101234567";

    // ---------- Constructor ----------

    [Fact]
    public void Constructor_WithValidData_CreatesSportsCenter()
    {
        // Act
        var center = new SportsCenter(ValidName, ValidAddress, ValidCity, "2101234567");

        // Assert
        Assert.Equal(ValidName, center.Name);
        Assert.Equal(ValidAddress, center.Address);
        Assert.Equal(ValidCity, center.City);
        Assert.Equal(ValidPhoneNumber, center.PhoneNumber);
        Assert.NotEqual(Guid.Empty, center.Id);
    }

    [Fact]
    public void Constructor_TrimsWhitespace()
    {
        // Act
        var center = new SportsCenter("  Padel Club  ", "  Syggrou 5 ", " Athens ");

        // Assert
        Assert.Equal("Padel Club", center.Name);
        Assert.Equal("Syggrou 5", center.Address);
        Assert.Equal("Athens", center.City);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyName_ThrowsDomainException(string name)
    {
        // Act & Assert
        Assert.Throws<DomainException>(() => new SportsCenter(name, ValidAddress, ValidCity));
    }

    [Fact]
    public void Constructor_WithTooLongName_ThrowsDomainException()
    {
        // Arrange
        var tooLongName = new string('a', SportsCenter.NameMaxLength + 1);

        // Act & Assert
        Assert.Throws<DomainException>(() => new SportsCenter(tooLongName, ValidAddress, ValidCity));
    }

    [Fact]
    public void Constructor_WithNameAtMaxLength_CreatesSportsCenter()
    {
        // Arrange
        var maxLengthName = new string('a', SportsCenter.NameMaxLength);

        // Act
        var center = new SportsCenter(maxLengthName, ValidAddress, ValidCity);

        // Assert
        Assert.Equal(SportsCenter.NameMaxLength, center.Name.Length);
    }

    // ---------- Rename ----------

    [Fact]
    public void Rename_WithValidName_ChangesName()
    {
        // Arrange
        var center = DomainFactory.CreateSportsCenter();

        // Act
        center.Rename("New Name");

        // Assert
        Assert.Equal("New Name", center.Name);
    }

    [Fact]
    public void Rename_WithEmptyName_ThrowsAndKeepsOldName()
    {
        // Arrange
        var center = DomainFactory.CreateSportsCenter();

        // Act & Assert
        Assert.Throws<DomainException>(() => center.Rename(""));
        Assert.Equal(ValidName, center.Name);
    }

    // ---------- UpdatePhoneNumber ----------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdatePhoneNumber_WithEmptyValue_SetsPhoneNumberToNull(string? phoneNumber)
    {
        // Arrange
        var center = DomainFactory.CreateSportsCenter(phoneNumber: ValidPhoneNumber);

        // Act
        center.UpdatePhoneNumber(phoneNumber);

        // Assert
        Assert.Null(center.PhoneNumber);
    }



    // ---------- AddCourt ----------

    [Fact]
    public void AddCourt_WithValidData_AddsCourtToCenter()
    {
        // Arrange
        var center = DomainFactory.CreateSportsCenter();

        // Act
        var court = center.AddCourt("Padel 1", SportType.Padel);

        // Assert
        Assert.Single(center.Courts);
        Assert.Contains(court, center.Courts);
        Assert.Equal("Padel 1", court.Name);
        Assert.Equal(SportType.Padel, court.SportType);
        Assert.Equal(center.Id, court.SportsCenterId);
        Assert.True(court.IsActive);
    }

    [Fact]
    public void AddCourt_WithDuplicateName_ThrowsDomainException()
    {
        // Arrange
        var center = DomainFactory.CreateSportsCenter();
        center.AddCourt("Padel 1", SportType.Padel);

        // Act & Assert
        Assert.Throws<DomainException>(() => center.AddCourt("  PADEL 1 ", SportType.Padel));
        Assert.Single(center.Courts);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(99)]
    public void AddCourt_WithInvalidSportType_ThrowsDomainException(int invalidSportType)
    {
        // Arrange
        var center = DomainFactory.CreateSportsCenter();

        // Act & Assert
        Assert.Throws<DomainException>(() => center.AddCourt("Court 1", (SportType)invalidSportType));
        Assert.Empty(center.Courts);
    }



}