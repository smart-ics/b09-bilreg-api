using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.PaymentContext.TataRekeningFeature;

public interface IMergeRequestKey
{
    string MergeRequestId { get; }
}

public record MergeRequestKey(string MergeRequestId) : IMergeRequestKey;

public interface IMergeRequestRepo :
    ISaveChange<MergeRequestModel>,
    ILoadEntity<MergeRequestModel, IMergeRequestKey>
{
    IEnumerable<MergeRequestModel> ListPendingByReg(IRegKey regKey);

    IEnumerable<MergeRequestModel> ListPendingByPatient(string pasienId);
}
