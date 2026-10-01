using Bilreg.Domain.AdmisiContext.LayananFeature;

namespace Bilreg.Domain.ChargeContext.TindakanFeature;

//  M03-F01 P1-S03 — order-type to accountable receiving-service mapping
//  (TD-04/TD-07). Mirrors trs_tujuan_lanjut: OrderType is the key,
//  Layanan is the accountable receiving service, IsActive lets ops
//  deactivate a mapping without touching code.
public record TujuanLanjutType : ITujuanLanjutKey
{
    #region CREATION
    public TujuanLanjutType(string orderType, LayananReff layanan, bool isActive)
    {
        OrderType = orderType;
        Layanan = layanan;
        IsActive = isActive;
    }

    public static TujuanLanjutType Default => new(
        "-", LayananType.Default.ToReff(), true);

    public static ITujuanLanjutKey Key(string orderType) => Default with { OrderType = orderType };
    #endregion

    #region PROPERTIES
    public string OrderType { get; init; }
    public LayananReff Layanan { get; init; }
    public bool IsActive { get; init; }
    #endregion
}

public interface ITujuanLanjutKey
{
    string OrderType { get; }
}

//  Resolves the accountable receiving service for an order type from
//  mapping data. Application code supplies the mapping (loaded from
//  trs_tujuan_lanjut); there is no per-order-type branching here, so
//  adding a receiving service needs no code change (TD-04).
public class TujuanLanjutService
{
    private readonly IReadOnlyDictionary<string, TujuanLanjutType> _map;

    public TujuanLanjutService(IEnumerable<TujuanLanjutType> listTujuanLanjut)
    {
        _map = (listTujuanLanjut ?? Enumerable.Empty<TujuanLanjutType>())
            .GroupBy(x => x.OrderType, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
    }

    public TujuanLanjutType Resolve(string orderType)
    {
        if (string.IsNullOrWhiteSpace(orderType))
            throw new ArgumentException("Tipe order harus diisi");

        if (!_map.TryGetValue(orderType, out var tujuan))
            throw new ArgumentException($"Tipe order '{orderType}' tidak dikenal");

        if (!tujuan.IsActive)
            throw new ArgumentException($"Tipe order '{orderType}' tidak aktif");

        return tujuan;
    }

    public LayananReff ResolveLayanan(string orderType) => Resolve(orderType).Layanan;
}
