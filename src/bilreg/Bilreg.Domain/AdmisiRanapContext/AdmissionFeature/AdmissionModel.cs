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
    private const string EmptyRefId = "-";
    private static readonly DateTime EmptyDate = new(3000, 1, 1);

    public AdmissionModel(
        string regId,
        AdmissionStatusEnum admissionStatus,
        PasienReff pasien,
        string opnameRequestId,
        string reservationId,
        KelasReff kelasRawat,
        BangsalReff bangsal,
        DateTime admissionDate,
        AuditTrailType auditTrail)
    {
        RegId = regId;
        AdmissionStatus = admissionStatus;
        Pasien = pasien;
        OpnameRequestId = opnameRequestId;
        ReservationId = reservationId;
        KelasRawat = kelasRawat;
        Bangsal = bangsal;
        AdmissionDate = admissionDate;
        AuditTrail = auditTrail;
    }

    #region CREATION

    public static AdmissionModel Admit(
        PasienReff pasien,
        KelasReff kelasRawat,
        BangsalReff bangsal,
        string? opnameRequestId,
        string? reservationId,
        string auditUserId)
    {
        Guard.Against.Null(pasien);
        Guard.Against.Null(kelasRawat);
        Guard.Against.Null(bangsal);
        Guard.Against.NullOrWhiteSpace(auditUserId);

        var now = DateTime.Now;
        var regId = NunaId.NewLegacyCompact("RG");
        return new AdmissionModel(
            regId,
            AdmissionStatusEnum.Admitted,
            pasien,
            string.IsNullOrWhiteSpace(opnameRequestId) ? EmptyRefId : opnameRequestId,
            string.IsNullOrWhiteSpace(reservationId) ? EmptyRefId : reservationId,
            kelasRawat,
            bangsal,
            now,
            AuditTrailType.Create(auditUserId, now));
    }

    public static AdmissionModel Default => new(
        "-",
        AdmissionStatusEnum.Admitted,
        new PasienReff("-", "-", new DateOnly(3000, 1, 1), "-"),
        EmptyRefId,
        EmptyRefId,
        new KelasReff("-", "-"),
        new BangsalReff("-", "-"),
        EmptyDate,
        AuditTrailType.Default);

    public static IRegKey Key(string id) => Default with { RegId = id };

    #endregion

    #region PROPERTIES

    public string RegId { get; init; }
    public AdmissionStatusEnum AdmissionStatus { get; init; }
    public PasienReff Pasien { get; init; }
    public string OpnameRequestId { get; init; }
    public string ReservationId { get; init; }
    public KelasReff KelasRawat { get; init; }
    public BangsalReff Bangsal { get; init; }
    public DateTime AdmissionDate { get; init; }
    public AuditTrailType AuditTrail { get; init; }

    #endregion

    #region BEHAVIOUR

    public AdmissionModel Update(KelasReff kelasRawat, BangsalReff bangsal, string auditUserId)
    {
        Guard.Against.Null(kelasRawat);
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
        return WithState(AdmissionStatusEnum.Updated, kelasRawat, bangsal, audit);
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
        return WithState(AdmissionStatusEnum.Waiting, KelasRawat, Bangsal, audit);
    }

    public AdmissionModel Complete(string auditUserId)
    {
        Guard.Against.NullOrWhiteSpace(auditUserId);
        EnsureMutable();

        var audit = AuditTrail;
        audit.Modif(auditUserId, DateTime.Now);
        return WithState(AdmissionStatusEnum.Completed, KelasRawat, Bangsal, audit);
    }

    public AdmissionModel Cancel(string auditUserId)
    {
        Guard.Against.NullOrWhiteSpace(auditUserId);
        EnsureMutable();

        var audit = AuditTrail;
        audit.Modif(auditUserId, DateTime.Now);
        return WithState(AdmissionStatusEnum.Cancelled, KelasRawat, Bangsal, audit);
    }

    #endregion

    #region HELPERS

    private void EnsureMutable()
    {
        if (AdmissionStatus is AdmissionStatusEnum.Completed or AdmissionStatusEnum.Cancelled)
            throw new InvalidOperationException(
                $"Admission {RegId} berstatus {AdmissionStatus}; perubahan tidak diperbolehkan.");
    }

    private AdmissionModel WithState(
        AdmissionStatusEnum status,
        KelasReff kelasRawat,
        BangsalReff bangsal,
        AuditTrailType audit) =>
        new(
            RegId,
            status,
            Pasien,
            OpnameRequestId,
            ReservationId,
            kelasRawat,
            bangsal,
            AdmissionDate,
            audit);

    #endregion
}
