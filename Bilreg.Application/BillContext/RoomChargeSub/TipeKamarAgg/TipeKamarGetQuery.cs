using Bilreg.Domain.BillContext.RoomChargeSub.TipeKamarAgg;
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
public class TipeKamarGetHandlerTest
{
    private readonly Mock<ITipeKamarDal> _tipeKamarDal;
    private readonly TipeKamarGetHandler _sut;

    public TipeKamarGetHandlerTest()
    {
        _tipeKamarDal = new Mock<ITipeKamarDal>();
        _sut = new TipeKamarGetHandler(_tipeKamarDal.Object);
    }

    [Fact]
    public async Task GivenInvalidTipeKamarId_ThenThrowKeyNotFoundException()
    {
        var request = new TipeKamarGetQuery("A");
        _tipeKamarDal.Setup(x => x.GetData(It.IsAny<ITipeKamarKey>()))
            .Returns(null as TipeKamarModel);
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<KeyNotFoundException>();
            
    }
    
    [Fact]
    public async Task GivenvalidTipeKamarId_ThenThrowKeyNotFoundException()
    {
        var request = new TipeKamarGetQuery("A");
        var expected = new TipeKamarModel("A","B");
        _tipeKamarDal.Setup(x => x.GetData(It.IsAny<ITipeKamarKey>()))
            .Returns(expected);
        var actual = await _sut.Handle(request, CancellationToken.None);
        actual.Should().BeEquivalentTo(expected);
        
    }
}