using NodaTime;

namespace FitnessCenter.Modules.Booking.Domain;

internal enum BookOutcome
{
    Booked,
    AlreadyBooked,
    Full,
    Started,
    NotOpenYet,
}

internal enum CancelOutcome
{
    Cancelled,
    NotBooked,
    CancellationClosed,
}

/// <summary>
/// One scheduled class, e.g. Spinning on Tuesday at 18:00, with its bookings. All booking
/// rules (D-039) live here, so they can be tested without a database.
/// </summary>
internal sealed class ClassSession
{
    public const int InstructorNameMaxLength = 100;

    private readonly List<ClassBooking> _bookings = [];

    private ClassSession()
    {
        InstructorName = string.Empty;
    }

    public ClassSessionId Id { get; private set; }

    public ClassTypeId ClassTypeId { get; private set; }

    /// <summary>Plain text until there is a Staff module (D-039).</summary>
    public string InstructorName { get; private set; }

    public Instant StartsAt { get; private set; }

    public Instant EndsAt { get; private set; }

    public int Capacity { get; private set; }

    public IReadOnlyList<ClassBooking> Bookings => _bookings;

    public int SpotsLeft => Capacity - _bookings.Count(booking => booking.IsActive);

    /// <summary>The last moment a member can cancel (D-039).</summary>
    public Instant CancelUntil => StartsAt - BookingPolicy.CancellationCutoff;

    public static ClassSession Schedule(
        ClassType type, string instructorName, Instant startsAt, Duration length, int capacity)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "A class needs at least one spot.");
        }

        if (length <= Duration.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(length), length, "A class must have a length.");
        }

        return new ClassSession
        {
            Id = ClassSessionId.New(),
            ClassTypeId = type.Id,
            InstructorName = instructorName,
            StartsAt = startsAt,
            EndsAt = startsAt + length,
            Capacity = capacity,
        };
    }

    public bool IsBookedBy(Guid accountId) => ActiveBookingOf(accountId) is not null;

    public BookOutcome Book(Guid accountId, Instant now)
    {
        if (now >= StartsAt)
        {
            return BookOutcome.Started;
        }

        if (StartsAt - now > BookingPolicy.BookingWindow)
        {
            return BookOutcome.NotOpenYet;
        }

        if (IsBookedBy(accountId))
        {
            return BookOutcome.AlreadyBooked;
        }

        if (SpotsLeft <= 0)
        {
            return BookOutcome.Full;
        }

        _bookings.Add(ClassBooking.Create(Id, accountId, now));
        return BookOutcome.Booked;
    }

    public CancelOutcome Cancel(Guid accountId, Instant now)
    {
        var booking = ActiveBookingOf(accountId);
        if (booking is null)
        {
            return CancelOutcome.NotBooked;
        }

        if (now > CancelUntil)
        {
            return CancelOutcome.CancellationClosed;
        }

        booking.Cancel(now);
        return CancelOutcome.Cancelled;
    }

    private ClassBooking? ActiveBookingOf(Guid accountId) =>
        _bookings.SingleOrDefault(booking => booking.IsActive && booking.AccountId == accountId);
}
