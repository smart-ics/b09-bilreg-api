namespace Bilreg.Domain.ChargeContext.AmbulanceFeature;

public class AmbulanceModel : IAmbulanceKey
{
    // PROPERTIES
    public string AmbulanceId { get; protected set; }
    public string AmbulanceName { get; protected set; }
    public bool IsAktif { get; protected set; } = true;
    public decimal Abonement { get; protected set; } = decimal.Zero;
    public string NoPolisi { get; protected set; } = string.Empty;
    public string TipeArmada { get; protected set; } = "Transport";
    public string StatusArmada { get; protected set; } = "Available";
    public List<AmbulanceKomponenModel> ListKomponen { get; protected set; } = [];
    public List<TransportTujuanModel> ListTujuan { get; protected set; } = [];

    // CONSTRUCTOR
    public AmbulanceModel(
        string id,
        string name,
        string noPolisi = "",
        string tipeArmada = "Transport",
        string statusArmada = "Available")
    {
        AmbulanceId = id;
        AmbulanceName = name;
        NoPolisi = noPolisi ?? string.Empty;
        TipeArmada = string.IsNullOrWhiteSpace(tipeArmada) ? "Transport" : tipeArmada;
        StatusArmada = string.IsNullOrWhiteSpace(statusArmada) ? "Available" : statusArmada;
    }

    // BEHAVIOUR
    public void SetAktif() => IsAktif = true;
    public void UnSetAktif() => IsAktif = false;
    public void SetName(string name) => AmbulanceName = name;
    public void SetAbonement(decimal abonement) => Abonement = abonement;
    public void SetNoPolisi(string noPolisi) => NoPolisi = noPolisi ?? string.Empty;
    public void SetTipeArmada(string tipeArmada) => TipeArmada = string.IsNullOrWhiteSpace(tipeArmada) ? "Transport" : tipeArmada;
    public void SetStatusArmada(string statusArmada) => StatusArmada = string.IsNullOrWhiteSpace(statusArmada) ? "Available" : statusArmada;
    public void SetAvailable() => StatusArmada = "Available";
    public void SetInUse() => StatusArmada = "InUse";
    public void SetMaintenance() => StatusArmada = "Maintenance";

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

    public void AddTujuan(TransportTujuanModel tujuan) => ListTujuan.Add(tujuan);

    public void AttachTujuan(IEnumerable<TransportTujuanModel> listTransportTujuan)
    {
        ArgumentNullException.ThrowIfNull(listTransportTujuan);
        var list = listTransportTujuan.ToList();
        if (list.Any(x => string.IsNullOrWhiteSpace(x.TujuanTransportId)))
            throw new ArgumentException("Tujuan Transport ID kosong");
        ListTujuan.AddRange(list);
    }

    public void RemoveTujuan(Predicate<TransportTujuanModel> predicate)
        => _ = ListTujuan.RemoveAll(predicate);

    public void SyncId()
    {
        ListKomponen.ForEach(x => x.SetAmbulanceId(AmbulanceId));
        ListTujuan.ForEach(x => x.SetAmbulanceId(AmbulanceId));
    }
}

public interface IAmbulanceKey
{
    string AmbulanceId { get; }
}

public record AmbulanceKey(string AmbulanceId) : IAmbulanceKey;
