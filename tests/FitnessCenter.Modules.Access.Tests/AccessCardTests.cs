using FitnessCenter.Modules.Access.Domain;
using NodaTime;

namespace FitnessCenter.Modules.Access.Tests;

[TestClass]
public sealed class AccessCardTests
{
    [TestMethod]
    public void A_card_stores_its_uid_normalized()
    {
        var card = AccessCard.Issue(Guid.NewGuid(), " sim-0011223344556 ");

        Assert.AreEqual("SIM-0011223344556", card.Uid);
    }

    [TestMethod]
    public void A_card_needs_a_uid()
    {
        Assert.ThrowsExactly<ArgumentException>(() => AccessCard.Issue(Guid.NewGuid(), " "));
    }

    [TestMethod]
    public void A_swipe_is_an_attendance_for_the_card_and_its_member_at_that_moment()
    {
        var memberId = Guid.NewGuid();
        var card = AccessCard.Issue(memberId, "SIM-0011223344556");
        var at = Instant.FromUtc(2027, 1, 12, 6, 30);

        var attendance = card.Swipe(at);

        Assert.AreEqual(card.Id, attendance.AccessCardId);
        Assert.AreEqual(memberId, attendance.MemberId);
        Assert.AreEqual(at, attendance.SwipedAt);
    }
}
