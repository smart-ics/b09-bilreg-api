namespace Bilreg.Application.PaymentContext.KasirFeature;

public interface ITrsJenisPaymentDal
{
    IEnumerable<JenisPaymentDto> ListAktif();
    IEnumerable<JenisPaymentGroupDto> ListAktifGrouped();
    JenisPaymentDto? GetData(string kodeJenisPayment);
}
