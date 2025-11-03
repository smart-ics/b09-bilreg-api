using Bilreg.Application.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.BillContext.BedUsageFeature;
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
        throw new NotImplementedException();
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
        var listDto = _polisDal.ListData(filter);
        var result = listDto.Select(x => new PolisView(
            x.fs_kd_polis,
            x.fs_no_polis,
            x.fs_atas_nama,
            new PasienReff(x.fs_mr, x.fs_nm_pasien, DateOnly.Parse(x.fd_tgl_lahir), x.fs_jns_kelamin),
            new TipeJaminanReff(x.fs_kd_tipe_jaminan, x.fs_nm_tipe_jaminan),
            DateOnly.Parse(x.fd_tgl_expired)));
        return result;
    }

}