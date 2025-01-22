using Bilreg.Domain.PasienContext.StatusSosialSub.StatusKawinDkAgg;
using MediatR;

namespace Bilreg.Application.PasienContext.StatusSosialSub.StatusKawinDkAgg;

public record StatusKawinDkGetQuery(string StatusKawinDkId) : IRequest<StatusKawinDkGetResponse>, IStatusKawinDkKey;
public record StatusKawinDkGetResponse(string StatusKawinDkId, string StatusKawinDkName);


public class StatusKawinDkGetHandler : IRequestHandler<StatusKawinDkGetQuery, StatusKawinDkGetResponse>
{
    private readonly IStatusKawinDkDal _statuskawinDkDal;

    public StatusKawinDkGetHandler(IStatusKawinDkDal statuskawinDkDal)
    {
        _statuskawinDkDal = statuskawinDkDal;
    }

    public Task<StatusKawinDkGetResponse> Handle(StatusKawinDkGetQuery request, CancellationToken cancellationToken)
    {
        //  QUERY
        var result = _statuskawinDkDal.GetData(request)
                     ?? throw new KeyNotFoundException($"Status Kawin not found: {request.StatusKawinDkId}");

        //  RESPONSE
        var response = new StatusKawinDkGetResponse(result.StatusKawinDkId, result.StatusKawinDkName);
        return Task.FromResult(response);
    }
}