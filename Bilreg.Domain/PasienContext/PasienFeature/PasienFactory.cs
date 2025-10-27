using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.Helpers;
using Bilreg.Domain.PasienContext.DemografiFeature;
using Bilreg.Domain.PasienContext.StatusSosialFeature;
using Nuna.Lib.CleanArchHelper;

namespace Bilreg.Domain.PasienContext.PasienFeature;

public interface IPasienFactory : INunaFactory<PasienModel>
{
    PasienModel CreateFromPerson(PersonInfoType person);
}
public interface IGetKodeRsService : INunaService<string>
{
}

public class PasienFactory : IPasienFactory
{
    private readonly ISequencerManual _sequencer;
    private readonly IGetKodeRsService _getKodeRs;

    public PasienFactory(ISequencerManual sequencer, 
        IGetKodeRsService getKodeRs)
    {
        _sequencer = sequencer;
        _getKodeRs = getKodeRs;
    }

    public PasienModel CreateFromPerson(PersonInfoType person)
    {
        var newNumber = _sequencer.GetNextNoUrut("NOMR", "Nomor Medical Record");
        var kodeRs = _getKodeRs.Execute();
        var newId = $"{kodeRs}{newNumber:D8}";
        var newPasien = new PasienModel(newId, person, "-", "-", GolDarahType.Default, 
            "-", AlamatType.Default, KelurahanType.Default, IdentitasType.Default, 
            new List<ContactType>(), PasienKeluargaType.Default, 
            AgamaType.Default, SukuType.Default, StatusKawinDkType.Default, 
            PendidikanDkType.Default, PekerjaanDkType.Default, 
            DateTime.Now, true);
        return newPasien;
    }
}

