using Bilreg.Domain.AdmisiContext.PpaFeature;

namespace Bilreg.Infrastructure.AdmisiContext.PpaFeature;

public record PpaMapHidokDto(string DokterRs, string DokterName, string DokterHidok)
{
    public static PpaMapHidokDto FromModel(PpaMapHidokType model)
    {
        var result = new PpaMapHidokDto(
            DokterRs: model.PpaId,       
            DokterName: model.PpaName,   
            DokterHidok: model.PpaHidokId 
        );
        return result;
    }

    public PpaMapHidokType ToModel()
    {
        var result = new PpaMapHidokType(
            ppaId: DokterRs,         
            ppaName: DokterName,     
            ppaHidokId: DokterHidok  
        );
        return result;
    }
}