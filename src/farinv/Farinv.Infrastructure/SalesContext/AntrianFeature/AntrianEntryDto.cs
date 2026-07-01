using Farinv.Domain.SalesContext.AntrianFeature;

namespace Farinv.Infrastructure.SalesContext.AntrianFeature;

public record AntrianEntryDto(
    string AntrianId, int NoAntrian, int AntrianStatus, 
    DateTime TakenAt, DateTime AssignedAt, DateTime PreparedAt, DateTime DeliveredAt, DateTime CancelAt,
    string RegId, string PasienId, string PasienName,
    string ReffId, string ReffDesc) : IAntrianKey
{
    public static AntrianEntryDto FromModel(string antrianId, AntrianEntryModel model)
    {
        var result = new AntrianEntryDto(antrianId, model.NoAntrian, (int)model.AntrianStatus,
            model.TakenAt, model.AssignedAt, model.PreparedAt, model.DeliveredAt,  model.CancelAt,
            model.Reg.RegId, model.Reg.PasienId, model.Reg.PasienName,
            model.ReffId, model.ReffDesc);
        return result;
    }

    public AntrianEntryModel ToModel()
    {
        var reg = new RegReff(RegId, PasienId, PasienName);
        var result = new AntrianEntryModel(NoAntrian, (AntrianStatusEnum)AntrianStatus,
            TakenAt, AssignedAt, PreparedAt, DeliveredAt, CancelAt,
            reg, ReffId, ReffDesc);
        return result;
    }
}
