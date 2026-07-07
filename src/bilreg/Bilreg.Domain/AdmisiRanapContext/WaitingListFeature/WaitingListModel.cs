using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Domain.AdmisiRanapContext.Shared;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.AdmisiRanapContext.WaitingListFeature;

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
        int priority,
        AuditTrailType auditTrail)
    {
        WaitingListId = waitingListId;
        WaitingListStatus = waitingListStatus;
        RegId = regId;
        Pasien = pasien;
        KelasRawat = kelasRawat;
        Bangsal = bangsal;
        Priority = priority;
        AuditTrail = auditTrail;
    }

    #region CREATION

    public static WaitingListModel Create(
        string regId,
        AdmissionStatusEnum admissionStatus,
        PasienReff pasien,
        KelasReff kelasRawat,
        BangsalReff bangsal,
        int priority,
        string auditUserId)
    {
        Guard.Against.NullOrWhiteSpace(regId);
        Guard.Against.Null(pasien);
        Guard.Against.Null(kelasRawat);
        Guard.Against.Null(bangsal);
        Guard.Against.NullOrWhiteSpace(auditUserId);
        AdmissionStatusGuard.EnsureCanEnterWaitingList(admissionStatus);

        var now = DateTime.Now;
        return new WaitingListModel(
            NunaId.New(ID_PREFIX),
            WaitingListStatusEnum.Waiting,
            regId,
            pasien,
            kelasRawat,
            bangsal,
            priority,
            AuditTrailType.Create(auditUserId, now));
    }

    public static WaitingListModel Default => new(
        "-",
        WaitingListStatusEnum.Waiting,
        "-",
        new PasienReff("-", "-", new DateOnly(3000, 1, 1), "-"),
        new KelasReff("-", "-"),
        new BangsalReff("-", "-"),
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
    public int Priority { get; init; }
    public AuditTrailType AuditTrail { get; init; }

    public bool IsActive =>
        WaitingListStatus is WaitingListStatusEnum.Waiting or WaitingListStatusEnum.Accepted;

    #endregion

    #region BEHAVIOUR

    public WaitingListModel Update(
        int priority,
        KelasReff kelasRawat,
        BangsalReff bangsal,
        string auditUserId)
    {
        Guard.Against.Null(kelasRawat);
        Guard.Against.Null(bangsal);
        Guard.Against.NullOrWhiteSpace(auditUserId);

        if (WaitingListStatus != WaitingListStatusEnum.Waiting)
            throw new InvalidOperationException(
                $"Waiting List {WaitingListId} harus Waiting untuk diperbarui (status saat ini: {WaitingListStatus}).");

        var audit = AuditTrail;
        audit.Modif(auditUserId, DateTime.Now);
        return WithState(WaitingListStatusEnum.Waiting, priority, kelasRawat, bangsal, audit);
    }

    public WaitingListModel Accept(string auditUserId)
    {
        Guard.Against.NullOrWhiteSpace(auditUserId);

        if (WaitingListStatus != WaitingListStatusEnum.Waiting)
            throw new InvalidOperationException(
                $"Waiting List {WaitingListId} harus Waiting untuk diterima (status saat ini: {WaitingListStatus}).");

        var audit = AuditTrail;
        audit.Modif(auditUserId, DateTime.Now);
        return WithState(WaitingListStatusEnum.Accepted, Priority, KelasRawat, Bangsal, audit);
    }

    public WaitingListModel Close(string auditUserId)
    {
        Guard.Against.NullOrWhiteSpace(auditUserId);

        if (WaitingListStatus != WaitingListStatusEnum.Accepted)
            throw new InvalidOperationException(
                $"Waiting List {WaitingListId} harus Accepted untuk ditutup (status saat ini: {WaitingListStatus}).");

        AuditTrail.Modif(auditUserId, DateTime.Now);
        return WithState(WaitingListStatusEnum.Closed, Priority, KelasRawat, Bangsal, AuditTrail);
    }

    #endregion

    #region HELPERS

    private WaitingListModel WithState(
        WaitingListStatusEnum status,
        int priority,
        KelasReff kelasRawat,
        BangsalReff bangsal,
        AuditTrailType audit) =>
        new(
            WaitingListId,
            status,
            RegId,
            Pasien,
            kelasRawat,
            bangsal,
            priority,
            audit);

    #endregion
}
