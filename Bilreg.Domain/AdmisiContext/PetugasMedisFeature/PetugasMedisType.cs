using Bilreg.Domain.AdmisiContext.PetugasMedisSub.PetugasMedisFeature;

namespace Bilreg.Domain.AdmisiContext.PetugasMedisFeature;

public record PetugasMedisType : IPetugasMedisKey
{
    private readonly List<PetugasMedisLayananType> _listLayanan;
    private readonly List<PetugasMedisSatTugasType> _listSatTugas;
    public PetugasMedisType(string petugasMedisId, string petugasMedisName, 
        string namaSingkat, SmfType smf,
        IEnumerable<PetugasMedisLayananType> listLayanan, 
        IEnumerable<PetugasMedisSatTugasType> listSatTugas)
    {
        PetugasMedisId = petugasMedisId;
        PetugasMedisName = petugasMedisName;
        NamaSingkat = namaSingkat;
        Smf = smf;
        _listLayanan = listLayanan?.ToList() ?? [];
        _listSatTugas = listSatTugas?.ToList() ?? [];
    }
    
    public string PetugasMedisId { get; init; }
    public string PetugasMedisName { get; init; }
    public string NamaSingkat { get; init; }
    public SmfType Smf { get; init; }
    public IEnumerable<PetugasMedisLayananType> ListLayanan => _listLayanan;
    public IEnumerable<PetugasMedisSatTugasType> ListSatTugas => _listSatTugas;
    
    public PetugasMedisReff ToReff() => new (PetugasMedisId, PetugasMedisName);
    
    public static PetugasMedisType Default => new("-", "-", "-", 
        SmfType.Default, [], []);
    public static IPetugasMedisKey Key(string id) => Default with { PetugasMedisId = id };
}

public interface IPetugasMedisKey
{
    string PetugasMedisId {get;}
}

public record PetugasMedisReff(string PetugasMedisId, string PetugasMedisName);