using CourtBook.Domain.Common;
using CourtBook.Domain.Entities;
using CourtBook.UnitTests.TestData;

namespace CourtBook.UnitTests.Domain.Entities;

public class CourtTests
{
    // ---------- Δημιουργία ----------

    [Fact]
    public void NewCourt_IsActiveByDefault()
    {
        // Act
        var court = DomainFactory.CreateCourt();

        // Assert
        Assert.True(court.IsActive);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void NewCourt_WithEmptyName_ThrowsDomainException(string name)
    {
        // Act & Assert
        Assert.Throws<DomainException>(() => DomainFactory.CreateCourt(name: name));
    }

    [Fact]
    public void NewCourt_WithTooLongName_ThrowsDomainException()
    {
        // Arrange
        var tooLongName = new string('a', Court.NameMaxLength + 1);

        // Act & Assert
        Assert.Throws<DomainException>(() => DomainFactory.CreateCourt(name: tooLongName));
    }

    // ---------- Deactivate / Activate ----------

    [Fact]
    public void Deactivate_SetsIsActiveToFalse()
    {
        // Arrange
        var court = DomainFactory.CreateCourt();

        // Act
        court.Deactivate();

        // Assert
        Assert.False(court.IsActive);
    }

    [Fact]
    public void Activate_AfterDeactivate_SetsIsActiveToTrue()
    {
        // Arrange
        var court = DomainFactory.CreateCourt();
        court.Deactivate();

        // Act
        court.Activate();

        // Assert
        Assert.True(court.IsActive);
    }

    [Fact]
    public void Deactivate_WhenAlreadyInactive_RemainsInactive()
    {
        // Arrange
        var court = DomainFactory.CreateCourt();
        court.Deactivate();

        // Act
        court.Deactivate();

        // Assert
        Assert.False(court.IsActive);
    }
}
