namespace Bilreg.Domain.AdmisiContext.AntrianFeature;



public record ServicePointType(
    string ServicePointId,
    string ServicePointName,
    ServicePointStatusEnum Status,
    AntrianModel Antrian)
{
    public static ServicePointType Default => new
    (
        "-", "-", ServicePointStatusEnum.Opened, AntrianModel.Default
    );
}