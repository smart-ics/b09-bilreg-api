using Bilreg.Domain.Shared.Param;
using MediatR;

namespace Bilreg.Application.PasienContext.DigitalSignFeature;

public record ResolvePatientSignerQuery(string NoMr) : IRequest<ResolvePatientSignerResponse>;

public record ResolvePatientSignerResponse(
    HiDokPatientSignerResolveStatus Status,
    string UserrId,
    string SignerId,
    string ErrorMessage);

public class ResolvePatientSignerQueryHandler : IRequestHandler<ResolvePatientSignerQuery, ResolvePatientSignerResponse>
{
    private readonly IGetKodeRsService _getKodeRs;
    private readonly IHiDokPatientSignerResolveClient _client;

    public ResolvePatientSignerQueryHandler(IGetKodeRsService getKodeRs,
        IHiDokPatientSignerResolveClient client)
    {
        _getKodeRs = getKodeRs;
        _client = client;
    }

    public Task<ResolvePatientSignerResponse> Handle(ResolvePatientSignerQuery request, CancellationToken cancellationToken)
    {
        var hospitalId = _getKodeRs.Execute();

        var result = _client.Execute(new HiDokPatientSignerResolveRequest(hospitalId, request.NoMr));

        var response = new ResolvePatientSignerResponse(
            result.Status, result.UserrId, result.SignerId, result.ErrorMessage);
        return Task.FromResult(response);
    }
}