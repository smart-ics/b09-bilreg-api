namespace Bilreg.Domain.PaymentContext.TataRekeningFeature;

/// <summary>
/// Port abstraction for validating whether two registrations have a valid episode relationship
/// established by the Admission domain (AdmisiContext).
/// </summary>
public interface IRegRelationPolicy
{
    /// <summary>
    /// Checks whether the source registration and target registration have a valid episode relationship.
    /// </summary>
    /// <param name="sourceRegId">The source registration ID.</param>
    /// <param name="targetRegId">The target registration ID.</param>
    /// <returns>True if both registrations are legitimately related; otherwise false.</returns>
    bool IsRelated(string sourceRegId, string targetRegId);
}
