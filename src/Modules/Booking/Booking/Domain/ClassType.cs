namespace FitnessCenter.Modules.Booking.Domain;

/// <summary>
/// A kind of class, e.g. Spinning. Each scheduled occurrence is a <see cref="ClassSession"/>.
/// Managers and admins create and rename class types (D-068); Spinning and CrossFit are seeded.
/// </summary>
internal sealed class ClassType
{
    // The center runs spinning and CrossFit classes (F3, D-001), so these two are seeded by the
    // initial migration. More are added through the staff API (D-068).
    public static readonly ClassType Spinning = new(new ClassTypeId(Guid.Parse("0199a3c0-0000-7000-8000-000000000101")), "Spinning");
    public static readonly ClassType CrossFit = new(new ClassTypeId(Guid.Parse("0199a3c0-0000-7000-8000-000000000102")), "CrossFit");

    /// <summary>The seeded class types.</summary>
    public static IReadOnlyList<ClassType> Seeded { get; } = [Spinning, CrossFit];

    public const int NameMaxLength = 100;

    private ClassType(ClassTypeId id, string name)
    {
        Id = id;
        Name = name;
    }

    public ClassTypeId Id { get; private set; }

    public string Name { get; private set; }

    /// <param name="name">Already trimmed and checked by <see cref="IsValidName"/>.</param>
    public static ClassType Create(string name) => new(ClassTypeId.New(), CheckedName(name));

    /// <param name="name">Already trimmed and checked by <see cref="IsValidName"/>.</param>
    public void Rename(string name) => Name = CheckedName(name);

    /// <summary>A name is 1 to <see cref="NameMaxLength"/> characters, without leading or trailing spaces.</summary>
    public static bool IsValidName(string? name) =>
        !string.IsNullOrWhiteSpace(name) && name.Length <= NameMaxLength && name.Trim() == name;

    private static string CheckedName(string name) =>
        IsValidName(name) ? name : throw new ArgumentException("A class type needs a name of 1 to 100 characters.", nameof(name));
}
