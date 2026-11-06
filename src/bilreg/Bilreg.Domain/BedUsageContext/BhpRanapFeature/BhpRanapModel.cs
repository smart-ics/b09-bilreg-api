using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.PakaiBedFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.BedUsageContext.BhpRanapFeature;

public class BhpRanapModel : IBhpRanapKey, IPakaiBed, IRegKey, ILayananKey
{
    private const string ID_PREFIX = "BHR";

    #region CREATION
    public BhpRanapModel(
        string bhpRanapId,
        string pakaiBedId,
        string regId,
        string layananId,
        string bhpItemId,
        string bhpItemName,
        int qty,
        decimal price,
        AuditInfoType audit,
        AuditInfoType voidAudit,
        string voidKet)
    {
        BhpRanapId = bhpRanapId;
        PakaiBedId = pakaiBedId;
        RegId = regId;
        LayananId = layananId;
        BhpItemId = bhpItemId;
        BhpItemName = bhpItemName;
        Qty = qty;
        Price = price;
        Audit = audit;
        VoidAudit = voidAudit;
        VoidKet = voidKet;
    }

    public static BhpRanapModel Default => new(
        bhpRanapId: "-",
        pakaiBedId: "-",
        regId: "-",
        layananId: "-",
        bhpItemId: "-",
        bhpItemName: "-",
        qty: 0,
        price: 0,
        audit: AuditInfoType.Default,
        voidAudit: AuditInfoType.Default,
        voidKet: string.Empty);

    public static IBhpRanapKey Key(string id) => new BhpRanapModel(
        bhpRanapId: id,
        pakaiBedId: "-",
        regId: "-",
        layananId: "-",
        bhpItemId: "-",
        bhpItemName: "-",
        qty: 0,
        price: 0,
        audit: AuditInfoType.Default,
        voidAudit: AuditInfoType.Default,
        voidKet: string.Empty);

    public static BhpRanapModel Create(
        string pakaiBedId,
        string regId,
        string layananId,
        string bhpItemId,
        string bhpItemName,
        int qty,
        decimal price,
        AuditInfoType audit)
    {
        Guard.Against.NullOrWhiteSpace(pakaiBedId, nameof(pakaiBedId));
        Guard.Against.NullOrWhiteSpace(regId, nameof(regId));
        Guard.Against.NullOrWhiteSpace(bhpItemId, nameof(bhpItemId));
        Guard.Against.NegativeOrZero(qty, nameof(qty));
        Guard.Against.Negative(price, nameof(price));
        Guard.Against.NullOrWhiteSpace(audit.UserId, nameof(audit.UserId));

        return new BhpRanapModel(
            bhpRanapId: NunaId.New(ID_PREFIX),
            pakaiBedId: pakaiBedId,
            regId: regId,
            layananId: layananId ?? string.Empty,
            bhpItemId: bhpItemId,
            bhpItemName: string.IsNullOrWhiteSpace(bhpItemName) ? "-" : bhpItemName,
            qty: qty,
            price: price,
            audit: audit,
            voidAudit: AuditInfoType.Default,
            voidKet: string.Empty);
    }
    #endregion

    #region BEHAVIOR
    public void Void(string userId, DateTime voidDate, string reason)
    {
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));
        VoidAudit = new AuditInfoType(userId, voidDate);
        VoidKet = string.IsNullOrWhiteSpace(reason) ? "Dibatalkan" : reason;
    }
    #endregion

    #region PROPERTIES
    public string BhpRanapId { get; init; }
    public string PakaiBedId { get; init; }
    public string RegId { get; init; }
    public string LayananId { get; init; }
    public string BhpItemId { get; init; }
    public string BhpItemName { get; init; }
    public int Qty { get; init; }
    public decimal Price { get; init; }
    public AuditInfoType Audit { get; init; }
    public AuditInfoType VoidAudit { get; private set; }
    public string VoidKet { get; private set; }
    public decimal Subtotal => Qty * Price;
    public bool IsVoid => VoidAudit.Timestamp < new DateTime(3000, 1, 1);
    #endregion
}
