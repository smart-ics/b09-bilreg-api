using Bilreg.Domain.AdmisiContext.LayananSub;
using MediatR;

namespace Bilreg.Application.AdmisiContext.LayananSub.LayananAgg;

public record LayananGetQuery(string LayananId) : IRequest<LayananGetResponse>, ILayananKey;
public record LayananGetResponse(
    string LayananId,
    string LayananName,
    bool IsAktif,
    string InstalasiId,
    string InstalasiName,
    string InstalasiDkId,
    string InstalasiDkName,
    string LayananDkId,
    string LayananDkName,
    string LayananTipeDkId,
    string LayananTipeDkName
    );

public class LayananGetHandler : IRequestHandler<LayananGetQuery, LayananGetResponse>
{
    private readonly ILayananDal _layananDal;

    public LayananGetHandler(ILayananDal layananDal)
    {
        _layananDal = layananDal;
    }
    public Task<LayananGetResponse> Handle(LayananGetQuery request, CancellationToken cancellationToken)
        => _layananDal.GetData(LayananType.Key(request.LayananId))
        .Match(
            onSome: x => Task.FromResult(new LayananGetResponse(x.LayananId, x.LayananName,
                            x.IsAktif, x.Instalasi.InstalasiId, x.Instalasi.InstalasiName,
                            x.InstalasiDk.InstalasiDkId, x.InstalasiDk.InstalasiDkName,
                            x.LayananDk.LayananDkId, x.LayananDk.LayananDkName,
                            x.TipeLayananDk.TipeLayananDkId, x.TipeLayananDk.TipeLayananDkName)),
            onNone: () => throw new KeyNotFoundException($"Layanan {request.LayananId} not found"));
}