using Bilreg.Domain.PasienContext.DataSosialPasienSub.PasienAgg;
using CommunityToolkit.Diagnostics;

namespace Bilreg.Domain.AdmisiContext.RegSub.RegAgg;

public record RegPasienVo(
    string PasienId, 
    string PasienName, 
    string NoMedRec, 
    DateTime TglLahir, 
    string Gender)
{
    private static readonly string[] ValidGender = ["L", "P", "W", "M", "F", "1", "0"];

    public static RegPasienVo Create(PasienModel pasien)
    {
        //  GUARD
        Guard.IsNotNull(pasien);
        Guard.IsNotNullOrEmpty(pasien.PasienName);
        if(!ValidGender.Contains(pasien.Gender))
            throw new ArgumentException($"'{pasien.Gender}' is not a valid gender");
        
        //  RETURN
        return new RegPasienVo(
            pasien.PasienId,
            pasien.GetNoMedrec(),
            pasien.PasienName,
            pasien.TglLahir,
            pasien.Gender);
    }

    public static RegPasienVo Load(string id, string name, string noMedRec, DateTime tglLahir, string gender)
    {
        return new RegPasienVo(id, name, noMedRec, tglLahir, gender);        
    }
}

