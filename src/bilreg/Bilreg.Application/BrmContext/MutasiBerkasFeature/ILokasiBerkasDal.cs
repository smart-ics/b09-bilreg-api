namespace Bilreg.Application.BrmContext.MutasiBerkasFeature;

public record LokasiBerkasDto
{
    public string FS_KD_LOKASI_BERKAS { get; init; } = string.Empty;
    public string FS_NM_LOKASI_BERKAS { get; init; } = string.Empty;
    public string FS_KD_LAYANAN { get; init; } = string.Empty;

    public string LokasiBerkasId => FS_KD_LOKASI_BERKAS;
    public string LokasiBerkasName => FS_NM_LOKASI_BERKAS;
    public string LayananId => FS_KD_LAYANAN;

    public static LokasiBerkasDto Create(string lokasiId, string lokasiName, string layananId)
    {
        return new LokasiBerkasDto
        {
            FS_KD_LOKASI_BERKAS = lokasiId,
            FS_NM_LOKASI_BERKAS = lokasiName,
            FS_KD_LAYANAN = layananId
        };
    }
}

public interface ILokasiBerkasDal
{
    IEnumerable<LokasiBerkasDto> ListAll();
    LokasiBerkasDto? GetData(string lokasiId);
    void Insert(LokasiBerkasDto dto);
    void Delete(string lokasiId);
}
