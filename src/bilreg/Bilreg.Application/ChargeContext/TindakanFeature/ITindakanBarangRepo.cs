using Bilreg.Domain.ChargeContext.TindakanFeature;

namespace Bilreg.Application.ChargeContext.TindakanFeature;

public interface ITindakanBarangRepo
{
    void Insert(TindakanBarangType model, string userId = "");
    void Insert(IEnumerable<TindakanBarangType> models, string userId = "");
    IEnumerable<TindakanBarangType> ListData(ITindakanKey filter);
    IEnumerable<TindakanBarangType> ListByTindakanId(string tindakanId);
    TindakanBarangType? GetData(string tindakanBarangId);
    void MarkVoid(string tindakanId, string updUser = "");
}
