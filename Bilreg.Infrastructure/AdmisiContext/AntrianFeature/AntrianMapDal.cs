using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.AdmisiContext.AntrianFeature
{
    public interface IAntrianMapDal : 
        IInsertBulk<AntrianMapDto>,
        IListData<AntrianMapDto, IAntrianMapKey>
    {
    }
        
    public class AntrianMapDal
    {
    }
}
