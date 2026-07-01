using Bilreg.Application.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.AdmisiContext.JaminanFeature;

public class PolisRepo : IPolisRepo
{
    private readonly IPolisDal _polisDal;
    private readonly IPolisCoverDal _polisCoverDal;

    public PolisRepo(IPolisDal polisDal, IPolisCoverDal polisCoverDal)
    {
        _polisDal = polisDal;
        _polisCoverDal = polisCoverDal;
    }

    public void SaveChanges(PolisModel model)
    {
        LoadEntity(model)
            .Match(
                onSome: _ => _polisDal.Update(PolisDto.FromModel(model)),
                onNone: () => _polisDal.Insert(PolisDto.FromModel(model))
            );

        var listCover = model.ListCover.Select(x => PolisCoverDto.FromModel(x)).ToList();
        
        _polisCoverDal.Delete(model);
        _polisCoverDal.Insert(listCover);
    }

    public MayBe<PolisModel> LoadEntity(IPolisKey key)
    {
        var hdr = _polisDal.GetData(key); 
        var listCover = _polisCoverDal.ListData(key)?.ToList() ?? [];
        var listCoverModel = listCover.Select(x => x.ToModel());
        var model = hdr?.ToModel(listCoverModel);
        return MayBe.From(model!);
    }

    public void DeleteEntity(IPolisKey key)
    {
        _polisDal.Delete(key);
        _polisCoverDal.Delete(key);
    }

    public IEnumerable<PolisView> ListData(IPasienKey filter)
    {
        var listDto = _polisDal.ListData(filter)?.ToList() ?? [];
        var result = listDto.Select(x => new PolisView(
            x.fs_kd_polis,
            x.fs_no_polis,
            x.fs_atas_nama,
            new PasienReff(x.fs_mr, x.fs_nm_pasien, DateOnly.Parse(x.fd_tgl_lahir), x.fs_jns_kelamin),
            new TipeJaminanReff(x.fs_kd_tipe_jaminan, x.fs_nm_tipe_jaminan),
            DateOnly.Parse(x.fd_tgl_expired)));
        return result;
    }

    public PolisModel GetDataByNoPeserta(string noPeserta)
    {
        var dto = _polisDal.GetDataByNoPeserta(noPeserta);
        if (dto is null) return PolisModel.Default;
        var lisCoverDto = _polisCoverDal.ListData(PolisModel.Key(dto.fs_kd_polis))?.ToList() ?? [];
        var listCover = lisCoverDto.Select(x => x.ToModel());
        var result = dto.ToModel(listCover);

        return result;
    }
}