using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.BedUsageContext.WaitingListFeature;

public record WaitingListModel : IWaitingListKey
{
    private const string ID_PREFIX = "WTL";

    public WaitingListModel(
        string waitingListId,
        WaitingListStatusEnum waitingListStatus,
        string regId,
        PasienReff pasien,
        KelasReff kelasRawat,
        BangsalReff bangsal,
        ArrivalSourceTypeEnum arrivalSourceType,
        LayananReff referringUnit,
        DokterReff referringDoctor,
        ClinicalUrgencyEnum clinicalUrgency,
        string specialRequirements,
        BedReff plannedBed,
        string cancellationReason,
        string rerouteReason,
        string previousWaitListId,
        string placedBedId,
        string placedPakaiBedId,
        int priority,
        AuditTrailType auditTrail)
    {
        WaitingListId = waitingListId;
        WaitingListStatus = waitingListStatus;
        RegId = regId;
        Pasien = pasien;
        KelasRawat = kelasRawat;
        Bangsal = bangsal;
        ArrivalSourceType = arrivalSourceType;
        ReferringUnit = referringUnit;
        ReferringDoctor = referringDoctor;
        ClinicalUrgency = clinicalUrgency;
        SpecialRequirements = specialRequirements;
        PlannedBed = plannedBed;
        CancellationReason = cancellationReason;
        RerouteReason = rerouteReason;
        PreviousWaitListId = previousWaitListId;
        PlacedBedId = placedBedId;
        PlacedPakaiBedId = placedPakaiBedId;
        Priority = priority;
        AuditTrail = auditTrail;
    }

    #region CREATION

    public static WaitingListModel Create(
        string regId,
        PasienReff pasien,
        KelasReff kelasRawat,
        BangsalReff bangsal,
        ArrivalSourceTypeEnum arrivalSourceType,
        LayananReff referringUnit,
        DokterReff referringDoctor,
        ClinicalUrgencyEnum clinicalUrgency,
        string specialRequirements,
        int priority,
        string auditUserId,
        DateTime createdAt = default,
        string previousWaitListId = "")
    {
        Guard.Against.NullOrWhiteSpace(regId, nameof(regId));
        Guard.Against.Null(pasien, nameof(pasien));
        Guard.Against.Null(kelasRawat, nameof(kelasRawat));
        Guard.Against.Null(bangsal, nameof(bangsal));
        Guard.Against.Null(referringUnit, nameof(referringUnit));
        Guard.Against.Null(referringDoctor, nameof(referringDoctor));
        Guard.Against.NullOrWhiteSpace(auditUserId, nameof(auditUserId));

        return new WaitingListModel(
            NunaId.New(ID_PREFIX),
            WaitingListStatusEnum.Waiting,
            regId,
            pasien,
            kelasRawat,
            bangsal,
            arrivalSourceType,
            referringUnit,
            referringDoctor,
            clinicalUrgency,
            specialRequirements ?? string.Empty,
            new BedReff("-", "-", false),
            cancellationReason: string.Empty,
            rerouteReason: string.Empty,
            previousWaitListId: previousWaitListId ?? string.Empty,
            placedBedId: string.Empty,
            placedPakaiBedId: string.Empty,
            priority,
            AuditTrailType.Create(auditUserId, createdAt));
    }

    public static WaitingListModel Create(
        string regId,
        PasienReff pasien,
        KelasReff kelasRawat,
        BangsalReff bangsal,
        int priority,
        string auditUserId,
        DateTime createdAt = default)
    {
        return Create(
            regId,
            pasien,
            kelasRawat,
            bangsal,
            ArrivalSourceTypeEnum.Adm,
            new LayananReff("-", "-"),
            new DokterReff("-", "-"),
            ClinicalUrgencyEnum.Routine,
            string.Empty,
            priority,
            auditUserId,
            createdAt);
    }

    public static WaitingListModel Default => new(
        "-",
        WaitingListStatusEnum.Waiting,
        "-",
        new PasienReff("-", "-", new DateOnly(3000, 1, 1), "-"),
        new KelasReff("-", "-"),
        new BangsalReff("-", "-"),
        ArrivalSourceTypeEnum.Adm,
        new LayananReff("-", "-"),
        new DokterReff("-", "-"),
        ClinicalUrgencyEnum.Routine,
        string.Empty,
        new BedReff("-", "-", false),
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        0,
        AuditTrailType.Default);

    public static IWaitingListKey Key(string id) => Default with { WaitingListId = id };

    #endregion

    #region PROPERTIES

    public string WaitingListId { get; init; }
    public WaitingListStatusEnum WaitingListStatus { get; init; }
    public string RegId { get; init; }
    public PasienReff Pasien { get; init; }
    public KelasReff KelasRawat { get; init; }
    public BangsalReff Bangsal { get; init; }
    public ArrivalSourceTypeEnum ArrivalSourceType { get; init; }
    public LayananReff ReferringUnit { get; init; }
    public DokterReff ReferringDoctor { get; init; }
    public ClinicalUrgencyEnum ClinicalUrgency { get; init; }
    public string SpecialRequirements { get; init; }
    public BedReff PlannedBed { get; init; }
    public string CancellationReason { get; init; }
    public string RerouteReason { get; init; }
    public string PreviousWaitListId { get; init; }
    public string PlacedBedId { get; init; }
    public string PlacedPakaiBedId { get; init; }
    public int Priority { get; init; }
    public AuditTrailType AuditTrail { get; init; }

    public bool IsActive =>
        WaitingListStatus is WaitingListStatusEnum.Waiting or WaitingListStatusEnum.Reserved;

    #endregion

    #region BEHAVIOUR

    public WaitingListModel ReservePlannedBed(BedReff plannedBed, string actorId, DateTime occurredAt = default)
    {
        Guard.Against.Null(plannedBed, nameof(plannedBed));
        Guard.Against.NullOrWhiteSpace(actorId, nameof(actorId));

        if (WaitingListStatus is not (WaitingListStatusEnum.Waiting or WaitingListStatusEnum.Reserved))
            throw new InvalidOperationException(
                $"Waiting List {WaitingListId} harus Waiting atau Reserved untuk menetapkan rencana bed (status saat ini: {WaitingListStatus}).");

        var audit = new AuditTrailType(AuditTrail.Created, AuditTrail.Modified, AuditTrail.Voided);
        audit.Modif(actorId, occurredAt == default ? DateTime.Now : occurredAt);

        return this with
        {
            WaitingListStatus = WaitingListStatusEnum.Reserved,
            PlannedBed = plannedBed,
            AuditTrail = audit
        };
    }

    public WaitingListModel ClearPlannedBed(string actorId, DateTime occurredAt = default)
    {
        Guard.Against.NullOrWhiteSpace(actorId, nameof(actorId));

        if (WaitingListStatus is not (WaitingListStatusEnum.Waiting or WaitingListStatusEnum.Reserved))
            throw new InvalidOperationException(
                $"Waiting List {WaitingListId} harus Waiting atau Reserved untuk menghapus rencana bed (status saat ini: {WaitingListStatus}).");

        var audit = new AuditTrailType(AuditTrail.Created, AuditTrail.Modified, AuditTrail.Voided);
        audit.Modif(actorId, occurredAt == default ? DateTime.Now : occurredAt);

        return this with
        {
            WaitingListStatus = WaitingListStatusEnum.Waiting,
            PlannedBed = new BedReff("-", "-", false),
            AuditTrail = audit
        };
    }

    public WaitingListModel Place(string bedId, string pakaiBedId, string actorId, DateTime occurredAt = default)
    {
        Guard.Against.NullOrWhiteSpace(bedId, nameof(bedId));
        Guard.Against.NullOrWhiteSpace(pakaiBedId, nameof(pakaiBedId));
        Guard.Against.NullOrWhiteSpace(actorId, nameof(actorId));

        if (WaitingListStatus is not (WaitingListStatusEnum.Waiting or WaitingListStatusEnum.Reserved))
            throw new InvalidOperationException(
                $"Waiting List {WaitingListId} harus Waiting atau Reserved untuk ditempatkan (status saat ini: {WaitingListStatus}).");

        var audit = new AuditTrailType(AuditTrail.Created, AuditTrail.Modified, AuditTrail.Voided);
        audit.Modif(actorId, occurredAt == default ? DateTime.Now : occurredAt);

        return this with
        {
            WaitingListStatus = WaitingListStatusEnum.Placed,
            PlacedBedId = bedId,
            PlacedPakaiBedId = pakaiBedId,
            AuditTrail = audit
        };
    }

    public WaitingListModel Cancel(string reason, string actorId, DateTime occurredAt = default)
    {
        Guard.Against.NullOrWhiteSpace(reason, nameof(reason));
        Guard.Against.NullOrWhiteSpace(actorId, nameof(actorId));

        if (WaitingListStatus is not (WaitingListStatusEnum.Waiting or WaitingListStatusEnum.Reserved))
            throw new InvalidOperationException(
                $"Waiting List {WaitingListId} harus Waiting atau Reserved untuk dibatalkan (status saat ini: {WaitingListStatus}).");

        var audit = new AuditTrailType(AuditTrail.Created, AuditTrail.Modified, AuditTrail.Voided);
        audit.Batal(actorId, occurredAt == default ? DateTime.Now : occurredAt);

        return this with
        {
            WaitingListStatus = WaitingListStatusEnum.Cancelled,
            CancellationReason = reason,
            AuditTrail = audit
        };
    }

    public WaitingListModel Reroute(string reason, string actorId, DateTime occurredAt = default)
    {
        Guard.Against.NullOrWhiteSpace(reason, nameof(reason));
        Guard.Against.NullOrWhiteSpace(actorId, nameof(actorId));

        if (WaitingListStatus is not (WaitingListStatusEnum.Waiting or WaitingListStatusEnum.Reserved))
            throw new InvalidOperationException(
                $"Waiting List {WaitingListId} harus Waiting atau Reserved untuk dialihkan (status saat ini: {WaitingListStatus}).");

        var audit = new AuditTrailType(AuditTrail.Created, AuditTrail.Modified, AuditTrail.Voided);
        audit.Modif(actorId, occurredAt == default ? DateTime.Now : occurredAt);

        return this with
        {
            WaitingListStatus = WaitingListStatusEnum.Rerouted,
            RerouteReason = reason,
            AuditTrail = audit
        };
    }

    public WaitingListModel Update(
        KelasReff kelasRawat,
        BangsalReff bangsal,
        ClinicalUrgencyEnum clinicalUrgency,
        string specialRequirements,
        int priority,
        string actorId,
        DateTime occurredAt = default)
    {
        Guard.Against.Null(kelasRawat, nameof(kelasRawat));
        Guard.Against.Null(bangsal, nameof(bangsal));
        Guard.Against.NullOrWhiteSpace(actorId, nameof(actorId));

        if (WaitingListStatus is not (WaitingListStatusEnum.Waiting or WaitingListStatusEnum.Reserved))
            throw new InvalidOperationException(
                $"Waiting List {WaitingListId} harus Waiting atau Reserved untuk diperbarui (status saat ini: {WaitingListStatus}).");

        var audit = new AuditTrailType(AuditTrail.Created, AuditTrail.Modified, AuditTrail.Voided);
        audit.Modif(actorId, occurredAt == default ? DateTime.Now : occurredAt);

        return this with
        {
            KelasRawat = kelasRawat,
            Bangsal = bangsal,
            ClinicalUrgency = clinicalUrgency,
            SpecialRequirements = specialRequirements ?? string.Empty,
            Priority = priority,
            AuditTrail = audit
        };
    }

    public WaitingListModel Update(
        int priority,
        KelasReff kelasRawat,
        BangsalReff bangsal,
        string actorId,
        DateTime occurredAt = default)
    {
        return Update(kelasRawat, bangsal, ClinicalUrgency, SpecialRequirements, priority, actorId, occurredAt);
    }

    #endregion
}

public record DokterReff(string DokterId, string DokterName)
{
    public static DokterReff Default => new("-", "-");
}
