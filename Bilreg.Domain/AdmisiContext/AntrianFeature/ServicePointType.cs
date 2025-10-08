namespace Bilreg.Domain.AdmisiContext.AntrianFeature;



public record ServicePointType(
    string ServicePointId,
    string ServicePointName,
    ServicePointStatusEnum Status)
{
    public static ServicePointType Default => 
        new ServicePointType("-", "-", ServicePointStatusEnum.Opened);
}