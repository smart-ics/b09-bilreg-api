using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.PaymentContext.DepositFeature;

public class DepositModel : IDepositId
{
    public DepositModel(string depositId, DateTime depositDate,
        RegReff reg, LayananReff layanan, string ketarangan,
        decimal nilaiDeposit, AuditTrailType audit)
    {
        DepositId = depositId;
        DepositDate = depositDate;
        Reg = reg;
        Layanan = layanan;
        Keterangan = ketarangan;
        NilaiDeposit = nilaiDeposit;
        Audit = audit;
    }

    #region PROPERTY
    public string DepositId { get; init; }
    public DateTime DepositDate { get; init; }
    public RegReff Reg { get; init; }
    public LayananReff Layanan { get; init; }
    public string Keterangan { get; init; }
    public decimal NilaiDeposit { get; init; }
    public AuditTrailType Audit { get; init; }
    #endregion

    public static DepositModel Create(RegModel reg, LayananType layanan, string keterangan, decimal nilaiDeposit, AuditTrailType audit)
    {
        var newId = NunaId.NewLegacy("DP", 'A');
        return new DepositModel(newId, DateTime.Now, reg.ToReff(), layanan.ToReff(), keterangan, nilaiDeposit, audit);
    }
}

public interface IDepositId
{
    string DepositId { get; }
}