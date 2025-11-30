using Bilreg.Application.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.AdmisiContext.JaminanFeature;

public class TipeJaminanRepo : ITipeJaminanRepo
{
    private readonly ITipeJaminanDal _tipeJaminanDal;

    public TipeJaminanRepo(ITipeJaminanDal tipeJaminanDal)
    {
        _tipeJaminanDal = tipeJaminanDal;
    }

    public void SaveChanges(TipeJaminanType model)
    {
        LoadEntity(model)
            .Match(
                onSome: x => _tipeJaminanDal.Update(TipeJaminanDto.FromModel(model)),
                onNone: () => _tipeJaminanDal.Insert(TipeJaminanDto.FromModel(model))
            );
    }

    public MayBe<TipeJaminanType> LoadEntity(ITipeJaminanKey key)
    {
        var result = _tipeJaminanDal.GetData(key);
        var model = result?.ToModel();
        return MayBe.From(model!);
    }

    public void DeleteEntity(ITipeJaminanKey key)
    {
        _tipeJaminanDal.Delete(key);
    }

    public IEnumerable<TipeJaminanView> ListData()
    {
        var listDto = _tipeJaminanDal.ListData();
        var result = listDto.Select(x => new TipeJaminanView(
            x.fs_kd_tipe_jaminan, x.fs_nm_tipe_jaminan,  
            x.fs_nm_jaminan, x.fs_nm_cara_bayar_dk));
        return result;
    }
}