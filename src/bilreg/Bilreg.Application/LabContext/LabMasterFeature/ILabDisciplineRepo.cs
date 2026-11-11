using Bilreg.Domain.LabContext.LabMasterFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.LabContext.LabMasterFeature;

public interface ILabDisciplineRepo :
    ISaveChange<LabDisciplineModel>,
    ILoadEntity<LabDisciplineModel, ILabDisciplineKey>,
    IListData<LabDisciplineModel>
{
    IEnumerable<LabDisciplineModel> ListData(string? search);
}
