using Bilreg.Domain.LabContext.LabMasterFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.LabContext.LabMasterFeature;

public interface ILabDisciplineDal :
    IInsert<LabDisciplineDto>,
    IUpdate<LabDisciplineDto>,
    IDelete<ILabDisciplineKey>,
    IGetData<LabDisciplineDto?, ILabDisciplineKey>,
    IListData<LabDisciplineDto>
{
    IEnumerable<LabDisciplineDto> ListData(string? search);
}
