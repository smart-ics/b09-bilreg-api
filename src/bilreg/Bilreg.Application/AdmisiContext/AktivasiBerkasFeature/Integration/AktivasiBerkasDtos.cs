using System.Text.Json.Serialization;

namespace Bilreg.Application.AdmisiContext.AktivasiBerkasFeature.Integration;

/// <summary>
/// Referensi item registrasi dalam transaksi aktivasi berkas.
/// </summary>
public record AktivasiBerkasRegisterRefDto
{
    public string RegId { get; init; } = string.Empty;
    public string TglVoid { get; init; } = string.Empty;
    public bool IsVoid => !string.IsNullOrWhiteSpace(TglVoid) && TglVoid != "3000-01-01" && TglVoid != "01/01/3000";

    public AktivasiBerkasRegisterRefDto() { }
    public AktivasiBerkasRegisterRefDto(string regId) => RegId = regId;
    public AktivasiBerkasRegisterRefDto(string regId, string tglVoid)
    {
        RegId = regId;
        TglVoid = tglVoid;
    }
}

/// <summary>
/// Item riwayat transaksi aktivasi berkas rekam medis.
/// </summary>
public record AktivasiBerkasListItemDto
{
    public string AktifasiBerkasId { get; init; } = string.Empty;
    public string TglJam { get; init; } = string.Empty;
    public string ReqUserrId { get; init; } = string.Empty;
    public string ReqUserrName { get; init; } = string.Empty;
    public string AktifasiUserrId { get; init; } = string.Empty;
    public string AktifasiUserrName { get; init; } = string.Empty;
    public string TglAktif { get; init; } = string.Empty;
    public string LayananId { get; init; } = string.Empty;
    public string LayananName { get; init; } = string.Empty;
    public IEnumerable<AktivasiBerkasItemDetilDto> ListBerkas { get; init; } = [];
}

/// <summary>
/// Detail item berkas/pasien dalam riwayat aktivasi berkas.
/// </summary>
public record AktivasiBerkasItemDetilDto
{
    public string AktifasiBerkasId { get; init; } = string.Empty;
    public string RegId { get; init; } = string.Empty;
    public string PasienId { get; init; } = string.Empty;
    public string PasienName { get; init; } = string.Empty;
    public string DokterId { get; init; } = string.Empty;
    public string DokterName { get; init; } = string.Empty;
    public string TipeJaminan { get; init; } = string.Empty;
    public string TglJamMasuk { get; init; } = string.Empty;
    public string TglDeaktif { get; init; } = string.Empty;
    public string DeactifUserrId { get; init; } = string.Empty;
}

/// <summary>
/// Data detail transaksi aktivasi berkas untuk inspeksi dan mode koreksi/edit.
/// </summary>
public record AktivasiBerkasDetailDto
{
    public string AktifasiBerkasId { get; init; } = string.Empty;
    public string TglJam { get; init; } = string.Empty;
    public string ReqUserrId { get; init; } = string.Empty;
    public string ReqUserrName { get; init; } = string.Empty;
    public string AktifasiUserrId { get; init; } = string.Empty;
    public string AktifasiUserrName { get; init; } = string.Empty;
    public string TglAktif { get; init; } = string.Empty;
    public string LayananId { get; init; } = string.Empty;
    public string LayananName { get; init; } = string.Empty;
    public IEnumerable<AktivasiBerkasItemDetilDto> ListBerkas { get; init; } = [];
}

/// <summary>
/// Payload pembuatan transaksi aktivasi berkas baru.
/// </summary>
public record AktivasiBerkasSaveDto
{
    public string TglJam { get; init; } = string.Empty;
    public string ReqUserrId { get; init; } = string.Empty;
    public string TglAktif { get; init; } = string.Empty;
    public string AktifasiUserrId { get; init; } = string.Empty;
    public string LayananId { get; init; } = string.Empty;
    public IEnumerable<AktivasiBerkasRegisterRefDto> ListRegister { get; init; } = [];
}

/// <summary>
/// Payload pembaruan transaksi aktivasi berkas eksisting.
/// </summary>
public record AktivasiBerkasUpdateDto
{
    public string TglJam { get; init; } = string.Empty;
    public string ReqUserrId { get; init; } = string.Empty;
    public string TglAktif { get; init; } = string.Empty;
    public string AktifasiUserrId { get; init; } = string.Empty;
    public string LayananId { get; init; } = string.Empty;
    public IEnumerable<AktivasiBerkasRegisterRefDto> ListRegister { get; init; } = [];
}

/// <summary>
/// Hasil mutasi simpan atau pembaruan aktivasi berkas.
/// </summary>
public record AktivasiBerkasMutationResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? AktifasiBerkasId { get; init; }

    [JsonIgnore]
    public int? StatusCode { get; init; }

    public AktivasiBerkasMutationResult() { }
    public AktivasiBerkasMutationResult(bool success, string message, string? aktifasiBerkasId = null)
    {
        Success = success;
        Message = message;
        AktifasiBerkasId = aktifasiBerkasId;
    }

    public static AktivasiBerkasMutationResult Ok(string id, string message = "Transaksi aktivasi berhasil diproses") =>
        new(true, message, id);

    public static AktivasiBerkasMutationResult Fail(string message, int statusCode = 400) =>
        new(false, message, null) { StatusCode = statusCode };
}

/// <summary>
/// Detail layanan dan dokter pada item registrasi hasil pencarian register non-aktif.
/// </summary>
public record AktivasiBerkasRegisterLayananDto
{
    public string LayananId { get; init; } = string.Empty;
    public string LayananName { get; init; } = string.Empty;
    public string KamarId { get; init; } = string.Empty;
    public string KamarName { get; init; } = string.Empty;
    public string DokterId { get; init; } = string.Empty;
    public string DokterName { get; init; } = string.Empty;
    public string SesionPoliId { get; init; } = string.Empty;
    public string AntrianX1Id { get; init; } = string.Empty;
}

/// <summary>
/// Item data registrasi pasien hasil pencarian register non-aktif untuk aktivasi berkas.
/// </summary>
public record AktivasiBerkasRegisterItemDto
{
    public string RegId { get; init; } = string.Empty;
    public string BookingId { get; init; } = string.Empty;
    public string TglReg { get; init; } = string.Empty;
    public string JamReg { get; init; } = string.Empty;
    public string KodeMr { get; init; } = string.Empty;
    public string NamaPasien { get; init; } = string.Empty;
    public string KodeTipeJaminan { get; init; } = string.Empty;
    public string NamaTipeJaminan { get; init; } = string.Empty;
    public string TglVoid { get; init; } = string.Empty;
    public string JamVoid { get; init; } = string.Empty;
    public string UserrIdVoid { get; init; } = string.Empty;
    public bool IsVoid => !string.IsNullOrWhiteSpace(TglVoid) && TglVoid != "3000-01-01" && TglVoid != "01/01/3000";
    public string TglKeluar { get; init; } = string.Empty;
    public string JamKeluar { get; init; } = string.Empty;
    public string Umur { get; init; } = string.Empty;
    public string JenisReg { get; init; } = string.Empty;
    public string LayananId { get; init; } = string.Empty;
    public string LayananName { get; init; } = string.Empty;
    public string LayananList { get; init; } = string.Empty;
    public string LayananAkhirId { get; init; } = string.Empty;
    public string LayananAkhirName { get; init; } = string.Empty;
    public string NoSep { get; init; } = string.Empty;
    public int KodeJenisReg { get; init; }
    public string KodeMedis { get; init; } = string.Empty;
    public string NamaMedis { get; init; } = string.Empty;
    public IEnumerable<AktivasiBerkasRegisterLayananDto> ListLayanan { get; init; } = [];
}

/// <summary>
/// Data daftar register yang sedang aktif kembali terkelompok per unit layanan.
/// </summary>
public record AktivasiBerkasListReaktifDto(
    string LayananId,
    string LayananName,
    IEnumerable<AktivasiBerkasItemDetilDto> ListBerkas)
{
    public AktivasiBerkasListReaktifDto() : this(string.Empty, string.Empty, []) { }
}

/// <summary>
/// Payload permintaan penonaktifan kembali registrasi yang sedang aktif.
/// </summary>
public record AktivasiBerkasDeactivateDto(
    string RegId,
    string UserrId)
{
    public AktivasiBerkasDeactivateDto() : this(string.Empty, string.Empty) { }
}
