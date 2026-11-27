namespace Bilreg.Application.PaymentContext.KasirFeature;

public interface IKasirOrderInboxDal
{
    Task<IEnumerable<OrderKasirItemDto>> ListOrderInboxAsync(string? targetDate = null, CancellationToken cancellationToken = default);
}
