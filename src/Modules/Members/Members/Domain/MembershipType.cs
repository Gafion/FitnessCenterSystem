using FitnessCenter.SharedKernel;

namespace FitnessCenter.Modules.Members.Domain;

/// <summary>
/// A kind of membership the center sells. The 6 real types are unknown until the
/// center meeting (M1); for now there is one placeholder (D-032).
/// </summary>
internal sealed class MembershipType
{
    /// <summary>Placeholder "General membership" with a synthetic price (D-032).</summary>
    public static readonly MembershipType General = new(
        new MembershipTypeId(Guid.Parse("0199a3c0-0000-7000-8000-000000000001")),
        "General membership",
        Money.FromKroner(299));

    public MembershipType(MembershipTypeId id, string name, Money monthlyPrice)
    {
        Id = id;
        Name = name;
        MonthlyPrice = monthlyPrice;
    }

    public MembershipTypeId Id { get; private set; }

    public string Name { get; private set; }

    public Money MonthlyPrice { get; private set; }

    /// <summary>
    /// The name in the language asked for (D-065). The stored name is English; the placeholder
    /// type has a Danish name here. A type without one shows the stored name.
    /// </summary>
    public string NameIn(Language language) =>
        language == Language.Danish && Id == General.Id ? "Almindeligt medlemskab" : Name;
}
