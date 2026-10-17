using Bilreg.Domain.BrmContext.MutasiBerkasFeature;

namespace Bilreg.Infrastructure.BrmContext.MutasiBerkasFeature;

public record MutasiBerkasDto : IMutasiBerkasKey
{
    public string FS_KD_TRS_BUAT { get; init; } = string.Empty;
    public string FS_MR { get; init; } = string.Empty;
    public string FS_KD_MUTASI { get; init; } = string.Empty;
    public string FD_TGL_MUTASI { get; init; } = string.Empty;
    public string FS_JAM_MUTASI { get; init; } = string.Empty;
    public string FS_KD_PETUGAS_MEDIS { get; init; } = string.Empty;
    public string FS_KD_LAYANAN { get; init; } = string.Empty;
    public string FD_TGL_PRAKTEK { get; init; } = string.Empty;
    public string FS_JAM_PRAKTEK { get; init; } = string.Empty;
    public string FS_KD_SESION_POLI { get; init; } = string.Empty;
    public int FN_STATUS { get; init; }
    public string FS_KD_PETUGAS { get; init; } = string.Empty;
    public int FN_CETAK { get; init; }
    public string FS_KD_PETUGAS_CETAK { get; init; } = string.Empty;
    public string fd_tgl_cetak { get; init; } = string.Empty;
    public string fs_jam_cetak { get; init; } = string.Empty;
    public string fd_tgl_void { get; init; } = string.Empty;
    public string fs_jam_void { get; init; } = string.Empty;
    public string fs_kd_petugas_void { get; init; } = string.Empty;

    // Legacy Audit Fields
    public string CRTTGL { get; init; } = string.Empty;
    public string CRTJAM { get; init; } = string.Empty;
    public string CRTIPA { get; init; } = string.Empty;
    public string CRTVER { get; init; } = string.Empty;
    public string CRTUSR { get; init; } = string.Empty;
    public string UPDTGL { get; init; } = string.Empty;
    public string UPDJAM { get; init; } = string.Empty;
    public string UPDIPA { get; init; } = string.Empty;
    public string UPDVER { get; init; } = string.Empty;
    public string UPDUSR { get; init; } = string.Empty;

    public string MutasiBerkasId => FS_KD_MUTASI;

    public static MutasiBerkasDto FromModel(MutasiBerkasModel model)
    {
        return new MutasiBerkasDto
        {
            FS_KD_MUTASI = model.MutasiBerkasId,
            FS_KD_TRS_BUAT = model.TrsBuatId,
            FS_MR = model.PasienId,
            FD_TGL_MUTASI = model.TglMutasi.ToString("yyyy-MM-dd"),
            FS_JAM_MUTASI = model.JamMutasi.ToString("HH:mm:ss"),
            FS_KD_PETUGAS_MEDIS = model.JadwalPelayanan.DokterId,
            FS_KD_LAYANAN = model.JadwalPelayanan.LayananId,
            FD_TGL_PRAKTEK = model.JadwalPelayanan.TglPraktek.ToString("yyyy-MM-dd"),
            FS_JAM_PRAKTEK = model.JadwalPelayanan.JamPraktek,
            FS_KD_SESION_POLI = model.JadwalPelayanan.SesiPoliId,
            FN_STATUS = (int)model.Status,
            FS_KD_PETUGAS = model.PetugasId,
            FN_CETAK = model.CetakTracerInfo.FrekuensiCetak,
            FS_KD_PETUGAS_CETAK = model.CetakTracerInfo.PetugasCetak,
            fd_tgl_cetak = model.CetakTracerInfo.TglCetak.ToString("yyyy-MM-dd"),
            fs_jam_cetak = model.CetakTracerInfo.JamCetak.ToString("HH:mm:ss"),
            fd_tgl_void = model.VoidInfo.IsVoid ? model.VoidInfo.TglVoid.ToString("yyyy-MM-dd") : "3000-01-01",
            fs_jam_void = model.VoidInfo.IsVoid ? model.VoidInfo.JamVoid.ToString("HH:mm:ss") : "00:00:00",
            fs_kd_petugas_void = model.VoidInfo.IsVoid ? model.VoidInfo.PetugasVoid : string.Empty,
            CRTTGL = DateTime.Now.ToString("yyyy-MM-dd"),
            CRTJAM = DateTime.Now.ToString("HH:mm:ss"),
            CRTIPA = "127.0.0.1",
            CRTVER = "1.0",
            CRTUSR = model.PetugasId,
            UPDTGL = DateTime.Now.ToString("yyyy-MM-dd"),
            UPDJAM = DateTime.Now.ToString("HH:mm:ss"),
            UPDIPA = "127.0.0.1",
            UPDVER = "1.0",
            UPDUSR = model.PetugasId
        };
    }

    public MutasiBerkasModel ToModel(VolumeBerkasType? volume = null)
    {
        var tglMutasi = DateOnly.TryParse(FD_TGL_MUTASI, out var tm) ? tm : new DateOnly(3000, 1, 1);
        var jamMutasi = TimeOnly.TryParse(FS_JAM_MUTASI, out var jm) ? jm : new TimeOnly(0, 0, 0);
        var tglPraktek = DateOnly.TryParse(FD_TGL_PRAKTEK, out var tp) ? tp : new DateOnly(3000, 1, 1);
        var jadwal = new JadwalPelayananType(FS_KD_LAYANAN, FS_KD_PETUGAS_MEDIS, tglPraktek, FS_JAM_PRAKTEK, FS_KD_SESION_POLI);

        var tglCetak = DateOnly.TryParse(fd_tgl_cetak, out var tc) ? tc : new DateOnly(3000, 1, 1);
        var jamCetak = TimeOnly.TryParse(fs_jam_cetak, out var jc) ? jc : new TimeOnly(0, 0, 0);
        var cetakInfo = new CetakTracerInfoType(FN_CETAK, FS_KD_PETUGAS_CETAK, tglCetak, jamCetak);

        var isVoid = !string.IsNullOrWhiteSpace(fd_tgl_void) && fd_tgl_void != "3000-01-01";
        var tglVoid = DateOnly.TryParse(fd_tgl_void, out var tv) ? tv : new DateOnly(3000, 1, 1);
        var jamVoid = TimeOnly.TryParse(fs_jam_void, out var jv) ? jv : new TimeOnly(0, 0, 0);
        var voidInfo = new VoidInfoType(isVoid, fs_kd_petugas_void, tglVoid, jamVoid);

        var status = (MutasiBerkasStatusEnum)FN_STATUS;
        return new MutasiBerkasModel(
            FS_KD_MUTASI,
            FS_KD_TRS_BUAT,
            FS_MR,
            tglMutasi,
            jamMutasi,
            FS_KD_PETUGAS,
            jadwal,
            status,
            volume ?? VolumeBerkasType.Default,
            cetakInfo,
            voidInfo);
    }
}
