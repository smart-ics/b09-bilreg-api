using Bilreg.Domain.LabContext.LabMasterFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.LabContext.LabMasterFeature;

public interface ILabAntibioticRepo :
    ISaveChange<LabAntibioticModel>,
    ILoadEntity<LabAntibioticModel, ILabAntibioticKey>,
    IListData<LabAntibioticModel>
{
    IEnumerable<LabAntibioticModel> ListData(string? search);
}
