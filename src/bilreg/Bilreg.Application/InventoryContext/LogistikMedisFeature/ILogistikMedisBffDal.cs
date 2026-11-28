using Bilreg.Domain.InventoryContext.MutasiFeature;

namespace Bilreg.Application.InventoryContext.LogistikMedisFeature;

public interface ILogistikMedisBffDal
{
    IEnumerable<LogistikMedisStokBatchRowDto> ListStokBatch(string layananId);
    IEnumerable<LogistikMedisBarangLayananRowDto> ListBarangLayanan(string layananId);
    IEnumerable<LogistikMedisAggQtyRowDto> ListPendingOrderQty(string layananId);
    IEnumerable<LogistikMedisAggQtyRowDto> ListIncomingMutasiQty(string layananId);

    IEnumerable<LogistikMedisOrderMutasiHeaderRowDto> ListOrderMutasiByLayananOrder(
        string layananOrderId,
        DateTime tgl1,
        DateTime tgl2);
    IEnumerable<LogistikMedisOrderMutasiItemRowDto> ListOrderMutasiItems(IEnumerable<string> orderMutasiIds);

    IEnumerable<LogistikMedisMutasiBrgHeaderRowDto> ListMutasiBrgInTransit(string layananTujuanId);
    IEnumerable<LogistikMedisMutasiBrgHeaderRowDto> ListMutasiBrgKeluar(
        string layananAsalId,
        DateTime tgl1,
        DateTime tgl2);
    IEnumerable<LogistikMedisMutasiBrgItemRowDto> ListMutasiBrgItems(IEnumerable<string> mutasiBrgIds);

    IEnumerable<LogistikMedisTerimaMutasiHeaderRowDto> ListTerimaMutasi(
        string layananTujuanId,
        DateTime tgl1,
        DateTime tgl2);
    IEnumerable<LogistikMedisTerimaMutasiItemRowDto> ListTerimaMutasiItems(IEnumerable<string> terimaMutasiIds);
}

public record LogistikMedisStokBatchRowDto(
    string BrgId,
    string BrgName,
    string SatuanId,
    string SatuanName,
    string GroupRekDkId,
    string NoBatch,
    string TglEd,
    decimal Hpp,
    decimal QtySisa
);

public record LogistikMedisBarangLayananRowDto(
    string BrgId,
    string BrgName,
    string SatuanId,
    string SatuanName,
    string GroupRekDkId,
    decimal MinStok,
    decimal MaxStok,
    string KdRak,
    string KetLokasi
);

public record LogistikMedisAggQtyRowDto(
    string BrgId,
    string BrgName,
    string SatuanId,
    string SatuanName,
    string GroupRekDkId,
    decimal Qty
);

public record LogistikMedisOrderMutasiHeaderRowDto(
    string OrderMutasiId,
    DateTime OrderMutasiDate,
    OrderMutasiStateEnum State,
    OrderMutasiPriorityEnum Priority,
    string LayananOrderId,
    string LayananOrderName,
    string LayananTujuanId,
    string LayananTujuanName,
    string OrderNote
);

public record LogistikMedisOrderMutasiItemRowDto(
    string OrderMutasiId,
    int NoUrut,
    string BrgId,
    string BrgName,
    decimal StokCurrent,
    decimal Qty,
    decimal QtyTerpenuhi,
    string SatuanId,
    string SatuanName
);

public record LogistikMedisMutasiBrgHeaderRowDto(
    string MutasiBrgId,
    DateTime MutasiBrgDate,
    MutasiBrgStateEnum State,
    string LayananAsalId,
    string LayananAsalName,
    string LayananTujuanId,
    string LayananTujuanName,
    string OrderMutasiId,
    string DispatchNote,
    string IssuerUserId,
    string CancelReason
);

public record LogistikMedisMutasiBrgItemRowDto(
    string MutasiBrgId,
    int NoUrut,
    string BrgId,
    string BrgName,
    string BatchNo,
    string ExpiredDate,
    decimal QtyKirim,
    string SatuanId,
    string SatuanName
);

public record LogistikMedisTerimaMutasiHeaderRowDto(
    string TerimaMutasiId,
    DateTime TerimaMutasiDate,
    TerimaMutasiStateEnum State,
    string MutasiBrgId,
    string OrderMutasiId,
    string LayananAsalId,
    string LayananAsalName,
    string LayananTujuanId,
    string LayananTujuanName,
    string ReceiverUserId,
    string Note
);

public record LogistikMedisTerimaMutasiItemRowDto(
    string TerimaMutasiId,
    int NoUrut,
    string BrgId,
    string BrgName,
    string BatchNo,
    string ExpiredDate,
    decimal QtyTerima,
    string SatuanId,
    string SatuanName
);
