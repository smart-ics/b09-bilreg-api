namespace Bilreg.Application.PaymentContext.KasirFeature;

public interface IKasirShiftTotalsDal
{
    Task<KasirShiftTotalsDto> GetShiftTotalsAsync(string shiftId, CancellationToken cancellationToken = default);
}
