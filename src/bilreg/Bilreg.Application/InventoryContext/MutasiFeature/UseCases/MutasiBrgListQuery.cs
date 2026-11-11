using Ardalis.GuardClauses;
using Bilreg.Domain.Shared.Helpers;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.InventoryContext.MutasiFeature.UseCases;

public record MutasiBrgListQuery(string Tgl1, string Tgl2) 
    : IRequest<IEnumerable<MutasiBrgHeaderView>>
{
    public string TglYmd1 => Tgl1;
    public string TglYmd2 => Tgl2;

    public MutasiBrgListQuery(Periode periode) 
        : this(periode.Tgl1.ToString("yyyy-MM-dd"), periode.Tgl2.ToString("yyyy-MM-dd"))
    {
    }

    public MutasiBrgListQuery(DateTime tgl1, DateTime tgl2) 
        : this(tgl1.ToString("yyyy-MM-dd"), tgl2.ToString("yyyy-MM-dd"))
    {
    }
}

public class MutasiBrgListHandler : IRequestHandler<MutasiBrgListQuery, IEnumerable<MutasiBrgHeaderView>>
{
    private readonly IMutasiBrgRepo _mutasiBrgRepo;

    public MutasiBrgListHandler(IMutasiBrgRepo mutasiBrgRepo)
    {
        _mutasiBrgRepo = mutasiBrgRepo;
    }

    public Task<IEnumerable<MutasiBrgHeaderView>> Handle(MutasiBrgListQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.Tgl1, nameof(request.Tgl1));
        Guard.Against.NullOrWhiteSpace(request.Tgl2, nameof(request.Tgl2));
        Guard.Against.InvalidDateFormat(request.Tgl1, nameof(request.Tgl1));
        Guard.Against.InvalidDateFormat(request.Tgl2, nameof(request.Tgl2));

        var tgl1 = request.Tgl1.ToDate("yyyy-MM-dd");
        var tgl2 = request.Tgl2.ToDate("yyyy-MM-dd");
        var periode = new Periode(tgl1, tgl2);

        var list = _mutasiBrgRepo.ListData(periode)?.ToList() ?? [];
        return Task.FromResult<IEnumerable<MutasiBrgHeaderView>>(list);
    }
}
