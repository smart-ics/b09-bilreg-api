using Bilreg.Application.ChargeContext.TindakanFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;

namespace Bilreg.Infrastructure.ChargeContext.TindakanFeature;

public class TindakanBarangRepo : ITindakanBarangRepo
{
    private readonly ITindakanBarangDal _tindakanBarangDal;

    public TindakanBarangRepo(ITindakanBarangDal tindakanBarangDal)
    {
        _tindakanBarangDal = tindakanBarangDal;
    }

    public void Insert(TindakanBarangType model, string userId = "")
    {
        var dto = TindakanBarangDto.FromModel(model, userId);
        _tindakanBarangDal.Insert(dto);
    }

    public void Insert(IEnumerable<TindakanBarangType> models, string userId = "")
    {
        var listDto = models.Select(m => TindakanBarangDto.FromModel(m, userId));
        _tindakanBarangDal.Insert(listDto);
    }

    public IEnumerable<TindakanBarangType> ListData(ITindakanKey filter)
    {
        var listDto = _tindakanBarangDal.ListData(filter)?.ToList() ?? [];
        return listDto.Select(x => x.ToModel());
    }

    public IEnumerable<TindakanBarangType> ListByTindakanId(string tindakanId)
    {
        var listDto = _tindakanBarangDal.ListByTindakanId(tindakanId)?.ToList() ?? [];
        return listDto.Select(x => x.ToModel());
    }

    public TindakanBarangType? GetData(string tindakanBarangId)
    {
        var dto = _tindakanBarangDal.GetData(tindakanBarangId);
        return dto?.ToModel();
    }

    public void MarkVoid(string tindakanId, string updUser = "")
    {
        _tindakanBarangDal.MarkVoid(tindakanId, updUser);
    }
}
