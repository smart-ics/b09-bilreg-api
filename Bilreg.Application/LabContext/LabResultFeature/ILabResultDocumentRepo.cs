using Bilreg.Domain.LabContext.LabResultFeature;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Application.LabContext.LabResultFeature;

public interface ILabResultDocumentRepo :
    ISaveChange<LabResultDocumentModel>,
    ILoadEntity<LabResultDocumentModel, ILabResultDocumentKey>,
    IDeleteEntity<ILabResultDocumentKey>
{
    MayBe<LabResultDocumentModel> LoadByOrderId(string orderId);
}
