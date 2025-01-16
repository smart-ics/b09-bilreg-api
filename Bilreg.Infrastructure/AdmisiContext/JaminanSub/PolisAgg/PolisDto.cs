using Bilreg.Domain.AdmisiContext.JaminanSub.PolisAgg;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Infrastructure.AdmisiContext.JaminanSub.PolisAgg;

internal class PolisDto : PolisModel
{
    public PolisDto() : base(string.Empty)
    {
    }
    public string fs_kd_polis {get => PolisId; set => PolisId = value;}
    public string fs_kd_tipe_jaminan {get => TipeJaminanId; set => TipeJaminanId = value;}
    public string fs_nm_tipe_jaminan {get => TipeJaminanName; set => TipeJaminanName = value;}
    public string fs_no_polis {get => NoPolis; set => NoPolis = value;}
    public string fs_atas_nama {get => AtasNama; set => AtasNama = value;}
    public string fd_expired {get => ExpiredDate.ToString("yyyy-MM-dd"); set => ExpiredDate = value.ToDate(DateFormatEnum.YMD);}
    public string fs_kd_kelas_ri {get => KelasId; set => KelasId = value;}
    public string fs_nm_kelas_ri {get => KelasName; set => KelasName = value;}
    public bool fb_cover_rj {get => IsCoverRajal; set => IsCoverRajal = value;}
}