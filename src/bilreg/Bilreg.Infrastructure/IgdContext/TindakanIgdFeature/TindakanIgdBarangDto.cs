using Bilreg.Domain.IgdContext.TindakanIgdFeature;

namespace Bilreg.Infrastructure.IgdContext.TindakanIgdFeature;

public record TindakanIgdBarangDto(
    string TindakanIgdId,
    int NoUrut,
    string BrgId,
    string SatuanBomId,
    decimal QtyBom,
    string SatuanKecilId,
    decimal QtyKecilRequested,
    decimal QtyKecilDeducted,
    decimal QtyKecilPending,
    bool IsChargeBarang,
    string PakaiBrgId,
    string TrsBillingId,
    bool IsVoid)
{
    public TindakanIgdBarangDto()
        : this("", 0, "", "", 0m, "", 0m, 0m, 0m, false, "", "", false) { }

    public static TindakanIgdBarangDto FromModel(TindakanIgdBarangType model, string tindakanIgdId)
        => new(
            TindakanIgdId: tindakanIgdId,
            NoUrut: model.NoUrut,
            BrgId: model.BrgId,
            SatuanBomId: model.SatuanBomId,
            QtyBom: model.QtyBom,
            SatuanKecilId: model.SatuanKecilId,
            QtyKecilRequested: model.QtyKecilRequested,
            QtyKecilDeducted: model.QtyKecilDeducted,
            QtyKecilPending: model.QtyKecilPending,
            IsChargeBarang: model.IsChargeBarang,
            PakaiBrgId: model.PakaiBrgId,
            TrsBillingId: model.TrsBillingId,
            IsVoid: model.IsVoid);

    public TindakanIgdBarangType ToModel()
        => new(
            NoUrut: NoUrut,
            BrgId: BrgId,
            SatuanBomId: SatuanBomId,
            QtyBom: QtyBom,
            SatuanKecilId: SatuanKecilId,
            QtyKecilRequested: QtyKecilRequested,
            QtyKecilDeducted: QtyKecilDeducted,
            QtyKecilPending: QtyKecilPending,
            IsChargeBarang: IsChargeBarang,
            PakaiBrgId: PakaiBrgId,
            TrsBillingId: TrsBillingId,
            IsVoid: IsVoid);
}
