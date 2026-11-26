using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.InventoryContext.MutasiFeature;
using MediatR;

namespace Bilreg.Application.InventoryContext.MutasiFeature.UseCases;

public record TerimaMutasiGetQuery(string TerimaMutasiId)
    : IRequest<TerimaMutasiGetResponse>, ITerimaMutasiKey;

public record TerimaMutasiGetResponse(
    string TerimaMutasiId,
    string TerimaMutasiDate,
    string State,
    string MutasiBrgId,
    string OrderMutasiId,
    LayananReff LayananAsal,
    LayananReff LayananTujuan,
    string ReceiverUserId,
    string Note,
    IEnumerable<TerimaMutasiGetItemResponse> ListItem)
{
    public static TerimaMutasiGetResponse FromModel(TerimaMutasiModel model)
    {
        var items = model.ListItem
            .Select(x => new TerimaMutasiGetItemResponse(
                x.NoUrut,
                x.Brg.BrgId,
                x.Brg.BrgName,
                x.BatchNo,
                x.ExpiredDate,
                x.QtyTerima,
                x.Satuan.SatuanId,
                x.Satuan.SatuanName))
            .ToList();

        var dateStr = model.TerimaMutasiDate == default
            ? string.Empty
            : model.TerimaMutasiDate.ToString("yyyy-MM-dd HH:mm:ss");

        return new TerimaMutasiGetResponse(
            model.TerimaMutasiId,
            dateStr,
            model.State.ToString(),
            model.MutasiBrgId,
            model.OrderMutasiId,
            model.LayananAsal,
            model.LayananTujuan,
            model.ReceiverUserId,
            model.Note,
            items);
    }
}

public record TerimaMutasiGetItemResponse(
    int NoUrut,
    string BrgId,
    string BrgName,
    string BatchNo,
    string ExpiredDate,
    decimal QtyTerima,
    string SatuanId,
    string SatuanName);

public class TerimaMutasiGetHandler : IRequestHandler<TerimaMutasiGetQuery, TerimaMutasiGetResponse>
{
    private readonly ITerimaMutasiRepo _terimaMutasiRepo;

    public TerimaMutasiGetHandler(ITerimaMutasiRepo terimaMutasiRepo)
    {
        _terimaMutasiRepo = terimaMutasiRepo;
    }

    public Task<TerimaMutasiGetResponse> Handle(TerimaMutasiGetQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.TerimaMutasiId, nameof(request.TerimaMutasiId));

        var model = _terimaMutasiRepo.LoadEntity(request)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"TerimaMutasi '{request.TerimaMutasiId}' not found")
            );

        return Task.FromResult(TerimaMutasiGetResponse.FromModel(model));
    }
}
