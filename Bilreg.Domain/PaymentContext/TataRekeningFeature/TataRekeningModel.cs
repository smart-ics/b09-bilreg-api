using Bilreg.Domain.AdmisiContext.RegFeature;

namespace Bilreg.Domain.PaymentContext.TataRekeningFeature;

public record TataRekeningModel : IRegKey
{
    private readonly List<TataRekeningPaymentType> _listTataRekeningPayment = [];
    public TataRekeningModel(string regId, TataRekeningStatusEnum status,
        IEnumerable<TataRekeningPaymentType> listTataRekeningPayment)
    {
        RegId = regId;
        Status = status;
        _listTataRekeningPayment = listTataRekeningPayment.ToList();
    }
    public string RegId { get; init; }
    public TataRekeningStatusEnum Status { get; init; }
    public IEnumerable<TataRekeningPaymentType> ListPayment => _listTataRekeningPayment;

    public void AddPayment(TataRekeningPaymentType payment)
    {
        
    }
    public void EnsureCanCreateTrsBill()
    {
        if (Status == TataRekeningStatusEnum.Closed)
            throw new InvalidOperationException(
                $"TrsBillType tidak dapat dibuat karena TataRekening untuk registrasi '{RegId}' berstatus Closed.");
    }
}
