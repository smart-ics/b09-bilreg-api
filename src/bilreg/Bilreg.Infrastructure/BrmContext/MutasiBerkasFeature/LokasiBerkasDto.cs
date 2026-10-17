namespace Bilreg.Infrastructure.BrmContext.MutasiBerkasFeature;

public record LokasiBerkasDto : Bilreg.Application.BrmContext.MutasiBerkasFeature.LokasiBerkasDto
{
    public static new LokasiBerkasDto Create(string lokasiId, string lokasiName, string layananId)
    {
        return new LokasiBerkasDto
        {
            FS_KD_LOKASI_BERKAS = lokasiId,
            FS_NM_LOKASI_BERKAS = lokasiName,
            FS_KD_LAYANAN = layananId
        };
    }
}
