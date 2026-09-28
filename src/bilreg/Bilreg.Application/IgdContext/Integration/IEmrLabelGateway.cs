namespace Bilreg.Application.IgdContext.Integration;

/// <summary>
/// Request payload for registering an assessment label to EMR 2.0 (architecture TD-01).
/// Note: UserrId has two 'r's to match the EMR 2.0 API contract.
/// </summary>
public record EmrAddSmassLabelRequest
{
    public string AssesmentId { get; init; } = string.Empty;
    public string LayananId { get; init; } = string.Empty;
    public string PaperId { get; init; } = string.Empty;
    public string PaperName { get; init; } = string.Empty;
    public string RegId { get; init; } = string.Empty;
    public string UserrId { get; init; } = string.Empty;
}

/// <summary>
/// Outcome of one outbound EMR 2.0 label registration operation (architecture TD-01).
/// </summary>
public record EmrLabelGatewayResult(bool Success, string? ErrorMessage);

/// <summary>
/// BILREG outbound port for EMR 2.0 label integration (architecture TD-01).
/// Implemented in Infrastructure by <c>EmrLabelGateway</c>.
/// </summary>
public interface IEmrLabelGateway
{
    Task<EmrLabelGatewayResult> AddSmassLabel(
        EmrAddSmassLabelRequest request,
        CancellationToken cancellationToken = default);
}
