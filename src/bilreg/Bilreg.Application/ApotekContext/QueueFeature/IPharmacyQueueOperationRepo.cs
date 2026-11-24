using Bilreg.Domain.ApotekContext.QueueFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Application.ApotekContext.QueueFeature;

/// <summary>
/// Status sesi pemanggilan aktif loket fisik farmasi (BILRG_AptLoketCurrentCall).
/// </summary>
public record PharmacyLoketSession(
    string LoketKey,
    LoketTypeEnum LoketType,
    string DisplayName,
    string DepoFarmasiId,
    string AntrianId,
    int NoUrut,
    LoketClaimStateEnum ClaimState,
    bool IsActive,
    long AnnouncementVersion,
    DateTime CalledAt,
    DateTime ServiceStartedAt,
    DateTime ReleasedAt,
    byte[] RowVersion);

/// <summary>
/// Kontrak operasi pemanggilan loket berkonkurensi tinggi dengan proteksi token ROWVERSION.
/// </summary>
public interface IPharmacyQueueOperationRepo
{
    /// <summary>
    /// Mengambil status sesi pemanggilan terkini pada loket fisik tertentu.
    /// </summary>
    MayBe<PharmacyLoketSession> GetLoket(string loketKey);

    /// <summary>
    /// Mengambil seluruh status loket fisik yang terdaftar pada suatu depo farmasi.
    /// </summary>
    IReadOnlyList<PharmacyLoketSession> ListLoketByDepo(string depoFarmasiId);

    /// <summary>
    /// Mencoba mengklaim pemanggilan nomor tiket antrean ke loket fisik farmasi dengan proteksi token ROWVERSION.
    /// </summary>
    bool TryCallLoket(
        string loketKey,
        string antrianId,
        int noUrut,
        byte[] expectedRowVersion,
        string userId,
        DateTime at);

    /// <summary>
    /// Mencoba memanggil ulang nomor tiket antrean yang sedang aktif di loket.
    /// </summary>
    bool TryRecallLoket(
        string loketKey,
        byte[] expectedRowVersion,
        string userId,
        DateTime at);

    /// <summary>
    /// Mencoba menandai dimulainya pelayanan tatap muka pada loket fisik (transisi ke InService).
    /// </summary>
    bool TryStartServiceLoket(
        string loketKey,
        byte[] expectedRowVersion,
        string userId,
        DateTime at);

    /// <summary>
    /// Mencoba melepaskan klaim loket kembali ke status Idle (pelepasan klaim atau selesai pelayanan).
    /// </summary>
    bool TryReleaseLoket(
        string loketKey,
        byte[] expectedRowVersion,
        string userId,
        DateTime at);

    /// <summary>
    /// Mencoba menandai nomor tiket antrean dilewati (Skip) dan melepaskan klaim loket menjadi Idle.
    /// </summary>
    bool TrySkipLoket(
        string loketKey,
        string reason,
        byte[] expectedRowVersion,
        string userId,
        DateTime at);
}
