using MediatR;

namespace Bilreg.Application.BillContext.TindakanSub.GrupTarifDkAgg;

public record GrupTarifDkListQuery():IRequest<IEnumerable<GrupTarifDkListResponse>>;
public record GrupTarifDkListResponse(string GrupTarifDkId, string GrupTarifDkName);

public class GrupTarifDkListHandler : IRequestHandler<GrupTarifDkListQuery, IEnumerable<GrupTarifDkListResponse>> 
{
    private IGrupTarifDkDal _grupTarifDkDal;

    public GrupTarifDkListHandler(IGrupTarifDkDal grupTarifDkDal)
    {
        _grupTarifDkDal = grupTarifDkDal;
    }

    public Task<IEnumerable<GrupTarifDkListResponse>> Handle(GrupTarifDkListQuery request, CancellationToken cancellationToken)
    {
        var result = _grupTarifDkDal.ListData()
            ?? throw new KeyNotFoundException("GrupTarifDk not Found");
        var response = result.Select(x => new GrupTarifDkListResponse(
            x.GrupTarifDkId,x.GrupTarifDkName
            ));
        return Task.FromResult(response);
    }
}
    