namespace Bilreg.Domain.AdmisiContext.RegSub.RegAgg;

public abstract class AbstractRegModel
{
    private static readonly DateTime DefaultDate = new DateTime(3000, 1, 1);
    
    protected const string TIPEJAMINAN_UMUM_ID = "00000";
    protected const string TIPEJAMINAN_UMUM_NAME = "UMUM [BAYAR SENDIRI]";
    protected const string JAMINAN_UMUM_ID = "000";
    protected const string JAMINAN_UMUM_NAME = "UMUM";
    protected const string CARAMASUK_DATANGSENDIRI_ID = "8";
    protected const string CARAMASUK_DATANGSENDIRI_NAME = "DATANG SENDIRI";
    

    public string RegId { get; protected set; } = string.Empty;
    public DateTime RegDate { get; protected set; } = DefaultDate;
    public string UserId { get; protected set; } = string.Empty;
    public DateTime RegOutDate { get; protected set; } = DefaultDate;
    public string UserIdOut { get; protected set; } = string.Empty;
    
    public DateTime VoidDate { get; protected set; } = DefaultDate;
    public string UserIdVoid { get; protected set; } = string.Empty;
    
    #region PASIEN
    public string PasienId { get; protected set; } = string.Empty;
    public string NoMedRec { get; protected set; } = string.Empty;
    public string PasienName { get; protected set; } = string.Empty;
    public DateTime TglLahir { get; protected set; } = DefaultDate;
    public string Gender { get; protected set; } = string.Empty;
    #endregion

    #region TIPE-JAMINAN
    public string TipeJaminanId { get; protected set; } = TIPEJAMINAN_UMUM_ID;
    public string TipeJaminanName { get; protected set; } = TIPEJAMINAN_UMUM_NAME;
    public string JaminanId { get; protected set; } = JAMINAN_UMUM_ID;
    public string JaminanName { get; protected set; } = JAMINAN_UMUM_NAME;
    #endregion

    #region CARA-MASUK
    public string CaraMasukDkId { get; protected set; } = CARAMASUK_DATANGSENDIRI_ID;
    public string CaraMasukDkName { get; protected set; } = CARAMASUK_DATANGSENDIRI_NAME;
    public string RujukanId { get; protected set; } = string.Empty;
    public string RujukanName { get; protected set; } = string.Empty;
    public string RujukanReffNo { get; protected set; } = string.Empty;
    public DateTime RujukanDate { get; protected set; } = DefaultDate;
    #endregion
    
    public JenisRegEnum JenisReg { get; protected set; }
}