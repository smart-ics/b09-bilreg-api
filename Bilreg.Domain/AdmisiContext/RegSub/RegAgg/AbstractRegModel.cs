namespace Bilreg.Domain.AdmisiContext.RegSub.RegAgg;

public abstract partial class AbstractRegModel
{
    private static readonly DateTime DefaultDate = new DateTime(3000, 1, 1);
    
    private const string TIPEJAMINAN_DEFAULT_ID = "00000";
    private const string TIPEJAMINAN_DEFAULT_NAME = "UMUM [BAYAR SENDIRI]";
    private const string JAMINAN_DEFAULT_ID = "000";
    private const string JAMINAN_DEFAULT_NAME = "UMUM";
    private const string CARAMASUK_DEFAULT_ID = "8";
    private const string CARAMASUK_DEFAULT_NAME = "DATANG SENDIRI";
    

    public string RegId { get; protected set; } = string.Empty;
    public DateTime RegDate { get; protected set; } = DefaultDate;
    public DateTime RegOutDate { get; protected set; } = DefaultDate;
    public DateTime VoidDate { get; protected set; } = DefaultDate;
    
    #region PASIEN
    public string PasienId { get; protected set; } = string.Empty;
    public string NoMedRec { get; protected set; } = string.Empty;
    public string PasienName { get; protected set; } = string.Empty;
    public DateTime TglLahir { get; protected set; } = DefaultDate;
    public string Gender { get; protected set; } = string.Empty;
    #endregion

    #region TIPE-JAMINAN
    public string TipeJaminanId { get; protected set; } = TIPEJAMINAN_DEFAULT_ID;
    public string TipeJaminanName { get; protected set; } = TIPEJAMINAN_DEFAULT_NAME;
    public string JaminanId { get; protected set; } = JAMINAN_DEFAULT_ID;
    public string JaminanName { get; protected set; } = JAMINAN_DEFAULT_NAME;
    #endregion

    #region CARA-MASUK
    public string CaraMasukDkId { get; protected set; } = CARAMASUK_DEFAULT_ID;
    public string CaraMasukDkName { get; protected set; } = CARAMASUK_DEFAULT_NAME;
    public string RujukanId { get; protected set; } = string.Empty;
    public string RujukanName { get; protected set; } = string.Empty;
    public string RujukanReffNo { get; protected set; } = string.Empty;
    public DateTime RujukanDate { get; protected set; } = DefaultDate;
    #endregion
    
    public JenisRegEnum JenisReg { get; protected set; }
}