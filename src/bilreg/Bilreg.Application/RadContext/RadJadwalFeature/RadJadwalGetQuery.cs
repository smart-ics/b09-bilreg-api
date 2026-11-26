using Ardalis.GuardClauses;
using Bilreg.Domain.RadContext.RadJadwalFeature;
using MediatR;

namespace Bilreg.Application.RadContext.RadJadwalFeature;

public record RadJadwalGetQuery(string JadwalRadId) : IRequest<RadJadwalDetailResponse?>, IRadJadwalKey;

public record RadJadwalDetailResponse(
    RadJadwalDto Header,
    IReadOnlyList<RadJadwalRescheduleDto> RescheduleHistory);

public class RadJadwalGetHandler : IRequestHandler<RadJadwalGetQuery, RadJadwalDetailResponse?>
{
    private readonly IRadJadwalDal _jadwalDal;

    public RadJadwalGetHandler(IRadJadwalDal jadwalDal)
    {
        _jadwalDal = jadwalDal;
    }

    public Task<RadJadwalDetailResponse?> Handle(RadJadwalGetQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.JadwalRadId, nameof(request.JadwalRadId));

        var header = _jadwalDal.GetData(request);
        if (header is null)
            return Task.FromResult<RadJadwalDetailResponse?>(null);

        var history = _jadwalDal
            .ListRescheduleHistory(request.JadwalRadId.Trim())
            .OrderBy(x => x.RescheduleIndex)
            .ToList();

        var response = new RadJadwalDetailResponse(header, history);
        return Task.FromResult<RadJadwalDetailResponse?>(response);
    }
}
