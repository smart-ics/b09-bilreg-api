namespace Bilreg.Domain.AdmisiContext.RegSub.RegAgg;

public abstract partial class AbstractRegModel : IRegKey
{
    private static readonly DateTime DefaultDate = new DateTime(3000, 1, 1);

    private const string TIPEJAMINAN_UMUM_ID = "00000";
    private const string TIPEJAMINAN_UMUM_NAME = "UMUM [BAYAR SENDIRI]";
    private const string JAMINAN_UMUM_ID = "000";
    private const string JAMINAN_UMUM_NAME = "UMUM";
    private const string CARAMASUK_DATANGSENDIRI_ID = "8";
    private const string CARAMASUK_DATANGSENDIRI_NAME = "DATANG SENDIRI";
    
    public string RegId { get; protected set; } = string.Empty;
    public DateTime RegDate { get; protected set; } = DefaultDate;
    public string UserId { get; protected set; } = string.Empty;
    public DateTime RegOutDate { get; protected set; } = DefaultDate;
    public string UserIdOut { get; protected set; } = string.Empty;
    public DateTime VoidDate { get; protected set; } = DefaultDate;
    public string UserIdVoid { get; protected set; } = string.Empty;
    public JenisRegEnum JenisReg { get; protected set; }
    
    #region PASIEN
    public string PasienId { get; protected set; } = string.Empty;
    public string NoMedRec { get; protected set; } = string.Empty;
    public string PasienName { get; protected set; } = string.Empty;
    public DateTime TglLahir { get; protected set; } = DefaultDate;
    public string Gender { get; protected set; } = string.Empty;
    #endregion

    public RegPasienVo Pasien { get; protected set; }
    public RegTipeJaminanVo TipeJaminan { get; protected set; }
    public RegCaraMasukVo CaraMasuk { get; protected set; }

}

public interface IRegKey
{
    string RegId { get; }
}