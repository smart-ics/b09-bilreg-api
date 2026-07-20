using Bilreg.Application.Shared;
using MediatR;

namespace Bilreg.Application.Shared.BusinessDateFeature;

public record GetBusinessDateStatusQry() : IRequest<BusinessDateStatusResponse>;

public record BusinessDateStatusResponse(
    string Mode,
    string? BusinessDate,
    string BusinessNow,
    string SystemDate,
    string SystemNow,
    bool IsSimulation);

public class GetBusinessDateStatusHandler
    : IRequestHandler<GetBusinessDateStatusQry, BusinessDateStatusResponse>
{
    private readonly IBusinessDateStatus _status;

    public GetBusinessDateStatusHandler(IBusinessDateStatus status)
    {
        _status = status;
    }

    public Task<BusinessDateStatusResponse> Handle(
        GetBusinessDateStatusQry request,
        CancellationToken cancellationToken)
    {
        var businessNow = _status.BusinessNow;
        var systemNow = _status.SystemNow;

        return Task.FromResult(new BusinessDateStatusResponse(
            _status.Mode,
            DateOnly.FromDateTime(businessNow).ToString("yyyy-MM-dd"),
            businessNow.ToString("yyyy-MM-ddTHH:mm:ss"),
            DateOnly.FromDateTime(systemNow).ToString("yyyy-MM-dd"),
            systemNow.ToString("yyyy-MM-ddTHH:mm:ss"),
            _status.IsSimulation));
    }
}
