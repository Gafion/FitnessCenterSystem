namespace FitnessCenter.Modules.Members.Domain;

internal readonly record struct MemberId(Guid Value)
{
    public static MemberId New() => new(Guid.CreateVersion7());
}

internal readonly record struct MembershipTypeId(Guid Value);

internal readonly record struct ConsentId(Guid Value)
{
    public static ConsentId New() => new(Guid.CreateVersion7());
}

internal readonly record struct PaymentRecordId(Guid Value)
{
    public static PaymentRecordId New() => new(Guid.CreateVersion7());
}
