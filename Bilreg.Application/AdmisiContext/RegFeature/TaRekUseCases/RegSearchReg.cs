using MediatR;

namespace Bilreg.Application.AdmisiContext.RegFeature.TaRekUseCases;

public record RegSearchReg(string Keyword) : IRequest<RegSearchRegResponse>;
public record RegSearchRegResponse(string RegId, string RegDate,
    string PasienId, string PasienName, string LayananName, string DokterName,
    string JenisRegString);

public class RegSearchRegHandler : IRequestHandler<RegSearchReg, RegSearchRegResponse>
{
    public Task<RegSearchRegResponse> Handle(RegSearchReg request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
