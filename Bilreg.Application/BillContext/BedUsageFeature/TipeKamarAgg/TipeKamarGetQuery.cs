using Bilreg.Domain.BillContext.BedUsageFeature.TipeKamarAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.BillContext.RoomChargeSub.TipeKamarAgg;

public record TipeKamarGetQuery(string TipeKamarId): IRequest<TipeKamarGetResponse>, ITipeKamarKey;
public record TipeKamarGetResponse(
    string TipeKamarId,
    string TipeKamarName,
    bool IsGabung,
    bool IsAktif,
    bool IsDefault,
    int NoUrut
    );

public class TipeKamarGetHandler : IRequestHandler<TipeKamarGetQuery, TipeKamarGetResponse>
{
    private readonly ITipeKamarDal _tipeKamarDal;

    public TipeKamarGetHandler(ITipeKamarDal tipeKamarDal)
    {
        _tipeKamarDal = tipeKamarDal;
    }

    public Task<TipeKamarGetResponse> Handle(TipeKamarGetQuery request, CancellationToken cancellationToken)
    {
        var tipeKamar = _tipeKamarDal.GetData(request)
            ?? throw new KeyNotFoundException($"Tipe Kamar Id {request.TipeKamarId} Not Found");
        var response = new TipeKamarGetResponse(
            tipeKamar.TipeKamarId,
            tipeKamar.TipeKamarName,
            tipeKamar.IsGabung,
            tipeKamar.IsAktif,
            tipeKamar.IsDefault,
            tipeKamar.NoUrut
        );
        return Task.FromResult(response);
    }
}