using Ardalis.GuardClauses;
using Bilreg.Domain.Shared.Helpers;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.InventoryContext.MutasiFeature.UseCases;

public record TerimaMutasiListQuery(string Tgl1, string Tgl2, string LayananTujuanId = "")
    : IRequest<IEnumerable<TerimaMutasiHeaderView>>
{
    public TerimaMutasiListQuery(Periode periode, string layananTujuanId = "")
        : this(periode.Tgl1.ToString("yyyy-MM-dd"), periode.Tgl2.ToString("yyyy-MM-dd"), layananTujuanId)
    {
    }

    public TerimaMutasiListQuery(DateTime tgl1, DateTime tgl2, string layananTujuanId = "")
        : this(tgl1.ToString("yyyy-MM-dd"), tgl2.ToString("yyyy-MM-dd"), layananTujuanId)
    {
    }
}

public class TerimaMutasiListHandler : IRequestHandler<TerimaMutasiListQuery, IEnumerable<TerimaMutasiHeaderView>>
{
    private readonly ITerimaMutasiRepo _terimaMutasiRepo;

    public TerimaMutasiListHandler(ITerimaMutasiRepo terimaMutasiRepo)
    {
        _terimaMutasiRepo = terimaMutasiRepo;
    }

    public Task<IEnumerable<TerimaMutasiHeaderView>> Handle(TerimaMutasiListQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.Tgl1, nameof(request.Tgl1));
        Guard.Against.NullOrWhiteSpace(request.Tgl2, nameof(request.Tgl2));
        Guard.Against.InvalidDateFormat(request.Tgl1, nameof(request.Tgl1));
        Guard.Against.InvalidDateFormat(request.Tgl2, nameof(request.Tgl2));

        var tgl1 = request.Tgl1.ToDate("yyyy-MM-dd");
        var tgl2 = request.Tgl2.ToDate("yyyy-MM-dd");
        var periode = new Periode(tgl1, tgl2);
        var layananTujuanId = request.LayananTujuanId?.Trim() ?? string.Empty;

        var list = _terimaMutasiRepo.ListData(periode, layananTujuanId)?.ToList() ?? [];
        return Task.FromResult<IEnumerable<TerimaMutasiHeaderView>>(list);
    }
}
