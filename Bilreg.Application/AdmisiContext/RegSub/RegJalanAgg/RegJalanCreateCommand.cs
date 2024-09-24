using Bilreg.Domain.AdmisiContext.RegSub.RegAgg;
using MediatR;

namespace Bilreg.Application.AdmisiContext.RegSub.RegJalanAgg;

public record RegJalanCreateCommand(
    string PasienId, DateTime RegDate, 
    string LayananId, string DokterId, 
    string TipeJaminan, string PolisId, 
    string CaraMasukId, string RujukanId, 
    string KarcisId) : IRequest<RegJalanCreateResponse>;

public record RegJalanCreateResponse(string RegId);

public class RegJalanCreateHandler : IRequestHandler<RegJalanCreateCommand, RegJalanCreateResponse>
{
    public Task<RegJalanCreateResponse> Handle(RegJalanCreateCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}