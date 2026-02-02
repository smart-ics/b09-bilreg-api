using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using MediatR;

namespace Bilreg.Application.PaymentContext.TrsBillingFeature;

public record TrBListBillingQuery(string RegId) : IRequest<IEnumerable<TrBListBillingResponse>>, IRegKey;

public record TrBListBillingResponse(
    string TrsBillingId, RegReff Reg, LayananReff Layanan, string Deskripsi, decimal TotalNilai);

public class TrBListBillingHandler : IRequestHandler<TrBListBillingQuery, IEnumerable<TrBListBillingResponse>>
{
    private readonly ITrsBillingRepo _trsBillRepo;

    public TrBListBillingHandler(ITrsBillingRepo trsBillRepo)
    {
        _trsBillRepo = trsBillRepo;
    }

    public Task<IEnumerable<TrBListBillingResponse>> Handle(TrBListBillingQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.RegId);

        var listBill = _trsBillRepo.ListData(request)?.ToList() ?? [];
        var response = listBill.Select(x => new TrBListBillingResponse(
            x.TrsBillingId, x.Reg, x.Layanan, x.Keterangan.Keterangan, x.Total));
        return Task.FromResult(response);
    }
}
