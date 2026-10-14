using Ardalis.GuardClauses;
using Bilreg.Domain.ApotekContext.TelaahResepFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.ApotekContext.TelaahResepFeature.UseCases;

public record TelaahKajianItemResponse(
    int NoUrut,
    string KajianResepId,
    string KajianResepName,
    string GrupKajianResepId,
    string GrupKajianResepName,
    bool IsYes,
    string CatatanKajian,
    bool RequiresNote);

public record TelaahItemDetailResponse(
    int ItemNo,
    int ResepKerjaItemNo,
    string PrescribedBrgId,
    string PrescribedBrgName,
    decimal PrescribedQty,
    string Disposition,
    string AcceptedBrgId,
    string AcceptedBrgName,
    decimal AcceptedQty,
    string Reason,
    string PharmacistId);

public record TelaahDetailResponse(
    string TelaahResepId,
    string ResepKerjaId,
    string RegId,
    string TelaahStatus,
    string PharmacistId,
    DateTime StartedAt,
    DateTime CompletedAt,
    int Version,
    IReadOnlyList<TelaahKajianItemResponse> KajianItems,
    IReadOnlyList<TelaahItemDetailResponse> Items);

public record TelaahGetQuery(string TelaahResepId) : IRequest<TelaahDetailResponse>;

public record TelaahGetByResepKerjaQuery(string ResepKerjaId) : IRequest<TelaahDetailResponse>;

public class TelaahGetHandler : IRequestHandler<TelaahGetQuery, TelaahDetailResponse>
{
    private readonly ITelaahResepRepo _repo;

    public TelaahGetHandler(ITelaahResepRepo repo)
    {
        _repo = repo;
    }

    public Task<TelaahDetailResponse> Handle(TelaahGetQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.TelaahResepId, nameof(request.TelaahResepId));
        var telaah = _repo.LoadEntity(TelaahResepModel.Key(request.TelaahResepId))
            .GetValueOrThrow($"Telaah '{request.TelaahResepId}' not found");
        return Task.FromResult(MapToDetail(telaah));
    }

    internal static TelaahDetailResponse MapToDetail(TelaahResepModel telaah)
    {
        var kajianItems = telaah.KajianItems.Select(k => new TelaahKajianItemResponse(
            k.NoUrut,
            k.KajianResepId,
            k.KajianResepName,
            k.GrupKajianResepId,
            k.GrupKajianResepName,
            k.IsYes,
            k.CatatanKajian,
            k.RequiresNote
        )).ToList();

        var items = telaah.Items.Select(i => new TelaahItemDetailResponse(
            i.ItemNo,
            i.ResepKerjaItemNo,
            i.PrescribedBrgId,
            i.PrescribedBrgName,
            i.PrescribedQty,
            i.Disposition.ToString(),
            i.AcceptedBrgId,
            i.AcceptedBrgName,
            i.AcceptedQty,
            i.Reason,
            i.PharmacistId
        )).ToList();

        return new TelaahDetailResponse(
            telaah.TelaahResepId,
            telaah.ResepKerjaId,
            telaah.RegId,
            telaah.TelaahStatus.ToString(),
            telaah.PharmacistId,
            telaah.StartedAt,
            telaah.CompletedAt,
            telaah.Version,
            kajianItems,
            items
        );
    }
}

public class TelaahGetByResepKerjaHandler : IRequestHandler<TelaahGetByResepKerjaQuery, TelaahDetailResponse>
{
    private readonly ITelaahResepRepo _repo;

    public TelaahGetByResepKerjaHandler(ITelaahResepRepo repo)
    {
        _repo = repo;
    }

    public Task<TelaahDetailResponse> Handle(TelaahGetByResepKerjaQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.ResepKerjaId, nameof(request.ResepKerjaId));
        var telaah = _repo.LoadByResepKerja(request.ResepKerjaId)
            .GetValueOrThrow($"Telaah for Resep Kerja '{request.ResepKerjaId}' not found");
        return Task.FromResult(TelaahGetHandler.MapToDetail(telaah));
    }
}
