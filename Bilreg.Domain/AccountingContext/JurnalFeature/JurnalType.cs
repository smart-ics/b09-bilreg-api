using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Domain.AccountingContext.JurnalFeature;

public record JurnalType : IJurnalKey
{
    private readonly List<Jurnal2Base> _listJurnal2 = [];
    #region CREATION
    public JurnalType(string jurnalId, DateTime tglJurnal,
        AuditInfoType auditInfo, JurnalKetType keterangan,
        RegReff reg, PasienReff pasien,
        IEnumerable<Jurnal2Base> listJurnal2)
    {
        JurnalId = jurnalId;
        TglJurnal = tglJurnal;
        AuditInfo = auditInfo;
        Keterangan = keterangan;
        Reg = reg;
        Pasien = pasien;
        _listJurnal2 = listJurnal2.ToList();
    }

    public static JurnalType Default => new("-", DateTime.MinValue,
        AuditInfoType.Default, JurnalKetType.Default,
        RegModel.Default.ToReff(), PasienModel.Default.ToReff(),
        []);

    public static IJurnalKey Key(string id) => Default with { JurnalId = id };
    #endregion

    #region PROPERTIES
    public string JurnalId { get; init; }
    public DateTime TglJurnal { get; init; }
    public AuditInfoType AuditInfo { get; init; }
    public JurnalKetType Keterangan { get; init; }
    public RegReff Reg { get; init; }
    public PasienReff Pasien { get; init; }
    public IEnumerable<Jurnal2Base> ListJurnal2 => _listJurnal2;
    #endregion

    public void AddJurnal2(Jurnal2Base jurnal2)
    {
        _listJurnal2.Add(jurnal2);
    }
}

public interface IJurnalKey
{
    string JurnalId { get; }
}
public record JurnalKetType(string Keterangan, string RefBukti1, string RefBukti2, string RefBukti3)
{
    public static JurnalKetType Default => new JurnalKetType("", "", "", "");
}