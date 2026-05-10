using Ardalis.GuardClauses;
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
        string tarifId,
        string tarifName,
        int qty,
        decimal price,
        AuditInfoType audit)
    {
        TindakanIgdId = tindakanIgdId;
        IgdVisitId = igdVisitId;
        RegId = regId;
        TarifId = tarifId;
        TarifName = tarifName;
        Qty = qty;
        Price = price;
        Audit = audit;
    }

    public static TindakanIgdModel Default => new(
        tindakanIgdId: "-",
        igdVisitId: "-",
        regId: "-",
        tarifId: "-",
        tarifName: "-",
        qty: 0,
        price: 0,
        audit: AuditInfoType.Default);

    public static ITindakanIgdKey Key(string id) => new TindakanIgdModel(
        tindakanIgdId: id,
        igdVisitId: "-",
        regId: "-",
        tarifId: "-",
        tarifName: "-",
        qty: 0,
        price: 0,
        audit: AuditInfoType.Default);

    public static TindakanIgdModel Create(
        IgdVisitModel visit,
        string tarifId, string tarifName, int qty, decimal price,
        AuditInfoType audit)
    {
        Guard.Against.Null(visit);
        Guard.Against.NullOrWhiteSpace(tarifId, nameof(tarifId));
        Guard.Against.NegativeOrZero(qty, nameof(qty));
        Guard.Against.Negative(price, nameof(price));
        Guard.Against.NullOrWhiteSpace(audit.UserId, nameof(audit.UserId));

        if (visit.IsTerminal)
            throw new InvalidOperationException(
                $"Visit {visit.IgdVisitId} sudah terminal; tindakan tidak dapat ditambahkan.");
        if (!visit.HasReg)
            throw new InvalidOperationException(
                $"Visit {visit.IgdVisitId} belum memiliki RegId; tindakan tidak dapat ditambahkan (rule 7.3).");

        return new TindakanIgdModel(
            tindakanIgdId: NunaId.New(ID_PREFIX),
            igdVisitId: visit.IgdVisitId,
            regId: visit.Reg.RegId,
            tarifId: tarifId,
            tarifName: string.IsNullOrWhiteSpace(tarifName) ? "-" : tarifName,
            qty: qty,
            price: price,
            audit: audit);
    }
    #endregion

    #region PROPERTIES
    public string TindakanIgdId { get; init; }
    public string IgdVisitId { get; init; }
    public string RegId { get; init; }
    public string TarifId { get; init; }
    public string TarifName { get; init; }
    public int Qty { get; init; }
    public decimal Price { get; init; }
    public AuditInfoType Audit { get; init; }
    public decimal Subtotal => Qty * Price;
    #endregion
}
