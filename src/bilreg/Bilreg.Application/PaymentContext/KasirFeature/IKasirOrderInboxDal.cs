namespace Bilreg.Application.PaymentContext.KasirFeature;

public interface IKasirOrderInboxDal
{
    IEnumerable<OrderKasirItemDto> ListOrderInbox(string? targetDate = null);
}
