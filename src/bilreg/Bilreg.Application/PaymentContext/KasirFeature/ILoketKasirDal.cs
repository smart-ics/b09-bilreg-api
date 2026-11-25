namespace Bilreg.Application.PaymentContext.KasirFeature;

public interface ILoketKasirDal
{
    IEnumerable<LoketKasirDto> ListAktif();
    IEnumerable<LoketKasirDto> ListData();
    LoketKasirDto? GetData(string loketId);
}
