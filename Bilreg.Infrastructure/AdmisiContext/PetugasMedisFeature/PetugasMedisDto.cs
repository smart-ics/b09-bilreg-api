using Bilreg.Domain.AdmisiContext.PetugasMedisFeature;

// ReSharper disable InconsistentNaming
namespace Bilreg.Infrastructure.AdmisiContext.PetugasMedisFeature;

public record PetugasMedisDto(string fs_kd_peg, string fs_nm_peg, 
    string fs_nm_alias, string fs_kd_smf, string fs_nm_smf)
{
    public static PetugasMedisDto FromModel(PetugasMedisType model)
    {
        var result = new PetugasMedisDto(model.PetugasMedisId, model.PetugasMedisName,
            model.NamaSingkat, model.Smf.SmfId, model.Smf.SmfName);
        return result;
    }
    
    public PetugasMedisType ToModel(IEnumerable<PetugasMedisLayananType> listLayanan,
        IEnumerable<PetugasMedisSatTugasType> listSatTugas)
    {
        var smf = new SmfType(fs_kd_smf, fs_nm_smf);
        var result = new PetugasMedisType(fs_kd_peg, fs_nm_peg, fs_nm_alias, smf,
            listLayanan, listSatTugas);
        return result;
    }
}