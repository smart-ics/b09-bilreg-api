using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PaymentContext.PasienBalanceFeature;

namespace Bilreg.Application.PaymentContext.PasienBalanceFeature;

public class PasienBalanceLoader : IPasienBalanceLoader
{
    private readonly IPasienBalanceRepo _repo;
    private readonly PasienBalanceBootstrapService _bootstrap;

    public PasienBalanceLoader(
        IPasienBalanceRepo repo,
        PasienBalanceBootstrapService bootstrap)
    {
        _repo = repo;
        _bootstrap = bootstrap;
    }

    public PasienBalanceModel Load(IPasienKey key)
    {
        return _repo.LoadEntity(key).Match(
            onSome: model => model,
            onNone: () =>
            {
                var model = _bootstrap.Bootstrap(key);
                _repo.SaveChanges(model);
                return model;
            });
    }
}
