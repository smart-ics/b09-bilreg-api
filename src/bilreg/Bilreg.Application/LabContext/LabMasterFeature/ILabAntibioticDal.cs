using Bilreg.Domain.LabContext.LabMasterFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.LabContext.LabMasterFeature;

public interface ILabAntibioticDal :
    IInsert<LabAntibioticDto>,
    IUpdate<LabAntibioticDto>,
    IDelete<ILabAntibioticKey>,
    IGetData<LabAntibioticDto?, ILabAntibioticKey>,
    IListData<LabAntibioticDto>
{
    IEnumerable<LabAntibioticDto> ListData(string? search);
}
