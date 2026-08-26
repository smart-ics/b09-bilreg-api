using Bilreg.Domain.SalesContext.ReturJualFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.SalesContext.ReturJualFeature;

public interface IReturJualItemDal :
    IInsertBulk<ReturJualItemDto>,
    IDelete<IReturJualKey>,
    IListData<ReturJualItemDto, IReturJualKey>
{
}

public class ReturJualItemDal : IReturJualItemDal
{
    private readonly DatabaseOptions _opt;

    public ReturJualItemDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(IEnumerable<ReturJualItemDto> listModel)
    {
        throw new NotImplementedException();
    }
    public void Delete(IReturJualKey key)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<ReturJualItemDto> ListData(IReturJualKey filter)
    {
        throw new NotImplementedException();
    }
}
