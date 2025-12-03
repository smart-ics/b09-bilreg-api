namespace Bilreg.Domain.AdmisiContext.PpaFeature;

public record PpaType : IPpaKey
{
    private readonly List<PpaLayananType> _listLayanan;
    private readonly List<PpaSatTugasType> _listSatTugas;
    public PpaType(string ppaId, string ppaName, 
        string namaSingkat, SmfType smf,
        GroupSpesialisType groupSpesialis,
        IEnumerable<PpaLayananType> listLayanan, 
        IEnumerable<PpaSatTugasType> listSatTugas)
    {
        PpaId = ppaId;
        PpaName = ppaName;
        NamaSingkat = namaSingkat;
        Smf = smf;
        GroupSpesialis = groupSpesialis;
        _listLayanan = listLayanan?.ToList() ?? [];
        _listSatTugas = listSatTugas?.ToList() ?? [];
    }
    
    public string PpaId { get; init; }
    public string PpaName { get; init; }
    public string NamaSingkat { get; init; }
    public SmfType Smf { get; init; }
    public GroupSpesialisType GroupSpesialis { get; init;  }
    public IEnumerable<PpaLayananType> ListLayanan => _listLayanan;
    public IEnumerable<PpaSatTugasType> ListSatTugas => _listSatTugas;
    
    public PpaReff ToReff() => new (PpaId, PpaName);
    
    public static PpaType Default => new("-", "-", "-", 
        SmfType.Default, GroupSpesialisType.Default, [], []);
    public static IPpaKey Key(string id) => Default with { PpaId = id };
}

public interface IPpaKey
{
    string PpaId {get;}
}

public record PpaReff(string PpaId, string PpaName);