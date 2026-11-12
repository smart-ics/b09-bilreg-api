namespace Bilreg.Application.AdmisiContext.AktivasiBerkasFeature.Integration;

/// <summary>
/// Kontrak gateway integrasi dari Bilreg.Application ke EMR 2.0 API untuk pengelolaan aktivasi berkas rekam medis.
/// </summary>
public interface IAktivasiBerkasEmrGateway
{
    /// <summary>
    /// Mengambil daftar riwayat aktivasi berkas berdasarkan rentang tanggal.
    /// </summary>
    Task<IEnumerable<AktivasiBerkasListItemDto>> ListData(string tglAwal, string tglAkhir, CancellationToken ct = default);

    /// <summary>
    /// Mengambil data detail transaksi aktivasi berkas berdasarkan ID transaksi.
    /// </summary>
    Task<AktivasiBerkasDetailDto?> GetData(string id, CancellationToken ct = default);

    /// <summary>
    /// Menyimpan transaksi aktivasi berkas baru ke backend EMR.
    /// </summary>
    Task<AktivasiBerkasMutationResult> Save(AktivasiBerkasSaveDto dto, CancellationToken ct = default);

    /// <summary>
    /// Memperbarui transaksi aktivasi berkas yang sudah ada di backend EMR.
    /// </summary>
    Task<AktivasiBerkasMutationResult> Update(string id, AktivasiBerkasUpdateDto dto, CancellationToken ct = default);

    /// <summary>
    /// Mencari data registrasi kunjungan pasien non-aktif berdasarkan kata kunci (RegID, MR, Nama, TglLahir).
    /// </summary>
    Task<IEnumerable<AktivasiBerkasRegisterItemDto>> FindRegister(string keyword, CancellationToken ct = default);

    /// <summary>
    /// Mengambil daftar register yang sedang aktif kembali, terkelompok per unit layanan.
    /// </summary>
    Task<IEnumerable<AktivasiBerkasListReaktifDto>> ListReaktif(string? layananId, CancellationToken ct = default);

    /// <summary>
    /// Menonaktifkan kembali registrasi yang sedang aktif di backend EMR.
    /// </summary>
    Task<AktivasiBerkasMutationResult> Deactivate(string regId, string userrId, CancellationToken ct = default);

    /// <summary>
    /// Membersihkan/menonaktifkan seluruh register yang sedang aktif di semua unit layanan pada backend EMR.
    /// </summary>
    Task<AktivasiBerkasMutationResult> CleanUp(CancellationToken ct = default);
}
