using CourtBook.Domain.Common;
using CourtBook.Domain.ValueObjects;

namespace CourtBook.UnitTests.Domain.ValueObjects;

public class DailyOpeningHoursTests
{
    private static readonly TimeOnly Eight = new(8, 0);
    private static readonly TimeOnly TwentyThree = new(23, 0);

    [Fact]
    public void Constructor_WithValidHours_CreatesOpeningHours()
    {
        // Act
        var hours = new DailyOpeningHours(DayOfWeek.Monday, Eight, TwentyThree);

        // Assert
        Assert.Equal(DayOfWeek.Monday, hours.DayOfWeek);
        Assert.Equal(Eight, hours.OpensAt);
        Assert.Equal(TwentyThree, hours.ClosesAt);
    }

    [Theory]
    [InlineData("10:00", "09:00")]   // κλείνει πριν ανοίξει
    [InlineData("10:00", "10:00")]   // κλείνει τη στιγμή που ανοίγει
    public void Constructor_WhenClosingIsNotAfterOpening_ThrowsDomainException(string opens, string closes)
    {
        Assert.Throws<DomainException>(() =>
            new DailyOpeningHours(DayOfWeek.Monday, TimeOnly.Parse(opens), TimeOnly.Parse(closes)));
    }

    [Theory]
    [InlineData("08:00", "09:30", true)]    // ξεκινά ακριβώς με το άνοιγμα
    [InlineData("21:30", "23:00", true)]    // τελειώνει ακριβώς με το κλείσιμο
    [InlineData("12:00", "13:00", true)]    // στη μέση της μέρας
    [InlineData("07:30", "09:00", false)]   // ξεκινά πριν ανοίξει
    [InlineData("22:30", "23:30", false)]   // τελειώνει αφού κλείσει
    [InlineData("10:00", "10:00", false)]   // μηδενική διάρκεια
    public void Covers_ReturnsExpectedResult(string start, string end, bool expected)
    {
        // Arrange
        var hours = new DailyOpeningHours(DayOfWeek.Monday, Eight, TwentyThree);

        // Act
        var result = hours.Covers(TimeOnly.Parse(start), TimeOnly.Parse(end));

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void TwoOpeningHours_WithSameValues_AreEqual()
    {
        // Arrange
        var first = new DailyOpeningHours(DayOfWeek.Monday, Eight, TwentyThree);
        var second = new DailyOpeningHours(DayOfWeek.Monday, Eight, TwentyThree);

        // Assert: δύο διαφορετικά αντικείμενα, αλλά ίσα (value object)
        Assert.Equal(first, second);
    }
}