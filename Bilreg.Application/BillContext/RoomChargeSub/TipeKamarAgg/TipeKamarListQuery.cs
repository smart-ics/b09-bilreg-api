using Bilreg.Domain.BillContext.RoomChargeSub.TipeKamarAgg;
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

public class TipeKamarListHandlerTest
{
    private readonly Mock<ITipeKamarDal> _tipeKamarDal;
    private TipeKamarListHandler _sut;
    
    public TipeKamarListHandlerTest()
    {
        _tipeKamarDal = new Mock<ITipeKamarDal>();
        _sut = new TipeKamarListHandler(_tipeKamarDal.Object);
    }

    [Fact]
    public async Task GivenEmptyData_ThenThrowKeyNotFoundException()
    {
        var request = new TipeKamarListQuery();
        _tipeKamarDal.Setup(x => x.ListData())
            .Returns(null as IEnumerable<TipeKamarModel>);
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GivenValidData_ThenReturnExpectedResponse()
    {
        var request = new TipeKamarListQuery();
        var expected = new TipeKamarModel("A", "B");
        _tipeKamarDal.Setup(x => x.ListData())
            .Returns(new List<TipeKamarModel> { expected });
        var actual = await _sut.Handle(request, CancellationToken.None);
        actual.Should().ContainEquivalentOf(expected);
    }
}