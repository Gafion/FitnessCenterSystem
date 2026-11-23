namespace FitnessCenter.Modules.Access.Contracts;

/// <summary>
/// What other modules may ask the Access module to do (rule 1).
/// Members uses it during signup, so every member has a card (D-003).
/// </summary>
public interface IAccessCards
{
    /// <summary>
    /// Gives the member a new synthetic access card (D-021). Runs inside the caller's
    /// transaction when there is one, so a failed signup leaves no card behind (D-015).
    /// The member must already be saved: the card points to it.
    /// </summary>
    Task IssueCardAsync(Guid memberId, CancellationToken cancellationToken);
}
