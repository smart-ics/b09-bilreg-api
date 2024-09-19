using Bilreg.Domain.BillContext.RekapCetakSub.GrupRekapCetakAgg;
using Bilreg.Domain.BillContext.RekapCetakSub.RekapCetakDkAgg;
using CommunityToolkit.Diagnostics;

namespace Bilreg.Domain.BillContext.RekapCetakSub.RekapCetakAgg;

public partial class RekapCetakModel
{
    public void SetNoUrut(int noUrut) => NoUrut = noUrut;
    public void SetLevel(int level) => Level = level;
    public void SetAsGrupBaru(bool value) => IsGrupBaru = value;

    public void SetRekapCetak(GrupRekapCetakModel grupRekapCetak)
    {
        Guard.IsNotNull(grupRekapCetak);

        GrupRekapCetakId = grupRekapCetak.GrupRekapCetakId;
        GrupRekapCetakName = grupRekapCetak.GrupRekapCetakName;
    }

    public void SetRekapCetakDk(RekapCetakDkModel rekapCetakDk)
    {
        Guard.IsNotNull(rekapCetakDk);
        
        RekapCetakDkId = rekapCetakDk.RekapCetakDkId;
        RekapCetakDkName = rekapCetakDk.RekapCetakDkName;
    }
    
}