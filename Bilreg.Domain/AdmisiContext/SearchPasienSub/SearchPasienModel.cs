using Bilreg.Domain.AdmisiContext.RegSub.RegAgg.ValueObjects;
using Bilreg.Domain.PasienContext.PasienFeature;

namespace Bilreg.Domain.AdmisiContext.SearchPasienSub;

public class SearchPasienModel : IPasienKey, IRegKey
{
    #region FACTORY
    public SearchPasienModel() { }
    public SearchPasienModel(string pasienId, 
        string pasienName, 
        DateTime tglLahir, 
        GenderType gender, 
        IdentitasType identitas,
        string ibuKandung, 
        AlamatType alamat,
        string regId, 
        string bookingId)
    {
        PasienId = pasienId;
        PasienName = pasienName;
        TglLahir = tglLahir;
        Gender = gender;
        Identitas = identitas;
        IbuKandung = ibuKandung;
        AlamatDomisili = alamat;
        RegId = regId;
        BookingId = bookingId;
    }

    public static SearchPasienModel Load(string pasienId,
        string pasienName,
        DateTime tglLahir,
        GenderType gender,
        IdentitasType identitas,
        string ibuKandung,
        AlamatType alamat,
        string regId,
        string bookingId)
        => new SearchPasienModel(pasienId, pasienName, 
            tglLahir, gender, identitas, ibuKandung, alamat, regId, bookingId);
    
    public static SearchPasienModel Default => new SearchPasienModel(
        "-", "-", new DateTime(3000,1,1), GenderType.Default, IdentitasType.Default, 
        "-", AlamatType.Default, "-", "-");
    
    #endregion

    #region PROPERTIES
    public string PasienId { get; init; }
    public string PasienName { get; init; }
    public DateTime TglLahir { get; init; }
    public GenderType Gender {  get; init; }
    public IdentitasType Identitas { get; init; }
    public string IbuKandung { get; init; }
    public AlamatType AlamatDomisili { get; init; }
    public  string RegId { get; init; }
    public string BookingId { get; init; }
    #endregion

    #region BEHAVIOR

    #endregion
}
