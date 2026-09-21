namespace Bilreg.Application.IgdContext.Integration;

/// <summary>
/// BILREG port for the outbound SMASS assessment contract (architecture §5.2, §6.2,
/// §6.3, §10.1). Implemented in Infrastructure by <c>SmassAssessmentGateway</c>.
/// Exactly one HTTP attempt is made per call; failures are returned, never thrown.
/// </summary>
public interface ISmassAssessmentGateway
{
    /// <summary>
    /// Creates one immutable SMASS assessment snapshot for one triage event.
    /// Returns a failed result (without any HTTP call) when required configuration
    /// is missing (AR-02, P-08).
    /// </summary>
    Task<SmassGatewayResult> GenerateIgdTriage(
        SmassGenerateIgdTriageRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Populates the administrative keys of every assessment correlated to the visit.
    /// Returns a failed result (without any HTTP call) when required configuration
    /// is missing (AR-02, P-08).
    /// </summary>
    Task<SmassGatewayResult> LinkIgdVisit(
        SmassLinkIgdVisitRequest request,
        CancellationToken cancellationToken = default);
}
