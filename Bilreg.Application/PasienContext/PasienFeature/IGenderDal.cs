using Bilreg.Domain.PasienContext.PasienFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.PasienContext.PasienFeature;

public interface IGenderDal : 
    IGetDataMayBe<GenderType, string>
{ }
