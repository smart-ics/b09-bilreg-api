using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Domain.AdmisiRanapContext.Shared;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.AdmisiRanapContext.AdmissionFeature;

public record AdmissionModel : IRegKey
{
    private const string EMPTY_REF_ID = "-";
    private static readonly DateTime EmptyDate = new(3000, 1, 1);

    public AdmissionModel(
        string regId,
        AdmissionStatusEnum admissionStatus,
        PasienReff pasien,
        string opnameRequestId,
        string reservationId,
        KelasDkType kelasDk,
        BangsalReff bangsal,
        DateTime admissionDate,
        AuditTrailType auditTrail,
        AdmissionSourceEnum admissionSource = AdmissionSourceEnum.Admission)
    {
        RegId = regId;
        AdmissionStatus = admissionStatus;
        AdmissionSource = admissionSource;
        Pasien = pasien;
        OpnameRequestId = opnameRequestId;
        ReservationId = reservationId;
        KelasDk = kelasDk;
        Bangsal = bangsal;
        AdmissionDate = admissionDate;
        AuditTrail = auditTrail;
    }

    #region CREATION

    public static AdmissionModel Admit(
        PasienReff pasien,
        KelasDkType kelasDk,
        BangsalReff bangsal,
        string? opnameRequestId,
        string? reservationId,
        string auditUserId)
    {
        Guard.Against.Null(pasien);
        Guard.Against.Null(kelasDk);
        Guard.Against.Null(bangsal);
        Guard.Against.NullOrWhiteSpace(auditUserId);

        var now = DateTime.Now;
        var regId = NunaId.NewLegacyCompact("RG");
        return new AdmissionModel(
            regId,
            AdmissionStatusEnum.Admitted,
            pasien,
            string.IsNullOrWhiteSpace(opnameRequestId) ? EMPTY_REF_ID : opnameRequestId,
            string.IsNullOrWhiteSpace(reservationId) ? EMPTY_REF_ID : reservationId,
            kelasDk,
            bangsal,
            now,
            AuditTrailType.Create(auditUserId, now),
            AdmissionSourceEnum.Admission);
    }

    public static AdmissionModel CreateFromLegacyRegistration(
        RegModel reg,
        string auditUserId)
    {
        Guard.Against.Null(reg);
        Guard.Against.NullOrWhiteSpace(auditUserId);

        if (reg.JenisReg != JenisRegEnum.RegInap)
            throw new InvalidOperationException("Admission hanya dibuat untuk RegInap.");

        if (IsEmpty(reg.KelasDk.KelasDkId))
            throw new InvalidOperationException(
                $"KelasDk untuk RegInap '{reg.RegId}' belum terpetakan.");

        if (IsEmpty(reg.Bangsal.BangsalId))
            throw new InvalidOperationException(
                $"Bangsal untuk RegInap '{reg.RegId}' belum terpetakan.");

        var now = DateTime.Now;
        return new AdmissionModel(
            reg.RegId,
            AdmissionStatusEnum.Admitted,
            reg.Pasien,
            EMPTY_REF_ID,
            EMPTY_REF_ID,
            reg.KelasDk,
            reg.Bangsal,
            now,
            AuditTrailType.Create(auditUserId, now),
            AdmissionSourceEnum.Legacy);
    }

    public static AdmissionModel Default => new(
        "-",
        AdmissionStatusEnum.Admitted,
        new PasienReff("-", "-", new DateOnly(3000, 1, 1), "-"),
        EMPTY_REF_ID,
        EMPTY_REF_ID,
        KelasDkType.Default,
        new BangsalReff("-", "-"),
        EmptyDate,
        AuditTrailType.Default,
        AdmissionSourceEnum.Admission);

    public static IRegKey Key(string id) => Default with { RegId = id };

    #endregion

    #region PROPERTIES

    public string RegId { get; init; }
    public AdmissionStatusEnum AdmissionStatus { get; init; }
    public AdmissionSourceEnum AdmissionSource { get; init; }
    public PasienReff Pasien { get; init; }
    public string OpnameRequestId { get; init; }
    public string ReservationId { get; init; }
    public KelasDkType KelasDk { get; init; }
    public BangsalReff Bangsal { get; init; }
    public DateTime AdmissionDate { get; init; }
    public AuditTrailType AuditTrail { get; init; }

    #endregion

    #region BEHAVIOUR

    public AdmissionModel Update(KelasDkType kelasDk, BangsalReff bangsal, string auditUserId)
    {
        Guard.Against.Null(kelasDk);
        Guard.Against.Null(bangsal);
        Guard.Against.NullOrWhiteSpace(auditUserId);
        EnsureMutable();

        if (AdmissionStatus is not AdmissionStatusEnum.Admitted
            and not AdmissionStatusEnum.Updated
            and not AdmissionStatusEnum.Waiting)
            throw new InvalidOperationException(
                $"Admission {RegId} tidak dapat diperbarui pada status {AdmissionStatus}.");

        var audit = AuditTrail;
        audit.Modif(auditUserId, DateTime.Now);
        return WithState(AdmissionStatusEnum.Updated, kelasDk, bangsal, audit);
    }

    public AdmissionModel MarkWaiting(string auditUserId)
    {
        Guard.Against.NullOrWhiteSpace(auditUserId);
        EnsureMutable();

        if (AdmissionStatus is not AdmissionStatusEnum.Admitted and not AdmissionStatusEnum.Updated)
            throw new InvalidOperationException(
                $"Admission {RegId} harus Admitted atau Updated untuk ditandai Waiting (status saat ini: {AdmissionStatus}).");

        var audit = AuditTrail;
        audit.Modif(auditUserId, DateTime.Now);
        return WithState(AdmissionStatusEnum.Waiting, KelasDk, Bangsal, audit);
    }

    public AdmissionModel Complete(string auditUserId)
    {
        Guard.Against.NullOrWhiteSpace(auditUserId);
        EnsureMutable();

        var audit = AuditTrail;
        audit.Modif(auditUserId, DateTime.Now);
        return WithState(AdmissionStatusEnum.Completed, KelasDk, Bangsal, audit);
    }

    public AdmissionModel Cancel(string userId, string reason, DateTime timestamp)
    {
        Guard.Against.NullOrWhiteSpace(userId);
        Guard.Against.NullOrWhiteSpace(reason);
        EnsureMutable();

        if (AdmissionStatus is not AdmissionStatusEnum.Admitted
            and not AdmissionStatusEnum.Updated
            and not AdmissionStatusEnum.Waiting)
            throw new InvalidOperationException(
                $"Admission {RegId} tidak dapat dibatalkan pada status {AdmissionStatus}.");

        var audit = CopyAuditTrail();
        audit.Batal(userId, timestamp);
        return WithState(AdmissionStatusEnum.Cancelled, KelasDk, Bangsal, audit);
    }

    // Retained for existing callers pending the coordinated-cancellation orchestrator.
    public AdmissionModel Cancel(string auditUserId)
    {
        Guard.Against.NullOrWhiteSpace(auditUserId);
        EnsureMutable();

        var audit = CopyAuditTrail();
        audit.Modif(auditUserId, DateTime.Now);
        return WithState(AdmissionStatusEnum.Cancelled, KelasDk, Bangsal, audit);
    }

    #endregion

    #region HELPERS

    private void EnsureMutable()
    {
        if (AdmissionStatus is AdmissionStatusEnum.Completed or AdmissionStatusEnum.Cancelled)
            throw new InvalidOperationException(
                $"Admission {RegId} berstatus {AdmissionStatus}; perubahan tidak diperbolehkan.");
    }

    private static bool IsEmpty(string value) =>
        string.IsNullOrWhiteSpace(value) || value == EMPTY_REF_ID;

    private AuditTrailType CopyAuditTrail() => new(
        AuditTrail.Created,
        AuditTrail.Modified,
        AuditTrail.Voided);

    private AdmissionModel WithState(
        AdmissionStatusEnum status,
        KelasDkType kelasDk,
        BangsalReff bangsal,
        AuditTrailType audit) =>
        new(
            RegId,
            status,
            Pasien,
            OpnameRequestId,
            ReservationId,
            kelasDk,
            bangsal,
            AdmissionDate,
            audit,
            AdmissionSource);

    #endregion
}
