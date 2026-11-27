using Bilreg.Domain.RadContext.RadOrderFeature;

namespace Bilreg.Application.RadContext.RadOrderFeature;

public interface IRadOrderDal
{
    RadOrderDto? GetData(IRadOrderKey key);
    bool HasRecentOrder(string patientId, string modality, IEnumerable<string> tarifIds, int windowHours);
    IEnumerable<RadOrderWorklistView> GetWorklist(DateTime date, string modality, int? status = null);
    IEnumerable<RadOrderDto> ListByRegId(string regId);
}
