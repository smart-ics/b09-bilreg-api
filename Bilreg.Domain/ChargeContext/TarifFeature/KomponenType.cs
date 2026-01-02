using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.PaymentContext.TrsBillingFeature;

namespace Bilreg.Domain.ChargeContext.TarifFeature;

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
    
    public CoaType RekPdpt { get; init; }
    public CoaType RekDiskon { get; init; }
    public IEnumerable<SatTugasType> ListSatTugas => _listSatTugas;
    #endregion
    
    #region BEHAVIOR
    public KomponenReff ToReff() => new(KomponenId, KomponenName);

    public bool IsValidPpa(PpaType ppa)
    {
        //  jika sat-tugas PPA dan Komponen ber-irisan, berarti valid
        if (!ListSatTugas.Any())
            return false;
        
        var ppaHasValidSatTugas = ppa.ListSatTugas
            .Select(x => x.SatTugas)
            .Any(ppaSatTugas => ListSatTugas
                .Any(kompSatTugas => ppaSatTugas.SatTugasId == kompSatTugas.SatTugasId));

        return ppaHasValidSatTugas;
    }
    #endregion
}

public interface IKomponenKey
{
    string KomponenId {get;}
}

public record KomponenReff(string KomponenId, string KomponenName) : IKomponenKey;

public record KomponenPpaView(KomponenType Komponen, PpaType Ppa);