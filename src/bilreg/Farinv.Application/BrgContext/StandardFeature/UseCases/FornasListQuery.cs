using Farinv.Domain.BrgContext.StandardFeature;
using MediatR;

namespace Farinv.Application.BrgContext.StandardFeature.UseCases;

public record FornasListQuery() : IRequest<IEnumerable<FornasListResponse>>;

public record FornasListResponse(string FornasId, string FornasName,
    string KelasTerapi, string KelasTerapi1, string KelasTerapi2, string KelasTerapi3,
    string NamaObat, string Sediaan, string Kekuatan, string Satuan,
    string MaksPeresepan, string RestriksiKelasTerapi, string RestriksiObat, string RestriksiSediaan);

public class FornasListHandler
    : IRequestHandler<FornasListQuery, IEnumerable<FornasListResponse>>
{
    private readonly IFornasRepo _fornasRepo;

    public FornasListHandler(IFornasRepo fornasRepo)
    {
        _fornasRepo = fornasRepo;
    }

    public Task<IEnumerable<FornasListResponse>> Handle(FornasListQuery request, 
        CancellationToken cancellationToken)
    {
        var listFornas = _fornasRepo.ListData()?.ToList() ?? [];
        return Task.FromResult(GenResponse(listFornas));
    }

    private static IEnumerable<FornasListResponse> GenResponse(List<FornasType> listFornas)
    {
        var response = listFornas
            .OrderBy(x => x.FornasName)
            .Select(x => new FornasListResponse(x.FornasId, x.FornasName,
                x.KelasTerapi, x.KelasTerapi1, x.KelasTerapi2, x.KelasTerapi3,
                x.NamaObat, x.Sediaan, x.Kekuatan, x.Satuan,
                x.MaksPeresepan, x.RestriksiKelasTerapi, x.RestriksiObat, x.RestriksiSediaan));

        return response;
    }
}