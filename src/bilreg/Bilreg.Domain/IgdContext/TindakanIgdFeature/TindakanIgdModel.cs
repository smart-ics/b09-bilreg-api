using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.IgdContext.TindakanIgdFeature;

public class TindakanIgdModel : ITindakanIgdKey
{
    private const string ID_PREFIX = "TKI";

    #region CREATION
    public TindakanIgdModel(
        string tindakanIgdId,
        string igdVisitId,
        string regId,
        string reffId,
        string descriptions,
        int qty,
        ActivityTindakanIgd aktifitas,
        PpaReff ppa,
        AuditInfoType audit,
        string trsTdkId = "",
        string trsBillId = "",
        bool isPosted = false,
        DateTime? postedDate = null)
    {
        TindakanIgdId = tindakanIgdId;
        IgdVisitId = igdVisitId;
        RegId = regId;
        ReffId = reffId;
        Descriptions = descriptions;
        Qty = qty;
        Aktifitas = aktifitas;
        Ppa = ppa;
        Audit = audit;
        TrsTdkId = trsTdkId ?? string.Empty;
        TrsBillId = trsBillId ?? string.Empty;
        IsPosted = isPosted;
        PostedDate = postedDate;
    }

    public static TindakanIgdModel Default => new(
        tindakanIgdId: "-",
        igdVisitId: "-",
        regId: "-",
        reffId: "-",
        descriptions: "-",
        qty: 0,
        aktifitas: 0,
        ppa: PpaType.Default.ToReff(),
        audit: AuditInfoType.Default);

    public static ITindakanIgdKey Key(string id) => new TindakanIgdModel(
        tindakanIgdId: id,
        igdVisitId: "-",
        regId: "-",
        reffId: "-",
        descriptions: "-",
        qty: 0,
        aktifitas: 0,
        ppa: PpaType.Default.ToReff(),
        audit: AuditInfoType.Default);

    public static TindakanIgdModel Create(
        IgdVisitModel visit,
        string reffId, string desciption, int qty, ActivityTindakanIgd aktifitas,
        PpaType ppa, string userId, DateTime occurredAt = default)
    {
        Guard.Against.Null(visit);
        Guard.Against.NullOrWhiteSpace(reffId, nameof(reffId));
        Guard.Against.NegativeOrZero(qty, nameof(qty));
        Guard.Against.Null(aktifitas, nameof(aktifitas));
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));

        if (visit.IsTerminal)
            throw new InvalidOperationException(
                $"Visit {visit.IgdVisitId} sudah terminal; tindakan tidak dapat ditambahkan.");
        var audit = new AuditInfoType(userId, occurredAt);

        return new TindakanIgdModel(
            NunaId.New(ID_PREFIX),
            visit.IgdVisitId,
            visit.Reg.RegId,
            reffId,
            string.IsNullOrWhiteSpace(desciption) ? "-" : desciption,
            qty,
            aktifitas,
            ppa.ToReff(),
            audit);
    }
    #endregion

    #region PROPERTIES
    public string TindakanIgdId { get; init; }
    public string IgdVisitId { get; init; }
    public string RegId { get; private set; }
    public ActivityTindakanIgd Aktifitas { get; init;  }
    public string ReffId { get; init; } // tarifId atau barangId
    public string Descriptions { get; init; }
    public int Qty { get; init; }
    public PpaReff Ppa {  get; init; }
    public AuditInfoType Audit { get; init; }
    public string TrsTdkId { get; private set; }
    public string TrsBillId { get; private set; }
    public bool IsPosted { get; private set; }
    public DateTime? PostedDate { get; private set; }
    #endregion

    #region BEHAVIOUR
    public void AssignRegister(string regId)
    {
        Guard.Against.NullOrWhiteSpace(regId, nameof(regId));
        RegId = regId;
    }

    public void MarkAsPosted(string trsTdkId, string trsBillId, DateTime postedAt)
    {
        Guard.Against.NullOrWhiteSpace(trsTdkId, nameof(trsTdkId));
        Guard.Against.NullOrWhiteSpace(trsBillId, nameof(trsBillId));
        TrsTdkId = trsTdkId;
        TrsBillId = trsBillId;
        IsPosted = true;
        PostedDate = postedAt;
    }

    public void MarkAsVoided(AuditInfoType audit, string voidReason)
    {
        Guard.Against.Null(audit, nameof(audit));
        Guard.Against.NullOrWhiteSpace(voidReason, nameof(voidReason));
    }
    #endregion

}


public enum ActivityTindakanIgd
{
    Tindakan, Barang
}
