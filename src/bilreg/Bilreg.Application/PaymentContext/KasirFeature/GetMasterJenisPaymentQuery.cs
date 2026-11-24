using Ardalis.GuardClauses;
using MediatR;

namespace Bilreg.Application.PaymentContext.KasirFeature;

public record GetMasterJenisPaymentQuery(string? Kategori = null) : IRequest<IEnumerable<JenisPaymentGroupDto>>;

public class GetMasterJenisPaymentHandler : IRequestHandler<GetMasterJenisPaymentQuery, IEnumerable<JenisPaymentGroupDto>>
{
    private readonly ITrsJenisPaymentDal _jenisPaymentDal;

    public GetMasterJenisPaymentHandler(ITrsJenisPaymentDal jenisPaymentDal)
    {
        _jenisPaymentDal = jenisPaymentDal;
    }

    public Task<IEnumerable<JenisPaymentGroupDto>> Handle(GetMasterJenisPaymentQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));

        var groups = _jenisPaymentDal.ListAktifGrouped() ?? Enumerable.Empty<JenisPaymentGroupDto>();

        if (!string.IsNullOrWhiteSpace(request.Kategori))
        {
            var targetKategori = request.Kategori.Trim();
            groups = groups.Where(g => string.Equals(g.Kategori, targetKategori, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        return Task.FromResult(groups);
    }
}
