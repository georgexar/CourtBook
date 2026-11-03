using System;
using System.Collections.Generic;
using System.Text;

namespace CourtBook.Domain.Enums
{
    public enum BookingStatus
    {
        Pending = 1,
        Confirmed = 2,
        Completed = 3,
        Cancelled = 4,
        NoShow = 5
    }
}
