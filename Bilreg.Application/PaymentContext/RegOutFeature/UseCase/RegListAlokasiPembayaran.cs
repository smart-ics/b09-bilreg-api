using Ardalis.GuardClauses;
using Bilreg.Application.PaymentContext.RegOutFeature.RegOutAgg;
using Bilreg.Domain.AdmisiContext.RegFeature;
using MediatR;

namespace Bilreg.Application.PaymentContext.RegOutFeature.UseCase;

public record RegListAlokasiPembayaranQuery(string RegId) : IRequest<IEnumerable<RegListAlokasiPembayaranResponse>>, IRegKey;
public record RegListAlokasiPembayaranResponse(
    string CaraBayarId, string CaraBayarName, decimal Jasa, decimal Obat, decimal SubTotal);

public class RegListAlokasiPembayaranHandler : IRequestHandler<RegListAlokasiPembayaranQuery, IEnumerable<RegListAlokasiPembayaranResponse>>
{
    private readonly IRegPembayaranRepo _regPembayaranRepo;
    public RegListAlokasiPembayaranHandler(IRegPembayaranRepo regPembayaranRepo)
    {
        _regPembayaranRepo = regPembayaranRepo;
    }
    public Task<IEnumerable<RegListAlokasiPembayaranResponse>> Handle(RegListAlokasiPembayaranQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.RegId);
        var litAlokasiPembayaran = _regPembayaranRepo.ListData(request)?.ToList() ?? [];

        var result = litAlokasiPembayaran
            .Select(x => new RegListAlokasiPembayaranResponse(
                x.CaraBayarId, 
                x.CaraBayarName, 
                x.NilaiJasa, 
                x.NilaiObat, 
                x.NilaiSubTotal
        ));

        return Task.FromResult(result);
    }
}
