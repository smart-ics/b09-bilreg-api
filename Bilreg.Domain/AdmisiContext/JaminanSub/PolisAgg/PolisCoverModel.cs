using Bilreg.Domain.PasienContext.DataSosialPasienSub.PasienAgg;

namespace Bilreg.Domain.AdmisiContext.JaminanSub.PolisAgg;

public class PolisCoverModel : IPolisKey, IPasienKey
{
    public PolisCoverModel(string id)
    {
        PolisId = id;
    }
    public string PolisId { get; protected set; }
    public string PasienId { get; protected set; }
    public string PasienName { get; protected set; }
    public string Status { get; protected set; }
    public string StatusDesc { get; protected set; }
    public DateTime ExpiredDate { get; protected set; }
    
    internal void WithPasien(PasienModel pasien)
    {
        ArgumentNullException.ThrowIfNull(pasien);
        ArgumentException.ThrowIfNullOrWhiteSpace(pasien.PasienId);
        ArgumentException.ThrowIfNullOrWhiteSpace(pasien.PasienName);
        
        PasienId = pasien.PasienId;
        PasienName = pasien.PasienName;
    }

    internal void SetStatusPeserta(string statusPeserta)
    {
        
    }
}