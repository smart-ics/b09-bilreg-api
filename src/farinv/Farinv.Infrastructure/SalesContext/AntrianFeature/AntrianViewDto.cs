using Farinv.Domain.SalesContext.AntrianFeature;

namespace Farinv.Infrastructure.SalesContext.AntrianFeature;

public record AntrianViewDto(string AntrianId, int NoAntrian, int AntrianStatus,
    string RegId, string PasienId, string PasienName,
    string ReffId, string ReffDesc, DateTime AntrianDate, int ServicePoint, string AntrianDescription)
{
    public AntrianView ToView()
    {
        var result = new AntrianView(
            AntrianId, NoAntrian, (AntrianStatusEnum)AntrianStatus, RegId, PasienId, PasienName, 
            ReffId, ReffDesc, AntrianDate, ServicePoint, AntrianDescription
            );
        return result;
    }
}
