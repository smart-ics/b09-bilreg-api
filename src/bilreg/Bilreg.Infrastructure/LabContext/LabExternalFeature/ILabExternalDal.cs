using Bilreg.Domain.LabContext.LabExternalFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.LabContext.LabExternalFeature;

public interface ILabExternalDal :
    IInsert<LabExternalDto>,
    IUpdate<LabExternalDto>,
    IDelete<ILabExternalKey>,
    IGetData<LabExternalDto, ILabExternalKey>
{
    LabExternalDto? GetByExternalMrNo(string externalMrNo);
    IEnumerable<LabExternalDto> ListData(string? searchTerm, DateTime? date1, DateTime? date2, int? status);
}
