using Bilreg.Domain.ApotekContext.QueueFeature;
using MediatR;

namespace Bilreg.Application.ApotekContext.QueueFeature;

#region Kiosk Intake DTOs

/// <summary>
/// Permintaan cetak / pengambilan nomor tiket antrean farmasi dari mesin kiosk mandiri.
/// </summary>
public record PharmacyQueueIntakeReq(
    string ServicePointId = "",
    string DepoFarmasiId = "",
    string UserId = "KIOSK",
    string RegId = "");

/// <summary>
/// Data tiket hasil alokasi on-demand mesin antrean farmasi.
/// </summary>
public record PharmacyQueueIntakeResponse(
    string AntrianId,
    int NoUrut,
    string QueueLabel,
    string ServicePointId,
    string ServicePointName,
    string QueuePrefix,
    ApotekPhaseEnum ApotekPhase,
    DateTime TakenAt,
    int WaitingAheadCount,
    string PatientName = "",
    string MedicalRecordNo = "",
    string Penjamin = "");

#endregion

#region Worklist Loket DTOs

/// <summary>
/// Kueri worklist antrean aktif untuk stasiun kerja petugas farmasi.
/// </summary>
public record PharmacyQueueWorklistQry(
    string DepoFarmasiId,
    string? FilterTab = null,
    ApotekPhaseEnum? Phase = null,
    IReadOnlyList<string>? ServicePointIds = null) : IRequest<PharmacyQueueWorklistResponse>;

/// <summary>
/// Baris item ringkasan tiket antrean pada panel worklist petugas apotek.
/// </summary>
public record PharmacyQueueWorklistItemResponse(
    string AntrianId,
    int NoUrut,
    string QueueLabel,
    string ServicePointId,
    string ServicePointName,
    ApotekPhaseEnum ApotekPhase,
    int CallCountPenerimaan,
    DateTime LastCalledPenerimaanAt,
    int CallCountSerah,
    DateTime LastCalledSerahAt,
    DateTime TakenAt,
    DateTime ServedAt,
    DateTime PreparedAt,
    DateTime DoneAt,
    bool PasienHadir,
    DateTime PasienHadirAt,
    IReadOnlyList<string> DemandIds,
    string PatientName,
    string MedicalRecordNo,
    string RegistrationId,
    string DokterName,
    string PoliName,
    string Penjamin,
    bool IsCito,
    bool IsPaymentOverridden,
    string PaymentOverrideReason);

/// <summary>
/// Respons kumpulan antrean worklist per depo farmasi.
/// </summary>
public record PharmacyQueueWorklistResponse(
    string DepoFarmasiId,
    IReadOnlyList<PharmacyQueueWorklistItemResponse> Items,
    int TotalCount,
    int WaitingCount,
    int PreparedCount);

#endregion

#region Calling Operations DTOs

/// <summary>
/// Permintaan pemanggilan nomor antrean berikutnya atau pemanggilan nomor spesifik ke loket fisik.
/// </summary>
public record PharmacyQueueCallReq(
    string LoketKey,
    string? AntrianId = null,
    int? NoUrut = null,
    byte[]? ExpectedRowVersion = null,
    string UserId = "");

/// <summary>
/// Permintaan pemanggilan ulang (Recall) nomor antrean yang sedang aktif di loket.
/// </summary>
public record PharmacyQueueRecallReq(
    string LoketKey,
    byte[] RowVersion,
    string UserId = "");

/// <summary>
/// Permintaan penandaan dimulainya pelayanan tatap muka di loket (Start Service / InService).
/// </summary>
public record PharmacyQueueStartServiceReq(
    string LoketKey,
    byte[] RowVersion,
    string UserId = "");

/// <summary>
/// Permintaan pelepasan klaim loket kembali ke status Idle (Release).
/// </summary>
public record PharmacyQueueReleaseReq(
    string LoketKey,
    byte[] RowVersion,
    string UserId = "");

/// <summary>
/// Permintaan menandai antrean tidak merespons (Skip) dan melepaskan klaim loket.
/// </summary>
public record PharmacyQueueSkipReq(
    string LoketKey,
    string Reason,
    byte[] RowVersion,
    string UserId = "");

/// <summary>
/// Data hasil mutasi operasional pemanggilan loket (termasuk token RowVersion dan versi audio pengumuman).
/// </summary>
public record PharmacyQueueLoketCallResponse(
    string LoketKey,
    string DisplayName,
    LoketTypeEnum LoketType,
    string AntrianId,
    int NoUrut,
    string QueueLabel,
    LoketClaimStateEnum ClaimState,
    long AnnouncementVersion,
    DateTime CalledAt,
    byte[] RowVersion,
    bool Success,
    string Message);

#endregion

#region Prescription & Demand Mapping DTOs

/// <summary>
/// Permintaan pengaitan tiket antrean farmasi ke dokumen permintaan obat (E-Resep / Jual Bebas).
/// </summary>
public record PharmacyQueueMapDemandReq(
    string AntrianId,
    int NoUrut,
    QueueDemandKindEnum DemandKind,
    string DemandId,
    string PasienTrackerId = "",
    QueueMappingMethodEnum MappingMethod = QueueMappingMethodEnum.Manual,
    string UserId = "");

/// <summary>
/// Data hasil pengaitan tiket antrean ke dokumen permintaan obat.
/// </summary>
public record PharmacyQueueMapDemandResponse(
    string AntrianId,
    int NoUrut,
    QueueDemandKindEnum DemandKind,
    string DemandId,
    DateTime MappedAt,
    bool Success);

#endregion

#region Pasien Hadir DTOs

/// <summary>
/// Permintaan penandaan kehadiran fisik pasien di depan loket penerimaan untuk resep yang belum ditelaah.
/// </summary>
public record PharmacyQueueFlagPasienHadirReq(
    QueueDemandKindEnum DemandKind,
    string DemandId,
    string AntrianId,
    int NoUrut,
    string UserId = "");

/// <summary>
/// Respons penandaan kehadiran fisik pasien yang akan disiarkan ke telaah resep.
/// </summary>
public record PharmacyQueueFlagPasienHadirResponse(
    QueueDemandKindEnum DemandKind,
    string DemandId,
    string AntrianId,
    int NoUrut,
    bool PasienHadir,
    DateTime PasienHadirAt,
    bool Success);

#endregion

#region Handover Resolution DTOs (Loket 2 Serah Obat)

/// <summary>
/// Permintaan penyelesaian penyerahan fisik obat di Loket 2 dengan evaluasi gating kasir/SEP.
/// </summary>
public record PharmacyQueueCompleteHandoverReq(
    string AntrianId,
    int NoUrut,
    string PenjualanId = "",
    string NoSep = "",
    bool IsPaymentOverridden = false,
    string PaymentOverrideReason = "",
    string PaymentOverridePharmacistId = "",
    string UserId = "");

/// <summary>
/// Data hasil penyelesaian penyerahan fisik obat di Loket 2.
/// </summary>
public record PharmacyQueueCompleteHandoverResponse(
    string AntrianId,
    int NoUrut,
    ApotekPhaseEnum ApotekPhase,
    DateTime DoneAt,
    bool IsPaymentOverridden,
    bool Success,
    string Message);

#endregion

#region Service Point DTOs

/// <summary>
/// Data konfigurasi service point antrean depo farmasi.
/// </summary>
public record PharmacyServicePointResponse(
    string ServicePointId,
    string DepoFarmasiId,
    string DisplayName,
    string QueuePrefix,
    bool Active);

#endregion

#region Loket Service Point DTOs

/// <summary>
/// Data konfigurasi daftar titik layanan yang dilayani oleh suatu loket fisik.
/// </summary>
public record LoketServicePointsResponse(
    string LoketKey,
    IReadOnlyList<string> ServicePointIds,
    byte[] RowVersion);

/// <summary>
/// Permintaan pembaruan daftar service point yang dilayani loket fisik.
/// </summary>
public record UpdateLoketServicePointsReq(
    IReadOnlyList<string> ServicePointIds,
    byte[] RowVersion,
    string UserId = "");

#endregion

