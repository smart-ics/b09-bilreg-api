using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.PetugasMedisSub;

namespace Bilreg.Domain.BillContext.TindakanSub.TarifFeature;

public record KomponenType : IKomponenKey
{
    #region CONSTRUCTOR
    public KomponenType(string komponenId, string komponenName, 
        GroupKomponenType groupKomponen, IList<SatTugasType> listSatTugas)
    {
        Guard.Against.NullOrWhiteSpace(komponenId, nameof(komponenId));
        Guard.Against.NullOrWhiteSpace(komponenName, nameof(komponenName));
        Guard.Against.Null(groupKomponen, nameof(groupKomponen));
        Guard.Against.Null(listSatTugas, nameof(listSatTugas));

        KomponenId = komponenId;
        KomponenName = komponenName;
        GroupKomponen = groupKomponen;
        ListSatTugas = listSatTugas.ToList();
    }
    #endregion
    
    #region PROPERTIES
    public string KomponenId { get; init; }
    public string KomponenName { get; init; }
    public GroupKomponenType GroupKomponen { get; init; }
    public IReadOnlyList<SatTugasType> ListSatTugas { get; init; }
    #endregion
    
    #region BEHAVIOR
    public KomponenReff ToReff() => new(KomponenId, KomponenName);
    #endregion
    
    #region STATIC-FACTORY
    public static KomponenType Default => new("-", "-", GroupKomponenType.Default, []);
    public static IKomponenKey Key(string id) => Default with { KomponenId = id };
    #endregion
}

public interface IKomponenKey
{
    string KomponenId {get;}
}

public record KomponenReff(string KomponenId, string KomponenName);