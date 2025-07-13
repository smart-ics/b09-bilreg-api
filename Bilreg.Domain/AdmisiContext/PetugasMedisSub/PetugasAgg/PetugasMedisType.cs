using Ardalis.GuardClauses;
using Bilreg.Domain.PasienContext;

namespace Bilreg.Domain.AdmisiContext.PetugasMedisSub.PetugasAgg;

public record PetugasMedisType : IPetugasMedisKey
{
    public PetugasMedisType(string petugasMedisId, string petugasMedisName, string namaSingkat, 
        IEnumerable<PetugasMedisLayananModel> listLayanan, 
        IEnumerable<PetugasMedisSatTugasModel> listSatTugas)
    {
        Guard.Against.NullOrWhiteSpace(petugasMedisId, nameof(petugasMedisId));
        Guard.Against.NullOrWhiteSpace(petugasMedisName, nameof(petugasMedisName));
        var listLayananFtch = listLayanan.ToList();
        var listSatTugasFtch = listSatTugas.ToList();
        Guard.Against.Null(listLayananFtch, nameof(listLayanan));
        Guard.Against.Null(listSatTugasFtch, nameof(listSatTugas));

        PetugasMedisId = petugasMedisId;
        PetugasMedisName = petugasMedisName;
        NamaSingkat = namaSingkat;
        PetugasMedisLayanan = listLayananFtch;
        PetugasMedisSatTugas = listSatTugasFtch;
    }
    
    public string PetugasMedisId { get; init; }
    public string PetugasMedisName { get; init; }
    public string NamaSingkat { get; init; }
    public SmfType Smf { get; init; }
    public IEnumerable<PetugasMedisLayananModel> PetugasMedisLayanan { get; init; }
    public IEnumerable<PetugasMedisSatTugasModel> PetugasMedisSatTugas { get; init; }
    
    public static PetugasMedisType Default => new("-", "-", "-", 
        new List<PetugasMedisLayananModel>(), 
        new List<PetugasMedisSatTugasModel>());
    public static IPetugasMedisKey Key(string id) => Default with { PetugasMedisId = id };
}

public interface IPetugasMedisKey
{
    string PetugasMedisId {get;}
}