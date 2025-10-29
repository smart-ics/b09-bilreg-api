using Bilreg.Domain.PasienContext.DemografiFeature;
using Bilreg.Domain.PasienContext.StatusSosialFeature;

namespace Bilreg.Domain.PasienContext.PasienFeature;

public interface IPasienAdministrativeInfo : IPasienKey
{
    AlamatType AlamatKtp { get; }
    AlamatType AlamatDomisili { get; }
    KelurahanType Kelurahan { get; }
    IdentitasType Identitas { get; }
    IdentitasType KartuKeluarga { get; }
    IEnumerable<ContactType> ListContact { get; }
    PasienKeluargaType PasienKeluarga { get; }
}

public interface IPasienStatusSosial : IPasienKey
{
    StatusKawinDkType StatusKawin { get; }
    AgamaType Agama { get; }
    SukuType Suku { get; }
    PekerjaanDkType PekerjaanDk { get; }
    PendidikanDkType PendidikanDk { get; }
}