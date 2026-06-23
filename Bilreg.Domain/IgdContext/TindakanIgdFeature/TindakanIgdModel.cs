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
        AuditInfoType audit)
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
        PpaType ppa, string userId)
    {
        Guard.Against.Null(visit);
        Guard.Against.NullOrWhiteSpace(reffId, nameof(reffId));
        Guard.Against.NegativeOrZero(qty, nameof(qty));
        Guard.Against.Null(aktifitas, nameof(aktifitas));
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));

        if (visit.IsTerminal)
            throw new InvalidOperationException(
                $"Visit {visit.IgdVisitId} sudah terminal; tindakan tidak dapat ditambahkan.");
        var audit = new AuditInfoType(userId, DateTime.Now);

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
    public string RegId { get; init; }
    public ActivityTindakanIgd Aktifitas { get; init;  }
    public string ReffId { get; init; } // tarifId atau barangId
    public string Descriptions { get; init; }
    public int Qty { get; init; }
    public PpaReff Ppa {  get; init; }
    public AuditInfoType Audit { get; init; }
    #endregion

}


public enum ActivityTindakanIgd
{
    Tindakan, Barang
}