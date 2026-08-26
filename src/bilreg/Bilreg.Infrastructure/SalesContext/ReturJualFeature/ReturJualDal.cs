using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.SalesContext.ReturJualFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.SalesContext.ReturJualFeature;

public interface IReturJualDal :
    IInsert<ReturJualDto>,
    IUpdate<ReturJualDto>,
    IDelete<IReturJualKey>,
    IGetData<ReturJualDto, IReturJualKey>,
    IListData<ReturJualDto, IRegKey>
{
}

public class ReturJualDal : IReturJualDal
{
    private const string ApiUser = "BILREG-API";
    private readonly DatabaseOptions _opt;

    public ReturJualDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(ReturJualDto model)
    {
        throw new NotImplementedException();
    }

    public void Update(ReturJualDto model)
    {
        throw new NotImplementedException();
    }

    public void Delete(IReturJualKey key)
    {
        throw new NotImplementedException();
    }

    public ReturJualDto GetData(IReturJualKey key)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<ReturJualDto> ListData(IRegKey filter)
    {
        throw new NotImplementedException();
    }
}
