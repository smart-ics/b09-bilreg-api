using Bilreg.Domain.AdmisiContext.LayananSub.LayananAgg;
using Bilreg.Domain.AdmisiContext.PetugasMedisSub.SatTugasAgg;
using Bilreg.Domain.AdmisiContext.PetugasMedisSub.SmfAgg;
using CommunityToolkit.Diagnostics;

namespace Bilreg.Domain.AdmisiContext.PetugasMedisSub.PetugasAgg;

public class PetugasMedisModel(string id, string name) : IPetugasMedisKey
{
    #region PROPERTIES
    public string PetugasMedisId { get; protected set; } = id;
    public string PetugasMedisName { get; protected set; } = name;
    public string NamaSingkat { get; protected set; } = string.Empty;
    public string SmfId { get; protected set; } = string.Empty;
    public string SmfName { get; protected set; } = string.Empty;

    public List<PetugasMedisSatTugasModel> ListSatTugas { get; protected set; } = [];
    public List<PetugasMedisLayananModel> ListLayanan { get; protected set; } = [];
    #endregion

    #region BEHAVIOUR

    public void Set(SmfModel smf)
    {
        ArgumentNullException.ThrowIfNull(smf);
        ArgumentNullException.ThrowIfNull(smf.SmfId);
        ArgumentNullException.ThrowIfNull(smf.SmfName);
        
        (SmfId, SmfName) = (smf.SmfId, smf.SmfName);
    }
    public void SetNama(string namaLengkap, string namaSingkat)
        => (PetugasMedisName, NamaSingkat) = (namaLengkap, namaSingkat);


    public void Add(SatuanTugasModel satTugas)
    {
        var newSatTugas = new PetugasMedisSatTugasModel(PetugasMedisId, satTugas.SatuanTugasId, satTugas.SatuanTugasName);
        ListSatTugas.Add(newSatTugas);
    }
    public void Attach(IEnumerable<PetugasMedisSatTugasModel> listSatTugas)
    {
        ArgumentNullException.ThrowIfNull(listSatTugas);
        var list = listSatTugas.ToList();
        if (list.Any(x => x.SatTugasId == string.Empty))
            throw new ArgumentException($"SatuanTugas ID kosong");
        if (list.Any(x => x.SatTugasName == string.Empty))
            throw new ArgumentException($"SatuanTugas Name kosong");
        ListSatTugas.AddRange(list);
    }
    public void Remove(Predicate<PetugasMedisSatTugasModel> predicate) 
        => _ = ListSatTugas.RemoveAll(predicate);
    

    public void Add(LayananModel layanan)
    {
        Guard.IsNotNull(layanan);
        var newLayanan = new PetugasMedisLayananModel(PetugasMedisId, layanan.LayananId, layanan.LayananName);
        ListLayanan.Add(newLayanan);
    }
    public void Attach(IEnumerable<PetugasMedisLayananModel> listLayanan)
    {
        ArgumentNullException.ThrowIfNull(listLayanan);
        var list = listLayanan.ToList();
        if (list.Any(x => x.LayananId == string.Empty))
            throw new ArgumentException($"SatuanTugas ID kosong");
        if (list.Any(x => x.LayananName == string.Empty))
            throw new ArgumentException($"SatuanTugas Name kosong");
        ListLayanan.AddRange(list);
    }
    public void Remove(Predicate<PetugasMedisLayananModel> predicate)
        => _ = ListLayanan.RemoveAll(predicate);
    public void SetAsSatTugasUtama(Predicate<PetugasMedisSatTugasModel> predicate)
    {
        ListSatTugas.ForEach(x => x.UnsetUtama());
        ListSatTugas.Find(predicate)?.SetUtama();
    }

    public void SyncId()
    {
        ListLayanan.ForEach(x => x.SetId(PetugasMedisId));
        ListSatTugas.ForEach(x => x.SetId(PetugasMedisId));
    }
    #endregion
}

public interface IPetugasMedisKey
{
    string PetugasMedisId { get; }
}
