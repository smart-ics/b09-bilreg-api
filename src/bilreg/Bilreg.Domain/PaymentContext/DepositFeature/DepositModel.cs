using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Domain.PaymentContext.DepositFeature;

public class DepositModel : IDepositId
{
    public DepositModel(string depositId, DateTime depositDate,
        RegReff reg, LayananReff layanan, string ketarangan,
        decimal nilaiDeposit, AuditInfoType auditInfo)
    {
        DepositId = depositId;
        DepositDate = depositDate;
        Reg = reg;
        Layanan = layanan;
        Keterangan = ketarangan;
        NilaiDeposit = nilaiDeposit;
        AuditInfo = auditInfo;
    }

    #region PROPERTY
    public string DepositId { get; init; }
    public DateTime DepositDate { get; init; }
    public RegReff Reg { get; init; }
    public LayananReff Layanan { get; init; }
    public string Keterangan { get; init; }
    public decimal NilaiDeposit { get; init; }
    public AuditInfoType AuditInfo { get; init; }
    #endregion

    public static DepositModel Create(RegModel reg, LayananType layanan, string keterangan, decimal nilaiDeposit, AuditInfoType auditInfo)
    {
        var newId = Nuna.Lib.AutoNumberHelper.NunaId.New("DEP");
        return new DepositModel(newId, DateTime.Now, reg.ToReff(), layanan.ToReff(), keterangan, nilaiDeposit, auditInfo);
    }
}

public interface IDepositId
{
    string DepositId { get; }
}