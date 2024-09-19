using Bilreg.Domain.BillContext.RekapCetakSub.RekapCetakAgg;

namespace Bilreg.Infrastructure.BillContext.RekapCetakSub.RekapCetakAgg;

public class RekapCetakDto() : RekapCetakModel("","")
{
    public void SetTestData()
    {
        RekapCetakId = "A";
        RekapCetakName = "B";
        NoUrut = 1;
        IsGrupBaru = true;
        Level = 2;
        GrupRekapCetakId = "C";
        GrupRekapCetakName = "";
        RekapCetakDkId = "D";
        RekapCetakDkName = "";
    }
    public string fs_kd_rekap_cetak_tarif {get => RekapCetakId; set => RekapCetakId = value;}
    public string fs_nm_rekap_cetak_tarif {get => RekapCetakName; set => RekapCetakName = value;}
    public string fs_urut {get => NoUrut.ToString(); set => NoUrut = Convert.ToInt16(value);}
    public bool fb_grup_baru {get => IsGrupBaru; set => IsGrupBaru = value;}
    public int fn_level {get => Level; set => Level = value;}
    public string fs_kd_grup_rekap_cetak {get => GrupRekapCetakId; set => GrupRekapCetakId = value;}
    public string fs_nm_grup_rekap_cetak {get => GrupRekapCetakName; set => GrupRekapCetakName = value;}
    public string fs_kd_rekap_cetak_dk {get => RekapCetakDkId; set => RekapCetakDkId = value;}
    public string fs_nm_rekap_cetak_dk {get => RekapCetakDkName; set => RekapCetakDkName = value;}
}