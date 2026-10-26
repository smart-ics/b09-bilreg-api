using Bilreg.Domain.PaymentContext.DepositFeature;

namespace Bilreg.Infrastructure.PaymentContext.DepositFeature;

public record DepositSaldoDto(
    string RegId = "",
    decimal TotalSetoran = 0,
    decimal TotalTerpakai = 0,
    decimal SaldoAktif = 0)
{
    public DepositSaldoModel ToModel() =>
        new(RegId, TotalSetoran, TotalTerpakai, SaldoAktif);

    public static DepositSaldoDto FromModel(DepositSaldoModel model) =>
        new(model.RegId, model.TotalSetoran, model.TotalTerpakai, model.SaldoAktif);
}
