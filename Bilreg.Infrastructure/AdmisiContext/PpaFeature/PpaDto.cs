using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;

// ReSharper disable InconsistentNaming
namespace Bilreg.Infrastructure.AdmisiContext.PpaFeature;

public record PpaDto(string fs_kd_peg, string fs_nm_peg, 
    string fs_nm_alias, string fs_kd_smf, string fs_nm_smf)
{
    public static PpaDto FromModel(PpaType model)
    {
        var result = new PpaDto(model.PpaId, model.PpaName,
            model.NamaSingkat, model.Smf.SmfId, model.Smf.SmfName);
        return result;
    }
    
    public PpaType ToModel(IEnumerable<PpaLayananType> listLayanan,
        IEnumerable<PpaSatTugasType> listSatTugas)
    {
        var smf = new SmfType(fs_kd_smf, fs_nm_smf);
        var result = new PpaType(fs_kd_peg, fs_nm_peg, fs_nm_alias, smf,
            listLayanan, listSatTugas);
        return result;
    }

    public PpaView ToView()
    {
        var smf = new SmfType(fs_kd_smf, fs_nm_smf);
        var result = new PpaView(fs_kd_peg, fs_nm_peg, fs_nm_alias, smf);
        return result;
    }
}