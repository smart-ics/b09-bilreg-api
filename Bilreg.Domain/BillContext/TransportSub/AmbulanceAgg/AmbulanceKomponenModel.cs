using Bilreg.Domain.BillContext.TindakanSub.KomponenTarifAgg;

namespace Bilreg.Domain.BillContext.TransportSub.AmbulanceAgg;

public class AmbulanceKomponenModel(string ambulanceId, string komponenId, decimal nilaiTarif, bool isTetap): IAmbulanceKey, IKomponenTarifKey
{ 
    // PROPERTIES
    public string AmbulanceId { get; protected set; } = ambulanceId;
    public string KomponenId { get; protected set; } = komponenId;
    public decimal NilaiTarif { get; protected set; } = nilaiTarif;
    public bool IsTetap { get; protected set; } = isTetap;

    // BEHAVIOUR
    public void SetTetap() => IsTetap = true;
    public void UnSetTetap() => IsTetap = false;
    public void SetAmbulanceId(string id) => AmbulanceId = id;
}