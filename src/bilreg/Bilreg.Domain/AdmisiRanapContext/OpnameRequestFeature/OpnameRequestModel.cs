using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.AdmisiRanapContext.OpnameRequestFeature;

public record OpnameRequestModel : IOpnameRequestKey
{
    private const string ID_PREFIX = "OPN";
    private const string EMPTY_REG_ID = "-";

    public OpnameRequestModel(
        string opnameRequestId,
        OpnameRequestStatusEnum opnameRequestStatus,
        PasienReff pasien,
        PpaReff dokter,
        DateTime plannedDate,
        string clinicalNotes,
        string fulfilledRegId,
        AuditTrailType auditTrail)
    {
        OpnameRequestId = opnameRequestId;
        OpnameRequestStatus = opnameRequestStatus;
        Pasien = pasien;
        Dokter = dokter;
        PlannedDate = plannedDate;
        ClinicalNotes = clinicalNotes ?? "";
        FulfilledRegId = fulfilledRegId;
        AuditTrail = auditTrail;
    }

    #region CREATION

    public static OpnameRequestModel Create(
        PasienReff pasien,
        PpaReff dokter,
        DateTime plannedDate,
        string clinicalNotes,
        string auditUserId)
    {
        Guard.Against.Null(pasien);
        Guard.Against.Null(dokter);
        Guard.Against.NullOrWhiteSpace(auditUserId);

        var now = DateTime.Now;
        return new OpnameRequestModel(
            NunaId.New(ID_PREFIX),
            OpnameRequestStatusEnum.Requested,
            pasien,
            dokter,
            plannedDate,
            clinicalNotes ?? "",
            EMPTY_REG_ID,
            AuditTrailType.Create(auditUserId, now));
    }

    public static OpnameRequestModel Default => new(
        "-",
        OpnameRequestStatusEnum.Requested,
        new PasienReff("-", "-", new DateOnly(3000, 1, 1), "-"),
        new PpaReff("-", "-"),
        new DateTime(3000,1,1),
        "",
        EMPTY_REG_ID,
        AuditTrailType.Default);

    public static IOpnameRequestKey Key(string id) => Default with { OpnameRequestId = id };

    #endregion

    #region PROPERTIES

    public string OpnameRequestId { get; init; }
    public OpnameRequestStatusEnum OpnameRequestStatus { get; init; }
    public PasienReff Pasien { get; init; }
    public PpaReff Dokter { get; init; }
    public DateTime PlannedDate { get; init; }
    public string ClinicalNotes { get; init; }
    public string FulfilledRegId { get; init; }
    public AuditTrailType AuditTrail { get; init; }

    #endregion

    #region BEHAVIOUR

    public OpnameRequestModel Cancel(string auditUserId)
    {
        Guard.Against.NullOrWhiteSpace(auditUserId);
        EnsureStatus(OpnameRequestStatusEnum.Requested, "dibatalkan");

        AuditTrail.Batal(auditUserId, DateTime.Now);
        return WithState(OpnameRequestStatusEnum.Cancelled, FulfilledRegId, AuditTrail);
    }

    public OpnameRequestModel Fulfill(string regId, string auditUserId)
    {
        Guard.Against.NullOrWhiteSpace(regId);
        Guard.Against.NullOrWhiteSpace(auditUserId);

        if (OpnameRequestStatus == OpnameRequestStatusEnum.Cancelled)
            throw new InvalidOperationException(
                $"Opname Request {OpnameRequestId} sudah dibatalkan; tidak dapat dipenuhi.");

        if (!string.IsNullOrWhiteSpace(FulfilledRegId) && FulfilledRegId != EMPTY_REG_ID)
            throw new InvalidOperationException(
                $"Opname Request {OpnameRequestId} sudah dipenuhi oleh Reg {FulfilledRegId}.");

        EnsureStatus(OpnameRequestStatusEnum.Requested, "dipenuhi");

        var audit = AuditTrail;
        audit.Modif(auditUserId, DateTime.Now);
        return WithState(OpnameRequestStatusEnum.Fulfilled, regId, audit);
    }

    public OpnameRequestModel Restore(string regId, string userId, DateTime timestamp)
    {
        Guard.Against.NullOrWhiteSpace(regId);
        Guard.Against.NullOrWhiteSpace(userId);

        if (OpnameRequestStatus != OpnameRequestStatusEnum.Fulfilled
            || FulfilledRegId != regId)
            throw new InvalidOperationException(
                $"Opname Request {OpnameRequestId} harus Fulfilled oleh Reg {regId} untuk dipulihkan.");

        var audit = new AuditTrailType(
            AuditTrail.Created,
            AuditTrail.Modified,
            AuditTrail.Voided);
        audit.Modif(userId, timestamp);
        return WithState(OpnameRequestStatusEnum.Requested, EMPTY_REG_ID, audit);
    }

    #endregion

    #region HELPERS

    private void EnsureStatus(OpnameRequestStatusEnum expected, string action)
    {
        if (OpnameRequestStatus != expected)
            throw new InvalidOperationException(
                $"Opname Request {OpnameRequestId} harus {expected} untuk {action} (status saat ini: {OpnameRequestStatus}).");
    }

    private OpnameRequestModel WithState(
        OpnameRequestStatusEnum status,
        string fulfilledRegId,
        AuditTrailType audit) =>
        new(
            OpnameRequestId,
            status,
            Pasien,
            Dokter,
            PlannedDate,
            ClinicalNotes,
            fulfilledRegId,
            audit);

    #endregion
}
