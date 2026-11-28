using Ardalis.GuardClauses;
using MediatR;

namespace Bilreg.Application.InventoryContext.MutasiFeature.UseCases;

public record MutasiBrgListStokLokasiQuery(string LayananId, string? Keyword = null)
    : IRequest<IEnumerable<MutasiBrgListStokLokasiResponse>>;

public record MutasiBrgListStokLokasiResponse(
    string BrgId,
    string BrgName,
    decimal TotalStok,
    string SatuanId,
    string SatuanName,
    string GroupRekDkId,
    decimal HppDefault,
    IReadOnlyList<MutasiBrgStokBatchResponse> Batches
);

public record MutasiBrgStokBatchResponse(
    string NoBatch,
    string TglEd,
    decimal QtySisa,
    decimal Hpp
);

public class MutasiBrgListStokLokasiHandler
    : IRequestHandler<MutasiBrgListStokLokasiQuery, IEnumerable<MutasiBrgListStokLokasiResponse>>
{
    private readonly IStokLokasiDal _stokLokasiDal;

    public MutasiBrgListStokLokasiHandler(IStokLokasiDal stokLokasiDal)
    {
        _stokLokasiDal = stokLokasiDal;
    }

    public Task<IEnumerable<MutasiBrgListStokLokasiResponse>> Handle(
        MutasiBrgListStokLokasiQuery request,
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.LayananId, nameof(request.LayananId));

        var list = _stokLokasiDal.ListStokLokasi(request.LayananId) ?? [];

        var filtered = list.Where(x => x.TotalStok > 0);

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var kw = request.Keyword.Trim();
            filtered = filtered.Where(x =>
                x.BrgName.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                x.BrgId.Contains(kw, StringComparison.OrdinalIgnoreCase));
        }

        var response = filtered.Select(x => new MutasiBrgListStokLokasiResponse(
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
                .Select(b => new MutasiBrgStokBatchResponse(
                    b.NoBatch,
                    b.TglEd == default ? string.Empty : b.TglEd.ToString("yyyy-MM-dd"),
                    b.QtySisa,
                    b.Hpp
                )).ToList()
        ));

        return Task.FromResult(response);
    }
}
