using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.AdmisiRanapContext.InpatientOrderFeature;

public record InpatientOrderModel : IInpatientOrderKey
{
    private const string ID_PREFIX = "OPN";
    private const string EMPTY_REF_ID = "-";
    private static readonly DateTime EmptyDate = new(3000, 1, 1);

    public InpatientOrderModel(
        string inpatientOrderId,
        OrderSourceEnum orderSource,
        InpatientOrderStatusEnum orderStatus,
        PasienReff pasien,
        PpaReff dokter,
        DateTime plannedDate,
        KelasReff kelasRawat,
        BangsalReff bangsal,
        string clinicalNotes,
        InpatientOrderInsuranceModel insurance,
        string fulfilledRegId,
        string externalReferenceId,
        string externalSystemSource,
        AuditTrailType auditTrail)
    {
        InpatientOrderId = inpatientOrderId;
        OrderSource = orderSource;
        OrderStatus = orderStatus;
        Pasien = pasien;
        Dokter = dokter;
        PlannedDate = plannedDate;
        KelasRawat = kelasRawat;
        Bangsal = bangsal;
        ClinicalNotes = clinicalNotes ?? "";
        Insurance = insurance ?? InpatientOrderInsuranceModel.Default;
        FulfilledRegId = string.IsNullOrWhiteSpace(fulfilledRegId) ? EMPTY_REF_ID : fulfilledRegId;
        ExternalReferenceId = string.IsNullOrWhiteSpace(externalReferenceId) ? EMPTY_REF_ID : externalReferenceId;
        ExternalSystemSource = externalSystemSource ?? "";
        AuditTrail = auditTrail;
    }

    #region CREATION

    public static InpatientOrderModel Create(
        OrderSourceEnum orderSource,
        PasienReff pasien,
        PpaReff? dokter,
        DateTime plannedDate,
        string clinicalNotes,
        string auditUserId,
        KelasReff? kelasRawat = null,
        BangsalReff? bangsal = null,
        InpatientOrderInsuranceModel? insurance = null,
        string externalReferenceId = "-",
        string externalSystemSource = "",
        DateTime createdAt = default)
    {
        Guard.Against.Null(pasien, nameof(pasien));
        Guard.Against.NullOrWhiteSpace(auditUserId, nameof(auditUserId));

        return new InpatientOrderModel(
            NunaId.New(ID_PREFIX),
            orderSource,
            InpatientOrderStatusEnum.Requested,
            pasien,
            dokter ?? new PpaReff("-", "-"),
            plannedDate,
            kelasRawat ?? new KelasReff("-", "-"),
            bangsal ?? new BangsalReff("-", "-"),
            clinicalNotes ?? "",
            insurance ?? InpatientOrderInsuranceModel.Default,
            EMPTY_REF_ID,
            externalReferenceId,
            externalSystemSource,
            AuditTrailType.Create(auditUserId, createdAt));
    }

    public static InpatientOrderModel CreateBooking(
        PasienReff pasien,
        DateTime plannedDate,
        KelasReff kelasRawat,
        BangsalReff bangsal,
        string auditUserId,
        PpaReff? dokter = null,
        string clinicalNotes = "",
        InpatientOrderInsuranceModel? insurance = null,
        DateTime createdAt = default)
    {
        Guard.Against.Null(kelasRawat, nameof(kelasRawat));
        Guard.Against.Null(bangsal, nameof(bangsal));

        return Create(
            OrderSourceEnum.AdmisiBooking,
            pasien,
            dokter ?? new PpaReff("-", "-"),
            plannedDate,
            clinicalNotes,
            auditUserId,
            kelasRawat,
            bangsal,
            insurance,
            "-",
            "",
            createdAt);
    }

    public static InpatientOrderModel CreateEmr(
        OrderSourceEnum orderSource,
        PasienReff pasien,
        PpaReff dokter,
        DateTime plannedDate,
        string clinicalNotes,
        string auditUserId,
        string externalReferenceId = "-",
        string externalSystemSource = "",
        InpatientOrderInsuranceModel? insurance = null,
        DateTime createdAt = default)
    {
        Guard.Against.Null(dokter, nameof(dokter));

        return Create(
            orderSource,
            pasien,
            dokter,
            plannedDate,
            clinicalNotes,
            auditUserId,
            new KelasReff("-", "-"),
            new BangsalReff("-", "-"),
            insurance,
            externalReferenceId,
            externalSystemSource,
            createdAt);
    }

    public static InpatientOrderModel Default => new(
        "-",
        OrderSourceEnum.EmrRawatJalan,
        InpatientOrderStatusEnum.Requested,
        new PasienReff("-", "-", new DateOnly(3000, 1, 1), "-"),
        new PpaReff("-", "-"),
        EmptyDate,
        new KelasReff("-", "-"),
        new BangsalReff("-", "-"),
        "",
        InpatientOrderInsuranceModel.Default,
        EMPTY_REF_ID,
        EMPTY_REF_ID,
        "",
        AuditTrailType.Default);

    public static IInpatientOrderKey Key(string id) => Default with { InpatientOrderId = id };

    #endregion

    #region PROPERTIES

    public string InpatientOrderId { get; init; }
    public OrderSourceEnum OrderSource { get; init; }
    public InpatientOrderStatusEnum OrderStatus { get; init; }
    public PasienReff Pasien { get; init; }
    public PpaReff Dokter { get; init; }
    public DateTime PlannedDate { get; init; }
    public KelasReff KelasRawat { get; init; }
    public BangsalReff Bangsal { get; init; }
    public string ClinicalNotes { get; init; }
    public InpatientOrderInsuranceModel Insurance { get; init; }
    public string FulfilledRegId { get; init; }
    public string ExternalReferenceId { get; init; }
    public string ExternalSystemSource { get; init; }
    public AuditTrailType AuditTrail { get; init; }

    #endregion

    #region BEHAVIOUR

    public InpatientOrderModel Maintain(
        DateTime plannedDate,
        KelasReff kelasRawat,
        BangsalReff bangsal,
        string auditUserId,
        DateTime maintainedAt = default,
        PpaReff? dokter = null)
    {
        Guard.Against.Null(kelasRawat, nameof(kelasRawat));
        Guard.Against.Null(bangsal, nameof(bangsal));
        Guard.Against.NullOrWhiteSpace(auditUserId, nameof(auditUserId));

        if (OrderStatus is not InpatientOrderStatusEnum.Requested and not InpatientOrderStatusEnum.Maintained)
        {
            throw new InvalidOperationException(
                $"Inpatient order {InpatientOrderId} harus berstatus Requested atau Maintained untuk dipelihara (status saat ini: {OrderStatus}).");
        }

        var audit = AuditTrail;
        audit.Modif(auditUserId, maintainedAt);

        return this with
        {
            OrderStatus = InpatientOrderStatusEnum.Maintained,
            PlannedDate = plannedDate,
            KelasRawat = kelasRawat,
            Bangsal = bangsal,
            Dokter = dokter ?? Dokter,
            AuditTrail = audit
        };
    }

    public InpatientOrderModel Maintain(
        KelasReff kelasRawat,
        BangsalReff bangsal,
        string auditUserId,
        DateTime maintainedAt = default) =>
        Maintain(PlannedDate, kelasRawat, bangsal, auditUserId, maintainedAt);

    public InpatientOrderModel Fulfill(
        string regId,
        string auditUserId,
        DateTime fulfilledAt = default)
    {
        Guard.Against.NullOrWhiteSpace(regId, nameof(regId));
        Guard.Against.NullOrWhiteSpace(auditUserId, nameof(auditUserId));

        if (OrderStatus == InpatientOrderStatusEnum.Cancelled)
        {
            throw new InvalidOperationException(
                $"Inpatient order {InpatientOrderId} sudah dibatalkan; tidak dapat dipenuhi.");
        }

        if (OrderStatus == InpatientOrderStatusEnum.Fulfilled ||
            (!string.IsNullOrWhiteSpace(FulfilledRegId) && FulfilledRegId != EMPTY_REF_ID))
        {
            throw new InvalidOperationException(
                $"Inpatient order {InpatientOrderId} sudah dipenuhi oleh Reg {FulfilledRegId}.");
        }

        if (OrderStatus is not InpatientOrderStatusEnum.Requested and not InpatientOrderStatusEnum.Maintained)
        {
            throw new InvalidOperationException(
                $"Inpatient order {InpatientOrderId} harus Requested atau Maintained untuk dipenuhi (status saat ini: {OrderStatus}).");
        }

        var audit = AuditTrail;
        audit.Modif(auditUserId, fulfilledAt);

        return this with
        {
            OrderStatus = InpatientOrderStatusEnum.Fulfilled,
            FulfilledRegId = regId,
            AuditTrail = audit
        };
    }

    public InpatientOrderModel Cancel(
        string auditUserId,
        DateTime cancelledAt = default)
    {
        Guard.Against.NullOrWhiteSpace(auditUserId, nameof(auditUserId));

        if (OrderStatus == InpatientOrderStatusEnum.Cancelled)
        {
            throw new InvalidOperationException(
                $"Inpatient order {InpatientOrderId} sudah dibatalkan.");
        }

        if (OrderStatus == InpatientOrderStatusEnum.Fulfilled ||
            (!string.IsNullOrWhiteSpace(FulfilledRegId) && FulfilledRegId != EMPTY_REF_ID))
        {
            throw new InvalidOperationException(
                $"Inpatient order {InpatientOrderId} sudah dipenuhi oleh Reg {FulfilledRegId}; tidak dapat dibatalkan.");
        }

        var audit = AuditTrail;
        audit.Batal(auditUserId, cancelledAt);

        return this with
        {
            OrderStatus = InpatientOrderStatusEnum.Cancelled,
            AuditTrail = audit
        };
    }

    public InpatientOrderModel Restore(
        string regId,
        string userId,
        DateTime timestamp = default,
        InpatientOrderStatusEnum? targetStatus = null)
    {
        Guard.Against.NullOrWhiteSpace(regId, nameof(regId));
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));

        if (OrderStatus != InpatientOrderStatusEnum.Fulfilled || FulfilledRegId != regId)
        {
            throw new InvalidOperationException(
                $"Inpatient order {InpatientOrderId} harus Fulfilled oleh Reg {regId} untuk dipulihkan (status saat ini: {OrderStatus}, FulfilledRegId: {FulfilledRegId}).");
        }

        var restoredStatus = targetStatus ?? (
            (!string.IsNullOrWhiteSpace(Bangsal.BangsalId) && Bangsal.BangsalId != "-" &&
             !string.IsNullOrWhiteSpace(KelasRawat.KelasId) && KelasRawat.KelasId != "-")
                ? InpatientOrderStatusEnum.Maintained
                : InpatientOrderStatusEnum.Requested);

        var audit = new AuditTrailType(
            AuditTrail.Created,
            AuditTrail.Modified,
            AuditTrail.Voided);
        audit.Modif(userId, timestamp);

        return this with
        {
            OrderStatus = restoredStatus,
            FulfilledRegId = EMPTY_REF_ID,
            AuditTrail = audit
        };
    }

    public InpatientOrderModel SetInsurance(
        InpatientOrderInsuranceModel insurance,
        string auditUserId,
        DateTime occurredAt = default)
    {
        Guard.Against.Null(insurance, nameof(insurance));
        Guard.Against.NullOrWhiteSpace(auditUserId, nameof(auditUserId));

        if (OrderStatus is InpatientOrderStatusEnum.Fulfilled or InpatientOrderStatusEnum.Cancelled)
        {
            throw new InvalidOperationException(
                $"Inpatient order {InpatientOrderId} berstatus {OrderStatus}; penjaminan tidak dapat diubah.");
        }

        var audit = AuditTrail;
        audit.Modif(auditUserId, occurredAt);

        return this with
        {
            Insurance = insurance,
            AuditTrail = audit
        };
    }

    public InpatientOrderModel SetInsurance(
        TipeJaminanReff tipeJaminan,
        string jaminanReffId,
        string auditUserId,
        DateTime occurredAt = default) =>
        SetInsurance(new InpatientOrderInsuranceModel(tipeJaminan, jaminanReffId), auditUserId, occurredAt);

    public InpatientOrderModel SetInsurance(
        TipeJaminanType tipeJaminan,
        string jaminanReffId,
        string auditUserId,
        DateTime occurredAt = default) =>
        SetInsurance(tipeJaminan.ToReff(), jaminanReffId, auditUserId, occurredAt);

    #endregion
}
