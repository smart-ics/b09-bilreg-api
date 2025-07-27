namespace Bilreg.Domain.AdmisiContext.AntrianFeature;

public record ServicePointType
{
    public string ServicePointId { get; init; }
    public string ServicePointName { get; init; }
    public ServicePointStatusEnum Status { get; init; }
    public AntrianModel Antrian { get; init; }
}