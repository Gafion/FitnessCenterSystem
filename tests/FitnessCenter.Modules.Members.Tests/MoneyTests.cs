using FitnessCenter.SharedKernel;

namespace FitnessCenter.Modules.Members.Tests;

[TestClass]
public sealed class MoneyTests
{
    [TestMethod]
    public void Kroner_are_stored_as_whole_ore()
    {
        Assert.AreEqual(29_900, Money.FromKroner(299).Ore);
    }

    [TestMethod]
    public void Negative_amounts_are_refused()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new Money(-1));
    }
}
