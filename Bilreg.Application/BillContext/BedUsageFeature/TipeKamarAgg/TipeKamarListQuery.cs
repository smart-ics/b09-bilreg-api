using Bilreg.Domain.BillContext.BedUsageFeature.TipeKamarAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.BillContext.RoomChargeSub.TipeKamarAgg;

public record TipeKamarListQuery():IRequest<IEnumerable<TipeKamarListResponse>>;
public record TipeKamarListResponse(
    string TipeKamarId,
    string TipeKamarName,
    bool IsGabung,
    bool IsAktif,
    bool IsDefault,
    int NoUrut
    );

public class TipeKamarListHandler : IRequestHandler<TipeKamarListQuery, IEnumerable<TipeKamarListResponse>>
{
    private readonly ITipeKamarDal _tipeKamarDal;

    public TipeKamarListHandler(ITipeKamarDal tipeKamarDal)
    {
        _tipeKamarDal = tipeKamarDal;
    }

    public Task<IEnumerable<TipeKamarListResponse>> Handle(TipeKamarListQuery request, CancellationToken cancellationToken)
    {
        var list = _tipeKamarDal.ListData()
                   ?? throw new KeyNotFoundException("Kamar not found");
        return Task.FromResult(list.Select(x => new TipeKamarListResponse(
            x.TipeKamarId,
            x.TipeKamarName,
            x.IsGabung,
            x.IsAktif,
            x.IsDefault,
            x.NoUrut
        )));
    }
}