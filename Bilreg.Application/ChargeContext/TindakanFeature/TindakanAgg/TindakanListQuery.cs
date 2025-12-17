using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using MediatR;

namespace Bilreg.Application.ChargeContext.TindakanFeature.TindakanAgg;

public record TindakanListQuery(string RegId) : IRequest<IEnumerable<TindakanListResponse>>, IRegKey;

public record TindakanListResponse(string TindakanId, string TindakanDate,
    TdkOrderReffResponse OrderTdk, RegReff Reg, LayananReff Layanan, 
    TipeTarifReff TipeTarif, TarifReff Tarif);

public record TdkOrderReffResponse(string OrderTdkId, string OrderDate, TarifReff Tindakan);
public class TindakanListHandler : IRequestHandler<TindakanListQuery, IEnumerable<TindakanListResponse>>
{
    private readonly ITindakanRepo _tdkRepo;

    public TindakanListHandler(ITindakanRepo tdkRepo)
    {
        _tdkRepo = tdkRepo;
    }

    public Task<IEnumerable<TindakanListResponse>> Handle(TindakanListQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.RegId));

        var listTdk = _tdkRepo.ListData(request)?.ToList() ?? [];
        var result = listTdk.Select(x => new TindakanListResponse
            (   
                x.TindakanId, x.TindakanDate.ToString("yyyy-MM-dd HH:mm:ss"), 
                new TdkOrderReffResponse(x.OrderTdk.OrderTdkId, 
                x.OrderTdk.OrderDate.ToString("yyyy-MM-dd HH:mm:ss"), x.OrderTdk.Tindakan),
                x.reg, x.Layanan, x.TipeTarif, x.Tarif)
            );
        return Task.FromResult(result);
    }
}
