namespace FitnessCenter.Modules.Access.Domain;

internal readonly record struct AccessCardId(Guid Value)
{
    public static AccessCardId New() => new(Guid.CreateVersion7());
}

internal readonly record struct AttendanceId(Guid Value)
{
    public static AttendanceId New() => new(Guid.CreateVersion7());
}
