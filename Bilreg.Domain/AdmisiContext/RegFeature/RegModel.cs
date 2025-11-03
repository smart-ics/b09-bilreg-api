using Bilreg.Domain.PasienContext.PasienFeature;

namespace Bilreg.Domain.AdmisiContext.RegFeature;

public class RegModel : IRegKey
{
    private static readonly DateTime DefaultDate = new DateTime(3000, 1, 1);
    private const string CARAMASUK_DATANGSENDIRI_ID = "8";
    
    public string RegId { get; init; }
    public DateOnly TglReg { get; init; }
    public PasienReff Pasien { get; init; }
    public JenisRegEnum JenisReg { get; private set; }
    // public TglJamTrsType TglJamTrs { get; private set; }
    // public VoidFlagType VoidFlag { get; private set; } = new(new DateTime(3000, 1, 1), "");
    // public PasienReff Pasien { get; private set; }
    //
    // public TipeJaminanViewType TipeJaminan { get; private set; }
    // public RegCaraMasukVo CaraMasuk { get; private set; }
    // public KarcisTarifVo KarcisTarif { get; private set; }
    // public RegKelasVo Kelas { get; private set; }
    
    public static IRegKey Key(string id) => new RegModel{RegId = id};    
}

