using Ardalis.GuardClauses;

namespace Farinv.Domain.BrgContext.BrgFeature;

public interface IBrg : IBrgKey
{
    string BrgName { get; }
    bool IsAktif { get; }
    string KetBarang { get; }
    GroupRekReff GroupRek { get; }
    GroupRekDkType GroupRekDk { get; }
    IEnumerable<BrgSatuanType> ListSatuan { get; }
    BrgReff ToReff();
}

public interface IBrgKey
{
    string BrgId { get; }
}

public record BrgReff(string BrgId, string BrgName) : IBrgKey;

public record BrgSatuanType(SatuanType Satuan, int Konversi)
{
    public static BrgSatuanType Default => new BrgSatuanType(SatuanType.Default, 1);
};