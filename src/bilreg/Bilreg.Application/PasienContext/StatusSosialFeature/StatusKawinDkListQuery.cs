using JetBrains.Annotations;
using MediatR;

namespace Bilreg.Application.PasienContext.StatusSosialFeature;

public record StatusKawinDkListQuery : IRequest<IEnumerable<StatusKawinDkListResponse>>;

[PublicAPI]
public record StatusKawinDkListResponse(string StatusKawinDkId, string StatusKawinDkName);

public class StatusKawinDkListHandler : IRequestHandler<StatusKawinDkListQuery, IEnumerable<StatusKawinDkListResponse>>
{
    private readonly IStatusKawinDkDal _statusKawinDkDal;

    public StatusKawinDkListHandler(IStatusKawinDkDal statusKawinDkDal)
    {
        _statusKawinDkDal = statusKawinDkDal;
    }

    public Task<IEnumerable<StatusKawinDkListResponse>> Handle(StatusKawinDkListQuery request, CancellationToken cancellationToken)
        => _statusKawinDkDal.ListData()
            .Match(
                onSome: x => Task.FromResult(x.Select(y 
                    => new StatusKawinDkListResponse(y.StatusKawinDkId, y.StatusKawinDkName))),
                onNone: () => throw new KeyNotFoundException($"StatusKawinDk not found"));
}