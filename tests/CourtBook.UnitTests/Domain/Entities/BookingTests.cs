using CourtBook.Domain.Common;
using CourtBook.Domain.Entities;
using CourtBook.Domain.Enums;
using CourtBook.UnitTests.TestData;

namespace CourtBook.UnitTests.Domain.Entities;

public class BookingTests
{
    private static readonly DateTimeOffset Now = DomainFactory.Now;

    // ---------- Δημιουργία ----------

    [Fact]
    public void NewBooking_WithValidData_IsPending()
    {
        // Act
        var booking = DomainFactory.CreateBooking(duration: TimeSpan.FromMinutes(90));

        // Assert
        Assert.Equal(BookingStatus.Pending, booking.Status);
        Assert.Equal(TimeSpan.FromMinutes(90), booking.Duration);
        Assert.True(booking.IsActive);
    }

    [Fact]
    public void NewBooking_WithEndBeforeStart_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() =>
            DomainFactory.CreateBooking(duration: TimeSpan.FromMinutes(-30)));
    }

    [Fact]
    public void NewBooking_WithZeroDuration_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() =>
            DomainFactory.CreateBooking(duration: TimeSpan.Zero));
    }

    [Fact]
    public void NewBooking_InThePast_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() =>
            DomainFactory.CreateBooking(startTime: Now.AddHours(-1)));
    }

    [Fact]
    public void NewBooking_StartingExactlyNow_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() =>
            DomainFactory.CreateBooking(startTime: Now));
    }

    // ---------- Confirm ----------

    [Fact]
    public void Confirm_WhenPending_SetsStatusToConfirmed()
    {
        // Arrange
        var booking = DomainFactory.CreateBooking();

        // Act
        booking.Confirm();

        // Assert
        Assert.Equal(BookingStatus.Confirmed, booking.Status);
    }

    [Fact]
    public void Confirm_WhenAlreadyConfirmed_ThrowsDomainException()
    {
        // Arrange
        var booking = DomainFactory.CreateBooking();
        booking.Confirm();

        // Act & Assert
        Assert.Throws<DomainException>(() => booking.Confirm());
    }

    // ---------- Cancel ----------

    [Fact]
    public void Cancel_MoreThan24HoursBefore_IsNotLateCancellation()
    {
        // Arrange
        var booking = DomainFactory.CreateBooking(startTime: Now.AddHours(48));
        booking.Confirm();

        // Act
        booking.Cancel(Now);

        // Assert
        Assert.Equal(BookingStatus.Cancelled, booking.Status);
        Assert.Equal(Now, booking.CancelledAt);
        Assert.False(booking.IsLateCancellation);
        Assert.False(booking.IsActive);
    }

    [Fact]
    public void Cancel_LessThan24HoursBefore_IsLateCancellation()
    {
        // Arrange
        var booking = DomainFactory.CreateBooking(startTime: Now.AddHours(5));

        // Act
        booking.Cancel(Now);

        // Assert
        Assert.True(booking.IsLateCancellation);
    }

    [Fact]
    public void Cancel_AfterBookingStarted_ThrowsDomainException()
    {
        // Arrange
        var booking = DomainFactory.CreateBooking(startTime: Now.AddHours(2));
        var afterStart = Now.AddHours(3);

        // Act & Assert
        Assert.Throws<DomainException>(() => booking.Cancel(afterStart));
    }

    [Fact]
    public void Cancel_WhenAlreadyCancelled_ThrowsDomainException()
    {
        // Arrange
        var booking = DomainFactory.CreateBooking();
        booking.Cancel(Now);

        // Act & Assert
        Assert.Throws<DomainException>(() => booking.Cancel(Now));
    }

    // ---------- OverlapsWith ----------
    // Υπάρχουσα κράτηση: start έως start + 90 λεπτά.
    // Οι τιμές είναι λεπτά σε σχέση με την έναρξη της υπάρχουσας κράτησης.

    [Theory]
    [InlineData(-60, 0, false)]    // τελειώνει ακριβώς όταν ξεκινά η υπάρχουσα
    [InlineData(90, 150, false)]   // ξεκινά ακριβώς όταν τελειώνει η υπάρχουσα
    [InlineData(120, 180, false)]  // εντελώς μετά
    [InlineData(-30, 30, true)]    // επικαλύπτει την αρχή
    [InlineData(60, 120, true)]    // επικαλύπτει το τέλος
    [InlineData(30, 60, true)]     // εντελώς μέσα
    [InlineData(-30, 120, true)]   // την περικλείει
    [InlineData(0, 90, true)]      // ακριβώς ίδια ώρα
    public void OverlapsWith_ReturnsExpectedResult(int startOffsetMinutes, int endOffsetMinutes, bool expected)
    {
        // Arrange
        var booking = DomainFactory.CreateBooking(duration: TimeSpan.FromMinutes(90));
        var otherStart = booking.StartTime.AddMinutes(startOffsetMinutes);
        var otherEnd = booking.StartTime.AddMinutes(endOffsetMinutes);

        // Act
        var overlaps = booking.OverlapsWith(otherStart, otherEnd);

        // Assert
        Assert.Equal(expected, overlaps);
    }

    // ---------- Complete ----------

    [Fact]
    public void Complete_AfterEnd_SetsStatusToCompleted()
    {
        // Arrange
        var booking = DomainFactory.CreateBooking();
        booking.Confirm();
        var justAfterEnd = booking.EndTime.AddMinutes(1);

        // Act
        booking.Complete(justAfterEnd);

        // Assert
        Assert.Equal(BookingStatus.Completed, booking.Status);
        Assert.False(booking.IsActive);
    }

    [Fact]
    public void Complete_BeforeEnd_ThrowsDomainException()
    {
        // Arrange
        var booking = DomainFactory.CreateBooking();
        booking.Confirm();
        var justBeforeEnd = booking.EndTime.AddMinutes(-1);

        // Act & Assert
        Assert.Throws<DomainException>(() => booking.Complete(justBeforeEnd));
        Assert.Equal(BookingStatus.Confirmed, booking.Status);
    }

    [Fact]
    public void Complete_WhenPending_ThrowsDomainException()
    {
        // Arrange
        var booking = DomainFactory.CreateBooking();   // χωρίς Confirm()
        var afterEnd = booking.EndTime.AddMinutes(1);

        // Act & Assert
        Assert.Throws<DomainException>(() => booking.Complete(afterEnd));
    }

    // ---------- MarkAsNoShow ----------

    [Fact]
    public void MarkAsNoShow_AfterStart_SetsStatusToNoShow()
    {
        // Arrange
        var booking = DomainFactory.CreateBooking();
        booking.Confirm();
        var fifteenMinutesAfterStart = booking.StartTime.AddMinutes(15);

        // Act
        booking.MarkAsNoShow(fifteenMinutesAfterStart);

        // Assert
        Assert.Equal(BookingStatus.NoShow, booking.Status);
        Assert.False(booking.IsActive);
    }

    [Fact]
    public void MarkAsNoShow_BeforeStart_ThrowsDomainException()
    {
        // Arrange
        var booking = DomainFactory.CreateBooking();
        booking.Confirm();
        var beforeStart = booking.StartTime.AddMinutes(-10);

        // Act & Assert
        Assert.Throws<DomainException>(() => booking.MarkAsNoShow(beforeStart));
    }


}