using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.AntrianFeature;

public interface IAdmissionServicePointRepo :
    ISaveChange<AdmissionServicePointModel>,
    ILoadEntity<AdmissionServicePointModel, IAdmissionServicePointKey>
{
    IReadOnlyList<AdmissionServicePointModel> ListAll();
}
