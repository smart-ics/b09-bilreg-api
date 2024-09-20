using Bilreg.Domain.AdmisiContext.LayananSub.InstalasiDkAgg;
using Bilreg.Domain.AdmisiContext.LayananSub.LayananAgg;
using Bilreg.Domain.BillContext.RekapCetakSub.RekapCetakAgg;
using Bilreg.Domain.BillContext.TindakanSub.KomponenTarifAgg;
using Bilreg.Domain.BillContext.TindakanSub.TarifAgg;

namespace Bilreg.Domain.AdmisiContext.RegSub.KarcisAgg;

public partial class KarcisModel
{
    public void SetName(string name) => KarcisName = name;
    public void SetInstalasiDk(InstalasiDkModel instalasiDk)
    {
        InstalasiDkId = instalasiDk.InstalasiDkId;
        InstalasiDkName = instalasiDk.InstalasiDkName;
    }

    public void SetRekapCetak(RekapCetakModel rekapCetak)
    {
        RekapCetakId = rekapCetak.RekapCetakId;
        RekapCetakName = rekapCetak.RekapCetakName;
    }

    public void Activate() => IsAktif = true;
    public void Deactivate() => IsAktif = false;

    public void SetTarif(TarifModel tarif)
    {
        TarifId = tarif.TarifId;
        TarifName = tarif.TarifName;
    }

    public void AddKomponen(KomponenModel komponen, decimal nilai)
    {
        var newKomponen = new KarcisKomponenModel(KarcisId, 
            komponen.KomponenId, komponen.KomponenName, nilai);
        ListKomponen.Add(newKomponen);
    }

    public void RemoveKomponen(Predicate<KarcisKomponenModel> predicate)
        => ListKomponen.RemoveAll(predicate);
    
    public void AddLayanan(LayananModel layanan)
    {
        var newLayanan = new KarcisLayananModel(KarcisId, layanan.LayananId, layanan.LayananName);
        ListLayanan.Add(newLayanan);
    }
    public void RemoveLayanan(Predicate<KarcisLayananModel> predicate)
        => _ = ListLayanan.RemoveAll(predicate);

    public void Attach(IEnumerable<KarcisLayananModel> listKarcis)
    {
        ListLayanan.Clear();
        ListLayanan.AddRange(listKarcis);
    }

    public void Attach(IEnumerable<KarcisKomponenModel> listKomponen)
    {
        ListKomponen.Clear();
        ListKomponen.AddRange(listKomponen);
    }

    public void SyncId()
    {
        ListKomponen.ForEach(x => x.SetKarcisId(KarcisId));
        ListLayanan.ForEach(x => x.SetKarcisId(KarcisId));
    }
}