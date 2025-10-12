using Bilreg.Domain.PasienContext.PasienFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.PasienContext.PasienFeature;

public interface IGenderDal : 
    IListData<GenderType>,
    IGetDataMayBe<GenderType, string>
{ }
