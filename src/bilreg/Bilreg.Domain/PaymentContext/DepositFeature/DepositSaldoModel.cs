using Bilreg.Domain.AdmisiContext.RegFeature;

namespace Bilreg.Domain.PaymentContext.DepositFeature;

public record DepositSaldoModel(
    string RegId,
    decimal TotalSetoran,
    decimal TotalTerpakai,
    decimal SaldoAktif) : IRegKey;

public record DepositSaldoData(
    string RegId,
    decimal TotalSetoran,
    decimal TotalTerpakai,
    decimal SaldoAktif) : DepositSaldoModel(RegId, TotalSetoran, TotalTerpakai, SaldoAktif)
{
    public DepositSaldoData(DepositSaldoModel model)
        : this(model.RegId, model.TotalSetoran, model.TotalTerpakai, model.SaldoAktif)
    {
    }
}
