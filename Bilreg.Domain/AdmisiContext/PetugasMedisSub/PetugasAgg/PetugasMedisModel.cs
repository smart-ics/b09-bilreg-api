using Bilreg.Domain.AdmisiContext.LayananSub.LayananAgg;
using Bilreg.Domain.AdmisiContext.PetugasMedisSub.SatTugasAgg;
using Bilreg.Domain.AdmisiContext.PetugasMedisSub.SmfAgg;

namespace Bilreg.Domain.AdmisiContext.PetugasMedisSub.PetugasAgg;

public class PetugasMedisModel(string id, string name)
{
    #region PROPERTIES
    public string PetugasId { get; private set; } = id;
    public string PetugasName { get; private set; } = name;
    public string NamaSingkat { get; private set; } = string.Empty;
    public string SmfId { get; private set; } = string.Empty;
    public string SmfName { get; private set; } = string.Empty;
    public List<PetugasMedisSatTugasModel> ListSatTugas { get; private set; } = [];
    public List<PetugasMedisLayananModel> ListLayanan { get; private set; } = [];
    #endregion

    #region BEHAVIOUR

    public void Set(SmfModel smf)
    {
        ArgumentNullException.ThrowIfNull(smf);
        ArgumentException.ThrowIfNullOrWhiteSpace(smf.SmfId);
        ArgumentException.ThrowIfNullOrWhiteSpace(smf.SmfName);
        
        (SmfId, SmfName) = (smf.SmfId, smf.SmfName);
    }
    
    public void Add(SatuanTugasModel satTugas)
    {
        ArgumentNullException.ThrowIfNull(satTugas);
        ArgumentException.ThrowIfNullOrWhiteSpace(satTugas.SatuanTugasId);
        ArgumentException.ThrowIfNullOrWhiteSpace(satTugas.SatuanTugasName);
        if (ListSatTugas.Any(x => x.SatTugasId == satTugas.SatuanTugasId))
            throw new ArgumentException("Add Satuan Tugas Failed: Duplicated");
        
        var newSatTugas = new PetugasMedisSatTugasModel(PetugasId, satTugas.SatuanTugasId, satTugas.SatuanTugasName);
        ListSatTugas.Add(newSatTugas);
    }
    
    public void Remove(Predicate<PetugasMedisSatTugasModel> predicate) 
        => _ = ListSatTugas.RemoveAll(predicate);
    
    public void Add(LayananModel layanan)
    {
        ArgumentNullException.ThrowIfNull(layanan);
        ArgumentException.ThrowIfNullOrWhiteSpace(layanan.LayananId);
        ArgumentException.ThrowIfNullOrWhiteSpace(layanan.LayananName);
        if (ListLayanan.Any(x => x.LayananId == layanan.LayananId))
            throw new ArgumentException("Add Layanan Failed: Duplicated");
        
        var newLayanan = new PetugasMedisLayananModel(PetugasId, layanan.LayananId, layanan.LayananName);
        ListLayanan.Add(newLayanan);
    }
    
    public void Remove(Predicate<PetugasMedisLayananModel> predicate)
        => _ = ListLayanan.RemoveAll(predicate);

    public void SetAsSatTugasUtama(Predicate<PetugasMedisSatTugasModel> predicate)
    {
        ListSatTugas.ForEach(x => x.UnsetUtama());
        ListSatTugas.Find(predicate)?.SetUtama();
    }
    #endregion
}