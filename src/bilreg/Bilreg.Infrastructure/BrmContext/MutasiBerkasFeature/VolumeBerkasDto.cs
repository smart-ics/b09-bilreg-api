using Bilreg.Domain.BrmContext.MutasiBerkasFeature;

namespace Bilreg.Infrastructure.BrmContext.MutasiBerkasFeature;

public record VolumeBerkasDto
{
    public string FS_KD_REG { get; init; } = string.Empty;
    public string FS_MR { get; init; } = string.Empty;
    public decimal FN_QTY_BERKAS_PINJAM { get; init; }
    public decimal FN_QTY_BERKAS_KEMBALI { get; init; }
    public decimal FN_QTY_BERKAS_PINJAM_RI { get; init; }
    public decimal FN_QTY_BERKAS_KEMBALI_RI { get; init; }
    public decimal FN_QTY_BERKAS_PINJAM_RJ { get; init; }
    public decimal FN_QTY_BERKAS_KEMBALI_RJ { get; init; }
    public string FD_TGL_LAST { get; init; } = string.Empty;
    public string FS_JAM_LAST { get; init; } = string.Empty;

    public VolumeBerkasType ToModel()
    {
        return new VolumeBerkasType(
            FN_QTY_BERKAS_PINJAM_RI,
            FN_QTY_BERKAS_PINJAM_RJ,
            FN_QTY_BERKAS_KEMBALI_RI,
            FN_QTY_BERKAS_KEMBALI_RJ);
    }

    public static VolumeBerkasDto FromModel(string regId, string mrId, VolumeBerkasType volume, DateOnly? tglLast = null, TimeOnly? jamLast = null)
    {
        var now = DateTime.Now;
        var tgl = tglLast?.ToString("yyyy-MM-dd") ?? now.ToString("yyyy-MM-dd");
        var jam = jamLast?.ToString("HH:mm:ss") ?? now.ToString("HH:mm:ss");

        return new VolumeBerkasDto
        {
            FS_KD_REG = regId,
            FS_MR = mrId,
            FN_QTY_BERKAS_PINJAM = volume.TotalPinjam,
            FN_QTY_BERKAS_KEMBALI = volume.TotalKembali,
            FN_QTY_BERKAS_PINJAM_RI = volume.QtyPinjamRi,
            FN_QTY_BERKAS_KEMBALI_RI = volume.QtyKembaliRi,
            FN_QTY_BERKAS_PINJAM_RJ = volume.QtyPinjamRj,
            FN_QTY_BERKAS_KEMBALI_RJ = volume.QtyKembaliRj,
            FD_TGL_LAST = tgl,
            FS_JAM_LAST = jam
        };
    }
}
