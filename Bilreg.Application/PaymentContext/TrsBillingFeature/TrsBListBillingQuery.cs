using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using MediatR;

namespace Bilreg.Application.PaymentContext.TrsBillingFeature;

public record TrsBListBillingQuery(string RegId) : IRequest<IEnumerable<TrsBListBillingResponse>>, IRegKey;

public record TrsBListBillingResponse(
    string TrsBillingId, RegReff Reg, LayananReff Layanan, string Deskripsi, decimal TotalNilai);

public class TrsBListBillingHandler : IRequestHandler<TrsBListBillingQuery, IEnumerable<TrsBListBillingResponse>>
{
    private readonly ITrsBillingRepo _trsBillRepo;

    public TrsBListBillingHandler(ITrsBillingRepo trsBillRepo)
    {
        _trsBillRepo = trsBillRepo;
    }

    public Task<IEnumerable<TrsBListBillingResponse>> Handle(TrsBListBillingQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.RegId);

        var listBill = _trsBillRepo.ListData(request)?.ToList() ?? [];
        var response = listBill.Select(x => new TrsBListBillingResponse(
            x.TrsBillingId, x.Reg, x.Layanan, x.Keterangan.Keterangan, x.Total));
        return Task.FromResult(response);
    }
}
