using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.PpaFeature;

namespace Bilreg.Domain.BillContext.TindakanFeature;

public record KomponenType : IKomponenKey
{
    private readonly List<SatTugasType> _listSatTugas;
    
    #region CREATION
    public KomponenType(string komponenId, string komponenName, 
        GroupKomponenType groupKomponen, IEnumerable<SatTugasType> listSatTugas)
    {
        Guard.Against.NullOrWhiteSpace(komponenId);
        Guard.Against.NullOrWhiteSpace(komponenName);
        Guard.Against.Null(groupKomponen);

        KomponenId = komponenId;
        KomponenName = komponenName;
        GroupKomponen = groupKomponen;
        _listSatTugas = listSatTugas?.ToList() ?? [];
    }
    public static KomponenType Default => new("-", "-", GroupKomponenType.Default, []);
    public static IKomponenKey Key(string id) => Default with { KomponenId = id };
    #endregion
    
    #region PROPERTIES
    public string KomponenId { get; init; }
    public string KomponenName { get; init; }
    public GroupKomponenType GroupKomponen { get; init; }
    public IEnumerable<SatTugasType> ListSatTugas { get; init; }
    #endregion
    
    #region BEHAVIOR
    public KomponenReff ToReff() => new(KomponenId, KomponenName);
    #endregion
}

public interface IKomponenKey
{
    string KomponenId {get;}
}

public record KomponenReff(string KomponenId, string KomponenName);