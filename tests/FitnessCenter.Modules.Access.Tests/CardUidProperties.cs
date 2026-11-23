using CsCheck;
using FitnessCenter.Modules.Access.Domain;

namespace FitnessCenter.Modules.Access.Tests;

/// <summary>Property tests (D-036): what the reader sends and what the issuer creates always match up.</summary>
[TestClass]
public sealed class CardUidProperties
{
    [TestMethod]
    public void A_simulated_uid_is_already_normalized()
    {
        Gen.Byte.Array[7].Sample(bytes =>
        {
            var uid = CardUid.Simulated(bytes);
            return CardUid.Normalize(uid) == uid && uid.StartsWith(CardUid.SimulatedPrefix, StringComparison.Ordinal);
        });
    }

    [TestMethod]
    public void Normalizing_twice_changes_nothing()
    {
        Gen.String.Sample(input => CardUid.Normalize(CardUid.Normalize(input)) == CardUid.Normalize(input));
    }

    [TestMethod]
    public void Case_and_surrounding_spaces_never_matter()
    {
        Gen.Select(Gen.Byte.Array[7], Gen.Int[0, 3], Gen.Bool).Sample((bytes, spaces, lower) =>
        {
            var uid = CardUid.Simulated(bytes);
            var pad = new string(' ', spaces);
            var typed = pad + (lower ? uid.ToLowerInvariant() : uid) + pad;
            return CardUid.Normalize(typed) == uid;
        });
    }
}
