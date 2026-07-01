using Bilreg.Domain.AdmisiContext.PpaFeature;

// ReSharper disable InconsistentNaming
namespace Bilreg.Infrastructure.AdmisiContext.PpaFeature;

public record PpaSatTugasDto(
    string fs_kd_peg,
    string fs_kd_sat_tugas,
    decimal fn_utama,
    string fs_nm_sat_tugas,
    string fs_kd_profesi,
    string fs_nm_profesi)
{
    public static PpaSatTugasDto Create(PpaType ptgMed, PpaSatTugasType ptgMedSat)
        =>  new PpaSatTugasDto(
            ptgMed.PpaId,
            ptgMedSat.SatTugas.SatTugasId,
            ptgMedSat.IsUtama ? 1 : 0,
            ptgMedSat.SatTugas.SatTugasName,
            ptgMedSat.SatTugas.Profesi.ProfesiId,
            ptgMedSat.SatTugas.Profesi.ProfesiName);

    public PpaSatTugasType ToModel()
    {
        
        var profesi = new ProfesiType(fs_kd_profesi, fs_nm_profesi);
        var satTugas = new SatTugasType(fs_kd_sat_tugas, fs_nm_sat_tugas, profesi);
        var result = new PpaSatTugasType(satTugas, fn_utama == 1);
        return result;
    }
}