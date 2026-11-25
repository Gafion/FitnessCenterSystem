namespace FitnessCenter.Modules.Booking.Domain;

internal readonly record struct ClassTypeId(Guid Value);

internal readonly record struct ClassSessionId(Guid Value)
{
    public static ClassSessionId New() => new(Guid.CreateVersion7());
}

internal readonly record struct ClassBookingId(Guid Value)
{
    public static ClassBookingId New() => new(Guid.CreateVersion7());
}

internal readonly record struct WaitlistEntryId(Guid Value)
{
    public static WaitlistEntryId New() => new(Guid.CreateVersion7());
}
