using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using MediatR;

namespace Bilreg.Application.ChargeContext.TindakanFeature.UseCases;

public record TindakanListQuery(string RegId, string PakaiBedId = "") : IRequest<IEnumerable<TindakanListResponse>>, IRegKey;

public record TindakanListResponse(
    string TindakanId,
    string TindakanDate,
    string OrderTdkId,
    RegReff Reg,
    LayananReff Layanan,
    TarifReff Tarif,
    string PakaiBedId = "");

public class TindakanListHandler : IRequestHandler<TindakanListQuery, IEnumerable<TindakanListResponse>>
{
    private readonly ITindakanRepo _tdkRepo;

    public TindakanListHandler(ITindakanRepo tdkRepo)
    {
        _tdkRepo = tdkRepo;
    }

    public Task<IEnumerable<TindakanListResponse>> Handle(TindakanListQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.RegId);

        var listTdk = _tdkRepo.ListData(request)?.ToList() ?? [];
        if (!string.IsNullOrWhiteSpace(request.PakaiBedId))
        {
            listTdk = listTdk.Where(x => x.PakaiBedId == request.PakaiBedId).ToList();
        }

        var result = listTdk.Select(x => new TindakanListResponse(
            x.TindakanId,
            x.TindakanDate.ToString("yyyy-MM-dd HH:mm:ss"),
            x.OrderTdkId,
            x.Reg,
            x.Layanan,
            x.Tarif,
            x.PakaiBedId
        ));

        return Task.FromResult(result);
    }
}
