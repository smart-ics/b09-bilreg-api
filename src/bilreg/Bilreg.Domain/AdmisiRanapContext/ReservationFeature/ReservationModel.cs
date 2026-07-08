using Ardalis.GuardClauses;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.AdmisiRanapContext.ReservationFeature;

public record ReservationModel : IReservationKey
{
    private const string ID_PREFIX = "RSV";
    private const string EMPTY_REG_ID = "-";
    private static readonly DateTime EmptyDate = new(3000, 1, 1);

    public ReservationModel(
        string reservationId,
        ReservationStatusEnum reservationStatus,
        PasienReff pasien,
        DateTime plannedDate,
        KelasReff kelasRawat,
        BangsalReff bangsal,
        string realizedRegId,
        AuditTrailType auditTrail)
    {
        ReservationId = reservationId;
        ReservationStatus = reservationStatus;
        Pasien = pasien;
        PlannedDate = plannedDate;
        KelasRawat = kelasRawat;
        Bangsal = bangsal;
        RealizedRegId = realizedRegId;
        AuditTrail = auditTrail;
    }

    #region CREATION

    public static ReservationModel Create(
        PasienReff pasien,
        DateTime plannedDate,
        KelasReff kelasRawat,
        BangsalReff bangsal,
        string auditUserId)
    {
        Guard.Against.Null(pasien);
        Guard.Against.Null(kelasRawat);
        Guard.Against.Null(bangsal);
        Guard.Against.NullOrWhiteSpace(auditUserId);

        var now = DateTime.Now;
        return new ReservationModel(
            NunaId.New(ID_PREFIX),
            ReservationStatusEnum.Reserved,
            pasien,
            plannedDate,
            kelasRawat,
            bangsal,
            EMPTY_REG_ID,
            AuditTrailType.Create(auditUserId, now));
    }

    public static ReservationModel Default => new(
        "-",
        ReservationStatusEnum.Reserved,
        new PasienReff("-", "-", new DateOnly(3000, 1, 1), "-"),
        EmptyDate,
        new KelasReff("-", "-"),
        new BangsalReff("-", "-"),
        EMPTY_REG_ID,
        AuditTrailType.Default);

    public static IReservationKey Key(string id) => Default with { ReservationId = id };

    #endregion

    #region PROPERTIES

    public string ReservationId { get; init; }
    public ReservationStatusEnum ReservationStatus { get; init; }
    public PasienReff Pasien { get; init; }
    public DateTime PlannedDate { get; init; }
    public KelasReff KelasRawat { get; init; }
    public BangsalReff Bangsal { get; init; }
    public string RealizedRegId { get; init; }
    public AuditTrailType AuditTrail { get; init; }

    #endregion

    #region BEHAVIOUR

    public ReservationModel Maintain(
        DateTime plannedDate,
        KelasReff kelasRawat,
        BangsalReff bangsal,
        string auditUserId)
    {
        Guard.Against.Null(kelasRawat);
        Guard.Against.Null(bangsal);
        Guard.Against.NullOrWhiteSpace(auditUserId);
        EnsureEditable();

        if (ReservationStatus is not ReservationStatusEnum.Reserved and not ReservationStatusEnum.Maintained)
            throw new InvalidOperationException(
                $"Reservation {ReservationId} harus Reserved atau Maintained untuk dirawat (status saat ini: {ReservationStatus}).");

        var audit = AuditTrail;
        audit.Modif(auditUserId, DateTime.Now);
        return WithState(
            ReservationStatusEnum.Maintained,
            plannedDate,
            kelasRawat,
            bangsal,
            RealizedRegId,
            audit);
    }

    public ReservationModel Realize(string regId, string auditUserId)
    {
        Guard.Against.NullOrWhiteSpace(regId);
        Guard.Against.NullOrWhiteSpace(auditUserId);

        if (ReservationStatus == ReservationStatusEnum.Cancelled)
            throw new InvalidOperationException(
                $"Reservation {ReservationId} sudah dibatalkan; tidak dapat direalisasi.");

        if (ReservationStatus != ReservationStatusEnum.Maintained)
            throw new InvalidOperationException(
                $"Reservation {ReservationId} harus Maintained untuk direalisasi (status saat ini: {ReservationStatus}).");

        if (!string.IsNullOrWhiteSpace(RealizedRegId) && RealizedRegId != EMPTY_REG_ID)
            throw new InvalidOperationException(
                $"Reservation {ReservationId} sudah direalisasi oleh Reg {RealizedRegId}.");

        var audit = AuditTrail;
        audit.Modif(auditUserId, DateTime.Now);
        return WithState(
            ReservationStatusEnum.Realized,
            PlannedDate,
            KelasRawat,
            Bangsal,
            regId,
            audit);
    }

    public ReservationModel Cancel(string auditUserId)
    {
        Guard.Against.NullOrWhiteSpace(auditUserId);
        EnsureEditable();

        var audit = AuditTrail;
        audit.Modif(auditUserId, DateTime.Now);
        return WithState(
            ReservationStatusEnum.Cancelled,
            PlannedDate,
            KelasRawat,
            Bangsal,
            RealizedRegId,
            audit);
    }

    #endregion

    #region HELPERS

    private void EnsureEditable()
    {
        if (ReservationStatus is ReservationStatusEnum.Realized or ReservationStatusEnum.Cancelled)
            throw new InvalidOperationException(
                $"Reservation {ReservationId} berstatus {ReservationStatus}; perubahan tidak diperbolehkan.");
    }

    private ReservationModel WithState(
        ReservationStatusEnum status,
        DateTime plannedDate,
        KelasReff kelasRawat,
        BangsalReff bangsal,
        string realizedRegId,
        AuditTrailType audit) =>
        new(
            ReservationId,
            status,
            Pasien,
            plannedDate,
            kelasRawat,
            bangsal,
            realizedRegId,
            audit);

    #endregion
}
