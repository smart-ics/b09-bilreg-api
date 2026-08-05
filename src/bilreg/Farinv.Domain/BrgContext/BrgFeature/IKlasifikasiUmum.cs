using Farinv.Domain.BrgContext.KlasifikasiFeature;

namespace Farinv.Domain.BrgContext.BrgFeature;

public interface IKlasifikasiUmum
{
    GolonganType Golongan { get; }
    GroupObatDkType GroupObatDk { get; }
    KelompokType Kelompok { get; }
    SifatType Sifat { get; }
    BentukType Bentuk { get; }
}