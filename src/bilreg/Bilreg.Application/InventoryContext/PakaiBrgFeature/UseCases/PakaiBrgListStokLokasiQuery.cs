using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using MediatR;

namespace Bilreg.Application.InventoryContext.PakaiBrgFeature.UseCases;

public record PakaiBrgListStokLokasiQuery(string LayananId, string? Keyword = null)
    : IRequest<IEnumerable<PakaiBrgListStokLokasiResponse>>;

public record PakaiBrgListStokLokasiResponse(
    string BrgId,
    string BrgName,
    decimal TotalStok,
    string SatuanId,
    string SatuanName,
    string GroupRekDkId,
    decimal HppDefault,
    IReadOnlyList<PakaiBrgListStokBatchResponse> Batches
);

public record PakaiBrgListStokBatchResponse(
    string NoBatch,
    string TglEd,
    decimal QtySisa,
    decimal Hpp
);

public class PakaiBrgListStokLokasiHandler
    : IRequestHandler<PakaiBrgListStokLokasiQuery, IEnumerable<PakaiBrgListStokLokasiResponse>>
{
    private readonly IPakaiBrgRepo _pakaiBrgRepo;

    public PakaiBrgListStokLokasiHandler(IPakaiBrgRepo pakaiBrgRepo)
    {
        _pakaiBrgRepo = pakaiBrgRepo;
    }

    public Task<IEnumerable<PakaiBrgListStokLokasiResponse>> Handle(
        PakaiBrgListStokLokasiQuery request,
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.LayananId, nameof(request.LayananId));

        var list = _pakaiBrgRepo.ListStokLokasi(new LayananReff(request.LayananId, string.Empty)) ?? [];

        var filtered = list.Where(x => x.TotalStok > 0);

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var kw = request.Keyword.Trim();
            filtered = filtered.Where(x =>
                x.BrgName.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                x.BrgId.Contains(kw, StringComparison.OrdinalIgnoreCase));
        }

        var response = filtered.Select(x => new PakaiBrgListStokLokasiResponse(
            x.BrgId,
            x.BrgName,
            x.TotalStok,
            x.SatuanId,
            x.SatuanName,
            x.GroupRekDkId,
            x.HppDefault,
            x.Batches
                .Where(b => b.QtySisa > 0)
                .OrderBy(b => b.TglEd)
                .Select(b => new PakaiBrgListStokBatchResponse(
                    b.NoBatch,
                    b.TglEd == default ? string.Empty : b.TglEd.ToString("yyyy-MM-dd"),
                    b.QtySisa,
                    b.Hpp
                )).ToList()
        ));

        return Task.FromResult(response);
    }
}
