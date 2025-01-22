using Bilreg.Domain.PasienContext.StatusSosialSub.PekerjaanDkAgg;
using MediatR;

namespace Bilreg.Application.PasienContext.StatusSosialSub.PekerjaanDkAgg;

public record PekerjaanDkGetQuery(string PekerjaanDkId) : IRequest<PekerjaanDkGetResponse>, IPekerjaanDkKey;

public record PekerjaanDkGetResponse(string PekerjaanDkId, string PekerjaanDkName);

public class PekerjaanDkGetHandler : IRequestHandler<PekerjaanDkGetQuery, PekerjaanDkGetResponse>
{
    private readonly IPekerjaanDkDal _pekerjaanDkDal;

    public PekerjaanDkGetHandler(IPekerjaanDkDal pekerjaanDkDal)
    {
        _pekerjaanDkDal = pekerjaanDkDal;
    }

    public Task<PekerjaanDkGetResponse> Handle(PekerjaanDkGetQuery request, CancellationToken cancellationToken)
    {
        //  QUERY
        var result = _pekerjaanDkDal.GetData(request)
            ?? throw new KeyNotFoundException($"Pekerjaan not found: {request.PekerjaanDkId}");

        //  RESPONSE
        var response = new PekerjaanDkGetResponse(result.PekerjaanDkId, result.PekerjaanDkName);
        return Task.FromResult(response);
    }
}