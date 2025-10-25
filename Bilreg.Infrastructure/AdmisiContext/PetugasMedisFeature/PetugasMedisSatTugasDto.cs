using Bilreg.Domain.AdmisiContext.PetugasMedisFeature;
using Bilreg.Domain.AdmisiContext.PetugasMedisSub;
using Bilreg.Domain.AdmisiContext.PetugasMedisSub.PetugasMedisFeature;

// ReSharper disable InconsistentNaming
namespace Bilreg.Infrastructure.AdmisiContext.PetugasMedisFeature;

public record PetugasMedisSatTugasDto(
    string fs_kd_peg,
    string fs_kd_sat_tugas,
    decimal fn_utama,
    string fs_nm_sat_tugas)
{
    public static PetugasMedisSatTugasDto Create(PetugasMedisType ptgMed, PetugasMedisSatTugasType ptgMedSat)
        =>  new PetugasMedisSatTugasDto(
            ptgMed.PetugasMedisId,
            ptgMedSat.SatTugas.SatTugasId,
            ptgMedSat.IsUtama ? 1 : 0,
            ptgMedSat.SatTugas.SatTugasName);

    public PetugasMedisSatTugasType ToModel()
    {
        var satTugas = new SatTugasType(fs_kd_sat_tugas, fs_nm_sat_tugas, true);
        var result = new PetugasMedisSatTugasType(satTugas, fn_utama == 1);
        return result;
    }
}