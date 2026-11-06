using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.PaymentContext.DepositFeature;

public class DepositModel : IDepositId
{
    public DepositModel(string depositId, DateTime depositDate,
        RegReff reg, LayananReff layanan, string ketarangan,
        decimal nilaiDeposit, AuditTrailType audit,
        string noKwitansi = "", string petugasKasir = "", decimal totalBayar = 0)
    {
        DepositId = depositId;
        DepositDate = depositDate;
        Reg = reg;
        Layanan = layanan;
        Keterangan = ketarangan;
        NilaiDeposit = nilaiDeposit;
        Audit = audit;
        NoKwitansi = noKwitansi ?? string.Empty;
        PetugasKasir = petugasKasir ?? string.Empty;
        TotalBayar = totalBayar;
    }

    #region PROPERTY
    public string DepositId { get; init; }
    public DateTime DepositDate { get; init; }
    public RegReff Reg { get; init; }
    public LayananReff Layanan { get; init; }
    public string Keterangan { get; init; }
    public decimal NilaiDeposit { get; init; }
    public AuditTrailType Audit { get; init; }
    public string NoKwitansi { get; init; }
    public string PetugasKasir { get; init; }
    public decimal TotalBayar { get; init; }
    public bool IsPaid => TotalBayar > 0 && !string.IsNullOrWhiteSpace(PetugasKasir);
    #endregion

    public static DepositModel Create(RegReff reg, LayananType layanan, string keterangan, decimal nilaiDeposit, AuditTrailType audit)
    {
        var newId = NunaId.NewLegacy("DP", 'A');
        return new DepositModel(newId, DateTime.Now, reg, layanan.ToReff(), keterangan, nilaiDeposit, audit);
    }

    public static DepositModel Create(RegModel reg, LayananType layanan, string keterangan, decimal nilaiDeposit, AuditTrailType audit)
    {
        return Create(reg.ToReff(), layanan, keterangan, nilaiDeposit, audit);
    }
}

public interface IDepositId
{
    string DepositId { get; }
}
