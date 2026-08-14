using Bilreg.Domain.BrgContext.KlasifikasiFeature;

namespace Bilreg.Domain.BrgContext.BrgFeature;

public interface IKlasifikasiUmum
{
    GolonganType Golongan { get; }
    GroupObatDkType GroupObatDk { get; }
    KelompokType Kelompok { get; }
    SifatType Sifat { get; }
    BentukType Bentuk { get; }
}