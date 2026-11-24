using Bilreg.Application.AdmisiContext.AktivasiBerkasFeature.Integration;
using MediatR;

namespace Bilreg.Application.AdmisiContext.AktivasiBerkasFeature.UseCases;

/// <summary>
/// Query MediatR untuk mencari register kunjungan non-aktif berdasarkan kata kunci (RegId, MR, Nama, TglLahir).
/// </summary>
public record AktivasiBerkasFindRegisterQuery(string Keyword) : IRequest<IEnumerable<AktivasiBerkasRegisterItemDto>>;

public class AktivasiBerkasFindRegisterHandler : IRequestHandler<AktivasiBerkasFindRegisterQuery, IEnumerable<AktivasiBerkasRegisterItemDto>>
{
    private readonly IAktivasiBerkasEmrGateway _gateway;

    public AktivasiBerkasFindRegisterHandler(IAktivasiBerkasEmrGateway gateway)
    {
        _gateway = gateway;
    }

    public async Task<IEnumerable<AktivasiBerkasRegisterItemDto>> Handle(
        AktivasiBerkasFindRegisterQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Keyword))
            return [];

        return await _gateway.FindRegister(request.Keyword, cancellationToken);
    }
}
