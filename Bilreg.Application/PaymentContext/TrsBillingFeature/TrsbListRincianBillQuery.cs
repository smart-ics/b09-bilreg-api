using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.RekapCetakFeature;
using MediatR;
using System.Globalization;

namespace Bilreg.Application.PaymentContext.TrsBillingFeature;

public record TrsbListRincianBillQuery(string RegId) : IRequest<IEnumerable<TrsbRincianBillResponse>>, IRegKey;

public record TrsbRincianBillResponse(
    int No, string TglJam, LayananReff Layanan, string TrsBillingId, string Deskripsi, RekapCetakReff RekapCetak, decimal Nominal);

public class TrsbListRincianBillHandler : IRequestHandler<TrsbListRincianBillQuery, IEnumerable<TrsbRincianBillResponse>>
{
    private readonly ITrsBillingRepo _trsBillRepo;
    public TrsbListRincianBillHandler(ITrsBillingRepo trsBillRepo)
    {
        _trsBillRepo = trsBillRepo;
    }
    public Task<IEnumerable<TrsbRincianBillResponse>> Handle(TrsbListRincianBillQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.RegId);

        var listBill = _trsBillRepo.ListData(request)?
            .OrderBy(x => x.TglTrs)
            .ToList() ?? [];

        var response = listBill.Select((x, index) => new TrsbRincianBillResponse(
            index + 1,
            x.TglTrs.ToString("dd-MM-yyyy HH:mm:ss", CultureInfo.InvariantCulture),
            x.Layanan,
            x.TrsBillingId,
            x.Keterangan.Keterangan,
            x.RekapCetak,
            x.Total));
        return Task.FromResult(response);
    }
}
