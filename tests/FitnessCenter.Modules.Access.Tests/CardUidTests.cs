using FitnessCenter.Modules.Access.Domain;

namespace FitnessCenter.Modules.Access.Tests;

[TestClass]
public sealed class CardUidTests
{
    [TestMethod]
    public void A_simulated_uid_is_the_prefix_and_seven_bytes_in_hex()
    {
        var uid = CardUid.Simulated([0x04, 0xA1, 0xB2, 0xC3, 0xD4, 0xE5, 0xF6]);

        Assert.AreEqual("SIM-04A1B2C3D4E5F6", uid);
    }

    [TestMethod]
    public void A_simulated_uid_needs_exactly_seven_bytes()
    {
        Assert.ThrowsExactly<ArgumentException>(() => CardUid.Simulated([1, 2, 3]));
    }

    [TestMethod]
    public void Typed_uids_are_trimmed_and_upper_cased()
    {
        Assert.AreEqual("SIM-04A1B2C3D4E5F6", CardUid.Normalize("  sim-04a1b2c3d4e5f6 "));
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void Empty_input_is_not_a_uid(string? input)
    {
        Assert.IsNull(CardUid.Normalize(input));
    }

    [TestMethod]
    public void Input_longer_than_a_uid_can_be_is_not_a_uid()
    {
        Assert.IsNotNull(CardUid.Normalize(new string('A', CardUid.MaxLength)));
        Assert.IsNull(CardUid.Normalize(new string('A', CardUid.MaxLength + 1)));
    }
}
