using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.InventoryContext.MutasiFeature;
using MediatR;

namespace Bilreg.Application.InventoryContext.MutasiFeature.UseCases;

public record MutasiBrgGetQuery(string MutasiBrgId) 
    : IRequest<MutasiBrgGetResponse>, IMutasiBrgKey;

public record MutasiBrgGetResponse(
    string MutasiBrgId,
    string MutasiBrgDate,
    string State,
    LayananReff LayananAsal,
    LayananReff LayananTujuan,
    string OrderMutasiId,
    string DispatchNote,
    string IssuerUserId,
    IEnumerable<MutasiBrgGetItemResponse> ListItem)
{
    public string LayananAsalId => LayananAsal?.LayananId ?? string.Empty;
    public string LayananAsalName => LayananAsal?.LayananName ?? string.Empty;
    public string LayananTujuanId => LayananTujuan?.LayananId ?? string.Empty;
    public string LayananTujuanName => LayananTujuan?.LayananName ?? string.Empty;
}

public record MutasiBrgGetItemResponse(
    int NoUrut,
    string BrgId,
    string BrgName,
    string BatchNo,
    string ExpiredDate,
    decimal QtyKirim,
    string SatuanId,
    string SatuanName);

public class MutasiBrgGetHandler : IRequestHandler<MutasiBrgGetQuery, MutasiBrgGetResponse>
{
    private readonly IMutasiBrgRepo _mutasiBrgRepo;

    public MutasiBrgGetHandler(IMutasiBrgRepo mutasiBrgRepo)
    {
        _mutasiBrgRepo = mutasiBrgRepo;
    }

    public Task<MutasiBrgGetResponse> Handle(MutasiBrgGetQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.MutasiBrgId, nameof(request.MutasiBrgId));

        var model = _mutasiBrgRepo.LoadEntity(request)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"MutasiBrg '{request.MutasiBrgId}' not found")
            );

        return Task.FromResult(GenResponse(model));
    }

    private static MutasiBrgGetResponse GenResponse(MutasiBrgModel model)
    {
        var items = model.ListItem
            .Select(x => new MutasiBrgGetItemResponse(
                x.NoUrut,
                x.Brg.BrgId,
                x.Brg.BrgName,
                x.BatchNo,
                x.ExpiredDate,
                x.QtyKirim,
                x.Satuan.SatuanId,
                x.Satuan.SatuanName
            ))
            .ToList();

        var dateStr = model.MutasiBrgDate == default 
            ? string.Empty 
            : model.MutasiBrgDate.ToString("yyyy-MM-dd HH:mm:ss");

        return new MutasiBrgGetResponse(
            model.MutasiBrgId,
            dateStr,
            model.State.ToString(),
            model.LayananAsal,
            model.LayananTujuan,
            model.OrderMutasiId,
            model.DispatchNote,
            model.IssuerUserId,
            items
        );
    }
}
