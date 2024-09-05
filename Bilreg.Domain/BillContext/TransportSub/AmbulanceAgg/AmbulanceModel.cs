namespace Bilreg.Domain.BillContext.TransportSub.AmbulanceAgg;

public class AmbulanceModel(string id, string name): IAmbulanceKey
{
    // PROPERTIES
    public string AmbulanceId { get; protected set; } = id;
    public string AmbulanceName { get; protected set; } = name;
    public bool IsAktif { get; protected set; } = true;
    public decimal Abonement { get; protected set; } = decimal.Zero;
    public List<AmbulanceKomponenModel> ListKomponen { get; protected set; } = [];

    // BEHAVIOUR
    public void SetAktif()  => IsAktif = true;
    public void UnSetAktif()  => IsAktif = false;
    public void SetName(string name) => AmbulanceName = name;
    public void SetAbonement(decimal abonement) => Abonement = abonement;

    public void Add(AmbulanceKomponenModel komponen) => ListKomponen.Add(komponen);

    public void Attach(IEnumerable<AmbulanceKomponenModel> listAmbulanceKomponen)
    {
        ArgumentNullException.ThrowIfNull(listAmbulanceKomponen);
        var list = listAmbulanceKomponen.ToList();
        if (list.Any(x => x.KomponenId == string.Empty))
            throw new ArgumentException("Komponen ID kosong");
        ListKomponen.AddRange(list);
    }

    public void Remove(Predicate<AmbulanceKomponenModel> predicate)
        => _ = ListKomponen.RemoveAll(predicate);

    public void SyncId() => ListKomponen.ForEach(x => x.SetAmbulanceId(AmbulanceId));
}

public interface IAmbulanceKey
{
    string AmbulanceId { get; }
}