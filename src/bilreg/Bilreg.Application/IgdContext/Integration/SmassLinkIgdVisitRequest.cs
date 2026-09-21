namespace Bilreg.Application.IgdContext.Integration;

/// <summary>
/// Gateway request DTO for <c>PATCH {Smass:BaseApiUrl}/api/Assesment/linkIgdVisit</c>
/// (architecture §5.3, §6.3). Administrative keys are populated all-or-nothing by
/// the caller from the loaded <c>RegModel</c>; the wire payload is camelCase JSON.
/// </summary>
public record SmassLinkIgdVisitRequest
{
    public string IgdVisitId { get; init; } = string.Empty;
    public string RegId { get; init; } = string.Empty;
    public string PasienId { get; init; } = string.Empty;
    public string PasienName { get; init; } = string.Empty;
    public string LayananId { get; init; } = string.Empty;
    public string LayananName { get; init; } = string.Empty;
}
