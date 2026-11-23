namespace FitnessCenter.Modules.Booking.Domain;

/// <summary>A kind of class, e.g. Spinning. Each scheduled occurrence is a <see cref="ClassSession"/>.</summary>
internal sealed class ClassType
{
    // The center runs spinning and CrossFit classes (F3, D-001). The full list comes from the
    // center meeting (D-004); until there are staff screens these are fixed (D-022).
    public static readonly ClassType Spinning = new(new ClassTypeId(Guid.Parse("0199a3c0-0000-7000-8000-000000000101")), "Spinning");
    public static readonly ClassType CrossFit = new(new ClassTypeId(Guid.Parse("0199a3c0-0000-7000-8000-000000000102")), "CrossFit");

    public static IReadOnlyList<ClassType> All { get; } = [Spinning, CrossFit];

    public const int NameMaxLength = 100;

    private ClassType(ClassTypeId id, string name)
    {
        Id = id;
        Name = name;
    }

    public ClassTypeId Id { get; private set; }

    public string Name { get; private set; }
}
