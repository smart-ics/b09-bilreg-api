using Bilreg.Domain.PasienContext.StatusSosialSub.AgamaAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.PasienContext.StatusSosialSub.AgamaAgg;

public record AgamaListQuery() : IRequest<IEnumerable<AgamaListResponse>>;

public record AgamaListResponse(string AgamaId, string AgamaName);

public class AgamaListHandler : IRequestHandler<AgamaListQuery, IEnumerable<AgamaListResponse>>
{
    private readonly IAgamaDal _agamaDal;

    public AgamaListHandler(IAgamaDal agamaDal)
    {
        _agamaDal = agamaDal;
    }

    public Task<IEnumerable<AgamaListResponse>> Handle(AgamaListQuery request, CancellationToken cancellationToken)
    {
        //  QUERY
        var result = _agamaDal.ListData()
            ?? throw new KeyNotFoundException($"Agama not found");

        //  RESPONSE
        var response = result.Select(x => new AgamaListResponse(x.AgamaId, x.AgamaName));
        return Task.FromResult(response);
    }

}
