using FitnessCenter.SharedKernel;
using NodaTime;

namespace FitnessCenter.Modules.Booking.Domain;

internal enum BookOutcome
{
    Booked,
    AlreadyBooked,
    Full,
    Started,
    NotOpenYet,

    /// <summary>
    /// The class starts after the last day of the member's access: their cancellation takes
    /// effect before it (D-048, D-055 option A).
    /// </summary>
    AfterMembershipEnds,
}

internal enum JoinWaitlistOutcome
{
    Joined,
    AlreadyWaitlisted,
    AlreadyBooked,

    /// <summary>The class has a free spot, so the member books it instead.</summary>
    NotFull,
    Started,
    NotOpenYet,

    /// <inheritdoc cref="BookOutcome.AfterMembershipEnds"/>
    AfterMembershipEnds,
}

internal enum LeaveWaitlistOutcome
{
    Left,
    NotWaitlisted,
}

internal enum CancelOutcome
{
    Cancelled,
    NotBooked,
    CancellationClosed,
}

/// <summary>
/// One scheduled class, e.g. Spinning on Tuesday at 18:00, with its bookings and its waitlist.
/// All booking rules (D-039) and waitlist rules (D-051) live here, so they can be tested
/// without a database.
/// </summary>
internal sealed class ClassSession
{
    public const int InstructorNameMaxLength = 100;

    private readonly List<ClassBooking> _bookings = [];

    private readonly List<WaitlistEntry> _waitlist = [];

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

    /// <summary>Every waitlist entry, including the ones that left the queue.</summary>
    public IReadOnlyList<WaitlistEntry> Waitlist => _waitlist;

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

    /// <summary>The members waiting for a spot, first come, first served (D-051).</summary>
    public IReadOnlyList<WaitlistEntry> Queue =>
        _waitlist
            .Where(entry => entry.IsWaiting)
            .OrderBy(entry => entry.Number)
            .ToList();

    /// <summary>The account's place in the queue, counting from 1, or null when it isn't waiting.</summary>
    public int? WaitlistPlaceOf(Guid accountId)
    {
        var index = Queue.ToList().FindIndex(entry => entry.AccountId == accountId);
        return index < 0 ? null : index + 1;
    }

    /// <param name="lastDayOfAccess">
    /// The member's last day of access (Copenhagen) when they have cancelled, else null (D-048).
    /// </param>
    public BookOutcome Book(Guid accountId, Instant now, LocalDate? lastDayOfAccess = null)
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

        if (StartsAfter(lastDayOfAccess))
        {
            return BookOutcome.AfterMembershipEnds;
        }

        if (SpotsLeft <= 0)
        {
            return BookOutcome.Full;
        }

        // A member is never both booked and waitlisted: booking takes them out of the queue.
        WaitingEntryOf(accountId)?.EndBecauseBooked(now);
        _bookings.Add(ClassBooking.Create(Id, accountId, now));
        return BookOutcome.Booked;
    }

    /// <summary>
    /// Puts the account at the back of the queue of a full class (D-051). The same booking
    /// window (D-039) and membership rule (D-055) apply as for booking.
    /// </summary>
    /// <param name="lastDayOfAccess">
    /// The member's last day of access (Copenhagen) when they have cancelled, else null (D-048).
    /// </param>
    public JoinWaitlistOutcome JoinWaitlist(Guid accountId, Instant now, LocalDate? lastDayOfAccess = null)
    {
        if (now >= StartsAt)
        {
            return JoinWaitlistOutcome.Started;
        }

        if (StartsAt - now > BookingPolicy.BookingWindow)
        {
            return JoinWaitlistOutcome.NotOpenYet;
        }

        if (IsBookedBy(accountId))
        {
            return JoinWaitlistOutcome.AlreadyBooked;
        }

        if (WaitingEntryOf(accountId) is not null)
        {
            return JoinWaitlistOutcome.AlreadyWaitlisted;
        }

        if (StartsAfter(lastDayOfAccess))
        {
            return JoinWaitlistOutcome.AfterMembershipEnds;
        }

        if (SpotsLeft > 0)
        {
            return JoinWaitlistOutcome.NotFull;
        }

        var number = _waitlist.Count == 0 ? 1 : _waitlist.Max(entry => entry.Number) + 1;
        _waitlist.Add(WaitlistEntry.Create(Id, accountId, number, now));
        return JoinWaitlistOutcome.Joined;
    }

    /// <summary>Takes the account out of the queue. Leaving is always allowed; there are no penalties (D-051).</summary>
    public LeaveWaitlistOutcome LeaveWaitlist(Guid accountId, Instant now)
    {
        if (WaitingEntryOf(accountId) is not { } entry)
        {
            return LeaveWaitlistOutcome.NotWaitlisted;
        }

        entry.Leave(now);
        return LeaveWaitlistOutcome.Left;
    }

    /// <summary>
    /// Whether the class starts on a Copenhagen day after <paramref name="lastDayOfAccess"/>
    /// (D-055 option A). A class on the last day itself is still within the membership.
    /// </summary>
    public bool StartsAfter(LocalDate? lastDayOfAccess) =>
        lastDayOfAccess is { } lastDay && StartsAt.InZone(CenterTime.Zone).Date > lastDay;

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

    /// <summary>
    /// Cancels the account's booking because their membership ends before this class: the
    /// class starts on a Copenhagen day after <paramref name="lastDayOfAccess"/> (D-055
    /// amendment, FIT-97). The 12-hour cut-off (D-039) doesn't apply; it limits what members
    /// do themselves, and here the system frees the spot.
    /// </summary>
    /// <returns>True when a booking was cancelled.</returns>
    public bool CancelBecauseMembershipEnds(Guid accountId, LocalDate lastDayOfAccess, Instant now)
    {
        if (!StartsAfter(lastDayOfAccess) || ActiveBookingOf(accountId) is not { } booking)
        {
            return false;
        }

        booking.Cancel(now);
        return true;
    }

    private WaitlistEntry? WaitingEntryOf(Guid accountId) =>
        _waitlist.SingleOrDefault(entry => entry.IsWaiting && entry.AccountId == accountId);

    private ClassBooking? ActiveBookingOf(Guid accountId) =>
        _bookings.SingleOrDefault(booking => booking.IsActive && booking.AccountId == accountId);
}
