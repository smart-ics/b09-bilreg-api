using Bilreg.Domain.PasienContext.StatusSosialFeature;
using JetBrains.Annotations;
using MediatR;

namespace Bilreg.Application.PasienContext.StatusSosialFeature;

public record StatusKawinDkGetQuery(string StatusKawinDkId) : IRequest<StatusKawinDkGetResponse>;

[PublicAPI]
public record StatusKawinDkGetResponse(string StatusKawinDkId, string StatusKawinDkName);

public class StatusKawinDkGetHandler : IRequestHandler<StatusKawinDkGetQuery, StatusKawinDkGetResponse>
{
    private readonly IStatusKawinDkDal _statusKawinDkDal;

    public StatusKawinDkGetHandler(IStatusKawinDkDal statusKawinDkDal)
    {
        _statusKawinDkDal = statusKawinDkDal;
    }

    public Task<StatusKawinDkGetResponse> Handle(StatusKawinDkGetQuery request, CancellationToken cancellationToken)
        =>  _statusKawinDkDal.GetData(StatusKawinDkType.Key(request.StatusKawinDkId))
            .Match(
                onSome: x => Task.FromResult(new StatusKawinDkGetResponse(x.StatusKawinDkId, x.StatusKawinDkName)), 
                onNone: () => throw new KeyNotFoundException($"StatusKawinDk {request.StatusKawinDkId} not found"));
}