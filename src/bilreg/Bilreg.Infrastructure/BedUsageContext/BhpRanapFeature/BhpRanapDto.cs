using Bilreg.Application.BedUsageContext.BhpRanapFeature;
using Bilreg.Domain.BedUsageContext.BhpRanapFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Infrastructure.BedUsageContext.BhpRanapFeature;

public record BhpRanapDto(
    string BhpRanapId,
    string PakaiBedId,
    string RegId,
    string LayananId,
    string BhpItemId,
    string BhpItemName,
    int Qty,
    decimal Price,
    string CrtUser,
    DateTime CrtDate,
    string VodUser,
    DateTime VodDate,
    string VodKet)
{
    public BhpRanapDto() : this(
        string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
        0, 0, string.Empty, default, string.Empty, new DateTime(3000, 1, 1), string.Empty)
    {
    }

    public static BhpRanapDto FromModel(BhpRanapModel model)
        => new(
            BhpRanapId: model.BhpRanapId,
            PakaiBedId: model.PakaiBedId,
            RegId: model.RegId,
            LayananId: model.LayananId,
            BhpItemId: model.BhpItemId,
            BhpItemName: model.BhpItemName,
            Qty: model.Qty,
            Price: model.Price,
            CrtUser: model.Audit.UserId,
            CrtDate: model.Audit.Timestamp,
            VodUser: model.VoidAudit.UserId,
            VodDate: model.VoidAudit.Timestamp == default ? new DateTime(3000, 1, 1) : model.VoidAudit.Timestamp,
            VodKet: model.VoidKet ?? string.Empty);

    public BhpRanapModel ToModel()
        => new(
            bhpRanapId: BhpRanapId,
            pakaiBedId: PakaiBedId,
            regId: RegId,
            layananId: LayananId,
            bhpItemId: BhpItemId,
            bhpItemName: BhpItemName,
            qty: Qty,
            price: Price,
            audit: new AuditInfoType(CrtUser, CrtDate),
            voidAudit: new AuditInfoType(VodUser, VodDate),
            voidKet: VodKet);

    public BhpRanapView ToView()
        => new(
            BhpRanapId: BhpRanapId,
            PakaiBedId: PakaiBedId,
            RegId: RegId,
            LayananId: LayananId,
            BhpItemId: BhpItemId,
            BhpItemName: BhpItemName,
            Qty: Qty,
            Price: Price,
            Subtotal: Qty * Price,
            CreatedDateTime: CrtDate,
            CreatedUser: CrtUser,
            IsVoid: VodDate < new DateTime(3000, 1, 1));
}
