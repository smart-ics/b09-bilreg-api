using Bilreg.Domain.RadContext.RadJadwalFeature;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Application.RadContext.RadJadwalFeature;

public interface IRadJadwalRepo :
    ISaveChange<RadJadwalModel>,
    ILoadEntity<RadJadwalModel, IRadJadwalKey>
{
    MayBe<RadJadwalModel> LoadActiveByOrderId(string orderId);
    IEnumerable<RadJadwalModel> ListByOrderId(string orderId);
    void SaveReschedule(RadJadwalModel model, RadJadwalRescheduleDto rescheduleHistory);
}
