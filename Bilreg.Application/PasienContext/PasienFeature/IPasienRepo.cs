using Bilreg.Application.Helpers;
using Bilreg.Domain.PasienContext.PasienFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.PasienContext.PasienFeature;

public interface IPasienRepo : 
    ISaveChange<PasienModel>,
    ILoadEntity<PasienModel, IPasienKey>,
    IDeleteEntity<PasienModel>,
    IListData<PasienReff, SearchKeyword>
{
}

public record SearchKeyword(string Keyword);
